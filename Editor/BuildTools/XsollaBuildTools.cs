using System.IO;
using UnityEngine;

namespace GameWarriors.VendorDomian.VendorEditor
{
    public static class XsollaBuildTools
    {
        public static void SetupMainTemplateForXsolla(bool isXsollaEnable, string xsollaVersion = "3.0.461")
        {
            VendorBuildTools.SetEnabled("Packages/com.xsolla.sdk/Plugins/XsollaSDK/Android/XsollaStoreClientNativeAndroid.java", isXsollaEnable);
            string mainGradle = Path.Combine(Application.dataPath, "Plugins/Android/mainTemplate.gradle");
            if (!File.Exists(mainGradle))
            {
                Debug.LogWarning("[IABTool] mainTemplate.gradle not found at: " + mainGradle);
                return;
            }
            var lines = new System.Collections.Generic.List<string>(File.ReadAllLines(mainGradle));
            int removes = lines.RemoveAll(l =>
                l.Contains("com.xsolla.android:mobile"));
            if (isXsollaEnable)
            {
                var xsollaDependencyLines = new[]
                {
                        $"    implementation 'com.xsolla.android:mobile:{xsollaVersion}'",
                    };

                // Inject just before the **DEPS** closing marker.
                int depsIdx = lines.FindIndex(l => l.TrimStart().StartsWith("**DEPS**"));
                if (depsIdx >= 0)
                    lines.InsertRange(depsIdx, xsollaDependencyLines);
                else
                    lines.AddRange(xsollaDependencyLines); // fallback
            }
            File.WriteAllLines(mainGradle, lines);
        }
    }
}
