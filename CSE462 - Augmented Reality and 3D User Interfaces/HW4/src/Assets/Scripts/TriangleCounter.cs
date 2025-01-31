using UnityEngine;

public class TriangleCounter : MonoBehaviour
{
    void Start()
    {
        int totalTriangles = 0;

        MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.mesh != null)
            {
                totalTriangles += meshFilter.mesh.triangles.Length / 3; 
            }
        }

        Debug.Log($"Total number of triangles in the scene: {totalTriangles}");
    }
}
