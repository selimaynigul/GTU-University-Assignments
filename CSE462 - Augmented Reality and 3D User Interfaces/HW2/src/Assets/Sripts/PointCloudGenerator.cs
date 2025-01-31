using System.IO;
using UnityEngine;

public class PointCloudGenerator : MonoBehaviour
{

    public int numOfPoints = 10;

    public void Generate()
    {
        GeneratePointCloud("Generated_1.txt", numOfPoints, new Vector3(0, 0, 0), new Vector3(20, 20, 20));
        GeneratePointCloud("Generated_2.txt", numOfPoints, new Vector3(30, 21, 0), new Vector3(50, 40, 20));
    }

    private void GeneratePointCloud(string fileName, int numPoints, Vector3 minRange, Vector3 maxRange)
    {
        using (StreamWriter writer = new StreamWriter(Path.Combine(Application.dataPath, "PointClouds", fileName)))
        {
            writer.WriteLine(numPoints);
            for (int i = 0; i < numPoints; i++)
            {
                float x = Random.Range(minRange.x, maxRange.x);
                float y = Random.Range(minRange.y, maxRange.y);
                float z = Random.Range(minRange.z, maxRange.z);
                writer.WriteLine($"{x:F2} {y:F2} {z:F2}");
            }
        }
    }
}
