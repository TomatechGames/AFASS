using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tomatech.AFASS
{
    public class BasicSavablePrefab : MonoBehaviour, ISavable
    {
        [SerializeField]
        [AddressableKey]
        string addressableKey;
        public string SaveKey => addressableKey;
        public virtual JSONNode ExtractSaveData(object _ = null)
        {
            JSONObject prefabData = new();
            prefabData.Add("pos", PositionToJSON(transform.position));
            prefabData.Add("rot", FloatToJSON(transform.rotation.eulerAngles.z));
            return prefabData;
        }

        public static JSONArray PositionToJSON(Vector2 pos)
        {
            JSONArray posData = new();
            posData.Add(Mathf.FloorToInt(pos.x * 100));
            posData.Add(Mathf.FloorToInt(pos.y * 100));
            return posData;
        }
        public static JSONNode FloatToJSON(float floatVal)
        {
            return Mathf.FloorToInt(floatVal * 100);
        }

        public virtual void ApplySaveData(JSONNode prefabData, object _ = null)
        {
            transform.position = JSONToPosition(prefabData["pos"].AsArray);
            transform.rotation = Quaternion.Euler(0, 0, JSONToFloat(prefabData["rot"]));
        }

        public static Vector2 JSONToPosition(JSONArray obj)
        {
            return new Vector2(obj[0].AsInt * 0.01f, obj[1].AsInt * 0.01f);
        }
        public static float JSONToFloat(JSONNode obj)
        {
            return obj.AsInt * 0.01f;
        }
    }
}