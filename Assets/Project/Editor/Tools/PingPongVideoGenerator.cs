using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;

namespace PowerMath.Editor.Tools
{
    public static class PingPongVideoGenerator
    {
        [MenuItem("Assets/PowerMath/Generate Ping-Pong (Reverse) Video Clip", true)]
        private static bool ValidateGenerateReverseVideoClip()
        {
            if (Selection.activeObject == null) return false;
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path)) return false;

            return Selection.activeObject is VideoClip ||
                   path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".mov", StringComparison.OrdinalIgnoreCase);
        }

        [MenuItem("Assets/PowerMath/Generate Ping-Pong (Reverse) Video Clip", false, 200)]
        public static void GenerateReverseVideoClip()
        {
            string inputPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(inputPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid VideoClip or video file.", "OK");
                return;
            }

            string fullInputPath = Path.GetFullPath(inputPath);
            string dir = Path.GetDirectoryName(fullInputPath);
            string filenameWithoutExt = Path.GetFileNameWithoutExtension(fullInputPath);
            string ext = Path.GetExtension(fullInputPath);

            string outputFilename = $"{filenameWithoutExt}_Reverse{ext}";
            string fullOutputPath = Path.Combine(dir, outputFilename);
            string relativeOutputPath = Path.Combine(Path.GetDirectoryName(inputPath), outputFilename).Replace("\\", "/");

            string ffmpegPath = FindFfmpegExecutable();
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                EditorUtility.DisplayDialog("FFmpeg Not Found",
                    "Could not locate an FFmpeg executable on your system.\n\n" +
                    "To generate reverse clips automatically, please ensure ffmpeg is in your PATH or install imageio-ffmpeg via pip:\n" +
                    "python -m pip install imageio-ffmpeg", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("PowerMath Video Tool", $"Generating reversed clip: {outputFilename}...", 0.3f);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-y -i \"{fullInputPath}\" -vf reverse -an -c:v libx264 -crf 18 -preset fast -pix_fmt yuv420p \"{fullOutputPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Error", "Failed to start FFmpeg process.", "OK");
                        return;
                    }

                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    EditorUtility.ClearProgressBar();

                    if (process.ExitCode == 0 && File.Exists(fullOutputPath))
                    {
                        AssetDatabase.ImportAsset(relativeOutputPath, ImportAssetOptions.ForceUpdate);
                        var newClip = AssetDatabase.LoadAssetAtPath<VideoClip>(relativeOutputPath);
                        if (newClip != null)
                        {
                            EditorGUIUtility.PingObject(newClip);
                            Selection.activeObject = newClip;
                        }

                        EditorUtility.DisplayDialog("Success",
                            $"Reverse video clip generated successfully!\n\nFile: {relativeOutputPath}\n\nYou can now assign this clip to your PingPongVideoPlayer's 'Reverse Clip' slot.", "OK");
                        Debug.Log($"[PingPongVideoGenerator] Created reverse video clip: {relativeOutputPath}");
                    }
                    else
                    {
                        Debug.LogError($"[PingPongVideoGenerator] FFmpeg failed with exit code {process.ExitCode}:\n{errorOutput}");
                        EditorUtility.DisplayDialog("Generation Failed", $"FFmpeg encountered an error:\n\n{errorOutput}", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Error", $"An unexpected error occurred: {ex.Message}", "OK");
            }
        }

        private static string FindFfmpegExecutable()
        {
            // 1. Check system PATH
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = "ffmpeg",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi))
                {
                    if (p != null)
                    {
                        string line = p.StandardOutput.ReadLine();
                        p.WaitForExit();
                        if (!string.IsNullOrEmpty(line) && File.Exists(line.Trim()))
                        {
                            return line.Trim();
                        }
                    }
                }
            }
            catch { }

            // 2. Query python for imageio_ffmpeg
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "-c \"import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi))
                {
                    if (p != null)
                    {
                        string line = p.StandardOutput.ReadLine();
                        p.WaitForExit();
                        if (!string.IsNullOrEmpty(line) && File.Exists(line.Trim()))
                        {
                            return line.Trim();
                        }
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
