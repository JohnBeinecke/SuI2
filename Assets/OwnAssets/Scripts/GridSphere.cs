using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GridSphere : MonoBehaviour
{
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material goldMaterial;
    [SerializeField] private Material pinkMaterial;
    [SerializeField] private LayerMask trackLayer;
    [SerializeField] private Transform player;

    private bool isStart;
    private bool isGoal;
    private bool isValid;

    private void Start()
    {
        //GetComponentInChildren<TextMeshPro>().text = "";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Physics.Linecast(transform.position + new Vector3(0, 2, 0), transform.position + new Vector3(0,1,0),trackLayer)) return;
        if (!Physics.Linecast(transform.position + new Vector3(0, -1, 0), transform.position, trackLayer)) return;

        Vector3 overHeadPos = transform.position + new Vector3(0, 1, 0);
        for (int i = -1; i <= 1; i++)
        {
            for(int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                if (Physics.Linecast(overHeadPos, overHeadPos + new Vector3(i, 0, j), trackLayer))
                {
                    return;
                }
                //Debug.DrawLine(overHeadPos, overHeadPos + new Vector3(i,0,j), Color.red, 20f);
            }
        }

        isValid = true; 
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

    public void setIsStart(bool thisSphereIsStart)
    {
        isStart = thisSphereIsStart;
    }

    public void setValue(float value)
    {
       //GetComponentInChildren<TextMeshPro>().text = $"{value:F1}";
    }
}