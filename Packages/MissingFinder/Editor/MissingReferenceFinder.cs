// (C)2022 BeXide,Inc

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace BxUni.MissingFinder
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

        [System.Flags]
        enum AssetType
        {
            Prefab   = 1 << 0,
            Material = 1 << 1,
            Animator = 1 << 2,
            Script   = 1 << 3,
            Shader   = 1 << 4,
            Mask     = 1 << 5,
            Other    = 1 << 6,
        };
        
        private readonly string[] k_extensions =
        {
            ".prefab", ".mat", ".controller", ".cs", ".shader", ".mask", ".asset"
        };

        /// <summary>設定</summary>
        private MissingFinderSettings Settings { get; set; }

        /// <summary> GUI </summary>
        private MultiColumnHeader m_columnHeader;

        private MultiColumnHeaderState.Column[] m_columns;

        private AssetType m_targetAssetTypes = (AssetType)~0;

        private List<AssetParameterData> MissingList { get; set; }

        private Vector2 m_scrollPos;

        [MenuItem("BeXide/Missing Reference Finder")]
        private static void ShowMissingList()
        {
            // ウィンドウを表示  
            var window = GetWindow<MissingReferenceFinder>();
            //window.minSize = new Vector2(900, 300);

            window.Initialize();
        }

        /// <summary>初期化</summary>
        private void Initialize()
        {
            LoadSettings();
            InitializeMultiColumnHeader();
        }

        private void LoadSettings()
        {
            string settingsPath = $"Assets/Editor/BxUniMissingReferenceFinder.asset";
            Settings = AssetDatabase.LoadAssetAtPath<MissingFinderSettings>(settingsPath);

            if (Settings == null)
            {
                Settings = CreateInstance<MissingFinderSettings>();
                CheckDirectory(settingsPath);
                AssetDatabase.CreateAsset(Settings, settingsPath);
            }
        }

        private void CheckDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// マルチカラムヘッダ初期化
        /// </summary>
        private void InitializeMultiColumnHeader()
        {
            m_columns = new[]
            {
                new MultiColumnHeaderState.Column()
                {
                    headerContent       = new GUIContent("Asset"),
                    width               = 100f,
                    autoResize          = true,
                    headerTextAlignment = TextAlignment.Left
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent       = new GUIContent("SubAsset"),
                    width               = 50f,
                    autoResize          = true,
                    headerTextAlignment = TextAlignment.Left
                },
                new MultiColumnHeaderState.Column()
                {
                    headerContent       = new GUIContent("Property"),
                    width               = 100f,
                    autoResize          = true,
                    headerTextAlignment = TextAlignment.Left
                },
            };
            m_columnHeader
                = new MultiColumnHeader(new MultiColumnHeaderState(m_columns)) { height = 25 };
            m_columnHeader.ResizeToFit();
            //m_columnHeader.sortingChanged += OnSortingChanged;
        }

        private void ClearResult()
        {
            if (MissingList == null) { MissingList = new List<AssetParameterData>(); }
            else { MissingList.Clear(); }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("アセット中の参照切れを検出します");
            EditorGUILayout.Space();

            var newTarget =
                EditorGUILayout.ObjectField(
                    "対象フォルダ",
                    Settings.TargetFolder,
                    typeof(DefaultAsset),
                    allowSceneObjects: false);
            Settings.TargetFolder = newTarget as DefaultAsset;

            m_targetAssetTypes = (AssetType)EditorGUILayout.EnumFlagsField(
                "対象アセットタイプ", m_targetAssetTypes);

            if (MissingList == null)
            {
                EditorGUILayout.HelpBox(
                    "チェックを開始するには下のチェックボタンを押してください。",
                    MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("チェック", GUILayout.MaxWidth(120)))
            {
                EditorCoroutineUtility.StartCoroutine(Execute(), this);
            }

            EditorGUI.BeginDisabledGroup(MissingList == null);
            if (GUILayout.Button("クリア", GUILayout.MaxWidth(120))) { ClearResult(); }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            DrawResult();
        }

        /// <summary>  
        /// Missingのリストを表示  
        /// </summary>  
        private void DrawResult()
        {
            if (MissingList == null) { return; }

            if (MissingList.Count == 0)
            {
                EditorGUILayout.HelpBox("見つかりませんでした。", MessageType.Info);
                return;
            }

            // カラムヘッダ
            var headerRect = EditorGUILayout.GetControlRect();
            headerRect.height = m_columnHeader.height;
            float xScroll = 0;
            m_columnHeader.OnGUI(headerRect, xScroll);

            // リスト表示
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

            foreach (var data in MissingList)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(
                    data.m_baseObj,
                    data.m_baseObj.GetType(),
                    true,
                    GUILayout.Width(m_columnHeader.GetColumnRect(0).width - 2f));
                EditorGUILayout.TextField(
                    data.m_objectPath,
                    GUILayout.Width(m_columnHeader.GetColumnRect(1).width - 2f));
                EditorGUILayout.TextField(data.m_property.propertyPath);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            // 修正ボタン
            if (GUILayout.Button("参照切れを削除")) { ConfirmFixMissingReferences(); }
        }

        /// <summary>  
        /// 検索実行  
        /// </summary>  
        private IEnumerator Execute()
        {
            ClearResult();

            string targetPath = AssetDatabase.GetAssetPath(Settings.TargetFolder);
            if (string.IsNullOrEmpty(targetPath)) { targetPath = "Assets"; }

            string[] guids       = AssetDatabase.FindAssets("", new[] { targetPath });
            int      guidsLength = guids.Length;
            if (guidsLength <= 0) { yield break; }

            string[] extensions = k_extensions
                .Where((_, index) => m_targetAssetTypes.HasFlag((AssetType)(1 << index)))
                .ToArray();

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

                // プログレスバーを表示
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Search Missing",
                        $"{i + 1}/{guidsLength}",
                        (float)i / guidsLength)) { break; }

                if (extensions.Contains(Path.GetExtension(path)))
                {
                    SearchMissing(path);
                    yield return null;
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
        /// 見つかった参照切れを削除する
        /// </summary>
        private void ConfirmFixMissingReferences()
        {
            if (EditorUtility.DisplayDialog(
                    "参照切れを削除",
                    "見つかった参照切れを削除します。同時に全ての未保存の修正は保存され、この操作をUndoすることはできません。よろしいですか？",
                    "削除実行",
                    "キャンセル"))
            {
                FixMissingReference();
            }
        }

        private void FixMissingReference()
        {
            var assets = MissingList.Select(param => param.m_baseObj).Distinct().ToList();

            int length = assets.Count;
            for (int i = 0; i < length; i++)
            {
                // プログレスバーを表示
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Remove Missing",
                        $"{i + 1}/{length}",
                        (float)i / length)) { break; }

                string path = AssetDatabase.GetAssetPath(assets[i]);
                FixMissingReference(path);
            }

            AssetDatabase.SaveAssets();

            // プログレスバーを消す  
            EditorUtility.ClearProgressBar();

            ClearResult();
        }

        private void FixMissingReference(string path)
        {
            // 指定パスのアセットを全て取得
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);

            // 各アセットについて、Missingのプロパティがあるかチェック  
            foreach (var obj in assets)
            {
                if (obj == null) { continue; }

                // SerializedObjectを通してアセットのプロパティを取得する  
                var sobj     = new SerializedObject(obj);
                var property = sobj.GetIterator();

                while (property.Next(true))
                {
                    if (IsMissing(property))
                    {
                        Debug.LogWarning($"Remove {path}:\t{property.propertyPath}");
                        var fileId = property.FindPropertyRelative("m_FileID");
                        fileId.intValue = 0;
                    }
                }

                sobj.ApplyModifiedPropertiesWithoutUndo();
            }
        }

    }
}