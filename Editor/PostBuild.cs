using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Device;

namespace Modules.Extensions.Prototypes.Editor
{
    public class PostBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (Application.isBatchMode)
                RemoveTempClass();
        }

        private static void RemoveTempClass()
        {
            Directory.Delete(PostProcessorCompilation.CompileHackDirectory, true);
            File.Delete($"{PostProcessorCompilation.CompileHackDirectory}.meta");
        }
    }
}