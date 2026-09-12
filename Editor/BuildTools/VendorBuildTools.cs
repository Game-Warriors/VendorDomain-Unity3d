using UnityEditor;
using UnityEngine;

namespace GameWarriors.VendorDomian.VendorEditor
{
    public static class VendorBuildTools
    {
        public static void SetEnabled(string packagePath, bool enabled, BuildTarget buildTarget = BuildTarget.Android)
        {
            var importer = AssetImporter.GetAtPath(packagePath) as PluginImporter;
            if (importer == null)
            {
                Debug.LogWarning($"{packagePath} not found");
                return;
            }
            importer.SetCompatibleWithPlatform(buildTarget, enabled);
        }
    }
}
