using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tomatech.AFASS
{
    public static class SavableResourceManager
    {
        static Dictionary<string, AsyncOperationHandle<Object>> loadedAssets = new();

        public static T GetSaveableAsset<T>(string key) where T : Object
        {
            if (!loadedAssets.ContainsKey(key))
                return null;
            if (loadedAssets[key].Result is T validObject)
                return validObject;
            return null;
        }

        public static async Task PreloadSaveableAssets(string[] newKeys, bool withLogs=false)
        {
            var toUnload = loadedAssets.Keys.Except(newKeys);
            var toLoad = newKeys.Except(loadedAssets.Keys);

            if (withLogs)
                Debug.Log("unloading " + toUnload.Count() + " key(s)");
            foreach (var key in toUnload)
            {
                Addressables.Release(loadedAssets[key]);
                loadedAssets.Remove(key);
            }
            if (withLogs)
                Debug.Log("loading " + toLoad.Count() + " key(s)");
            foreach (var key in toLoad)
            {
                var assetHandle = Addressables.LoadAssetAsync<Object>(key);
                loadedAssets.Add(key, assetHandle);
            }

            if (withLogs)
                Debug.Log("waiting for assets to load...");
            await Task.WhenAll(loadedAssets.Values.Select(a => a.Task));
            if (withLogs)
                Debug.Log("loading complete with " + loadedAssets.Count + " loaded object" + (loadedAssets.Count == 1 ? "" : "s"));
        }
    }
}

