using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Threading.Tasks;
using System.Linq;

namespace Tomatech.AFASS
{
    public class SaveManager : MonoBehaviour
    {
        const string LENGTH_KEY = "l";
        const string KEYS_KEY = "k";
        const string TILES_KEY = "t";
        const string PREFABS_KEY = "p";
        const string ID_KEY = "i";
        const string DATA_KEY = "d";

        [SerializeField]
        Transform spawnParent;
        [SerializeField]
        Tilemap[] savableTilemaps;
        public Tilemap[] SavableTilemaps => savableTilemaps;

        public JSONObject Save()
        {
            JSONObject levelData = new();
            List<string> assetKeys = new();
            JSONArray tilemapArray = new();
            foreach (var map in savableTilemaps)
            {
                JSONObject mapData = new();

                Vector3Int currentStripStart = Vector3Int.zero;
                JSONObject currentStripData = new();
                int currentStripLength = 0;

                foreach (var tilePos in map.cellBounds.allPositionsWithin)
                {
                    TileBase tile = map.GetTile(tilePos);
                    if (!tile)
                    {
                        if (currentStripLength != 0)
                        {
                            currentStripData.Add(LENGTH_KEY, currentStripLength);
                            mapData.Add(TilePosToString(currentStripStart), currentStripData);
                        }
                        currentStripStart = Vector3Int.zero;
                        currentStripData = new();
                        currentStripLength = 0;
                        continue;
                    }
                    if (tile is ISavable savableTile)
                    {
                        JSONObject tileData = new();

                        int keyIndex = assetKeys.Count;
                        if (!assetKeys.Contains(savableTile.SaveKey))
                            assetKeys.Add(savableTile.SaveKey);
                        else
                            keyIndex = assetKeys.IndexOf(savableTile.SaveKey);
                        tileData.Add(ID_KEY, keyIndex);

                        JSONNode customData = savableTile.ExtractSaveData();
                        if (customData != null)
                            tileData.Add(DATA_KEY, customData);

                        if (currentStripData.ToString() != tileData.ToString() || currentStripStart.y != tilePos.y)
                        {
                            if (currentStripLength != 0)
                            {
                                currentStripData.Add(LENGTH_KEY, currentStripLength);
                                mapData.Add(TilePosToString(currentStripStart), currentStripData);
                            }
                            currentStripStart = tilePos;
                            currentStripData = tileData;
                            currentStripLength = 1;
                        }
                        else
                            currentStripLength++;

                    }
                }
                if (currentStripLength != 0)
                {
                    currentStripData.Add(LENGTH_KEY, currentStripLength);
                    mapData.Add(TilePosToString(currentStripStart), currentStripData);
                }
                tilemapArray.Add(mapData);
            }
            levelData.Add(TILES_KEY, tilemapArray);

            JSONArray prefabArray = new();
            var savableParts = spawnParent.GetComponentsInChildren<MonoBehaviour>().OfType<ISavable>();
            foreach (var prefab in savableParts)
            {
                JSONObject prefabData = new();

                int keyIndex = assetKeys.Count;
                if (!assetKeys.Contains(prefab.SaveKey))
                    assetKeys.Add(prefab.SaveKey);
                else
                    keyIndex = assetKeys.IndexOf(prefab.SaveKey);
                prefabData.Add(ID_KEY, keyIndex);

                JSONNode customData = prefab.ExtractSaveData();
                if (customData != null)
                    prefabData.Add(DATA_KEY, customData);
                prefabArray.Add(prefabData);
            }
            levelData.Add(PREFABS_KEY, prefabArray);

            JSONArray keyArray = new();
            foreach (var key in assetKeys)
            {
                keyArray.Add(key);
            }
            levelData.Add(KEYS_KEY, keyArray);

            return levelData;
        }
        static string TilePosToString(Vector3Int pos) => pos.x + "_" + pos.y;

        public async Task Load(JSONObject levelData)
        {
            for (int i = 0; i < spawnParent.childCount; i++)
                Destroy(spawnParent.GetChild(i).gameObject);
            for (int i = 0; i < savableTilemaps.Length; i++)
                savableTilemaps[i].ClearAllTiles();

            //JSONObject levelData = JSON.Parse(plainText).AsObject;
            JSONArray keyData = levelData[KEYS_KEY].AsArray;
            string[] assetKeys = keyData.AsEnumerable().Select(x => x.Value).ToArray();
            await SavableResourceManager.PreloadSaveableAssets(assetKeys);

            JSONArray tilemapArray = levelData[TILES_KEY].AsArray;
            for (int i = 0; i < Mathf.Min(tilemapArray.Count, savableTilemaps.Length); i++)
            {
                Tilemap map = savableTilemaps[i];
                JSONObject mapTiles = tilemapArray[i].AsObject;
                foreach (var item in mapTiles)
                {
                    Vector3Int tilePos = StringToTilePos(item.Key);
                    TileBase tile = SavableResourceManager.GetSaveableAsset<TileBase>(assetKeys[item.Value[ID_KEY].AsInt]);
                    JSONNode data = item.Value.GetValueOrDefault(DATA_KEY, null);
                    ISavable savableTile = null;
                    bool isSavable = false;
                    if (tile is ISavable newSavableTile)
                    {
                        isSavable = true;
                        savableTile = newSavableTile;
                    }
                    for (int j = 0; j < item.Value[LENGTH_KEY].AsInt; j++)
                    {
                        map.SetTile(tilePos, tile);
                        if (data != null && isSavable)
                            savableTile.ApplySaveData(data, (tilePos, map));
                        tilePos.x++;
                    }
                }
            }
            JSONArray prefabArray = levelData[PREFABS_KEY].AsArray;
            for (int i = 0; i < prefabArray.Count; i++)
            {
                JSONObject prefabObject = prefabArray[i].AsObject;
            
                var spawned = SpawnObject(assetKeys[prefabObject[ID_KEY].AsInt]);
                spawned.transform.SetParent(spawnParent, true);
                if (prefabObject.HasKey(DATA_KEY) && spawned.TryGetComponent<ISavable>(out var savablePrefab))
                    savablePrefab.ApplySaveData(prefabObject[DATA_KEY]);
            }
        }

        protected virtual GameObject SpawnObject(string key)
        {
            return Instantiate(SavableResourceManager.GetSaveableAsset<GameObject>(key));
        }

        static Vector3Int StringToTilePos(string str)
        {
            string[] split = str.Split('_');
            return new(int.Parse(split[0]), int.Parse(split[1]));
        }
    }
    public interface ISavable
    {
        string SaveKey { get; }
        JSONNode ExtractSaveData(object context = null) => null;
        void ApplySaveData(JSONNode node, object context = null) { }
    }

}
