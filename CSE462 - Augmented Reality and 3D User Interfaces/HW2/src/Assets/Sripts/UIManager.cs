using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI topLeftText; 
    public TextMeshProUGUI detailsText;
    public TextMeshProUGUI variablesText;
    public PointCloudVisualizer pointCloudVisualizer;
    public PointCloudRegistration pointCloudRegistration;
    public TMP_Dropdown fileSelectorDropdown;
    public PointCloudGenerator pointCloudGenerator;


    private Matrix4x4 transformationMatrix;
    private Quaternion rotationMatrix;
    private float errorRate;
    private Vector3[] alignedPoints; 
    private bool areLinesVisible = false; 


    public void OnButtonLoadClick()
    {
        // Get the selected file from the dropdown
        int selectedFileIndex = fileSelectorDropdown.value;
        string selectedFileName = fileSelectorDropdown.options[selectedFileIndex].text;

        if (selectedFileName == "Generated")
        {
            pointCloudGenerator.Generate();
        }

        topLeftText.text = $"Loading {selectedFileName}...";

        string filePathP = System.IO.Path.Combine(Application.dataPath, "PointClouds", $"{selectedFileName}_1.txt");
        string filePathQ = System.IO.Path.Combine(Application.dataPath, "PointClouds", $"{selectedFileName}_2.txt");

        Vector3[] pointsP = pointCloudVisualizer.LoadPointCloudFromFile(filePathP);
        Vector3[] pointsQ = pointCloudVisualizer.LoadPointCloudFromFile(filePathQ);

        variablesText.text = $"Number of points P: {pointsP.Length}\n" +
            $"Number of points Q: {pointsQ.Length}\n" +
            $"Max RANSAC iterations: {pointCloudRegistration.maxIterations}\n" +
            $"Threshold: {pointCloudRegistration.threshold}";
        

        if (pointsP == null || pointsQ == null)
        {
            topLeftText.text = $"Error: Failed to load {selectedFileName}.";
            return;
        }

        areLinesVisible = false;
        detailsText.text = "";
        alignedPoints = null;
        topLeftText.text = "Visualization Cleared!";

        pointCloudVisualizer.InitializePointClouds(pointsP, pointsQ);
        topLeftText.text = $"{selectedFileName} Loaded!";
    }


public void OnButtonRegisterClick()
    {
        if (pointCloudVisualizer.PointSetP == null || pointCloudVisualizer.PointSetQ == null)
        {
            topLeftText.text = "Error: Point clouds not loaded.";
            return;
        }

        pointCloudVisualizer.ClearLines();  
        areLinesVisible = false;

        topLeftText.text = "Registering Point Clouds...";
        alignedPoints = pointCloudRegistration.RegisterPointClouds(
            pointCloudVisualizer.PointSetP,
            pointCloudVisualizer.PointSetQ,
            out transformationMatrix,
            out rotationMatrix,
            out errorRate
        );

        if (alignedPoints != null)
        {
            // Animate the points to their aligned positions
            pointCloudVisualizer.AnimateAlignedPoints(alignedPoints, 1.2f); 
            UpdateDetailsText();
            topLeftText.text = $"Registration Complete! Inliers: {pointCloudRegistration.InlierCount}";
        }
        else
        {
            topLeftText.text = "Registration Failed!";
        }
    }


    public void OnButtonShowLinesClick()
    {
        if (pointCloudVisualizer.PointSetP == null || pointCloudVisualizer.PointSetQ == null || alignedPoints == null)
        {
            topLeftText.text = "Error: Point clouds not loaded or registration not performed.";
            return;
        }

        if (!areLinesVisible)
        {
            pointCloudVisualizer.ShowMovementLines(pointCloudVisualizer.PointSetQ, alignedPoints);
        }
        else
        {
            pointCloudVisualizer.ClearLines();
        }

        areLinesVisible = !areLinesVisible; 
    }

    public void OnButtonClearClick()
    {
        topLeftText.text = "Clearing Visualizations...";
        pointCloudVisualizer.ClearAll();
        areLinesVisible=false;
        detailsText.text = "";
        alignedPoints = null; 
        topLeftText.text = "Visualization Cleared!";
    }

    private void UpdateDetailsText()
    {
        detailsText.text = "Transformation Matrix:\n" +
                           $"{MatrixToString(transformationMatrix)}\n\n" +
                           "Rotation Matrix:\n" +
                           $"{QuaternionToMatrixString(rotationMatrix)}\n\n" +
                           $"Error Rate: {errorRate:F2}\n\n" +
                           $"Inliers: {pointCloudRegistration.InlierCount}";
    }

    private string MatrixToString(Matrix4x4 matrix)
    {
        return $"{matrix.m00:F2} {matrix.m01:F2} {matrix.m02:F2} {matrix.m03:F2}\n" +
               $"{matrix.m10:F2} {matrix.m11:F2} {matrix.m12:F2} {matrix.m13:F2}\n" +
               $"{matrix.m20:F2} {matrix.m21:F2} {matrix.m22:F2} {matrix.m23:F2}\n" +
               $"{matrix.m30:F2} {matrix.m31:F2} {matrix.m32:F2} {matrix.m33:F2}";
    }

    private string QuaternionToMatrixString(Quaternion rotation)
    {
        Matrix4x4 matrix = Matrix4x4.Rotate(rotation);
        return $"{matrix.m00:F2} {matrix.m01:F2} {matrix.m02:F2}\n" +
               $"{matrix.m10:F2} {matrix.m11:F2} {matrix.m12:F2}\n" +
               $"{matrix.m20:F2} {matrix.m21:F2} {matrix.m22:F2}";
    }
}
