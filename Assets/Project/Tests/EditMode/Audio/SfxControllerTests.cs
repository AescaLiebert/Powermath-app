using System.Reflection;
using NUnit.Framework;
using PowerMath.Audio;
using UnityEngine;

namespace PowerMath.Tests.EditMode.Audio
{
    public sealed class SfxControllerTests
    {
        private GameObject _holder;
        private SfxController _controller;
        private SfxLibraryDefinition _library;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("TestSfxControllerHolder");
            _controller = _holder.AddComponent<SfxController>();
            _library = ScriptableObject.CreateInstance<SfxLibraryDefinition>();

            var field = typeof(SfxController).GetField("library", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(_controller, _library);
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
            if (_library != null)
            {
                Object.DestroyImmediate(_library);
            }
        }

        [Test]
        public void SfxLibraryDefinition_GeneratesProceduralFallbacks_ForAllCues()
        {
            string[] cueKeys = {
                "swing", "fail", "hit", "crit", "hurt",
                "enemy_appear", "enemy_attack", "enemy_hurt", "die_normal", "die_major",
                "popup", "click", "tick", "success", "failure", "multiplier", "biome_transition",
                "auth_click", "auth_success", "auth_failure", "auth_language",
                "reward_drop", "reward_magnet", "currency_rank", "currency_coin"
            };

            for (int i = 0; i < cueKeys.Length; i++)
            {
                AudioClip clip = _library.GetFallbackClip(cueKeys[i]);
                Assert.That(clip, Is.Not.Null, $"Fallback clip for '{cueKeys[i]}' should not be null.");
                Assert.That(clip.length, Is.GreaterThan(0f), $"Fallback clip for '{cueKeys[i]}' should have positive length.");
            }
        }

        [Test]
        public void SfxCueConfig_PicksNonNullVariant_AndResolvesPitchRange()
        {
            var clipA = AudioClip.Create("TestA", 100, 1, 22050, false);
            var clipB = AudioClip.Create("TestB", 100, 1, 22050, false);

            var cue = new SfxCueConfig(new[] { clipA, clipB }, 0.85f, 0.95f, 1.05f);

            Assert.That(cue.HasClip(), Is.True);
            Assert.That(cue.VolumeScale, Is.EqualTo(0.85f).Within(0.01f));

            AudioClip picked = cue.PickClip();
            Assert.That(picked == clipA || picked == clipB, Is.True);

            float pitch = cue.ResolvePitch();
            Assert.That(pitch, Is.GreaterThanOrEqualTo(0.95f).And.LessThanOrEqualTo(1.05f));
        }

        [Test]
        public void SfxController_PlayPlayer_DoesNotThrow_AndPlaysExpectedCues()
        {
            Assert.DoesNotThrow(() => _controller.PlayPlayer(PlayerSfxState.AttackSwing));
            Assert.DoesNotThrow(() => _controller.PlayPlayer(PlayerSfxState.AttackFail));
            Assert.DoesNotThrow(() => _controller.PlayPlayer(PlayerSfxState.Hit));
            Assert.DoesNotThrow(() => _controller.PlayPlayer(PlayerSfxState.CriticalHit));
            Assert.DoesNotThrow(() => _controller.PlayPlayer(PlayerSfxState.Hurt));
            Assert.DoesNotThrow(() => _controller.PlayPlayer(PlayerSfxState.Die));
        }

        [Test]
        public void SfxController_PlayEnemy_RespectsCustomProfileAndEncounterKind()
        {
            var customClip = AudioClip.Create("CustomEnemyDie", 100, 1, 22050, false);
            var customProfile = new DummyEnemySfxProfile(new SfxCueConfig(customClip, 0.9f));

            Assert.DoesNotThrow(() => _controller.PlayEnemy(EnemySfxState.Appear, "NormalMonster"));
            Assert.DoesNotThrow(() => _controller.PlayEnemy(EnemySfxState.Attack, "BigBoss"));
            Assert.DoesNotThrow(() => _controller.PlayEnemy(EnemySfxState.Die, "BigBoss", customProfile));

            var binding = new EncounterKindSfxBinding("Big Boss", new SfxCueConfig(customClip), null, null, null);
            var bindingsField = typeof(SfxLibraryDefinition).GetField("encounterKindBindings", BindingFlags.NonPublic | BindingFlags.Instance);
            bindingsField?.SetValue(_library, new[] { binding });

            Assert.That(_library.FindEncounterBinding("BigBoss"), Is.Not.Null);
            Assert.That(_library.FindEncounterBinding("Big Boss"), Is.Not.Null);
            Assert.That(_library.FindEncounterBinding("big_boss"), Is.Not.Null);
        }

