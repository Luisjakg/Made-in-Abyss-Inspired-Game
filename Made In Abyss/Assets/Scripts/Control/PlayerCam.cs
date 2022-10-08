using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
   [SerializeField] float sensX = 1f;
   [SerializeField] float sensY = 1f;
   [SerializeField, Range(1, 180)] private float upperLookLimit = 90.0f;
   [SerializeField, Range(1, 180)] private float lowerLookLimit = 90.0f;

   [SerializeField] private Transform orientation;
   
   private float xRotation = 0f;
   private float yRotation = 0f;

   private void Awake()
   {
      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;
   }

   private void Update()
   {
      float mouseX = Input.GetAxisRaw("Mouse X") * Time.fixedDeltaTime * sensX;
      float mouseY = Input.GetAxisRaw("Mouse Y") * Time.fixedDeltaTime * sensY;
      
      yRotation += mouseX;
      xRotation -= mouseY;

      
      xRotation = Mathf.Clamp(xRotation, -lowerLookLimit, upperLookLimit);
      
      transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
      orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
   }

   public Vector3 getCameraLookDirection()
   {
      return transform.forward;
   }
}
