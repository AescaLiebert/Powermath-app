using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine.Networking;

namespace PowerMath.Gameplay.Pets
{
    public sealed class FirestorePetGachaCommandStore : IPetGachaCommandStore
    {
        private readonly GameApiSettings _settings;
        private readonly PlayerSnapshot _player;
        private readonly PetGachaCatalog _catalog;
        private readonly IPetGachaRandomSource _random;
        private readonly string _username;
        private readonly string _url;

        public FirestorePetGachaCommandStore(
            GameApiSettings settings,
            PlayerSnapshot player,
            PetGachaCatalog catalog,
            IPetGachaRandomSource random)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            int separator = (player.playerId ?? string.Empty).IndexOf(':');
            if (separator <= 0 || separator >= player.playerId.Length - 1)
            {
                throw new ArgumentException(
                    "Player ID cannot resolve its Firestore document.",
                    nameof(player));
            }
            string levelId = player.playerId.Substring(0, separator);
            _username = player.playerId.Substring(separator + 1);
            if (!_settings.TryGetPlayerDocument(levelId, _username, out _url))
            {
                throw new ArgumentException(
                    "Player ID cannot resolve its Firestore document.",
                    nameof(player));
            }
        }

        public IEnumerator Pull(
            PetGachaCommand command,
            Action<PetGachaReceipt> completed,
            Action<PetGachaFailure> failed)
        {
            if (!string.Equals(command.CatalogVersion, _catalog.Version, StringComparison.Ordinal))
            {
                failed?.Invoke(new PetGachaFailure(
                    PetGachaFailureCode.InvalidCatalog,
                    "Pet content changed. Reopen Pet Gacha to see the current probabilities."));
                yield break;
            }

            AuthoritativeState state = null;
            string updateTime = string.Empty;
            PetGachaFailure refreshFailure = default;
            yield return Refresh(
                (value, time) =>
                {
                    state = value;
                    updateTime = time;
                },
                value => refreshFailure = value);
            if (state == null)
            {
                failed?.Invoke(refreshFailure);
                yield break;
            }

            if (PetGachaTransactionPolicy.TryRecover(
                command,
                state.LastReceipt,
                out PetGachaReceipt recovered))
            {
                Apply(state, recovered);
                completed?.Invoke(recovered);
                yield break;
            }

            if (state.Revision != command.PreviewRevision)
            {
                ApplyRefreshedState(state);
                failed?.Invoke(new PetGachaFailure(
                    PetGachaFailureCode.StalePreview,
                    "Your balance or collection changed. Review the refreshed probabilities before confirming."));
                yield break;
            }
            if (!string.IsNullOrEmpty(state.CommittedAttemptId) ||
                string.Equals(state.RunPhase, "RunDefeat", StringComparison.Ordinal))
            {
                failed?.Invoke(new PetGachaFailure(
                    PetGachaFailureCode.UnavailableDuringAttempt,
                    "Finish the current question or run settlement before using Pet Gacha."));
                yield break;
            }
            long totalCost;
            try
            {
                totalCost = PetGachaTransactionPolicy.GetCost(command.PullCount);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                failed?.Invoke(new PetGachaFailure(
                    PetGachaFailureCode.InvalidSavedState,
                    exception.Message));
                yield break;
            }
            if (state.PowerCoins < totalCost)
            {
                ApplyRefreshedState(state);
                failed?.Invoke(new PetGachaFailure(
                    PetGachaFailureCode.InsufficientFunds,
                    $"You need {totalCost - state.PowerCoins:N0} more Power Coins."));
                yield break;
            }
            if (!TryCanonicalizeInventory(
                    state.Inventory,
                    out List<PlayerSnapshot.InventoryItemData> nextInventory,
                    out Dictionary<string, int> ownedPetCounts,
                    out string inventoryError))
            {
                failed?.Invoke(new PetGachaFailure(
                    PetGachaFailureCode.InvalidSavedState,
                    inventoryError));
                yield break;
            }

            bool isFirstPull = !state.FirstGachaPullCompleted;
            PetGachaReceipt receipt;
            try
            {
                receipt = PetGachaTransactionPolicy.CreateReceipt(
                    command,
                    state.PowerCoins,
                    _catalog,
                    ownedPetCounts,
                    state.PullsSinceSsr,
                    _random,
                    isFirstPull);
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is InvalidOperationException ||
                exception is OverflowException)
            {
                failed?.Invoke(new PetGachaFailure(
                    PetGachaFailureCode.InvalidSavedState,
                    "The pull could not be calculated safely: " + exception.Message));
                yield break;
            }

            foreach (PetGachaResult roll in receipt.Results)
            {
                var existingItem = nextInventory.FirstOrDefault(item =>
                    string.Equals(item.itemId, roll.PetId, StringComparison.OrdinalIgnoreCase));
                if (existingItem != null)
                {
                    if (existingItem.count != roll.PreviousCount)
                    {
                        failed?.Invoke(new PetGachaFailure(
                            PetGachaFailureCode.InvalidSavedState,
                            "Gacha receipt count does not match canonical inventory state."));
                        yield break;
                    }
                    existingItem.count = roll.ResultingCount;
                }
                else
                {
                    if (roll.PreviousCount != 0 || roll.ResultingCount != 1)
                    {
                        failed?.Invoke(new PetGachaFailure(
                            PetGachaFailureCode.InvalidSavedState,
                            "Gacha receipt count does not match a new inventory entry."));
                        yield break;
                    }
                    nextInventory.Add(new PlayerSnapshot.InventoryItemData
                    {
                        itemId = roll.PetId,
                        upgradeLevel = 0,
                        owned = true,
                        count = roll.ResultingCount
                    });
                }
            }

            // The first-pull policy owns the guaranteed pet identity. Auto-equip
            // its first result without duplicating a Sapphire-specific rule here.
            string nextEquippedPetId = state.EquippedPetId;
            if (isFirstPull && string.IsNullOrWhiteSpace(nextEquippedPetId) &&
                receipt.Results.Count > 0)
                nextEquippedPetId = receipt.Results[0].PetId;

            long nextRevision;
            try
            {
                nextRevision = checked(state.Revision + 1);
            }
            catch (OverflowException)
            {
                failed?.Invoke(new PetGachaFailure(
                    PetGachaFailureCode.InvalidSavedState,
                    "Player revision is outside the supported range."));
                yield break;
            }

            var builder = new FirestorePatchDocumentBuilder();
            string[] root = { "gamedata" };
            builder.AddInteger(Join(root, "revision"), nextRevision);
            builder.AddInteger(Join(root, "wallet", "powerCoins"), receipt.ResultingPowerCoins);
            builder.AddInventoryArray(Join(root, "inventory"), nextInventory);
            if (!string.Equals(
                    nextEquippedPetId,
                    state.EquippedPetId,
                    StringComparison.OrdinalIgnoreCase))
                builder.AddString(Join(root, "loadout", "petId"), nextEquippedPetId);
            builder.AddString(Join(root, "economy", "lastPetGachaTransactionId"), receipt.TransactionId);
            builder.AddString(Join(root, "economy", "lastPetGachaCatalogVersion"), receipt.CatalogVersion);
            builder.AddString(Join(root, "economy", "lastPetGachaPetId"), receipt.PetId);
            builder.AddBoolean(Join(root, "economy", "lastPetGachaWasNew"), receipt.WasNew);
            builder.AddInteger(Join(root, "economy", "lastPetGachaCost"), receipt.Cost);
            builder.AddInteger(
                Join(root, "economy", "lastPetGachaResultingPowerCoins"),
                receipt.ResultingPowerCoins);
            builder.AddInteger(
                Join(root, "economy", "petGachaPullsSinceSsr"),
                receipt.ResultingPityCount);
            builder.AddInteger(
                Join(root, "economy", "lastPetGachaPreviousPityCount"),
                receipt.PreviousPityCount);
            builder.AddInteger(
                Join(root, "economy", "lastPetGachaResultingPityCount"),
                receipt.ResultingPityCount);
            builder.AddPetGachaResultArray(
                Join(root, "economy", "lastPetGachaResults"),
                ToResultData(receipt.Results));
            builder.AddBoolean(
                Join(root, "economy", "firstGachaPullCompleted"),
                true);

            bool saved = false;
            PetGachaFailure saveFailure = default;
            yield return Patch(
                builder.Build(),
                updateTime,
                () => saved = true,
                value => saveFailure = value);
            if (!saved)
            {
                failed?.Invoke(saveFailure);
                yield break;
            }

            state.Revision = nextRevision;
            state.PowerCoins = receipt.ResultingPowerCoins;
            state.Inventory = nextInventory.ToArray();
            state.EquippedPetId = nextEquippedPetId;
            state.PullsSinceSsr = receipt.ResultingPityCount;
            state.LastReceipt = receipt;
            state.FirstGachaPullCompleted = true;
            Apply(state, receipt);
            completed?.Invoke(receipt);
        }

