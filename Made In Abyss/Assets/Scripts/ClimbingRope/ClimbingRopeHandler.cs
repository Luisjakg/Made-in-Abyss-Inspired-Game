using System;
using System.Collections;
using System.Collections.Generic;
using Obi;
using UnityEngine;

namespace MIA.ClimbingRope
{
    public class ClimbingRopeHandler : MonoBehaviour
    {
        private bool shouldRetract => Input.GetKey(retractRope);
        private bool shouldExtend => Input.GetKey(extendRope);
        
        [Header("Functional Options")] 
        [SerializeField] private bool canChangeSize = true;
        
        [Header("Controls")] 
        [SerializeField] private KeyCode retractRope = KeyCode.Mouse0;
        [SerializeField] private KeyCode extendRope = KeyCode.Mouse1;
        
        [Header("Size Change Control")]
        [SerializeField] private float airExtendMultiplier = .8f;
        [SerializeField] private float minimumRopeSize = .4f;
        [SerializeField] private float retractSpeed;
        [SerializeField] private float extendSpeed;
        
        [Header("References")]
        [SerializeField] private GameObject rope;
        [SerializeField] private ObiSolver obiSolver;
        [SerializeField] private GameObject hook;
        private ObiRopeCursor ropeCursor;
        private ObiCollider player;
        private Vector3 startGravityValue;
        private ObiParticleAttachment[] particleAttachments;
        
        private void Awake()
        {
            particleAttachments = rope.GetComponents<ObiParticleAttachment>();
            ropeCursor = rope.GetComponent<ObiRopeCursor>();
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<ObiCollider>();
        }
        
        void Start()
        {
            AttachRopeToPlayer();
        }
        
        void Update()
        {
            Debug.Log(rope.GetComponent<ObiRope>().restLength);
            if (!rope.GetComponent<ObiRope>().isLoaded) return;
            if (!hook.GetComponent<Hook>().GetTargetHit())
                ExtendRope(hook.GetComponent<Hook>().GetMoveSpeed() * airExtendMultiplier);
            if (canChangeSize) HandleRopeLength();
        }
        
        private void AttachRopeToPlayer()
        {
            particleAttachments[1].target = player.transform;
        }
        
        private void HandleRopeLength()
        {
            if (shouldExtend && shouldRetract) return;
            if (shouldRetract && !(rope.GetComponent<ObiRope>().restLength <= minimumRopeSize))
                RetractRope(retractSpeed);
            else if (shouldExtend) ExtendRope(extendSpeed);
        }
        
        private void RetractRope(float velocity)
        {
            Debug.Log("Retracting Rope");
            ropeCursor.ChangeLength(rope.GetComponent<ObiRope>().restLength - velocity * Time.deltaTime);
        }
        
        private void ExtendRope(float velocity)
        {
            Debug.Log("Extending Rope");
            ropeCursor.ChangeLength(rope.GetComponent<ObiRope>().restLength + velocity * Time.deltaTime);
        }
        
        public GameObject GetHook()
        {
            return hook; 
        }
    }
}