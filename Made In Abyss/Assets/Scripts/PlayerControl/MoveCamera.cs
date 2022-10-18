using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MIA.PlayerControl
{
    public class MoveCamera : MonoBehaviour
    {
        [SerializeField] private Transform cameraPosition;

        private void Update()
        {
            transform.position = cameraPosition.position;
        }
    }

}