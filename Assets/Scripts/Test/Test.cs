using Core;
using UnityEngine;

namespace Test
{
    public class Test : MonoBehaviour
    {
        private void Start()
        {
            GameDataStorage.Instance.Initialize();
        }
    }
}