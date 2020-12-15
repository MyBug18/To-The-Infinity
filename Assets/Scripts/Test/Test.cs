﻿using Core;
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
            var tileMap = TileMapData.Instance.CreateDirectly("Default", null, 5, null);

            var obj = Instantiate(tileMapPrefab, transform);
            obj.Init(tileMap);
        }

        private void OnApplicationQuit()
        {
            Logger.Instance.Dispose();
        }
    }
}
