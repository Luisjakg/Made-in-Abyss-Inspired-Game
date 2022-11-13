using System;
using System.Collections;
using System.Collections.Generic;
using MIA.PlayerControl;
using UnityEngine;

public class DamageTest : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController.OnTakeDamage(10);
        }
    }
}
