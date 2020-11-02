using Core;
using UnityEngine;

namespace View
{
    public class TileMapWrapper : MonoBehaviour
    {
        [SerializeField]
        private HexTileWrapper hexTilePrefab;

        public TileMap TileMap { get; private set; }

        private void Start()
        {
            ConstructTileMap();
        }

        public void Init(TileMap tileMap)
        {
            TileMap = tileMap;
        }

        private void ConstructTileMap()
        {
            var sqr3 = Mathf.Sqrt(3);

            foreach (var t in TileMap)
            {
                var c = t.Coord;
                var pos = new Vector3(sqr3 * c.Q + sqr3 * c.R / 2, 0, 1.5f * c.R) -
                          new Vector3(TileMap.Radius * 1.5f * sqr3, 0, TileMap.Radius * 1.5f);

                var tileObj = Instantiate(hexTilePrefab, transform);
                tileObj.Init(t);
                tileObj.transform.localPosition = pos;
            }
        }
    }
}
