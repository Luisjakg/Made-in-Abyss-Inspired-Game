using System;
using System.Collections;
using System.Collections.Generic;
using MIA.PlayerControl;
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
        [SerializeField] private float maxRopeSize = 2f;
        [SerializeField] private float retractSpeed;
        [SerializeField] private float extendSpeed;
        private float currentRopeSize;
        
        [Header("References")]
        [SerializeField] private GameObject rope;
        [SerializeField] private ObiSolver obiSolver;
        private GameObject hook;
        private ObiRopeCursor ropeCursor;
        private PlayerMovementController playerMovementController;
        private GameObject player;
        private Vector3 startGravityValue;
        private ObiParticleAttachment[] particleAttachments; //0 is for hook & 1 is for player
        private ClimbingRopeShooter climbingRopeShooter;

        private void Awake()
        {
            particleAttachments = rope.GetComponents<ObiParticleAttachment>();
            ropeCursor = rope.GetComponent<ObiRopeCursor>();
            player = GameObject.FindGameObjectWithTag("Player");
            playerMovementController = player.GetComponent<PlayerMovementController>();
            climbingRopeShooter = player.GetComponent<ClimbingRopeShooter>();
        }
        
        void Start()
        {
            hook = climbingRopeShooter.GetHook();
            AttachRopeEndpoints();
        }
        
        void Update()
        {
            if (!rope.GetComponent<ObiRope>().isLoaded) return;

            currentRopeSize = rope.GetComponent<ObiRope>().restLength;
            
            if (!hook.GetComponent<Hook>().GetIsTargetHit())
                ExtendRope(hook.GetComponent<Hook>().GetMoveSpeed() * airExtendMultiplier);
            
            HandleParticleAttachments();
            
            if (canChangeSize) HandleRopeLength();
        }
        
        private void AttachRopeEndpoints()
        {
            particleAttachments[0].target = hook.transform;
            particleAttachments[1].target = player.transform;
        }
        
        private void HandleRopeLength()
        {
            if (shouldExtend && shouldRetract) return;
            if (shouldRetract && !(currentRopeSize <= minimumRopeSize))
                RetractRope(retractSpeed);
            else if (shouldExtend) ExtendRope(extendSpeed);
        }
        
        private void RetractRope(float velocity)
        {
            ropeCursor.ChangeLength(currentRopeSize - velocity * Time.deltaTime);
        }
        
        private void ExtendRope(float velocity)
        {
            ropeCursor.ChangeLength(currentRopeSize + velocity * Time.deltaTime);
        }

        private void HandleParticleAttachments()
        {
            if (currentRopeSize >= maxRopeSize && !hook.GetComponent<Hook>().GetIsTargetHit())
                particleAttachments[0].attachmentType = ObiParticleAttachment.AttachmentType.Dynamic;
            else
                particleAttachments[0].attachmentType = ObiParticleAttachment.AttachmentType.Static;
            
            if (hook.GetComponent<Hook>().GetIsTargetHit())
                particleAttachments[1].attachmentType = ObiParticleAttachment.AttachmentType.Dynamic;
            else
                particleAttachments[1].attachmentType = ObiParticleAttachment.AttachmentType.Static;
        }
    }
}