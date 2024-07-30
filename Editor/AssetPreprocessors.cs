using System;
using UnityEditor;
using UnityEngine;

namespace Modules.Extensions.Prototypes.Editor
{
    public class AssetPreprocessors : AssetPostprocessor
    {
        private void OnPreprocessAsset()
        {
            if (!SessionState.GetBool("ModulesProto.IsCompiledOnce", false))
                PostProcessorCompilation.ForceUpdateAssemblies();
            SessionState.SetBool("ModulesProto.IsCompiledOnce", true);
            if (assetPath.EndsWith(".unity") || assetPath.EndsWith(".prefab"))
            {
                Debug.Log("Preprocessing " + assetPath);
            }
        }
    }
}