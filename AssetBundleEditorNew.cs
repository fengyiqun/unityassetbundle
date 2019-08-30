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

    private sealed class AssetBundleItem
    {
        private static Texture s_CachedUnknownIcon = null;
        private static Texture s_CachedIcon = null;
        private static Texture s_CachedAssetIcon = null;
        private static Texture s_CachedSceneIcon = null;

        private readonly List<AssetBundleItem> m_Items;
        public AssetBundleItem(string name, AssetBundleCollEctionNew.AssetBundle assetBundle,AssetBundleItem item)
        {
            if(assetBundle != null)
            {
                AssetBundle = assetBundle;
            }
            if (item != null)
            {
                Folder = item;
            }
            Name = name;
            m_Items = new List<AssetBundleItem>();

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
        public AssetBundleItem Folder
        {
            get;
            private set;
        }
        public string FromRootPath
        {
            get
            {
                return (Folder == null ? Name : string.Format("{0}/{1}", Folder.FromRootPath, Name));
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
            get {
                if (AssetBundle  ==  null)
                {
                    return CachedAssetFolderIcon;
                }
                else
                {
                    if(AssetBundle.GetAssets().Length> 0)
                    {
                        return CachedAssetIcon;
                    }
                    else
                    {
                        return CachedUnknownIcon;
                    }
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
        private static Texture CachedAssetFolderIcon
        {
            get
            {
                if(s_CachedIcon == null)
                {
                    s_CachedIcon = AssetDatabase.GetCachedIcon("Assets");
                }
                return s_CachedIcon;
            }
        }

        public void Clear()
        {
            m_Items.Clear();
        }
        public AssetBundleItem[] GetFolders()
        {
            List<AssetBundleItem> folder = new List<AssetBundleItem>();
            for(int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i].AssetBundle == null)
                {
                    folder.Add(m_Items[i]);
                }
            }
            return folder.ToArray();
        }
        public AssetBundleItem GetFolder(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("AssetBundle folder name is invalid.");
            }
            foreach(AssetBundleItem folder in m_Items)
            {
                if(folder.Name == name&&folder.AssetBundle == null)
                {
                    return folder;
                }
            }
            return null;
        }
        public AssetBundleItem AddFolder(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("AssetBundle folder name is invalid.");
            }
            AssetBundleItem folder = GetFolder(name);
            if(folder!= null)
            {
                Debug.LogWarning("AssetBundle folder is already exist." + name);
            }
            folder = new AssetBundleItem(name, null, this);
            m_Items.Add(folder);
            return folder;
        }
        public AssetBundleItem[] GetItems()
        {
            List<AssetBundleItem> items = new List<AssetBundleItem>();
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i].AssetBundle != null)
                {
                    items.Add(m_Items[i]);
                }
            }
            return items.ToArray();
        }
        public AssetBundleItem GetItem(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("AssetBundle folder name is invalid.");
            }
            foreach (AssetBundleItem folder in m_Items)
            {
                if (folder.Name == name && folder.AssetBundle != null)
                {
                    return folder;
                }
            }
            return null;
        }
        public void AddItem(string name, AssetBundleCollEctionNew.AssetBundle assetBundle)
        {
            AssetBundleItem item = GetItem(name);
            if(item != null)
            {
                Debug.LogWarning("AssetBundle item is already exist.");
            }
            item = new AssetBundleItem(name, assetBundle, this);
            m_Items.Add(item);
            m_Items.Sort(AssetBundleItemComparer);
        }
        private int AssetBundleItemComparer(AssetBundleItem a,AssetBundleItem b)
        {
            return a.Name.CompareTo(b.Name);
        }

    }



   
    private AssetBundleEditorControllerNew.AssetBundleEditorController m_AssetBundleColledtion;
    private MenuState m_MenuState = MenuState.Normal;
    private AssetBundleCollEctionNew.AssetBundle m_SelectedAssetBundle = null;
    

    private AssetBundleItem m_AssetbundleItemRoot = null;


    private HashSet<string> m_ExpandedAssetBundleFolderNames = null;
    private HashSet<AssetBundleCollEctionNew.Asset> m_SelectedAssetsInSelectedAssetBundle = null;

    private HashSet <AssetBundleCollEctionNew.Asset> m_ExpandedSourceAssets = null;
    private HashSet<AssetBundleCollEctionNew.Asset>m_SelectedAssets = null;
    private Texture m_MissingSourceAssetIcon = null;
    private HashSet <AssetBundleCollEctionNew.Asset> m_CachedSelectedSourceAssets = null;
    private HashSet <AssetBundleCollEctionNew.Asset> m_CachedUnSelectedSourceAssets = null;
    private HashSet <AssetBundleCollEctionNew.Asset> m_Cachedassignedassets = null;
    private HashSet <AssetBundleCollEctionNew.Asset> m_CachedUnassignedassets = null;


    private AssetBundleCollEctionNew.AssetBundleInfo assetbundleinfo;

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
        m_AssetbundleItemRoot = new AssetBundleItem("AssetBundles", null, null);
        m_ExpandedAssetBundleFolderNames = new HashSet<string>();
        m_SelectedAssetsInSelectedAssetBundle = new HashSet<AssetBundleCollEctionNew.Asset>();
        m_MissingSourceAssetIcon = EditorGUIUtility.IconContent("console.warnicon.sml").image;
        
        m_Cachedassignedassets = new HashSet<AssetBundleCollEctionNew.Asset>();
        m_CachedUnassignedassets = new HashSet<AssetBundleCollEctionNew.Asset>();
        m_ExpandedSourceAssets = new HashSet<AssetBundleCollEctionNew.Asset>();
        m_CachedSelectedSourceAssets = new HashSet<AssetBundleCollEctionNew.Asset>();
        m_CachedUnSelectedSourceAssets = new HashSet<AssetBundleCollEctionNew.Asset>();
        m_SelectedAssets = new HashSet<AssetBundleCollEctionNew.Asset>();
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
        assetbundleinfo = m_AssetBundleColledtion.GetAssetBundleInfo;
        EditorUtility.DisplayProgressBar("Prepare AssetBundle Editor", "Processing...", 0f);
        RefreshAssetBundleItemTree();
        EditorUtility.ClearProgressBar();
    }
   
   
    private void RefreshAssetBundleItemTree()
    {
        m_AssetbundleItemRoot.Clear();
        AssetBundleCollEctionNew.AssetBundle[] assetbundles = m_AssetBundleColledtion.GetAssetBundles();
        foreach(var assetbundle in assetbundles)
        {
            string[] splitedpath = assetbundle.Name.Split('/');
            AssetBundleItem folder = m_AssetbundleItemRoot;
            for(int i = 0; i < splitedpath.Length - 1; i++)
            {
                AssetBundleItem subfolder = folder.GetFolder(splitedpath[i]);
                folder = subfolder == null ? folder.AddFolder(splitedpath[i]) : subfolder;
            }
            string assetBundlefullName = splitedpath[splitedpath.Length - 1];
            folder.AddItem(assetBundlefullName, assetbundle);
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
                EditorGUILayout.LabelField(string.Format("AssetBundle List {0}", assetbundleinfo.AssetBundles.Count.ToString()), EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal("box", GUILayout.Height(position.height - 52f));
                {
                    DrawAssetBundlesView();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(5f);
                    DrawAssetBundlesMenu();
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
                    DrawSourceAssetsViewNew();
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

    private void DrawAssetBundleMenu()
    {
        if (GUILayout.Button("All", GUILayout.Width(50f)) && m_SelectedAssetBundle != null)
        {
            AssetBundleCollEctionNew.Asset[] assets = m_AssetBundleColledtion.GetAssets(m_SelectedAssetBundle.Name, m_SelectedAssetBundle.Variant);
            foreach (AssetBundleCollEctionNew.Asset asset in assets)
            {
                SetSelectedAssetInSelectedAssetBundle(asset, true);
            }
        }
        if (GUILayout.Button("None", GUILayout.Width(50f)))
        {
            m_SelectedAssetsInSelectedAssetBundle.Clear();
        }
        m_AssetBundleColledtion.AssetSorter = (AssetBundleEditorControllerNew.AssetSorterType)EditorGUILayout.EnumPopup(m_AssetBundleColledtion.AssetSorter, GUILayout.Width(60f));
        GUILayout.Label(string.Empty);
        EditorGUI.BeginDisabledGroup(m_SelectedAssetBundle == null || m_SelectedAssetsInSelectedAssetBundle.Count <= 0);
        {
            if (GUILayout.Button(string.Format("{0} >>", m_SelectedAssetsInSelectedAssetBundle.Count.ToString()), GUILayout.Width(80f)))
            {
                foreach (AssetBundleCollEctionNew.Asset asset in m_SelectedAssetsInSelectedAssetBundle)
                {
                    UnassignAsset(asset);
                }
                m_SelectedAssetsInSelectedAssetBundle.Clear();
            }
        }
        EditorGUI.EndDisabledGroup();
    }
    private void UnassignAsset(AssetBundleCollEctionNew.Asset asset)
    {
        if (!m_AssetBundleColledtion.UnassignAsset(asset.Guid))
        {
            Debug.LogWarning(string.Format("Unassign asset '{0}' from AssetBundle '{1}' failure.", asset.Guid, m_SelectedAssetBundle.FullName));
        }
    }
    private void DrawSourceAssetsMenu()
    {
        HashSet<AssetBundleCollEctionNew.Asset> selectedSourceAssets = GetSelectedSourceAssets();
        EditorGUI.BeginDisabledGroup(m_SelectedAssetBundle == null || selectedSourceAssets.Count <= 0);
        {
            if (GUILayout.Button(string.Format("<< {0}", selectedSourceAssets.Count.ToString(), GUILayout.Width(80f))))
            {
                foreach (AssetBundleCollEctionNew.Asset sourceAsset in selectedSourceAssets)
                {
                    AssignAsset(sourceAsset, m_SelectedAssetBundle);
                }
                m_SelectedAssets.Clear();
            }
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(selectedSourceAssets.Count <= 0);
        {
            if (GUILayout.Button(string.Format("<<< {0}", selectedSourceAssets.Count.ToString()), GUILayout.Width(80f)))
            {
                int index = 0;
                int count = selectedSourceAssets.Count;
                foreach (AssetBundleCollEctionNew.Asset sourceAsset in selectedSourceAssets)
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
                RefreshAssetBundleItemTree();
                EditorUtility.ClearProgressBar();
                m_SelectedAssets.Clear();
            }
        }
        EditorGUI.EndDisabledGroup();
        bool hideAssignedSourceAssets = EditorGUILayout.ToggleLeft("Hide Assigned", m_HideAssignedSourceAssets, GUILayout.Width(100f));
        if (hideAssignedSourceAssets != m_HideAssignedSourceAssets)
        {
            m_HideAssignedSourceAssets = hideAssignedSourceAssets;
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
            SaveConfigurationNew();
            EditorUtility.ClearProgressBar();
        }
    }
    HashSet<AssetBundleCollEctionNew.Asset> selectassets = new HashSet<AssetBundleCollEctionNew.Asset>();
    private void SaveConfigurationNew()
    {
        SaveConfiguration();
    }
    private bool setselectassets(AssetBundleCollEctionNew.Asset asset)
    {
        if(asset.assetparent != null)
        {
            if (!m_CachedSelectedSourceAssets.Contains(asset))
            {
                return false;
            }
            else
            {
               if(!setselectassets(asset.assetparent))
                {
                    selectassets.Add(asset);
                    return true;
                }

            }
        }
        return false;
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
      
        
    }
    private void AssignAsset(AssetBundleCollEctionNew.Asset sourceAsset, AssetBundleCollEctionNew.AssetBundle assetBundle)
    {
        if (!m_AssetBundleColledtion.AssignAsset(sourceAsset.Guid, assetBundle.Name, assetBundle.Variant))
        {
            Debug.LogWarning(string.Format("Assign asset '{0}' to AssetBundle '{1}' failure.", sourceAsset.Name, m_SelectedAssetBundle.FullName));
        }
    }
    private HashSet<AssetBundleCollEctionNew.Asset> GetSelectedSourceAssets()
    {
        if (!m_HideAssignedSourceAssets)
        {
            return m_SelectedAssets;
        }
        HashSet<AssetBundleCollEctionNew.Asset> selectedUnassignedSourceAssets = new HashSet<AssetBundleCollEctionNew.Asset>();
        foreach (AssetBundleCollEctionNew.Asset sourceAsset in m_SelectedAssets)
        {
            if (!IsAssignedSourceAsset(sourceAsset))
            {
                selectedUnassignedSourceAssets.Add(sourceAsset);
            }
        }
        return selectedUnassignedSourceAssets;
    }
   
    private void DrawSourceAssetsViewNew()
    {
        m_CurrentSourceRowOnDraw = 0;
        m_SourceAssetsViewScroll = EditorGUILayout.BeginScrollView(m_SourceAssetsViewScroll);
        {
            DrawSourceAsset(m_AssetBundleColledtion.AssetRoot);
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawSourceAsset(AssetBundleCollEctionNew.Asset sourceasset)
    {
        if (m_HideAssignedSourceAssets && IsAssignedAsset(sourceasset))
        {
            return;
        }
        float layoutwidth = 0;
        float spacewidth = 0;
        if (sourceasset.IsFolder)
        {
            layoutwidth = 12f + 14f * sourceasset.Depth;
            spacewidth = -14f * sourceasset.Depth;
        }
        else
        {
            float emptySpace = position.width;
            layoutwidth = emptySpace - 12f;
            spacewidth = -emptySpace + 24f;
        }

        bool expand = IsExpandedSourceAsset(sourceasset);
        EditorGUILayout.BeginHorizontal();
        {
            bool select = IsSelectedSourceAsset(sourceasset);
            if (select != EditorGUILayout.Toggle(select, GUILayout.Width(layoutwidth)))
            {
                
                select = !select;
                SetSelectedSouceAsset(sourceasset, select);
                
            }
            GUILayout.Space(spacewidth);
            if (sourceasset.IsFolder)
            {
                if (expand != EditorGUI.Foldout(new Rect(18f + 14f * sourceasset.Depth, 20f * m_CurrentSourceRowOnDraw + 2f, int.MaxValue, 14f), expand, string.Empty, true))
                {
                    
                    expand = !expand;
                    SetExpandedSourceAsset(sourceasset, expand);
                    
                }
            }
            GUI.DrawTexture(new Rect(32f + 14f * sourceasset.Depth, 20f * m_CurrentSourceRowOnDraw + 1f, 16f, 16f), sourceasset.Icon);
            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(26f + 14f * sourceasset.Depth), GUILayout.Height(18f));
            EditorGUILayout.LabelField(sourceasset.Name);
            
            if (!sourceasset.IsFolder)
            {
                AssetBundleCollEctionNew.Asset asset = m_AssetBundleColledtion.GetAsset(sourceasset.Guid);
                EditorGUILayout.LabelField(asset != null?GetAssetBundleFullName(asset.AssetBundle.Name, asset.AssetBundle.Variant):string.Empty,GUILayout.Width(position.width*0.15f));
            }
        }
        EditorGUILayout.EndHorizontal();
        
        m_CurrentSourceRowOnDraw++;
        
        if (expand)
        {
            foreach (AssetBundleCollEctionNew.Asset subasset in sourceasset.GetAssets())
            {
                DrawSourceAsset(subasset);
            }
        }
    }
    private bool IsExpandedSourceAsset(AssetBundleCollEctionNew.Asset sourceAsset)
    {
        return m_ExpandedSourceAssets.Contains(sourceAsset);
    }
   
    private void SetExpandedSourceAsset(AssetBundleCollEctionNew.Asset sourceAsset,bool expand){
        if(expand){
            m_ExpandedSourceAssets.Add(sourceAsset);
        }else{
            m_ExpandedSourceAssets.Remove(sourceAsset);
        }
    }

   
    private void SetSelectedSouceAsset(AssetBundleCollEctionNew.Asset sourceAsset, bool select)
    {
        if (select)
        {
            if (!sourceAsset.IsFolder)
            {
                m_SelectedAssets.Add(sourceAsset);
            }

            m_CachedSelectedSourceAssets.Add(sourceAsset);
            m_CachedUnSelectedSourceAssets.Remove(sourceAsset);
            AssetBundleCollEctionNew.Asset asset = sourceAsset;
            while (asset != null)
            {
                m_CachedUnSelectedSourceAssets.Remove(asset);
                asset = asset.assetparent;
            }
        }
        else
        {
            if (!sourceAsset.IsFolder)
            {
                m_SelectedAssets.Remove(sourceAsset);
            }
            m_CachedSelectedSourceAssets.Remove(sourceAsset);
            m_CachedUnSelectedSourceAssets.Add(sourceAsset);
            AssetBundleCollEctionNew.Asset asset = sourceAsset;
            while (asset != null)
            {
                m_CachedSelectedSourceAssets.Remove(asset);
                asset = asset.assetparent;
            }
        }
        foreach (AssetBundleCollEctionNew.Asset asset in sourceAsset.GetAssets())
        {
            if (m_HideAssignedSourceAssets && IsAssignedAsset(asset))
            {
                continue;
            }
            SetSelectedSouceAsset(asset, select);
        }
    }
   
   
    private bool IsSelectedSourceAsset(AssetBundleCollEctionNew.Asset sourceAsset){
        if(m_CachedSelectedSourceAssets.Contains(sourceAsset)){
            return true;
        }
        if(m_CachedUnSelectedSourceAssets.Contains(sourceAsset)){
            return false;
        }
        if (sourceAsset.IsFolder)
        {
            foreach (AssetBundleCollEctionNew.Asset subSourceAsset in sourceAsset.GetAssets())
            {
                if (m_HideAssignedSourceAssets && IsAssignedSourceAsset(subSourceAsset))
                {
                    continue;
                }
                if (!m_SelectedAssets.Contains(subSourceAsset))
                {
                    m_CachedUnSelectedSourceAssets.Add(sourceAsset);
                    return false;
                }
            }
        }
        else
        {
            if (!m_SelectedAssets.Contains(sourceAsset))
            {
                m_CachedUnSelectedSourceAssets.Add(sourceAsset);
                return false;
            }
        }
        m_CachedSelectedSourceAssets.Add(sourceAsset);
        return true;
    }
   
    private bool IsAssignedAsset(AssetBundleCollEctionNew.Asset sourceAsset){
        if(m_Cachedassignedassets.Contains(sourceAsset)){
            return true;
        }
        if(m_CachedUnassignedassets.Contains(sourceAsset)){
            return false;
        }
        if (sourceAsset.IsFolder)
        {
            foreach (AssetBundleCollEctionNew.Asset asset in sourceAsset.GetAssets())
            {
                if (!IsAssignedAsset(asset))
                {
                    m_CachedUnassignedassets.Add(asset);
                    return false;
                }
            }
        }
        else
        {
            if (!IsAssignedAsset(sourceAsset))
            {
                m_CachedUnassignedassets.Add(sourceAsset);
                return false;
            }
        }
        m_Cachedassignedassets.Add(sourceAsset);
        return true;
    }
    private bool IsAssignedSourceAsset(AssetBundleCollEctionNew.Asset sourceAsset){
        if(m_Cachedassignedassets.Contains(sourceAsset)){
            return true;
        }
        if(m_CachedUnassignedassets.Contains(sourceAsset)){
            return false;
        }
        if (sourceAsset.IsFolder)
        {
            foreach(AssetBundleCollEctionNew.Asset asset in sourceAsset.GetAssets())
            {
               if(!IsAssignedSourceAsset(asset))
               {
                    return false;
               }
            }
            return true;
        }
       
        return m_AssetBundleColledtion.GetAsset(sourceAsset.Guid) != null;
    }
    private void DrawAssetBundlesMenu()
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
            RefreshAssetBundleItemTree();
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
       
    }
    private void AddAssetBundle(string assetBundleName, string assetBundleVariant, bool refresh)
    {
        if (assetBundleVariant == string.Empty)
        {
            assetBundleVariant = null;
        }
        string assetBundleFullName = GetAssetBundleFullName(assetBundleName, assetBundleVariant);
        if (m_AssetBundleColledtion.AddAssetBundle(assetBundleName, assetBundleVariant))
        {
            if (refresh)
            {
                RefreshAssetBundleItemTree();
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
                    AssetBundleCollEctionNew.Asset sourceAsset = m_AssetBundleColledtion.GetAsset(asset.Guid);
                    string assetName = sourceAsset.Name;
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
            DrawAssetBundleFolder(m_AssetbundleItemRoot);
        }
        EditorGUILayout.EndScrollView();
    }
    private void DrawAssetBundleFolder(AssetBundleItem assetBundleFolder)
    {
        bool expand = IsExpandedAssetBundleFolder(assetBundleFolder);
        EditorGUILayout.BeginHorizontal();
        {
            if (expand != EditorGUI.Foldout(new Rect(18f + 14f * assetBundleFolder.Depth, 20f * m_CurrentAssetBundleRowOnDraw + 2f, int.MaxValue, 14f), expand, string.Empty, true))
            {
                expand = !expand;
                SetExpandedAssetBundleFolder(assetBundleFolder, expand);
            }
            GUI.DrawTexture(new Rect(32f + 14f * assetBundleFolder.Depth, 20f * m_CurrentAssetBundleRowOnDraw + 1f, 16f, 16f), assetBundleFolder.Icon);
            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(40f + 14f * assetBundleFolder.Depth), GUILayout.Height(18f));
            EditorGUILayout.LabelField(assetBundleFolder.Name);
        }
        EditorGUILayout.EndHorizontal();

        m_CurrentAssetBundleRowOnDraw++;
        if (expand)
        {
            foreach (AssetBundleItem subAssetBundleFolder in assetBundleFolder.GetFolders())
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
    private bool IsExpandedAssetBundleFolder(AssetBundleItem assetBundleFolder)
    {
        return m_ExpandedAssetBundleFolderNames.Contains(assetBundleFolder.FromRootPath);
    }
    private void SetExpandedAssetBundleFolder(AssetBundleItem assetBundleFolder, bool expand)
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