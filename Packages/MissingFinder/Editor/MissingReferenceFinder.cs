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
            public string             path;
            public Object             baseObj;
            public Object             obj;
            public SerializedProperty property;
        }

        private readonly string[] extensions =
        {
            ".prefab", ".mat", ".controller", ".cs", ".shader", ".mask", ".asset"
        };

        private List<AssetParameterData> m_missingList;

        private List<AssetParameterData> MissingList
        {
            get
            {
                if (m_missingList == null) { m_missingList = new List<AssetParameterData>(); }
                return m_missingList;
            }
        }

        private Vector2 scrollPos;

        [MenuItem("BeXide/Missing Reference Finder")]
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

            // 全てのアセットのファイルパスを取得  
            string[] allPaths = AssetDatabase.GetAllAssetPaths();
            int      length   = allPaths.Length;

            for (int i = 0; i < length; i++)
            {
                // プログレスバーを表示
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Search Missing",
                        string.Format("{0}/{1}", i + 1, length),
                        (float)i / length)) { break; }

                // Missing状態のプロパティを検索  
                if (extensions.Contains(Path.GetExtension(allPaths[i])))
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
                //if (obj.name == "Deprecated EditorExtensionImpl")
                //{
                //    continue;
                //}

                // SerializedObjectを通してアセットのプロパティを取得する  
                var sobj     = new SerializedObject(obj);
                var property = sobj.GetIterator();

                while (property.Next(true))
                {
                    // プロパティの種類がオブジェクト（アセット）への参照で、  
                    // その参照がnullなのにもかかわらず、参照先インスタンスIDが0でないものはMissing状態！  
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == null &&
                        property.objectReferenceInstanceIDValue != 0)
                    {
                        Debug.Log($"{path}:\t{property.propertyPath}");

                        // Missing状態のプロパティリストに追加する  
                        MissingList.Add(
                            new AssetParameterData()
                            {
                                path     = path,
                                baseObj  = baseObj,
                                obj      = obj,
                                property = property.Copy(),
                            });
                    }
                }
            }
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
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var data in MissingList)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(
                    data.baseObj,
                    data.baseObj.GetType(),
                    true,
                    GUILayout.Width(200));
                EditorGUILayout.ObjectField(
                    data.obj,
                    data.obj.GetType(),
                    true,
                    GUILayout.Width(200));
                EditorGUILayout.TextField(data.property.propertyPath);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}