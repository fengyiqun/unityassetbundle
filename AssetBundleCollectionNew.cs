using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
public class AssetBundleCollEctionNew {

    public sealed class Asset {
        readonly List<Asset> m_Assets = new List<Asset> ();
        private Texture m_CachedIcon;
        private Asset (string guid, Asset asset, AssetBundle assetbundle) {
            Guid = guid;
            AssetBundle = assetbundle;
            m_Assets = new List<Asset> ();
            assetparent = asset;
        }

        public Asset assetparent{
            get;
            private set;
        }
        public bool IsFolder
        {
            get
            {
                if(m_Assets.Count> 0 )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void AddAsset (Asset asset) {
            m_Assets.Add (asset);
        }
        public Asset AddAsset(string name){
            if(string.IsNullOrEmpty(name)){
                Debug.LogWarning("Asset name is invalid");
            }
            Asset asset = GetAsset(name);
            if (asset!= null){
                Debug.LogWarning("asset is already exist"+name);
            }
            asset = Asset.Create(name,this);
            m_Assets.Add(asset);
            return asset;
        }
        public Asset[] GetAssets () {
            return m_Assets.ToArray ();
        }
        public void Clear () {
            m_Assets.Clear ();
        }
        public string Guid {
            get;
            private set;
        }
        public string FromRootPath{
            get{
                return assetparent == null ? string.Empty:(assetparent.assetparent == null ? Guid :string.Format("{0}/{1}",assetparent.FromRootPath,Guid));
            }
        }
        public int Depth{
            get{
                return assetparent != null ? assetparent.Depth + 1:0;
            }
        }
        public string Name {
            get {
                return AssetDatabase.GUIDToAssetPath (Guid);
            }
        }
        public AssetBundle AssetBundle {
            get;
            private set;
        }
        public Asset GetAsset(string guid){
            if(string.IsNullOrEmpty(guid)){
                Debug.Log("asset name is invalid");
            }
            foreach(Asset asset in  m_Assets){
                if(asset.Guid == guid)
                {
                    return asset;
                }
            }
            return null;
        }
        public static Asset Create (string guid) {
            return new Asset (guid,null, null);
        }
         public static Asset Create (string guid,Asset asset) {
            return new Asset (guid,asset, null);
        }
        public static Asset Create (string guid,Asset asset, AssetBundle assetBundle) {
            return new Asset (guid,asset, assetBundle);
        }
        public void SetAssetBundle (AssetBundle assetBundle) {
            AssetBundle = assetBundle;
        }
        public Texture Icon
        {
            get
            {
                if(m_CachedIcon == null)
                {
                    if (m_Assets.Count <= 0)
                    {
                        string iconpath = "Assets/" + FromRootPath;
                        m_CachedIcon = AssetDatabase.GetCachedIcon(iconpath);
                    }
                    else
                    {
                        m_CachedIcon = AssetDatabase.GetCachedIcon("Assets");
                    }
                }
                return m_CachedIcon;
            }
        }


    }

    public sealed class AssetBundle {
        private readonly List<Asset> m_Assets;
        private AssetBundle (string name, string variant) {
            m_Assets = new List<Asset> ();
            Name = name;
            Variant = variant;
        }
        public string Name {
            get;
            private set;
        }
        public string Variant {
            get;
            private set;
        }
        public string FullName {
            get {
                return Variant != null ? string.Format ("{0}.{1}", Name, Variant) : Name;
            }
        }

        public bool Packed {
            get;
            private set;
        }
        public static AssetBundle Create (string name, string variant) {
            return new AssetBundle (name, variant);
        }
        public Asset[] GetAssets () {
            return m_Assets.ToArray ();
        }
        public void Rename (string name, string variant) {
            Name = name;
            Variant = variant;
        }

        public void SetPacked (bool packed) {
            Packed = packed;
        }
        public void AssignAsset (Asset asset) {
            if (asset.AssetBundle != null) {
                asset.AssetBundle.Unassign (asset);
            }
            asset.SetAssetBundle (this);
            m_Assets.Add (asset);
            m_Assets.Sort (AssetComparer);
        }
        public void Unassign (Asset asset) {
            asset.SetAssetBundle (null);
            m_Assets.Remove (asset);

        }
        public void Clear () {
            foreach (Asset asset in m_Assets) {
                asset.SetAssetBundle (null);
            }
            m_Assets.Clear ();
        }
        private int AssetComparer (Asset a, Asset b) {
            return a.Guid.CompareTo (b.Guid);
        }
    }
    public class AssetBundleCollection {
        private const string AssetBundleNamePattern = @"^([A-Za-z0-9\._-]+/)*[A-Za-z0-9\._-]+$";
        private const string AssetBundleVariantPattern = @"^[a-z0-9_-]+$";
        private const string PostfixOfScene = ".unity";
        private static string m_ConfigurationPath = "";
        private SortedDictionary<string, AssetBundle> m_AssetBundles;
        private SortedDictionary<string, Asset> m_Assets;
        public AssetBundleCollection () {
            m_ConfigurationPath = TsianFramework.Utility.Path.GetCombinePath (Application.dataPath, "App/Configs/AssetBundleCollection.xml");
            m_AssetBundles = new SortedDictionary<string, AssetBundle> ();
            m_Assets = new SortedDictionary<string, Asset> ();
        }
        public int assetBundlecount {
            get {
                return m_AssetBundles.Count;
            }
        }
        public int assetcount {
            get {
                return m_Assets.Count;
            }
        }
        public void Clear () {
            m_AssetBundles.Clear ();
            m_Assets.Clear ();
        }
        public bool Load () {
            Clear ();
            if (!System.IO.File.Exists (m_ConfigurationPath)) {
                return false;
            }
            try {
                System.Xml.XmlDocument xmlDocument = new System.Xml.XmlDocument ();
                xmlDocument.Load (m_ConfigurationPath);
                System.Xml.XmlNode xmlRoot = xmlDocument.SelectSingleNode ("UnityTsianFramework");
                System.Xml.XmlNode xmlCollection = xmlRoot.SelectSingleNode ("AssetBundleCollection");
                System.Xml.XmlNode xmlAssetBundles = xmlCollection.SelectSingleNode ("AssetBundles");
                System.Xml.XmlNode xmlAssets = xmlCollection.SelectSingleNode ("Assets");

                System.Xml.XmlNodeList xmlNodeList = null;
                System.Xml.XmlNode xmlNode = null;
                int count = 0;
                xmlNodeList = xmlAssetBundles.ChildNodes;
                count = xmlNodeList.Count;
                for (int i = 0; i < count; i++) {
                    xmlNode = xmlNodeList.Item (i);
                    string assetBundleName = xmlNode.Attributes.GetNamedItem ("Name").Value;
                    string assetBundleVariant = xmlNode.Attributes.GetNamedItem ("Variant") != null ? xmlNode.Attributes.GetNamedItem ("Variant").Value : null;
                    int assetBundleLoadType = 0;
                    if (xmlNode.Attributes.GetNamedItem ("LoadType") != null) {
                        int.TryParse (xmlNode.Attributes.GetNamedItem ("LoadType").Value, out assetBundleLoadType);
                    }
                    bool assetbundlePacked = false;
                    if (xmlNode.Attributes.GetNamedItem ("Packed") != null) {
                        bool.TryParse (xmlNode.Attributes.GetNamedItem ("Packed").Value, out assetbundlePacked);
                    }
                    if (!AddAssetBundle (assetBundleName, assetBundleVariant)) {
                        string assetBundleFullName = assetBundleVariant != null ? string.Format ("{0}.{1}", assetBundleName, assetBundleVariant) : assetBundleName;
                        Debug.LogWarning (string.Format ("Can not add AssetBundle '{0}'.", assetBundleFullName));
                        continue;
                    }
                }
                xmlNodeList = xmlAssets.ChildNodes;
                count = xmlNodeList.Count;
                for (int i = 0; i < count; i++) {
                    xmlNode = xmlNodeList.Item (i);
                    if (xmlNode.Name != "Asset") {
                        continue;
                    }
                    string assetGuid = xmlNode.Attributes.GetNamedItem ("Guid").Value;
                    assetGuid = AssetDatabase.AssetPathToGUID (assetGuid);
                    string assetBundleName = xmlNode.Attributes.GetNamedItem ("AssetBundleName").Value;
                    string assetbundlevariant = xmlNode.Attributes.GetNamedItem ("AssetBundleVariant") != null ? xmlNode.Attributes.GetNamedItem ("AssetBundleVariant").Value : null;
                    if (!AssignAsset (assetGuid, assetBundleName, assetbundlevariant)) {
                        string assetBundleFullName = assetbundlevariant != null ? string.Format ("{0}.{1}", assetBundleName, assetbundlevariant) : assetBundleName;
                        Debug.LogWarning (string.Format ("Can not assign asset '{0}' to AssetBundle '{1}'.", assetGuid, assetBundleFullName));
                        continue;
                    }
                }
                return true;
            } catch {
                Debug.LogWarning ("LoadAssetBundleCollent ");
                System.IO.File.Delete (m_ConfigurationPath);

                return false;
            }

        }
        public bool Save () {
            try {
                XmlDocument xmlDocument = new XmlDocument ();
                xmlDocument.AppendChild (xmlDocument.CreateXmlDeclaration ("1.0", "UTF-8", null));
                XmlElement xmlRoot = xmlDocument.CreateElement ("UnityTsianFramework");
                xmlDocument.AppendChild (xmlRoot);
                XmlElement xmlCollection = xmlDocument.CreateElement ("AssetBundleCollection");
                xmlRoot.AppendChild (xmlCollection);
                XmlElement xmlAssetBundles = xmlDocument.CreateElement ("AssetBundles");
                xmlCollection.AppendChild (xmlAssetBundles);
                XmlElement xmlAssets = xmlDocument.CreateElement ("Assets");
                xmlCollection.AppendChild (xmlAssets);
                XmlElement xmlElement = null;
                XmlAttribute xmlAttribute = null;
                foreach (AssetBundle assetbundle in m_AssetBundles.Values) {
                    xmlElement = xmlDocument.CreateElement ("AssetBundle");
                    xmlAttribute = xmlDocument.CreateAttribute ("Name");
                    xmlAttribute.Value = assetbundle.Name;
                    xmlElement.Attributes.SetNamedItem (xmlAttribute);
                    if (assetbundle.Variant != null) {
                        xmlAttribute = xmlDocument.CreateAttribute ("Variant");
                        xmlAttribute.Value = assetbundle.Variant;
                        xmlElement.Attributes.SetNamedItem (xmlAttribute);
                    }

                    xmlElement.Attributes.SetNamedItem (xmlAttribute);
                    xmlAssetBundles.AppendChild (xmlElement);
                }
                foreach (Asset asset in m_Assets.Values) {
                    xmlElement = xmlDocument.CreateElement ("Asset");
                    xmlAttribute = xmlDocument.CreateAttribute ("Guid");
                    xmlAttribute.Value = AssetDatabase.GUIDToAssetPath (asset.Guid);
                    xmlElement.Attributes.SetNamedItem (xmlAttribute);
                    xmlAttribute = xmlDocument.CreateAttribute ("AssetBundleName");
                    xmlAttribute.Value = asset.AssetBundle.Name;
                    xmlElement.Attributes.SetNamedItem (xmlAttribute);
                    if (asset.AssetBundle.Variant != null) {
                        xmlAttribute = xmlDocument.CreateAttribute ("AssetBundleVariant");
                        xmlAttribute.Value = asset.AssetBundle.Variant;
                        xmlElement.Attributes.SetNamedItem (xmlAttribute);
                    }
                    xmlAssets.AppendChild (xmlElement);
                }
                string confifurationDirectorName = System.IO.Path.GetDirectoryName (m_ConfigurationPath);
                if (!System.IO.Directory.Exists (confifurationDirectorName)) {
                    System.IO.Directory.CreateDirectory (confifurationDirectorName);
                }
                xmlDocument.Save (m_ConfigurationPath);
                AssetDatabase.Refresh ();
                return true;

            } catch {
                if (System.IO.File.Exists (m_ConfigurationPath)) {
                    System.IO.File.Delete (m_ConfigurationPath);
                }

                return false;
            }
        }
        public AssetBundle[] GetAssetBundles () {
            return m_AssetBundles.Values.ToArray ();
        }
        public AssetBundle GetAssetBundle (string assetBundleName, string assetBundleVariant) {
            if (!IsValidAssetBundleName (assetBundleName, assetBundleVariant)) {
                return null;
            }
            AssetBundle assetbundle = null;
            if (m_AssetBundles.TryGetValue (GetAssetBundleFullName (assetBundleName, assetBundleVariant), out assetbundle)) {
                return assetbundle;
            }
            return null;
        }
        public bool HasAssetBundle (string assetBundleName, string assetBundleVariant) {
            if (!IsValidAssetBundleName (assetBundleName, assetBundleVariant)) {
                return false;
            }
            return m_AssetBundles.ContainsKey (GetAssetBundleFullName (assetBundleName, assetBundleVariant));
        }
        public bool AddAssetBundle (string assetBundleName, string assetBundleVariant) {
            if (!IsValidAssetBundleName (assetBundleName, assetBundleVariant)) {
                return false;
            }
            if (!IsAvailableBundleName (assetBundleName, assetBundleVariant, null)) {
                return false;
            }
            AssetBundle assetbundle = AssetBundle.Create (assetBundleName, assetBundleVariant);
            m_AssetBundles.Add (assetbundle.FullName, assetbundle);
            return true;
        }
        public bool RenameAssetBundle (string oldAssetBundleName, string oldAssetBundleVariant, string newAssetbundleName, string newassetbundleVariant) {
            if (!IsValidAssetBundleName (oldAssetBundleName, oldAssetBundleVariant) || !IsValidAssetBundleName (newAssetbundleName, newassetbundleVariant)) {
                return false;
            }
            AssetBundle assetbundle = GetAssetBundle (oldAssetBundleName, oldAssetBundleVariant);
            if (assetbundle == null) {
                return false;
            }
            if (!IsAvailableBundleName (newAssetbundleName, newassetbundleVariant, assetbundle)) {
                return false;
            }
            m_AssetBundles.Remove (assetbundle.FullName);
            assetbundle.Rename (newAssetbundleName, newassetbundleVariant);
            m_AssetBundles.Add (assetbundle.FullName, assetbundle);
            return true;
        }
        public bool RemoveAssetBundle (string assetBundleName, string assetBundleVariant) {
            if (!IsValidAssetBundleName (assetBundleName, assetBundleVariant)) {
                return false;
            }
            AssetBundle assetbundle = GetAssetBundle (assetBundleName, assetBundleVariant);
            if (assetbundle == null) {
                return false;
            }
            Asset[] assets = assetbundle.GetAssets ();
            assetbundle.Clear ();
            m_AssetBundles.Remove (assetbundle.FullName);
            foreach (Asset asset in assets) {
                m_Assets.Remove (asset.Guid);
            }
            return true;

        }
        public bool SetAssetBundleLoadType (string assetBundleName, string assetBundleVariant) {
            if (!IsValidAssetBundleName (assetBundleName, assetBundleVariant)) {
                return false;
            }

            AssetBundle assetBundle = GetAssetBundle (assetBundleName, assetBundleVariant);
            if (assetBundle == null) {
                return false;
            }

            return true;
        }
        public Asset[] Getassets () {
            return m_Assets.Values.ToArray ();
        }
        public Asset[] Getassets (string assetBundleName, string assetBundlevariant) {
            if (!IsValidAssetBundleName (assetBundleName, assetBundlevariant)) {
                return new Asset[0];
            }
            AssetBundle assetbundle = GetAssetBundle (assetBundleName, assetBundlevariant);
            if (assetbundle == null) {
                return new Asset[0];
            }
            return assetbundle.GetAssets ();
        }
        public Asset GetAsset (string assetGuid) {
            if (string.IsNullOrEmpty (assetGuid)) {
                return null;
            }
            Asset asset = null;
            if (m_Assets.TryGetValue (assetGuid, out asset)) {
                return asset;
            }
            return null;
        }
        public bool HasAsset (string assetGuid) {
            if (string.IsNullOrEmpty (assetGuid)) {
                return false;
            }
            return m_Assets.ContainsKey (assetGuid);
        }
        public bool AssignAsset (string assetGuid, string assetBundleName, string assetBundleVariant) {
            if (string.IsNullOrEmpty (assetGuid)) {
                return false;
            }
            if (!IsValidAssetBundleName (assetBundleName, assetBundleVariant)) {
                return false;
            }
            AssetBundle assetbundle = GetAssetBundle (assetBundleName, assetBundleVariant);
            if (assetbundle == null) {
                return false;
            }
            string assetName = AssetDatabase.GUIDToAssetPath (assetGuid);
            if (string.IsNullOrEmpty (assetName)) {
                return false;
            }

            Asset asset = GetAsset (assetGuid);
            if (asset == null) {
                asset = Asset.Create (assetGuid);
                m_Assets.Add (asset.Guid, asset);
            }
            assetbundle.AssignAsset (asset);
            return true;
        }
        public bool UnassignAsset (string assetGuid) {
            if (string.IsNullOrEmpty (assetGuid)) {
                return false;
            }
            Asset asset = GetAsset (assetGuid);
            if (asset != null) {
                asset.AssetBundle.Unassign (asset);
                m_Assets.Remove (asset.Guid);
            }
            return true;
        }
        public bool IsValidAssetBundleName (string assetBundleName, string assetBundleVariant) {
            if (string.IsNullOrEmpty (assetBundleName)) {
                return false;
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch (assetBundleName, AssetBundleNamePattern)) {
                return false;
            }
            if (!string.IsNullOrEmpty (assetBundleVariant) && !System.Text.RegularExpressions.Regex.IsMatch (assetBundleVariant, AssetBundleVariantPattern)) {
                return false;
            }
            return true;
        }
        public bool IsAvailableBundleName (string assetBundleName, string assetBundleVariant, AssetBundle selfAssetBundle) {
            AssetBundle fildAssetBundle = GetAssetBundle (assetBundleName, assetBundleVariant);
            if (fildAssetBundle != null) {
                return fildAssetBundle == selfAssetBundle;
            }

            foreach (AssetBundle assetbundle in m_AssetBundles.Values) {
                if (selfAssetBundle != null && assetbundle == selfAssetBundle) {
                    continue;
                }
                if (assetbundle.Name == assetBundleName) {
                    if (assetbundle.Variant == null && assetBundleVariant != null) {
                        return false;
                    }
                    if (assetbundle.Variant != null && assetBundleVariant == null) {
                        return false;
                    }
                    if (assetbundle.Name.Length > assetBundleName.Length &&
                        assetbundle.Name.IndexOf (assetBundleName, System.StringComparison.CurrentCultureIgnoreCase) == 0 &&
                        assetbundle.Name[assetBundleName.Length] == '/') {
                        return false;
                    }
                    if (assetBundleName.Length > assetbundle.Name.Length &&
                        assetBundleName.IndexOf (assetbundle.Name, System.StringComparison.CurrentCultureIgnoreCase) == 0 &&
                        assetbundle.Name[assetBundleName.Length] == '/') {
                        return false;
                    }
                }

            }
            return true;
        }
        public string GetAssetBundleFullName (string assetBundleName, string assetBundleVariant) {
            return (!string.IsNullOrEmpty (assetBundleVariant) ? string.Format ("{0}.{1}", assetBundleName, assetBundleVariant) : assetBundleName);

        }
    }
}