using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    /// <summary>
    /// Estimated cost of reaching the current node from the start node (distance).
    /// </summary>
    public float StartCost { get; set; }

    /// <summary>
    /// Estimated total cost of reaching the goal.
    /// </summary>
    public float GoalCost { get; set; }
    public float nodeWidth { get; private set; } = 0.0f;
    public float nodeHeight { get; private set; } = 0.0f;

    /// <summary>
    ///  Contains the position of the node centered at the coordinates "center".
    ///  item1 = X coordinate and item2 = Z coordinate.
    /// </summary>
    public (float, float) center { get; private set; } = (0, 0);

    public List<Node> neighorNodes { get; private set; } = new List<Node>();

    public Node(float width, float height, (float,float) centerPoint)
    {
        nodeWidth = width;
        nodeHeight = height;
        center = centerPoint;
    }

    public void AddNeighbor(Node newNode)
    {
        neighorNodes.Add(newNode);
    }
}
