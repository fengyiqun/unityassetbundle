using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
public class Dependencies
{
	int idx = 0;
	Dictionary<string, int> str_int = new Dictionary<string, int>();
	Dictionary<int, List<int>> dependencies = new Dictionary<int, List<int>>();
	private int str_to_int(string str) {
		int n;
		if (str.EndsWith(".unity") != true) {
			if (str.CompareTo(str.ToLower()) != 0) {
				int a = 3;
			}
		}
		if (!str_int.TryGetValue(str, out n)) {
			n = ++idx;
			str_int[str] = n;
		}
		return n;
	}
	private void build_one(HashSet<string> assets, string name, List<int> deps) {
		var list = UnityEditor.AssetDatabase.GetDependencies(name);
		foreach (var s in list) {
			var dep = s;
			if (dep.EndsWith(".unity") == false)
				dep = dep.ToLower();
			if (dep.EndsWith(".cs") || dep.CompareTo(name) == 0)
				continue;
			if (assets.Contains(dep)) {
				deps.Add(str_to_int(dep));
			} else {
				build_one(assets, dep, deps);
			}
		}
	}
	public void build(List<string> all, string path) {
		HashSet<string> assets = new HashSet<string>();
		foreach (var name in all) {
			if (name.EndsWith(".unity") == false)
				assets.Add(name.ToLower());
			else
				assets.Add(name);
		}
		foreach (var name in assets) {
			var nameid = str_to_int(name);
			List<int> deps = new List<int>();
			build_one(assets, name, deps);
			if (deps.Count > 65535)
				UnityEngine.Debug.LogError("ABM: asset:" + name + " too many dependcy:" + dependencies.Count);
			if (deps.Count > 0)
				dependencies[nameid] = deps;
		}
		MemoryStream ms = new MemoryStream(1024);
		BinaryWriter bw = new BinaryWriter(ms);
		bw.Write((uint)str_int.Count);
		foreach (var x in str_int) {
			var buf = System.Text.Encoding.UTF8.GetBytes(x.Key);
			bw.Write((uint)x.Value);
			bw.Write((ushort)buf.Length);
			bw.Write(buf);
		}
		bw.Write((uint)dependencies.Count);
		foreach (var d in dependencies) {
			bw.Write((uint)d.Key);
			bw.Write((short)d.Value.Count);
			foreach (var dd in d.Value)
				bw.Write((uint)dd);
		}
		File.WriteAllBytes(path, ms.ToArray());
		UnityEditor.AssetDatabase.Refresh();
		return;
	}
}
