#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace PowerMath.Editor.Tools
{
    internal static class WebGLCloudflarePostprocessor
    {
        private static readonly string[] RequiredPagesFiles =
        {
            "version.json",
            "_headers"
        };

        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string outputPath)
        {
            if (target != BuildTarget.WebGL)
            {
                return;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string cloudflarePublic = Path.Combine(projectRoot, "Cloudflare", "public");

            foreach (string fileName in RequiredPagesFiles)
            {
                string source = Path.Combine(cloudflarePublic, fileName);
                string destination = Path.Combine(outputPath, fileName);

                if (!File.Exists(source))
                {
                    throw new BuildFailedException(
                        $"Required Cloudflare deployment file is missing: {source}");
                }

                File.Copy(source, destination, true);
            }

            string templateData = Path.Combine(outputPath, "TemplateData");
            string faviconSource = Path.Combine(templateData, "favicon.ico");
            if (File.Exists(faviconSource))
            {
                File.Copy(faviconSource, Path.Combine(outputPath, "favicon.ico"), true);
            }

            string touchIconSource = Path.Combine(templateData, "icons", "mathworld-180.png");
            if (File.Exists(touchIconSource))
            {
                File.Copy(touchIconSource, Path.Combine(outputPath, "apple-touch-icon.png"), true);
                File.Copy(touchIconSource, Path.Combine(outputPath, "apple-touch-icon-precomposed.png"), true);
            }

            Debug.Log(
                $"[WebGLCloudflarePostprocessor] Added deployment files and root fallback icons to {outputPath}");
        }
    }
}
#endif
