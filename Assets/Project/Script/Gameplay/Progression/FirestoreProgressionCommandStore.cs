using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerMath.Gameplay.Academic;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine.Networking;

namespace PowerMath.Gameplay.Progression
{
    public sealed class FirestoreProgressionCommandStore
    {
        private readonly GameApiSettings _settings;
        private readonly PlayerSnapshot _player;
        private readonly QuestionCatalog _catalog;
        private readonly int _baseWeaponAttack;
        private readonly string _username;
        private readonly string _url;

        public FirestoreProgressionCommandStore(
            GameApiSettings settings,
            PlayerSnapshot player,
            QuestionCatalog catalog)
            : this(settings, player, catalog,
                WeaponAscensionPolicy.DefaultBaseWeaponAttack)
        {
        }

        public FirestoreProgressionCommandStore(
            GameApiSettings settings,
            PlayerSnapshot player,
            QuestionCatalog catalog,
            int baseWeaponAttack)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (baseWeaponAttack < 0)
                throw new ArgumentOutOfRangeException(nameof(baseWeaponAttack));
            _baseWeaponAttack = baseWeaponAttack;
            int separator = (player.playerId ?? string.Empty).IndexOf(':');
            if (separator <= 0 || separator >= player.playerId.Length - 1 ||
                !_settings.TryGetLevelDocumentById(player.playerId.Substring(0, separator), out _url))
                throw new ArgumentException("Player ID cannot resolve its Firestore document.", nameof(player));
            _username = player.playerId.Substring(separator + 1);
        }

        public IEnumerator Settle(
            RunSettlementType type,
            Action<RunSettlementAward> completed,
            Action<string> failed)
        {
            string runId = _player.activeRun?.runId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(runId))
            {
                failed?.Invoke("This run has no settlement identity. Reload and try again.");
                yield break;
            }
            if (string.Equals(_player.lastRunSettlement?.runId, runId, StringComparison.Ordinal))
            {
                completed?.Invoke(new RunSettlementAward(
                    _player.lastRunSettlement.stageReached,
                    _player.lastRunSettlement.powerCoinsGranted,
                    _player.lastRunSettlement.legacyAtkBasisPointsGranted,
                    _player.lastRunSettlement.prestigeGranted));
                yield break;
            }
            if (!RunSettlementPolicy.CanSettle(_player, type, out string reason))
            {
                failed?.Invoke(reason);
                yield break;
            }

            RunSettlementAward award = RunSettlementPolicy.Calculate(_player, type);
            AcademicPersistenceSnapshot reset;
            try
            {
                reset = CreateResetAcademic();
            }
            catch (Exception exception)
            {
                failed?.Invoke("Question progress could not be reset: " + exception.Message);
                yield break;
            }

