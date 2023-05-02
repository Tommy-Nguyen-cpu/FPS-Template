using UnityEngine;

public class TerrainScript : MonoBehaviour
{
    int height = 256;
    int width = 256;
    int depth = 100;

    float offsetX = 100f;
    float offsetY = 100f;
    private void Update()
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateLandscape(terrain.terrainData);
    }

    private TerrainData GenerateLandscape(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, depth, height);
        terrainData.SetHeights(0, 0, GenerateHeightValues(height, width));
        return terrainData;
    }

    private float[,] GenerateHeightValues(int height, int width)
    {
        float[,] heights = new float[width, height];
        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                float perlinNoise = CalculateHeight(x, y);
                heights[x, y] = perlinNoise < .6 ? 0f : perlinNoise;
            }
        }

        return heights;
    }

    private float CalculateHeight(int x, int y)
    {
        float newX = (float)x / width * 20 + offsetX;
        float newY = (float)y / height * 20 + offsetY;
        return Mathf.PerlinNoise(newX, newY);
    }

    
}
