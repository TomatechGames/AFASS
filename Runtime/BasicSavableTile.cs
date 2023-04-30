using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tomatech.AFASS
{
    public class BasicSavableTile : Tile, ISavable
    {
        [SerializeField]
        [AddressableKey]
        string addressableKey;
        public string SaveKey => addressableKey;
        // tile positions and map layers are handled automatically by the save system.
        // if any custom data is used, the context parameter of the ApplySaveData function will be
        // a tuple containing a Vector3Int and a Tilemap representing the coordinates and map of the tile
    }
}
