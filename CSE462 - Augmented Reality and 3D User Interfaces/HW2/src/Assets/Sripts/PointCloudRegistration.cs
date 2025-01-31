using System;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra; // For matrix operations

public class PointCloudRegistration : MonoBehaviour
{
    public int InlierCount { get; private set; } // the best inlier count for canvas display
    public int maxIterations = 1000;
    public float threshold = 1.0f;

    // Register point clouds using rigid transformation and RANSAC
    public Vector3[] RegisterPointClouds(Vector3[] pointSetP, Vector3[] pointSetQ, out Matrix4x4 transformationMatrix, out Quaternion rotationMatrix, out float errorRate)
    {
        // Validate inputs
        if (pointSetP == null || pointSetQ == null || pointSetP.Length < 3 || pointSetQ.Length < 3)
        {
            Debug.LogError("Input point clouds are invalid or have fewer than 3 points.");
            transformationMatrix = Matrix4x4.identity;
            rotationMatrix = Quaternion.identity;
            errorRate = float.MaxValue;
            return null;
        }

        // Use RANSAC to find the best transformation
        (Matrix<float> rotation, Vector<float> translation) = RANSACAlignment(pointSetP, pointSetQ);

        // Apply the transformation to pointSetQ
        Vector3[] alignedPoints = ApplyRigidTransformation(pointSetQ, rotation, translation);

        // Calculate transformation matrix and error rate
        transformationMatrix = BuildTransformationMatrix(rotation, translation);
        rotationMatrix = QuaternionFromRotationMatrix(transformationMatrix);
        errorRate = CalculateErrorRate(pointSetP, alignedPoints);

        return alignedPoints;
    }

    // Function to apply a rigid transformation to a point cloud
    private Vector3[] ApplyRigidTransformation(Vector3[] pointSet, Matrix<float> rotation, Vector<float> translation)
    {
        Vector3[] transformedSet = new Vector3[pointSet.Length];

        for (int i = 0; i < pointSet.Length; i++)
        {
            Vector<float> point = Vector<float>.Build.DenseOfArray(new float[] { pointSet[i].x, pointSet[i].y, pointSet[i].z });
            Vector<float> transformedPoint = rotation * point + translation;

            transformedSet[i] = new Vector3(transformedPoint[0], transformedPoint[1], transformedPoint[2]);
        }

        return transformedSet;
    }

    // Function to implement RANSAC with three-point alignment
    private (Matrix<float> rotation, Vector<float> translation) RANSACAlignment(Vector3[] pointSetP, Vector3[] pointSetQ)
    {
      
        int bestInlierCount = 0;

        Matrix<float> bestRotation = Matrix<float>.Build.DenseIdentity(3, 3);
        Vector<float> bestTranslation = Vector<float>.Build.Dense(3);

        System.Random random = new System.Random();

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            // Randomly select 3 points from each set
            int[] indicesP = RandomIndices(pointSetP.Length, 3, random);
            int[] indicesQ = RandomIndices(pointSetQ.Length, 3, random);

            Vector3[] subsetP = { pointSetP[indicesP[0]], pointSetP[indicesP[1]], pointSetP[indicesP[2]] };
            Vector3[] subsetQ = { pointSetQ[indicesQ[0]], pointSetQ[indicesQ[1]], pointSetQ[indicesQ[2]] };

            // Estimate transformation using the subsets
            (Matrix<float> rotation, Vector<float> translation) = EstimateTransformation(subsetP, subsetQ);
            

            // Count inliers
            int inlierCount = CountInliers(pointSetP, pointSetQ, rotation, translation, threshold);

            // Update best transformation if this one is better
            if (inlierCount > bestInlierCount)
            {
                bestInlierCount = inlierCount;
                bestRotation = rotation;
                bestTranslation = translation;
            }
        }

