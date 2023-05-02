using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    #region NPC Action
    /// <summary>
    /// Ensures that random actions don't occur on every frame.
    /// </summary>
    float timeSinceLastRandomAction = 0.0f;

    /// <summary>
    /// How long we want random action to occur for.
    /// </summary>
    float randomActionDuration = 10.0f;


    ParticleSystem particleSystem;
    float damageAnimationDuration = 1.0f;
    float timeSinceDamageAnimationStart = 0.0f;
    bool WasHit = false;
    #endregion

    #region Enemy Stats
    /// <summary>
    /// General stopping distance.
    /// </summary>
    float stoppingDistance = 10f;
    float npcSpeed = 5f;

    float npcHealth = 50f;

    public float damageDealt = 0.01f;
    #endregion

    Rigidbody rb;

    GameObject Player;

    /// <summary>
    /// Fields used by enemy to move towards the player using low level A* algorithm.
    /// </summary>
    #region A* Algorithm
    LowLevelAStar localAStar;

    /// <summary>
    /// The index of the current node the NPC is in for the low level A* algorithm.
    /// </summary>
    int pathIndex = -1;

    /// <summary>
    /// The vector, node, or cluster that the NPC is trying to reach.
    /// </summary>
    Vector3? nodeGoalDestination = null;

    /// <summary>
    /// Whether or not the NPC was able to reach the edge of a cluster.
    /// </summary>
    bool reachedClusterEdge = false;

    /// <summary>
    /// Keeps track of the current cluster the NPC is in.
    /// </summary>
    int currentClusterIndex = -1;
    #endregion

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Player = GameObject.Find("Player");
        localAStar = new LowLevelAStar(Player);
        particleSystem = GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// Is only ran once (at the beginning) to retrieve the index of the cluster the NPC is in.
    /// </summary>
    private void Update()
    {

        // Currently, NPCs that fall off the map are still counted in the algorithm.
        if (transform.position.y < 0 || npcHealth == 0)
        {
            Debug.Log("NPC FELL OFF THE MAP OR LOST ALL HEALTH!");
            GridSystem.NPCs.Remove(gameObject);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Used to determine whether the NPC should move or not based on whether the NPC is on the edge of the cluster.
    /// </summary>
    private void LateUpdate()
    {
        timeSinceLastRandomAction += Time.deltaTime;

        if (WasHit)
        {
            timeSinceDamageAnimationStart += Time.deltaTime;

            if(timeSinceDamageAnimationStart >= damageAnimationDuration)
            {
                WasHit = false;
                timeSinceDamageAnimationStart = 0.0f;
                particleSystem.Stop();
            }
        }


        // If NPC is within player range
        float distanceFromPlayer = Mathf.Abs(Vector3.Distance(transform.position, Player.transform.position));
        if(distanceFromPlayer <= stoppingDistance)
        {
            // If the player and NPC are in the same node, NPC should follow the player.
            if(localAStar.PathToGoal.Count == 0)
            {
                pathIndex = -1;
                nodeGoalDestination = new Vector3(Player.transform.position.x, 0, Player.transform.position.z);
            }
            // Otherwise, if the player and the NPC are in different nodes, find path to player.
            else
            {
                // If the NPC is at the goal or if we haven't ran the local yet, run the local A* algorithm.
                if (pathIndex == -1 || pathIndex >= localAStar.PathToGoal.Count - 1)
                {
                    localAStar.CalculatePath(gameObject);
                    pathIndex = 0;
                    Node nodeToReach = localAStar.PathToGoal[pathIndex];
                    nodeGoalDestination = new Vector3(nodeToReach.center.Item1, 0, nodeToReach.center.Item2);
                }

                // Otherwise, increment the node index.
                else
                {
                    pathIndex++;
                    Node nodeToReach = localAStar.PathToGoal[pathIndex];
                    nodeGoalDestination = new Vector3(nodeToReach.center.Item1, 0, nodeToReach.center.Item2);
                }
            }
        }

        // If the NPC is NOT within range
        else
        {
            if (timeSinceLastRandomAction >= randomActionDuration && NPCRandomMovement.ShouldRandomize())
            {
                currentClusterIndex = -1;
                nodeGoalDestination = NPCRandomMovement.NewDestination();

                // Resets the random timer.
                timeSinceLastRandomAction = 0.0f;
                return;
            }

            // If we're running this for the first time or if the next cluster edge has already been reached.
            if(nodeGoalDestination == null || reachedClusterEdge)
            {
                // Retrieve the current cluster index of the NPC.
                // We don't need to increment because the clusters are recalculated every frame.
                currentClusterIndex = GetClusterIndex();

                // If the current cluster is not the goal cluster.
                if(currentClusterIndex != HighLevelAStarAlgorithm.PathToGoal.Count - 1)
                {
                    // Get the edge of the NEXT cluster the NPC should move towards
                    Node nextCluster = HighLevelAStarAlgorithm.PathToGoal[currentClusterIndex + 1];

                    Debug.Log($"Next Next: ({nextCluster.center.Item1},{nextCluster.center.Item2})");

                    nodeGoalDestination = GetClusterEdge(nextCluster.center, nextCluster.nodeWidth, nextCluster.nodeHeight);
                    Debug.Log($"Cluster Edge: ({nodeGoalDestination.Value.x},{nodeGoalDestination.Value.z})");
                }

                // We reset "reachedCLusterEdge" field to false.
                reachedClusterEdge = false;
            }

            // If the NPC reaches the edge, switch to another edge of the next cluster to go to.
            float distanceFromNextClusterEdge = Vector3.Distance(transform.position, (Vector3)nodeGoalDestination);
            if(distanceFromNextClusterEdge <= stoppingDistance)
            {
                reachedClusterEdge = true;
            }
        }
    }

    private void FixedUpdate()
    {
        if(nodeGoalDestination != null)
        {
            // Retrieves the unit vector (direction vector) between the current NPCs position and the goal node position.
            // Used to determine the direction the NPC should move in.
            Vector3 npcMovementDirection = ((Vector3)nodeGoalDestination - transform.position).normalized;
            rb.velocity = npcMovementDirection * npcSpeed;
        }
    }

    /// <summary>
    /// Gets the index of the cluster the NPC is currently in.
    /// </summary>
    /// <returns></returns>
    private int GetClusterIndex()
    {
        // Finds the cluster the NPC is currently in.
        for(int i = 0; i < HighLevelAStarAlgorithm.PathToGoal.Count; i++)
        {
            // Edges of the cluster.
            Debug.Log($"Half-Width: {HighLevelAStarAlgorithm.PathToGoal[i].nodeWidth / 2}, Half-Height: {HighLevelAStarAlgorithm.PathToGoal[i].nodeHeight / 2}");
            float maxEdgeX = HighLevelAStarAlgorithm.PathToGoal[i].center.Item1 + (HighLevelAStarAlgorithm.PathToGoal[i].nodeWidth/2);
            float minEdgeX = HighLevelAStarAlgorithm.PathToGoal[i].center.Item1 - (HighLevelAStarAlgorithm.PathToGoal[i].nodeWidth/2);
            float maxEdgeZ = HighLevelAStarAlgorithm.PathToGoal[i].center.Item2 + (HighLevelAStarAlgorithm.PathToGoal[i].nodeHeight/2);
            float minEdgeZ = HighLevelAStarAlgorithm.PathToGoal[i].center.Item2 - (HighLevelAStarAlgorithm.PathToGoal[i].nodeHeight/2);

            float npcX = transform.position.x;
            float npcZ = transform.position.z;

            Debug.Log($"NPC: ({npcX},{npcZ}), MAX: ({maxEdgeX},{maxEdgeZ}), MIN: ({minEdgeX},{minEdgeZ})");

            // If NPC is within the bounds of the cluster.
            if(npcX >= minEdgeX && npcX <= maxEdgeX && npcZ >= minEdgeZ && npcZ <= maxEdgeZ)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Retrieves a Vector3 representing the edge the NPC should move towards.
    /// </summary>
    /// <param name="clusterCenter"></param>
    /// <param name="clusterWidth"></param>
    /// <param name="clusterHeight"></param>
    /// <returns></returns>
    private Vector3 GetClusterEdge((float,float) clusterCenter, float clusterWidth, float clusterHeight)
    {
        // Get the center position of the cluster
        Vector3 clusterPos = new Vector3(clusterCenter.Item1, 0, clusterCenter.Item2);

        // Calculate the half-width and half-height of the cluster
        float halfWidth = clusterWidth / 2f;
        float halfHeight = clusterHeight / 2f;

        // Calculate the position of the edge of the cluster closest to the NPC
        float xDiff = transform.position.x - clusterPos.x;
        float yDiff = transform.position.z - clusterPos.z;

        // Basically makes sure the values are inside the bounds of the cluster.
        float xEdge = Mathf.Clamp(clusterPos.x + Mathf.Sign(xDiff) * halfWidth, clusterPos.x - halfWidth, clusterPos.x + halfWidth);
        float yEdge = Mathf.Clamp(clusterPos.z + Mathf.Sign(yDiff) * halfHeight, clusterPos.z - halfHeight, clusterPos.z + halfHeight);
        Vector3 edgePos = new Vector3(xEdge, transform.position.y, yEdge);

        return edgePos;
    }


    private void OnDrawGizmos()
    {
        if(localAStar.PathToGoal.Count > 0)
        {
            Node previousNode = localAStar.PathToGoal[0];

            for(int i = 1; i < localAStar.PathToGoal.Count; i++)
            {
                Gizmos.DrawLine(new Vector3(previousNode.center.Item1, 0, previousNode.center.Item2),
                    new Vector3(localAStar.PathToGoal[i].center.Item1,0, localAStar.PathToGoal[i].center.Item2));

                previousNode = localAStar.PathToGoal[i];
            }
        }
    }


    public void BulletHit()
    {
        particleSystem.Play();

        npcHealth -= 10f;

        Debug.Log("Hit by bullet!");
        WasHit = true;
    }
}
