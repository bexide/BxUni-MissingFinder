using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Missing Script のある GameObject をプロジェクト中から探す
/// </summary>
public class MissingScriptFinder : EditorWindow
{
    private class ResultData
    {
        public Object prefab;
        public Object obj;
        public string path;
    }

    private List<ResultData> m_missingList;
    private List<ResultData> MissingList
    {
        get
        {
            if (m_missingList == null)
            {
                m_missingList = new List<ResultData>();
            }
            return m_missingList;
        }
    }
    private Vector2 m_scrollPos;

    [MenuItem("BeXide/Missing Script Finder")]
    private static void ShowMissingList()
    {
        // ウィンドウを表示  
        var window = GetWindow<MissingScriptFinder>();
        //window.minSize = new Vector2(900, 300);

        window.Search();
    }

    /// <summary>  
    /// Missingがあるアセットを検索  
    /// </summary>  
    private void Search()
    {
        MissingList.Clear();

        // 全ての Prefab を検索
        string[] guids = AssetDatabase.FindAssets("t:prefab");
        int guidsLength = guids.Length;
        for (int i = 0; i < guidsLength; i++)
        {
            string guid = guids[i];
            //Debug.Log($"[{guid}]");
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($" cannot get path from GUID [{guid}]");
                continue;
            }

            // プログレスバー
            if (EditorUtility.DisplayCancelableProgressBar("集計中",
                $"{i + 1}/{guidsLength}", (float)(i + 1) / guidsLength))
            {
                // キャンセルされた
                break;
            }

            // Missing状態のコンポーネントを検索
            SearchMissing(path);
        }

        // プログレスバーを消す  
        EditorUtility.ClearProgressBar();
    }

    private void SearchMissing(string path)
    {
        // 指定パスのオブジェクト
        var baseObj = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (baseObj == null)
        {
            Debug.LogError($"cannot load Object at [{path}]. skip.");
            return;
        }

        // 指定パスのアセットを全て取得  
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);

        foreach (var obj in assets)
        {
            if (obj == null)
            {
                Debug.LogError($"null object in [{path}]");
                continue;

            }
            //Debug.Log($"{baseObj.name}:\t{obj.name}\t({obj.GetType()})");
            if (obj is GameObject go)
            {
                var components = go.GetComponents<Component>();
                if (components.Any(c => c == null))
                {
                    //Debug.Log($"{baseObj.name}:\t{obj.name} has missing script!");
                    MissingList.Add(new ResultData()
                    {
                        path = path,
                        prefab = baseObj,
                        obj = obj,
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
        EditorGUILayout.LabelField("Prefab", GUILayout.Width(200));
        EditorGUILayout.LabelField("GameObject", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        // リスト表示  
        m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

        foreach (var data in MissingList)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(data.prefab, data.prefab.GetType(), true, GUILayout.Width(200));
            EditorGUILayout.ObjectField(data.obj, data.obj.GetType(), true, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}