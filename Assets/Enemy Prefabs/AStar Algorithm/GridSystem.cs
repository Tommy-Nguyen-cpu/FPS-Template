using System.Collections.Generic;
using UnityEngine;

public class GridSystem : MonoBehaviour
{
    /// <summary>
    /// Size of the map.
    /// </summary>
    float mapHeight = 250f;
    float mapWidth = 250f;

    /// <summary>
    /// Useful for when we move the map (and the map grid) to a starting coordinate other than (0,0).
    /// </summary>
    (float, float) origin = (0f, 0f);

    /// <summary>
    /// Each node will have a width and height of 10.
    /// </summary>
    float nodeHeight = 10f;
    float nodeWidth = 10f;

    // Dictionary containing the "row" index as the key and another dictionary as the value.
    // The inner dictionary contains the column as the key and the node in the (row,column) coordinate as the value.
    public static Dictionary<float, Dictionary<float, Node>> nodeMatrix = new Dictionary<float, Dictionary<float, Node>>();

    // Lists containing NPCs that 
    public static List<GameObject> NPCs = new List<GameObject>();

    /// <summary>
    /// Used by the low level A* algorithm to traverse to the player.
    /// </summary>
    public static List<Node> NonTerrainNodes = new List<Node>();

    public KMeans Kmean { get; private set; } = new KMeans();

    #region Test

    public Terrain terrain;
    public GameObject Enemy;

    #endregion


    void Start()
    {
        // Iterates through the entire map (250 width by 250 height) and initializes nodes for each of them.
        for (int row = 0; row < mapHeight; row += (int)nodeWidth)
        {
            for (int column = 0; column < mapWidth; column += (int)nodeHeight)
            {
                // Creates a node with the specified width, height, and centered at the point specified.
                Node newNode = new Node(nodeWidth, nodeHeight, (column + (nodeWidth / 2), row +(nodeHeight/2)));

                // Adds the new node to a dictionary.
                if(!nodeMatrix.ContainsKey(newNode.center.Item2))
                {
                    nodeMatrix.Add(newNode.center.Item2, new Dictionary<float, Node>() { { newNode.center.Item1, newNode } });
                }
                else
                {
                    nodeMatrix[newNode.center.Item2].Add(newNode.center.Item1, newNode);
                }
            }
        }


        SetUpNeighbors();

        //PrintNumNeighbors();
        CheckPositions();

        Kmean.CreateClusters();
        Debug.Log("Number of clusters: " + Kmean.clusters.Count);
    }

    /// <summary>
    /// Sets up the neighbor relation for each node.
    /// </summary>
    public void SetUpNeighbors()
    {
        foreach(var row in nodeMatrix)
        {
            foreach(var columnKey in row.Value)
            {
                Node currentNode = nodeMatrix[row.Key][columnKey.Key];

                Debug.Log($"Node Centered at ({currentNode.center.Item1}, {currentNode.center.Item2})");

                // Checks to see if the current node has a top node (current node is not the top most node).
                float topRow = currentNode.center.Item2 + currentNode.nodeHeight;
                if(topRow < mapHeight)
                {
                    Node upperNeighbor = nodeMatrix[topRow][currentNode.center.Item1];
                    currentNode.AddNeighbor(upperNeighbor);

                    Debug.Log("Has upper row neighbor!");
                }

                // Checks to see if the current node has a bottom node (current node is not the bottom most node).
                float bottomRow = currentNode.center.Item2 - currentNode.nodeHeight;
                if(bottomRow > origin.Item2 && bottomRow < mapHeight)
                {
                    Node bottomNeighbor = nodeMatrix[bottomRow][currentNode.center.Item1];
                    currentNode.AddNeighbor(bottomNeighbor);

                    Debug.Log("Has bottom row neighbor!");
                }

                // Checks to see if the current node has a left node (current node is not the left most node).
                float leftColumn = currentNode.center.Item1 - currentNode.nodeWidth;
                if(leftColumn > origin.Item1 && leftColumn < mapWidth)
                {
                    Node leftNeighbor = nodeMatrix[currentNode.center.Item2][leftColumn];
                    currentNode.AddNeighbor(leftNeighbor);

                    Debug.Log("Has left column neighbor!");
                }

                // Checks to see if the current node has a right node (current node is not the right most node).
                float rightColumn = currentNode.center.Item1 + currentNode.nodeWidth;
                if(rightColumn < mapWidth)
                {
                    Node rightNeighbor = nodeMatrix[currentNode.center.Item2][rightColumn];
                    currentNode.AddNeighbor(rightNeighbor);

                    Debug.Log("Has right column neighbor!");
                }

                Debug.Log("\n");
            }
        }
    }

