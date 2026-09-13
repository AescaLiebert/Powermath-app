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
        /// If already playing battle music for this biome or same track, maintains continuous playback without restart.
        /// If changing biomes to a different track, smoothly crossfades without audio pops.
        /// If transitioning from a boss to a new biome with different music, suddenly kicks in at full volume.
        /// </summary>
        void PlayBattleMusic(string biomeId = null, bool suddenKickIn = false, bool forceRestart = false);

        /// <summary>
        /// Clears runtime battle track history and stops battle/boss playback so a new run or stage 1 restart begins fresh.
        /// </summary>
        void ResetBattleState();

        /// <summary>
        /// Overrides biome/default battle music for the current enemy encounter.
        /// Boss encounters without a custom track use the global boss battle track.
        /// Passing no custom track for a non-boss restores the underlying battle music.
        /// </summary>
        void SetEncounterMusicOverride(MusicTrackConfig customTrack, bool isBossEncounter = false);

        /// <summary>
        /// Activates or deactivates the Big Boss battle music.
        /// When active, Boss track fades in on its dedicated channel while normal Battle music
        /// fades to silent without stopping or retracking.
        /// When deactivated, Boss track fades out and Battle music smoothly fades back in
        /// at its current timeline position.
        /// </summary>
        void SetBossBattleActive(bool isActive);

        /// <summary>
        /// Fades out the Big Boss or custom encounter music to silence when a boss is defeated.
        /// </summary>
        void FadeOutBossMusic(float fadeDuration = 1.2f);

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

        /// <summary>
        /// Ensures active music channels resume playback if interrupted by external overlays (e.g. YouTube iframe).
        /// </summary>
        void EnsurePlayback();

        float MasterVolume { get; set; }
        float MusicVolume { get; set; }
        bool IsMuted { get; set; }

        bool IsBossActive { get; }
        bool IsEncounterOverrideActive { get; }
        bool IsDucked { get; }
        string CurrentBattleBiomeId { get; }
    }
}
