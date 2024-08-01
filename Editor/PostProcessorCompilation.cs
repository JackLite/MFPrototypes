using System.IO;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Modules.Extensions.Prototypes.Editor
{
    [InitializeOnLoad]
    public static class PostProcessorCompilation
    {
        private const string CompileHackFile = CompileHackDirectory + "/ModulesProtoHack.cs";
        private const string CacheLibraryFile = "Library/_ModulesFramework";
        public const string CompileHackDirectory = "Assets/__ModulesProto__";
        public const string CacheBuildFile = "Library/_ModulesFramework.build";
        public const string LibraryDir = "Library";

        static PostProcessorCompilation()
        {
            if (Directory.Exists(CompileHackDirectory) && !File.Exists(CacheBuildFile))
            {
                Directory.Delete(CompileHackDirectory, true);
                File.Delete($"{CompileHackDirectory}.meta");
            }

            if (!File.Exists(CacheLibraryFile))
            {
                Debug.Log("[Modules.Proto] There's no prototypes cache. Force update assemblies.");
                ForceUpdateAssemblies();
                if (!Directory.Exists(LibraryDir))
                    Directory.CreateDirectory(LibraryDir);
                File.WriteAllText(CacheLibraryFile, "");
                if (!Directory.Exists(CompileHackDirectory))
                    Directory.CreateDirectory(CompileHackDirectory);
                File.WriteAllText(CompileHackFile, "internal static class __ModulesProtoHack__ { }");
            }

            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }

        [MenuItem("Modules/Prototypes/Force update prototypes", priority = -10)]
        public static void ForceUpdateAssemblies()
        {
            Debug.Log("[Modules.Proto] Force update assemblies");
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            foreach (var assembly in assemblies)
            {
                var assemblyName = assembly.name;
                if (assemblyName.StartsWith("Unity.") || assemblyName.StartsWith("UnityEngine"))
                    continue;

                if (assemblyName.StartsWith("ModulesFramework.") || assemblyName.StartsWith("Modules.Extensions."))
                    continue;

                CreateComponentsWrappers(assembly.outputPath);
            }

            EditorApplication.UnlockReloadAssemblies();
        }

        private static void OnCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
        {
            if (compilerMessages.Any(c => c.type == CompilerMessageType.Error))
                return;

            var assemblyName = Path.GetFileName(assemblyPath);
            if (assemblyName.StartsWith("Unity.") || assemblyName.StartsWith("UnityEngine"))
                return;

            if (assemblyName.StartsWith("ModulesFramework.") || assemblyName.StartsWith("Modules.Extensions."))
                return;

            CreateComponentsWrappers(assemblyPath);
        }

        private static void CreateComponentsWrappers(string assemblyPath)
        {
            Debug.Log("[Modules.Proto] Create wrappers for " + assemblyPath);

            AssemblyDefinition assembly;
            var mutex = new Mutex();
            try
            {
                mutex.WaitOne();
                using var fileStream =
                    new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                assembly = AssemblyDefinition.ReadAssembly(fileStream, new ReaderParameters(ReadingMode.Immediate)
                {
                    ReadWrite = true,
                    AssemblyResolver = new AssemblyResolver(assemblyPath),
                    ReadSymbols = true,
                    ReadingMode = ReadingMode.Immediate,
                    SymbolReaderProvider = new PdbReaderProvider()
                });

                var module = assembly.MainModule;

                var attrRef = module.ImportReference(typeof(PrototypeAttribute)).Resolve();
                var serializedTypes = module.GetTypes()
                    .Where(t => t.CustomAttributes.Any(attr => attr.AttributeType.Resolve() == attrRef))
                    .ToList();

                foreach (var type in serializedTypes)
                {
                    CreateWrapper(module, type);
                }

                assembly.Write(new WriterParameters
                {
                    WriteSymbols = true,
                    SymbolWriterProvider = new PdbWriterProvider()
                });
            }
            catch (IOException e)
            {
                Debug.LogWarning("[Modules.Proto] Failed to read assembly: " + assemblyPath + "\n\r Exception: " + e);
                return;
            }
            finally
            {
                mutex.ReleaseMutex();
            }

            EditorUtility.RequestScriptReload();
        }

        private static void CreateWrapper(ModuleDefinition module, TypeDefinition componentType)
        {
            var attrType = module.ImportReference(typeof(PrototypeAttribute)).Resolve();
            var attr = componentType.CustomAttributes.First(a => a.AttributeType.Resolve() == attrType);
            var componentName = attr.ConstructorArguments[0].Value as string;
            var typeName = $"ComponentWrapper_{componentName}";
            const string ns = "Modules.Extensions.Prototypes.Generated";
            if (module.GetType(ns, typeName) != null)
                return;

            var baseClass = module.ImportReference(typeof(MonoComponent<>));

            var genericType = new GenericInstanceType(baseClass);
            genericType.GenericArguments.Add(module.ImportReference(componentType));
            var newType = new TypeDefinition(
                ns,
                typeName,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable,
                genericType
            );

            var methodAttributes = MethodAttributes.Public
                                   | MethodAttributes.HideBySig
                                   | MethodAttributes.SpecialName
                                   | MethodAttributes.RTSpecialName;
            var ctor = new MethodDefinition(".ctor", methodAttributes, module.TypeSystem.Void);
            newType.Methods.Add(ctor);
            var il = ctor.Body.GetILProcessor();
            il.Emit(OpCodes.Ret);

            module.Types.Add(newType);
        }
    }
}