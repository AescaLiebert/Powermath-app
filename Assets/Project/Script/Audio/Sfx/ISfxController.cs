namespace PowerMath.Audio
{
    /// <summary>
    /// Global interface for playing sound effects across Player, Enemy, Question Sequence, Battle, and UI states.
    /// </summary>
    public interface ISfxController
    {
        void PlayPlayer(PlayerSfxState state);
        void PlayEnemy(EnemySfxState state, string encounterKind = null, IEnemySfxProfile customProfile = null, bool isMajorDeath = false);
        void PlayQuestion(QuestionSequenceSfxState state);
        void PlayBattle(BattleSfxState state);
        void PlayCharacterSelection(CharacterSelectionSfxState state);
        void PlayUiStyle(string ussClassOrStyleKey, bool isClick = true);
        void PlayCue(SfxCueConfig cue, float volumeMultiplier = 1f);

        float MasterVolume { get; set; }
        float SfxVolume { get; set; }
        bool IsMuted { get; set; }
    }
}
