using UnityEngine;

namespace PowerMath.Audio
{
    /// <summary>
    /// Dedicated audio controller for dialogue voice lines, character speech, and narration.
    /// Provides independent channel separation from Music and SFX, plus adaptive interruption
    /// so consecutive voice lines transition smoothly without audio pops or overlapping clutter.
    /// </summary>
    public interface IVoiceController
    {
        /// <summary>
        /// Plays a direct audio clip on the dedicated voice channel.
        /// If a voice is already active, it is adaptively interrupted (faded out smoothly).
        /// </summary>
        void PlayVoice(AudioClip clip, float volumeScale = 1f, float pitch = 1f);

        /// <summary>
        /// Plays a voice line identified by cue ID, speaker, or emotion.
        /// Falls back to an emotion-tailored procedural vocalization if no recorded asset is found.
        /// </summary>
        void PlayVoiceCue(string cueId, string speakerKey = null, string emotionId = null, float volumeScale = 1f);

        /// <summary>
        /// Stops the currently playing voice.
        /// </summary>
        /// <param name="immediate">If true, cuts off immediately; if false, fades out smoothly (e.g. 60-80ms).</param>
        void StopVoice(bool immediate = false);

        /// <summary>
        /// True if voice audio is actively playing or fading.
        /// </summary>
        bool IsSpeaking { get; }

        float VoiceVolume { get; set; }
        float MasterVolume { get; set; }
        bool IsMuted { get; set; }
    }
}
