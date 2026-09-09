using System;
using System.Collections;
using System.Text;
using PowerMath.Gameplay.Combat;
using PowerMath.Session;
using PowerMath.Gameplay.Progression;
using UnityEngine.Networking;

namespace PowerMath.PlayerData
{
    public sealed class FirestoreLeaderboardProjectionPublisher
    {
        private readonly GameApiSettings _settings;

        public FirestoreLeaderboardProjectionPublisher(GameApiSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public IEnumerator Publish(PlayerSnapshot player, Action completed, Action<string> failed)
        {
            if (!TryResolve(player, out string levelId, out string publicId) ||
                !_settings.TryGetLeaderboardDocument(levelId, out string url))
            {
                failed?.Invoke("Leaderboard projection is not configured for this player.");
                yield break;
            }

            PlayerSnapshot.ProfileData profile = player.profile ?? new PlayerSnapshot.ProfileData();
            PlayerSnapshot.ProgressionData progression = player.progression ?? new PlayerSnapshot.ProgressionData();
            PlayerSnapshot.WalletData wallet = player.wallet ?? new PlayerSnapshot.WalletData();
            PlayerSnapshot.LoadoutData loadout = player.loadout ?? new PlayerSnapshot.LoadoutData();
            int weaponLevel = 0;
            foreach (PlayerSnapshot.InventoryItemData item in player.inventory ?? Array.Empty<PlayerSnapshot.InventoryItemData>())
                if (item != null && item.itemId == WeaponAscensionPolicy.CanonicalItemId)
                    weaponLevel = Math.Max(weaponLevel, item.upgradeLevel);
            int currentStage = Math.Max(1, Math.Min(StageId.Final, progression.currentStage));
            int highestStage = Math.Max(currentStage, Math.Min(StageId.Final, progression.highestStage));
            long weighted;
            try { checked { weighted = wallet.silver * 5L + wallet.gold * 7L + wallet.diamond * 10L; } }
            catch (OverflowException)
            {
                failed?.Invoke("Leaderboard currency values are too large.");
                yield break;
            }

            var builder = new FirestorePatchDocumentBuilder();
            string[] root = { publicId };
            builder.AddInteger(Join(root, "entryRevision"), player.revision);
            builder.AddString(Join(root, "displayName"), profile.displayName ?? string.Empty);
            builder.AddString(Join(root, "characterId"), profile.characterId ?? string.Empty);
            builder.AddString(Join(root, "iconId"), profile.iconId ?? string.Empty);
            builder.AddString(Join(root, "avatarId"), loadout.avatarId ?? profile.iconId ?? string.Empty);
            builder.AddString(Join(root, "petId"), loadout.petId ?? string.Empty);
            builder.AddString(Join(root, "weaponId"), loadout.weaponId ?? string.Empty);
            builder.AddInteger(Join(root, "weaponLevel"), weaponLevel);
            builder.AddInteger(Join(root, "currentStage"), currentStage);
            builder.AddInteger(Join(root, "highestStage"), highestStage);
            builder.AddInteger(Join(root, "silver"), Math.Max(0, wallet.silver));
            builder.AddInteger(Join(root, "gold"), Math.Max(0, wallet.gold));
            builder.AddInteger(Join(root, "diamond"), Math.Max(0, wallet.diamond));
            builder.AddInteger(Join(root, "weightedCurrencyScore"), weighted);
            builder.AddInteger(Join(root, "totalDamage"), Math.Max(0, progression.totalDamage));
            builder.AddInteger(Join(root, "prestige"), Math.Max(0, progression.prestige));
            builder.AddInteger(Join(root, "firstStage200ReachedAtUnixSeconds"),
                Math.Max(0, progression.firstStage200ReachedAtUnixSeconds));
            FirestorePatchPlan plan = builder.Build();

            var address = new StringBuilder(url);
            string separator = url.IndexOf('?') >= 0 ? "&" : "?";
            foreach (string path in plan.FieldPaths)
            {
                address.Append(separator).Append("updateMask.fieldPaths=")
                    .Append(Uri.EscapeDataString(path));
                separator = "&";
            }
            using (var request = new UnityWebRequest(address.ToString(), "PATCH"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(plan.ToJson()));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = _settings.RequestTimeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    failed?.Invoke("Your private progress was saved, but leaderboard sync is pending.");
                    yield break;
                }
            }
            completed?.Invoke();
        }

        private static bool TryResolve(PlayerSnapshot player, out string levelId, out string publicId)
        {
            levelId = string.Empty;
            publicId = player?.profile?.publicPlayerId?.Trim() ?? string.Empty;
            string playerId = player?.playerId ?? string.Empty;
            int separator = playerId.IndexOf(':');
            if (separator > 0) levelId = playerId.Substring(0, separator);
            if (levelId.Length == 0 || publicId.Length != 32) return false;
            foreach (char character in publicId)
                if (!Uri.IsHexDigit(character)) return false;
            return true;
        }

        private static string[] Join(string[] left, string right)
        {
            var result = new string[left.Length + 1];
            Array.Copy(left, result, left.Length);
            result[left.Length] = right;
            return result;
        }
    }
}
