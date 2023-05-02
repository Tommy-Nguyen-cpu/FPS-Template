using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LowLevelAStar
{

    public List<Node> PathToGoal { get; private set; } = new List<Node>();

    Node? goalNode = null;

    public LowLevelAStar(GameObject Player)
    {
        goalNode = FindNode(Player);
    }

    public void CalculatePath(GameObject NPC)
    {
        Node npcVector = FindNode(NPC);

        // Only time it is null is if the NPC falls off the map.
        if (npcVector == null)
            return;

        PathToGoal = new List<Node>();
        PriorityQueue priorityQueue = new PriorityQueue();
        List<Node> visitedNodes = new List<Node>();

        npcVector.StartCost = 0;
        npcVector.GoalCost = Heuristics(npcVector, goalNode);

        priorityQueue.Enqueue(npcVector, npcVector.GoalCost);

        while (!priorityQueue.IsEmpty())
        {
            Node currentNode = priorityQueue.Dequeue();

            if(currentNode == goalNode)
            {
                PathToGoal.Insert(PathToGoal.Count, currentNode);
                return;
            }

            visitedNodes.Add(currentNode);

            foreach(var neighbor in currentNode.neighorNodes)
            {
                if (visitedNodes.Contains(neighbor))
                    continue;

                float tentativeGScore = currentNode.StartCost + Heuristics(currentNode, neighbor);

                if (!priorityQueue.Contains(neighbor))
                {
                    priorityQueue.Enqueue(neighbor, tentativeGScore + Heuristics(neighbor, goalNode));
                }
                else if (tentativeGScore >= neighbor.StartCost)
                    continue;

                PathToGoal.Insert(PathToGoal.Count, neighbor);
                neighbor.StartCost = tentativeGScore;
                neighbor.GoalCost = tentativeGScore + Heuristics(neighbor, goalNode);
            }
        }
    }

    /// <summary>
    /// Attempts to find the node the player is in.
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    private Node? FindNode(GameObject character)
    {
        Debug.Log($"Num of Nodes: {GridSystem.NonTerrainNodes.Count}");
        foreach (var node in GridSystem.NonTerrainNodes)
        {
            float halfWidth = node.nodeWidth / 2;
            float halfHeight = node.nodeHeight / 2;
            float nodeMaxEdgeX = node.center.Item1 + halfWidth;
            float nodeMinEdgeX = node.center.Item1 - halfWidth;
            float nodeMaxEdgeZ = node.center.Item2 + halfHeight;
            float nodeMinEdgeZ = node.center.Item2 - halfHeight;

            float characterX = character.transform.position.x;
            float characterZ = character.transform.position.z;

            if(characterX >= nodeMinEdgeX && characterX <= nodeMaxEdgeX && characterZ >= nodeMinEdgeZ && characterZ <= nodeMaxEdgeZ)
            {
                return node;
            }
        }

        return null;
    }

    float Heuristics(Node npcVector, Node goalNode)
    {
        return KMeans.Distance(npcVector.center.Item1, 
                                npcVector.center.Item2,
                                goalNode.center.Item1,
                                goalNode.center.Item2);
    }
}
