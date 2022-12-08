using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace BxUni
{
    /// <summary>
    /// Missingがあるアセットを検索してそのリストを表示する  
    /// </summary>  
    public class MissingReferenceFinder : EditorWindow
    {
        private class AssetParameterData
        {
            public Object             m_baseObj;
            public string             m_objectPath;
            public SerializedProperty m_property;
        }

        private readonly string[] k_extensions =
        {
            ".prefab", ".mat", ".controller", ".cs", ".shader", ".mask", ".asset"
        };

        private List<AssetParameterData> m_missingList = new List<AssetParameterData>();
        private List<AssetParameterData> MissingList => m_missingList;

        private Vector2 m_scrollPos;

        [MenuItem("BeXide/Missing Finder/Missing Reference Finder")]
        private static void ShowMissingList()
        {
            // ウィンドウを表示  
            var window = GetWindow<MissingReferenceFinder>();
            //window.minSize = new Vector2(900, 300);

            // Missingがあるアセットを検索  
            window.Search();
        }

        /// <summary>  
        /// Missingがあるアセットを検索  
        /// </summary>  
        private void Search()
        {
            MissingList.Clear();

            var selections = Selection.GetFiltered<Object>(SelectionMode.Assets);

            string[] paths = selections.Length <= 0
                ? AssetDatabase.GetAllAssetPaths()
                : selections.Select(AssetDatabase.GetAssetPath).ToArray();

            SearchOnPath(paths);
        }

        private void SearchOnPath(string[] allPaths)
        {
            int length = allPaths.Length;
            for (int i = 0; i < length; i++)
            {
                // プログレスバーを表示
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Search Missing",
                        $"{i + 1}/{length}",
                        (float)i / length)) { break; }

                // Missing状態のプロパティを検索  
                if (k_extensions.Contains(Path.GetExtension(allPaths[i])))
                {
                    SearchMissing(allPaths[i]);
                }
            }

            // プログレスバーを消す  
            EditorUtility.ClearProgressBar();
        }

        /// <summary>  
        /// 指定アセットにMissingのプロパティがあれば、それをmissingListに追加する  
        /// </summary>  
        /// <param name="path">Path.</param>  
        private void SearchMissing(string path)
        {
            // 指定パスのオブジェクト
            var baseObj = AssetDatabase.LoadAssetAtPath<Object>(path);

            // 指定パスのアセットを全て取得  
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);

            // 各アセットについて、Missingのプロパティがあるかチェック  
            foreach (var obj in assets)
            {
                if (obj == null) { continue; }

                //Debug.Log($" obj=[{obj}]");
                var currentObj = obj;

                // SerializedObjectを通してアセットのプロパティを取得する  
                var sobj = new SerializedObject(obj);

                if (obj.ToString().EndsWith("PrefabInstance)"))
                {
                    var source = sobj.FindProperty("m_SourcePrefab");
                    if (source != null && source.objectReferenceValue != null)
                    {
                        var    refObj  = source.objectReferenceValue;
                        string refPath = AssetDatabase.GetAssetPath(refObj);
                        //Debug.Log($"  SourcePrefabPath=[{refPath}]");
                        var sourcePrefab = AssetDatabase.LoadAssetAtPath<Object>(refPath);
                        currentObj = sourcePrefab;
                    }
                }

                var property = sobj.GetIterator();

                while (property.Next(true))
                {
                    if (IsMissing(property))
                    {
                        Debug.LogWarning($"Missing in {path}:\t{property.propertyPath}");

                        // Missing状態のプロパティリストに追加する  
                        MissingList.Add(
                            new AssetParameterData
                            {
                                m_baseObj    = baseObj,
                                m_objectPath = currentObj.name,
                                m_property   = property.Copy()
                            });
                    }
                }
            }
        }

        /// <summary>
        /// 特定のプロパティがMissingかどうかを調べる
        /// original information from https://teratail.com/questions/167668
        /// </summary>
        private bool IsMissing(SerializedProperty sp)
        {
            if (sp.propertyType == SerializedPropertyType.ObjectReference &&
                sp.objectReferenceValue == null &&
                sp.hasChildren)
            {
                var fileId = sp.FindPropertyRelative("m_FileID");
                if (fileId != null &&
                    fileId.intValue != 0) { return true; }
            }
            return false;
        }

        /// <summary>  
        /// Missingのリストを表示  
        /// </summary>  
        private void OnGUI()
        {
            // 列見出し  
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Asset", GUILayout.Width(200));
            EditorGUILayout.LabelField("SubAsset", GUILayout.Width(200));
            EditorGUILayout.LabelField("Property");
            EditorGUILayout.EndHorizontal();

            // リスト表示  
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

            foreach (var data in MissingList)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(
                    data.m_baseObj,
                    data.m_baseObj.GetType(),
                    true,
                    GUILayout.Width(200));
                EditorGUILayout.LabelField(
                    data.m_objectPath,
                    GUILayout.Width(200));
                EditorGUILayout.TextField(data.m_property.propertyPath);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}