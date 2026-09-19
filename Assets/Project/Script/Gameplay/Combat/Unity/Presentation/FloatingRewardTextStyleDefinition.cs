using UnityEngine;
using TMPro;

namespace PowerMath.Gameplay.Combat.Unity
{
    [CreateAssetMenu(
        fileName = "FloatingRewardTextStyle",
        menuName = "PowerMath/Combat/Floating Reward Text Style")]
    public sealed class FloatingRewardTextStyleDefinition : ScriptableObject
    {
        [Header("Pool")]
        [Tooltip("Starting pool capacity for reward floaters.")]
        [SerializeField, Min(1)] private int poolCapacity = 8;

        [Header("Text Typography")]
        [Tooltip("TMP font asset. Defaults to FDT-Font if empty.")]
        [SerializeField] private TMP_FontAsset fontAsset;

        [Tooltip("Optional shared TMP material authored for outline/glow treatment.")]
        [SerializeField] private Material fontMaterial;

        [Tooltip("Base point size on the reusable TMP component.")]
        [SerializeField, Min(1f)] private float fontSize = 56f;

        [Header("Currency Colors")]
        [Tooltip("Color for Silver Rank currency gain.")]
        [SerializeField] private Color silverColor = new Color(0.76f, 0.82f, 0.89f, 1f);

        [Tooltip("Color for Gold Rank currency gain.")]
        [SerializeField] private Color goldColor = new Color(1f, 0.78f, 0.25f, 1f);

        [Tooltip("Color for Diamond Rank currency gain.")]
        [SerializeField] private Color diamondColor = new Color(0.41f, 0.87f, 0.95f, 1f);

        [Tooltip("Color for Power Coin currency gain (Flame Orange).")]
        [SerializeField] private Color powerCoinColor = new Color(1f, 0.55f, 0.05f, 1f);

        [Header("Currency Icons")]
        [Tooltip("Sprite icon for Silver Rank currency.")]
        [SerializeField] private Sprite silverIcon;

        [Tooltip("Sprite icon for Gold Rank currency.")]
        [SerializeField] private Sprite goldIcon;

        [Tooltip("Sprite icon for Diamond Rank currency.")]
        [SerializeField] private Sprite diamondIcon;

        [Tooltip("Sprite icon for Power Coin currency.")]
        [SerializeField] private Sprite powerCoinIcon;

        [Header("Celebratory Float Motion")]
        [Tooltip("Pop duration with overshoot bounce.")]
        [SerializeField, Min(0f)] private float popSeconds = 0.13f;

        [Tooltip("Initial spawn scale before pop.")]
        [SerializeField] private float startScale = 0.5f;

        [Tooltip("Horizontal separation offset for simultaneous spawns.")]
        [SerializeField] private float overlapOffset = 28f;

        [Header("Juice & Physical Dynamics")]
        [Tooltip("Burst arc peak height above spawn origin.")]
        [SerializeField, Min(0f)] private float burstHeight = 72f;

        [Tooltip("Editable horizontal travel across the reward's ballistic flight.")]
        [SerializeField, Min(0f)] private float fallHorizontalDistance = 150f;

        [Tooltip("Downward gravity drop distance from peak down to ground level.")]
        [SerializeField, Min(0f)] private float dropDistance = 104f;

        [Tooltip("Duration of the gravity drop and landing bounce.")]
        [SerializeField, Min(0f)] private float dropSeconds = 0.34f;

        [Tooltip("Brief contact hold before the ballistic launch begins.")]
        [SerializeField, Min(0f)] private float popHoldSeconds = 0.06f;

        [Tooltip("Distance beyond the overlay bottom before the text is pooled.")]
        [SerializeField, Min(0f)] private float offscreenPadding = 72f;

        [Tooltip("Initial rotational kick in degrees on explosive pop.")]
        [SerializeField, Range(0f, 25f)] private float rotationalKick = 9f;

        [Tooltip("Duration of pure white impact flash at spawn.")]
        [SerializeField, Min(0f)] private float flashSeconds = 0.08f;

        [Tooltip("Squash & stretch exaggeration factor (1.0 = standard, > 1.0 = more juicy).")]
        [SerializeField, Range(0.5f, 2f)] private float squashStretchFactor = 1.35f;

        [Tooltip("Random horizontal scatter offset applied on spawn.")]
        [SerializeField, Min(0f)] private float spawnJitterX = 28f;

        [Tooltip("Random vertical scatter offset applied on spawn.")]
        [SerializeField, Min(0f)] private float spawnJitterY = 18f;

        public int PoolCapacity => Mathf.Max(1, poolCapacity);
        public TMP_FontAsset FontAsset => fontAsset;
        public Material FontMaterial => fontMaterial;
        public float FontSize => Mathf.Max(1f, fontSize);

        public Color SilverColor => silverColor;
        public Color GoldColor => goldColor;
        public Color DiamondColor => diamondColor;
        public Color PowerCoinColor => powerCoinColor;

        public Sprite SilverIcon => silverIcon;
        public Sprite GoldIcon => goldIcon;
        public Sprite DiamondIcon => diamondIcon;
        public Sprite PowerCoinIcon => powerCoinIcon;

        public float PopSeconds => Mathf.Max(0f, popSeconds);
        public float StartScale => startScale;
        public float OverlapOffset => overlapOffset;

        public float BurstHeight => Mathf.Max(0f, burstHeight);
        public float FallHorizontalDistance => Mathf.Max(0f, fallHorizontalDistance);
        public float DropDistance => Mathf.Max(0f, dropDistance);
        public float DropSeconds => Mathf.Max(0f, dropSeconds);
        public float PopHoldSeconds => Mathf.Max(0f, popHoldSeconds);
        public float OffscreenPadding => Mathf.Max(0f, offscreenPadding);
        public float RotationalKick => rotationalKick;
        public float FlashSeconds => Mathf.Max(0f, flashSeconds);
        public float SquashStretchFactor => Mathf.Max(0.1f, squashStretchFactor);
        public float SpawnJitterX => Mathf.Max(0f, spawnJitterX);
        public float SpawnJitterY => Mathf.Max(0f, spawnJitterY);

        public Color GetColor(RewardCurrencyKind kind)
        {
            return kind switch
            {
                RewardCurrencyKind.RankSilver => silverColor,
                RewardCurrencyKind.RankGold => goldColor,
                RewardCurrencyKind.RankDiamond => diamondColor,
                RewardCurrencyKind.PowerCoin => powerCoinColor,
                _ => Color.white
            };
        }

        public Sprite GetIcon(RewardCurrencyKind kind)
        {
            return kind switch
            {
                RewardCurrencyKind.RankSilver => silverIcon,
                RewardCurrencyKind.RankGold => goldIcon,
                RewardCurrencyKind.RankDiamond => diamondIcon,
                RewardCurrencyKind.PowerCoin => powerCoinIcon,
                _ => null
            };
        }
    }
}
