using System.Reflection;
using NUnit.Framework;
using PowerMath.Audio;
using UnityEngine;

namespace PowerMath.Tests.EditMode.Audio
{
    public sealed class MusicControllerTests
    {
        private GameObject _holder;
        private MusicController _controller;
        private MusicLibraryDefinition _library;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("TestMusicControllerHolder");
            _controller = _holder.AddComponent<MusicController>();
            _library = ScriptableObject.CreateInstance<MusicLibraryDefinition>();

            // Inject library
            var field = typeof(MusicController).GetField("library", BindingFlags.NonPublic | BindingFlags.Instance);
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
        public void MusicLibraryDefinition_FallbackClips_GenerateValidLoopingAudio()
        {
            AudioClip loginClip = _library.GetLoginClipWithFallback();
            AudioClip battleClip = _library.GetBattleClipWithFallback();
            AudioClip bossClip = _library.GetBossClipWithFallback();

            Assert.That(loginClip, Is.Not.Null);
            Assert.That(loginClip.length, Is.GreaterThan(0f));

            Assert.That(battleClip, Is.Not.Null);
            Assert.That(battleClip.length, Is.GreaterThan(0f));

            Assert.That(bossClip, Is.Not.Null);
            Assert.That(bossClip.length, Is.GreaterThan(0f));
        }

        [Test]
        public void MusicLibraryDefinition_ResolveBattleTrack_ResolvesBiomeOrFallback()
        {
            var customClip = AudioClip.Create("CustomBiomeClip", 1000, 1, 22050, false);
            var biomeBinding = new BiomeMusicBinding("magma-core", new MusicTrackConfig(customClip, 0.9f, true));

            var bindingsField = typeof(MusicLibraryDefinition).GetField("biomeBattleTracks", BindingFlags.NonPublic | BindingFlags.Instance);
            bindingsField?.SetValue(_library, new[] { biomeBinding });

            MusicTrackConfig resolved = _library.ResolveBattleTrack("magma-core");
            Assert.That(resolved.Clip, Is.SameAs(customClip));
            Assert.That(resolved.VolumeScale, Is.EqualTo(0.9f));

            MusicTrackConfig fallback = _library.ResolveBattleTrack("unknown-biome");
            Assert.That(fallback, Is.SameAs(_library.DefaultBattleMusic));
        }

        [Test]
        public void MusicController_PlayLoginMusic_SmoothlyFadesIn()
        {
            _controller.PlayLoginMusic();
            Assert.That(_controller.ThemeSource.clip, Is.Not.Null);

            // Initially volume is 0 before ticking
            Assert.That(_controller.ThemeSource.volume, Is.EqualTo(0f));

            // Tick for half of crossfade duration
            float halfDuration = _library.DefaultCrossfadeDuration * 0.5f;
            _controller.Tick(halfDuration);
            Assert.That(_controller.ThemeSource.volume, Is.GreaterThan(0.2f).And.LessThan(0.85f));

            // Tick past full duration
            _controller.Tick(halfDuration + 0.1f);
            Assert.That(_controller.ThemeSource.volume, Is.EqualTo(0.85f).Within(0.02f));
        }

        [Test]
        public void MusicController_BossInterruption_SeparatesChannelsWithoutRetracking()
        {
            // 1. Start battle music
            _controller.PlayBattleMusic("verdant-grove");
            _controller.Tick(_library.DefaultCrossfadeDuration + 0.1f);
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.5f));

            // 2. Simulate timeline elapsed
            _controller.BattleSource.time = 15.0f;

            // 3. Big Boss appears
            _controller.SetBossBattleActive(true);
            Assert.That(_controller.IsBossActive, Is.True);

            // Tick past boss interrupt duration
            _controller.Tick(_library.BossInterruptDuration + 0.1f);

            // Boss channel should be at full volume
            Assert.That(_controller.BossSource.volume, Is.GreaterThan(0.5f));

            // Battle channel should be silent (0 volume) but NOT stopped!
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(0f));
            // Timeline should not have reset to 0
            Assert.That(_controller.BattleSource.time, Is.EqualTo(15.0f).Within(0.1f));

            // 4. Boss defeated, returning to regular battle
            _controller.SetBossBattleActive(false);
            Assert.That(_controller.IsBossActive, Is.False);

            _controller.Tick(_library.BossInterruptDuration + 0.1f);

            // Boss fades to 0, Battle music returns to full volume
            Assert.That(_controller.BossSource.volume, Is.EqualTo(0f));
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.5f));
            Assert.That(_controller.BattleSource.time, Is.EqualTo(15.0f).Within(0.1f));
        }

        [Test]
        public void MusicController_SetDucking_ReducesCombatVolumeBy80To90Percent()
        {
            _controller.PlayBattleMusic();
            _controller.Tick(_library.DefaultCrossfadeDuration + 0.1f);
            float normalVolume = _controller.BattleSource.volume;
            Assert.That(normalVolume, Is.GreaterThan(0.5f));

            // Question sequence / YouTube appears
            _controller.SetDucking(true);
            Assert.That(_controller.IsDucked, Is.True);

            _controller.Tick(_library.DuckFadeDuration + 0.1f);

            // Volume should be ducked by ~85% (0.15 of normalVolume)
            float expectedDucked = normalVolume * _library.DuckVolumeFactor;
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(expectedDucked).Within(0.02f));
            Assert.That(_controller.CurrentDuckMultiplier, Is.EqualTo(_library.DuckVolumeFactor).Within(0.01f));

            // Question sequence window ends
            _controller.SetDucking(false);
            Assert.That(_controller.IsDucked, Is.False);

            _controller.Tick(_library.DuckFadeDuration + 0.1f);

            // Volume restored
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(normalVolume).Within(0.02f));
            Assert.That(_controller.CurrentDuckMultiplier, Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void MusicController_Mute_SilencesActiveChannels()
        {
            _controller.PlayBattleMusic();
            _controller.Tick(_library.DefaultCrossfadeDuration + 0.1f);
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.5f));

            _controller.IsMuted = true;
            _controller.Tick(0.01f);
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(0f));

            _controller.IsMuted = false;
            _controller.Tick(0.01f);
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.5f));
        }

        [Test]
        public void MusicController_LiveVolumeScaleUpdate_ImmediatelyReflectsInSourceVolume()
        {
            _controller.PlayBattleMusic();
            _controller.Tick(_library.DefaultCrossfadeDuration + 0.1f);
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.5f));

            // User changes VolumeScale in MusicLibrary inspector to 0 at runtime
            _library.DefaultBattleMusic.VolumeScale = 0f;
            _controller.Tick(0.01f);

            // Volume should immediately drop to 0 without restarting track
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(0f));

            // User adjusts VolumeScale to 0.35f
            _library.DefaultBattleMusic.VolumeScale = 0.35f;
            _controller.Tick(0.01f);
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(0.35f).Within(0.01f));
        }
    }
}