        private IEnumerator Refresh(
            Action<AuthoritativeState, string> completed,
            Action<PetGachaFailure> failed)
        {
            using (UnityWebRequest get = UnityWebRequest.Get(_url))
            {
                get.timeout = _settings.RequestTimeoutSeconds;
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success)
                {
                    failed?.Invoke(new PetGachaFailure(
                        PetGachaFailureCode.RecoverableTransport,
                        "Could not check whether the pull was saved. Retry recovery with the same pull."));
                    yield break;
                }
                if (!FirestoreJsonNavigator.TryParse(
                        get.downloadHandler.text,
                        out JsonValue document,
                        out _) ||
                    !document.TryGet("updateTime", out JsonValue updateValue) ||
                    updateValue.Kind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(updateValue.Text))
                {
                    failed?.Invoke(new PetGachaFailure(
                        PetGachaFailureCode.InvalidSavedState,
                        "Player data could not be read safely."));
                    yield break;
                }
                if (!TryMapState(document, out AuthoritativeState state, out string error))
                {
                    failed?.Invoke(new PetGachaFailure(
                        PetGachaFailureCode.InvalidSavedState,
                        string.IsNullOrEmpty(error)
                            ? "Player data could not be read safely."
                            : error));
                    yield break;
                }
                completed?.Invoke(state, updateValue.Text);
            }
        }

        private IEnumerator Patch(
            FirestorePatchPlan plan,
            string updateTime,
            Action completed,
            Action<PetGachaFailure> failed)
        {
            var address = new StringBuilder(_url);
            string separator = _url.IndexOf('?') >= 0 ? "&" : "?";
            foreach (string path in plan.FieldPaths)
            {
                address.Append(separator)
                    .Append("updateMask.fieldPaths=")
                    .Append(Uri.EscapeDataString(path));
                separator = "&";
            }
            address.Append(separator)
                .Append("currentDocument.updateTime=")
                .Append(Uri.EscapeDataString(updateTime));

            using (var patch = new UnityWebRequest(address.ToString(), "PATCH"))
            {
                patch.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(plan.ToJson()));
                patch.downloadHandler = new DownloadHandlerBuffer();
                patch.timeout = _settings.RequestTimeoutSeconds;
                patch.SetRequestHeader("Content-Type", "application/json");
                yield return patch.SendWebRequest();
                if (patch.result != UnityWebRequest.Result.Success)
                {
                    bool conflict = patch.responseCode == 409 || patch.responseCode == 412;
                    failed?.Invoke(new PetGachaFailure(
                        conflict
                            ? PetGachaFailureCode.StalePreview
                            : PetGachaFailureCode.RecoverableTransport,
                        conflict
                            ? "Player data changed before the pull was saved. Review refreshed probabilities."
                            : "The pull result is not confirmed yet. Retry recovery with the same pull."));
                    yield break;
                }
            }
            completed?.Invoke();
        }

        private bool TryMapState(
            JsonValue document,
            out AuthoritativeState state,
            out string error)
        {
            state = null;
            error = string.Empty;
            if (!FirestoreJsonNavigator.TryGetDocumentFields(document, out JsonValue fields))
            {
                if (string.IsNullOrEmpty(error)) error = "Required wallet, inventory, or revision data is invalid.";
                return false;
            }

            JsonValue gameData;
            if (fields.TryGet("gamedata", out JsonValue directGameData) &&
                FirestoreJsonNavigator.TryGetMapFields(directGameData, out gameData))
            {
            }
            else if (fields.TryGet(_username, out JsonValue studentValue) &&
                     FirestoreJsonNavigator.TryGetMapFields(studentValue, out JsonValue student) &&
                     student.TryGet("gamedata", out JsonValue nestedGameData) &&
                     FirestoreJsonNavigator.TryGetMapFields(nestedGameData, out gameData))
            {
            }
            else
            {
                if (string.IsNullOrEmpty(error)) error = "Required wallet, inventory, or revision data is invalid.";
                return false;
            }

            if (!TryReadInteger(gameData, "revision", out long revision) || revision < 0 ||
                !TryGetMap(gameData, "wallet", out JsonValue wallet) ||
                !TryReadInteger(wallet, "powerCoins", out long powerCoins) || powerCoins < 0 ||
                !TryMapInventory(gameData, out PlayerSnapshot.InventoryItemData[] inventory, out error))
            {
                if (string.IsNullOrEmpty(error)) error = "Required wallet, inventory, or revision data is invalid.";
                return false;
            }

            TryGetMap(gameData, "activeRun", out JsonValue activeRun);
            TryGetMap(gameData, "economy", out JsonValue economy);
            TryGetMap(gameData, "loadout", out JsonValue loadout);
            string transactionId = ReadString(economy, "lastPetGachaTransactionId");
            string catalogVersion = ReadString(economy, "lastPetGachaCatalogVersion");
            string petId = ReadString(economy, "lastPetGachaPetId");
            bool wasNew = ReadBoolean(economy, "lastPetGachaWasNew");
            long cost = ReadInteger(economy, "lastPetGachaCost");
            long resulting = ReadInteger(economy, "lastPetGachaResultingPowerCoins");
            long pityValue = ReadInteger(economy, "petGachaPullsSinceSsr");
            if (pityValue < 0 || pityValue >= PetGachaTransactionPolicy.SsrHardPityPulls)
            {
                error = "Saved SSR pity progress is invalid.";
                return false;
            }
            long previousPityValue = ReadInteger(economy, "lastPetGachaPreviousPityCount");
            long resultingPityValue = ReadInteger(economy, "lastPetGachaResultingPityCount");
            if (previousPityValue < 0 || previousPityValue >= PetGachaTransactionPolicy.SsrHardPityPulls ||
                resultingPityValue < 0 || resultingPityValue >= PetGachaTransactionPolicy.SsrHardPityPulls)
            {
                error = "Saved gacha receipt pity values are invalid.";
                return false;
            }
            int previousPity = (int)previousPityValue;
            int resultingPity = (int)resultingPityValue;

            PetGachaReceipt receipt = default;
            if (!string.IsNullOrEmpty(transactionId) &&
                !string.IsNullOrEmpty(catalogVersion) &&
                !string.IsNullOrEmpty(petId) &&
                cost > 0 &&
                resulting >= 0)
            {
                bool hasFullResults = TryMapGachaResults(
                    economy,
                    out PetGachaResult[] results);
                if (cost == PetGachaTransactionPolicy.MultiPullCost &&
                    (!hasFullResults || results.Length != PetGachaTransactionPolicy.MultiPullCount))
                {
                    error = "Saved 10x gacha receipt is incomplete.";
                    return false;
                }
                if (hasFullResults && results.Length > 0)
                {
                    receipt = new PetGachaReceipt(
                        transactionId, catalogVersion, results, cost, resulting,
                        previousPity, resultingPity);
                }
                else
                {
                    receipt = new PetGachaReceipt(
                        transactionId, catalogVersion, petId, wasNew, cost, resulting);
                }
            }

            bool firstGachaPullCompleted = ReadBoolean(economy, "firstGachaPullCompleted") ||
                !string.IsNullOrEmpty(transactionId);

            state = new AuthoritativeState
            {
                Revision = revision,
                PowerCoins = powerCoins,
                Inventory = inventory,
                EquippedPetId = ReadString(loadout, "petId"),
                PullsSinceSsr = (int)pityValue,
                CommittedAttemptId = ReadString(activeRun, "committedAttemptId"),
                RunPhase = ReadString(activeRun, "phase"),
                LastReceipt = receipt,
                FirstGachaPullCompleted = firstGachaPullCompleted
            };
            return true;
        }

        private bool TryCanonicalizeInventory(
            IReadOnlyList<PlayerSnapshot.InventoryItemData> inventory,
            out List<PlayerSnapshot.InventoryItemData> canonicalInventory,
            out Dictionary<string, int> owned,
            out string error)
        {
            canonicalInventory = new List<PlayerSnapshot.InventoryItemData>(inventory.Count);
            owned = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            error = string.Empty;
            var petsById = new Dictionary<string, PlayerSnapshot.InventoryItemData>(
                StringComparer.OrdinalIgnoreCase);
            foreach (PlayerSnapshot.InventoryItemData item in inventory)
            {
                if (item == null) continue;
                if (!_catalog.TryGetPet(item.itemId, out PetGachaPet pet))
                {
                    canonicalInventory.Add(Clone(item));
                    continue;
                }
                if (!item.owned || item.upgradeLevel != 0)
                {
                    error = "Saved pet ownership is inconsistent and requires data repair.";
                    return false;
                }
                int count = Math.Max(1, item.count);

                if (petsById.TryGetValue(pet.Id, out PlayerSnapshot.InventoryItemData existing))
                {
                    try
                    {
                        existing.count = checked(existing.count + count);
                    }
                    catch (OverflowException)
                    {
                        error = "Saved pet copy count exceeds the supported range.";
                        return false;
                    }
                }
                else
                {
                    var canonical = new PlayerSnapshot.InventoryItemData
                    {
                        itemId = pet.Id,
                        upgradeLevel = 0,
                        owned = true,
                        count = count
                    };
                    petsById.Add(pet.Id, canonical);
                    canonicalInventory.Add(canonical);
                }
                owned[pet.Id] = petsById[pet.Id].count;
            }
            return true;
        }

        private static bool TryMapInventory(
            JsonValue gameData,
            out PlayerSnapshot.InventoryItemData[] inventory,
            out string error)
        {
            inventory = Array.Empty<PlayerSnapshot.InventoryItemData>();
            error = string.Empty;
            if (!gameData.TryGet("inventory", out JsonValue inventoryValue) ||
                !FirestoreJsonNavigator.TryGetArrayValues(
                    inventoryValue,
                    out IReadOnlyList<JsonValue> values))
            {
                error = "Inventory data is missing or invalid.";
                return false;
            }

            var result = new List<PlayerSnapshot.InventoryItemData>(values.Count);
            foreach (JsonValue value in values)
            {
                if (!FirestoreJsonNavigator.TryGetMapFields(value, out JsonValue fields) ||
                    !TryReadString(fields, "itemId", out string itemId) ||
                    string.IsNullOrWhiteSpace(itemId) ||
                    !TryReadBoolean(fields, "owned", out bool owned) ||
                    !TryReadInteger(fields, "upgradeLevel", out long level) ||
                    level < 0 || level > int.MaxValue)
                {
                    error = "Inventory contains an invalid item record.";
                    return false;
                }
                int count = 1;
                if (fields.TryGet("count", out JsonValue countLeaf))
                {
                    if (!FirestoreJsonNavigator.TryReadInteger(countLeaf, out long c) ||
                        c > int.MaxValue)
                    {
                        error = "Inventory contains an invalid copy count.";
                        return false;
                    }
                    count = c <= 0 ? 1 : (int)c;
                }
                result.Add(new PlayerSnapshot.InventoryItemData
                {
                    itemId = itemId,
                    owned = owned,
                    upgradeLevel = (int)level,
                    count = count
                });
            }
            inventory = result.ToArray();
            return true;
        }

        private void Apply(AuthoritativeState state, PetGachaReceipt receipt)
        {
            ApplyRefreshedState(state);
            _player.economy = _player.economy ?? new PlayerSnapshot.EconomyData();
            _player.economy.lastPetGachaTransactionId = receipt.TransactionId;
            _player.economy.lastPetGachaCatalogVersion = receipt.CatalogVersion;
            _player.economy.lastPetGachaPetId = receipt.PetId;
            _player.economy.lastPetGachaWasNew = receipt.WasNew;
            _player.economy.lastPetGachaCost = receipt.Cost;
            _player.economy.lastPetGachaResultingPowerCoins = receipt.ResultingPowerCoins;
            _player.economy.petGachaPullsSinceSsr = receipt.ResultingPityCount;
            _player.economy.lastPetGachaPreviousPityCount = receipt.PreviousPityCount;
            _player.economy.lastPetGachaResultingPityCount = receipt.ResultingPityCount;
            _player.economy.lastPetGachaResults = ToResultData(receipt.Results);
            _player.economy.firstGachaPullCompleted = true;
        }

        private void ApplyRefreshedState(AuthoritativeState state)
        {
            _player.revision = state.Revision;
            _player.wallet = _player.wallet ?? new PlayerSnapshot.WalletData();
            _player.wallet.powerCoins = state.PowerCoins;
            _player.inventory = state.Inventory.Select(Clone).ToArray();
            _player.loadout = _player.loadout ?? new PlayerSnapshot.LoadoutData();
            _player.loadout.petId = state.EquippedPetId;
            _player.economy = _player.economy ?? new PlayerSnapshot.EconomyData();
            _player.economy.petGachaPullsSinceSsr = state.PullsSinceSsr;
        }

        private static PlayerSnapshot.InventoryItemData Clone(PlayerSnapshot.InventoryItemData item) =>
            new PlayerSnapshot.InventoryItemData
            {
                itemId = item.itemId,
                upgradeLevel = item.upgradeLevel,
                owned = item.owned,
                count = Math.Max(1, item.count)
            };

        private static bool TryGetMap(JsonValue fields, string name, out JsonValue map)
        {
            map = null;
            return fields != null && fields.TryGet(name, out JsonValue value) &&
                FirestoreJsonNavigator.TryGetMapFields(value, out map);
        }

        private static bool TryReadString(JsonValue fields, string name, out string value)
        {
            value = string.Empty;
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadString(leaf, out value);
        }

        private static string ReadString(JsonValue fields, string name) =>
            TryReadString(fields, name, out string value) ? value : string.Empty;

        private static bool TryReadInteger(JsonValue fields, string name, out long value)
        {
            value = 0;
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadInteger(leaf, out value);
        }

        private static long ReadInteger(JsonValue fields, string name) =>
            TryReadInteger(fields, name, out long value) ? value : 0;

        private static bool TryReadBoolean(JsonValue fields, string name, out bool value)
        {
            value = false;
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadBoolean(leaf, out value);
        }

        private static bool ReadBoolean(JsonValue fields, string name) =>
            TryReadBoolean(fields, name, out bool value) && value;

        private static PlayerSnapshot.PetGachaResultData[] ToResultData(
            IReadOnlyList<PetGachaResult> results)
        {
            var mapped = new PlayerSnapshot.PetGachaResultData[results?.Count ?? 0];
            for (int index = 0; index < mapped.Length; index++)
            {
                PetGachaResult result = results[index];
                mapped[index] = new PlayerSnapshot.PetGachaResultData
                {
                    petId = result.PetId,
                    rarityId = result.RarityId,
                    wasNew = result.WasNew,
                    previousCount = result.PreviousCount,
                    resultingCount = result.ResultingCount
                };
            }
            return mapped;
        }

        private static bool TryMapGachaResults(
            JsonValue economy,
            out PetGachaResult[] results)
        {
            results = Array.Empty<PetGachaResult>();
            if (economy == null || !economy.TryGet("lastPetGachaResults", out JsonValue value) ||
                !FirestoreJsonNavigator.TryGetArrayValues(value, out IReadOnlyList<JsonValue> rows))
                return false;
            var mapped = new List<PetGachaResult>(rows.Count);
            foreach (JsonValue row in rows)
            {
                if (!FirestoreJsonNavigator.TryGetMapFields(row, out JsonValue fields) ||
                    !TryReadString(fields, "petId", out string resultPetId) ||
                    !TryReadString(fields, "rarityId", out string rarityId) ||
                    !TryReadBoolean(fields, "wasNew", out bool resultWasNew) ||
                    !TryReadInteger(fields, "previousCount", out long previous) ||
                    !TryReadInteger(fields, "resultingCount", out long current) ||
                    previous < 0 || previous > int.MaxValue ||
                    current <= 0 || current > int.MaxValue)
                    return false;
                mapped.Add(new PetGachaResult(
                    resultPetId, rarityId, resultWasNew, (int)previous, (int)current));
            }
            results = mapped.ToArray();
            return true;
        }

        private static string[] Join(IReadOnlyList<string> left, params string[] right)
        {
            var result = new string[left.Count + right.Length];
            for (int index = 0; index < left.Count; index++) result[index] = left[index];
            for (int index = 0; index < right.Length; index++) result[left.Count + index] = right[index];
            return result;
        }

        private sealed class AuthoritativeState
        {
            public long Revision;
            public long PowerCoins;
            public PlayerSnapshot.InventoryItemData[] Inventory;
            public string EquippedPetId;
            public int PullsSinceSsr;
            public string CommittedAttemptId;
            public string RunPhase;
            public PetGachaReceipt LastReceipt;
            public bool FirstGachaPullCompleted;
        }
    }
}
