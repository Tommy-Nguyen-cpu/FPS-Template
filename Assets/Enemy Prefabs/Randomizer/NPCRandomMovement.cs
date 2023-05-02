using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCRandomMovement
{
    /// <summary>
    /// Probability of the NPC acting randomly.
    /// </summary>
    const float PROBABILITY = .30f;

    /// <summary>
    /// Determines whether or not there should be randomness.
    /// </summary>
    /// <returns></returns>
    public static bool ShouldRandomize()
    {
        return Random.value < PROBABILITY;
    }

    /// <summary>
    /// Randomizes the NPCs movement by altering its destination.
    /// </summary>
    /// <returns></returns>
    public static Vector3 NewDestination()
    {
        int randomIndex = Random.Range(0, GridSystem.NonTerrainNodes.Count);

        Node randomNode = GridSystem.NonTerrainNodes[randomIndex];

        return new Vector3(randomNode.center.Item1, 0, randomNode.center.Item2);
    }
}
