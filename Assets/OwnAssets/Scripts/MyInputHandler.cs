using System;
using System.Collections.Generic;
using KartGame.KartSystems;
using UnityEngine;

namespace OwnAssets.Scripts
{
    public class MyInputHandler : BaseInput
    {
        private bool acc;
        private bool brk;
        private float angle;
        private bool isRunning;
        private List<Transform> trackPath;
        private int nextPoint;
        private float velocity;
        private Camera cam;

        private void Start()
        {
            acc = false;
            brk = false;
            angle = 0f;
            isRunning = false;
            cam = GetComponentInChildren<Camera>();
            cam.enabled = false;
        }

        public void Setup(List<Transform> path)
        {
            trackPath = path;
            nextPoint = 1;
            isRunning = true;
            cam.enabled = true;
            Camera.SetupCurrent(cam);
        }
        
        [SerializeField]
        private float m_Speed;

        public void Update()
        {
            if (isRunning)
            {
                Debug.DrawLine(transform.position, trackPath[nextPoint].position,Color.blue);
                if (Vector3.Distance(transform.position, trackPath[nextPoint].position) < 1f)
                {
                    nextPoint++;
                }

                if (Vector3.Distance(transform.position, trackPath[nextPoint].position) > 0.3f)
                {
                    acc = true;
                    brk = false;
                }
                else
                {
                    acc = false;
                    brk = true;
                }
                
                Vector3 lTargetDir = trackPath[nextPoint].position - transform.position;
                lTargetDir.y = 0.0f;
                Quaternion q = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lTargetDir), Time.time * m_Speed);
                q.x = q.z = 0f;
                transform.rotation = q;

                //transform.LookAt(new Vector3(trackPath[nextPoint].position.x, transform.position.y, trackPath[nextPoint].position.z));

                Debug.DrawLine(transform.position, trackPath[nextPoint].position,Color.green);
                //Debug.Log("ANGLE: "+angle);
            }
        }

        public override InputData GenerateInput()
        {
            return new InputData
            {
                Accelerate = acc,
                Brake = brk,
                TurnInput = 0f
            };
        }

        //[SerializeField]  [Range(-1,1)] private float myTurnInput;
        
    }
}