using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HighLevelAStarAlgorithm : MonoBehaviour
{
    public GameObject Player;

    PriorityQueue priorityQueue = new PriorityQueue();
    List<Node> visitedNodes = new List<Node>();

    public static List<Node> PathToGoal { get; private set; } = new List<Node>();

    KMeans kMean;

    private void Start()
    {
        // Retrieves the Kmean object from the "GridSystem" mono behaviour object.
        kMean = GetComponent<GridSystem>().Kmean;
    }

    // Update is called once per frame
    void Update()
    {
        // Could be very expensive in terms of performance.
        // Recalculates cluster on each update (because NPCs will be moving on every update).
        kMean.CreateClusters();

        // Retrieves the cluster the player is closest to. Will be the goal cluster for the high level A* algorithm.
        (float,float) goalClusterKey = kMean.NearestCluster(Player);

        Debug.Log($"Player Position:({Player.transform.position.x},{Player.transform.position.z}), Cluster Center: ({goalClusterKey.Item1}, {goalClusterKey.Item2}) ");

        Node? goalCluster = null;

        // Iterates and find the goal cluster the player is currently in/near.
        foreach (var cluster in kMean.clusterNodes)
        {
            if (cluster.center.Item1 == goalClusterKey.Item1 && cluster.center.Item2 == goalClusterKey.Item2)
            {
                goalCluster = cluster;
                break;
            }
        }

        // Calculates the path from all other clusters other than the goal cluster.
        foreach (var cluster in kMean.clusterNodes)
        {
            if(cluster.center.Item1 != goalClusterKey.Item1 && cluster.center.Item2 != goalClusterKey.Item2)
            {
                HighLevelAStar(cluster, goalCluster);
                break;
            }
        }
    }

    private void HighLevelAStar(Node cluster, Node goalCluster)
    {
        priorityQueue = new PriorityQueue();
        visitedNodes = new List<Node>();
        PathToGoal = new List<Node>();
        cluster.StartCost = 0;
        cluster.GoalCost = Heuristics(cluster, goalCluster);

        priorityQueue.Enqueue(cluster, cluster.GoalCost);

        // Adds the starting cluster to our path.
        PathToGoal.Add(cluster);

        while (!priorityQueue.IsEmpty())
        {
            Node currentCluster = priorityQueue.Dequeue();

            if (currentCluster == goalCluster)
            {
                Debug.Log($"Player Goal: ({goalCluster.center.Item1},{goalCluster.center.Item2})");
                PathToGoal.Insert(PathToGoal.Count, goalCluster);
                return;
            }

            visitedNodes.Add(currentCluster);

            foreach(var neighbor in currentCluster.neighorNodes)
            {
                if (visitedNodes.Contains(neighbor))
                    continue;

                float tentativeGScore = currentCluster.StartCost + Heuristics(currentCluster, neighbor);

                // If the neighbor node has not been visited yet, we add it to our priority queue
                // and add the costs of getting to the neighbor cluster + costs of getting to goal from neighbor cluster.
                if (!priorityQueue.Contains(neighbor))
                    priorityQueue.Enqueue(neighbor, tentativeGScore + Heuristics(neighbor, goalCluster));
                else if (tentativeGScore >= neighbor.StartCost)
                    continue;

                PathToGoal.Insert(PathToGoal.Count, neighbor);
                neighbor.StartCost = tentativeGScore;
                neighbor.GoalCost = tentativeGScore + Heuristics(neighbor, goalCluster);
            }
        }
    }

    private float Heuristics(Node currentNode, Node neighborNode)
    {
        return KMeans.Distance(currentNode.center.Item1, 
                            currentNode.center.Item2,
                            neighborNode.center.Item1,
                            neighborNode.center.Item2);
    }


    private void OnDrawGizmos()
    {
        if(PathToGoal.Count > 0)
        {
            Node previousNode = PathToGoal[0];
            for (int i = 1; i < PathToGoal.Count; i++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(new Vector3(previousNode.center.Item1, 0, previousNode.center.Item2),
                    new Vector3(PathToGoal[i].center.Item1, 0, PathToGoal[i].center.Item2));


/*                if(i == PathToGoal.Count - 1)
                {
                    Debug.Log($"Goal Center: ({PathToGoal[i].center.Item1},{PathToGoal[i].center.Item2})");
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(new Vector3(PathToGoal[i].center.Item1, 0, PathToGoal[i].center.Item2),
                        new Vector3(PathToGoal[i].nodeWidth, 0, PathToGoal[i].nodeHeight));
                }*/

                previousNode = PathToGoal[i];
            }
        }
    }
}
