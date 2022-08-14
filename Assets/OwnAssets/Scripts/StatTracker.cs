using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KartGame.KartSystems;
using UnityEngine;

public class StatTracker : MonoBehaviour
{
    class CarStats
    {
        public Rigidbody carBody;
        public string name;
        public float timeToFinishLine;
        public List<float> speedOverTime;
        public int wallHits;
        public bool isFinished;

        public CarStats(string name, Rigidbody carBody)
        {
            this.name = name;
            this.carBody = carBody;
            timeToFinishLine = 0f;
            speedOverTime = new List<float>();
            wallHits = 0;
            isFinished = false;
        }
    }

    private CarStats[] players;

    private bool isRunning;

    private float trackingInterval = 0.1f;

    private float currTime;

    private int playersFinished;

    // Start is called before the first frame update
    void Start()
    {
        isRunning = false;
    }

    public void StartTracking(Rigidbody AStarBody, Rigidbody MLBody)
    {
        currTime = trackingInterval;
        playersFinished = 0;
        players = new CarStats[2];
        players[0] = new CarStats("KartClassic_Player", AStarBody);
        players[1] = new CarStats("KartClassic_MLAgent", MLBody);
        isRunning = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            for (int i = 0; i < players.Length; i++)
            {
                // update timeToFinishLine
                if (!players[i].isFinished)
                {
                    players[i].timeToFinishLine += Time.deltaTime;
                }
            }

            if (currTime > 0f)
            {
                currTime -= Time.deltaTime;

                if (currTime <= 0f)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        // update speedOverTime
                        if (!players[i].isFinished)
                        {
                            players[i].speedOverTime.Add(players[i].carBody.velocity.magnitude);
                        }
                    }

                    currTime = trackingInterval;
                }
            }

            if (playersFinished >= players.Length)
            {
                isRunning = false;
                WriteResults();
            }
        }
    }

    private void WriteResults()
    {
        for (int i = 0; i < players.Length; i++)
        {
            Debug.Log("Name: " + players[i].name + ", TTF: " + players[i].timeToFinishLine + ", Collisions: " + players[i].wallHits);
        }

        string filePath = "Z:/save.csv";

        StreamWriter writer = new StreamWriter(filePath);

        writer.WriteLine("Seconds;AStar;MLAgent");

        for (int i = 0; i < Math.Min(players[0].speedOverTime.Count, players[1].speedOverTime.Count); i++)
        {
            string row = $"{i * trackingInterval:0.##}" + ";" + players[0].speedOverTime[i] + ";" +
                         players[1].speedOverTime[i];

            writer.WriteLine(row);
        }

        writer.Flush();
        writer.Close();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isRunning)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (other.gameObject.GetComponentInParent<ArcadeKart>().name == players[i].name &&
                    !players[i].isFinished)
                {
                    players[i].isFinished = true;
                    playersFinished++;
                }
            }
        }
    }
}