using Core;
using UnityEngine;

namespace View
{
    public class HexTileWrapper : MonoBehaviour
    {
        public HexTile HexTile { get; private set; }

        public void Init(HexTile hexTile)
        {
            HexTile = hexTile;
            name = $"Tile ({hexTile.Coord.Q}, {hexTile.Coord.R})";
        }
    }
}
