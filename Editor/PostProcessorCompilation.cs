using System.IO;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Modules.Extensions.Prototypes.Editor
{
    [InitializeOnLoad]
    public static class PostProcessorCompilation
    {
        static PostProcessorCompilation()
        {
            CompilationPipeline.assemblyCompilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(string assemblyPath, CompilerMessage[] compilerMessages)
        {
            if (compilerMessages.Any(c => c.type == CompilerMessageType.Error))
                return;

            CreateComponentsWrappers(assemblyPath);
        }

        private static void CreateComponentsWrappers(string assemblyPath)
        {
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
                    ReadingMode = ReadingMode.Immediate
                });

                var module = assembly.MainModule;

                var attrRef = module.ImportReference(typeof(SerializedComponentAttribute)).Resolve();
                var serializedTypes = module.GetTypes()
                    .Where(t => t.CustomAttributes.Any(attr => attr.AttributeType.Resolve() == attrRef))
                    .ToList();

                foreach (var type in serializedTypes)
                {
                    CreateWrapper(module, type);
                }

                assembly.Write(new WriterParameters
                {
                    WriteSymbols = true
                });
            }
            catch (IOException e)
            {
                Debug.LogWarning("Failed to read assembly: " + assemblyPath + "\n\r Exception: " + e);
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
            var baseClass = module.ImportReference(typeof(MonoComponent<>));

            var genericType = new GenericInstanceType(baseClass);
            genericType.GenericArguments.Add(module.ImportReference(componentType));

            var attrType = module.ImportReference(typeof(SerializedComponentAttribute)).Resolve();
            var attr = componentType.CustomAttributes.First(a => a.AttributeType.Resolve() == attrType);
            var name = attr.ConstructorArguments[0].Value as string;
            var newType = new TypeDefinition(
                "Modules.Extensions.Prototypes.Generated",
                $"ComponentWrapper_{name}",
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