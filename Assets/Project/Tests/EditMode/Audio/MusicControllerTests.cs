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

            // 2. Simulate timeline elapsed (within 2.5s fallback clip length)
            _controller.BattleSource.time = 1.5f;

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
            Assert.That(_controller.BattleSource.time, Is.EqualTo(1.5f).Within(0.1f));

            // 4. Boss defeated, returning to regular battle
            _controller.SetBossBattleActive(false);
            Assert.That(_controller.IsBossActive, Is.False);

            _controller.Tick(_library.BossInterruptDuration + 0.1f);

            // Boss fades to 0, Battle music returns to full volume
            Assert.That(_controller.BossSource.volume, Is.EqualTo(0f));
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.5f));
            Assert.That(_controller.BattleSource.time, Is.EqualTo(1.5f).Within(0.1f));
        }

        [Test]
        public void MusicController_EnemyOverride_ReplacesAndRestoresBattleMusic()
        {
            var enemyClip = AudioClip.Create("EnemyBattleMusic", 1000, 1, 22050, false);
            var enemyTrack = new MusicTrackConfig(enemyClip, 0.6f, true);

            _controller.PlayBattleMusic("verdant-grove");
            _controller.Tick(_library.DefaultCrossfadeDuration + 0.1f);

            _controller.SetEncounterMusicOverride(enemyTrack);
            Assert.That(_controller.IsEncounterOverrideActive, Is.True);
            Assert.That(_controller.IsBossActive, Is.False);
            Assert.That(_controller.BossSource.clip, Is.SameAs(enemyClip));

            _controller.Tick(_library.BossInterruptDuration + 0.1f);
            Assert.That(_controller.BossSource.volume, Is.EqualTo(0.6f).Within(0.02f));
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(0f));

            _controller.SetEncounterMusicOverride(null);
            _controller.Tick(_library.BossInterruptDuration + 0.1f);

            Assert.That(_controller.IsEncounterOverrideActive, Is.False);
            Assert.That(_controller.BossSource.volume, Is.EqualTo(0f));
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.5f));
        }

        [Test]
        public void MusicController_BossCustomOverride_TakesPriorityOverGlobalBossMusic()
        {
            var customBossClip = AudioClip.Create("CustomBossBattleMusic", 1000, 1, 22050, false);
            var customBossTrack = new MusicTrackConfig(customBossClip, 0.7f, true);

            _controller.PlayBattleMusic();
            _controller.SetEncounterMusicOverride(customBossTrack, isBossEncounter: true);

            Assert.That(_controller.IsBossActive, Is.True);
            Assert.That(_controller.IsEncounterOverrideActive, Is.True);
            Assert.That(_controller.BossSource.clip, Is.SameAs(customBossClip));

            _controller.Tick(_library.BossInterruptDuration + 0.1f);
            Assert.That(_controller.BossSource.volume, Is.EqualTo(0.7f).Within(0.02f));
        }

        [Test]
        public void MusicController_SetDucking_ReducesCombatVolumeToDuckedFactor()
        {
            _controller.PlayBattleMusic();
            _controller.Tick(_library.DefaultCrossfadeDuration + 0.1f);
            float normalVolume = _controller.BattleSource.volume;
            Assert.That(normalVolume, Is.GreaterThan(0.5f));

            // Question sequence / YouTube appears
            _controller.SetDucking(true);
            Assert.That(_controller.IsDucked, Is.True);

            _controller.Tick(_library.DuckFadeDuration + 0.1f);

            // Volume should be ducked by duckVolumeFactor
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
        public void MusicController_SetDucking_SlowsDownCombatMusicPitchToDuckPitchFactor()
        {
            _controller.PlayBattleMusic();
            _controller.Tick(_library.DefaultCrossfadeDuration + 0.1f);
            Assert.That(_controller.BattleSource.pitch, Is.EqualTo(1f));
            Assert.That(_controller.ThemeSource.pitch, Is.EqualTo(1f));

            // Question sequence begins -> music ducks and slows down by 95%
            _controller.SetDucking(true);
            _controller.Tick(_library.DuckFadeDuration + 0.1f);

            Assert.That(_controller.BattleSource.pitch, Is.EqualTo(_library.DuckPitchFactor).Within(0.01f));
            Assert.That(_controller.CurrentDuckPitchMultiplier, Is.EqualTo(_library.DuckPitchFactor).Within(0.01f));
            // Non-combat theme music channel remains unaffected at normal pitch (1.0)
            Assert.That(_controller.ThemeSource.pitch, Is.EqualTo(1f));

            // Question sequence ends -> pitch smoothly restores to 1.0
            _controller.SetDucking(false);
            _controller.Tick(_library.DuckFadeDuration + 0.1f);

            Assert.That(_controller.BattleSource.pitch, Is.EqualTo(1f).Within(0.01f));
            Assert.That(_controller.CurrentDuckPitchMultiplier, Is.EqualTo(1f).Within(0.01f));
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

        [Test]
        public void MusicController_FadeOutBossMusic_SmoothlyFadesBossChannelToZeroAndClearsState()
        {
            _controller.PlayBattleMusic();
            _controller.SetBossBattleActive(true);
            _controller.Tick(_library.BossInterruptDuration + 0.1f);
            Assert.That(_controller.BossSource.volume, Is.GreaterThan(0.5f));
            Assert.That(_controller.IsBossActive, Is.True);

            _controller.FadeOutBossMusic(1.0f);
            Assert.That(_controller.IsBossActive, Is.False);
            Assert.That(_controller.IsEncounterOverrideActive, Is.False);

            _controller.Tick(1.1f);
            Assert.That(_controller.BossSource.volume, Is.EqualTo(0f));
        }

        [Test]
        public void MusicController_PlayBattleMusic_BossToNewBiome_DifferentMusic_FadesUpSmoothly()
        {
            // Configure a second biome with a distinct clip
            AudioClip distinctClip = AudioClip.Create("DistinctBiomeClip", 44100, 1, 22050, false);
            var binding = new BiomeMusicBinding("crystal-caverns", new MusicTrackConfig(distinctClip, 0.9f, true));
            var tracksField = typeof(MusicLibraryDefinition).GetField("biomeBattleTracks", BindingFlags.NonPublic | BindingFlags.Instance);
            tracksField?.SetValue(_library, new[] { binding });

            _controller.PlayBattleMusic();
            _controller.SetBossBattleActive(true);
            _controller.Tick(_library.BossInterruptDuration + 0.1f);
            Assert.That(_controller.BossSource.volume, Is.GreaterThan(0.5f));

            // Boss dies -> boss music fades out
            _controller.FadeOutBossMusic(1.0f);
            _controller.Tick(1.1f);
            Assert.That(_controller.BossSource.volume, Is.EqualTo(0f));

            // Entering new biome with DIFFERENT music -> starts silent at 0 and smoothly fades up
            _controller.PlayBattleMusic("crystal-caverns");

            Assert.That(_controller.BattleSource.clip, Is.SameAs(distinctClip));
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(0f), "Different track after boss must start silently from volume 0");
            Assert.That(_controller.BattleSource.time, Is.EqualTo(0f), "Different track must start at 0");
            _controller.Tick(_library.DefaultCrossfadeDuration * 0.6f);
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.4f), "Different track fades up smoothly during transition");
        }

        [Test]
        public void MusicController_PlayBattleMusic_BossToNewBiome_SameMusic_DoesNotRestartPlayback()
        {
            _controller.PlayBattleMusic();
            _controller.BattleSource.time = 1.2f;

            _controller.SetBossBattleActive(true);
            _controller.Tick(_library.BossInterruptDuration + 0.1f);
            Assert.That(_controller.BossSource.volume, Is.GreaterThan(0.5f));

            // Boss dies -> boss music fades out
            _controller.FadeOutBossMusic(1.0f);
            _controller.Tick(1.1f);

            // Entering a biome with the SAME music track -> continuous playback must NOT restart
            _controller.PlayBattleMusic("same-track-biome");

            Assert.That(_controller.BattleSource.time, Is.EqualTo(1.2f).Within(0.001f), "Same music track must retain playback position and not restart");
        }

        [Test]
        public void MusicController_PlayBattleMusic_MidBiome_DoesNotRestartPlayback()
        {
            _controller.PlayBattleMusic("verdant-grove");
            _controller.BattleSource.time = 1.5f;

            // Mid-biome call with the same biome
            _controller.PlayBattleMusic("verdant-grove");

            Assert.That(_controller.BattleSource.time, Is.EqualTo(1.5f).Within(0.001f), "Mid-biome change must not restart music");
        }

        [Test]
        public void MusicController_PlayBattleMusic_SameTrack_DoesNotRestartPlayback()
        {
            _controller.PlayBattleMusic();
            _controller.BattleSource.time = 0.8f;

            // Repeated call for default track
            _controller.PlayBattleMusic();

            Assert.That(_controller.BattleSource.time, Is.EqualTo(0.8f).Within(0.001f), "Same music track must not restart itself");
        }

        [Test]
        public void MusicController_FadeOutBossMusic_LeavesBattleChannelSilentUntilBiomeTransition()
        {
            _controller.PlayBattleMusic("verdant-grove");
            _controller.Tick(_library.DefaultCrossfadeDuration + 0.1f);
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.5f));

            // Encounter override / boss activates -> battle channel fades to 0
            var enemyClip = AudioClip.Create("CustomEnemyClip", 1000, 1, 22050, false);
            _controller.SetEncounterMusicOverride(new MusicTrackConfig(enemyClip, 0.8f, true));
            _controller.Tick(_library.BossInterruptDuration + 0.1f);

            Assert.That(_controller.BossSource.volume, Is.EqualTo(0.8f).Within(0.02f));
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(0f));

            // Question sequence ducks volume during playback
            _controller.SetDucking(true);
            _controller.Tick(_library.DuckFadeDuration + 0.1f);
            Assert.That(_controller.BossSource.volume, Is.LessThan(0.8f));

            // Question resolved, enemy defeated -> ducking ends and boss music fades out
            _controller.SetDucking(false);
            _controller.FadeOutBossMusic(0.8f);
            _controller.Tick(0.8f + 0.1f);

            // Boss music must be faded to 0, and underlying battle music remains silent (must NOT play old biome music)
            Assert.That(_controller.BossSource.volume, Is.EqualTo(0f));
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(0f), "Underlying battle channel must remain silent after boss defeat until biome transition");

            // Biome transition triggers new biome music: should start from 0 and smoothly fade up
            _controller.PlayBattleMusic("azure-depths");
            Assert.That(_controller.BattleSource.volume, Is.EqualTo(0f), "New biome music must start silently from volume 0");
            _controller.Tick(_library.DefaultCrossfadeDuration * 0.5f);
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.1f).And.LessThan(0.9f), "New biome music must smoothly fade up during biome transition");
            _controller.Tick(_library.DefaultCrossfadeDuration * 0.6f);
            Assert.That(_controller.BattleSource.volume, Is.GreaterThan(0.5f), "New biome music reaches full volume after crossfade");
        }
    }
}
