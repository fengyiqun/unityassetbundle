using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class AssetBundleEditorNew : EditorWindow
{

    private enum MenuState
    {
        Normal,
        Add,
        Rename,
        Remove,
    }
    private sealed class AssetBundleFolder
    {
        private static Texture s_CachedIcon = null;
        private readonly List<AssetBundleFolder> m_Folders;
        private readonly List<AssetBundleItem> m_Items;
        public AssetBundleFolder(string name, AssetBundleFolder folder)
        {
            m_Folders = new List<AssetBundleFolder>();
            m_Items = new List<AssetBundleItem>();
            Name = name;
            Folder = folder;
        }
        public string Name
        {
            get;
            private set;
        }
        public AssetBundleFolder Folder
        {
            get;
            private set;
        }
        public string FromRootPath
        {
            get
            {
                return Folder == null ? string.Empty : (Folder.Folder == null ? Name : string.Format("{0}/{1}", Folder.FromRootPath, Name));
            }
        }
        public int Depth
        {
            get
            {
                return Folder != null ? Folder.Depth + 1 : 0;
            }
        }
        public static Texture Icon
        {
            get
            {
                if (s_CachedIcon == null)
                {
                    s_CachedIcon = AssetDatabase.GetCachedIcon("Assets");
                }
                return s_CachedIcon;
            }
        }
        public void Clear()
        {
            m_Folders.Clear();
            m_Items.Clear();
        }
        public AssetBundleFolder[] GetFolders()
        {
            return m_Folders.ToArray();
        }
        public AssetBundleFolder GetFolder(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("AssetBundle folder name is invalid.");
            }
            foreach (AssetBundleFolder folder in m_Folders)
            {
                if (folder.Name == name)
                {
                    return folder;
                }
            }
            return null;
        }
        public AssetBundleFolder AddFolder(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("AssetBundle folder name is invalid.");
            }
            AssetBundleFolder folder = GetFolder(name);
            if (folder != null)
            {
                Debug.LogWarning("AssetBundle folder is already exist.");
            }
            folder = new AssetBundleFolder(name, this);
            m_Folders.Add(folder);
            return folder;
        }
        public AssetBundleItem[] GetItems()
        {
            return m_Items.ToArray();
        }
        public AssetBundleItem GetItem(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("AssetBundle item name is invalid.");
            }

            foreach (AssetBundleItem item in m_Items)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }

            return null;
        }
        public void AddItem(string name, AssetBundleCollEctionNew.AssetBundle assetBundle)
        {
            AssetBundleItem item = GetItem(name);
            if (item != null)
            {
                Debug.LogWarning("AssetBundle item is already exist.");
            }

            item = new AssetBundleItem(name, assetBundle, this);
            m_Items.Add(item);
            m_Items.Sort(AssetBundleItemComparer);
        }
        private int AssetBundleItemComparer(AssetBundleItem a, AssetBundleItem b)
        {
            return a.Name.CompareTo(b.Name);
        }

    }
    private sealed class AssetBundleItem
    {
        private static Texture s_CachedUnknownIcon = null;
        private static Texture s_CachedAssetIcon = null;
        private static Texture s_CachedSceneIcon = null;
        public AssetBundleItem(string name, AssetBundleCollEctionNew.AssetBundle assetbundle, AssetBundleFolder folder)
        {
            if (assetbundle == null)
            {
                Debug.LogWarning("AssetBundle is invalid.");
            }
            if (folder == null)
            {
                Debug.LogWarning("AssetBundle folder is invalid.");
            }

            Name = name;
            AssetBundle = assetbundle;
            Folder = folder;
        }
        public string Name
        {
            get;
            private set;
        }
        public AssetBundleCollEctionNew.AssetBundle AssetBundle
        {
            get;
            private set;
        }
        public AssetBundleFolder Folder
        {
            get;
            private set;
        }
        public string FromRootPath
        {
            get
            {
                return (Folder.Folder == null ? Name : string.Format("{0}/{1}", Folder.FromRootPath, Name));
            }
        }
        public int Depth
        {
            get
            {
                return Folder != null ? Folder.Depth + 1 : 0;
            }
        }
        public Texture Icon
        {

            get
            {
                switch (AssetBundle.Type)
                {
                    case AssetBundleCollEctionNew.AssetBundleType.Asset:
                        return CachedAssetIcon;
                    case AssetBundleCollEctionNew.AssetBundleType.Scene:
                        return CachedSceneIcon;
                    default:
                        return CachedUnknownIcon;
                }
            }

        }
        private static Texture CachedUnknownIcon
        {
            get
            {
                if (s_CachedUnknownIcon == null)
                {
                    s_CachedUnknownIcon = EditorGUIUtility.IconContent("Prefab Icon").image;
                }
                return s_CachedUnknownIcon;
            }
        }
        private static Texture CachedAssetIcon
        {
            get
            {
                if (s_CachedAssetIcon == null)
                {
                    s_CachedAssetIcon = EditorGUIUtility.IconContent("PrefabNormal Icon").image;
                }
                return s_CachedAssetIcon;
            }
        }
        private static Texture CachedSceneIcon
        {
            get
            {
                if (s_CachedSceneIcon == null)
                {
                    s_CachedSceneIcon = EditorGUIUtility.IconContent("SceneAsset Icon").image;
                }
                return s_CachedSceneIcon;
            }
        }
    }
    private AssetBundleEditorControllerNew.AssetBundleEditorController m_AssetBundleColledtion;
    private MenuState m_MenuState = MenuState.Normal;
    private AssetBundleCollEctionNew.AssetBundle m_SelectedAssetBundle = null;

    private AssetBundleFolder m_AssetbundleRoot = null;

    private HashSet<string> m_ExpandedAssetBundleFolderNames = null;
    private HashSet<AssetBundleCollEctionNew.Asset> m_SelectedAssetsInSelectedAssetBundle = null;
    private HashSet<AssetBundleEditorControllerNew.SourceFolder> m_ExpandedSourceFolders = null;
    private HashSet<AssetBundleEditorControllerNew.SourceAsset> m_SelectedSourceAssets = null;
    private Texture m_MissingSourceAssetIcon = null;
    private HashSet<AssetBundleEditorControllerNew.SourceFolder> m_CachedSelectedSourceFolders = null;
    private HashSet<AssetBundleEditorControllerNew.SourceFolder> m_CachedUnselectedSourceFolders = null;
    private HashSet<AssetBundleEditorControllerNew.SourceFolder> m_CachedAssingnedSourceFolders = null;
    private HashSet<AssetBundleEditorControllerNew.SourceFolder> m_CachedUnassignedSourceFolders = null;
    private HashSet<AssetBundleEditorControllerNew.SourceAsset> m_CachedAssignedSourceAssets = null;
    private HashSet<AssetBundleEditorControllerNew.SourceAsset> m_CachedUnassignedSourceAssets = null;
    private Vector2 m_AssetBundlesViewScroll = Vector2.zero;
    private Vector2 m_AssetBundleViewScroll = Vector2.zero;
    private Vector2 m_SourceAssetsViewScroll = Vector2.zero;
    private string m_InputAssetBundleName = null;
    private string m_InputAssetBundleVariant = null;
    private bool m_HideAssignedSourceAssets = false;
    private int m_CurrentAssetBundleContentCount = 0;
    private int m_CurrentAssetBundleRowOnDraw = 0;
    private int m_CurrentSourceRowOnDraw = 0;
    [MenuItem("Tools/AssetBundleEditor", false, 32)]
    private static void Open()
    {

        AssetBundleEditorNew window = GetWindow<AssetBundleEditorNew>(true, "AssetBundle Editor", true);
        window.minSize = new Vector2(1400f, 600f);
    }
    private void OnEnable()
    {
        m_AssetBundleColledtion = new AssetBundleEditorControllerNew.AssetBundleEditorController();
        m_MenuState = AssetBundleEditorNew.MenuState.Normal;
        m_SelectedAssetBundle = null;
        m_AssetbundleRoot = new AssetBundleFolder("AssetBundles", null);
        m_ExpandedAssetBundleFolderNames = new HashSet<string>();
        m_SelectedAssetsInSelectedAssetBundle = new HashSet<AssetBundleCollEctionNew.Asset>();
        m_ExpandedSourceFolders = new HashSet<AssetBundleEditorControllerNew.SourceFolder>();
        m_SelectedSourceAssets = new HashSet<AssetBundleEditorControllerNew.SourceAsset>();
        m_MissingSourceAssetIcon = EditorGUIUtility.IconContent("console.warnicon.sml").image;

        m_CachedSelectedSourceFolders = new HashSet<AssetBundleEditorControllerNew.SourceFolder>();
        m_CachedUnselectedSourceFolders = new HashSet<AssetBundleEditorControllerNew.SourceFolder>();
        m_CachedAssingnedSourceFolders = new HashSet<AssetBundleEditorControllerNew.SourceFolder>();
        m_CachedUnassignedSourceFolders = new HashSet<AssetBundleEditorControllerNew.SourceFolder>();
        m_CachedAssignedSourceAssets = new HashSet<AssetBundleEditorControllerNew.SourceAsset>();
        m_CachedUnassignedSourceAssets = new HashSet<AssetBundleEditorControllerNew.SourceAsset>();

        m_AssetBundleViewScroll = Vector2.zero;
        m_AssetBundlesViewScroll = Vector2.zero;
        m_InputAssetBundleName = null;
        m_InputAssetBundleVariant = null;
        m_HideAssignedSourceAssets = false;
        m_CurrentAssetBundleContentCount = 0;
        m_CurrentAssetBundleRowOnDraw = 0;
        m_CurrentSourceRowOnDraw = 0;
        if (m_AssetBundleColledtion.Load())
        {
            Debug.Log("Load Configuration success.");
        }
        else
        {
            Debug.LogWarning("Load configuration failure.");
        }
        EditorUtility.DisplayProgressBar("Prepare AssetBundle Editor", "Processing...", 0f);
        RefreshAssetBundleTree();
        EditorUtility.ClearProgressBar();
    }

    private void RefreshAssetBundleTree()
    {
        m_AssetbundleRoot.Clear();
        AssetBundleCollEctionNew.AssetBundle[] assetBundles = m_AssetBundleColledtion.GetAssetBundles();
        foreach (var assetbundle in assetBundles)
        {
            string[] splitedPath = assetbundle.Name.Split('/');
            AssetBundleFolder folder = m_AssetbundleRoot;
            for (int i = 0; i < splitedPath.Length - 1; i++)
            {
                AssetBundleFolder subFolder = folder.GetFolder(splitedPath[i]);
                folder = subFolder == null ? folder.AddFolder(splitedPath[i]) : subFolder;
            }
            string assetBundleFullName = assetbundle.Variant != null ? string.Format("{0}.{1}", splitedPath[splitedPath.Length - 1], assetbundle.Variant) : splitedPath[splitedPath.Length - 1];
            folder.AddItem(assetBundleFullName, assetbundle);

        }
    }
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.Width(position.width), GUILayout.Height(position.height));
        {
            
            GUILayout.Space(2f);
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.25f));
            {
                GUILayout.Space(5);
                EditorGUILayout.LabelField(string.Format("AssetBundle List {0}", m_AssetBundleColledtion.AssetBundleCount.ToString()), EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal("box", GUILayout.Height(position.height - 52f));
                {
                    DrawAssetBundlesView();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(5f);
                    DrawAssetBundleMenu();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.25f));
            {
                GUILayout.Space(5f);
                EditorGUILayout.LabelField(string.Format("AssetBundle Content({0})", m_CurrentAssetBundleContentCount.ToString()), EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal("box", GUILayout.Height(position.height - 52f));
                {
                    DrawAssetBundleView();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(5f);
                    DrawAssetBundleMenu();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f - 16f));
            {

                GUILayout.Space(5f);
                EditorGUILayout.LabelField("Asset List", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal("box", GUILayout.Height(position.height - 52f));
                {
                    DrawSourceAssetsView();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(5f);
                    DrawSourceAssetsMenu();
                }
                EditorGUILayout.EndHorizontal();
                
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(5f);

        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSourceAssetsMenu()
    {
        HashSet<AssetBundleEditorControllerNew.SourceAsset> selectedSourceAssets = GetSelectedSourceAssets();
        EditorGUI.BeginDisabledGroup(m_SelectedAssetBundle == null || selectedSourceAssets.Count <= 0);
        {
            if (GUILayout.Button(string.Format("<< {0}", selectedSourceAssets.Count.ToString(), GUILayout.Width(80f))))
            {
                foreach (AssetBundleEditorControllerNew.SourceAsset sourceAsset in selectedSourceAssets)
                {
                    AssignAsset(sourceAsset, m_SelectedAssetBundle);
                }
                m_SelectedSourceAssets.Clear();
                m_CachedSelectedSourceFolders.Clear();
            }
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(selectedSourceAssets.Count <= 0);
        {
            if (GUILayout.Button(string.Format("<<< {0}", selectedSourceAssets.Count.ToString()), GUILayout.Width(80f)))
            {
                int index = 0;
                int count = selectedSourceAssets.Count;
                foreach (AssetBundleEditorControllerNew.SourceAsset sourceAsset in selectedSourceAssets)
                {
                    EditorUtility.DisplayProgressBar("Add AssetBundles", string.Format("{0}/{1} processing...", (++index).ToString(), count.ToString()), (float)index / count);
                    int dotIndex = sourceAsset.FromRootPath.IndexOf('.');
                    string assetBundleName = dotIndex > 0 ? sourceAsset.FromRootPath.Substring(0, dotIndex) : sourceAsset.FromRootPath;
                    AddAssetBundle(assetBundleName, null, false);
                    AssetBundleCollEctionNew.AssetBundle assetBundle = m_AssetBundleColledtion.GetAssetBundle(assetBundleName, null);
                    if (assetBundle == null)
                    {
                        continue;
                    }
                    AssignAsset(sourceAsset, assetBundle);
                }
                EditorUtility.DisplayProgressBar("Add AssetBundles", "Complete processing...", 1f);
                RefreshAssetBundleTree();
                EditorUtility.ClearProgressBar();
                m_SelectedSourceAssets.Clear();
                m_CachedSelectedSourceFolders.Clear();
            }
        }
        EditorGUI.EndDisabledGroup();
        bool hideAssignedSourceAssets = EditorGUILayout.ToggleLeft("Hide Assigned", m_HideAssignedSourceAssets, GUILayout.Width(100f));
        if (hideAssignedSourceAssets != m_HideAssignedSourceAssets)
        {
            m_HideAssignedSourceAssets = hideAssignedSourceAssets;
            m_CachedSelectedSourceFolders.Clear();
            m_CachedUnselectedSourceFolders.Clear();
            m_CachedAssingnedSourceFolders.Clear();
            m_CachedUnassignedSourceFolders.Clear();
        }
        GUILayout.Label(string.Empty);
        if (GUILayout.Button("Clean", GUILayout.Width(80f)))
        {
            EditorUtility.DisplayProgressBar("Clean", "Processing...", 0f);
            CleanAssetBundle();
            EditorUtility.ClearProgressBar();
        }
        if (GUILayout.Button("Save", GUILayout.Width(80f)))
        {
            EditorUtility.DisplayCancelableProgressBar("Save", "Processing...", 0f);
            SaveConfiguration();
            EditorUtility.ClearProgressBar();
        }
    }
    private void SaveConfiguration()
    {
        if (m_AssetBundleColledtion.Save())
        {
            Debug.Log("Save configuration success.");
        }
        else
        {
            Debug.LogWarning("Save configuration failure.");
        }
    }
    private void CleanAssetBundle()
    {
        int unknownAssetCount = m_AssetBundleColledtion.RemoveUnknownAssets();
        int unusedAssetBundleCount = m_AssetBundleColledtion.RemoveUnusedAssetBundles();
        RefreshAssetBundleTree();
        Debug.Log(string.Format("Clean complete,{0} unknown assets and {1} unused AssetBundles has been removed. ", unknownAssetCount.ToString(), unusedAssetBundleCount.ToString()));
    }
    private void AssignAsset(AssetBundleEditorControllerNew.SourceAsset sourceAsset, AssetBundleCollEctionNew.AssetBundle assetBundle)
    {
        if (!m_AssetBundleColledtion.AssignAsset(sourceAsset.Guid, assetBundle.Name, assetBundle.Variant))
        {
            Debug.LogWarning(string.Format("Assign asset '{0}' to AssetBundle '{1}' failure.", sourceAsset.Name, m_SelectedAssetBundle.FullName));
        }
    }
    private HashSet<AssetBundleEditorControllerNew.SourceAsset> GetSelectedSourceAssets()
    {
        if (!m_HideAssignedSourceAssets)
        {
            return m_SelectedSourceAssets;
        }
        HashSet<AssetBundleEditorControllerNew.SourceAsset> selectedUnassignedSourceAssets = new HashSet<AssetBundleEditorControllerNew.SourceAsset>();
        foreach (AssetBundleEditorControllerNew.SourceAsset sourceAsset in m_SelectedSourceAssets)
        {
            if (!IsAssignedSourceAsset(sourceAsset))
            {
                selectedUnassignedSourceAssets.Add(sourceAsset);
            }
        }
        return selectedUnassignedSourceAssets;
    }
    private void DrawSourceAssetsView()
    {

        m_CurrentSourceRowOnDraw = 0;
        m_SourceAssetsViewScroll = EditorGUILayout.BeginScrollView(m_SourceAssetsViewScroll);
        {
            DrawSourceFolder(m_AssetBundleColledtion.SourceAssetRoot);
        }
        EditorGUILayout.EndScrollView();
    }
    private void DrawSourceFolder(AssetBundleEditorControllerNew.SourceFolder sourceFolder)
    {

        if (m_HideAssignedSourceAssets && IsAssignedSourceFolder(sourceFolder))
        {
            return;
        }
        bool expand = IsExpandedSourceFolder(sourceFolder);
        EditorGUILayout.BeginHorizontal();
        {

            bool select = IsSelectedSourceFolder(sourceFolder);
            if (select != EditorGUILayout.Toggle(select, GUILayout.Width(12f + 14f * sourceFolder.Depth)))
            {
                select = !select;
                SetSelectedSourceFolder(sourceFolder, select);
            }
            GUILayout.Space(-14f * sourceFolder.Depth);
            if (expand != EditorGUI.Foldout(new Rect(18f + 14f * sourceFolder.Depth, 20f * m_CurrentSourceRowOnDraw + 2f, int.MaxValue, 14f), expand, string.Empty, true))
            {
                expand = !expand;
                SetExpandedSourceFolder(sourceFolder, expand);
            }
            GUI.DrawTexture(new Rect(32f + 14f * sourceFolder.Depth, 20f * m_CurrentSourceRowOnDraw + 1f, 16f, 16f), AssetBundleEditorControllerNew.SourceFolder.Icon);
            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(26f + 14f * sourceFolder.Depth), GUILayout.Height(18f));
            EditorGUILayout.LabelField(sourceFolder.Name);

        }
        EditorGUILayout.EndHorizontal();
        m_CurrentSourceRowOnDraw++;
        if (expand)
        {
            foreach (AssetBundleEditorControllerNew.SourceFolder subSourceFolder in sourceFolder.GetFolders())
            {
                DrawSourceFolder(subSourceFolder);
            }
            foreach (AssetBundleEditorControllerNew.SourceAsset sourceAsset in sourceFolder.GetAssets())
            {
                DrawSourceAsset(sourceAsset);
            }
        }


    }
    private void DrawSourceAsset(AssetBundleEditorControllerNew.SourceAsset sourceAsset)
    {
        if (m_HideAssignedSourceAssets && IsAssignedSourceAsset(sourceAsset))
        {
            return;
        }
        EditorGUILayout.BeginHorizontal();
        {
            float emptySpace = position.width;
            bool select = IsSelectedSourceAsset(sourceAsset);
            if (select != EditorGUILayout.Toggle(select, GUILayout.Width(emptySpace - 12f)))
            {
                select = !select;
                SetSelectedSourceAsset(sourceAsset, select);
            }
            GUILayout.Space(-emptySpace + 24f);
            GUI.DrawTexture(new Rect(32f + 14f * sourceAsset.Depth, 20f * m_CurrentSourceRowOnDraw + 1f, 16f, 16f), sourceAsset.Icon);
            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(26f + 14f * sourceAsset.Depth), GUILayout.Height(18f));
            EditorGUILayout.LabelField(sourceAsset.Name);
            AssetBundleCollEctionNew.Asset asset = m_AssetBundleColledtion.GetAsset(sourceAsset.Guid);
            EditorGUILayout.LabelField(asset != null ? GetAssetBundleFullName(asset.AssetBundle.Name, asset.AssetBundle.Variant) : string.Empty, GUILayout.Width(position.width * 0.15f));

        }
        EditorGUILayout.EndHorizontal();
        m_CurrentSourceRowOnDraw++;
    }
    private void SetExpandedSourceFolder(AssetBundleEditorControllerNew.SourceFolder sourcefolder, bool expand)
    {
        if (expand)
        {
            m_ExpandedSourceFolders.Add(sourcefolder);
        }
        else
        {
            m_ExpandedSourceFolders.Remove(sourcefolder);
        }
    }
    private void SetSelectedSourceFolder(AssetBundleEditorControllerNew.SourceFolder sourceFolder, bool select)
    {
        if (select)
        {
            m_CachedSelectedSourceFolders.Add(sourceFolder);
            m_CachedUnselectedSourceFolders.Remove(sourceFolder);
            AssetBundleEditorControllerNew.SourceFolder folder = sourceFolder;
            while (folder != null)
            {
                m_CachedUnselectedSourceFolders.Remove(folder);
                folder = folder.Folder;
            }

        }
        else
        {
            m_CachedSelectedSourceFolders.Remove(sourceFolder);
            m_CachedUnselectedSourceFolders.Add(sourceFolder);
            AssetBundleEditorControllerNew.SourceFolder folder = sourceFolder;
            while (folder != null)
            {
                m_CachedSelectedSourceFolders.Remove(folder);
                folder = folder.Folder;

            }
        }
        foreach (AssetBundleEditorControllerNew.SourceAsset sourceAsset in sourceFolder.GetAssets())
        {
            if (m_HideAssignedSourceAssets && IsAssignedSourceAsset(sourceAsset))
            {
                continue;
            }
            SetSelectedSourceAsset(sourceAsset, select);
        }
        foreach (AssetBundleEditorControllerNew.SourceFolder subSourceFolder in sourceFolder.GetFolders())
        {
            if (m_HideAssignedSourceAssets && IsAssignedSourceFolder(subSourceFolder))
            {
                continue;
            }
            SetSelectedSourceFolder(subSourceFolder, select);
        }
    }
    private void SetSelectedSourceAsset(AssetBundleEditorControllerNew.SourceAsset sourceAsset, bool select)
    {
        if (select)
        {
            m_SelectedSourceAssets.Add(sourceAsset);

            AssetBundleEditorControllerNew.SourceFolder folder = sourceAsset.Folder;
            while (folder != null)
            {
                m_CachedUnselectedSourceFolders.Remove(folder);
                folder = folder.Folder;
            }
        }
        else
        {
            m_SelectedSourceAssets.Remove(sourceAsset);
            AssetBundleEditorControllerNew.SourceFolder folder = sourceAsset.Folder;
            while (folder != null)
            {
                m_CachedSelectedSourceFolders.Remove(folder);
                folder = folder.Folder;
            }
        }
    }
    private bool IsSelectedSourceFolder(AssetBundleEditorControllerNew.SourceFolder sourceFolder)
    {
        if (m_CachedSelectedSourceFolders.Contains(sourceFolder))
        {
            return true;
        }
        if (m_CachedUnselectedSourceFolders.Contains(sourceFolder))
        {
            return false;
        }
        foreach (AssetBundleEditorControllerNew.SourceAsset sourceAsset in sourceFolder.GetAssets())
        {
            if (m_HideAssignedSourceAssets && IsAssignedSourceAsset(sourceAsset))
            {
                continue;
            }
            if (!IsSelectedSourceAsset(sourceAsset))
            {
                m_CachedUnselectedSourceFolders.Add(sourceFolder);
                return false;
            }
        }
        foreach (AssetBundleEditorControllerNew.SourceFolder subSourceFolder in sourceFolder.GetFolders())
        {
            if (m_HideAssignedSourceAssets && IsAssignedSourceFolder(sourceFolder))
            {
                continue;
            }
            if (!IsSelectedSourceFolder(subSourceFolder))
            {
                m_CachedUnselectedSourceFolders.Add(sourceFolder);
                return false;
            }
        }
        m_CachedSelectedSourceFolders.Add(sourceFolder);
        return true;
    }

    private bool IsSelectedSourceAsset(AssetBundleEditorControllerNew.SourceAsset sourceAsset)
    {
        return m_SelectedSourceAssets.Contains(sourceAsset);
    }
    private bool IsExpandedSourceFolder(AssetBundleEditorControllerNew.SourceFolder sourceFolder)
    {
        return m_ExpandedSourceFolders.Contains(sourceFolder);
    }
    private bool IsAssignedSourceFolder(AssetBundleEditorControllerNew.SourceFolder sourceFolder)
    {
        if (m_CachedAssingnedSourceFolders.Contains(sourceFolder))
        {
            return true;
        }
        if (m_CachedUnassignedSourceFolders.Contains(sourceFolder))
        {
            return false;
        }
        foreach (AssetBundleEditorControllerNew.SourceAsset sourceAsset in sourceFolder.GetAssets())
        {
            if (!IsAssignedSourceAsset(sourceAsset))
            {
                m_CachedUnassignedSourceFolders.Add(sourceFolder);
                return false;
            }
        }
        foreach (AssetBundleEditorControllerNew.SourceFolder subSourceFolder in sourceFolder.GetFolders())
        {
            if (!IsAssignedSourceFolder(subSourceFolder))
            {
                m_CachedUnassignedSourceFolders.Add(sourceFolder);
                return false;
            }
        }
        m_CachedAssingnedSourceFolders.Add(sourceFolder);
        return true;
    }
    private bool IsAssignedSourceAsset(AssetBundleEditorControllerNew.SourceAsset sourceAsset)
    {
        if (m_CachedAssignedSourceAssets.Contains(sourceAsset))
        {
            return true;
        }
        if (m_CachedUnassignedSourceAssets.Contains(sourceAsset))
        {
            return false;
        }
        return m_AssetBundleColledtion.GetAsset(sourceAsset.Guid) != null;
    }
    private void DrawAssetBundleMenu()
    {
        switch (m_MenuState)
        {
            case AssetBundleEditorNew.MenuState.Normal:
                DrawAssetBundleMenu_Normal();
                break;
            case AssetBundleEditorNew.MenuState.Add:
                DrawAssetBundlesMenu_Add();
                break;
            case MenuState.Rename:
                DrawAssetBundlesMenu_Rename();
                break;
            case MenuState.Remove:
                DrawAssetBundlesMenu_Remove();
                break;

        }
    }
    private void DrawAssetBundleMenu_Normal()
    {
        if (GUILayout.Button("Add", GUILayout.Width(65f)))
        {
            m_MenuState = AssetBundleEditorNew.MenuState.Add;
            m_InputAssetBundleName = null;
            m_InputAssetBundleVariant = null;
            GUI.FocusControl(null);
        }
        EditorGUI.BeginDisabledGroup(m_SelectedAssetBundle == null);
        {
            if (GUILayout.Button("Rename", GUILayout.Width(65f)))
            {
                m_MenuState = AssetBundleEditorNew.MenuState.Rename;
                m_InputAssetBundleName = m_SelectedAssetBundle != null ? m_SelectedAssetBundle.Name : null;
                m_InputAssetBundleVariant = m_SelectedAssetBundle != null ? m_SelectedAssetBundle.Variant : null;
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("Remove", GUILayout.Width(65f)))
            {
                m_MenuState = AssetBundleEditorNew.MenuState.Remove;
            }
            if (m_SelectedAssetBundle == null)
            {
                EditorGUILayout.EnumPopup(AssetBundleCollEctionNew.AssetBundleLoadType.LoadFromFile);
            }
            else
            {
                AssetBundleCollEctionNew.AssetBundleLoadType loadType = (AssetBundleCollEctionNew.AssetBundleLoadType)EditorGUILayout.EnumPopup(m_SelectedAssetBundle.LoadType);
                if (loadType != m_SelectedAssetBundle.LoadType)
                {
                    SetAssetBundleLoadType(loadType);
                }
            }
        }
        EditorGUI.EndDisabledGroup();
    }
    private void DrawAssetBundlesMenu_Add()
    {
        GUI.SetNextControlName("NewAssetBUndleNameTexField");
        m_InputAssetBundleName = EditorGUILayout.TextField(m_InputAssetBundleName);
        GUI.SetNextControlName("NewAssetBundleVariantTexField");
        m_InputAssetBundleVariant = EditorGUILayout.TextField(m_InputAssetBundleVariant);
        if (GUI.GetNameOfFocusedControl() == "NewAssetBundleNameTextField" || GUI.GetNameOfFocusedControl() == "NewAssetBundleVariantTextField")
        {
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                EditorUtility.DisplayProgressBar("Add AssetBundle", "Processing...", 0f);
                AddAssetBundle(m_InputAssetBundleName, m_InputAssetBundleVariant, true);
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }
        if (GUILayout.Button("Add", GUILayout.Width(50f)))
        {
            EditorUtility.DisplayProgressBar("Add AssetBundle", "Processing...", 0f);
            AddAssetBundle(m_InputAssetBundleName, m_InputAssetBundleVariant, true);
            EditorUtility.ClearProgressBar();
        }
        if (GUILayout.Button("Back", GUILayout.Width(50f)))
        {
            m_MenuState = MenuState.Normal;
        }
    }
    private void DrawAssetBundlesMenu_Rename()
    {
        if (m_SelectedAssetBundle == null)
        {
            m_MenuState = MenuState.Normal;
        }
        GUI.SetNextControlName("RenameAssetBundleNameTextField");
        m_InputAssetBundleName = EditorGUILayout.TextField(m_InputAssetBundleName);
        GUI.SetNextControlName("RenameAssetBundleVariantTexField");
        m_InputAssetBundleVariant = EditorGUILayout.TextField(m_InputAssetBundleVariant, GUILayout.Width(60f));
        if (GUI.GetNameOfFocusedControl() == "RenameAssetBundleNameTextField" || GUI.GetNameOfFocusedControl() == "RenameAssetBundleVariantTexField")
        {
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                EditorUtility.DisplayProgressBar("Rename AssetBundle", "Processing...", 0f);
                RenameAssetBundle(m_SelectedAssetBundle, m_InputAssetBundleName, m_InputAssetBundleVariant);
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }
        if (GUILayout.Button("OK", GUILayout.Width(50f)))
        {
            EditorUtility.DisplayProgressBar("Rename AssetBundle", "Processing...", 0f);
            RenameAssetBundle(m_SelectedAssetBundle, m_InputAssetBundleName, m_InputAssetBundleVariant);
            EditorUtility.ClearProgressBar();
        }
        if (GUILayout.Button("Back", GUILayout.Width(50f)))
        {
            m_MenuState = MenuState.Normal;
        }
    }
    private void DrawAssetBundlesMenu_Remove()
    {
        if (m_SelectedAssetBundle == null)
        {
            m_MenuState = MenuState.Normal;
        }
        GUILayout.Label(string.Format("Remove '{0}' ?", m_SelectedAssetBundle.FullName));
        if (GUILayout.Button("Yes", GUILayout.Width(50f)))
        {
            EditorUtility.DisplayProgressBar("Remove AssetBundle", "Processing...", 0f);
            RemoveAssetBundle();
            EditorUtility.ClearProgressBar();
            m_MenuState = MenuState.Normal;
        }
        if (GUILayout.Button("No", GUILayout.Width(50f)))
        {
            m_MenuState = MenuState.Normal;
        }
    }
    private void RenameAssetBundle(AssetBundleCollEctionNew.AssetBundle assetBundle, string newAssetBundleName, string newAssetBundleVariant)
    {
        if (assetBundle == null)
        {
            Debug.LogWarning("AssetBundle is invalid");
            return;
        }
        if (newAssetBundleVariant == string.Empty)
        {
            newAssetBundleVariant = null;
        }
        string oldAssetBundleFullName = assetBundle.FullName;
        string newAssetBundleFullName = GetAssetBundleFullName(newAssetBundleName, newAssetBundleVariant);
        if (m_AssetBundleColledtion.RenameAssetBundle(assetBundle.Name, assetBundle.Variant, newAssetBundleFullName, newAssetBundleFullName))
        {
            RefreshAssetBundleTree();
            Debug.Log(string.Format("Rename AssetBundle '{0}' to '{1}' success", oldAssetBundleFullName, newAssetBundleFullName));
            m_MenuState = MenuState.Normal;
        }
        else
        {
            Debug.LogWarning(string.Format("Rename AssetBundle '{0}' to '{1}' failure ", oldAssetBundleFullName, newAssetBundleFullName));
        }
    }
    private void RemoveAssetBundle()
    {
        string assetBundleFullName = m_SelectedAssetBundle.FullName;
        if (m_AssetBundleColledtion.RemoveAssetBundle(m_SelectedAssetBundle.Name, m_SelectedAssetBundle.Variant))
        {
            ChangeSelectedAssetBundle(null);
            RefreshAssetBundleTree();
            Debug.Log(string.Format("Remove AssetBundle '{0}' success", assetBundleFullName));
        }
        else
        {
            Debug.LogWarning(string.Format("Remove AssetBundle '{0}' failure", assetBundleFullName));
        }
    }
    private void AddAssetBundle(string assetBundleName, string assetBundleVariant, bool refresh)
    {
        if (assetBundleVariant == string.Empty)
        {
            assetBundleVariant = null;
        }
        string assetBundleFullName = GetAssetBundleFullName(assetBundleName, assetBundleVariant);
        if (m_AssetBundleColledtion.AddAssetBundle(assetBundleName, assetBundleVariant, AssetBundleCollEctionNew.AssetBundleLoadType.LoadFromFile, false))
        {
            if (refresh)
            {
                RefreshAssetBundleTree();
            }
            Debug.Log(string.Format("Add AssetBundle '{0}' success", assetBundleFullName));
        }
        else
        {
            Debug.LogWarning(string.Format("Add AssetBundle '{0}' failure", assetBundleFullName));
        }
    }
    private string GetAssetBundleFullName(string assetBundleName, string assetBundleVariant)
    {
        return assetBundleVariant != null ? string.Format("{0}.{1}", assetBundleName, assetBundleVariant) : assetBundleName;
    }
    private void SetAssetBundleLoadType(AssetBundleCollEctionNew.AssetBundleLoadType loadtype)
    {
        string assetBundleFullName = m_SelectedAssetBundle.FullName;
        if (m_AssetBundleColledtion.SetAssetBundleLoadType(m_SelectedAssetBundle.Name, m_SelectedAssetBundle.Variant, loadtype))
        {
            Debug.Log(string.Format("Set AssetBundle '{0}' load type to '{1}' success.", assetBundleFullName, loadtype.ToString()));
        }
        else
        {
            Debug.LogWarning(string.Format("Set AssetBundle '{0}' load type to '{1}' failure.", assetBundleFullName, loadtype.ToString()));

        }
    }
    private void DrawAssetBundleView()
    {
        m_AssetBundleViewScroll = EditorGUILayout.BeginScrollView(m_AssetBundleViewScroll);
        {
            if (m_SelectedAssetBundle != null)
            {

                int index = 0;
                AssetBundleCollEctionNew.Asset[] assets = m_AssetBundleColledtion.GetAssets(m_SelectedAssetBundle.Name, m_SelectedAssetBundle.Variant);
                m_CurrentAssetBundleContentCount = assets.Length;
                foreach (AssetBundleCollEctionNew.Asset asset in assets)
                {
                    AssetBundleEditorControllerNew.SourceAsset sourceAsset = m_AssetBundleColledtion.GetSourceAsset(asset.Guid);
                    string assetName = sourceAsset != null ? (m_AssetBundleColledtion.AssetSorter == AssetBundleEditorControllerNew.AssetSorterType.Path ? sourceAsset.Path : (m_AssetBundleColledtion.AssetSorter == AssetBundleEditorControllerNew.AssetSorterType.Name ? sourceAsset.Name : sourceAsset.Guid)) : asset.Guid;
                    EditorGUILayout.BeginHorizontal();
                    {
                        float emptySpace = position.width;
                        bool select = IsSelectedAssetInSelectedAssetBundle(asset);
                        if (select != EditorGUILayout.Toggle(select, GUILayout.Width(emptySpace - 12f)))
                        {
                            select = !select;
                            SetSelectedAssetInSelectedAssetBundle(asset, select);
                        }
                        GUILayout.Space(-emptySpace + 24f);
                        GUI.DrawTexture(new Rect(20f, 20f * (index++) + 1f, 16f, 16f), (sourceAsset != null ? sourceAsset.Icon : m_MissingSourceAssetIcon));
                        EditorGUILayout.LabelField(string.Empty, GUILayout.Width(14f), GUILayout.Height(18f));
                        EditorGUILayout.LabelField(assetName);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                m_CurrentAssetBundleContentCount = 0;
            }
        }
        EditorGUILayout.EndScrollView();
    }
    private void SetSelectedAssetInSelectedAssetBundle(AssetBundleCollEctionNew.Asset asset, bool select)
    {
        if (select)
        {
            m_SelectedAssetsInSelectedAssetBundle.Add(asset);
        }
        else
        {
            m_SelectedAssetsInSelectedAssetBundle.Remove(asset);
        }
    }
    private bool IsSelectedAssetInSelectedAssetBundle(AssetBundleCollEctionNew.Asset asset)
    {
        return m_SelectedAssetsInSelectedAssetBundle.Contains(asset);
    }
    private void DrawAssetBundlesView()
    {
        m_CurrentAssetBundleRowOnDraw = 0;
        m_AssetBundleViewScroll = EditorGUILayout.BeginScrollView(m_AssetBundleViewScroll);
        {
            DrawAssetBundleFolder(m_AssetbundleRoot);
        }
        EditorGUILayout.EndScrollView();
    }
    private void DrawAssetBundleFolder(AssetBundleFolder assetBundleFolder)
    {
        bool expand = IsExpandedAssetBundleFolder(assetBundleFolder);
        EditorGUILayout.BeginHorizontal();
        {
            if (expand != EditorGUI.Foldout(new Rect(18f + 14f * assetBundleFolder.Depth, 20f * m_CurrentAssetBundleRowOnDraw + 2f, int.MaxValue, 14f), expand, string.Empty, true))
            {
                expand = !expand;
                SetExpandedAssetBundleFolder(assetBundleFolder, expand);
            }
            GUI.DrawTexture(new Rect(32f + 14f * assetBundleFolder.Depth, 20f * m_CurrentAssetBundleRowOnDraw + 1f, 16f, 16f), AssetBundleFolder.Icon);
            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(40f + 14f * assetBundleFolder.Depth), GUILayout.Height(18f));
            EditorGUILayout.LabelField(assetBundleFolder.Name);
        }
        EditorGUILayout.EndHorizontal();

        m_CurrentAssetBundleRowOnDraw++;
        if (expand)
        {
            foreach (AssetBundleFolder subAssetBundleFolder in assetBundleFolder.GetFolders())
            {
                DrawAssetBundleFolder(subAssetBundleFolder);
            }
            foreach (AssetBundleItem assetbundleItem in assetBundleFolder.GetItems())
            {
                DrawAssetBundleItem(assetbundleItem);
            }
        }

    }
    private void DrawAssetBundleItem(AssetBundleItem assetBundleItem)
    {
        EditorGUILayout.BeginHorizontal();
        {
            string title = assetBundleItem.Name;
            if (assetBundleItem.AssetBundle.Packed)
            {
                title = "[Packed]" + title;
            }
            float emptySpace = position.width;
            if (EditorGUILayout.Toggle(m_SelectedAssetBundle == assetBundleItem.AssetBundle, GUILayout.Width(emptySpace - 12f)))
            {
                ChangeSelectedAssetBundle(assetBundleItem.AssetBundle);
            }
            else if (m_SelectedAssetBundle == assetBundleItem.AssetBundle)
            {
                ChangeSelectedAssetBundle(null);
            }
            GUILayout.Space(-emptySpace + 24f);
            GUI.DrawTexture(new Rect(32f + 14f * assetBundleItem.Depth, 20f * m_CurrentAssetBundleRowOnDraw + 1f, 16f, 16f), assetBundleItem.Icon);
            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(26f + 14f * assetBundleItem.Depth), GUILayout.Height(18f));
            EditorGUILayout.LabelField(title);
        }
        EditorGUILayout.EndHorizontal();
        m_CurrentAssetBundleRowOnDraw++;
    }
    private void ChangeSelectedAssetBundle(AssetBundleCollEctionNew.AssetBundle assetBundle)
    {
        if (m_SelectedAssetBundle == assetBundle)
        {
            return;
        }
        m_SelectedAssetBundle = assetBundle;
        m_SelectedAssetsInSelectedAssetBundle.Clear();
    }
    private bool IsExpandedAssetBundleFolder(AssetBundleFolder assetBundleFolder)
    {
        return m_ExpandedAssetBundleFolderNames.Contains(assetBundleFolder.FromRootPath);
    }
    private void SetExpandedAssetBundleFolder(AssetBundleFolder assetBundleFolder, bool expand)
    {
        if (expand)
        {
            m_ExpandedAssetBundleFolderNames.Add(assetBundleFolder.FromRootPath);
        }
        else
        {
            m_ExpandedAssetBundleFolderNames.Remove(assetBundleFolder.FromRootPath);
        }
    }

}