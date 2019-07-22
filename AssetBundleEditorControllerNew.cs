using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
public class AssetBundleEditorControllerNew
{
    
    public sealed class SourceAsset
    {
        private Texture m_CachedIcon;
        public SourceAsset(string guid, string path, string name, SourceFolder folder)
        {
            if (folder == null)
            {
                Debug.LogWarning("Source asset folder is invalid.");
            }
            Guid = guid;
            Path = path;
            Name = name;
            Folder = folder;
            m_CachedIcon = null;
        }
        public string Guid
        {
            get;
            private set;
        }
        public string Path
        {
            get;
            private set;
        }
        public string Name
        {
            get;
            private set;
        }
        public SourceFolder Folder
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
                if (m_CachedIcon == null)
                {
                    m_CachedIcon = AssetDatabase.GetCachedIcon(Path);
                }
                return m_CachedIcon;
            }
        }
    }
    public sealed class SourceFolder
    {
        private static Texture s_CachedIcon = null;
        private readonly List<SourceFolder> m_Folders;
        private readonly List<SourceAsset> m_Assets;
        public SourceFolder(string name, SourceFolder folder)
        {
            m_Folders = new List<SourceFolder>();
            m_Assets = new List<SourceAsset>();
            Name = name;
            Folder = folder;
        }
        public string Name
        {
            get;
            private set;
        }
        public SourceFolder Folder
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
            m_Assets.Clear();
            m_Folders.Clear();
        }
        public SourceFolder[] GetFolders()
        {
            return m_Folders.ToArray();
        }
        public SourceFolder GetFolder(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("Source folder name is invalid." + name);
            }
            foreach (SourceFolder folder in m_Folders)
            {
                if (folder.Name == name)
                {
                    return folder;
                }
            }
            return null;
        }
        public SourceFolder AddFolder(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("Source folder name is invalid." + name);
            }
            SourceFolder folder = GetFolder(name);
            if (folder != null)
            {
                Debug.Log("Source folder is already exist." + name);
            }
            folder = new SourceFolder(name, this);
            m_Folders.Add(folder);
            return folder;
        }
        public SourceAsset[] GetAssets()
        {
            return m_Assets.ToArray();
        }
        public SourceAsset GetAsset(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("Source asset name is invalid." + name);
            }
            foreach (SourceAsset asset in m_Assets)
            {
                if (asset.Name == name)
                {
                    return asset;
                }
            }
            return null;
        }
        public SourceAsset AddAsset(string guid, string path, string name)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning("Source asset guid is invalid." + name);
            }
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Source asset path is invalid." + path);
            }
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("Source asset name is invalid." + name);
            }
            SourceAsset asset = GetAsset(name);
            if (asset != null)
            {
                Debug.LogWarning(string.Format("Source asset '{0}' is already exist.", name));
            }
            asset = new SourceAsset(guid, path, name, this);
            m_Assets.Add(asset);
            return asset;
        }
    }
    public enum AssetSorterType
    {
        Path,
        Name,
        Guid,
    }
    public sealed partial class AssetBundleEditorController
    {
        private const string DefaultSourceAssetRootPath = "Assets";
        private readonly string m_ConfigurationPath;
        private readonly AssetBundleCollEctionNew.AssetBundleCollection m_AssetBundleCollection;
        private readonly List<string> m_SourceAssetSearchPaths;
        private readonly List<string> m_SourceAssetSearchRelativePaths;
        private readonly Dictionary<string, SourceAsset> m_SourceAssets;
        private SourceFolder m_SourceAssetRoot;
        private string m_SourceAssetRootPath;
        private string m_sourceAssetUnionTypeFilter;
        private string m_sourceAssetUnionLabelFilter;
        private string m_sourceAssetExceptTypeFilter;
        private string m_sourceAssetExceptLableFilter;
        private AssetSorterType m_AssetSorter;
        public AssetBundleEditorController()
        {
            m_ConfigurationPath = TsianFramework.Utility.Path.GetCombinePath(Application.dataPath, "App/Configs/AssetBundleEditor.xml");
            m_AssetBundleCollection = new AssetBundleCollEctionNew.AssetBundleCollection();
            m_SourceAssetSearchPaths = new List<string>();
            m_SourceAssetSearchRelativePaths = new List<string>();
            m_SourceAssets = new Dictionary<string, SourceAsset>();
            m_SourceAssetRoot = null;
            m_SourceAssetRootPath = null;
            m_sourceAssetUnionTypeFilter = null;
            m_sourceAssetUnionLabelFilter = null;
            m_sourceAssetExceptLableFilter = null;
            m_sourceAssetExceptTypeFilter = null;
            m_AssetSorter = AssetSorterType.Path;

            SourceAssetRootPath = DefaultSourceAssetRootPath;
            //m_SourceAssetRootPath
        }
        public int AssetBundleCount
        {
            get
            {
                return m_AssetBundleCollection.assetBundlecount;
            }
        }
        public int AssetCount
        {
            get
            {
                return m_AssetBundleCollection.assetcount;
            }
        }
        public SourceFolder SourceAssetRoot
        {
            get
            {
                return m_SourceAssetRoot;
            }
        }
        public string SourceAssetRootPath
        {
            get
            {
                return m_SourceAssetRootPath;
            }
            set
            {
                if (m_SourceAssetRootPath == value)
                {
                    return;
                }
                m_SourceAssetRootPath = value.Replace('\\', '/');
                m_SourceAssetRoot = new SourceFolder(m_SourceAssetRootPath, null);
                RefreshSourceAssetSearchPaths();
            }
        }
        public string SourceAssetUnionTypeFilter
        {
            get
            {
                return m_sourceAssetUnionTypeFilter;
            }
            set
            {
                if (m_sourceAssetUnionTypeFilter == value)
                {
                    return;
                }
                m_sourceAssetUnionTypeFilter = value;
            }
        }
        public string SourceAssetUnionLabelFilter
        {
            get
            {
                return m_sourceAssetUnionLabelFilter;
            }
            set
            {
                if (m_sourceAssetUnionLabelFilter == value)
                {
                    return;
                }
                m_sourceAssetUnionLabelFilter = value;
            }
        }
        public string SourceAssetExceptTypeFilter
        {
            get
            {
                return m_sourceAssetExceptTypeFilter;
            }
            set
            {
                if (m_sourceAssetExceptTypeFilter == value)
                {
                    return;
                }
                m_sourceAssetExceptTypeFilter = value;
            }
        }
        public string SourceAssetExceptLabelFilter
        {
            get
            {
                return m_sourceAssetExceptLableFilter;
            }
            set
            {
                if (m_sourceAssetExceptLableFilter == value)
                {
                    return;
                }

                m_sourceAssetExceptLableFilter = value;
            }
        }
        public AssetSorterType AssetSorter
        {
            get
            {
                return m_AssetSorter;
            }
            set
            {
                if (m_AssetSorter == value)
                {
                    return;
                }
                m_AssetSorter = value;
            }
        }

        public bool Load()
        {
            
            if (!File.Exists(m_ConfigurationPath))
            {
                return false;
            }
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(m_ConfigurationPath);
                XmlNode xmlRoot = xmlDocument.SelectSingleNode("UnityTsianFramework");
                XmlNode xmlEditor = xmlRoot.SelectSingleNode("AssetBundleEditor");
                XmlNode xmlSettings = xmlEditor.SelectSingleNode("Settings");
                XmlNodeList xmlNodeList = null;
                XmlNode xmlNode = null;
                xmlNodeList = xmlSettings.ChildNodes;
                for (int i = 0; i < xmlNodeList.Count; i++)
                {
                    xmlNode = xmlNodeList.Item(i);
                    switch (xmlNode.Name)
                    {
                        case "SourceAssetRootPath":
                            SourceAssetRootPath = xmlNode.InnerText;
                            break;
                        case "SourceAssetSearchPaths":
                            m_SourceAssetSearchRelativePaths.Clear();
                            XmlNodeList xmlNodeListInner = xmlNode.ChildNodes;
                            XmlNode xmlNodeInner = null;
                            for (int j = 0; j < xmlNodeListInner.Count; j++)
                            {
                                xmlNodeInner = xmlNodeListInner.Item(j);
                                if (xmlNodeInner.Name != "SourceAssetSearchPath")
                                {
                                    continue;
                                }
                                m_SourceAssetSearchRelativePaths.Add(xmlNodeInner.Attributes.GetNamedItem("RelativePath").Value);

                            }
                            break;
                        case "SourceAssetUnionTypeFilter":
                            SourceAssetUnionTypeFilter = xmlNode.InnerText;
                            break;
                        case "SourceAssetUnionLabelFilter":
                            SourceAssetUnionLabelFilter = xmlNode.InnerText;
                            break;
                        case "SourceAssetExceptTypeFilter":
                            SourceAssetExceptTypeFilter = xmlNode.InnerText;
                            break;
                        case "SourceAssetExceptLabelFilter":
                            SourceAssetExceptLabelFilter = xmlNode.InnerText;
                            break;
                        case "AssetSorter":
                            AssetSorter = (AssetSorterType)Enum.Parse(typeof(AssetSorterType), xmlNode.InnerText);
                            break;
                    }
                }
                RefreshSourceAssetSearchPaths();
            }
            catch
            {
                File.Delete(m_ConfigurationPath);
                return false;
            }
            
            ScanSourceAssets();
            m_AssetBundleCollection.Load();
            return true;
        }
        public bool Save()
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.AppendChild(xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null));
                XmlElement xmlRoot = xmlDocument.CreateElement("UnityTsianFramework");
                xmlDocument.AppendChild(xmlRoot);
                XmlElement xmleditor = xmlDocument.CreateElement("AssetBundleEditor");
                xmlRoot.AppendChild(xmleditor);
                XmlElement xmlSettings = xmlDocument.CreateElement("Settings");
                xmleditor.AppendChild(xmlSettings);
                XmlElement xmlElement = null;
                XmlAttribute xmlAttribute = null;
                xmlElement = xmlDocument.CreateElement("SourceAssetRootPath");
                xmlElement.InnerText = SourceAssetRootPath.ToString();
                xmlSettings.AppendChild(xmlElement);

                xmlElement = xmlDocument.CreateElement("SourceAssetSearchPaths");
                xmlSettings.AppendChild(xmlElement);
                foreach (string sourceAssetSearchRelativePath in m_SourceAssetSearchRelativePaths)
                {
                    XmlElement xmlElementInner = xmlDocument.CreateElement("SourceAssetSearchPath");
                    xmlAttribute = xmlDocument.CreateAttribute("RelativePath");
                    xmlAttribute.Value = sourceAssetSearchRelativePath;
                    xmlElementInner.Attributes.SetNamedItem(xmlAttribute);
                    xmlElement.AppendChild(xmlElementInner);
                }
                xmlElement = xmlDocument.CreateElement("SourceAssetUnionTypeFilter");
                xmlElement.InnerText = SourceAssetUnionTypeFilter ?? string.Empty;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("SourceAssetUnionLabelFilter");
                xmlElement.InnerText = SourceAssetUnionLabelFilter ?? string.Empty;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("SourceAssetExceptTypeFilter");
                xmlElement.InnerText = SourceAssetExceptTypeFilter ?? string.Empty;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("SourceAssetExceptLabelFilter");
                xmlElement.InnerText = SourceAssetExceptLabelFilter ?? string.Empty;
                xmlSettings.AppendChild(xmlElement);
                xmlElement = xmlDocument.CreateElement("AssetSorter");
                xmlElement.InnerText = AssetSorter.ToString();
                xmlSettings.AppendChild(xmlElement);

                string configurationDirectoryName = Path.GetDirectoryName(m_ConfigurationPath);
                if (!Directory.Exists(configurationDirectoryName))
                {
                    Directory.CreateDirectory(configurationDirectoryName);
                }

                xmlDocument.Save(m_ConfigurationPath);
                AssetDatabase.Refresh();
            }
            catch
            {
                if (File.Exists(m_ConfigurationPath))
                {
                    File.Delete(m_ConfigurationPath);
                }

                return false;
            }
            return m_AssetBundleCollection.Save();
        }
        public AssetBundleCollEctionNew.AssetBundle[] GetAssetBundles()
        {
            return m_AssetBundleCollection.GetAssetBundles();
        }
        public AssetBundleCollEctionNew.AssetBundle GetAssetBundle(string assetBundleName, string assetBundleVariant)
        {
            return m_AssetBundleCollection.GetAssetBundle(assetBundleName, assetBundleVariant);
        }
        public bool HasAssetBundle(string assetBundleName, string assetBundleVariant)
        {
            return m_AssetBundleCollection.HasAssetBundle(assetBundleName, assetBundleVariant);
        }

        public bool AddAssetBundle(string assetBundleName, string assetBundleVariant, AssetBundleCollEctionNew.AssetBundleLoadType assetBundleLoadType, bool assetBundlePacked)
        {
            return m_AssetBundleCollection.AddAssetBundle(assetBundleName, assetBundleVariant, assetBundleLoadType, assetBundlePacked);
        }

        public bool RenameAssetBundle(string oldAssetBundleName, string oldAssetBundleVariant, string newAssetBundleName, string newAssetBundleVariant)
        {
            return m_AssetBundleCollection.RenameAssetBundle(oldAssetBundleName, oldAssetBundleVariant, newAssetBundleName, newAssetBundleVariant);
        }
        public bool RemoveAssetBundle(string assetBundleName, string assetBundleVariant)
        {
            AssetBundleCollEctionNew.Asset[] assetsToRemove = m_AssetBundleCollection.Getassets(assetBundleName, assetBundleVariant);
            if (m_AssetBundleCollection.RemoveAssetBundle(assetBundleName, assetBundleVariant))
            {
                List<SourceAsset> unassignedSourceAssets = new List<SourceAsset>();
                foreach (AssetBundleCollEctionNew.Asset asset in assetsToRemove)
                {
                    SourceAsset sourceAsset = GetSourceAsset(asset.Guid);
                    if (sourceAsset != null)
                    {
                        unassignedSourceAssets.Add(sourceAsset);
                    }
                }



                return true;
            }

            return false;
        }

        public bool SetAssetBundleLoadType(string assetBundleName, string assetBundleVariant,AssetBundleCollEctionNew.AssetBundleLoadType assetBundleLoadType)
        {
            return m_AssetBundleCollection.SetAssetBundleLoadType(assetBundleName, assetBundleVariant, assetBundleLoadType);
        }
        public int RemoveUnusedAssetBundles()
        {
            List<AssetBundleCollEctionNew.AssetBundle> assetBundles = new List<AssetBundleCollEctionNew.AssetBundle>(m_AssetBundleCollection.GetAssetBundles());
            List<AssetBundleCollEctionNew.AssetBundle> removeAssetBundles = assetBundles.FindAll(assetBundle => GetAssets(assetBundle.Name, assetBundle.Variant).Length <= 0);
            foreach (AssetBundleCollEctionNew.AssetBundle assetBundle in removeAssetBundles)
            {
                m_AssetBundleCollection.RemoveAssetBundle(assetBundle.Name, assetBundle.Variant);
            }

            return removeAssetBundles.Count;
        }
        public AssetBundleCollEctionNew.Asset[] GetAssets(string assetBundleName, string assetBundleVariant)
        {
            List<AssetBundleCollEctionNew.Asset> assets = new List<AssetBundleCollEctionNew.Asset>(m_AssetBundleCollection.Getassets(assetBundleName, assetBundleVariant));
            switch (AssetSorter)
            {
                case AssetSorterType.Path:
                    assets.Sort(AssetPathComparer);
                    break;
                case AssetSorterType.Name:
                    assets.Sort(AssetNameComparer);
                    break;
                case AssetSorterType.Guid:
                    assets.Sort(AssetGuidComparer);
                    break;
            }

            return assets.ToArray();
        }
        public AssetBundleCollEctionNew.Asset GetAsset(string assetGuid)
        {
            return m_AssetBundleCollection.GetAsset(assetGuid);
        }

        public bool AssignAsset(string assetGuid, string assetBundleName, string assetBundleVariant)
        {
            if (m_AssetBundleCollection.AssignAsset(assetGuid, assetBundleName, assetBundleVariant))
            {
               

                return true;
            }

            return false;
        }
        public bool UnassignAsset(string assetGuid)
        {
            if (m_AssetBundleCollection.UnassignAsset(assetGuid))
            {
                SourceAsset sourceAsset = GetSourceAsset(assetGuid);
                if (sourceAsset != null)
                {
                    
                }

                return true;
            }

            return false;
        }

        public int RemoveUnknownAssets()
        {
            List<AssetBundleCollEctionNew.Asset> assets = new List<AssetBundleCollEctionNew.Asset>(m_AssetBundleCollection.Getassets());
            List<AssetBundleCollEctionNew.Asset> removeAssets = assets.FindAll(asset => GetSourceAsset(asset.Guid) == null);
            foreach (AssetBundleCollEctionNew.Asset asset in removeAssets)
            {
                m_AssetBundleCollection.UnassignAsset(asset.Guid);
            }

            return removeAssets.Count;
        }
        public SourceAsset GetSourceAsset(string assetGuid)
        {
            if (string.IsNullOrEmpty(assetGuid))
            {
                return null;
            }
              
            SourceAsset sourceAsset = null;
            if (m_SourceAssets.TryGetValue(assetGuid, out sourceAsset))
            {
                return sourceAsset;
            }

            return null;
        }
        public void ScanSourceAssets()
        {
            m_SourceAssets.Clear();
            m_SourceAssetRoot.Clear();

            string[] sourceAssetSearchPaths = m_SourceAssetSearchPaths.ToArray();
            HashSet<string> tempGuids = new HashSet<string>();
            tempGuids.UnionWith(AssetDatabase.FindAssets(SourceAssetUnionTypeFilter, sourceAssetSearchPaths));
            tempGuids.UnionWith(AssetDatabase.FindAssets(SourceAssetUnionLabelFilter, sourceAssetSearchPaths));
            tempGuids.ExceptWith(AssetDatabase.FindAssets(SourceAssetExceptTypeFilter, sourceAssetSearchPaths));
            tempGuids.ExceptWith(AssetDatabase.FindAssets(SourceAssetExceptLabelFilter, sourceAssetSearchPaths));

            string[] assetGuids = new List<string>(tempGuids).ToArray();
            foreach (string assetGuid in assetGuids)
            {
                string fullPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (AssetDatabase.IsValidFolder(fullPath))
                {
                    // Skip folder.
                    continue;
                }

                string assetPath = fullPath.Substring(SourceAssetRootPath.Length + 1);
                string[] splitedPath = assetPath.Split('/');
                SourceFolder folder = m_SourceAssetRoot;
                for (int i = 0; i < splitedPath.Length - 1; i++)
                {
                    SourceFolder subFolder = folder.GetFolder(splitedPath[i]);
                    folder = subFolder == null ? folder.AddFolder(splitedPath[i]) : subFolder;
                }

                SourceAsset asset = folder.AddAsset(assetGuid, fullPath, splitedPath[splitedPath.Length - 1]);
                m_SourceAssets.Add(asset.Guid, asset);
            }
        }
        private void RefreshSourceAssetSearchPaths()
        {
            m_SourceAssetSearchPaths.Clear();
            if (string.IsNullOrEmpty(m_SourceAssetRootPath))
            {
                SourceAssetRootPath = DefaultSourceAssetRootPath;
            }
            if (m_SourceAssetSearchRelativePaths.Count > 0)
            {
                foreach (string sourceAssetSearchRelativePath in m_SourceAssetSearchRelativePaths)
                {
                    m_SourceAssetSearchPaths.Add(TsianFramework.Utility.Path.GetCombinePath(m_SourceAssetRootPath, sourceAssetSearchRelativePath));
                }
            }
            else
            {
                m_SourceAssetSearchPaths.Add(m_SourceAssetRootPath);
            }
        }
        private int AssetPathComparer(AssetBundleCollEctionNew.Asset a, AssetBundleCollEctionNew.Asset b)
        {
            SourceAsset sourceAssetA = GetSourceAsset(a.Guid);
            SourceAsset sourceAssetB = GetSourceAsset(b.Guid);

            if (sourceAssetA != null && sourceAssetB != null)
            {
                return sourceAssetA.Path.CompareTo(sourceAssetB.Path);
            }

            if (sourceAssetA == null && sourceAssetB == null)
            {
                return a.Guid.CompareTo(b.Guid);
            }

            if (sourceAssetA == null)
            {
                return -1;
            }

            if (sourceAssetB == null)
            {
                return 1;
            }

            return 0;
        }
        private int AssetNameComparer(AssetBundleCollEctionNew.Asset a, AssetBundleCollEctionNew.Asset b)
        {
            SourceAsset sourceAssetA = GetSourceAsset(a.Guid);
            SourceAsset sourceAssetB = GetSourceAsset(b.Guid);

            if (sourceAssetA != null && sourceAssetB != null)
            {
                return sourceAssetA.Name.CompareTo(sourceAssetB.Name);
            }

            if (sourceAssetA == null && sourceAssetB == null)
            {
                return a.Guid.CompareTo(b.Guid);
            }

            if (sourceAssetA == null)
            {
                return -1;
            }

            if (sourceAssetB == null)
            {
                return 1;
            }

            return 0;
        }
        private int AssetGuidComparer(AssetBundleCollEctionNew.Asset a, AssetBundleCollEctionNew.Asset b)
        {
            SourceAsset sourceAssetA = GetSourceAsset(a.Guid);
            SourceAsset sourceAssetB = GetSourceAsset(b.Guid);

            if (sourceAssetA != null && sourceAssetB != null || sourceAssetA == null && sourceAssetB == null)
            {
                return a.Guid.CompareTo(b.Guid);
            }

            if (sourceAssetA == null)
            {
                return -1;
            }

            if (sourceAssetB == null)
            {
                return 1;
            }

            return 0;
        }

    }
}