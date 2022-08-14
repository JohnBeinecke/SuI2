using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OwnAssets.Scripts
{
    /*
     * This class is responsible for the detection, calculation and creation of the path
     * for the A* approach.
     */
    public class TrackManager : MonoBehaviour
    {
        // Utility class used for the A* algorithm
        private class Node
        {
            // Connects the node to the actual GridSphere in the scene
            public readonly Transform Transform;

            // Connects the node to its position in the grid
            public readonly Tuple<int, int> GridIndex;

            // The value of the node (in regards to the A* algorithm)
            public float Value;

            // Constructor
            public Node(Transform t, Tuple<int, int> g, float v)
            {
                Transform = t;
                GridIndex = g;
                Value = v;
            }
        }

        // Enables and disables different debug functionalities
        [SerializeField] private bool isDebug;

        // Transform of the player
        [SerializeField] private Transform player;

        // Prefab of the gridSpheres to spawn
        [SerializeField] private GameObject spherePrefab;

        // Debug material for a sphere that has been checked in the A* algorithm
        [SerializeField] private Material checkedMaterial;

        // Layer of the environment
        [SerializeField] private LayerMask environmentLayer;

        // Layer of the track
        [SerializeField] private LayerMask trackLayer;

        // Size of nodes to spawn in the grid (nodeAmount * nodeAmount)
        [SerializeField] int nodeAmount;

        // Size of the grid in the scene
        [SerializeField] private float gridSize;

        [SerializeField] private Text delayText;

        // Grid of nodes
        private Node[][] grid;

        // Goal node
        private Node goalPoint;

        // Start node
        private Node startPoint;

        // Distance between gridSpheres
        private float stepSize;

        // Timer for delay
        private float currTimer;

        // Amount of delay for delayTimer
        private float delayTimer = 3f;

        // Track returned by A* algorithm
        private List<Node> aStarPath;

        // Track after shortenPath algorithm
        private List<Vector3> shortenedTrack;

        // Track after hermite smoothing
        private List<Vector3> smoothedTrack;

        // Called on first frame, handles the spawning of the gridSpheres 
        void Start()
        {
            // Spawn gridSpheres
            stepSize = gridSize / nodeAmount;
            grid = new Node[nodeAmount][];
            for (int i = 0; i < nodeAmount; i++)
            {
                grid[i] = new Node[nodeAmount];
            }

            for (int i = -nodeAmount / 2; i < nodeAmount / 2; i++)
            {
                for (int j = -nodeAmount / 2; j < nodeAmount / 2; j++)
                {
                    Vector3 posToSpawn =
                        new Vector3(transform.position.x + i * stepSize, transform.position.y,
                            transform.position.z + j * stepSize);

                    GameObject tmp = Instantiate(spherePrefab, posToSpawn, Quaternion.identity, transform);
                    grid[i + nodeAmount / 2][j + nodeAmount / 2] = new Node(tmp.transform,
                        new Tuple<int, int>(i + nodeAmount / 2, j + nodeAmount / 2), 0);
                }
            }

            // Find start
            FindClosestNodeToPlayer();

            // Set timer to allow time for gridSphere collision detection
            currTimer = delayTimer;
        }

        // Finds the closest node to the player
        private void FindClosestNodeToPlayer()
        {
            Collider[] colliders = Physics.OverlapSphere(player.transform.position, stepSize * 2, environmentLayer);

            Collider closestCollider = colliders[0];
            float closestDist = Vector3.Distance(colliders[0].transform.position, player.position);

            for (int i = 1; i < colliders.Length; i++)
            {
                float dist = Vector3.Distance(colliders[i].transform.position, player.position);

                if (dist < closestDist)
                {
                    closestCollider = colliders[i];
                    closestDist = dist;
                }
            }

            closestCollider.GetComponent<Collider>().GetComponent<GridSphere>().SetIsStart(true);
        }


        // Called every frame, waits for timer to run out and then runs A* algorithm
        private void Update()
        {
            if (currTimer > 0f)
            {
                currTimer -= Time.deltaTime;

                delayText.text = ((int)currTimer+1f).ToString();
                if (currTimer <= 0f)
                {
                    delayText.text = "";
                    foreach (Node[] gridLine in grid)
                    {
                        foreach (Node node in gridLine)
                        {
                            // Disable the sphere
                            node.Transform.GetComponent<GridSphere>().DisableSphere();

                            // Check if it was in the goal area
                            if (node.Transform.GetComponent<GridSphere>().IsGoal())
                            {
                                goalPoint = node;
                            }
                            
                            // Check if it was the start node
                            if (node.Transform.GetComponent<GridSphere>().IsStart())
                            {
                                startPoint = node;
                            }
                        }
                    }

                    // Run A*
                    RunAStar();
                }
            }
        }


        // Implementation of the A* pathfinding algorithm (without heuristic)
        private void RunAStar()
        {
            List<Node> openList = new List<Node>();
            List<Node> closedList = new List<Node>();

            int counter = 0;
            openList.Add(startPoint);

            while (openList.Count != 0)
            {
                // Safety fall back to stop infinite loop
                if (++counter > 10000) break;
                
                // Get the current node
                Node currNode = GetNodeWithLowestValue(openList);
                openList.Remove(currNode);
                closedList.Add(currNode);

                // Check if goal node was reached
                if (currNode.Transform.GetComponent<GridSphere>().IsGoal())
                {
                    BackTrackGrid(closedList);
                    return;
                }

                // Get adjacent nodes
                List<Node> currentChildren = GetValidAdjacentNodes(currNode);

                // iterate through neighbour nodes
                for (int i = 0; i < currentChildren.Count; i++)
                {
                    Node child = currentChildren[i];
                    if (closedList.Contains(child)) continue;

                    float tmp = currNode.Value +
                                (Vector3.Distance(currNode.Transform.position, child.Transform.position));

                    if (openList.Contains(child))
                    {
                        if (tmp <= child.Value)
                        {
                            child.Value = tmp;
                        }
                    }
                    else
                    {
                        child.Value = tmp;
                        openList.Add(child);
                    }
                }
            }

            //Debug.Log("I didn't find the goal after checking X nodes: " + closedList.Distinct().Count());
            //Debug.Log("I didn't find the goal after checking X nodes: " + closedList.Count());

            // Mark checked nodes for debugging, if A* was unsuccessful
            foreach (Node node in closedList)
            {
                node.Transform.GetComponent<MeshRenderer>().material = checkedMaterial;
            }
        }

        // Goes from the start backwards and creates the path by always looking for the node with the smallest value
        private void BackTrackGrid(List<Node> checkedNodes)
        {
            // A* path
            aStarPath = new List<Node>();

            // Start at end
            Node currNode = goalPoint;

            // Iterate until at start
            while (!currNode.Equals(startPoint))
            {
                // Add current node
                aStarPath.Add(currNode);
                
                // This gets the checked neighbours and removes to nodes that are already in the A* path
                List<Node> removedOldNodes =
                    GetValidAdjacentNodes(currNode).Intersect(checkedNodes).Except(aStarPath).ToList();
                
                // Get neighbour with lowest value
                currNode = GetNodeWithLowestValue(removedOldNodes);
            }

            // Reverse the path
            aStarPath.Reverse();
            
            // Continue with further setup
            SetupPathForPlayer();
        }

        // This handles further optimization of the path after the A* path was created
        private void SetupPathForPlayer()
        {
            // Set y to player height (so the player aims for nodes at his height)
            for (int i = 0; i < aStarPath.Count; i++)
            {
                aStarPath[i].Transform
                    .Translate(new Vector3(0, player.position.y - aStarPath[i].Transform.position.y, 0));
            }

            // shortenPath algorithm
            List<Node> optimizedPath = new List<Node>();
            optimizedPath.Add(aStarPath[0]);

            int currIndex = 1;
            while (currIndex < aStarPath.Count)
            {
                for (int i = currIndex; i < aStarPath.Count; i++)
                {
                    if (i == aStarPath.Count - 1)
                    {
                        currIndex = aStarPath.Count;
                        optimizedPath.Add(aStarPath[^1]);
                        break;
                    }

                    float thickness = 0.75f;
                    Vector3 origin = aStarPath[currIndex].Transform.position;
                    Vector3 dir = aStarPath[i + 1].Transform.position - aStarPath[currIndex].Transform.position;
                    Vector3 leftShift = new Vector3(-dir.x, 0, dir.z).normalized * thickness;
                    Vector3 rightShift = new Vector3(dir.x, 0, -dir.z).normalized * thickness;
                    /*
                        Debug.DrawLine(origin,shortestPath[i+1].transform.position,Color.red,100f);
                        Debug.DrawLine(origin+leftShift,shortestPath[i+1].transform.position+leftShift,Color.yellow,100f);
                        Debug.DrawLine(origin+rightShift,shortestPath[i+1].transform.position+rightShift,Color.yellow, 100f);
                    */
                    
                    // Check with additional width
                    if (Physics.Linecast(origin, aStarPath[i + 1].Transform.position, trackLayer) ||
                        Physics.Linecast(origin + leftShift, aStarPath[i + 1].Transform.position + leftShift,
                            trackLayer) ||
                        Physics.Linecast(origin + rightShift, aStarPath[i + 1].Transform.position + rightShift,
                            trackLayer))
                    {
                        optimizedPath.Add(aStarPath[i]);
                        aStarPath[currIndex].Transform.GetComponent<MeshRenderer>().material = checkedMaterial;
                        currIndex = i;
                        break;
                    }
                }
            }

            // Reduce optimizedPath to its positions
            shortenedTrack = new List<Vector3>();
            foreach (var t in optimizedPath)
            {
                shortenedTrack.Add(t.Transform.position);
            }

            // Smooth track with hermite spline
            smoothedTrack = HermiteSpline(shortenedTrack, 10);

            // Setup player input handler
            player.GetComponent<MyInputHandler>().Setup(smoothedTrack);
        }

        // Hermite interpolation based on public domain code: https://en.wikibooks.org/wiki/Cg_Programming/Unity/Hermite_Curves
        private List<Vector3> HermiteSpline(List<Vector3> controlPoints, int numberOfPoints)
        {
            List<Vector3> newTrack = new List<Vector3>();
            // loop over segments of spline
            Vector3 p0, p1, m0, m1;

            for (int j = 0; j < controlPoints.Count - 1; j++)
            {
                // check control points
                if (controlPoints[j] == null ||
                    controlPoints[j + 1] == null ||
                    (j > 0 && controlPoints[j - 1] == null) ||
                    (j < controlPoints.Count - 2 && controlPoints[j + 2] == null))
                {
                    break;
                }

                // determine control points of segment
                p0 = controlPoints[j];
                p1 = controlPoints[j + 1];

                if (j > 0)
                {
                    m0 = 0.5f * (controlPoints[j + 1]
                                 - controlPoints[j - 1]);
                }
                else
                {
                    m0 = controlPoints[j + 1]
                         - controlPoints[j];
                }

                if (j < controlPoints.Count - 2)
                {
                    m1 = 0.5f * (controlPoints[j + 2]
                                 - controlPoints[j]);
                }
                else
                {
                    m1 = controlPoints[j + 1]
                         - controlPoints[j];
                }

                // set points of Hermite curve
                Vector3 position;
                float t;
                float pointStep = 1.0f / numberOfPoints;

                if (j == controlPoints.Count - 2)
                {
                    pointStep = 1.0f / (numberOfPoints - 1.0f);
                    // last point of last segment should reach p1
                }

                for (int i = 0; i < numberOfPoints; i++)
                {
                    t = i * pointStep;
                    position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * p0
                               + (t * t * t - 2.0f * t * t + t) * m0
                               + (-2.0f * t * t * t + 3.0f * t * t) * p1
                               + (t * t * t - t * t) * m1;

                    newTrack.Add(position);
                }
            }

            return newTrack;
        }


        // Gets valid neighbours (Moore neighbourhood)
        private List<Node> GetValidAdjacentNodes(Node node)
        {
            List<Node> validNodes = new List<Node>();
            Tuple<int, int> nodeIdx = node.GridIndex;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    if (nodeIdx.Item1 + i >= 0 && nodeIdx.Item1 + i < grid.Length && nodeIdx.Item2 + j >= 0 &&
                        nodeIdx.Item2 + j < grid[nodeIdx.Item1 + i].Length &&
                        grid[nodeIdx.Item1 + i][nodeIdx.Item2 + j].Transform.GetComponent<GridSphere>().IsValid())
                    {
                        validNodes.Add(grid[nodeIdx.Item1 + i][nodeIdx.Item2 + j]);
                    }
                }
            }

            return validNodes;
        }

        // Gets the node with the lowest value
        private static Node GetNodeWithLowestValue(List<Node> nodes)
        {
            if (nodes.Count == 1) return nodes[0];
            Node currLowest = nodes[0];

            foreach (Node currNode in nodes)
            {
                if (currNode.Value <
                    currLowest.Value)
                {
                    currLowest = currNode;
                }
            }

            return currLowest;
        }

        // Debugging help
        public void OnDrawGizmos()
        {
            if (smoothedTrack.Count > 1 && isDebug)
            {
                for (int i = 0; i < smoothedTrack.Count - 1; i++)
                {
                    //Gizmos.DrawSphere(smoothedTrack[i], 0.25f);
                    //DrawLine(smoothedTrack[i], smoothedTrack[i + 1], 20);
                }

                //DrawLine(smoothedTrack[^1], smoothedTrack[0], 20);
            }

            if (shortenedTrack.Count > 1 && isDebug)
            {
                for (int i = 0; i < shortenedTrack.Count - 1; i++)
                {
                    Color oldColor = Gizmos.color;
                    Gizmos.color = Color.blue;
                    // Gizmos.DrawSphere(filledTrack[i], 0.25f);
                    Gizmos.color = oldColor;
                    //DrawLine(filledTrack[i], filledTrack[i + 1], 20);
                }

                //DrawLine(filledTrack[^1], filledTrack[0], 20);
            }
        }
        
        // Additional debugging
        public static void DrawLine(Vector3 p1, Vector3 p2, float width)
        {
            int count = 1 + Mathf.CeilToInt(width); // how many lines are needed.
            if (count == 1)
            {
                Gizmos.DrawLine(p1, p2);
            }
            else
            {
                Camera c = Camera.main;
                if (c == null)
                {
                    Debug.LogError("Camera.current is null");
                    return;
                }

                var scp1 = c.WorldToScreenPoint(p1);
                var scp2 = c.WorldToScreenPoint(p2);

                Vector3 v1 = (scp2 - scp1).normalized; // line direction
                Vector3 n = Vector3.Cross(v1, Vector3.forward); // normal vector

                Color oldColor = Gizmos.color;
                Gizmos.color = Color.cyan;
                for (int i = 0; i < count; i++)
                {
                    Vector3 o = 0.99f * n * width * ((float)i / (count - 1) - 0.5f);
                    Vector3 origin = c.ScreenToWorldPoint(scp1 + o);
                    Vector3 destiny = c.ScreenToWorldPoint(scp2 + o);
                    Gizmos.DrawLine(origin, destiny);
                }

                Gizmos.color = oldColor;
            }
        }
    }
}