            long nextCoins = checked((_player.wallet?.powerCoins ?? 0) + award.PowerCoins);
            long nextLegacy = checked((_player.progression?.legacyAtkBonusBasisPoints ?? 0) + award.LegacyBasisPoints);
            int nextPrestige = checked((_player.progression?.prestige ?? 0) + award.Prestige);
            long nextRevision = checked(_player.revision + 1);
            string nextRunId = Guid.NewGuid().ToString("N");
            var builder = new FirestorePatchDocumentBuilder();
            string[] root = { _username, "gamedata" };
            builder.AddInteger(Join(root, "revision"), nextRevision);
            builder.AddInteger(Join(root, "wallet", "powerCoins"), nextCoins);
            builder.AddInteger(Join(root, "progression", "currentStage"), 1);
            builder.AddInteger(Join(root, "progression", "legacyAtkBonusBasisPoints"), nextLegacy);
            builder.AddInteger(Join(root, "progression", "prestige"), nextPrestige);
            builder.AddInteger(Join(root, "academic", "auditScore"), 0);
            builder.AddInteger(Join(root, "academic", "auditResolvedCount"), 0);
            builder.AddNull(Join(root, "academic", "activeAttempt"));
            AddAcademicInventory(builder, root, "silver", reset.Silver);
            AddAcademicInventory(builder, root, "gold", reset.Gold);
            AddAcademicInventory(builder, root, "diamond", reset.Diamond);
            builder.AddString(Join(root, "activeRun", "runId"), nextRunId);
            builder.AddInteger(Join(root, "activeRun", "currentStage"), 1);
            builder.AddString(Join(root, "activeRun", "committedAttemptId"), string.Empty);
            foreach (string field in new[] { "biomeId", "biomeTitle", "encounterKind", "encounterId", "questionContentKind", "questionDocumentId" })
                builder.AddString(Join(root, "activeRun", field), string.Empty);
            builder.AddString(Join(root, "activeRun", "enemyId"), string.Empty);
            foreach (string field in new[] { "questionId", "eventAttemptOrdinal", "enemyCurrentHp", "enemyMaximumHp", "enemyRemainingCooldown", "enemyMaximumCooldown", "playerCurrentHearts", "playerMaximumHearts", "silverEarned", "goldEarned", "diamondEarned" })
                builder.AddInteger(Join(root, "activeRun", field), 0);
            builder.AddInteger(Join(root, "activeRun", "bonusMultiplierBasisPoints"), 10000);
            builder.AddString(Join(root, "activeRun", "phase"), "EnemyReady");
            builder.AddString(Join(root, "lastRunSettlement", "runId"), runId);
            builder.AddString(Join(root, "lastRunSettlement", "type"), type.ToString());
            builder.AddInteger(Join(root, "lastRunSettlement", "stageReached"), award.StageReached);
            builder.AddInteger(Join(root, "lastRunSettlement", "powerCoinsGranted"), award.PowerCoins);
            builder.AddInteger(Join(root, "lastRunSettlement", "legacyAtkBasisPointsGranted"), award.LegacyBasisPoints);
            builder.AddInteger(Join(root, "lastRunSettlement", "prestigeGranted"), award.Prestige);
            builder.AddInteger(Join(root, "lastRunSettlement", "resultingPowerCoins"), nextCoins);

            bool saved = false;
            string failure = string.Empty;
            yield return Patch(builder.Build(), () => saved = true, message => failure = message);
            if (!saved)
            {
                failed?.Invoke(failure);
                yield break;
            }

