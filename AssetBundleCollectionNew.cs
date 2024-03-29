using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;
using System.IO;
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
        private Asset(string name,string guid, Asset asset, AssetBundle assetbundle)
        {
            Name = name;
            Guid = guid;
            AssetBundle = assetbundle;
            m_Assets = new List<Asset>();
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
        public Asset AddAsset(string name,string guid){
            if(string.IsNullOrEmpty(name)){
                Debug.LogWarning("Asset name is invalid");
            }
            Asset asset = GetAsset(name);
            if (asset!= null){
                Debug.LogWarning("asset is already exist"+name);
            }
            asset = Asset.Create(name,guid,this);
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
                return assetparent == null ? string.Empty:(assetparent.assetparent == null ? Name : string.Format("{0}/{1}",assetparent.FromRootPath, Name));
            }
        }
        public int Depth{
            get{
                return assetparent != null ? assetparent.Depth + 1:0;
            }
        }
        public string Name {

            get;
            private set;
        }
        public AssetBundle AssetBundle {
            get;
            private set;
        }
        public Asset GetAsset(string name){
            if(string.IsNullOrEmpty(name)){
                Debug.Log("asset name is invalid");
            }
            foreach(Asset asset in  m_Assets){
                if(asset.Name == name)
                {
                    return asset;
                }
            }
            return null;
        }
        public static Asset Create (string guid) {
            return new Asset (guid,null, null);
        }
        public static Asset Create(string guid,string name)
        {
            return new Asset(name,guid, null, null);
        }
        public static Asset Create (string guid,Asset asset) {
            return new Asset (guid,asset, null);
        }
        public static Asset Create(string name,string guid,Asset asset)
        {
            return new Asset(name, guid, asset, null);
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
                        string iconpath = "Assets/" + FromRootPath; ;
                        if (string.IsNullOrEmpty(FromRootPath))
                        {
                            iconpath = Name;
                        }
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
    static string ENTRY = "MAIN.bundle";
    public class AssetBundleInfo{
        public AssetBundleInfo(){
            AssetBundles = new Dictionary<string, List<string>>();
        }
        public string Entry{get;set;}
        public Dictionary<string,List<string>> AssetBundles{get;set;}
    }

    public class AssetBundleCollection {
        private const string AssetBundleNamePattern = @"^([A-Za-z0-9\._-]+/)*[A-Za-z0-9\._-]+$";
        private const string AssetBundleVariantPattern = @"^[a-z0-9_-]+$";
        private const string PostfixOfScene = ".unity";
        private static string m_ConfigurationPath = "";

        private static string m_configurationpathnew = "";
        private SortedDictionary<string, AssetBundle> m_AssetBundles;
        private SortedDictionary<string, Asset> m_Assets;
        private AssetBundleInfo assetbundleinfo;
        public AssetBundleCollection()
        {
            assetbundleinfo = new AssetBundleInfo();
            m_ConfigurationPath = TsianFramework.Utility.Path.GetCombinePath(Application.dataPath, "App/Configs/AssetBundleCollection.xml");
            m_AssetBundles = new SortedDictionary<string, AssetBundle>();
            m_Assets = new SortedDictionary<string, Asset>();
            m_configurationpathnew = TsianFramework.Utility.Path.GetCombinePath(Application.dataPath, "App/Configs/ABT.yaml");
           
        }

        public void parse(string path)
        {
            if (!File.Exists(path))
            {
                FileStream filestream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                filestream.Close();

            }
            var str = File.ReadAllText(path);
            if (!string.IsNullOrEmpty(str))
            {
                var reader = new YamlDotNet.Serialization.Deserializer();
                assetbundleinfo = reader.Deserialize<AssetBundleInfo>(str);
                foreach (var value in assetbundleinfo.AssetBundles)
                {
                    if (!AddAssetBundleNew(value.Key, null))
                    {
                        Debug.LogWarning(string.Format("Can not add assetBundle '{0}'.", value.Key));
                        continue;
                    }
                    for(int i  = 0; i < value.Value.Count; i++)
                    {
                        if (!AssignAssetNew(value.Value[i], value.Key, null))
                        {
                            Debug.LogWarning(string.Format("Can not Assign asset '{0}' to assetBundle '{1}'.", value.Value[i], value.Key));
                            continue;
                        }
                    }
                }
            }
            
        }
        private void SaveNew()
        {
            AssetBundleInfo assetbundleinfo = new AssetBundleInfo();
            foreach (var assetbundle in m_AssetBundles)
            {
                List<string> asset = new List<string>();
                for (int i = 0; i < assetbundle.Value.GetAssets().Length; i++)
                {
                    asset.Add(assetbundle.Value.GetAssets()[i].Name);
                }
                if (assetbundleinfo.AssetBundles.ContainsKey(assetbundle.Key))
                {
                    UnityEngine.Debug.LogWarning(assetbundle.Key);
                    continue;
                }
                assetbundleinfo.AssetBundles.Add(assetbundle.Key, asset);
            }
           save(assetbundleinfo, m_configurationpathnew);
        }
        public void save(AssetBundleInfo abi,string path)
        {
            var witer = new YamlDotNet.Serialization.Serializer();
            File.WriteAllText(path, witer.Serialize(abi));
        }
        public AssetBundleInfo GetAssetBundleInfo
        {
            get { return assetbundleinfo; }
            set { assetbundleinfo = value; }
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
        public bool Load()
        {
            /*
            UnityTsianFramework.Editor.AssetBundleTools.AssetBundleCollection m_controller = new UnityTsianFramework.Editor.AssetBundleTools.AssetBundleCollection();
            if (m_controller.Load())
            {
                for(int i = 0; i < m_controller.GetAssetBundles().Length; i++)
                {
                    if (!AddAssetBundleNew(m_controller.GetAssetBundles()[i].FullName, null))
                    {
                        continue;
                    }
                    for (int ii = 0; ii < m_controller.GetAssetBundles()[i].GetAssets().Length; ii++)
                    {
                        if (!AssignAssetNew(m_controller.GetAssetBundles()[i].GetAssets()[ii].Guid, m_controller.GetAssetBundles()[i].Name, null))
                        {
                            //Debug.LogWarning(string.Format("Can not Assign asset '{0}' to assetBundle '{1}'.", value.Value[i], value.Key));
                            continue;
                        }
                    }
                }
                
            }*/
            parse(m_configurationpathnew);
            return false;

        }
        public bool Save () {
            
            SaveNew();
            return false;
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
        public bool AddAssetBundleNew(string assetBundleName,string assetBundleVariant){
            if(!IsValidAssetBundleName(assetBundleName,assetBundleVariant)){
                return false;
            }
            if(!IsAvailableBundleName(assetBundleName,assetBundleVariant,null)){
                return false;
            }
            AssetBundle assetbundle = AssetBundle.Create(assetBundleName,assetBundleVariant);
            m_AssetBundles.Add(assetbundle.FullName,assetbundle);
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
        public bool AssignAssetNew(string assetname, string assetbundlename,string assetbundlevariant){
             if (string.IsNullOrEmpty (assetname)) {
                return false;
            }
            if (!IsValidAssetBundleName (assetbundlename, assetbundlevariant)) {
                return false;
            }
            AssetBundle assetbundle = GetAssetBundle(assetbundlename, assetbundlevariant);
            if(assetbundle == null)
            {
                return false;
            }
            string assetguid = AssetDatabase.AssetPathToGUID(assetname);
            if (string.IsNullOrEmpty(assetguid))
            {
                return false;
            }
            Asset asset = GetAsset(assetguid);
            if(asset == null)
            {
                asset = Asset.Create(assetguid, assetname);
                m_Assets.Add(asset.Guid, asset);
            }
            assetbundle.AssignAsset(asset);
            return true;
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
                asset = Asset.Create (assetGuid,assetName);
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