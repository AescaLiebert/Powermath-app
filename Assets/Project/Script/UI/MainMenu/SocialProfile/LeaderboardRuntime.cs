using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PowerMath.Gameplay.Combat;
using PowerMath.Gameplay.Pets;
using PowerMath.Gameplay.Progression;
using PowerMath.PlayerData;
using PowerMath.PlayerLifecycle;
using PowerMath.Session;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace PowerMath.UI.MainMenu.SocialProfile
{
    public sealed class LeaderboardEntry
    {
        public string PlayerId;
        public string DisplayName;
        public string CharacterId;
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
        public long SnapshotAtUnixSeconds;
        public string LevelId;
        public string GradeBand;
    }

    public sealed class RankedLeaderboardEntry
    {
        public LeaderboardEntry Entry;
        public int Rank;
        public bool IsSelf;
        public bool IsTied;
    }

    public static class LeaderboardRanking
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
                .ThenBy(value => value.SnapshotAtUnixSeconds > 0 ? value.SnapshotAtUnixSeconds : long.MaxValue)
                .ThenBy(value => value.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.PlayerId, StringComparer.Ordinal)
                .ToList();
            var result = new List<RankedLeaderboardEntry>(ordered.Count);
            int displayedRank = 0;
            for (int index = 0; index < ordered.Count; index++)
            {
                LeaderboardEntry entry = ordered[index];
                bool tied = index > 0 && AreTied(entry, ordered[index - 1]);
                if (!tied) displayedRank = index + 1;
                result.Add(new RankedLeaderboardEntry
                {
                    Entry = entry,
                    Rank = displayedRank,
                    IsSelf = string.Equals(entry.PlayerId, selfId, StringComparison.Ordinal),
                    IsTied = tied || (index + 1 < ordered.Count && AreTied(entry, ordered[index + 1]))
                });
            }
            return result;
        }

        private static bool AreTied(LeaderboardEntry a, LeaderboardEntry b)
        {
            if (a == null || b == null) return false;
            return a.HighestStage == b.HighestStage &&
                   a.WeightedScore == b.WeightedScore &&
                   a.SnapshotAtUnixSeconds == b.SnapshotAtUnixSeconds;
        }
    }

    public sealed class FirestoreLeaderboardRepository
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

        public void LoadAll(Action<Dictionary<string, List<LeaderboardEntry>>> completed, Action<string> failed)
        {
            if (_operation != null) return;
            _operation = _host.StartCoroutine(LoadAllRoutine(completed, failed));
        }

        private IEnumerator LoadAllRoutine(
            Action<Dictionary<string, List<LeaderboardEntry>>> completed,
            Action<string> failed)
        {
            if (_settings == null || _settings.LevelDocumentCount <= 0)
            {
                _operation = null;
                failed?.Invoke("Leaderboard configuration is missing.");
                yield break;
            }

            var result = new Dictionary<string, List<LeaderboardEntry>>(StringComparer.OrdinalIgnoreCase);
            int total = _settings.LevelDocumentCount;
            for (int i = 0; i < total; i++)
            {
                if (!_settings.TryGetLevelDocument(i, out string docId, out string grade, out _))
                    continue;

                if (!_settings.TryGetLeaderboardDocument(docId, out string url))
                    continue;

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = _settings.RequestTimeoutSeconds;
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        PowerMath.Diagnostics.AppLog.Warning("Leaderboard", "Failed to fetch leaderboard document for: " + docId);
                        result[docId] = new List<LeaderboardEntry>();
                        continue;
                    }

                    if (TryMap(request.downloadHandler.text, out List<LeaderboardEntry> entries, docId, grade))
                    {
                        result[docId] = entries;
                    }
                    else
                    {
                        result[docId] = new List<LeaderboardEntry>();
                    }
                }
            }

            _operation = null;
            completed?.Invoke(result);
        }

        internal static bool TryMap(string json, out List<LeaderboardEntry> entries, string levelId = null, string gradeBand = null)
        {
            entries = new List<LeaderboardEntry>();
            if (!FirestoreJsonNavigator.TryParse(json, out JsonValue document, out _))
                return false;
            if (!FirestoreJsonNavigator.TryGetDocumentFields(document, out JsonValue fields) ||
                fields.Object == null)
                return true;
            foreach (KeyValuePair<string, JsonValue> pair in fields.Object)
            {
                if (pair.Key == "_meta") continue;
                if (!IsPublicPlayerId(pair.Key))
                {
                    PowerMath.Diagnostics.AppLog.Warning("Leaderboard", "Skipping invalid public ID in cohort: " + pair.Key);
                    continue;
                }
                if (!FirestoreJsonNavigator.TryGetMapFields(pair.Value, out JsonValue value))
                {
                    PowerMath.Diagnostics.AppLog.Warning("Leaderboard", "Skipping malformed entry map for: " + pair.Key);
                    continue;
                }
                if (!TryString(value, "displayName", out string displayName) ||
                    !TryLong(value, "currentStage", out long currentStage) ||
                    !TryLong(value, "highestStage", out long highestStage) ||
                    !TryLong(value, "silver", out long silver) ||
                    !TryLong(value, "gold", out long gold) ||
                    !TryLong(value, "diamond", out long diamond) ||
                    !TryLong(value, "weightedCurrencyScore", out long storedScore) ||
                    !TryLong(value, "totalDamage", out long damage))
                {
                    PowerMath.Diagnostics.AppLog.Warning("Leaderboard", "Skipping incomplete entry fields for: " + pair.Key);
                    continue;
                }
                long score;
                try { score = LeaderboardRanking.Score(silver, gold, diamond); }
                catch (Exception)
                {
                    PowerMath.Diagnostics.AppLog.Warning("Leaderboard", "Skipping entry with arithmetic overflow: " + pair.Key);
                    continue;
                }
                if (storedScore != score || currentStage < 1 || highestStage < 1 ||
                    currentStage > highestStage || highestStage > StageId.Final || damage < 0)
                {
                    PowerMath.Diagnostics.AppLog.Warning("Leaderboard", "Skipping entry with invalid stage/score bounds: " + pair.Key);
                    continue;
                }
                long snapshotAt = ReadOptionalLong(value, "snapshotAtUnixSeconds");
                if (snapshotAt <= 0)
                    snapshotAt = ReadOptionalLong(value, "updatedAtUnixSeconds");
                entries.Add(new LeaderboardEntry
                {
                    PlayerId = pair.Key,
                    DisplayName = displayName,
                    CharacterId = ReadString(value, "characterId"),
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
                    WeaponLevel = (int)Math.Max(0, ReadOptionalLong(value, "weaponLevel")),
                    SnapshotAtUnixSeconds = snapshotAt,
                    LevelId = levelId ?? string.Empty,
                    GradeBand = gradeBand ?? string.Empty
                });
            }
            return true;
        }

        internal static bool IsPublicPlayerId(string value)
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
        private readonly Label _throneStage;
        private readonly Label _throneSilver;
        private readonly Label _throneGold;
        private readonly Label _throneDiamond;
        private readonly Label _self;
        private readonly Label _selfRank;
        private readonly Label _selfCurrent;
        private readonly Label _selfBest;
        private readonly Label _selfDamage;
        private readonly VisualElement _selfCurrencies;
        private readonly VisualElement _selfLoadout;
        private readonly Button _jumpToSelf;
        private readonly ScrollView _list;
        private readonly VisualElement _attemptPanel;
        private readonly VisualElement _selfAvatarSlot;
        private readonly VisualElement _selfRankSlot;
        private readonly VisualElement _selfPinnedRow;
        private readonly VisualElement _thronePetSlot;
        private readonly FirestoreLeaderboardRepository _repository;
        private readonly FirestoreLeaderboardProjectionPublisher _publisher;
        private readonly MonoBehaviour _host;
        private readonly PlayerSnapshot _player;
        private readonly string _levelId;
        private readonly IMainMenuPanelHost _panelHost;
        private readonly LeaderboardCharacterVideoPresenter _characterVideo;
        private readonly Label _powerCoins;
        private List<RankedLeaderboardEntry> _cached;
        private bool _bound;
        private bool _loading;
        internal const int BatchSize = 20;
        private bool _batchLoading;
        private int _renderedCount;
        private VisualElement _loadMoreIndicator;
        private Label _loadMoreLabel;

        public LeaderboardPanelController(
            VisualElement root,
            MonoBehaviour host,
            GameApiSettings settings,
            PlayerSnapshot player,
            IMainMenuPanelHost panelHost)
        {
            _open = root.Q<Button>("leaderboard");
            _modal = root.Q<VisualElement>("leaderboard-modal");
            _close = root.Q<Button>("leaderboard-close");
            _refresh = root.Q<Button>("leaderboard-refresh");
            _status = root.Q<Label>("leaderboard-status");
            _cohort = root.Q<Label>("leaderboard-cohort");
            _throne = root.Q<Label>("leaderboard-throne");
            _throneStage = root.Q<Label>("leaderboard-throne-stage");
            _throneSilver = root.Q<Label>("leaderboard-throne-silver");
            _throneGold = root.Q<Label>("leaderboard-throne-gold");
            _throneDiamond = root.Q<Label>("leaderboard-throne-diamond");
            _powerCoins = root.Q<Label>("leaderboard-power-coins");
            _self = root.Q<Label>("leaderboard-self");
            _selfRank = root.Q<Label>("leaderboard-self-rank");
            _selfCurrent = root.Q<Label>("leaderboard-self-current");
            _selfBest = root.Q<Label>("leaderboard-self-best");
            _selfDamage = root.Q<Label>("leaderboard-self-damage");
            _selfCurrencies = root.Q<VisualElement>("leaderboard-self-currencies");
            _selfLoadout = root.Q<VisualElement>("leaderboard-self-loadout");
            _selfAvatarSlot = root.Q<VisualElement>(className: "leaderboard-avatar-slot--self") ??
                root.Q<VisualElement>(".leaderboard-pinned-self")?.Q<VisualElement>(".row-avatar-slot");
            _selfRankSlot = root.Q<VisualElement>(className: "leaderboard-self-rank") ??
                root.Q<VisualElement>(".rank-badge-self") ??
                _selfRank?.parent;
            _selfPinnedRow = root.Q<VisualElement>(className: "leaderboard-pinned-row") ??
                root.Q<VisualElement>(".pinned-self-row");
            _thronePetSlot = root.Q<VisualElement>(".pet-sprite-placeholder");
            _jumpToSelf = root.Q<Button>("leaderboard-jump-to-self");
            _list = root.Q<ScrollView>("leaderboard-list");
            _attemptPanel = root.Q<VisualElement>("combat-attempt-panel");
            _host = host;
            _publisher = settings == null ? null : new FirestoreLeaderboardProjectionPublisher(settings);
            _repository = new FirestoreLeaderboardRepository(host, settings);
            _player = player;
            _levelId = ResolveLevel(player == null ? null : player.playerId);
            _panelHost = panelHost ?? throw new ArgumentNullException(nameof(panelHost));
            _characterVideo = host.GetComponent<LeaderboardCharacterVideoPresenter>() ??
                host.gameObject.AddComponent<LeaderboardCharacterVideoPresenter>();
            _characterVideo.Bind(_modal);
        }

        public bool IsValid => _open != null && _modal != null && _close != null &&
            _refresh != null && _status != null && _list != null;

        public void Bind()
        {
            if (_bound || !IsValid) return;
            _open.clicked += Open;
            _close.clicked += Close;
            _refresh.clicked += Load;
            if (_jumpToSelf != null) _jumpToSelf.clicked += JumpToSelf;
            if (_list != null && _list.verticalScroller != null)
                _list.verticalScroller.valueChanged += OnScrollChanged;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed += OnPlayerChanged;
            UpdatePowerCoins(_player ?? PlayerSessionStore.Instance?.Snapshot);
            _modal.style.display = DisplayStyle.None;
            _bound = true;
        }

        public void Dispose()
        {
            if (!_bound) return;
            if (PlayerSessionStore.Instance != null)
                PlayerSessionStore.Instance.Changed -= OnPlayerChanged;
            _open.clicked -= Open;
            _close.clicked -= Close;
            _refresh.clicked -= Load;
            if (_jumpToSelf != null) _jumpToSelf.clicked -= JumpToSelf;
            if (_list != null && _list.verticalScroller != null)
                _list.verticalScroller.valueChanged -= OnScrollChanged;
            _repository.Cancel();
            _characterVideo.Hide();
            if (_panelHost.OpenPanel == MainMenuPanelId.Leaderboard)
                _panelHost.TryClose(MainMenuPanelId.Leaderboard, _open);
            _bound = false;
        }

        private void Open()
        {
            TryOpenFromTutorial();
        }

        public bool TryOpenFromTutorial()
        {
            if (_attemptPanel != null && _attemptPanel.resolvedStyle.display != DisplayStyle.None)
                return false;
            if (!_panelHost.TryOpen(
                    MainMenuPanelId.Leaderboard,
                    _modal, _open)) return false;
            UpdatePowerCoins(_player ?? PlayerSessionStore.Instance?.Snapshot);
            _cohort.text = (_player?.profile?.gradeBand ?? "Your Grade") + " · " + _levelId.ToUpperInvariant();
            if (_cached != null) Render(_cached, "Showing saved standings · refreshing…");
            else
            {
                _list.Clear();
                _status.text = PowerMath.Localization.LocalizationService.Get("menu.leaderboardLoading");
            }
            if (_publisher != null && _player != null && _host != null)
            {
                _host.StartCoroutine(_publisher.Publish(
                    _player,
                    () => { },
                    error => PowerMath.Diagnostics.AppLog.Warning("Leaderboard", error)));
            }
            Load();
            return true;
        }

        private void OnPlayerChanged(PlayerSnapshot player)
        {
            UpdatePowerCoins(player);
        }

        private void UpdatePowerCoins(PlayerSnapshot player)
        {
            if (_powerCoins == null) return;
            long coins = player?.wallet?.powerCoins ?? 0;
            _powerCoins.text = coins.ToString("N0");
        }

        private void Close()
        {
            _repository.Cancel();
            _loading = false;
            _panelHost.TryClose(MainMenuPanelId.Leaderboard, _open);
            _characterVideo.Hide();
            _refresh.SetEnabled(true);
            SetSemanticState();
        }

        private void Load()
        {
            if (_loading) return;
            _loading = true;
            _refresh.SetEnabled(false);
            _status.text = PowerMath.Localization.LocalizationService.Get("menu.refreshing");
            SetSemanticState("is-loading");
            _repository.Load(
                _levelId,
                entries =>
                {
                    _loading = false;
                    _refresh.SetEnabled(true);
                    try
                    {
                        IncludeAuthoritativeSelf(entries);
                        _cached = LeaderboardRanking.Rank(
                            entries,
                            _player?.profile?.publicPlayerId ?? string.Empty);
                        Render(_cached, "Updated " + DateTime.Now.ToString("t", CultureInfo.CurrentCulture));
                        SetSemanticState("is-ready");
                    }
                    catch (Exception exception)
                    {
                        PowerMath.Diagnostics.AppLog.Error("Leaderboard", "Leaderboard ranking failed: " + exception.Message);
                        _status.text = PowerMath.Localization.LocalizationService.Get("errors.leaderboardInvalid");
                        SetSemanticState("is-error");
                    }
                },
                message =>
                {
                    _loading = false;
                    _refresh.SetEnabled(true);
                    if (_cached == null)
                    {
                        var localEntries = new List<LeaderboardEntry>();
                        IncludeAuthoritativeSelf(localEntries);
                        if (localEntries.Count > 0)
                        {
                            _cached = LeaderboardRanking.Rank(
                                localEntries,
                                _player?.profile?.publicPlayerId ?? string.Empty);
                            Render(
                                _cached,
                                "Public standings unavailable · showing your saved standing");
                        }
                        else
                        {
                            _status.text = message;
                        }
                    }
                    else
                    {
                        _status.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, PowerMath.Localization.LocalizationService.Get("menu.savedStandings"), message);
                    }
                    SetSemanticState("is-error");
                });
        }

        private void IncludeAuthoritativeSelf(List<LeaderboardEntry> entries)
        {
            LeaderboardEntry self = CreateAuthoritativeSelfEntry(_player);
            if (self == null) return;
            LeaderboardEntry existingInRemote = entries.Find(entry => string.Equals(
                entry.PlayerId,
                self.PlayerId,
                StringComparison.Ordinal));
            if (existingInRemote != null && existingInRemote.SnapshotAtUnixSeconds > 0)
            {
                if (self.HighestStage == existingInRemote.HighestStage &&
                    self.WeightedScore == existingInRemote.WeightedScore)
                {
                    self.SnapshotAtUnixSeconds = existingInRemote.SnapshotAtUnixSeconds;
                    if (_player?.progression != null)
                    {
                        _player.progression.leaderboardSnapshotAtUnixSeconds = existingInRemote.SnapshotAtUnixSeconds;
                        _player.progression.lastSnapshotHighestStage = self.HighestStage;
                        _player.progression.lastSnapshotWeightedScore = self.WeightedScore;
                    }
                }
            }
            entries.RemoveAll(entry => string.Equals(
                entry.PlayerId,
                self.PlayerId,
                StringComparison.Ordinal));
            entries.Add(self);
        }

        private static LeaderboardEntry CreateAuthoritativeSelfEntry(PlayerSnapshot player)
        {
            string publicId = player?.profile?.publicPlayerId?.Trim() ?? string.Empty;
            if (!FirestoreLeaderboardRepository.IsPublicPlayerId(publicId))
                return null;

            PlayerSnapshot.ProfileData profile = player.profile ?? new PlayerSnapshot.ProfileData();
            PlayerSnapshot.ProgressionData progression =
                player.progression ?? new PlayerSnapshot.ProgressionData();
            PlayerSnapshot.WalletData wallet = player.wallet ?? new PlayerSnapshot.WalletData();
            PlayerSnapshot.LoadoutData loadout = player.loadout ?? new PlayerSnapshot.LoadoutData();
            long silver = Math.Max(0, wallet.silver);
            long gold = Math.Max(0, wallet.gold);
            long diamond = Math.Max(0, wallet.diamond);
            int currentStage = Math.Max(1, Math.Min(StageId.Final, progression.currentStage));
            int highestStage = Math.Max(currentStage, Math.Min(StageId.Final, progression.highestStage));
            int weaponLevel = 0;
            foreach (PlayerSnapshot.InventoryItemData item in
                     player.inventory ?? Array.Empty<PlayerSnapshot.InventoryItemData>())
            {
                if (item != null && item.itemId == WeaponAscensionPolicy.CanonicalItemId)
                    weaponLevel = Math.Max(weaponLevel, item.upgradeLevel);
            }

            long snapshotAt = player?.progression?.leaderboardSnapshotAtUnixSeconds ?? 0;
            if (snapshotAt <= 0)
            {
                snapshotAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (player?.progression != null)
                    player.progression.leaderboardSnapshotAtUnixSeconds = snapshotAt;
            }

            return new LeaderboardEntry
            {
                PlayerId = publicId,
                DisplayName = string.IsNullOrWhiteSpace(profile.displayName)
                    ? "You"
                    : profile.displayName,
                CharacterId = profile.characterId ?? string.Empty,
                CurrentStage = currentStage,
                HighestStage = highestStage,
                Silver = silver,
                Gold = gold,
                Diamond = diamond,
                WeightedScore = LeaderboardRanking.Score(silver, gold, diamond),
                TotalDamage = Math.Max(0, progression.totalDamage),
                AvatarId = loadout.avatarId ?? profile.iconId ?? string.Empty,
                IconId = profile.iconId ?? string.Empty,
                PetId = loadout.petId ?? string.Empty,
                WeaponId = loadout.weaponId ?? string.Empty,
                WeaponLevel = weaponLevel,
                SnapshotAtUnixSeconds = snapshotAt
            };
        }

        private static CharacterPresentationCatalog s_characterCatalog;
        private static WeaponAscensionCatalogDefinition s_weaponCatalog;
        private static PetGachaCatalogDefinition s_petCatalog;
        private static VisualTreeAsset s_leaderboardRowTemplate;
        private static Sprite s_badge1st;
        private static Sprite s_badge2nd;
        private static Sprite s_badge3rd;

        private void Render(List<RankedLeaderboardEntry> entries, string status)
        {
            _status.text = status;
            _cached = entries;
            _list.Clear();
            _renderedCount = 0;
            RankedLeaderboardEntry first = entries?.FirstOrDefault();
            _throne.text = first == null ? "No standings yet" : first.Entry.DisplayName;
            _throneStage.text = first == null ? "--" : first.Entry.HighestStage.ToString(CultureInfo.CurrentCulture);
            _throneSilver.text = FormatNumber(first?.Entry.Silver ?? 0);
            _throneGold.text = FormatNumber(first?.Entry.Gold ?? 0);
            _throneDiamond.text = FormatNumber(first?.Entry.Diamond ?? 0);
            _characterVideo.Show(first?.Entry.CharacterId);
            RenderThronePet(first?.Entry?.PetId);

            RankedLeaderboardEntry self = entries?.FirstOrDefault(value => value.IsSelf);
            RenderSelf(self);
            LoadNextBatch();
        }

        internal int RenderedCount => _renderedCount;

        private void OnScrollChanged(float value)
        {
            if (_batchLoading || _cached == null || _renderedCount >= _cached.Count || _list == null) return;
            float max = _list.verticalScroller != null ? _list.verticalScroller.highValue : 0;
            if (max > 0 && (value >= max - 200f || value >= max * 0.8f))
            {
                LoadNextBatch();
            }
        }

        internal void LoadNextBatch()
        {
            if (_batchLoading || _cached == null || _renderedCount >= _cached.Count)
            {
                UpdateLoadMoreIndicator();
                return;
            }

            _batchLoading = true;
            try
            {
                if (_loadMoreIndicator != null && _loadMoreIndicator.parent == _list)
                    _loadMoreIndicator.RemoveFromHierarchy();

                int target = Math.Min(_renderedCount + BatchSize, _cached.Count);
                for (int i = _renderedCount; i < target; i++)
                {
                    _list.Add(CreateRow(_cached[i]));
                }
                _renderedCount = target;

                UpdateLoadMoreIndicator();
            }
            finally
            {
                _batchLoading = false;
            }
        }

        private void UpdateLoadMoreIndicator()
        {
            if (_cached == null || _cached.Count == 0)
            {
                if (_loadMoreIndicator != null && _loadMoreIndicator.parent == _list)
                    _loadMoreIndicator.RemoveFromHierarchy();
                return;
            }

            if (_loadMoreIndicator == null)
            {
                _loadMoreIndicator = new VisualElement { name = "leaderboard-load-more" };
                _loadMoreIndicator.AddToClassList("leaderboard-load-more");
                _loadMoreIndicator.style.alignItems = Align.Center;
                _loadMoreIndicator.style.justifyContent = Justify.Center;
                _loadMoreIndicator.style.paddingTop = 16;
                _loadMoreIndicator.style.paddingBottom = 24;

                _loadMoreLabel = new Label { name = "leaderboard-load-more-label" };
                _loadMoreLabel.AddToClassList("leaderboard-load-more-label");
                _loadMoreLabel.style.color = new Color(0.7f, 0.7f, 0.75f, 0.9f);
                _loadMoreLabel.style.fontSize = 13;
                _loadMoreIndicator.Add(_loadMoreLabel);
            }

            if (_renderedCount < _cached.Count)
            {
                _loadMoreLabel.text = string.Format(CultureInfo.InvariantCulture,
                    "Showing {0} of {1} explorers · Scroll to load more", _renderedCount, _cached.Count);
                if (_loadMoreIndicator.parent != _list)
                    _list.Add(_loadMoreIndicator);
            }
            else
            {
                if (_cached.Count > BatchSize)
                {
                    _loadMoreLabel.text = string.Format(CultureInfo.InvariantCulture,
                        "Showing all {0} explorers", _cached.Count);
                    if (_loadMoreIndicator.parent != _list)
                        _list.Add(_loadMoreIndicator);
                }
                else if (_loadMoreIndicator.parent == _list)
                {
                    _loadMoreIndicator.RemoveFromHierarchy();
                }
            }
        }

        private void RenderThronePet(string petId)
        {
            if (_thronePetSlot == null) return;
            _thronePetSlot.Clear();
            Sprite petIcon = ResolvePetIcon(petId);
            if (petIcon != null)
            {
                var image = new Image
                {
                    name = "Throne Pet Icon",
                    sprite = petIcon,
                    pickingMode = PickingMode.Ignore
                };
                image.AddToClassList("leaderboard-pet-throne-image");
                _thronePetSlot.Add(image);
                _thronePetSlot.style.display = DisplayStyle.Flex;
            }
            else
            {
                _thronePetSlot.style.display = DisplayStyle.None;
            }
        }

        private void RenderSelf(RankedLeaderboardEntry self)
        {
            _self.text = self == null ? "Rank updating…" : self.Entry.DisplayName;
            _selfRank.text = self == null ? "--" : "#" + self.Rank;
            _selfCurrent.text = self == null ? "--" : self.Entry.CurrentStage.ToString(CultureInfo.CurrentCulture);
            _selfBest.text = self == null ? "--" : self.Entry.HighestStage.ToString(CultureInfo.CurrentCulture);
            _selfDamage.text = self == null ? "0" : FormatCompact(self.Entry.TotalDamage);
            _selfCurrencies.Clear();
            _selfLoadout.Clear();
            if (_selfPinnedRow != null)
            {
                _selfPinnedRow.RemoveFromClassList("leaderboard-row--diamond");
                _selfPinnedRow.RemoveFromClassList("leaderboard-row--gold");
                _selfPinnedRow.RemoveFromClassList("leaderboard-row--silver");
                if (self != null && self.Rank == 1) _selfPinnedRow.AddToClassList("leaderboard-row--diamond");
                else if (self != null && self.Rank == 2) _selfPinnedRow.AddToClassList("leaderboard-row--gold");
                else if (self != null && self.Rank == 3) _selfPinnedRow.AddToClassList("leaderboard-row--silver");
            }
            ApplyRankSlotVisuals(_selfRankSlot, _selfRank, self != null ? self.Rank : 0);
            if (self == null) return;
            AddCurrencyRows(_selfCurrencies, self.Entry);
            AddLoadoutSlots(_selfLoadout, self.Entry);
            RenderAvatar(_selfAvatarSlot, self.Entry);
        }

        private static VisualElement CreateRow(RankedLeaderboardEntry ranked)
        {
            VisualElement authoredRow = CreateAuthoredRow(ranked);
            if (authoredRow != null) return authoredRow;

            var row = new VisualElement();
            row.AddToClassList("leaderboard-row");
            if (ranked.Rank == 1) row.AddToClassList("leaderboard-row--diamond");
            else if (ranked.Rank == 2) row.AddToClassList("leaderboard-row--gold");
            else if (ranked.Rank == 3) row.AddToClassList("leaderboard-row--silver");
            if (ranked.IsSelf) row.AddToClassList("leaderboard-row--self");
            row.name = ".leaderboard-row--rank-" + ranked.Rank;
            var rankSlot = CreateElement(".row-rank-slot", "leaderboard-row-rank-slot");
            var rankNumber = CreateLabel("Rank Number", ranked.Rank.ToString(CultureInfo.CurrentCulture), "leaderboard-row-rank-number");
            rankSlot.Add(rankNumber);
            ApplyRankSlotVisuals(rankSlot, rankNumber, ranked.Rank);
            rankSlot.tooltip = ranked.IsTied ? "Tied rank" : "Rank " + ranked.Rank;
            row.Add(rankSlot);

            var avatar = CreateElement(".row-avatar-slot", "leaderboard-avatar-slot");
            avatar.tooltip = DisplayItem(ranked.Entry.AvatarId);
            RenderAvatar(avatar, ranked.Entry);
            row.Add(avatar);

            var identity = CreateElement(".row-identity", "leaderboard-row-identity");
            identity.Add(CreateLabel(".player-name", ranked.Entry.DisplayName, "leaderboard-player-name"));
            identity.Add(CreateStagesRow(ranked.Entry));
            row.Add(identity);

            var currencies = CreateElement(".row-currencies", "leaderboard-row-currencies");
            AddCurrencyRows(currencies, ranked.Entry);
            row.Add(currencies);

            var loadout = CreateElement(".row-loadout", "leaderboard-row-loadout");
            AddLoadoutSlots(loadout, ranked.Entry);
            row.Add(loadout);

            var damage = CreateElement(".row-damage", "leaderboard-row-damage");
            damage.Add(CreateLabel(".dmg-label", "TOTAL DMG", "leaderboard-damage-label"));
            damage.Add(CreateLabel(".dmg-value", FormatCompact(ranked.Entry.TotalDamage), "leaderboard-damage-value"));
            row.Add(damage);
            return row;
        }

        private static readonly Dictionary<string, Sprite> s_profileIconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> s_petIconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        internal sealed class RowViewHolder
        {
            public readonly VisualElement Root;
            public readonly VisualElement RankSlot;
            public readonly Label RankNumber;
            public readonly VisualElement Avatar;
            public readonly Label PlayerName;
            public readonly Label CurrentStage;
            public readonly Label BestStage;
            public readonly Label Silver;
            public readonly Label Gold;
            public readonly Label Diamond;
            public readonly Label DamageValue;
            public readonly VisualElement Loadout;

            public RowViewHolder(VisualElement root)
            {
                Root = root;
                RankSlot = root.Q<VisualElement>("leaderboard-row-rank-slot");
                RankNumber = root.Q<Label>("leaderboard-row-rank-number");
                Avatar = root.Q<VisualElement>("leaderboard-row-avatar");
                PlayerName = root.Q<Label>("leaderboard-row-player-name");
                CurrentStage = root.Q<Label>("leaderboard-row-current");
                BestStage = root.Q<Label>("leaderboard-row-best");
                Silver = root.Q<Label>("leaderboard-row-silver");
                Gold = root.Q<Label>("leaderboard-row-gold");
                Diamond = root.Q<Label>("leaderboard-row-diamond");
                DamageValue = root.Q<Label>("leaderboard-row-damage-value");
                Loadout = root.Q<VisualElement>("leaderboard-row-loadout");
                root.userData = this;
            }

            public void Bind(RankedLeaderboardEntry ranked, bool showCohortBadge)
            {
                Root.RemoveFromClassList("leaderboard-row--diamond");
                Root.RemoveFromClassList("leaderboard-row--gold");
                Root.RemoveFromClassList("leaderboard-row--silver");
                Root.RemoveFromClassList("leaderboard-row--self");

                if (ranked.Rank == 1) Root.AddToClassList("leaderboard-row--diamond");
                else if (ranked.Rank == 2) Root.AddToClassList("leaderboard-row--gold");
                else if (ranked.Rank == 3) Root.AddToClassList("leaderboard-row--silver");
                if (ranked.IsSelf) Root.AddToClassList("leaderboard-row--self");
                Root.name = ".leaderboard-row--rank-" + ranked.Rank;

                if (RankNumber != null)
                {
                    RankNumber.text = ranked.Rank.ToString(CultureInfo.CurrentCulture);
                }
                ApplyRankSlotVisuals(RankSlot, RankNumber, ranked.Rank);
                if (RankSlot != null)
                    RankSlot.tooltip = ranked.IsTied ? "Tied rank" : "Rank " + ranked.Rank;

                if (Avatar != null)
                {
                    Avatar.tooltip = DisplayItem(ranked.Entry.AvatarId);
                    RenderAvatar(Avatar, ranked.Entry);
                }

                string displayName = ranked.Entry.DisplayName;
                if (showCohortBadge && !string.IsNullOrEmpty(ranked.Entry.GradeBand))
                {
                    displayName = $"{displayName}  ({ranked.Entry.GradeBand})";
                }

                if (PlayerName != null) PlayerName.text = displayName;
                if (CurrentStage != null) CurrentStage.text = ranked.Entry.CurrentStage.ToString(CultureInfo.CurrentCulture);
                if (BestStage != null) BestStage.text = ranked.Entry.HighestStage.ToString(CultureInfo.CurrentCulture);
                if (Silver != null) Silver.text = FormatNumber(ranked.Entry.Silver);
                if (Gold != null) Gold.text = FormatNumber(ranked.Entry.Gold);
                if (Diamond != null) Diamond.text = FormatNumber(ranked.Entry.Diamond);
                if (DamageValue != null) DamageValue.text = FormatCompact(ranked.Entry.TotalDamage);

                if (Loadout != null)
                {
                    Loadout.Clear();
                    AddLoadoutSlots(Loadout, ranked.Entry);
                }
            }
        }

        internal static VisualElement CreateAuthoredRow(RankedLeaderboardEntry ranked, bool showCohortBadge = false)
        {
            if (s_leaderboardRowTemplate == null)
                s_leaderboardRowTemplate = Resources.Load<VisualTreeAsset>("LeaderboardListRow");
            if (s_leaderboardRowTemplate == null) return null;

            TemplateContainer instance = s_leaderboardRowTemplate.Instantiate();
            VisualElement row = instance.Q<VisualElement>("leaderboard-row-root");
            if (row == null) return null;
            row.RemoveFromHierarchy();

            var holder = new RowViewHolder(row);
            holder.Bind(ranked, showCohortBadge);
            return row;
        }

        private static void SetText(VisualElement root, string name, string value)
        {
            Label label = root.Q<Label>(name);
            if (label != null) label.text = value;
        }

        private static void RenderAvatar(VisualElement slot, LeaderboardEntry entry)
        {
            if (slot == null) return;
            slot.Clear();
            Sprite profileSprite = ResolveProfileIcon(entry);
            if (profileSprite != null)
            {
                var image = new Image
                {
                    name = "Avatar Icon",
                    sprite = profileSprite,
                    pickingMode = PickingMode.Ignore
                };
                image.AddToClassList("leaderboard-avatar-image");
                slot.Add(image);
            }
            else
            {
                slot.Add(CreateLabel("Slot Label", "ProfilePic Slot", "leaderboard-slot-label"));
            }
        }

        internal static Sprite ResolveProfileIcon(LeaderboardEntry entry)
        {
            if (entry == null) return null;
            string charId = !string.IsNullOrWhiteSpace(entry.CharacterId)
                ? entry.CharacterId
                : (!string.IsNullOrWhiteSpace(entry.AvatarId) ? entry.AvatarId : "ricko");

            if (s_profileIconCache.TryGetValue(charId, out Sprite cached))
                return cached;

            if (s_characterCatalog == null)
                s_characterCatalog = CharacterPresentationCatalog.Load();
            CharacterPresentationCatalog.Character def = s_characterCatalog?.Find(charId);
            Sprite authored = def?.profileIcon != null ? def.profileIcon : def?.selectionArt;
            Sprite resolved = CharacterPlaceholderSprites.Resolve(authored, charId);
            s_profileIconCache[charId] = resolved;
            return resolved;
        }

        internal static Sprite ResolveWeaponIcon(LeaderboardEntry entry)
        {
            if (entry == null) return null;
            if (s_weaponCatalog == null)
                s_weaponCatalog = Resources.Load<WeaponAscensionCatalogDefinition>("WeaponAscensionCatalog");
            if (s_weaponCatalog == null) return null;
            WeaponAscensionCatalogDefinition.Tier tier = s_weaponCatalog.Resolve(entry.WeaponLevel);
            return tier?.icon;
        }

        internal static Sprite ResolvePetIcon(string petId)
        {
            if (string.IsNullOrWhiteSpace(petId) || string.Equals(petId, "none", StringComparison.OrdinalIgnoreCase))
                return null;

            if (s_petIconCache.TryGetValue(petId, out Sprite cached))
                return cached;

            if (s_petCatalog == null)
            {
                s_petCatalog = Resources.Load<PetGachaCatalogDefinition>("Pets/PetGachaCatalog") ??
                               Resources.Load<PetGachaCatalogDefinition>("PetGachaCatalog");
            }
            if (s_petCatalog == null) return null;
            foreach (PetGachaCatalogDefinition.RarityContent rarity in s_petCatalog.Rarities)
            {
                if (rarity?.pets == null) continue;
                foreach (PetDefinition pet in rarity.pets)
                {
                    if (pet != null && string.Equals(pet.PetId, petId, StringComparison.OrdinalIgnoreCase))
                    {
                        s_petIconCache[petId] = pet.Icon;
                        return pet.Icon;
                    }
                }
            }
            s_petIconCache[petId] = null;
            return null;
        }

        internal static Sprite ResolveBadgeIcon(int rank)
        {
            if (rank == 1)
            {
                if (s_badge1st == null)
                {
#if UNITY_EDITOR
                    s_badge1st = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/UI/Icon/Icon_Leaderboard_badge_1st.png");
#endif
                }
                return s_badge1st;
            }
            if (rank == 2)
            {
                if (s_badge2nd == null)
                {
#if UNITY_EDITOR
                    s_badge2nd = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/UI/Icon/Icon_Leaderboard_badge_2nd.png");
#endif
                }
                return s_badge2nd;
            }
            if (rank == 3)
            {
                if (s_badge3rd == null)
                {
#if UNITY_EDITOR
                    s_badge3rd = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/UI/Icon/Icon_Leaderboard_badge_3rd.png");
#endif
                }
                return s_badge3rd;
            }
            return null;
        }

        private static void ApplyRankSlotVisuals(VisualElement rankSlot, Label rankLabel, int rank)
        {
            if (rankSlot == null) return;
            rankSlot.RemoveFromClassList("leaderboard-row-rank-slot--1st");
            rankSlot.RemoveFromClassList("leaderboard-row-rank-slot--2nd");
            rankSlot.RemoveFromClassList("leaderboard-row-rank-slot--3rd");
            rankSlot.style.backgroundImage = StyleKeyword.Null;

            if (rankLabel != null)
            {
                rankLabel.style.display = DisplayStyle.Flex;
            }
        }

        private static VisualElement CreateStagesRow(LeaderboardEntry entry)
        {
            var stages = CreateElement(".stages-row", "leaderboard-stages-row");
            var current = CreateElement(".stage-col-current", "leaderboard-stage-column");
            current.Add(CreateLabel("lbl", "CURRENT", "leaderboard-stage-label"));
            current.Add(CreateLabel("val", entry.CurrentStage.ToString(CultureInfo.CurrentCulture), "leaderboard-stage-current"));
            var best = CreateElement(".stage-col-best", "leaderboard-stage-column");
            best.Add(CreateLabel("lbl", "BEST", "leaderboard-stage-label"));
            best.Add(CreateLabel("val", entry.HighestStage.ToString(CultureInfo.CurrentCulture), "leaderboard-stage-best"));
            stages.Add(current);
            stages.Add(best);
            return stages;
        }

        private static void AddCurrencyRows(VisualElement parent, LeaderboardEntry entry)
        {
            parent.Add(CreateCurrencyRow(".currency-silver", "Silver Amount", entry.Silver, "leaderboard-currency-icon--silver"));
            parent.Add(CreateCurrencyRow(".currency-gold", "Gold Amount", entry.Gold, "leaderboard-currency-icon--gold"));
            parent.Add(CreateCurrencyRow(".currency-diamond", "Diamond Amount", entry.Diamond, "leaderboard-currency-icon--diamond"));
        }

        private static VisualElement CreateCurrencyRow(string name, string valueName, long value, string iconClass)
        {
            var row = CreateElement(name, "leaderboard-currency-row");
            var icon = CreateElement("Vector / Currency Icon", "leaderboard-currency-icon");
            icon.AddToClassList(iconClass);
            row.Add(icon);
            row.Add(CreateLabel(valueName, FormatNumber(value), "leaderboard-currency-value"));
            return row;
        }

        private static void AddLoadoutSlots(VisualElement parent, LeaderboardEntry entry)
        {
            var petSlot = CreateElement(".slot-pet", "leaderboard-loadout-slot");
            petSlot.tooltip = DisplayItem(entry.PetId);
            Sprite petIcon = ResolvePetIcon(entry.PetId);
            if (petIcon != null)
            {
                var petImage = new Image
                {
                    name = "Pet Icon",
                    sprite = petIcon,
                    pickingMode = PickingMode.Ignore
                };
                petImage.AddToClassList("leaderboard-loadout-image");
                petSlot.Add(petImage);
            }
            else
            {
                petSlot.Add(CreateLabel("Slot Label", "Pet\nSlot", "leaderboard-slot-label"));
            }
            parent.Add(petSlot);

            var weaponSlot = CreateElement(".slot-weapon", "leaderboard-loadout-slot");
            weaponSlot.tooltip = DisplayWeapon(entry);
            Sprite weaponIcon = ResolveWeaponIcon(entry);
            if (weaponIcon != null)
            {
                var weaponImage = new Image
                {
                    name = "Weapon Icon",
                    sprite = weaponIcon,
                    pickingMode = PickingMode.Ignore
                };
                weaponImage.AddToClassList("leaderboard-loadout-image");
                weaponSlot.Add(weaponImage);
                if (entry.WeaponLevel > 0)
                {
                    var levelBadge = CreateLabel("Weapon Level", "Lv." + entry.WeaponLevel, "leaderboard-weapon-level-badge");
                    weaponSlot.Add(levelBadge);
                }
            }
            else
            {
                weaponSlot.Add(CreateLabel("Slot Label", "Weapon\nSlot", "leaderboard-slot-label"));
            }
            parent.Add(weaponSlot);
        }

        private static VisualElement CreateLoadoutSlot(string name, string label, string tooltip)
        {
            var slot = CreateElement(name, "leaderboard-loadout-slot");
            slot.tooltip = tooltip;
            slot.Add(CreateLabel("Slot Label", label, "leaderboard-slot-label"));
            return slot;
        }

        private static VisualElement CreateElement(string name, string className)
        {
            var element = new VisualElement { name = name };
            element.AddToClassList(className);
            return element;
        }

        private static Label CreateLabel(string name, string text, string className)
        {
            var label = new Label(text) { name = name };
            label.AddToClassList(className);
            return label;
        }

        private void JumpToSelf()
        {
            if (_cached == null || _cached.Count == 0) return;
            VisualElement row = _list.Q<VisualElement>(className: "leaderboard-row--self");
            if (row == null)
            {
                int selfIndex = _cached.FindIndex(entry => entry.IsSelf);
                if (selfIndex >= 0)
                {
                    while (_renderedCount <= selfIndex && _renderedCount < _cached.Count)
                    {
                        LoadNextBatch();
                    }
                    row = _list.Q<VisualElement>(className: "leaderboard-row--self");
                }
            }
            if (row != null) _list.ScrollTo(row);
        }

        private void SetSemanticState(string state = null)
        {
            _modal.EnableInClassList("is-loading", state == "is-loading");
            _modal.EnableInClassList("is-ready", state == "is-ready");
            _modal.EnableInClassList("is-error", state == "is-error");
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

        internal static string FormatNumber(long value) =>
            Math.Max(0, value).ToString("N0", CultureInfo.CurrentCulture);

        internal static string FormatCompact(long value)
        {
            value = Math.Max(0, value);
            if (value >= 1_000_000) return (value / 1_000_000d).ToString("0.#", CultureInfo.CurrentCulture) + "M";
            if (value >= 1_000) return (value / 1_000d).ToString("0.#", CultureInfo.CurrentCulture) + "K";
            return value.ToString(CultureInfo.CurrentCulture);
        }

        internal static string ResolveLevel(string playerId)
        {
            int separator = string.IsNullOrEmpty(playerId) ? -1 : playerId.IndexOf(':');
            return separator > 0 ? playerId.Substring(0, separator) : string.Empty;
        }
    }
}
