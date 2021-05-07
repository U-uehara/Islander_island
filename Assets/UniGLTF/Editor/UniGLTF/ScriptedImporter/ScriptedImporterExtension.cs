﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif


namespace UniGLTF
{
    public static class ScriptedImporterExtension
    {
        public static void ClearExternalObjects<T>(this ScriptedImporter importer) where T : UnityEngine.Object
        {
            foreach (var externalObject in importer.GetExternalObjectMap().Where(x => x.Key.type == typeof(T)))
            {
                importer.RemoveRemap(externalObject.Key);
            }

            AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
            AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
        }

        public static IEnumerable<(SubAssetKey, T)> GetSubAssets<T>(this ScriptedImporter importer, string assetPath) where T : UnityEngine.Object
        {
            return AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .Where(x => AssetDatabase.IsSubAsset(x))
                .Where(x => x is T)
                .Select(x => (new SubAssetKey(typeof(T), x.name), x as T));
        }

        public static void DrawRemapGUI<T>(this ScriptedImporter importer, IEnumerable<SubAssetKey> keys) where T : UnityEngine.Object
        {
            EditorGUI.indentLevel++;
            {
                var map = importer.GetExternalObjectMap();
                foreach (var key in keys)
                {
                    if (string.IsNullOrEmpty(key.Name))
                    {
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(key.Name);
                    map.TryGetValue(new AssetImporter.SourceAssetIdentifier(key.Type, key.Name), out UnityEngine.Object value);
                    var asset = EditorGUILayout.ObjectField(value, typeof(T), true) as T;
                    if (asset != value)
                    {
                        // update
                        importer.SetExternalUnityObject(new AssetImporter.SourceAssetIdentifier(value), asset);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// subAsset を 指定された path に extract する
        /// </summary>
        /// <param name="subAsset"></param>
        /// <param name="destinationPath"></param>
        /// <param name="isForceUpdate"></param>
        public static void ExtractSubAsset(this UnityEngine.Object subAsset, string destinationPath, bool isForceUpdate)
        {
            string assetPath = AssetDatabase.GetAssetPath(subAsset);

            // clone を path に出力
            var clone = UnityEngine.Object.Instantiate(subAsset);
            AssetDatabase.CreateAsset(clone, destinationPath);

            // subAsset を clone に対して remap する
            var assetImporter = AssetImporter.GetAtPath(assetPath);
            assetImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(clone), clone);

            if (isForceUpdate)
            {
                AssetDatabase.WriteImportSettingsIfDirty(assetPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }
    }
}