    /// <summary>
    /// Just to help us visualize how the grid system is laid out.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Creates the general grid.
        foreach(var nodeList in nodeMatrix.Values)
        {
            foreach(var node in nodeList.Values)
            {
                Gizmos.color = Color.green;

                // Draw a wireframe box at the specified center and with the specific size
                Gizmos.DrawWireCube(new Vector3(node.center.Item1,0,  node.center.Item2), new Vector3(node.nodeWidth, 0, node.nodeHeight));
            }
        }


        foreach(var cluster in Kmean.clusterNodes)
        {

            Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(new Vector3(cluster.center.Item1, 0, cluster.center.Item2), 10f);
            
            //Gizmos.DrawWireCube(new Vector3(cluster.center.Item1, 0, cluster.center.Item2), new Vector3(cluster.nodeWidth, 0, cluster.nodeHeight));
        }
    }

    #region Test Methods

    /// <summary>
    /// Tests to see that all methods have been properly assigned neighbors.
    /// </summary>
    private void PrintNumNeighbors()
    {
        foreach(var row in nodeMatrix.Values)
        {
            foreach(var node in row.Values)
            {
                Debug.Log($"Node has {node.neighorNodes.Count} neighbors.");
            }
        }
    }

    /// <summary>
    /// WORKS! Finds all points not within the terrain and generates enemies accordingly!
    /// TODO: We can now use create a new list of nodes not inside of the terrain and spawn enemies at random nodes.
    /// </summary>
    private void CheckPositions()
    {
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        foreach(var row in nodeMatrix.Values)
        {
            foreach(var node in row.Values)
            {
                
                Vector3 terrainPos = terrain.transform.InverseTransformPoint(new Vector3(node.center.Item1, 10, node.center.Item2));
                
                // If block below checks to see if the point is within the terrains X, Y, and Z.
                // In other words, it checks to see if the point is within bounds, NOT whether it collides with the terrain.
                // Typically the code below is acceptable for normal terrains, but for our case (our terrain contains hills)
                // We need to account for the hill heights.
                if (collider.bounds.Contains(terrainPos))
                {
                    // The region below checks to see if the specific point in the terrain is a float surface or a hill.
                    // If the point is a hill (i.e. "terrainHeight" is greater than 0), then we say the point is within the hill.
                    #region Check Hill Height

                    // X and Z are points within the range [0,1].
                    // We normalize our X and Z in order to get within that range (to remove the scaling factor from our X and Z).
                    float normalizedX = terrainPos.x / terrain.terrainData.size.x;
                    float normalizedZ = terrainPos.z / terrain.terrainData.size.z;

                    float terrainHeight = terrain.terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
                    if(terrainHeight > 0)
                    {
                        Debug.Log($"Node centered at ({node.center.Item1},{node.center.Item2}) is inside of the terrain!");
                    }
                    else
                    {
                        // If the node is not a terrain, we add the node to the "NonTerrainNodes" list and instantiate an NPC at the location.
                        NonTerrainNodes.Add(node);
                        Debug.Log("Not Contained!");
                        GameObject newEnemy = Instantiate(Enemy, new Vector3(node.center.Item1, 2.2f, node.center.Item2), Enemy.transform.rotation);
                        NPCs.Add(newEnemy);
                    }
                    #endregion
                }
            }
        }
    }

    #endregion
}
