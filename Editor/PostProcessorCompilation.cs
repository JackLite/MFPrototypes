using System;
using System.Collections.Generic;
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
        public const string CompileHackDirectory = "Assets/__ModulesProto__";

        private static readonly List<string> _assembliesPath = new();

        static PostProcessorCompilation()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            // update prototypes when it's first time editor loads domain
            if (!SessionState.GetBool("Modules.FirstEditorDomainLoad", false))
            {
                SessionState.SetBool("Modules.FirstEditorDomainLoad", true);
                Debug.Log("[Modules.Proto] Force update assemblies when editor starts.");
                ForceUpdateAssemblies();
                CreateHackFile();
            }

            StartDeletingHackFile();
        }

        [MenuItem("Modules/Prototypes/Force update prototypes", priority = -10)]
        public static void ForceUpdateFromMenu()
        {
            Debug.Log("[Modules.Proto] Force update assemblies");
            ForceUpdateAssemblies();
        }

        private static void OnCompilationFinished(object obj)
        {
            if (_assembliesPath.Count == 0)
                return;

            CompilationPipeline.compilationFinished += AddPrototypes;
            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.None);
        }

        private static void ForceUpdateAssemblies()
        {
            EditorApplication.LockReloadAssemblies();
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
            var assembliesLookUp = assemblies.Select(ass => ass.name).ToHashSet();
            var list = new LinkedList<Assembly>(assemblies);
            var infSave = 10 + list.Count;
            while (list.Count > 0 && infSave > 0)
            {
                infSave--;
                var assembly = list.First();
                var assemblyName = assembly.name;
                if (assemblyName.StartsWith("Unity.") || assemblyName.StartsWith("UnityEngine"))
                {
                    list.RemoveFirst();
                    assembliesLookUp.Remove(assemblyName);
                    continue;
                }

                if (assemblyName.StartsWith("ModulesFramework.") || assemblyName.StartsWith("Modules.Extensions."))
                {
                    list.RemoveFirst();
                    assembliesLookUp.Remove(assemblyName);
                    continue;
                }

                var isRelyingOn = false;
                foreach (var reference in assembly.assemblyReferences)
                {
                    if (assembliesLookUp.Contains(reference.name))
                    {
                        isRelyingOn = true;
                        break;
                    }
                }

                if (isRelyingOn)
                {
                    list.RemoveFirst();
                    list.AddLast(assembly);
                    continue;
                }

                var suc = CreateComponentsWrappers(assembly.outputPath);
                if (suc)
                {
                    list.RemoveFirst();
                    assembliesLookUp.Remove(assemblyName);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }

            EditorApplication.UnlockReloadAssemblies();
            EditorUtility.RequestScriptReload();
        }

        private static void CreateHackFile()
        {
            Debug.Log("[Modules.Proto] Create temp hack file");
            if (!Directory.Exists(CompileHackDirectory))
                Directory.CreateDirectory(CompileHackDirectory);
            var timestamp = (long)(DateTime.Now - DateTime.UnixEpoch).TotalSeconds;
            File.WriteAllText(CompileHackFile, $"internal static class __ModulesProtoHack__{timestamp} {{}}");
            AssetDatabase.Refresh();
        }

        private static void StartDeletingHackFile()
        {
            EditorApplication.update += CheckDeletionHackFile;
        }

        private static void CheckDeletionHackFile()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                return;

            if (Directory.Exists(CompileHackDirectory) && !Application.isBatchMode)
            {
                Debug.Log("[Modules.Proto] Delete temp hack file");
                AssetDatabase.DeleteAsset(CompileHackDirectory);
            }

            EditorApplication.update -= CheckDeletionHackFile;
        }

        private static void AddPrototypes(object _)
        {
            _assembliesPath.RemoveAll(CreateComponentsWrappers);
            CompilationPipeline.compilationFinished -= AddPrototypes;
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
        {
            if (compilerMessages.Any(c => c.type == CompilerMessageType.Error))
            {
                _assembliesPath.Clear();
                return;
            }

            var assemblyName = Path.GetFileName(assemblyPath);
            if (assemblyName.StartsWith("Unity.") || assemblyName.StartsWith("UnityEngine"))
                return;

            if (assemblyName.StartsWith("ModulesFramework.") || assemblyName.StartsWith("Modules.Extensions."))
                return;

            _assembliesPath.Add(assemblyPath);
        }

        private static bool CreateComponentsWrappers(string assemblyPath)
        {
            AssemblyDefinition assembly;
            try
            {
                using var fileStream =
                    new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                assembly = AssemblyDefinition.ReadAssembly(fileStream, new ReaderParameters(ReadingMode.Immediate)
                {
                    ReadWrite = true,
                    AssemblyResolver = new AssemblyResolver(assemblyPath),
                    ReadSymbols = false,
                    ReadingMode = ReadingMode.Deferred,
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
                Debug.Log("[Modules.Proto] Created wrappers for " + assemblyPath);
                return true;
            }
            catch (IOException e)
            {
                Debug.LogWarning("[Modules.Proto] Failed to read assembly: " + assemblyPath + ". " +
                                 "There will be another attempt\n\r Exception: " + e);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Modules.Proto] Failed to read assembly: " + assemblyPath + "\n\r Exception: " + e);
                return true;
            }
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