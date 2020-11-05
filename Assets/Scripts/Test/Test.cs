using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Core;
using Core.GameData;
using UnityEngine;
using View;
using Logger = Core.Logger;

namespace Test
{
    public class Test : MonoBehaviour
    {
        [SerializeField]
        private TileMapWrapper tileMapPrefab;

        private void Awake()
        {
            GameDataStorage.Instance.Initialize();
        }

        private void Start()
        {
            var tileMap = GameDataStorage.Instance.GetGameData<TileMapData>().CreateDirectly("Default", null, 5, null);

            var obj = Instantiate(tileMapPrefab, transform);
            obj.Init(tileMap);
        }

        private void OnApplicationQuit()
        {
            Logger.Instance.Dispose();
        }
    }
}
