#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace PowerMath.Diagnostics
{
    /// <summary>
    /// Development-only memory sampling for startup, scene transitions, and media playback.
    /// The component is created automatically in Development Players and never in release Players.
    /// </summary>
    public sealed class DevelopmentMemoryProfiler : MonoBehaviour
    {
        private const float DefaultSampleIntervalSeconds = 2f;
        private const int DefaultMaximumSamples = 2048;
        private const long PeakLogThresholdBytes = 4L * 1024L * 1024L;

        private static DevelopmentMemoryProfiler _instance;

        private readonly List<MemorySample> _samples = new();
        private ProfilerRecorder _totalUsedMemory;
        private ProfilerRecorder _systemUsedMemory;
        private ProfilerRecorder _gcReservedMemory;
        private ProfilerRecorder _videoUsedMemory;
        private float _nextSampleAt;
        private long _lastReportedPeak;
        private bool _reportSaved;

        [SerializeField] private float _sampleIntervalSeconds = DefaultSampleIntervalSeconds;
        [SerializeField] private int _maximumSamples = DefaultMaximumSamples;
        [SerializeField] private bool _writeReportFile = true;

        public static bool IsActive => _instance != null;

        /// <summary>
        /// WebGL report files are opt-in because writing to the browser's virtual
        /// filesystem can increase memory pressure during the very run being measured.
        /// </summary>
        public static bool AllowWebGlReportFileWrites { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Debug.isDebugBuild is true for Unity Development Builds. Avoid adding any
            // profiler object to a release WebGL player or to normal Editor play sessions.
            if (!Debug.isDebugBuild || Application.isEditor || _instance != null)
            {
                return;
            }

            var gameObject = new GameObject(nameof(DevelopmentMemoryProfiler));
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<DevelopmentMemoryProfiler>();
        }

        /// <summary>
        /// Records a named point without requiring a reference to this component.
        /// Calls are safe in release code because this method becomes a no-op there.
        /// </summary>
        public static void MarkCheckpoint(string checkpoint)
        {
            _instance?.Capture(string.IsNullOrWhiteSpace(checkpoint) ? "checkpoint" : checkpoint);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _sampleIntervalSeconds = Mathf.Max(0.25f, _sampleIntervalSeconds);
            _maximumSamples = Mathf.Clamp(_maximumSamples, 32, 8192);

            _totalUsedMemory = StartRecorder("Total Used Memory");
            _systemUsedMemory = StartRecorder("System Used Memory");
            _gcReservedMemory = StartRecorder("GC Reserved Memory");
            _videoUsedMemory = StartRecorder("Video Used Memory");

            SceneManager.sceneLoaded += OnSceneLoaded;
            Capture("player-ready");
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextSampleAt)
            {
                return;
            }

            _nextSampleAt = Time.unscaledTime + _sampleIntervalSeconds;
            Capture("interval");
        }

        private void OnApplicationPause(bool paused)
        {
            Capture(paused ? "application-paused" : "application-resumed");
        }

        private void OnApplicationQuit()
        {
            Capture("application-quit");
            SaveReport();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (!_reportSaved)
            {
                SaveReport();
            }

            DisposeRecorder(ref _totalUsedMemory);
            DisposeRecorder(ref _systemUsedMemory);
            DisposeRecorder(ref _gcReservedMemory);
            DisposeRecorder(ref _videoUsedMemory);

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Capture($"scene-loaded:{scene.name}");
        }

        private void Capture(string checkpoint)
        {
            var sample = new MemorySample
            {
                checkpoint = checkpoint,
                scene = SceneManager.GetActiveScene().name,
                elapsedSeconds = Time.realtimeSinceStartup,
                frame = Time.frameCount,
                unityUsedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                unityReservedBytes = Profiler.GetTotalReservedMemoryLong(),
                managedUsedBytes = Profiler.GetMonoUsedSizeLong(),
                managedHeapBytes = Profiler.GetMonoHeapSizeLong(),
                graphicsDriverBytes = ReadGraphicsDriverMemory(),
                totalUsedBytes = ReadRecorder(_totalUsedMemory),
                systemUsedBytes = ReadRecorder(_systemUsedMemory),
                gcReservedBytes = ReadRecorder(_gcReservedMemory),
                videoUsedBytes = ReadRecorder(_videoUsedMemory)
            };

            _samples.Add(sample);
            while (_samples.Count > _maximumSamples)
            {
                _samples.RemoveAt(0);
            }

            long comparablePeak = Math.Max(
                sample.unityUsedBytes,
                Math.Max(sample.systemUsedBytes, sample.totalUsedBytes));
            if (checkpoint != "interval" || comparablePeak >= _lastReportedPeak + PeakLogThresholdBytes)
            {
                _lastReportedPeak = Math.Max(_lastReportedPeak, comparablePeak);
                Debug.Log(
                    $"[MemoryProfile] {checkpoint} scene={sample.scene} " +
                    $"unity={ToMegabytes(sample.unityUsedBytes):F0}MB " +
                    $"system={ToMegabytes(sample.systemUsedBytes):F0}MB " +
                    $"gc={ToMegabytes(sample.gcReservedBytes):F0}MB " +
                    $"video={ToMegabytes(sample.videoUsedBytes):F0}MB");
            }
        }

        private void SaveReport()
        {
            if (_reportSaved || !_writeReportFile || _samples.Count == 0)
            {
                return;
            }

            if (Application.platform == RuntimePlatform.WebGLPlayer && !AllowWebGlReportFileWrites)
            {
                return;
            }

            _reportSaved = true;

            try
            {
                var report = new MemoryProfileReport
                {
                    capturedUtc = DateTime.UtcNow.ToString("O"),
                    applicationVersion = Application.version,
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    deviceModel = SystemInfo.deviceModel,
                    operatingSystem = SystemInfo.operatingSystem,
                    reportedSystemMemoryMegabytes = SystemInfo.systemMemorySize,
                    reportedGraphicsMemoryMegabytes = SystemInfo.graphicsMemorySize,
                    samples = _samples.ToArray()
                };

                string fileName = $"PowerMathMemoryProfile-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                string path = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
                Debug.Log($"[MemoryProfile] Report written to {path}");
            }
            catch (Exception exception)
            {
                // A browser may not expose a writable persistent filesystem. Console
                // samples remain useful and avoid making profiling itself a crash risk.
                Debug.LogWarning($"[MemoryProfile] Could not write report: {exception.Message}");
            }
        }

        private static ProfilerRecorder StartRecorder(string counterName)
        {
            try
            {
                return ProfilerRecorder.StartNew(ProfilerCategory.Memory, counterName, 1);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MemoryProfile] Counter unavailable ({counterName}): {exception.Message}");
                return default;
            }
        }

        private static long ReadRecorder(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : 0L;
        }

        private static long ReadGraphicsDriverMemory()
        {
            try
            {
                return Profiler.GetAllocatedMemoryForGraphicsDriver();
            }
            catch
            {
                return 0L;
            }
        }

        private static void DisposeRecorder(ref ProfilerRecorder recorder)
        {
            if (recorder.Valid)
            {
                recorder.Dispose();
            }

            recorder = default;
        }

        private static float ToMegabytes(long bytes)
        {
            return bytes <= 0 ? 0f : bytes / (1024f * 1024f);
        }

        [Serializable]
        private sealed class MemoryProfileReport
        {
            public string capturedUtc;
            public string applicationVersion;
            public string unityVersion;
            public string platform;
            public string deviceModel;
            public string operatingSystem;
            public int reportedSystemMemoryMegabytes;
            public int reportedGraphicsMemoryMegabytes;
            public MemorySample[] samples;
        }

        [Serializable]
        private sealed class MemorySample
        {
            public string checkpoint;
            public string scene;
            public float elapsedSeconds;
            public int frame;
            public long unityUsedBytes;
            public long unityReservedBytes;
            public long managedUsedBytes;
            public long managedHeapBytes;
            public long graphicsDriverBytes;
            public long totalUsedBytes;
            public long systemUsedBytes;
            public long gcReservedBytes;
            public long videoUsedBytes;
        }
    }
}
#else
namespace PowerMath.Diagnostics
{
    /// <summary>
    /// Release-player stub so production code can leave checkpoint calls compiled out
    /// without adding profiling code or allocations to the shipped player.
    /// </summary>
    public static class DevelopmentMemoryProfiler
    {
        public static bool IsActive => false;
        public static bool AllowWebGlReportFileWrites { get; set; }

        public static void MarkCheckpoint(string checkpoint)
        {
        }
    }
}
#endif
