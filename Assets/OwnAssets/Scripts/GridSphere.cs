using UnityEngine;

namespace OwnAssets.Scripts
{
    /*
     * A spherical collider used for detecting the track upon start.
     */
    public class GridSphere : MonoBehaviour
    {
        // Green material if track was detected
        [SerializeField] private Material greenMaterial;

        // Gold material if goal was detected
        [SerializeField] private Material goldMaterial;

        // LayerMask of the track
        [SerializeField] private LayerMask trackLayer;

        // Is this the start node
        private bool isStart;

        // Is this a goal node
        private bool isGoal;

        // Is this a valid node (on the track)
        private bool isValid;

        // Is this on the FinishLine
        private bool isFinishLine;

        // Detect collision with sphere collider (needs a short time after start)
        private void OnTriggerEnter(Collider other)
        {
            // Additional edge detection with raycasts
            if (Physics.Linecast(transform.position + new Vector3(0, 2, 0), transform.position + new Vector3(0, 1, 0),
                    trackLayer)) return;
            if (!Physics.Linecast(transform.position + new Vector3(0, -1, 0), transform.position, trackLayer)) return;
            
            Vector3 overHeadPos = transform.position + new Vector3(0, 1, 0);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;

                    if (Physics.Linecast(overHeadPos, overHeadPos + new Vector3(i, 0, j), trackLayer))
                    {
                        return;
                    }
                    //Debug.DrawLine(overHeadPos, overHeadPos + new Vector3(i,0,j), Color.red, 20f);
                }
            }

            // Check for finish line
            if (other.name == "FinishLine")
            {
                isFinishLine = true;
                isValid = false;
            }
            else if (!isFinishLine)
            {
                isValid = true;
                
                // Check for goal
                if (other.name == "Goal")
                {
                    isGoal = true;
                    GetComponent<MeshRenderer>().material = goldMaterial;
                }
                else if (!isGoal)
                {
                    GetComponent<MeshRenderer>().material = greenMaterial;
                }
            }
        }
        
        // Disables visibility of sphere
        public void DisableSphere()
        {
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<SphereCollider>().enabled = false;
        }

        public bool IsGoal()
        {
            return isGoal;
        }

        public bool IsStart()
        {
            return isStart;
        }

        public bool IsValid()
        {
            return isValid;
        }

        public void SetIsStart(bool thisSphereIsStart)
        {
            isStart = thisSphereIsStart;
        }
    }
}