using System.IO;
using UnityEngine;

namespace GameWarriors.VendorDomian.VendorEditor
{
    public static class BazaarBuildTools
    {
        public static void SetupMainTemplateForBazaar(bool useBazaar, string poolakeyVersion = "2.0.0", string kotlinVersion = "1.4.20")
        {
            VendorBuildTools.SetEnabled("Packages/com.gamewarriors.bazaar/Plugins/poolakey-release.aar", useBazaar);
            VendorBuildTools.SetEnabled("Assets/Plugins/Android/poolakey-release.aar", useBazaar);

            string mainGradle = Path.Combine(Application.dataPath, "Plugins/Android/mainTemplate.gradle");
            if (!File.Exists(mainGradle))
            {
                Debug.LogWarning("[IABTool] mainTemplate.gradle not found at: " + mainGradle);
                return;
            }
            var lines = new System.Collections.Generic.List<string>(File.ReadAllLines(mainGradle));
            lines.RemoveAll(l =>
                l.Contains($"com.github.cafebazaar.Poolakey:poolakey:{poolakeyVersion}") ||
                l.Contains($"org.jetbrains.kotlin:kotlin-stdlib-jdk7:{kotlinVersion}"));
            if (useBazaar && false)
            {
                var bazaarDependencyLines = new[]
                {
                        $"    implementation 'com.github.cafebazaar.Poolakey:poolakey:{poolakeyVersion}'",
                        $"    implementation 'org.jetbrains.kotlin:kotlin-stdlib-jdk7:{kotlinVersion}'",
                    };

                // Inject just before the **DEPS** closing marker.
                int depsIdx = lines.FindIndex(l => l.TrimStart().StartsWith("**DEPS**"));
                if (depsIdx >= 0)
                    lines.InsertRange(depsIdx, bazaarDependencyLines);
                else
                    lines.AddRange(bazaarDependencyLines); // fallback
            }
            File.WriteAllLines(mainGradle, lines);
        }
    }
}
