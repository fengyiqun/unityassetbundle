using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
public class AssetBundleEditorControllerNew {

    
    public enum AssetSorterType {
        Path,
        Name,
        Guid,
    }
    public sealed partial class AssetBundleEditorController {
        private const string DefaultSourceAssetRootPath = "Assets";
        private readonly string m_ConfigurationPath;
        private readonly AssetBundleCollEctionNew.AssetBundleCollection m_AssetBundleCollection;
        private readonly List<string> m_SourceAssetSearchPaths;
        private readonly List<string> m_SourceAssetSearchRelativePaths;

        private readonly AssetBundleCollEctionNew.Asset m_assetRoot;
        private string m_SourceAssetRootPath;
        private string m_sourceAssetUnionTypeFilter;
        private string m_sourceAssetUnionLabelFilter;
        private string m_sourceAssetExceptTypeFilter;
        private string m_sourceAssetExceptLableFilter;
        private AssetSorterType m_AssetSorter;
        public AssetBundleEditorController () {
            m_ConfigurationPath = TsianFramework.Utility.Path.GetCombinePath (Application.dataPath, "App/Configs/AssetBundleEditor.xml");
            m_AssetBundleCollection = new AssetBundleCollEctionNew.AssetBundleCollection ();
            m_SourceAssetSearchPaths = new List<string> ();
            m_SourceAssetSearchRelativePaths = new List<string> ();
            m_SourceAssetRootPath = null;
            m_sourceAssetUnionTypeFilter = null;
            m_sourceAssetUnionLabelFilter = null;
            m_sourceAssetExceptLableFilter = null;
            m_sourceAssetExceptTypeFilter = null;
            m_AssetSorter = AssetSorterType.Path;
            string guid = AssetDatabase.GUIDToAssetPath(DefaultSourceAssetRootPath);
            SourceAssetRootPath = DefaultSourceAssetRootPath;

            m_assetRoot = AssetBundleCollEctionNew.Asset.Create (guid, DefaultSourceAssetRootPath);
            //m_SourceAssetRootPath
        }
        public AssetBundleCollEctionNew.AssetBundleInfo GetAssetBundleInfo
        {
            get { return m_AssetBundleCollection.GetAssetBundleInfo; }
            set { m_AssetBundleCollection.GetAssetBundleInfo = value; }
        }
        public int AssetBundleCount {
            get {
                return m_AssetBundleCollection.assetBundlecount;
            }
        }
        public int AssetCount {
            get {
                return m_AssetBundleCollection.assetcount;
            }
        }
       
        public AssetBundleCollEctionNew.Asset AssetRoot
        {
            get
            {
                return m_assetRoot;
            }
        }
        public string SourceAssetRootPath {
            get {
                return m_SourceAssetRootPath;
            }
            set {
                if (m_SourceAssetRootPath == value) {
                    return;
                }
                m_SourceAssetRootPath = value.Replace ('\\', '/');
              
                RefreshSourceAssetSearchPaths ();
            }
        }
        public string SourceAssetUnionTypeFilter {
            get {
                return m_sourceAssetUnionTypeFilter;
            }
            set {
                if (m_sourceAssetUnionTypeFilter == value) {
                    return;
                }
                m_sourceAssetUnionTypeFilter = value;
            }
        }
        public string SourceAssetUnionLabelFilter {
            get {
                return m_sourceAssetUnionLabelFilter;
            }
            set {
                if (m_sourceAssetUnionLabelFilter == value) {
                    return;
                }
                m_sourceAssetUnionLabelFilter = value;
            }
        }
        public string SourceAssetExceptTypeFilter {
            get {
                return m_sourceAssetExceptTypeFilter;
            }
            set {
                if (m_sourceAssetExceptTypeFilter == value) {
                    return;
                }
                m_sourceAssetExceptTypeFilter = value;
            }
        }
        public string SourceAssetExceptLabelFilter {
            get {
                return m_sourceAssetExceptLableFilter;
            }
            set {
                if (m_sourceAssetExceptLableFilter == value) {
                    return;
                }

                m_sourceAssetExceptLableFilter = value;
            }
        }
        public AssetSorterType AssetSorter {
            get {
                return m_AssetSorter;
            }
            set {
                if (m_AssetSorter == value) {
                    return;
                }
                m_AssetSorter = value;
            }
        }

