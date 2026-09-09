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
    internal sealed class LeaderboardEntry
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
            if (!FirestoreJsonNavigator.TryParse(json, out JsonValue document, out _))
                return false;
            if (!FirestoreJsonNavigator.TryGetDocumentFields(document, out JsonValue fields) ||
                fields.Object == null)
                return true;
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
                    currentStage > highestStage || highestStage > StageId.Final || damage < 0)
                    return false;
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
                    WeaponLevel = (int)Math.Max(0, ReadOptionalLong(value, "weaponLevel"))
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
        private readonly VisualElement _thronePetSlot;
        private readonly FirestoreLeaderboardRepository _repository;
        private readonly PlayerSnapshot _player;
        private readonly string _levelId;
        private readonly IMainMenuPanelHost _panelHost;
        private readonly LeaderboardCharacterVideoPresenter _characterVideo;
        private readonly Label _powerCoins;
        private List<RankedLeaderboardEntry> _cached;
        private bool _bound;
        private bool _loading;

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
            _thronePetSlot = root.Q<VisualElement>(".pet-sprite-placeholder");
            _jumpToSelf = root.Q<Button>("leaderboard-jump-to-self");
            _list = root.Q<ScrollView>("leaderboard-list");
            _attemptPanel = root.Q<VisualElement>("combat-attempt-panel");
            _repository = new FirestoreLeaderboardRepository(host, settings);
            _player = player;
            _levelId = ResolveLevel(player == null ? null : player.playerId);
            _panelHost = panelHost ?? throw new ArgumentNullException(nameof(panelHost));
            _characterVideo = host.GetComponent<LeaderboardCharacterVideoPresenter>() ??
                host.gameObject.AddComponent<LeaderboardCharacterVideoPresenter>();
            _characterVideo.Bind(root);
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
            _repository.Cancel();
            _characterVideo.Hide();
            if (_panelHost.OpenPanel == MainMenuPanelId.Leaderboard)
                _panelHost.TryClose(MainMenuPanelId.Leaderboard, _open);
            _bound = false;
        }

        private void Open()
        {
            if (_attemptPanel != null && _attemptPanel.resolvedStyle.display != DisplayStyle.None)
                return;
            if (!_panelHost.TryOpen(
                    MainMenuPanelId.Leaderboard,
                    _modal,
                    _open)) return;
            UpdatePowerCoins(_player ?? PlayerSessionStore.Instance?.Snapshot);
            _cohort.text = (_player?.profile?.gradeBand ?? "Your Grade") + " · " + _levelId.ToUpperInvariant();
            if (_cached != null) Render(_cached, "Showing saved standings · refreshing…");
            else
            {
                _list.Clear();
                _status.text = "Loading your grade leaderboard…";
            }
            Load();
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
            _status.text = "Refreshing…";
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
                        _status.text = "Leaderboard values are invalid.";
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
                        _status.text = "Showing saved standings · " + message;
                    }
                    SetSemanticState("is-error");
                });
        }

        private void IncludeAuthoritativeSelf(List<LeaderboardEntry> entries)
        {
            LeaderboardEntry self = CreateAuthoritativeSelfEntry(_player);
            if (self == null) return;
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
                WeaponLevel = weaponLevel
            };
        }

        private static CharacterPresentationCatalog s_characterCatalog;
        private static WeaponAscensionCatalogDefinition s_weaponCatalog;
        private static PetGachaCatalogDefinition s_petCatalog;

        private void Render(List<RankedLeaderboardEntry> entries, string status)
        {
            _status.text = status;
            _list.Clear();
            RankedLeaderboardEntry first = entries.FirstOrDefault();
            _throne.text = first == null ? "No standings yet" : first.Entry.DisplayName;
            _throneStage.text = first == null ? "--" : first.Entry.HighestStage.ToString(CultureInfo.CurrentCulture);
            _throneSilver.text = FormatNumber(first?.Entry.Silver ?? 0);
            _throneGold.text = FormatNumber(first?.Entry.Gold ?? 0);
            _throneDiamond.text = FormatNumber(first?.Entry.Diamond ?? 0);
            _characterVideo.Show(first?.Entry.CharacterId);
            RenderThronePet(first?.Entry?.PetId);

            RankedLeaderboardEntry self = entries.FirstOrDefault(value => value.IsSelf);
            RenderSelf(self);
            foreach (RankedLeaderboardEntry ranked in entries)
                _list.Add(CreateRow(ranked));
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
            if (self == null) return;
            AddCurrencyRows(_selfCurrencies, self.Entry);
            AddLoadoutSlots(_selfLoadout, self.Entry);
            RenderAvatar(_selfAvatarSlot, self.Entry);
        }

        private static VisualElement CreateRow(RankedLeaderboardEntry ranked)
        {
            var row = new VisualElement();
            row.AddToClassList("leaderboard-row");
            if (ranked.Rank == 1) row.AddToClassList("leaderboard-row--diamond");
            else if (ranked.Rank == 2) row.AddToClassList("leaderboard-row--gold");
            else if (ranked.Rank == 3) row.AddToClassList("leaderboard-row--silver");
            if (ranked.IsSelf) row.AddToClassList("leaderboard-row--self");
            row.name = ".leaderboard-row--rank-" + ranked.Rank;
            var rankSlot = CreateElement(".row-rank-slot", "leaderboard-row-rank-slot");
            if (ranked.Rank > 3)
                rankSlot.Add(CreateLabel("Rank Number", ranked.Rank.ToString(CultureInfo.CurrentCulture), "leaderboard-row-rank-number"));
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
            if (s_characterCatalog == null)
                s_characterCatalog = Resources.Load<CharacterPresentationCatalog>("CharacterPresentationCatalog");
            CharacterPresentationCatalog.Character def = s_characterCatalog?.Find(charId);
            Sprite authored = def?.profileIcon != null ? def.profileIcon : def?.selectionArt;
            return CharacterPlaceholderSprites.Resolve(authored, charId);
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
                        return pet.Icon;
                }
            }
            return null;
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
            VisualElement row = _list.Q<VisualElement>(className: "leaderboard-row--self");
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

        private static string FormatNumber(long value) =>
            Math.Max(0, value).ToString("N0", CultureInfo.CurrentCulture);

        private static string FormatCompact(long value)
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
