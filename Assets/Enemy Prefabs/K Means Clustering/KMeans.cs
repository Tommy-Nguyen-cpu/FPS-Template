using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KMeans
{
    /// <summary>
    /// "KCentroids" represent the number of clusters.
    /// </summary>
    private int KCentroids = 10;

    /// <summary>
    /// "clusters" dictionary is the dictionary that stores the raw clusters (i.e. the cluster x and z means and the list of game objects in each cluster).
    /// The key is a tuple, with item1 being the x mean and item2 being the z mean (in other words the x and z coordinates of the cluster).
    /// The size of "clusters" should be "KCentroids".
    /// </summary>
    public Dictionary<(float, float), List<GameObject>> clusters { get; private set; } = new Dictionary<(float, float), List<GameObject>>();

    /// <summary>
    /// A list of clusters, where the clusters have been converted to nodes.
    /// Used by "HighLevelAStarAlgorithm" to run the high level A* algorithm betwen clusters.
    /// </summary>
    public List<Node> clusterNodes { get; private set; } = new List<Node>();

    /// <summary>
    /// Creates "KCentroids" clusters that NPCs will be placed inside of.
    /// </summary>
    public void CreateClusters()
    {
        Debug.Log("Created Cluster!");
        // Clears the cluster dictionary and cluster node list.
        clusters = new Dictionary<(float, float), List<GameObject>>();
        clusterNodes = new List<Node>();

        // Initializes each cluster with 1 data point.
        int startingPoint = InitiateClusters();

        Debug.Log($"There are {clusters.Count} clusters.");

        PopulateClusters(startingPoint);

        UpdateCentroids();

        SetUpClusterNodes();
        Debug.Log("DONE CALCULATING K-MEAN!");
        
        VerifyNodes();
    }

    /// <summary>
    /// Initializes "Kcentroids" clusters with 1 data point each.
    /// </summary>
    /// <returns></returns>
    private int InitiateClusters()
    {
        int startingPoint = 0;
        for (int i = 0; i < KCentroids; i++)
        {
            GameObject npc = GridSystem.NPCs[i];

            // Because there is only 1 data point in the cluster, the X and Z means are just the data points' X and Z value.
            float xMean = npc.transform.position.x;
            float zMean = npc.transform.position.z;
            clusters.Add((xMean, zMean), new List<GameObject>() { npc });

            startingPoint++;
        }
        return startingPoint;
    }

    /// <summary>
    /// Populates each cluster with nodes/data points closest to each cluster.
    /// </summary>
    /// <param name="startingPoint"></param>
    private void PopulateClusters(int startingPoint)
    {
        // Iterate through the list of NPCs starting after the initial data points assigned to each cluster.
        for (int i = startingPoint; i < GridSystem.NPCs.Count; i++)
        {
            (float, float) nearestCluster = NearestCluster(GridSystem.NPCs[i]);
            clusters[nearestCluster].Add(GridSystem.NPCs[i]);
        }
    }

    /// <summary>
    /// Updates the mean/centroid of every cluster.
    /// Mean/Centroid is calculated by using the coordinates of each data point (i.e. there is a x mean and a z mean).
    /// </summary>
    private void UpdateCentroids()
    {
        // Retrieves the mean of all of the clusters.
        List<(float, float)> meansToUpdate = new List<(float, float)>();
        foreach (var mean in clusters.Keys)
        {
            meansToUpdate.Add(mean);
        }

        // Recomputes the centroids (means) of each cluster.
        foreach (var mean in meansToUpdate)
        {
            float newXMean = 0;
            float newZMean = 0;

            List<GameObject> dataPoints = clusters[mean];
            foreach (var dataPoint in dataPoints)
            {
                newXMean += dataPoint.transform.position.x;
                newZMean += dataPoint.transform.position.z;
            }

            newXMean /= dataPoints.Count;
            newZMean /= dataPoints.Count;

            clusters.Remove(mean);
            clusters.Add((newXMean, newZMean), dataPoints);
        }
    }

    /// <summary>
    /// Simple algorithm designed to find the cluster closest to the data point.
    /// </summary>
    /// <param name="dataPoint"></param>
    /// <returns></returns>
    public (float,float) NearestCluster(GameObject dataPoint)
    {
        // Begrudgingly using LINQ.
        (float, float) minCluster = clusters.ElementAt(0).Key;
        float minDistance = Distance(dataPoint.transform.position.x, 
                                    dataPoint.transform.position.z, 
                                    minCluster.Item1, 
                                    minCluster.Item2);

        // Finds the closest cluster to the current data point.
        for(int i = 1; i < clusters.Count; i++)
        {
            (float, float) newClusterPoint = clusters.ElementAt(i).Key;
            float newDistance = Distance(dataPoint.transform.position.x, 
                                        dataPoint.transform.position.z,
                                        newClusterPoint.Item1, 
                                        newClusterPoint.Item2);

            if(newDistance < minDistance)
            {
                minDistance = newDistance;
                minCluster = newClusterPoint;
            }
        }

        return minCluster;
    }

    /// <summary>
    /// Sets up the neighbor for every cluster.
    /// </summary>
    /// <param name="currentCluster"></param>
    /// <param name="desirableDistance"></param>
    /// <returns></returns>
    private List<Node> ClusterNeighbors(Node currentCluster, float desirableDistance)
    {
        List<Node> neighborNodes = new List<Node>();
        foreach(var cluster in clusterNodes)
        {
            if (cluster == currentCluster)
                continue;

            // Calculates the Euclidean distance between the clusters.
            float distanceBetweenCluster = Distance(currentCluster.center.Item1,
                                            currentCluster.center.Item2,
                                            cluster.center.Item1,
                                            cluster.center.Item2);

            // If the cluster is less than or equal to the desired distance, then we add it to the neighbors.
            if(distanceBetweenCluster <= desirableDistance)
            {
                neighborNodes.Add(cluster);
            }
        }

        return neighborNodes;
    }

    /// <summary>
    /// Calculates the magnitude of vector W.
    /// Vector W = end point - start point
    /// Magnitude = ||Vector W|| or ( (x pos)^2 + (z pos)^2)^(1/2) )
    /// </summary>
    /// <param name="dataPointX"></param>
    /// <param name="datapointZ"></param>
    /// <param name="clusterX"></param>
    /// <param name="clusterZ"></param>
    /// <returns></returns>
    public static float Distance(float dataPointX, float datapointZ, float clusterX, float clusterZ)
    {
        float diffX = clusterX - dataPointX;
        float diffZ = clusterZ - datapointZ;

        float sum = diffX * diffX + diffZ * diffZ;

        return Mathf.Sqrt(sum);
    }

    /// <summary>
    /// Convert clusters into nodes and adds to the "clusterNodes" list. List will be used by the high level A* algorithm.
    /// </summary>
    private void SetUpClusterNodes()
    {
        foreach(var clusterKey in clusters.Keys)
        {
            (float width, float height) = FindClusterProportions(clusterKey);

            Node newClusterNode = new Node(width, height, (clusterKey.Item1, clusterKey.Item2));
            clusterNodes.Add(newClusterNode);
        }

        // After we create the clusters, we want to set up the cluster neighbors.
        SetUpClusterNeighbors();
    }

    /// <summary>
    /// Sets up neighbors for each cluster.
    /// </summary>
    private void SetUpClusterNeighbors()
    {
        // The distance we want each adjacent nodes to be different by.
        float desiredDistanceBetweenClusters = 1000f;
        foreach(var cluster in clusterNodes)
        {
            List<Node> clusterNeighbors = ClusterNeighbors(cluster, desiredDistanceBetweenClusters);
            foreach(var node in clusterNeighbors)
            {
                cluster.AddNeighbor(node);
            }
        }
    }

    /// <summary>
    /// Find the width and height of the cluster. Called by "SetUpClusterNodes" method.
    /// </summary>
    private (float, float) FindClusterProportions((float,float) clusterKey)
    {

            float maxX = clusters[clusterKey][0].transform.position.x;
            float minX = clusters[clusterKey][0].transform.position.x;
            float maxZ = clusters[clusterKey][0].transform.position.z;
            float minZ = clusters[clusterKey][0].transform.position.z;

            for (int i = 1; i < clusters[clusterKey].Count; i++)
            {
                float newX = clusters[clusterKey][i].transform.position.x;
                float newZ = clusters[clusterKey][i].transform.position.z;

                if (maxX < newX)
                    maxX = newX;
                if (minX > newX)
                    minX = newX;
                if (maxZ < newZ)
                    maxZ = newZ;
                if (minZ > newZ)
                    minZ = newZ;
            }

            // TODO: The padding shouldn't be an issue anymore (not sure why it was happening). I'll leave this comment here in case something happens in the future.
            float width = maxX - minX;
            float height = maxZ - minZ;

        return (width, height);
    }

    /// <summary>
    /// Test: Used to see if the total number of nodes is correct.
    /// </summary>
    private void VerifyNodes()
    {
        int totalNodes = 0;
        foreach(var node in clusters)
        {
            totalNodes += node.Value.Count;
        }

        Debug.Log($"KMeans NPCs: {totalNodes}, Actual Total: {GridSystem.NonTerrainNodes.Count}");
    }
}
