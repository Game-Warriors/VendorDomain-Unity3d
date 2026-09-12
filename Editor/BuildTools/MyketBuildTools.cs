using System.IO;
using UnityEngine;

namespace GameWarriors.VendorDomian.VendorEditor
{
    public static class MyketBuildTools
    {
        public static void SetupMyketIabPublicKey(bool useMyket, string myketIabPublicKey)
        {
            string launcherGradle = Path.Combine(Application.dataPath, "Plugins/Android/launcherTemplate.gradle");
            SetupMyketIabPublicKey(useMyket, myketIabPublicKey, launcherGradle);
        }

        public static void SetupMyketIabPublicKey(bool useMyket, string myketIabPublicKey, string launcherGradle)
        {
            if (!File.Exists(launcherGradle))
            {
                Debug.LogWarning("[IABTool] launcherTemplate.gradle not found at: " + launcherGradle);
                return;
            }

            var lines = new System.Collections.Generic.List<string>(File.ReadAllLines(launcherGradle));
            lines.RemoveAll(line => line.Contains("buildConfigField") && line.Contains("IAB_PUBLIC_KEY"));

            int placeholderIndex = lines.FindIndex(line =>
               line.Contains("manifestPlaceholders") && line.Contains("marketApplicationId"));
            if (placeholderIndex >= 0)
            {
                int endIndex = placeholderIndex;
                while (endIndex < lines.Count && !lines[endIndex].Contains("]"))
                    endIndex++;

                lines.RemoveRange(placeholderIndex, endIndex - placeholderIndex + 1);
            }

            int applicationIdIndex = lines.FindIndex(line => line.TrimStart().StartsWith("applicationId "));
            if (applicationIdIndex < 0)
            {
                Debug.LogError("[IABTool] applicationId was not found in launcherTemplate.gradle.");
                return;
            }

            string marketApplicationId = useMyket ? "ir.mservices.market" : string.Empty;
            string marketBindAddress = useMyket
                ? "ir.mservices.market.InAppBillingService.BIND"
                : string.Empty;
            string marketPermission = useMyket
                ? "ir.mservices.market.BILLING"
                : string.Empty;
            lines.Insert(applicationIdIndex + 1,
                $"        manifestPlaceholders = [marketApplicationId: \"{marketApplicationId}\", marketBindAddress: \"{marketBindAddress}\", marketPermission: \"{marketPermission}\"]");


            if (useMyket && !string.IsNullOrEmpty(myketIabPublicKey))
            {
                applicationIdIndex = lines.FindIndex(line => line.TrimStart().StartsWith("applicationId "));
                if (applicationIdIndex < 0)
                {
                    Debug.LogError("[IABTool] applicationId was not found in launcherTemplate.gradle.");
                }
                else
                {
                    lines.Insert(applicationIdIndex + 1,
                        $"        buildConfigField \"String\", \"IAB_PUBLIC_KEY\", \"\\\"{myketIabPublicKey}\\\"\"");
                }
            }

            File.WriteAllLines(launcherGradle, lines);
        }

        public static void SetupMainTemplateForMyket(bool useMyket)
        {
            string mainGradle = Path.Combine(Application.dataPath, "Plugins/Android/mainTemplate.gradle");
            SetupMainTemplateForMyket(useMyket, mainGradle);
        }

        public static void SetupMainTemplateForMyket(bool useMyket, string mainGradle, string billingVersion = "unity-1.6")
        {
            if (!File.Exists(mainGradle))
            {
                Debug.LogWarning("[IABTool] mainTemplate.gradle not found at: " + mainGradle);
                return;
            }

            var lines = new System.Collections.Generic.List<string>(File.ReadAllLines(mainGradle));
            lines.RemoveAll(l =>
                l.Contains($"com.github.myketstore:myket-billing-unity:{billingVersion}"));
            if (useMyket)
            {
                var bazaarDependencyLines = new[]
                {
                        $"    implementation 'com.github.myketstore:myket-billing-unity:{billingVersion}'"
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

