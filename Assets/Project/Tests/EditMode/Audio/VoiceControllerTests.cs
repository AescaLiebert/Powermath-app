using System.Reflection;
using NUnit.Framework;
using PowerMath.Audio;
using UnityEngine;

namespace PowerMath.Tests.EditMode.Audio
{
    public sealed class VoiceControllerTests
    {
        private GameObject _holder;
        private VoiceController _controller;

        [SetUp]
        public void SetUp()
        {
            var instanceField = typeof(VoiceController).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var existing = instanceField?.GetValue(null) as VoiceController;
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
            instanceField?.SetValue(null, null);

            _holder = new GameObject("TestVoiceControllerHolder");
            _controller = _holder.AddComponent<VoiceController>();
            _controller.InitializeSources();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
            var instanceField = typeof(VoiceController).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);
        }

        [Test]
        public void VoiceController_Initialization_CreatesDualChannelsWith2DSettings()
        {
            var sourceAField = typeof(VoiceController).GetField("_sourceA", BindingFlags.NonPublic | BindingFlags.Instance);
            var sourceBField = typeof(VoiceController).GetField("_sourceB", BindingFlags.NonPublic | BindingFlags.Instance);

            var sourceA = sourceAField?.GetValue(_controller) as AudioSource;
            var sourceB = sourceBField?.GetValue(_controller) as AudioSource;

            Assert.That(sourceA, Is.Not.Null, "Channel A must be created on initialization.");
            Assert.That(sourceB, Is.Not.Null, "Channel B must be created on initialization.");
            Assert.That(sourceA.spatialBlend, Is.EqualTo(0f), "Channel A must be 2D.");
            Assert.That(sourceB.spatialBlend, Is.EqualTo(0f), "Channel B must be 2D.");
            Assert.That(sourceA.loop, Is.False, "Channel A must not loop.");
            Assert.That(sourceB.loop, Is.False, "Channel B must not loop.");
        }

        [Test]
        public void VoiceController_VolumeProperties_ClampBetween0And1()
        {
            _controller.MasterVolume = 1.5f;
            Assert.That(_controller.MasterVolume, Is.EqualTo(1f));

            _controller.MasterVolume = -0.5f;
            Assert.That(_controller.MasterVolume, Is.EqualTo(0f));

            _controller.VoiceVolume = 2.0f;
            Assert.That(_controller.VoiceVolume, Is.EqualTo(1f));

            _controller.VoiceVolume = -1.0f;
            Assert.That(_controller.VoiceVolume, Is.EqualTo(0f));
        }

        [Test]
        public void VoiceController_PlayVoice_PlaysClipOnActiveSource()
        {
            var clip = AudioClip.Create("TestVoiceClip", 100, 1, 22050, false);
            _controller.PlayVoice(clip, 0.75f, 1.2f);

            var activeSourceField = typeof(VoiceController).GetField("_activeSource", BindingFlags.NonPublic | BindingFlags.Instance);
            var activeSource = activeSourceField?.GetValue(_controller) as AudioSource;

            Assert.That(activeSource, Is.Not.Null);
            Assert.That(activeSource.clip, Is.EqualTo(clip));
            Assert.That(activeSource.pitch, Is.EqualTo(1.2f).Within(0.01f));
        }

        [Test]
        public void VoiceController_IsMuted_PreventsVoicePlayback()
        {
            var clip = AudioClip.Create("TestVoiceMuted", 100, 1, 22050, false);
            _controller.IsMuted = true;
            _controller.PlayVoice(clip);

            var activeSourceField = typeof(VoiceController).GetField("_activeSource", BindingFlags.NonPublic | BindingFlags.Instance);
            var activeSource = activeSourceField?.GetValue(_controller) as AudioSource;

            Assert.That(activeSource, Is.Null, "Voice playback should not occur when muted.");
        }

        [Test]
        public void VoiceController_AdaptiveInterruption_AlternatesSources()
        {
            var clip1 = AudioClip.Create("TestVoice1", 100, 1, 22050, false);
            var clip2 = AudioClip.Create("TestVoice2", 100, 1, 22050, false);

            var sourceAField = typeof(VoiceController).GetField("_sourceA", BindingFlags.NonPublic | BindingFlags.Instance);
            var sourceBField = typeof(VoiceController).GetField("_sourceB", BindingFlags.NonPublic | BindingFlags.Instance);
            var activeSourceField = typeof(VoiceController).GetField("_activeSource", BindingFlags.NonPublic | BindingFlags.Instance);

            var sourceA = sourceAField?.GetValue(_controller) as AudioSource;
            var sourceB = sourceBField?.GetValue(_controller) as AudioSource;

            _controller.PlayVoice(clip1);
            var activeAfter1 = activeSourceField?.GetValue(_controller) as AudioSource;
            Assert.That(activeAfter1, Is.EqualTo(sourceA), "First playback should occupy Channel A.");

            _controller.PlayVoice(clip2);
            var activeAfter2 = activeSourceField?.GetValue(_controller) as AudioSource;
            Assert.That(activeAfter2, Is.EqualTo(sourceB), "Interrupting playback should occupy Channel B.");
        }

        [Test]
        public void VoiceController_PlayVoiceCue_GeneratesProceduralFallbackForEmotions()
        {
            string[] emotions = { "happy", "curious", "excited", "proud", "thoughtful", "encouraging", "neutral", "unknown" };

            foreach (var emotion in emotions)
            {
                _controller.PlayVoiceCue(null, "Power", emotion);

                var activeSourceField = typeof(VoiceController).GetField("_activeSource", BindingFlags.NonPublic | BindingFlags.Instance);
                var activeSource = activeSourceField?.GetValue(_controller) as AudioSource;

                Assert.That(activeSource, Is.Not.Null, $"Active source should be assigned for emotion '{emotion}'.");
                Assert.That(activeSource.clip, Is.Not.Null, $"Fallback clip should be generated for emotion '{emotion}'.");
                Assert.That(activeSource.clip.length, Is.GreaterThan(0f), $"Generated clip for '{emotion}' should have positive duration.");
            }
        }

        [Test]
        public void VoiceController_StopVoice_Immediate_StopsPlayback()
        {
            var clip = AudioClip.Create("TestVoiceStop", 100, 1, 22050, false);
            _controller.PlayVoice(clip);

            var activeSourceField = typeof(VoiceController).GetField("_activeSource", BindingFlags.NonPublic | BindingFlags.Instance);
            var activeSource = activeSourceField?.GetValue(_controller) as AudioSource;

            Assert.That(activeSource, Is.Not.Null);

            _controller.StopVoice(immediate: true);
            Assert.That(activeSource.isPlaying, Is.False);
        }

        [Test]
        public void VoiceController_NullClip_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _controller.PlayVoice(null));
        }
    }
}