        InlierCount = bestInlierCount; 
        return (bestRotation, bestTranslation);
    }

    // Function to estimate transformation from three pairs of points
    private (Matrix<float> rotation, Vector<float> translation) EstimateTransformation(Vector3[] subsetP, Vector3[] subsetQ)
    {
        // Validate subset size
        if (subsetP.Length < 3 || subsetQ.Length < 3)
        {
            Debug.LogError("Subsets must contain at least 3 points.");
            throw new InvalidOperationException("Insufficient points for SVD.");
        }

        // Compute centroids
        Vector3 centroidP = ComputeCentroid(subsetP);
        Vector3 centroidQ = ComputeCentroid(subsetQ);

        // Subtract centroids
        Matrix<float> centeredP = BuildMatrix(subsetP, centroidP);
        Matrix<float> centeredQ = BuildMatrix(subsetQ, centroidQ);


        if (IsDegenerate(subsetP) || IsDegenerate(subsetQ))
        {
            Debug.LogError("Degenerate subset detected. Skipping this iteration.");
            throw new InvalidOperationException("Degenerate data for SVD.");
        }

        // Compute cross-covariance matrix
        Matrix<float> covariance = centeredP.Transpose() * centeredQ;

        // Compute SVD for rotation
        var svd = covariance.Svd();
        Matrix<float> rotation = svd.U * svd.VT;

        // Ensure proper rotation
        if (rotation.Determinant() < 0)
        {
            Matrix<float> correctedU = svd.U.Clone();
            correctedU.SetColumn(2, svd.U.Column(2) * -1);
            rotation = correctedU * svd.VT;
        }

        // Compute translation
        Vector<float> translation = Vector<float>.Build.DenseOfArray(new float[]
        {
            centroidP.x, centroidP.y, centroidP.z
        }) - rotation * Vector<float>.Build.DenseOfArray(new float[]
        {
            centroidQ.x, centroidQ.y, centroidQ.z
        });

        return (rotation, translation);
    }

    private bool IsDegenerate(Vector3[] subset)
    {
        if (subset.Length < 3)
            return true;

        // Calculate pairwise distances between points in the subset
        float distance1 = Vector3.Distance(subset[0], subset[1]);
        float distance2 = Vector3.Distance(subset[0], subset[2]);
        float distance3 = Vector3.Distance(subset[1], subset[2]);

        // Ensure distances are not too small (indicating near-identical points)
        float minAllowedDistance = 0.01f; // Adjust based on your data scale
        if (distance1 < minAllowedDistance || distance2 < minAllowedDistance || distance3 < minAllowedDistance)
            return true;

        return false;
    }


    // Function to compute the centroid of a set of points
    private Vector3 ComputeCentroid(Vector3[] points)
    {
        Vector3 centroid = Vector3.zero;

        foreach (Vector3 point in points)
        {
            centroid += point;
        }

        return centroid / points.Length;
    }

    // Function to build a matrix of centered points
    private Matrix<float> BuildMatrix(Vector3[] points, Vector3 centroid)
    {
        Matrix<float> matrix = Matrix<float>.Build.Dense(points.Length, 3);

        for (int i = 0; i < points.Length; i++)
        {
            matrix[i, 0] = points[i].x - centroid.x;
            matrix[i, 1] = points[i].y - centroid.y;
            matrix[i, 2] = points[i].z - centroid.z;
        }

        return matrix;
    }

    // Function to count inliers for a given transformation
    private int CountInliers(Vector3[] pointSetP, Vector3[] pointSetQ, Matrix<float> rotation, Vector<float> translation, float threshold)
    {
        int inlierCount = 0;

        for (int i = 0; i < pointSetQ.Length; i++)
        {
            Vector<float> transformedPoint = rotation * Vector<float>.Build.DenseOfArray(new float[] { pointSetQ[i].x, pointSetQ[i].y, pointSetQ[i].z }) + translation;
            Vector3 transformedVec = new Vector3(transformedPoint[0], transformedPoint[1], transformedPoint[2]);

            // Find nearest neighbor in pointSetP
            float minDistance = float.MaxValue;
            foreach (var pointP in pointSetP)
            {
                float distance = Vector3.Distance(transformedVec, pointP);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            if (minDistance < threshold)
            {
                inlierCount++;
            }
        }

        return inlierCount;
    }

    // Function to generate random indices
    private int[] RandomIndices(int max, int count, System.Random random)
    {
        HashSet<int> indices = new HashSet<int>();

        while (indices.Count < count)
        {
            indices.Add(random.Next(max));
        }

        return new List<int>(indices).ToArray();
    }

    // Function to build a transformation matrix from rotation and translation
    private Matrix4x4 BuildTransformationMatrix(Matrix<float> rotation, Vector<float> translation)
    {
        Matrix4x4 matrix = Matrix4x4.identity;

        matrix.m00 = rotation[0, 0];
        matrix.m01 = rotation[0, 1];
        matrix.m02 = rotation[0, 2];
        matrix.m10 = rotation[1, 0];
        matrix.m11 = rotation[1, 1];
        matrix.m12 = rotation[1, 2];
        matrix.m20 = rotation[2, 0];
        matrix.m21 = rotation[2, 1];
        matrix.m22 = rotation[2, 2];

        matrix.m03 = translation[0];
        matrix.m13 = translation[1];
        matrix.m23 = translation[2];

        return matrix;
    }

    // Function to convert a transformation matrix to a quaternion
    private Quaternion QuaternionFromRotationMatrix(Matrix4x4 matrix)
    {
        return Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
    }

    // Function to calculate the error rate between two point sets
    private float CalculateErrorRate(Vector3[] pointSetP, Vector3[] alignedPoints)
    {
        float totalError = 0;

        for (int i = 0; i < alignedPoints.Length; i++)
        {
            float minDistance = float.MaxValue;
            foreach (var pointP in pointSetP)
            {
                float distance = Vector3.Distance(alignedPoints[i], pointP);
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            totalError += minDistance;
        }

        return totalError / alignedPoints.Length;
    }
}
