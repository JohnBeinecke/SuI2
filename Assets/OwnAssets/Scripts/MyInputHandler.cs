using System;
using System.Collections.Generic;
using KartGame.KartSystems;
using UnityEngine;

namespace OwnAssets.Scripts
{
    // Own steering logic for the kart agent
    public class MyInputHandler : BaseInput
    {
        // Should the kart accelerate
        private bool acc;
        
        // Should the kart break
        private bool brk;
        
        // Is this input handler active
        private bool isRunning;
        
        // The path for the kart to follow
        private List<Vector3> trackPath;
        
        // Index of the next point on the path to reach
        private int nextPointIdx;
        
        // LayerMask of the track
        [SerializeField] private LayerMask trackLayer;
        
        // Speed of kart rotation toward next point
        [SerializeField] private float rotSpeed;

        // Starts the input handler
        public void StartPlayer()
        {
            isRunning = true;
        }

        // Checks if the input handler has a path to follow
        public bool IsPlayerReady()
        {
            return trackPath != null;
        }

        // Called on first frame
        private void Start()
        {
            acc = false;
            brk = false;
            isRunning = false;
        }

        // Add path to the input handler
        public void Setup(List<Vector3> path)
        {
            nextPointIdx = 1;
            trackPath = path;
        }
        

        // Called every frame, handles the driving
        public void Update()
        {
            if (isRunning)
            {
                //Debug.DrawLine(transform.position, trackPath[GetHighestVisibleIndex()], Color.green);
                //Debug.DrawLine(transform.position, trackPath[nextPoint],Color.blue);

                // Get highestVisibleIndex
                nextPointIdx = GetHighestVisibleIndex();

                // Check acceleration distance
                if (Vector3.Distance(transform.position, trackPath[nextPointIdx]) > 0.3f)
                {
                    acc = true;
                    brk = false;
                }
                else
                {
                    acc = false;
                    brk = true;
                }
                
                // Handle rotation
                Vector3 lTargetDir = trackPath[nextPointIdx] - transform.position;
                lTargetDir.y = 0.0f;
                Quaternion q = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lTargetDir), Time.time * rotSpeed);
                q.x = q.z = 0f;
                transform.rotation = q;
            }
        }

        // Gets the highestVisibleIndex from the kart on the track (with a range 1/3 of the track)
        private int GetHighestVisibleIndex()
        {
            int currHighest = nextPointIdx;
            for (int i = 0; i < trackPath.Count/3; i++)
            {
                int currIndex = nextPointIdx + i < trackPath.Count ? nextPointIdx + i : (nextPointIdx + i) - trackPath.Count;

                
                // Checking for additional playerWidth
                float thickness = 2f;
                Vector3 origin = transform.position;
                Vector3 dir = trackPath[currIndex] - transform.position;
                Vector3 leftShift = new Vector3(-dir.x, 0, dir.z).normalized * thickness;
                Vector3 rightShift = new Vector3(dir.x, 0, -dir.z).normalized * thickness;
                
                if (!Physics.Linecast(origin, trackPath[currIndex], trackLayer) &&
                    !Physics.Linecast(origin + leftShift, trackPath[currIndex] + leftShift,
                        trackLayer) &&
                    !Physics.Linecast(origin + rightShift, trackPath[currIndex] + rightShift,
                        trackLayer))
                {
                   
                    //Debug.DrawLine(origin,trackPath[currIndex],Color.red);
                    //Debug.DrawLine(origin+leftShift,trackPath[currIndex]+leftShift,Color.yellow);
                    //Debug.DrawLine(origin+rightShift,trackPath[currIndex]+rightShift,Color.yellow);

                    currHighest = currIndex;
                }
            }

            return currHighest;
        }

        // Generates the actual input
        public override InputData GenerateInput()
        {
            return new InputData
            {
                Accelerate = acc,
                Brake = brk,
                TurnInput = 0f
            };
        }

    }
}