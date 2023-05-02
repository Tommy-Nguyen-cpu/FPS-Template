using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityQueue
{
    List<Node> Queue = new List<Node>();
    List<float> DistancePriority = new List<float>();
    public void Enqueue(Node newNode, float estimatedCostToGoal)
    {
        if (Queue.Count == 0)
        {
            Queue.Add(newNode);
            DistancePriority.Add(estimatedCostToGoal);
        }
        else
        {
            float firstDistance = DistancePriority[DistancePriority.Count - 1];
            if(firstDistance > estimatedCostToGoal)
            {
                Queue.Insert(Queue.Count - 1, newNode);
                DistancePriority.Insert(DistancePriority.Count - 1, estimatedCostToGoal);
            }
        }
    }


    public Node Dequeue()
    {
        Node firstNode = Queue[Queue.Count - 1];
        Queue.RemoveAt(Queue.Count - 1);
        DistancePriority.RemoveAt(DistancePriority.Count - 1);
        return firstNode;
    }

    public bool IsEmpty()
    {
        return (Queue.Count == 0);
    }

    public bool Contains(Node node)
    {
        return Queue.Contains(node);
    }
}