            ApplySettlement(type, award, reset, runId, nextRunId, nextCoins, nextLegacy, nextPrestige, nextRevision);
            completed?.Invoke(award);
        }

        public IEnumerator AscendWeapon(
            string transactionId,
            Action<WeaponAscensionStats, long> completed,
            Action<string> failed)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) throw new ArgumentException("Transaction ID is required.", nameof(transactionId));
            _player.economy = _player.economy ?? new PlayerSnapshot.EconomyData();
            if (string.Equals(_player.economy.lastWeaponAscendTransactionId, transactionId, StringComparison.Ordinal))
            {
                completed?.Invoke(WeaponAscensionPolicy.GetStats(
                    _player.economy.lastWeaponAscendLevel,
                    _baseWeaponAttack), _player.economy.lastWeaponAscendCost);
                yield break;
            }
            if (!string.IsNullOrEmpty(_player.activeRun?.committedAttemptId) ||
                string.Equals(_player.activeRun?.phase, "RunDefeat", StringComparison.Ordinal))
            {
                failed?.Invoke("Weapon Ascend is unavailable during a question or before settling defeat.");
                yield break;
            }

            var inventory = (_player.inventory ?? Array.Empty<PlayerSnapshot.InventoryItemData>())
                .Where(item => item != null)
                .Select(Clone).ToList();
            List<PlayerSnapshot.InventoryItemData> matches = inventory
                .Where(item => item.itemId == WeaponAscensionPolicy.CanonicalItemId).ToList();
            if (matches.Count > 1)
            {
                failed?.Invoke("Duplicate Weapon Ascend inventory records require data repair.");
                yield break;
            }
            PlayerSnapshot.InventoryItemData weapon;
            if (matches.Count == 0)
            {
                weapon = new PlayerSnapshot.InventoryItemData
                {
                    itemId = WeaponAscensionPolicy.CanonicalItemId,
                    upgradeLevel = 0,
                    owned = true
                };
                inventory.Add(weapon);
            }
            else weapon = matches[0];

            if (weapon.upgradeLevel >= WeaponAscensionPolicy.MaximumLevel)
            {
                failed?.Invoke("Your weapon has reached Level 100.");
                yield break;
            }
            long cost = WeaponAscensionPolicy.GetNextCost(weapon.upgradeLevel);
            if ((_player.wallet?.powerCoins ?? 0) < cost)
            {
                failed?.Invoke($"You need {cost} Power Coins to ascend.");
                yield break;
            }
            weapon.upgradeLevel++;
            long nextCoins = checked(_player.wallet.powerCoins - cost);
            long nextRevision = checked(_player.revision + 1);
            var builder = new FirestorePatchDocumentBuilder();
            string[] root = { _username, "gamedata" };
            builder.AddInteger(Join(root, "revision"), nextRevision);
            builder.AddInteger(Join(root, "wallet", "powerCoins"), nextCoins);
            builder.AddInventoryArray(Join(root, "inventory"), inventory);
            builder.AddString(Join(root, "loadout", "weaponId"), WeaponAscensionPolicy.CanonicalItemId);
            builder.AddString(Join(root, "economy", "lastWeaponAscendTransactionId"), transactionId);
            builder.AddInteger(Join(root, "economy", "lastWeaponAscendLevel"), weapon.upgradeLevel);
            builder.AddInteger(Join(root, "economy", "lastWeaponAscendCost"), cost);

            bool saved = false;
            string failure = string.Empty;
            yield return Patch(builder.Build(), () => saved = true, message => failure = message);
            if (!saved)
            {
                failed?.Invoke(failure);
                yield break;
            }
            _player.inventory = inventory.ToArray();
            _player.loadout = _player.loadout ?? new PlayerSnapshot.LoadoutData();
            _player.loadout.weaponId = WeaponAscensionPolicy.CanonicalItemId;
            _player.wallet.powerCoins = nextCoins;
            _player.revision = nextRevision;
            _player.economy.lastWeaponAscendTransactionId = transactionId;
            _player.economy.lastWeaponAscendLevel = weapon.upgradeLevel;
            _player.economy.lastWeaponAscendCost = cost;
            completed?.Invoke(WeaponAscensionPolicy.GetStats(
                weapon.upgradeLevel,
                _baseWeaponAttack), cost);
        }

        private AcademicPersistenceSnapshot CreateResetAcademic()
        {
            if (!AcademicRank.TryParseExact(_player.progression.activeRank, out AcademicRank rank))
                throw new InvalidOperationException("Active Rank is invalid.");
            var balances = new RankCurrencyBalances(_player.wallet.silver, _player.wallet.gold, _player.wallet.diamond);
            return new AcademicProgressionEngine(_catalog).CreateInitialState(rank, balances).ExportPersistence();
        }

        private IEnumerator Patch(FirestorePatchPlan plan, Action completed, Action<string> failed)
        {
            string updateTime = string.Empty;
            using (UnityWebRequest get = UnityWebRequest.Get(_url))
            {
                get.timeout = _settings.RequestTimeoutSeconds;
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success ||
                    !FirestoreJsonNavigator.TryParse(get.downloadHandler.text, out JsonValue root, out _) ||
                    !root.TryGet("updateTime", out JsonValue update) || update.Kind != JsonValueKind.String)
                {
                    failed?.Invoke("Could not refresh player data before saving.");
                    yield break;
                }
                if (!TryReadAuthoritativeRevision(root, out long revision) || revision != _player.revision)
                {
                    failed?.Invoke("Player data changed on another client. Reload before trying again.");
                    yield break;
                }
                updateTime = update.Text;
            }
            var address = new StringBuilder(_url);
            string separator = _url.IndexOf('?') >= 0 ? "&" : "?";
            foreach (string path in plan.FieldPaths)
            {
                address.Append(separator).Append("updateMask.fieldPaths=").Append(Uri.EscapeDataString(path));
                separator = "&";
            }
            address.Append(separator).Append("currentDocument.updateTime=").Append(Uri.EscapeDataString(updateTime));
            using (var patch = new UnityWebRequest(address.ToString(), "PATCH"))
            {
                patch.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(plan.ToJson()));
                patch.downloadHandler = new DownloadHandlerBuffer();
                patch.timeout = _settings.RequestTimeoutSeconds;
                patch.SetRequestHeader("Content-Type", "application/json");
                yield return patch.SendWebRequest();
                if (patch.result != UnityWebRequest.Result.Success)
                {
                    failed?.Invoke(patch.responseCode == 409 || patch.responseCode == 412
                        ? "Player data changed on another client. Reload before trying again."
                        : "The progression change could not be saved.");
                    yield break;
                }
            }
            completed?.Invoke();
        }

        private bool TryReadAuthoritativeRevision(JsonValue document, out long revision)
        {
            revision = 0;
            return FirestoreJsonNavigator.TryGetDocumentFields(document, out JsonValue fields) &&
                fields.TryGet(_username, out JsonValue studentValue) &&
                FirestoreJsonNavigator.TryGetMapFields(studentValue, out JsonValue student) &&
                student.TryGet("gamedata", out JsonValue gameDataValue) &&
                FirestoreJsonNavigator.TryGetMapFields(gameDataValue, out JsonValue gameData) &&
                gameData.TryGet("revision", out JsonValue revisionValue) &&
                FirestoreJsonNavigator.TryReadInteger(revisionValue, out revision);
        }

        private void ApplySettlement(
            RunSettlementType type, RunSettlementAward award, AcademicPersistenceSnapshot reset,
            string runId, string nextRunId, long nextCoins, long nextLegacy, int nextPrestige, long revision)
        {
            _player.revision = revision;
            _player.wallet.powerCoins = nextCoins;
            _player.progression.currentStage = 1;
            _player.progression.legacyAtkBonusBasisPoints = nextLegacy;
            _player.progression.prestige = nextPrestige;
            _player.activeRun = new PlayerSnapshot.ActiveRunData
            {
                runId = nextRunId, currentStage = 1, phase = "EnemyReady", bonusMultiplierBasisPoints = 10000
            };
            _player.academic = new PlayerSnapshot.AcademicData
            {
                auditScore = 0, auditResolvedCount = 0,
                silver = ToPlayer(reset.Silver), gold = ToPlayer(reset.Gold), diamond = ToPlayer(reset.Diamond)
            };
            _player.lastRunSettlement = new PlayerSnapshot.RunSettlementData
            {
                runId = runId, type = type.ToString(), stageReached = award.StageReached,
                powerCoinsGranted = award.PowerCoins, legacyAtkBasisPointsGranted = award.LegacyBasisPoints,
                prestigeGranted = award.Prestige, resultingPowerCoins = nextCoins
            };
        }

        private static PlayerSnapshot.InventoryItemData Clone(PlayerSnapshot.InventoryItemData item) =>
            new PlayerSnapshot.InventoryItemData { itemId = item.itemId, upgradeLevel = item.upgradeLevel, owned = item.owned };

        private static PlayerSnapshot.RankInventoryData ToPlayer(RankQuestionInventorySnapshot value) =>
            new PlayerSnapshot.RankInventoryData
            {
                cycle = value.Cycle,
                pendingIds = value.Pending.Select(id => id.Value).ToArray(),
                failedIds = value.Failed.Select(id => id.Value).ToArray(),
                attemptedInAuditIds = value.Attempted.Select(id => id.Value).ToArray(),
                clearedInCycleIds = value.Cleared.Select(id => id.Value).ToArray()
            };

        private static void AddAcademicInventory(FirestorePatchDocumentBuilder builder, string[] root, string rank, RankQuestionInventorySnapshot value)
        {
            string[] prefix = Join(root, "academic", "inventories", rank);
            builder.AddInteger(Join(prefix, "cycle"), value.Cycle);
            builder.AddIntegerArray(Join(prefix, "pendingIds"), value.Pending.Select(id => id.Value).ToArray());
            builder.AddIntegerArray(Join(prefix, "failedIds"), value.Failed.Select(id => id.Value).ToArray());
            builder.AddIntegerArray(Join(prefix, "attemptedInAuditIds"), value.Attempted.Select(id => id.Value).ToArray());
            builder.AddIntegerArray(Join(prefix, "clearedInCycleIds"), value.Cleared.Select(id => id.Value).ToArray());
        }

        private static string[] Join(IReadOnlyList<string> left, params string[] right)
        {
            var result = new string[left.Count + right.Length];
            for (int index = 0; index < left.Count; index++) result[index] = left[index];
            for (int index = 0; index < right.Length; index++) result[left.Count + index] = right[index];
            return result;
        }
    }
}
