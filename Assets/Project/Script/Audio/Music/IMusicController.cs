namespace PowerMath.Audio
{
    /// <summary>
    /// Contract for controlling background music playback, seamless track transitions,
    /// boss battle channel separation, and volume ducking.
    /// </summary>
    public interface IMusicController
    {
        /// <summary>
        /// Plays the looping login/onboarding music on the theme channel.
        /// </summary>
        /// <param name="forceRestart">If true, rewinds the track to beginning even if already playing.</param>
        void PlayLoginMusic(bool forceRestart = false);

        /// <summary>
        /// Plays combat battle music corresponding to the specified biome (or default battle track).
        /// If already playing battle music for this biome, maintains continuous playback.
        /// If changing biomes, smoothly crossfades without audio pops.
        /// </summary>
        void PlayBattleMusic(string biomeId = null);

        /// <summary>
        /// Activates or deactivates the Big Boss battle music.
        /// When active, Boss track fades in on its dedicated channel while normal Battle music
        /// fades to silent without stopping or retracking.
        /// When deactivated, Boss track fades out and Battle music smoothly fades back in
        /// at its current timeline position.
        /// </summary>
        void SetBossBattleActive(bool isActive);

        /// <summary>
        /// Sets whether volume ducking is active (e.g. during Question Sequence and YouTube playback).
        /// Reduces combat music volume by -80% to -90% smoothly while allowing music to continue playing.
        /// </summary>
        void SetDucking(bool isDucked);

        /// <summary>
        /// Smoothly fades out and stops all playing music.
        /// </summary>
        /// <param name="fadeDuration">Custom fade duration in seconds, or negative for library default.</param>
        void StopMusic(float fadeDuration = -1f);

        float MasterVolume { get; set; }
        float MusicVolume { get; set; }
        bool IsMuted { get; set; }

        bool IsBossActive { get; }
        bool IsDucked { get; }
        string CurrentBattleBiomeId { get; }
    }
}
