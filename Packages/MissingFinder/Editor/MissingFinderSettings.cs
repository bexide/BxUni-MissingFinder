// 2022-12-08 BeXide,Inc.
// by Y.Hayashi

using UnityEngine;
using UnityEditor;

namespace BxUni.MissingFinder
{
    /// <summary>
    /// TextureCheckerの設定を保存するアセット
    /// </summary>
    internal class MissingFinderSettings : ScriptableObject
    {
        /// <summary>
        /// デフォルトの検査対象パス
        /// </summary>
        [SerializeField]
        private DefaultAsset m_targetFolder;

        public DefaultAsset TargetFolder
        {
            get => m_targetFolder;
            set
            {
                if (value != m_targetFolder)
                {
                    m_targetFolder = value;
                    EditorUtility.SetDirty(this);
                }
            }
        }
    }
}