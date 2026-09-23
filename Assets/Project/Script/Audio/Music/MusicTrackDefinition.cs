using System;
using UnityEngine;

namespace PowerMath.Audio
{
    /// <summary>
    /// Configuration for an individual music track entry.
    /// </summary>
    [Serializable]
    public sealed class MusicTrackConfig
    {
        [Tooltip("The audio clip to play. If unassigned and an addressable key is set, the clip will be loaded on demand.")]
        [SerializeField] private AudioClip clip;

        [Tooltip("Optional Addressable key or remote asset key for streaming on demand (e.g. 'OST_B2-Theme').")]
        [SerializeField] private string addressableKey;

        [Range(0f, 1f)]
        [Tooltip("Volume multiplier specific to this track.")]
        [SerializeField] private float volumeScale = 1f;

        [Tooltip("Whether the track should loop indefinitely.")]
        [SerializeField] private bool loop = true;

        public MusicTrackConfig()
        {
            volumeScale = 1f;
            loop = true;
        }

        public MusicTrackConfig(AudioClip clip, float volumeScale = 1f, bool loop = true)
        {
            this.clip = clip;
            this.volumeScale = Mathf.Clamp01(volumeScale);
            this.loop = loop;
        }

        public static MusicTrackConfig FromAddressable(string addressableKey, float volumeScale = 1f, bool loop = true)
        {
            var config = new MusicTrackConfig(null, volumeScale, loop);
            config.addressableKey = addressableKey;
            return config;
        }

        public AudioClip Clip => clip;
        public string AddressableKey => addressableKey;
        public bool HasAddressableKey => !string.IsNullOrEmpty(addressableKey);
        public float VolumeScale
        {
            get => volumeScale;
            set => volumeScale = Mathf.Clamp01(value);
        }
        public bool Loop => loop;

        public void SetClip(AudioClip newClip) => clip = newClip;
        public void SetAddressableKey(string key) => addressableKey = key;
    }

    /// <summary>
    /// Maps a specific Biome ID to its dedicated battle track.
    /// </summary>
    [Serializable]
    public sealed class BiomeMusicBinding
    {
        [Tooltip("Identifier matching BiomeData.Id (e.g. 'verdant-grove', 'magma-caverns').")]
        [SerializeField] private string biomeId;

        [Tooltip("Battle track configuration for this biome.")]
        [SerializeField] private MusicTrackConfig track = new MusicTrackConfig();

        public BiomeMusicBinding() { }

        public BiomeMusicBinding(string biomeId, MusicTrackConfig track)
        {
            this.biomeId = biomeId;
            this.track = track ?? new MusicTrackConfig();
        }

        public string BiomeId => biomeId;
        public MusicTrackConfig Track => track;
    }
}
