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
            if (separator <= 0 || separator >= player.playerId.Length - 1 ||
                !_settings.TryGetLevelDocumentById(player.playerId.Substring(0, separator), out _url))
            {
                throw new ArgumentException(
                    "Player ID cannot resolve its Firestore document.",
                    nameof(player));
            }
            _username = player.playerId.Substring(separator + 1);
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
            if (state.PowerCoins < PetGachaTransactionPolicy.PullCost)
            {
                ApplyRefreshedState(state);
                failed?.Invoke(new PetGachaFailure(
                    PetGachaFailureCode.InsufficientFunds,
                    $"You need {PetGachaTransactionPolicy.PullCost - state.PowerCoins:N0} more Power Coins."));
                yield break;
            }
            if (!TryGetOwnedPets(state.Inventory, out HashSet<string> ownedPetIds, out string inventoryError))
            {
                failed?.Invoke(new PetGachaFailure(
                    PetGachaFailureCode.InvalidSavedState,
                    inventoryError));
                yield break;
            }

            PetGachaReceipt receipt;
            try
            {
                receipt = PetGachaTransactionPolicy.CreateReceipt(
                    command,
                    state.PowerCoins,
                    _catalog,
                    ownedPetIds,
                    _random);
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

            List<PlayerSnapshot.InventoryItemData> nextInventory = state.Inventory
                .Select(Clone).ToList();
            if (receipt.WasNew)
            {
                nextInventory.Add(new PlayerSnapshot.InventoryItemData
                {
                    itemId = receipt.PetId,
                    upgradeLevel = 0,
                    owned = true
                });
            }

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
            string[] root = { _username, "gamedata" };
            builder.AddInteger(Join(root, "revision"), nextRevision);
            builder.AddInteger(Join(root, "wallet", "powerCoins"), receipt.ResultingPowerCoins);
            if (receipt.WasNew)
                builder.AddInventoryArray(Join(root, "inventory"), nextInventory);
            builder.AddString(Join(root, "economy", "lastPetGachaTransactionId"), receipt.TransactionId);
            builder.AddString(Join(root, "economy", "lastPetGachaCatalogVersion"), receipt.CatalogVersion);
            builder.AddString(Join(root, "economy", "lastPetGachaPetId"), receipt.PetId);
            builder.AddBoolean(Join(root, "economy", "lastPetGachaWasNew"), receipt.WasNew);
            builder.AddInteger(Join(root, "economy", "lastPetGachaCost"), receipt.Cost);
            builder.AddInteger(
                Join(root, "economy", "lastPetGachaResultingPowerCoins"),
                receipt.ResultingPowerCoins);

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
            state.LastReceipt = receipt;
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
            if (!FirestoreJsonNavigator.TryGetDocumentFields(document, out JsonValue fields) ||
                !fields.TryGet(_username, out JsonValue studentValue) ||
                !FirestoreJsonNavigator.TryGetMapFields(studentValue, out JsonValue student) ||
                !student.TryGet("gamedata", out JsonValue gameDataValue) ||
                !FirestoreJsonNavigator.TryGetMapFields(gameDataValue, out JsonValue gameData) ||
                !TryReadInteger(gameData, "revision", out long revision) || revision < 0 ||
                !TryGetMap(gameData, "wallet", out JsonValue wallet) ||
                !TryReadInteger(wallet, "powerCoins", out long powerCoins) || powerCoins < 0 ||
                !TryMapInventory(gameData, out PlayerSnapshot.InventoryItemData[] inventory, out error))
            {
                if (string.IsNullOrEmpty(error)) error = "Required wallet, inventory, or revision data is invalid.";
                return false;
            }

            TryGetMap(gameData, "activeRun", out JsonValue activeRun);
            TryGetMap(gameData, "economy", out JsonValue economy);
            string transactionId = ReadString(economy, "lastPetGachaTransactionId");
            string catalogVersion = ReadString(economy, "lastPetGachaCatalogVersion");
            string petId = ReadString(economy, "lastPetGachaPetId");
            bool wasNew = ReadBoolean(economy, "lastPetGachaWasNew");
            long cost = ReadInteger(economy, "lastPetGachaCost");
            long resulting = ReadInteger(economy, "lastPetGachaResultingPowerCoins");

            PetGachaReceipt receipt = default;
            if (!string.IsNullOrEmpty(transactionId))
            {
                if (string.IsNullOrEmpty(catalogVersion) || string.IsNullOrEmpty(petId) ||
                    cost != PetGachaTransactionPolicy.PullCost || resulting < 0 ||
                    !_catalog.ContainsPet(petId))
                {
                    error = "The saved Pet Gacha receipt is invalid and requires data repair.";
                    return false;
                }
                receipt = new PetGachaReceipt(
                    transactionId,
                    catalogVersion,
                    petId,
                    wasNew,
                    cost,
                    resulting);
            }

            state = new AuthoritativeState
            {
                Revision = revision,
                PowerCoins = powerCoins,
                Inventory = inventory,
                CommittedAttemptId = ReadString(activeRun, "committedAttemptId"),
                RunPhase = ReadString(activeRun, "phase"),
                LastReceipt = receipt
            };
            return true;
        }

        private bool TryGetOwnedPets(
            IReadOnlyList<PlayerSnapshot.InventoryItemData> inventory,
            out HashSet<string> owned,
            out string error)
        {
            owned = new HashSet<string>(StringComparer.Ordinal);
            error = string.Empty;
            foreach (PlayerSnapshot.InventoryItemData item in inventory)
            {
                if (item == null || !_catalog.ContainsPet(item.itemId)) continue;
                if (!item.owned || item.upgradeLevel != 0 || !owned.Add(item.itemId))
                {
                    error = "Saved pet ownership is inconsistent and requires data repair.";
                    return false;
                }
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
                result.Add(new PlayerSnapshot.InventoryItemData
                {
                    itemId = itemId,
                    owned = owned,
                    upgradeLevel = (int)level
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
        }

        private void ApplyRefreshedState(AuthoritativeState state)
        {
            _player.revision = state.Revision;
            _player.wallet = _player.wallet ?? new PlayerSnapshot.WalletData();
            _player.wallet.powerCoins = state.PowerCoins;
            _player.inventory = state.Inventory.Select(Clone).ToArray();
        }

        private static PlayerSnapshot.InventoryItemData Clone(PlayerSnapshot.InventoryItemData item) =>
            new PlayerSnapshot.InventoryItemData
            {
                itemId = item.itemId,
                upgradeLevel = item.upgradeLevel,
                owned = item.owned
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
            public string CommittedAttemptId;
            public string RunPhase;
            public PetGachaReceipt LastReceipt;
        }
    }
}
