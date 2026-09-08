using System;
using System.Collections;
using System.Globalization;
using System.Text;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu.SocialProfile
{
    internal static class DisplayNamePolicy
    {
        public const long CooldownSeconds = 7L * 24L * 60L * 60L;

        public static bool TryNormalize(string input, out string value, out string error)
        {
            value = (input ?? string.Empty).Trim().Normalize(NormalizationForm.FormKC);
            error = string.Empty;
            int length = new StringInfo(value).LengthInTextElements;
            if (length < 3 || length > 20)
                error = "Display name must be 3–20 characters.";
            else
            {
                foreach (char character in value)
                {
                    if (char.IsControl(character) || character == '<' || character == '>' ||
                        character == '{' || character == '}')
                    {
                        error = "Display name contains an unsupported character.";
                        break;
                    }
                }
            }
            return error.Length == 0;
        }
    }

    internal sealed class FirestoreDisplayNameStore
    {
        private readonly MonoBehaviour _host;
        private readonly GameApiSettings _settings;
        private readonly PlayerSnapshot _player;
        private readonly FirestoreLeaderboardProjectionPublisher _publisher;
        private Coroutine _operation;

        public FirestoreDisplayNameStore(MonoBehaviour host, GameApiSettings settings, PlayerSnapshot player)
        {
            _host = host;
            _settings = settings;
            _player = player;
            _publisher = settings == null ? null : new FirestoreLeaderboardProjectionPublisher(settings);
        }

        public void Save(string displayName, Action<string> completed, Action<string> failed)
        {
            if (_operation != null) return;
            _operation = _host.StartCoroutine(SaveRoutine(displayName, completed, failed));
        }

        public void Cancel()
        {
            if (_operation != null) _host.StopCoroutine(_operation);
            _operation = null;
        }

        private IEnumerator SaveRoutine(string displayName, Action<string> completed, Action<string> failed)
        {
            if (!TryAddress(out string url, out string username))
            {
                FinishFailure(failed, "Profile editing is not configured.");
                yield break;
            }

            string updateTime = string.Empty;
            long serverSeconds = 0;
            using (UnityWebRequest get = UnityWebRequest.Get(url))
            {
                get.timeout = _settings.RequestTimeoutSeconds;
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success ||
                    !FirestoreJsonNavigator.TryParse(get.downloadHandler.text, out JsonValue root, out _) ||
                    !root.TryGet("updateTime", out JsonValue update) ||
                    update.Kind != JsonValueKind.String ||
                    !TryReadServerTime(get, out serverSeconds))
                {
                    FinishFailure(failed, "Could not verify the rename cooldown. Try again.");
                    yield break;
                }
                updateTime = update.Text;
            }

            long changedAt = _player.profile?.displayNameChangedAtUnixSeconds ?? 0;
            if (changedAt > 0 && serverSeconds < changedAt + DisplayNamePolicy.CooldownSeconds)
            {
                TimeSpan remaining = TimeSpan.FromSeconds(changedAt + DisplayNamePolicy.CooldownSeconds - serverSeconds);
                FinishFailure(failed, "You can rename again in " + Math.Ceiling(remaining.TotalDays) + " day(s).");
                yield break;
            }

            var builder = new FirestorePatchDocumentBuilder();
            string[] data = { username, "gamedata" };
            builder.AddInteger(Join(data, "revision"), _player.revision + 1);
            builder.AddString(Join(data, "profile", "displayName"), displayName);
            builder.AddInteger(Join(data, "profile", "displayNameChangedAtUnixSeconds"), serverSeconds);
            FirestorePatchPlan plan = builder.Build();
            var address = new StringBuilder(url);
            string separator = url.IndexOf('?') >= 0 ? "&" : "?";
            foreach (string path in plan.FieldPaths)
            {
                address.Append(separator).Append("updateMask.fieldPaths=")
                    .Append(Uri.EscapeDataString(path));
                separator = "&";
            }
            address.Append(separator).Append("currentDocument.updateTime=")
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
                    FinishFailure(failed, patch.responseCode == 409 || patch.responseCode == 412
                        ? "Profile changed on another device. Reopen this panel."
                        : "Display name could not be saved.");
                    yield break;
                }
            }

            _player.revision++;
            _player.profile.displayName = displayName;
            _player.profile.displayNameChangedAtUnixSeconds = serverSeconds;
            PlayerSessionStore.Instance?.NotifyAuthoritativeUpdate();
            string projectionFailure = string.Empty;
            yield return _publisher.Publish(_player, () => { }, message => projectionFailure = message);
            _operation = null;
            completed?.Invoke(projectionFailure.Length == 0
                ? "Display name updated. Next change is available in 7 days."
                : "Display name updated. Leaderboard sync is pending.");
        }

        private void FinishFailure(Action<string> failed, string message)
        {
            _operation = null;
            failed?.Invoke(message);
        }

        private bool TryAddress(out string url, out string username)
        {
            url = string.Empty;
            username = string.Empty;
            string playerId = _player?.playerId ?? string.Empty;
            int separator = playerId.IndexOf(':');
            if (_settings == null || separator <= 0 || separator >= playerId.Length - 1) return false;
            username = playerId.Substring(separator + 1);
            return _settings.TryGetLevelDocumentById(playerId.Substring(0, separator), out url);
        }

        private static bool TryReadServerTime(UnityWebRequest request, out long seconds)
        {
            seconds = 0;
            string date = request.GetResponseHeader("Date");
            if (!DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTimeOffset value))
                return false;
            seconds = value.ToUnixTimeSeconds();
            return true;
        }

        private static string[] Join(string[] root, params string[] values)
        {
            var result = new string[root.Length + values.Length];
            Array.Copy(root, result, root.Length);
            Array.Copy(values, 0, result, root.Length, values.Length);
            return result;
        }
    }

    internal sealed class ProfileAnalyticsPanelController
    {
        private readonly VisualElement _open;
        private readonly VisualElement _modal;
        private readonly Button _close;
        private readonly Label _balance;
        private readonly TextField _name;
        private readonly Button _save;
        private readonly Label _status;
        private readonly Label _summary;
        private readonly Label _adventure;
        private readonly Label _economy;
        private readonly Label _learning;
        private readonly Label _ranks;
        private readonly VisualElement _attemptPanel;
        private readonly PlayerSnapshot _player;
        private readonly FirestoreDisplayNameStore _store;
        private readonly IMainMenuPanelHost _panelHost;
        private bool _bound;
        private bool _busy;

        public ProfileAnalyticsPanelController(VisualElement root, MonoBehaviour host,
            GameApiSettings settings, PlayerSnapshot player,
            IMainMenuPanelHost panelHost)
        {
            _open = root.Q<VisualElement>("profile-panel");
            _modal = root.Q<VisualElement>("profile-analytics-modal");
            _close = root.Q<Button>("profile-analytics-close");
            _balance = root.Q<Label>("profile-balance");
            _name = root.Q<TextField>("profile-display-name-input");
            _save = root.Q<Button>("profile-display-name-save");
            _status = root.Q<Label>("profile-display-name-status");
            _summary = root.Q<Label>("profile-summary");
            _adventure = root.Q<Label>("profile-adventure");
            _economy = root.Q<Label>("profile-economy");
            _learning = root.Q<Label>("profile-learning");
            _ranks = root.Q<Label>("profile-ranks");
            _attemptPanel = root.Q<VisualElement>("combat-attempt-panel");
            _player = player;
            _store = new FirestoreDisplayNameStore(host, settings, player);
            _panelHost = panelHost ?? throw new ArgumentNullException(nameof(panelHost));
        }

        public bool IsValid => _open != null && _modal != null && _close != null &&
            _name != null && _save != null && _status != null && _summary != null;

        public void Bind()
        {
            if (_bound || !IsValid) return;
            _open.RegisterCallback<ClickEvent>(Open);
            _close.clicked += Close;
            _save.clicked += Save;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed += OnPlayerChanged;
            _modal.style.display = DisplayStyle.None;
            _bound = true;
        }

        public void Dispose()
        {
            if (!_bound) return;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed -= OnPlayerChanged;
            _open.UnregisterCallback<ClickEvent>(Open);
            _close.clicked -= Close;
            _save.clicked -= Save;
            _store.Cancel();
            if (_panelHost.OpenPanel == MainMenuPanelId.ProfileAnalytics)
                _panelHost.TryClose(MainMenuPanelId.ProfileAnalytics, _open);
            _bound = false;
        }

        private void Open(ClickEvent _)
        {
            if (_attemptPanel != null && _attemptPanel.resolvedStyle.display != DisplayStyle.None)
                return;
            if (!_panelHost.TryOpen(
                    MainMenuPanelId.ProfileAnalytics,
                    _modal,
                    _open)) return;
            Render();
            SetSemanticState();
        }
        private void Close()
        {
            if (_busy) return;
            _panelHost.TryClose(MainMenuPanelId.ProfileAnalytics, _open);
            _save.SetEnabled(true);
            SetSemanticState();
        }

        private void Save()
        {
            if (!DisplayNamePolicy.TryNormalize(_name.value, out string value, out string error))
            { _status.text = error; SetSemanticState("is-error"); return; }
            if (string.Equals(value, _player.profile.displayName, StringComparison.Ordinal))
            { _status.text = "That is already your display name."; SetSemanticState("is-error"); return; }
            _busy = true;
            _save.SetEnabled(false);
            _close.SetEnabled(false);
            _status.text = "Saving…";
            SetSemanticState("is-busy");
            _store.Save(value,
                message =>
                {
                    _busy = false;
                    _save.SetEnabled(true);
                    _close.SetEnabled(true);
                    Render();
                    _status.text = message;
                    SetSemanticState("is-success");
                },
                message =>
                {
                    _busy = false;
                    _save.SetEnabled(true);
                    _close.SetEnabled(true);
                    _status.text = message;
                    SetSemanticState("is-error");
                });
        }

        private void OnPlayerChanged(PlayerSnapshot player)
        {
            if (_balance != null && player?.wallet != null)
                _balance.text = player.wallet.powerCoins.ToString("N0");
        }

        private void Render()
        {
            PlayerSnapshot.AnalyticsData analytics = _player.analytics ?? new PlayerSnapshot.AnalyticsData();
            PlayerSnapshot.ProgressionData progress = _player.progression ?? new PlayerSnapshot.ProgressionData();
            PlayerSnapshot.WalletData wallet = _player.wallet ?? new PlayerSnapshot.WalletData();
            PlayerSnapshot.LoadoutData loadout = _player.loadout ?? new PlayerSnapshot.LoadoutData();
            if (_balance != null)
                _balance.text = wallet.powerCoins.ToString("N0");
            _name.value = _player.profile?.displayName ?? string.Empty;
            long changedAt = _player.profile?.displayNameChangedAtUnixSeconds ?? 0;
            if (changedAt <= 0)
                _status.text = "First change is available now.";
            else
            {
                DateTimeOffset next = DateTimeOffset.FromUnixTimeSeconds(
                    changedAt + DisplayNamePolicy.CooldownSeconds).ToLocalTime();
                _status.text = DateTimeOffset.Now >= next
                    ? "Display name change is available."
                    : "Next change: " + next.ToString("g", CultureInfo.CurrentCulture);
            }
            _summary.text = (_player.profile?.displayName ?? "Student") + "\n" +
                (_player.profile?.gradeBand ?? "Grade") + " • " + (progress.activeRank ?? "Silver") + " Rank";
            _adventure.text = "ADVENTURE\nCurrent Stage  " + progress.currentStage +
                "\nBest Stage  " + progress.highestStage + "\nPrestige / Honor  " + progress.prestige +
                "\nFirst Stage 200  " + FormatTimestamp(progress.firstStage200ReachedAtUnixSeconds) +
                "\nTotal Damage  " + progress.totalDamage.ToString("N0");
            _economy.text = "RANK CURRENCY\nSilver  " + wallet.silver.ToString("N0") +
                "\nGold  " + wallet.gold.ToString("N0") + "\nDiamond  " + wallet.diamond.ToString("N0") +
                "\nPower Coins  " + wallet.powerCoins.ToString("N0") + "\nPet  " + Item(loadout.petId) +
                "\nWeapon  " + Item(loadout.weaponId) + "\nAvatar  " + Item(loadout.avatarId);
            _learning.text = "LEARNING\nQuestions  " + analytics.totalQuestionsResolved.ToString("N0") +
                "\nAccuracy  " + Percent(analytics.totalCorrect, analytics.totalQuestionsResolved) +
                "\nMean Response Score  " + Average(analytics.responseScoreSum, analytics.totalQuestionsResolved) + " / 10" +
                "\nMedian Response Score  " + Median(analytics.responseScoreHistogram, 1d, " / 10") +
                "\nMean Response Time  " + AverageSeconds(analytics.responseDurationMillisecondsSum, analytics.totalQuestionsResolved) +
                "\nMedian Response Time  " + Median(analytics.responseDuration100msHistogram, 0.1d, "s") +
                "\nMean Efficiency  " + Average(analytics.responseEfficiencySum, analytics.totalQuestionsResolved) + "%" +
                "\nMedian Efficiency  " + Median(analytics.responseEfficiencyHistogram, 10d, "%") +
                "\nIncorrect / Timeout / Abandoned  " + analytics.totalIncorrect + " / " +
                analytics.totalTimeout + " / " + analytics.totalAbandoned +
                "\nRecorded Play Time  " + FormatDuration(analytics.totalPlaySeconds) +
                "\nRegistration  unavailable";
            _ranks.text = "BY RANK\n" + RankLine("Silver", analytics.silver) + "\n" +
                RankLine("Gold", analytics.gold) + "\n" + RankLine("Diamond", analytics.diamond) +
                QuestionLines(analytics.byQuestion);
        }

        private static string Percent(long value, long total) => total <= 0 ? "—" :
            (100d * value / total).ToString("0.#", CultureInfo.CurrentCulture) + "%";
        private static string Average(long sum, long total) => total <= 0 ? "—" :
            ((double)sum / total).ToString("0.#", CultureInfo.CurrentCulture);
        private static string AverageSeconds(long milliseconds, long total) => total <= 0 ? "—" :
            ((double)milliseconds / total / 1000d).ToString("0.0", CultureInfo.CurrentCulture) + "s";
        private static string Median(long[] histogram, double scale, string suffix)
        {
            if (histogram == null || histogram.Length == 0) return "—";
            long total = 0;
            foreach (long count in histogram) total += Math.Max(0, count);
            if (total <= 0) return "—";
            long target = (total + 1) / 2;
            long seen = 0;
            for (int index = 0; index < histogram.Length; index++)
            {
                seen += Math.Max(0, histogram[index]);
                if (seen >= target)
                    return (index * scale).ToString("0.#", CultureInfo.CurrentCulture) + suffix;
            }
            return "—";
        }
        private static string RankLine(string name, PlayerSnapshot.RankAnalyticsData data)
        {
            data = data ?? new PlayerSnapshot.RankAnalyticsData();
            return name + "  " + data.resolved + " solved • " + Percent(data.correct, data.resolved) + " accuracy";
        }
        private static string QuestionLines(PlayerSnapshot.QuestionAnalyticsData[] values)
        {
            if (values == null || values.Length == 0) return "\n\nBY QUESTION\nComplete questions to see this insight.";
            var builder = new StringBuilder("\n\nBY QUESTION");
            int count = Math.Min(6, values.Length);
            for (int index = 0; index < count; index++)
            {
                PlayerSnapshot.QuestionAnalyticsData value = values[index];
                builder.Append("\nQ").Append(value.questionId).Append("  ")
                    .Append(value.resolved).Append(" solved • ")
                    .Append(Percent(value.correct, value.resolved)).Append(" accuracy");
            }
            if (values.Length > count) builder.Append("\n+").Append(values.Length - count).Append(" more questions");
            return builder.ToString();
        }
        private static string Item(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "None";
            string[] words = value.Replace('_', '-').Split('-');
            for (int index = 0; index < words.Length; index++)
                if (words[index].Length > 0)
                    words[index] = char.ToUpperInvariant(words[index][0]) + words[index].Substring(1);
            return string.Join(" ", words);
        }
        private static string FormatTimestamp(long seconds) => seconds <= 0 ? "Not reached" :
            DateTimeOffset.FromUnixTimeSeconds(seconds).ToLocalTime().ToString("d", CultureInfo.CurrentCulture);
        private static string FormatDuration(long seconds)
        {
            if (seconds <= 0) return "No recorded time yet";
            TimeSpan value = TimeSpan.FromSeconds(seconds);
            return ((int)value.TotalHours).ToString(CultureInfo.CurrentCulture) + "h " + value.Minutes + "m";
        }

        private void SetSemanticState(string state = null)
        {
            _modal.EnableInClassList("is-busy", state == "is-busy");
            _modal.EnableInClassList("is-success", state == "is-success");
            _modal.EnableInClassList("is-error", state == "is-error");
        }
    }
}
