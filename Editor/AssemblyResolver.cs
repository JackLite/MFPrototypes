using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using UnityEditor;

namespace Modules.Extensions.Prototypes.Editor
{
    /// <summary>
    ///     This resolver was taken from the Weaver
    ///     https://github.com/ByronMayne/Weaver
    /// </summary>
    public class AssemblyResolver : DefaultAssemblyResolver
    {
        public AssemblyResolver(string assemblyPath)
        {
            var asm = UnityEditor.Compilation.CompilationPipeline.GetAssemblies()
                .FirstOrDefault(x => x.outputPath == assemblyPath);
            List<string> dependencies = new()
            {
                UnityEditorInternal.InternalEditorUtility.GetEngineCoreModuleAssemblyPath(),
                Path.GetDirectoryName(asm.outputPath)
            };
            foreach (var refer in asm.compiledAssemblyReferences)
            {
                var directory = Path.GetDirectoryName(refer);
                if (dependencies.Contains(directory) == false)
                    dependencies.Add(directory);
            }

            foreach (var str in dependencies)
                AddSearchDirectory(str);

            AddSearchDirectory(assemblyPath);
            AddSearchDirectory(Path.GetDirectoryName(EditorApplication.applicationPath) + "\\Data\\Managed");
        }
    }
}