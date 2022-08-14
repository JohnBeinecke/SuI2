using UnityEngine;

namespace OwnAssets.Scripts
{
    // Handles the overall starting of the A* and MLAgents approaches
    public class WorldManager : MonoBehaviour
    {
        // Reference to the A* kart
        [SerializeField] private MyInputHandler AStarPlayer;
        
        // Reference to the MLAgent kart
        [SerializeField] private GameObject MLAgent;

        // Update is called once per frame
        void Update()
        {
            if (AStarPlayer.IsPlayerReady())
            {
                MLAgent.SetActive(true);
                AStarPlayer.StartPlayer();
            }
        }
    }
}
