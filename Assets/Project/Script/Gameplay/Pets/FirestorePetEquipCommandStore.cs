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
    public sealed class FirestorePetEquipCommandStore : IPetEquipCommandStore
    {
        private readonly GameApiSettings _settings;
        private readonly PlayerSnapshot _player;
        private readonly PetGachaCatalogDefinition _catalog;
        private readonly string _username;
        private readonly string _url;

        public FirestorePetEquipCommandStore(
            GameApiSettings settings,
            PlayerSnapshot player,
            PetGachaCatalogDefinition catalog)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (!_catalog.TryBuildCatalog(out _, out string error))
                throw new ArgumentException("Pet catalog is invalid: " + error, nameof(catalog));

            int separator = (player.playerId ?? string.Empty).IndexOf(':');
            if (separator <= 0 || separator >= player.playerId.Length - 1 ||
                !_settings.TryGetLevelDocumentById(
                    player.playerId.Substring(0, separator), out _url))
            {
                throw new ArgumentException(
                    "Player ID cannot resolve its Firestore document.", nameof(player));
            }
            _username = player.playerId.Substring(separator + 1);
        }

        public IEnumerator Equip(
            PetEquipCommand command,
            Action<PetEquipReceipt> completed,
            Action<PetEquipFailure> failed)
        {
            if (!_catalog.TryResolvePet(command.PetId, out _, out _))
            {
                failed?.Invoke(new PetEquipFailure(
                    PetEquipFailureCode.InvalidPet,
                    "This pet is no longer available in the current catalog."));
                yield break;
            }

            AuthoritativeState state = null;
            string updateTime = string.Empty;
            PetEquipFailure refreshFailure = default;
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

            if (string.Equals(
                    state.LastTransactionId,
                    command.TransactionId,
                    StringComparison.Ordinal))
            {
                if (!string.Equals(state.LastPetId, command.PetId, StringComparison.Ordinal) ||
                    !string.Equals(state.EquippedPetId, command.PetId, StringComparison.Ordinal))
                {
                    failed?.Invoke(new PetEquipFailure(
                        PetEquipFailureCode.InvalidSavedState,
                        "The saved pet equip receipt is inconsistent and requires data repair."));
                    yield break;
                }
                Apply(state);
                completed?.Invoke(new PetEquipReceipt(
                    command.TransactionId,
                    command.PetId,
                    state.Revision));
                yield break;
            }

            if (state.Revision != command.PreviewRevision)
            {
                Apply(state);
                failed?.Invoke(new PetEquipFailure(
                    PetEquipFailureCode.StaleState,
                    "Your collection changed. Player Hub has refreshed; tap the pet again."));
                yield break;
            }
            if (!string.IsNullOrEmpty(state.CommittedAttemptId) ||
                string.Equals(state.RunPhase, "RunDefeat", StringComparison.Ordinal))
            {
                failed?.Invoke(new PetEquipFailure(
                    PetEquipFailureCode.Unavailable,
                    "Finish the current question or run settlement before changing pets."));
                yield break;
            }
            if (!TryGetOwnedPets(state.Inventory, out HashSet<string> owned, out string error))
            {
                failed?.Invoke(new PetEquipFailure(
                    PetEquipFailureCode.InvalidSavedState,
                    error));
                yield break;
            }
            if (!owned.Contains(command.PetId))
            {
                failed?.Invoke(new PetEquipFailure(
                    PetEquipFailureCode.NotOwned,
                    "Only pets in your collection can be equipped."));
                yield break;
            }

            if (string.Equals(state.EquippedPetId, command.PetId, StringComparison.Ordinal))
            {
                Apply(state);
                completed?.Invoke(new PetEquipReceipt(
                    command.TransactionId,
                    command.PetId,
                    state.Revision));
                yield break;
            }

            long nextRevision;
            try
            {
                nextRevision = checked(state.Revision + 1);
            }
            catch (OverflowException)
            {
                failed?.Invoke(new PetEquipFailure(
                    PetEquipFailureCode.InvalidSavedState,
                    "Player revision is outside the supported range."));
                yield break;
            }

            var builder = new FirestorePatchDocumentBuilder();
            string[] root = { _username, "gamedata" };
            builder.AddInteger(Join(root, "revision"), nextRevision);
            builder.AddString(Join(root, "loadout", "petId"), command.PetId);
            builder.AddString(
                Join(root, "economy", "lastPetEquipTransactionId"),
                command.TransactionId);
            builder.AddString(
                Join(root, "economy", "lastPetEquipPetId"),
                command.PetId);

            bool saved = false;
            PetEquipFailure saveFailure = default;
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
            state.EquippedPetId = command.PetId;
            state.LastTransactionId = command.TransactionId;
            state.LastPetId = command.PetId;
            Apply(state);
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            completed?.Invoke(new PetEquipReceipt(
                command.TransactionId,
                command.PetId,
                nextRevision));
        }

        private IEnumerator Refresh(
            Action<AuthoritativeState, string> completed,
            Action<PetEquipFailure> failed)
        {
            using (UnityWebRequest get = UnityWebRequest.Get(_url))
            {
                get.timeout = _settings.RequestTimeoutSeconds;
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success)
                {
                    failed?.Invoke(new PetEquipFailure(
                        PetEquipFailureCode.RecoverableTransport,
                        "Could not check whether the pet was equipped. Tap again to recover."));
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
                    failed?.Invoke(new PetEquipFailure(
                        PetEquipFailureCode.InvalidSavedState,
                        "Player data could not be read safely."));
                    yield break;
                }
                if (!TryMapState(document, out AuthoritativeState state, out string error))
                {
                    failed?.Invoke(new PetEquipFailure(
                        PetEquipFailureCode.InvalidSavedState,
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
            Action<PetEquipFailure> failed)
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
                    failed?.Invoke(new PetEquipFailure(
                        conflict
                            ? PetEquipFailureCode.StaleState
                            : PetEquipFailureCode.RecoverableTransport,
                        conflict
                            ? "Player data changed before the pet was equipped. Tap again after refresh."
                            : "The equip result is not confirmed yet. Tap the same pet to recover."));
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
                !TryMapInventory(gameData, out PlayerSnapshot.InventoryItemData[] inventory, out error))
            {
                if (string.IsNullOrEmpty(error))
                    error = "Required inventory or revision data is invalid.";
                return false;
            }

            TryGetMap(gameData, "activeRun", out JsonValue activeRun);
            TryGetMap(gameData, "loadout", out JsonValue loadout);
            TryGetMap(gameData, "economy", out JsonValue economy);
            string equippedPetId = ReadString(loadout, "petId");
            string lastTransactionId = ReadString(economy, "lastPetEquipTransactionId");
            string lastPetId = ReadString(economy, "lastPetEquipPetId");
            if (!string.IsNullOrEmpty(lastTransactionId) &&
                (string.IsNullOrEmpty(lastPetId) ||
                 !_catalog.TryResolvePet(lastPetId, out _, out _)))
            {
                error = "The saved pet equip receipt is invalid and requires data repair.";
                return false;
            }

            state = new AuthoritativeState
            {
                Revision = revision,
                Inventory = inventory,
                EquippedPetId = equippedPetId,
                CommittedAttemptId = ReadString(activeRun, "committedAttemptId"),
                RunPhase = ReadString(activeRun, "phase"),
                LastTransactionId = lastTransactionId,
                LastPetId = lastPetId
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
                if (item == null || !_catalog.TryResolvePet(item.itemId, out _, out _))
                    continue;
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
                if (!FirestoreJsonNavigator.TryGetMapFields(value, out JsonValue item) ||
                    !TryReadString(item, "itemId", out string itemId) ||
                    string.IsNullOrWhiteSpace(itemId) ||
                    !TryReadBoolean(item, "owned", out bool owned) ||
                    !TryReadInteger(item, "upgradeLevel", out long level) ||
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

        private void Apply(AuthoritativeState state)
        {
            _player.revision = state.Revision;
            _player.inventory = state.Inventory.Select(Clone).ToArray();
            _player.loadout = _player.loadout ?? new PlayerSnapshot.LoadoutData();
            _player.loadout.petId = state.EquippedPetId;
            _player.economy = _player.economy ?? new PlayerSnapshot.EconomyData();
            _player.economy.lastPetEquipTransactionId = state.LastTransactionId;
            _player.economy.lastPetEquipPetId = state.LastPetId;
        }

        private static PlayerSnapshot.InventoryItemData Clone(
            PlayerSnapshot.InventoryItemData item) =>
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

        private static bool TryReadString(
            JsonValue fields,
            string name,
            out string value)
        {
            value = string.Empty;
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadString(leaf, out value);
        }

        private static string ReadString(JsonValue fields, string name) =>
            TryReadString(fields, name, out string value) ? value : string.Empty;

        private static bool TryReadInteger(
            JsonValue fields,
            string name,
            out long value)
        {
            value = 0;
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadInteger(leaf, out value);
        }

        private static bool TryReadBoolean(
            JsonValue fields,
            string name,
            out bool value)
        {
            value = false;
            return fields != null && fields.TryGet(name, out JsonValue leaf) &&
                FirestoreJsonNavigator.TryReadBoolean(leaf, out value);
        }

        private static string[] Join(
            IReadOnlyList<string> left,
            params string[] right)
        {
            var result = new string[left.Count + right.Length];
            for (int index = 0; index < left.Count; index++) result[index] = left[index];
            for (int index = 0; index < right.Length; index++)
                result[left.Count + index] = right[index];
            return result;
        }

        private sealed class AuthoritativeState
        {
            public long Revision;
            public PlayerSnapshot.InventoryItemData[] Inventory;
            public string EquippedPetId;
            public string CommittedAttemptId;
            public string RunPhase;
            public string LastTransactionId;
            public string LastPetId;
        }
    }
}
