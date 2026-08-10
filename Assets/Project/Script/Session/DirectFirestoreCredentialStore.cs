using System;
using UnityEngine;

namespace PowerMath.Session
{
    public static class DirectFirestoreCredentialStore
    {
        public readonly struct Credentials
        {
            public Credentials(string username, string password, bool remembered)
            {
                Username = username;
                Password = password;
                Remembered = remembered;
            }

            public string Username { get; }
            public string Password { get; }
            public bool Remembered { get; }
        }

        private const string UsernameKey = "powermath.direct.username";
        private const string PasswordKey = "powermath.direct.password";
        private const string RememberedKey = "powermath.direct.remembered";

        private static Credentials? _runtimeCredentials;

        public static void Set(string username, string password, bool rememberDevice)
        {
            Credentials credentials = new Credentials(
                NormalizeUsername(username),
                password,
                rememberDevice
            );
            _runtimeCredentials = credentials;

            if (rememberDevice)
            {
                PlayerPrefs.SetString(UsernameKey, credentials.Username);
                PlayerPrefs.SetString(PasswordKey, credentials.Password);
                PlayerPrefs.SetInt(RememberedKey, 1);
                PlayerPrefs.Save();
            }
            else
            {
                ClearRemembered();
            }
        }

        public static bool TryGet(out Credentials credentials)
        {
            if (_runtimeCredentials.HasValue)
            {
                credentials = _runtimeCredentials.Value;
                return true;
            }

            if (PlayerPrefs.GetInt(RememberedKey, 0) == 1)
            {
                string username = PlayerPrefs.GetString(UsernameKey, string.Empty);
                string password = PlayerPrefs.GetString(PasswordKey, string.Empty);
                if (IsValidUsername(username) && IsSixDigitPassword(password))
                {
                    credentials = new Credentials(username, password, true);
                    _runtimeCredentials = credentials;
                    return true;
                }
            }

            ClearRemembered();
            credentials = default;
            return false;
        }

        public static void Clear()
        {
            _runtimeCredentials = null;
            ClearRemembered();
        }

        public static string NormalizeUsername(string username)
        {
            return (username ?? string.Empty).Trim().ToLowerInvariant();
        }

        public static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length > 64)
            {
                return false;
            }

            foreach (char character in username)
            {
                bool allowed = character >= 'a' && character <= 'z' ||
                    character >= '0' && character <= '9' ||
                    character == '.' || character == '_' || character == '-';
                if (!allowed) return false;
            }
            return true;
        }

        public static bool IsSixDigitPassword(string password)
        {
            if (password == null || password.Length != 6) return false;
            foreach (char character in password)
            {
                if (character < '0' || character > '9') return false;
            }
            return true;
        }

        private static void ClearRemembered()
        {
            PlayerPrefs.DeleteKey(UsernameKey);
            PlayerPrefs.DeleteKey(PasswordKey);
            PlayerPrefs.DeleteKey(RememberedKey);
            PlayerPrefs.Save();
        }
    }
}
