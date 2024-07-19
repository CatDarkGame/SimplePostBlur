using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatDarkGame
{
    public class SampleTest : MonoBehaviour
    {
        public GameObject obj = null;
        private void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                if (obj)
                {
                    obj.SetActive(!obj.activeSelf);
                }
            }
        }
    }

}
