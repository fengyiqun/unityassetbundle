using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetBundleBuilder :  EditorWindow{

    const string ENTRY = "SSRAB";
    const string OUTPUT_PATH = "";
    const string suffix = ".dj";
    const string depenstr = "dependcies";
    static void build_target(BuildTarget target,string foldername)
    {
        string parth = TsianFramework.Utility.Path.GetCombinePath(Application.dataPath, "../../");
        parth = Path.GetFullPath(parth);
        string output = Path.Combine(parth, ENTRY+"/"+foldername);
        string m_ConfigurationPath = TsianFramework.Utility.Path.GetCombinePath(Application.dataPath, "App/Configs/ABT.yaml");
        string depenpath = TsianFramework.Utility.Path.GetCombinePath(Application.dataPath, depenstr+".manifest");
        BuildAssetBundleOptions option = BuildAssetBundleOptions.DeterministicAssetBundle;
        option |= BuildAssetBundleOptions.StrictMode;
        option |= BuildAssetBundleOptions.ChunkBasedCompression;
        option |= BuildAssetBundleOptions.DisableLoadAssetByFileName;
        option |= BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
        option |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
        try { Directory.Delete(output, true); } catch (System.Exception) { }
        Directory.CreateDirectory(output);
        var abi = parse(m_ConfigurationPath);
        List< AssetBundleBuild> abb =new List<AssetBundleBuild>();
        List<string> depen = new List<string>();
        foreach (var x in abi.AssetBundles)
        {
            int j = 0;
            Dictionary<string, string> tmp = new Dictionary<string, string>();
            var bb = new AssetBundleBuild();
            bb.assetBundleName = x.Key + suffix;
            foreach (var y in x.Value)
            {
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(y)))
                {
                    Debug.LogError(string.Format("AB包中资源错误:{0}ab包中的{1}资源", x.Key, y));
                    continue;
                }
                tmp[y] = x.Key;
            }
            if (tmp.Count <= 0)
            {
                Debug.LogError("AB包中没有资源:" + x.Key);
                continue;
            }
            bb.assetNames = new string[tmp.Count];
            foreach (var z in tmp)
            {
                bb.assetNames[j++] = z.Key;
                depen.Add(z.Key);
            }
            abb.Add( bb);
        }
        Dependencies dependencies = new Dependencies();
        dependencies.build(depen,depenpath);
        var depena = new AssetBundleBuild();
        depena.assetNames = new string[1];
        depena.assetBundleName = depenstr+suffix;
        depena.assetNames[0] = "Assets/"+depenstr+".manifest";
        abb.Add(depena);
        BuildPipeline.BuildAssetBundles(output, abb.ToArray(), option, target);
        AssetDatabase.Refresh();
        Debug.Log("ABT build finish:" + output);
    }
    static AssetBundleCollEctionNew.AssetBundleInfo parse(string path)
    {
        if (!File.Exists(path))
        {


            UnityEngine.Debug.LogWarning("Assetbundle 配置错误");
            return null;

        }
        var str = File.ReadAllText(path);
        if (!string.IsNullOrEmpty(str))
        {
            var reader = new YamlDotNet.Serialization.Deserializer();
            return reader.Deserialize<AssetBundleCollEctionNew.AssetBundleInfo>(str);
            
        }
        UnityEngine.Debug.LogWarning("Assetbundle 配置错误");
        return null;
    }
    [MenuItem("Tools/Build AssetBundle(Win)")]
    static void build_win()
    {
        build_target(BuildTarget.StandaloneWindows,"Win");
    }
    [MenuItem("Tools/Build AssetBundle(IOS)")]
    static void build_ios()
    {
        build_target(BuildTarget.iOS,"IOS");
    }
    [MenuItem("Tools/Build AssetBundle(Android)")]
    static void build_android()
    {
        build_target(BuildTarget.Android,"android");
    }
}
