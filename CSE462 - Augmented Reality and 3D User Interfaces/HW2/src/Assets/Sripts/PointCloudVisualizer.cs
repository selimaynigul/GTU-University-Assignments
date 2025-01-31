using System.Collections.Generic;
using UnityEngine;

public class PointCloudVisualizer : MonoBehaviour
{
    public GameObject pointPrefab;
    public Color color1 = Color.red;
    public Color color2 = Color.blue;
    public Color alignedColor = Color.green;
    public Color lineColor = Color.yellow;
    public float pointSize = 1.0f;
   

    public Vector3[] PointSetP { get; private set; }
    public Vector3[] PointSetQ { get; private set; }

    private List<GameObject> pointObjectsP = new List<GameObject>();
    private List<GameObject> pointObjectsQ = new List<GameObject>();
    private List<GameObject> alignedPointObjects = new List<GameObject>();
    private List<GameObject> movementLines = new List<GameObject>();

    public void InitializePointClouds(Vector3[] pointsP, Vector3[] pointsQ)
    {
        ClearAll();

        PointSetP = pointsP;
        PointSetQ = pointsQ;

        VisualizePoints(pointsP, color1, pointObjectsP, false);
        VisualizePoints(pointsQ, color2, pointObjectsQ, false);
    }

    public void AnimateAlignedPoints(Vector3[] alignedPoints, float duration)
    {
        if (alignedPoints == null || alignedPoints.Length == 0)
        {
            Debug.LogError("Aligned points must not be null or empty.");
            return;
        }

        ClearPoints(alignedPointObjects); 

        foreach (var startPosition in PointSetQ)
        {
            GameObject pointObj = Instantiate(pointPrefab, startPosition, Quaternion.identity);
            pointObj.transform.localScale = Vector3.one * pointSize * 1.1f; // Slightly larger for visibility

            var renderer = pointObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = alignedColor;
            }

            alignedPointObjects.Add(pointObj);
        }

        // Start animation to move these green points to aligned positions
        StartCoroutine(AnimatePointsToAlignedCoroutine(alignedPointObjects, alignedPoints, duration));
    }

    private System.Collections.IEnumerator AnimatePointsToAlignedCoroutine(List<GameObject> pointObjects, Vector3[] targetPositions, float duration)
    {
        if (pointObjects.Count != targetPositions.Length)
        {
            Debug.LogError("Aligned point objects and target positions count mismatch.");
            yield break;
        }

        Vector3[] startPositions = new Vector3[pointObjects.Count];
        for (int i = 0; i < pointObjects.Count; i++)
        {
            startPositions[i] = pointObjects[i].transform.position;
        }

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;

            for (int i = 0; i < pointObjects.Count; i++)
            {
                pointObjects[i].transform.position = Vector3.Lerp(startPositions[i], targetPositions[i], progress);
            }

            yield return null; 
        }

       
        for (int i = 0; i < pointObjects.Count; i++)
        {
            pointObjects[i].transform.position = targetPositions[i];
        }
    }

    public void ShowMovementLines(Vector3[] originalPoints, Vector3[] alignedPoints)
    {
        if (originalPoints == null || alignedPoints == null || originalPoints.Length != alignedPoints.Length)
        {
            Debug.LogError("Point sets must not be null and must have the same number of points.");
            return;
        }

        ClearLines();

        for (int i = 0; i < originalPoints.Length; i++)
        {
            GameObject line = new GameObject($"Line_{i}");
            LineRenderer lineRenderer = line.AddComponent<LineRenderer>();

            lineRenderer.startWidth = 0.3f;
            lineRenderer.endWidth = 0.3f;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, originalPoints[i]); 
            lineRenderer.SetPosition(1, alignedPoints[i]); 

            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;

            movementLines.Add(line);
        }
    }

    public void ClearAll()
    {
        ClearPoints(pointObjectsP);
        ClearPoints(pointObjectsQ);
        ClearPoints(alignedPointObjects);
        ClearLines();
    }


    public Vector3[] LoadPointCloudFromFile(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return null;
        }

        try
        {
            string[] lines = System.IO.File.ReadAllLines(filePath);
            int numPoints = int.Parse(lines[0]);
            Vector3[] points = new Vector3[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                string[] coords = lines[i + 1].Split(' ');
                points[i] = new Vector3(float.Parse(coords[0]), float.Parse(coords[1]), float.Parse(coords[2]));
            }

            return points;
        }
        catch
        {
            Debug.LogError("Failed to read point cloud file.");
            return null;
        }
    }


    private void VisualizePoints(Vector3[] points, Color color, List<GameObject> pointObjects, bool isAlignedPoint)
    {
        foreach (var point in points)
        {
            GameObject pointObj = Instantiate(pointPrefab, point, Quaternion.identity);
            pointObj.transform.localScale = Vector3.one * pointSize;

            var renderer = pointObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            pointObjects.Add(pointObj);
        }
    }

    private void ClearPoints(List<GameObject> points)
    {
        foreach (var point in points)
        {
            Destroy(point);
        }
        points.Clear();
    }

    public void ClearLines()
    {
        foreach (var line in movementLines)
        {
            Destroy(line);
        }
        movementLines.Clear();
    }
}