        [Test]
        public void SfxController_PlayQuestion_PlaysStateCuesWithoutThrowing()
        {
            Assert.DoesNotThrow(() => _controller.PlayQuestion(QuestionSequenceSfxState.PopUp));
            Assert.DoesNotThrow(() => _controller.PlayQuestion(QuestionSequenceSfxState.KeypadTap));
            Assert.DoesNotThrow(() => _controller.PlayQuestion(QuestionSequenceSfxState.CountdownTick));
            Assert.DoesNotThrow(() => _controller.PlayQuestion(QuestionSequenceSfxState.ResultSuccess));
            Assert.DoesNotThrow(() => _controller.PlayQuestion(QuestionSequenceSfxState.ResultFail));
            Assert.DoesNotThrow(() => _controller.PlayQuestion(QuestionSequenceSfxState.DamageMultiplying));
        }

        [Test]
        public void SfxController_PlayBattleAndCharacterSelection_PlaysCuesWithoutThrowing()
        {
            Assert.DoesNotThrow(() => _controller.PlayBattle(BattleSfxState.BiomeTransition));
            Assert.DoesNotThrow(() => _controller.PlayBattle(BattleSfxState.StageAdvance));
            Assert.DoesNotThrow(() => _controller.PlayBattle(BattleSfxState.ActorClick));

            Assert.DoesNotThrow(() => _controller.PlayCharacterSelection(CharacterSelectionSfxState.CardHover));
            Assert.DoesNotThrow(() => _controller.PlayCharacterSelection(CharacterSelectionSfxState.CardClick));
            Assert.DoesNotThrow(() => _controller.PlayCharacterSelection(CharacterSelectionSfxState.CharacterAccepted));
        }

        [Test]
        public void SfxController_PlayAuthentication_PlaysExpectedCues()
        {
            Assert.DoesNotThrow(() => _controller.PlayAuthentication(AuthenticationSfxState.LoginClick));
            Assert.DoesNotThrow(() => _controller.PlayAuthentication(AuthenticationSfxState.Success));
            Assert.DoesNotThrow(() => _controller.PlayAuthentication(AuthenticationSfxState.Failure));
            Assert.DoesNotThrow(() => _controller.PlayAuthentication(AuthenticationSfxState.LanguageSwitch));
        }

        [Test]
        public void SfxController_PlayReward_PlaysExpectedCues()
        {
            Assert.DoesNotThrow(() => _controller.PlayReward(RewardSfxState.OnDrop));
            Assert.DoesNotThrow(() => _controller.PlayReward(RewardSfxState.Magnetism));
            Assert.DoesNotThrow(() => _controller.PlayReward(RewardSfxState.CurrencyRank));
            Assert.DoesNotThrow(() => _controller.PlayReward(RewardSfxState.CurrencyPowerCoin));
        }

        [Test]
        public void SfxController_UiStyles_ResolvesConfiguredStyles()
        {
            var customClick = AudioClip.Create("BtnClickClip", 100, 1, 22050, false);
            var style = new UiAnimationSfxStyle("btn-primary", null, new SfxCueConfig(customClick, 0.7f));

            var stylesField = typeof(SfxLibraryDefinition).GetField("uiStyles", BindingFlags.NonPublic | BindingFlags.Instance);
            stylesField?.SetValue(_library, new[] { style });

            UiAnimationSfxStyle resolved = _library.FindUiStyle("btn-primary");
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved.ClickSfx.HasClip(), Is.True);
            Assert.That(_library.FindUiStyle(".btn-primary"), Is.Not.Null);

            Assert.DoesNotThrow(() => _controller.PlayUiStyle("btn-primary", isClick: true));
            Assert.DoesNotThrow(() => _controller.PlayUiStyle("btn-primary", isClick: false));
            Assert.DoesNotThrow(() => _controller.PlayUiStyle("unknown-style", isClick: true));
            Assert.DoesNotThrow(() => _controller.PlayPanelOpen());
            Assert.DoesNotThrow(() => _controller.PlayPanelClose());
        }

        [Test]
        public void UiSfxAudioBinder_PrioritizesSpecificCustomStyles_OverDefaultStyles()
        {
            var root = new UnityEngine.UIElements.VisualElement();
            var btn = new UnityEngine.UIElements.Button();
            btn.AddToClassList("hud-player-menu-button");
            btn.AddToClassList("hud-player-menu-button--hub");
            root.Add(btn);

            var hubStyle = new UiAnimationSfxStyle("hud-player-menu-button--hub", null, new SfxCueConfig(AudioClip.Create("HubClick", 10, 1, 22050, false)));
            var stylesField = typeof(SfxLibraryDefinition).GetField("uiStyles", BindingFlags.NonPublic | BindingFlags.Instance);
            stylesField?.SetValue(_library, new[] { hubStyle });

            Assert.DoesNotThrow(() => UiSfxAudioBinder.Bind(root, _library));
            Assert.That(btn.ClassListContains("sfx-audio-bound"), Is.True);
        }

        private sealed class DummyEnemySfxProfile : IEnemySfxProfile
        {
            public SfxCueConfig AppearSfx => null;
            public SfxCueConfig HurtSfx => null;
            public SfxCueConfig AttackSfx => null;
            public SfxCueConfig DieSfx { get; }

            public DummyEnemySfxProfile(SfxCueConfig die)
            {
                DieSfx = die;
            }
        }
    }
}