        public bool Load () {

            
            if (!File.Exists (m_ConfigurationPath)) {
                return false;
            }
            try {
                XmlDocument xmlDocument = new XmlDocument ();
                xmlDocument.Load (m_ConfigurationPath);
                XmlNode xmlRoot = xmlDocument.SelectSingleNode ("UnityTsianFramework");
                XmlNode xmlEditor = xmlRoot.SelectSingleNode ("AssetBundleEditor");
                XmlNode xmlSettings = xmlEditor.SelectSingleNode ("Settings");
                XmlNodeList xmlNodeList = null;
                XmlNode xmlNode = null;
                xmlNodeList = xmlSettings.ChildNodes;
                for (int i = 0; i < xmlNodeList.Count; i++) {
                    xmlNode = xmlNodeList.Item (i);
                    switch (xmlNode.Name) {
                        case "SourceAssetRootPath":
                            SourceAssetRootPath = xmlNode.InnerText;
                            break;
                        case "SourceAssetSearchPaths":
                            m_SourceAssetSearchRelativePaths.Clear ();
                            XmlNodeList xmlNodeListInner = xmlNode.ChildNodes;
                            XmlNode xmlNodeInner = null;
                            for (int j = 0; j < xmlNodeListInner.Count; j++) {
                                xmlNodeInner = xmlNodeListInner.Item (j);
                                if (xmlNodeInner.Name != "SourceAssetSearchPath") {
                                    continue;
                                }
                                m_SourceAssetSearchRelativePaths.Add (xmlNodeInner.Attributes.GetNamedItem ("RelativePath").Value);

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
                            AssetSorter = (AssetSorterType) Enum.Parse (typeof (AssetSorterType), xmlNode.InnerText);
                            break;
                    }
                }
                RefreshSourceAssetSearchPaths ();
            } catch {
                File.Delete (m_ConfigurationPath);
                return false;
            }
            
            ScanSourceAssets ();
            m_AssetBundleCollection.Load ();
            
            return true;
        }
        public bool Save () {
            return m_AssetBundleCollection.Save ();
        }
        public AssetBundleCollEctionNew.AssetBundle[] GetAssetBundles () {
            return m_AssetBundleCollection.GetAssetBundles ();
        }
        public AssetBundleCollEctionNew.AssetBundle GetAssetBundle (string assetBundleName, string assetBundleVariant) {
            return m_AssetBundleCollection.GetAssetBundle (assetBundleName, assetBundleVariant);
        }
        public bool HasAssetBundle (string assetBundleName, string assetBundleVariant) {
            return m_AssetBundleCollection.HasAssetBundle (assetBundleName, assetBundleVariant);
        }

        public bool AddAssetBundle (string assetBundleName, string assetBundleVariant) {
            return m_AssetBundleCollection.AddAssetBundle (assetBundleName, assetBundleVariant);
        }

        public bool RenameAssetBundle (string oldAssetBundleName, string oldAssetBundleVariant, string newAssetBundleName, string newAssetBundleVariant) {
            return m_AssetBundleCollection.RenameAssetBundle (oldAssetBundleName, oldAssetBundleVariant, newAssetBundleName, newAssetBundleVariant);
        }
       

        public bool SetAssetBundleLoadType (string assetBundleName, string assetBundleVariant) {
            return m_AssetBundleCollection.SetAssetBundleLoadType (assetBundleName, assetBundleVariant);
        }
        public int RemoveUnusedAssetBundles () {
            List<AssetBundleCollEctionNew.AssetBundle> assetBundles = new List<AssetBundleCollEctionNew.AssetBundle> (m_AssetBundleCollection.GetAssetBundles ());
            List<AssetBundleCollEctionNew.AssetBundle> removeAssetBundles = assetBundles.FindAll (assetBundle => GetAssets (assetBundle.Name, assetBundle.Variant).Length <= 0);
            foreach (AssetBundleCollEctionNew.AssetBundle assetBundle in removeAssetBundles) {
                m_AssetBundleCollection.RemoveAssetBundle (assetBundle.Name, assetBundle.Variant);
            }

            return removeAssetBundles.Count;
        }
        public AssetBundleCollEctionNew.Asset[] GetAssets (string assetBundleName, string assetBundleVariant) {
            List<AssetBundleCollEctionNew.Asset> assets = new List<AssetBundleCollEctionNew.Asset> (m_AssetBundleCollection.Getassets (assetBundleName, assetBundleVariant));          
            return assets.ToArray ();
        }
        public AssetBundleCollEctionNew.Asset GetAsset (string assetGuid) {
            return m_AssetBundleCollection.GetAsset (assetGuid);
        }

        public bool AssignAsset (string assetGuid, string assetBundleName, string assetBundleVariant) {
            if (m_AssetBundleCollection.AssignAsset (assetGuid, assetBundleName, assetBundleVariant)) {

                return true;
            }

            return false;
        }
       
        public bool UnassignAsset(string assetGuid)
        {
            if (m_AssetBundleCollection.UnassignAsset(assetGuid))
            {
                return true;
            }
            return false;
        }
        
        public void ScanSourceAssets () {
           
            string[] sourceAssetSearchPaths = m_SourceAssetSearchPaths.ToArray ();
            HashSet<string> tempGuids = new HashSet<string> ();
            tempGuids.UnionWith (AssetDatabase.FindAssets (SourceAssetUnionTypeFilter, sourceAssetSearchPaths));
            tempGuids.UnionWith (AssetDatabase.FindAssets (SourceAssetUnionLabelFilter, sourceAssetSearchPaths));
            tempGuids.ExceptWith (AssetDatabase.FindAssets (SourceAssetExceptTypeFilter, sourceAssetSearchPaths));
            tempGuids.ExceptWith (AssetDatabase.FindAssets (SourceAssetExceptLabelFilter, sourceAssetSearchPaths));

            string[] assetGuids = new List<string> (tempGuids).ToArray ();
            foreach (string assetguid in assetGuids) {
                string fullPath = AssetDatabase.GUIDToAssetPath (assetguid);
                if (AssetDatabase.IsValidFolder (fullPath)) {
                    continue;
                }
                string assetPath = fullPath.Substring (SourceAssetRootPath.Length + 1);
                string[] splitedpath = assetPath.Split ('/');
                AssetBundleCollEctionNew.Asset assetroot = m_assetRoot;
                for (int i = 0; i < splitedpath.Length; i++) {

                    AssetBundleCollEctionNew.Asset subasset = assetroot.GetAsset (splitedpath[i]);
                    if (subasset == null) {
                        assetroot = assetroot.AddAsset (splitedpath[i], assetguid);
                    } else {
                        assetroot = subasset;
                    }
                }
            }
        }
        private void RefreshSourceAssetSearchPaths () {
            m_SourceAssetSearchPaths.Clear ();
            if (string.IsNullOrEmpty (m_SourceAssetRootPath)) {
                SourceAssetRootPath = DefaultSourceAssetRootPath;
            }
            if (m_SourceAssetSearchRelativePaths.Count > 0) {
                foreach (string sourceAssetSearchRelativePath in m_SourceAssetSearchRelativePaths) {
                    m_SourceAssetSearchPaths.Add (TsianFramework.Utility.Path.GetCombinePath (m_SourceAssetRootPath, sourceAssetSearchRelativePath));
                }
            } else {
                m_SourceAssetSearchPaths.Add (m_SourceAssetRootPath);
            }
        }
        

    }
}