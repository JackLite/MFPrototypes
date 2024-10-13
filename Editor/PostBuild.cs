using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Modules.Extensions.Prototypes.Editor
{
    public class PostBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder { get; } = int.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            RemoveTempClass();
        }

        private static void RemoveTempClass()
        {
            if (!Directory.Exists(PostProcessorCompilation.CompileHackDirectory))
                return;

            Debug.Log("[Modules.Proto] Remove hack");
            Directory.Delete(PostProcessorCompilation.CompileHackDirectory, true);
            File.Delete($"{PostProcessorCompilation.CompileHackDirectory}.meta");
        }
    }
}