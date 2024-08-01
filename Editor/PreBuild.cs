using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Modules.Extensions.Prototypes.Editor
{
    public class PreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get; }
        public void OnPreprocessBuild(BuildReport report)
        {
            File.WriteAllText(PostProcessorCompilation.CacheBuildFile, "");
        }
    }
}