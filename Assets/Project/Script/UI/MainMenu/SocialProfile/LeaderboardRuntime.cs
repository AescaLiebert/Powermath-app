using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PowerMath.PlayerData;
using PowerMath.Session;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu.SocialProfile
{
    internal sealed class LeaderboardEntry
    {
        public string PlayerId;
        public string DisplayName;
        public int CurrentStage;
        public int HighestStage;
        public long Silver;
        public long Gold;
        public long Diamond;
        public long WeightedScore;
        public long TotalDamage;
        public string AvatarId;
        public string IconId;
        public string PetId;
        public string WeaponId;
        public int WeaponLevel;
    }

    internal sealed class RankedLeaderboardEntry
    {
        public LeaderboardEntry Entry;
        public int Rank;
        public bool IsSelf;
        public bool IsTied;
    }

    internal static class LeaderboardRanking
    {
        public static long Score(long silver, long gold, long diamond)
        {
            if (silver < 0 || gold < 0 || diamond < 0)
                throw new ArgumentOutOfRangeException(nameof(silver));
            checked { return silver * 5L + gold * 7L + diamond * 10L; }
        }

        public static List<RankedLeaderboardEntry> Rank(
            IEnumerable<LeaderboardEntry> source,
            string selfId)
        {
            var ordered = source.OrderByDescending(value => value.HighestStage)
                .ThenByDescending(value => value.WeightedScore)
                .ThenBy(value => value.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.PlayerId, StringComparer.Ordinal)
                .ToList();
            var result = new List<RankedLeaderboardEntry>(ordered.Count);
            int displayedRank = 0;
            for (int index = 0; index < ordered.Count; index++)
            {
                LeaderboardEntry entry = ordered[index];
                bool tied = index > 0 &&
                    entry.HighestStage == ordered[index - 1].HighestStage &&
                    entry.WeightedScore == ordered[index - 1].WeightedScore;
                if (!tied) displayedRank = index + 1;
                result.Add(new RankedLeaderboardEntry
                {
                    Entry = entry,
                    Rank = displayedRank,
                    IsSelf = string.Equals(entry.PlayerId, selfId, StringComparison.Ordinal),
                    IsTied = tied || (index + 1 < ordered.Count &&
                        entry.HighestStage == ordered[index + 1].HighestStage &&
                        entry.WeightedScore == ordered[index + 1].WeightedScore)
                });
            }
            return result;
        }
    }

    internal sealed class FirestoreLeaderboardRepository
    {
        private readonly MonoBehaviour _host;
        private readonly GameApiSettings _settings;
        private Coroutine _operation;

        public FirestoreLeaderboardRepository(MonoBehaviour host, GameApiSettings settings)
        {
            _host = host;
            _settings = settings;
        }

        public void Load(string levelId, Action<List<LeaderboardEntry>> completed, Action<string> failed)
        {
            if (_operation != null) return;
            _operation = _host.StartCoroutine(LoadRoutine(levelId, completed, failed));
        }

        public void Cancel()
        {
            if (_operation != null) _host.StopCoroutine(_operation);
            _operation = null;
        }

        private IEnumerator LoadRoutine(
            string levelId,
            Action<List<LeaderboardEntry>> completed,
            Action<string> failed)
        {
            if (_settings == null || !_settings.TryGetLeaderboardDocument(levelId, out string url))
            {
                _operation = null;
                failed?.Invoke("Leaderboard is not configured for your grade.");
                yield break;
            }
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = _settings.RequestTimeoutSeconds;
                yield return request.SendWebRequest();
                _operation = null;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    failed?.Invoke("Could not load your grade leaderboard.");
                    yield break;
                }
                if (!TryMap(request.downloadHandler.text, out List<LeaderboardEntry> entries))
                {
                    failed?.Invoke("The leaderboard data is invalid.");
                    yield break;
                }
                completed?.Invoke(entries);
            }
        }

        private static bool TryMap(string json, out List<LeaderboardEntry> entries)
        {
            entries = new List<LeaderboardEntry>();
            if (!FirestoreJsonNavigator.TryParse(json, out JsonValue document, out _) ||
                !FirestoreJsonNavigator.TryGetDocumentFields(document, out JsonValue fields) ||
                fields.Object == null)
                return false;
            foreach (KeyValuePair<string, JsonValue> pair in fields.Object)
            {
                if (pair.Key == "_meta") continue;
                if (!IsPublicPlayerId(pair.Key)) return false;
                if (!FirestoreJsonNavigator.TryGetMapFields(pair.Value, out JsonValue value))
                    return false;
                if (!TryString(value, "displayName", out string displayName) ||
                    !TryLong(value, "currentStage", out long currentStage) ||
                    !TryLong(value, "highestStage", out long highestStage) ||
                    !TryLong(value, "silver", out long silver) ||
                    !TryLong(value, "gold", out long gold) ||
                    !TryLong(value, "diamond", out long diamond) ||
                    !TryLong(value, "weightedCurrencyScore", out long storedScore) ||
                    !TryLong(value, "totalDamage", out long damage))
                    return false;
                long score;
                try { score = LeaderboardRanking.Score(silver, gold, diamond); }
                catch (OverflowException) { return false; }
                catch (ArgumentOutOfRangeException) { return false; }
                if (storedScore != score || currentStage < 1 || highestStage < 1 ||
                    currentStage > highestStage || highestStage > 200 || damage < 0)
                    return false;
                entries.Add(new LeaderboardEntry
                {
                    PlayerId = pair.Key,
                    DisplayName = displayName,
                    CurrentStage = (int)currentStage,
                    HighestStage = (int)highestStage,
                    Silver = silver,
                    Gold = gold,
                    Diamond = diamond,
                    WeightedScore = score,
                    TotalDamage = damage,
                    AvatarId = ReadString(value, "avatarId"),
                    IconId = ReadString(value, "iconId"),
                    PetId = ReadString(value, "petId"),
                    WeaponId = ReadString(value, "weaponId"),
                    WeaponLevel = (int)Math.Max(0, ReadOptionalLong(value, "weaponLevel"))
                });
            }
            return true;
        }

        private static bool IsPublicPlayerId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32) return false;
            foreach (char character in value)
                if (!Uri.IsHexDigit(character)) return false;
            return true;
        }

        private static bool TryString(JsonValue fields, string name, out string value)
        {
            value = string.Empty;
            return fields != null && fields.TryGet(name, out JsonValue field) &&
                FirestoreJsonNavigator.TryReadString(field, out value) &&
                !string.IsNullOrWhiteSpace(value);
        }

        private static string ReadString(JsonValue fields, string name) =>
            fields != null && fields.TryGet(name, out JsonValue field) &&
            FirestoreJsonNavigator.TryReadString(field, out string value) ? value : string.Empty;

        private static bool TryLong(JsonValue fields, string name, out long value)
        {
            value = 0;
            return fields != null && fields.TryGet(name, out JsonValue field) &&
                FirestoreJsonNavigator.TryReadInteger(field, out value);
        }

        private static long ReadOptionalLong(JsonValue fields, string name) =>
            TryLong(fields, name, out long value) ? value : 0;
    }

    internal sealed class LeaderboardPanelController
    {
        private readonly VisualElement _modal;
        private readonly Button _open;
        private readonly Button _close;
        private readonly Button _refresh;
        private readonly Label _status;
        private readonly Label _cohort;
        private readonly Label _throne;
        private readonly Label _self;
        private readonly ScrollView _list;
        private readonly VisualElement _attemptPanel;
        private readonly FirestoreLeaderboardRepository _repository;
        private readonly PlayerSnapshot _player;
        private readonly string _levelId;
        private List<RankedLeaderboardEntry> _cached;
        private bool _bound;

        public LeaderboardPanelController(
            VisualElement root,
            MonoBehaviour host,
            GameApiSettings settings,
            PlayerSnapshot player)
        {
            _open = root.Q<Button>("leaderboard");
            _modal = root.Q<VisualElement>("leaderboard-modal");
            _close = root.Q<Button>("leaderboard-close");
            _refresh = root.Q<Button>("leaderboard-refresh");
            _status = root.Q<Label>("leaderboard-status");
            _cohort = root.Q<Label>("leaderboard-cohort");
            _throne = root.Q<Label>("leaderboard-throne");
            _self = root.Q<Label>("leaderboard-self");
            _list = root.Q<ScrollView>("leaderboard-list");
            _attemptPanel = root.Q<VisualElement>("combat-attempt-panel");
            _repository = new FirestoreLeaderboardRepository(host, settings);
            _player = player;
            _levelId = ResolveLevel(player == null ? null : player.playerId);
        }

        public bool IsValid => _open != null && _modal != null && _close != null &&
            _refresh != null && _status != null && _list != null;

        public void Bind()
        {
            if (_bound || !IsValid) return;
            _open.clicked += Open;
            _close.clicked += Close;
            _refresh.clicked += Load;
            _modal.style.display = DisplayStyle.None;
            _bound = true;
        }

        public void Dispose()
        {
            if (!_bound) return;
            _open.clicked -= Open;
            _close.clicked -= Close;
            _refresh.clicked -= Load;
            _repository.Cancel();
            _bound = false;
        }

        private void Open()
        {
            if (_attemptPanel != null && _attemptPanel.resolvedStyle.display != DisplayStyle.None)
                return;
            _modal.BringToFront();
            _modal.style.display = DisplayStyle.Flex;
            _cohort.text = (_player?.profile?.gradeBand ?? "Your Grade") + " · " + _levelId.ToUpperInvariant();
            if (_cached != null) Render(_cached, "Showing saved standings · refreshing…");
            else
            {
                _list.Clear();
                _status.text = "Loading your grade leaderboard…";
            }
            Load();
        }

        private void Close()
        {
            _repository.Cancel();
            _modal.style.display = DisplayStyle.None;
            _refresh.SetEnabled(true);
        }

        private void Load()
        {
            _refresh.SetEnabled(false);
            _status.text = "Refreshing…";
            _repository.Load(
                _levelId,
                entries =>
                {
                    _refresh.SetEnabled(true);
                    try
                    {
                        _cached = LeaderboardRanking.Rank(
                            entries,
                            _player?.profile?.publicPlayerId ?? string.Empty);
                        Render(_cached, "Updated " + DateTime.Now.ToString("t", CultureInfo.CurrentCulture));
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError("Leaderboard ranking failed: " + exception.Message);
                        _status.text = "Leaderboard values are invalid.";
                    }
                },
                message =>
                {
                    _refresh.SetEnabled(true);
                    _status.text = _cached == null ? message : "Showing saved standings · " + message;
                });
        }

        private void Render(List<RankedLeaderboardEntry> entries, string status)
        {
            _status.text = status;
            _list.Clear();
            RankedLeaderboardEntry first = entries.FirstOrDefault();
            _throne.text = first == null
                ? "No standings yet"
                : "♛  #1 " + first.Entry.DisplayName + "\nBest Stage " + first.Entry.HighestStage +
                  "\nPet: " + DisplayItem(first.Entry.PetId) + "  Weapon: " + DisplayWeapon(first.Entry);
            RankedLeaderboardEntry self = entries.FirstOrDefault(value => value.IsSelf);
            _self.text = self == null
                ? "YOUR STANDING · Rank updating…"
                : "YOUR STANDING · #" + self.Rank + " · " + self.Entry.DisplayName +
                  " · Best Stage " + self.Entry.HighestStage;
            foreach (RankedLeaderboardEntry ranked in entries)
                _list.Add(CreateRow(ranked));
        }

        private static VisualElement CreateRow(RankedLeaderboardEntry ranked)
        {
            var row = new VisualElement();
            row.AddToClassList("leaderboard-row");
            if (ranked.Rank == 1) row.AddToClassList("leaderboard-row--diamond");
            else if (ranked.Rank == 2) row.AddToClassList("leaderboard-row--gold");
            else if (ranked.Rank == 3) row.AddToClassList("leaderboard-row--silver");
            if (ranked.IsSelf) row.AddToClassList("leaderboard-row--self");
            string tied = ranked.IsTied ? " TIED" : string.Empty;
            row.Add(new Label("#" + ranked.Rank + tied));
            row.Add(new Label((ranked.IsSelf ? "YOU · " : string.Empty) + ranked.Entry.DisplayName));
            row.Add(new Label("Current " + ranked.Entry.CurrentStage + " · Best " + ranked.Entry.HighestStage));
            row.Add(new Label("S " + ranked.Entry.Silver + "  G " + ranked.Entry.Gold + "  D " + ranked.Entry.Diamond));
            row.Add(new Label("Pet: " + DisplayItem(ranked.Entry.PetId) + "  Weapon: " + DisplayWeapon(ranked.Entry)));
            row.Add(new Label("Damage " + ranked.Entry.TotalDamage.ToString("N0", CultureInfo.CurrentCulture)));
            return row;
        }

        private static string DisplayItem(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "None";
            string[] words = id.Replace('_', '-').Split('-');
            for (int index = 0; index < words.Length; index++)
                if (words[index].Length > 0)
                    words[index] = char.ToUpperInvariant(words[index][0]) + words[index].Substring(1);
            return string.Join(" ", words);
        }

        private static string DisplayWeapon(LeaderboardEntry entry) =>
            DisplayItem(entry.WeaponId) + (entry.WeaponLevel > 0 ? " Lv." + entry.WeaponLevel : string.Empty);

        internal static string ResolveLevel(string playerId)
        {
            int separator = string.IsNullOrEmpty(playerId) ? -1 : playerId.IndexOf(':');
            return separator > 0 ? playerId.Substring(0, separator) : string.Empty;
        }
    }
}
