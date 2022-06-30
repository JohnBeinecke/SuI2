using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OwnAssets.Scripts;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    private class Node
    {
        public Transform transform;
        public Tuple<int, int> gridIndex;
        public float value;

        public Node(Transform t, Tuple<int, int> g, float v)
        {
            transform = t;
            gridIndex = g;
            value = v;
        }
    }


    [SerializeField] private Transform player;
    [SerializeField] private GameObject spherePrefab;
    private Node[][] grid;
    private Node goalPoint;
    private Node startPoint;
    [SerializeField] private Material checkedMaterial;
    [SerializeField] private LayerMask environmentLayer;
    [SerializeField] private LayerMask trackLayer;

    [SerializeField] int nodeAmount;
    [SerializeField] private float gridSize;
    private float stepSize;

    void Start()
    {
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

        findClosestNodeToPlayer();
    }

    private void findClosestNodeToPlayer()
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

        closestCollider.GetComponent<Collider>().GetComponent<GridSphere>().setIsStart(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            foreach (Node[] gridLine in grid)
            {
                foreach (Node node in gridLine)
                {
                    if (node.transform.GetComponent<GridSphere>().IsGoal())
                    {
                        goalPoint = node;
                    }

                    if (node.transform.GetComponent<GridSphere>().IsStart())
                    {
                        startPoint = node;
                    }
                }
            }

            RunAStar();
        }
    }


    public void RunAStar()
    {
        List<Node> openList = new List<Node>();
        List<Node> closedList = new List<Node>();

        int counter = 0;
        openList.Add(startPoint);

        while (openList.Count != 0)
        {
            if (++counter > 10000) break;
            // Get the current node
            Node currNode = GetNodeWithLowestValue(openList);
            openList.Remove(currNode);
            closedList.Add(currNode);

            // Found the goal
            if (currNode.transform.GetComponent<GridSphere>().IsGoal())
            {
                Debug.Log("Found Goal!");
                BackTrackGrid(closedList);
                foreach (Node node in closedList)
                {
                    //node.transform.GetComponent<MeshRenderer>().material = checkedMaterial;
                    //node.transform.GetComponent<GridSphere>().setValue(node.value);
                }

                return;
            }

            List<Node> currentChildren = GetValidAdjacentNodes(currNode);

            for (int i = 0; i < currentChildren.Count; i++)
            {
                Node child = currentChildren[i];
                if (closedList.Contains(child)) continue;

                float tmp = currNode.value + (Vector3.Distance(currNode.transform.position, child.transform.position));

                if (openList.Contains(child))
                    if (tmp > child.value)
                    {
                    }
                    else
                    {
                        child.value = tmp;
                    }
                else
                {
                    child.value = tmp;
                    openList.Add(child);
                }
            }
        }

        Debug.Log("I didn't find the goal after checking X nodes: " + closedList.Distinct().Count());
        Debug.Log("I didn't find the goal after checking X nodes: " + closedList.Count());

        foreach (Node node in closedList)
        {
            node.transform.GetComponent<MeshRenderer>().material = checkedMaterial;
            node.transform.GetComponent<GridSphere>().setValue(node.value);
        }
    }

    private List<Node> shortestPath;

    private void BackTrackGrid(List<Node> checkedNodes)
    {
        shortestPath = new List<Node>();

        Node currNode = goalPoint;

        while (!currNode.Equals(startPoint))
        {
            shortestPath.Add(currNode);
            List<Node> removedOldNodes =
                GetValidAdjacentNodes(currNode).Intersect(checkedNodes).Except(shortestPath).ToList();
            //Debug.Log("VALID NODES: "+removedOldNodes.Count);
            currNode = GetNodeWithLowestValue(removedOldNodes);
        }

        shortestPath.Reverse();
        Debug.Log("ShortestPath Length: " + shortestPath.Count);
        SetupPathForPlayer();
    }

    private void SetupPathForPlayer()
    {
        // Set y to player height
        for (int i = 0; i < shortestPath.Count; i++)
        {
            shortestPath[i].transform
                .Translate(new Vector3(0, player.position.y - shortestPath[i].transform.position.y, 0));
        }

        // optimize path
        List<Node> optimizedPath = new List<Node>();
        optimizedPath.Add(shortestPath[0]);

        int currIndex = 1;
        while (currIndex < shortestPath.Count)
        {
            for (int i = currIndex; i < shortestPath.Count; i++)
            {
                if (i == shortestPath.Count - 1)
                {
                    currIndex = shortestPath.Count;
                    optimizedPath.Add(shortestPath[^1]);
                    break;
                }

                float thickness = 0.75f;
                Vector3 origin = shortestPath[currIndex].transform.position;
                Vector3 dir = shortestPath[i + 1].transform.position - shortestPath[currIndex].transform.position;
                Vector3 leftShift = new Vector3(-dir.x, 0, dir.z).normalized * thickness;
                Vector3 rightShift = new Vector3(dir.x, 0, -dir.z).normalized * thickness;
                /*
                Debug.DrawLine(origin,shortestPath[i+1].transform.position,Color.red,100f);
                Debug.DrawLine(origin+leftShift,shortestPath[i+1].transform.position+leftShift,Color.yellow,100f);
                Debug.DrawLine(origin+rightShift,shortestPath[i+1].transform.position+rightShift,Color.yellow, 100f);
                */
                if(Physics.Linecast(origin,shortestPath[i+1].transform.position,trackLayer)||
                    Physics.Linecast(origin+leftShift,shortestPath[i+1].transform.position+leftShift,trackLayer)||
                    Physics.Linecast(origin+rightShift,shortestPath[i+1].transform.position+rightShift,trackLayer))
                {
                    optimizedPath.Add(shortestPath[i]);
                    shortestPath[currIndex].transform.GetComponent<MeshRenderer>().material = checkedMaterial;
                    currIndex = i;
                    break;
                }
            }
        }

        shortestPath = optimizedPath;
        
        //disableAllSpheres
        for (int i = 0; i < grid.Length; i++)
        {
            for (int j = 0; j < grid[i].Length; j++)
            {
                grid[i][j].transform.GetComponent<SphereCollider>().enabled = false;
            }
        }

        player.GetComponent<MyInputHandler>().Setup(shortestPath.Select(n => n.transform).ToList());
    }

    private List<Node> GetValidAdjacentNodes(Node node)
    {
        List<Node> validNodes = new List<Node>();
        Tuple<int, int> nodeIdx = node.gridIndex;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                if (nodeIdx.Item1 + i >= 0 && nodeIdx.Item1 + i < grid.Length && nodeIdx.Item2 + j >= 0 &&
                    nodeIdx.Item2 + j < grid[nodeIdx.Item1 + i].Length &&
                    grid[nodeIdx.Item1 + i][nodeIdx.Item2 + j].transform.GetComponent<GridSphere>().IsValid())
                {
                    validNodes.Add(grid[nodeIdx.Item1 + i][nodeIdx.Item2 + j]);
                }
            }
        }

        return validNodes;
    }


    private Node GetNodeWithLowestValue(List<Node> nodes)
    {
        if (nodes.Count == 1) return nodes[0];
        Node currLowest = nodes[0];

        foreach (Node currNode in nodes)
        {
            if (currNode.value <
                currLowest.value)
            {
                currLowest = currNode;
            }
        }

        return currLowest;
    }

    public void OnDrawGizmos()
    {
        if (shortestPath != null)
        {
            for (int i = 0; i < shortestPath.Count - 1; i++)
            {
                DrawLine(shortestPath[i].transform.position, shortestPath[i + 1].transform.position, 20);
            }
        }
    }

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