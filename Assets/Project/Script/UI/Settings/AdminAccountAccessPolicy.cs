using System;
using PowerMath.PlayerData;

namespace PowerMath.UI.Settings
{
    public static class AdminAccountAccessPolicy
    {
        public static bool IsAuthorized(PlayerSnapshot player)
        {
            return player != null && player.isAdmin && TryGetUsername(player, out _);
        }

        public static bool TryGetUsername(
            PlayerSnapshot player,
            out string username)
        {
            username = string.Empty;
            string playerId = player?.playerId?.Trim();
            if (string.IsNullOrEmpty(playerId)) return false;
            int separator = playerId.IndexOf(':');
            if (separator <= 0 || separator != playerId.LastIndexOf(':') ||
                separator >= playerId.Length - 1)
            {
                return false;
            }

            username = playerId.Substring(separator + 1).Trim();
            return username.Length > 0;
        }
    }
}
