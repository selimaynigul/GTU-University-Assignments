using UnityEngine;
using UnityEngine.UI; 
using System.Collections;
using TMPro; 

public class PinholeCamera : MonoBehaviour
{
    public float fieldOfView = 60f; // Field of view in degrees
    public int resolutionX = 640; // Image width
    public int resolutionY = 480; // Image height
    public Transform blackHole; // Reference to the black hole
    public float blackHoleInfluence = 1.0f; // How strongly the black hole bends rays
    public float blackHoleAbsorption = 0.5f; // Light absorption near the black hole
    public Button renderButton; // Reference to the UI Button
    public Toggle enableBlackHoleToggle; // Reference to the UI Checkbox
    public TextMeshProUGUI text; 
    private Vector3 cameraPosition;
    private Vector3 cameraDirection;
    private Texture2D outputTexture;
    private bool enableBlackHole = true; // Default to enabled
    private string temporaryText = "Image Saved!"; // The temporary message
    private float displayDuration = 3f; // Time in seconds to display the temporary text
    private string originalText; // To store the original text
    private KeyCode triggerKey = KeyCode.F; // Key to bind (default is 'F')

    void Start()
    {
        cameraPosition = transform.position;
        cameraDirection = transform.forward;

        // Initialize a 640x480 texture
        outputTexture = new Texture2D(resolutionX, resolutionY, TextureFormat.RGB24, false);

        if (text != null)
        {
            originalText = text.text;
        }

        if (blackHole == null)
        {
            Debug.LogError("BlackHole Transform is not assigned! Please assign it in the Inspector.");
        }

        if (renderButton != null)
        {
            renderButton.onClick.AddListener(RenderScene); 
        }
        else
        {
            Debug.LogError("Render Button is not assigned! Please assign it in the Inspector.");
        }

        if (enableBlackHoleToggle != null)
        {
            enableBlackHoleToggle.onValueChanged.AddListener(delegate { ToggleBlackHole(enableBlackHoleToggle); });
            enableBlackHole = enableBlackHoleToggle.isOn; 
        }
        else
        {
            Debug.LogError("EnableBlackHoleToggle is not assigned! Please assign it in the Inspector.");
        }
    }

    void Update()
    {
        cameraPosition = transform.position;
        cameraDirection = transform.forward;

        // Check if the 'F' key is pressed
        if (Input.GetKeyDown(triggerKey))
        {
            renderButton.onClick.Invoke();
        }
    }

    void ToggleBlackHole(Toggle toggle)
    {
        enableBlackHole = toggle.isOn;
        Debug.Log("Black Hole Effect: " + (enableBlackHole ? "Enabled" : "Disabled"));
    }

    void RenderScene()
    {
        float aspectRatio = (float)resolutionX / resolutionY;
        float halfFovRad = Mathf.Deg2Rad * (fieldOfView / 2f);
        float cameraPlaneHeight = Mathf.Tan(halfFovRad) * 2f;
        float cameraPlaneWidth = cameraPlaneHeight * aspectRatio;

        Vector3 right = transform.right * (cameraPlaneWidth / 2f);
        Vector3 up = transform.up * (cameraPlaneHeight / 2f);
        Vector3 lowerLeft = cameraDirection - right - up;

        for (int y = 0; y < resolutionY; y++)
        {
            for (int x = 0; x < resolutionX; x++)
            {
                float u = (float)x / resolutionX;
                float v = (float)y / resolutionY;

                Vector3 rayDirection = lowerLeft + right * u * 2f + up * v * 2f;
                rayDirection.Normalize();

                CastRay(cameraPosition, rayDirection, x, y);
            }
        }

        outputTexture.Apply();
        SaveImage();
        StartCoroutine(TemporaryTextCoroutine());
    }

    IEnumerator TemporaryTextCoroutine()
    {
        text.text = temporaryText;
        yield return new WaitForSeconds(displayDuration);
        text.text = originalText;
    }

    void CastRay(Vector3 origin, Vector3 direction, int x, int y)
    {
        Color pixelColor = Color.black;

        // Calculate proximity to the black hole
        Vector3 toBlackHole = blackHole.position - origin;
        float distanceToBlackHole = Vector3.Cross(direction, toBlackHole).magnitude; 

        // Apply proper degree-2 bending if near the black hole
        if (enableBlackHole) 
        {
            float proximityFactor = Mathf.Clamp01(1 / distanceToBlackHole); // Linear falloff based on proximity
            Vector3 deflection = toBlackHole - Vector3.Project(toBlackHole, direction); // Orthogonal component
            deflection = deflection.normalized * Mathf.Pow(proximityFactor, 2); // Degree-2 curve effect
            direction += deflection; 
            direction.Normalize();
        }

        // Check if the ray hits any object
        if (Physics.Raycast(origin, direction, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("BlackHole"))
            {
                // Rays hitting the black hole
                pixelColor = Color.black;

            }
            else
            {
                // Get the object's material color
                Renderer renderer = hit.collider.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    Texture2D mainTexture = renderer.material.mainTexture as Texture2D;
                    Color baseColor = renderer.material.color;

                    // Sample the texture color if it exists
                    if (mainTexture != null)
                    {
                        Vector2 uv = hit.textureCoord; // Get UV coordinates
                        baseColor *= mainTexture.GetPixelBilinear(uv.x, uv.y);
                    }

                    // Simulate lighting
                    float lightIntensity = CalculateLighting(hit.point, hit.normal);
                    pixelColor = baseColor * Mathf.Clamp01(lightIntensity);
                }
            }
        }
        else
        {
            // Background color for rays that hit nothing
            pixelColor = Color.black;
        }

        // Set the pixel color in the texture
        outputTexture.SetPixel(x, y, pixelColor);
    }

    float CalculateLighting(Vector3 hitPoint, Vector3 normal)
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        float totalIntensity = 0f;

        foreach (Light light in lights)
        {
            Vector3 toLight = (light.transform.position - hitPoint).normalized;
            float distance = Vector3.Distance(light.transform.position, hitPoint);
            float attenuation = 1.0f / (distance * distance); // Inverse-square law
            float intensity = Mathf.Max(0, Vector3.Dot(normal, toLight)) * light.intensity * attenuation;
            totalIntensity += intensity;
        }

        return totalIntensity;
    }


    void SaveImage()
    {
        // Encode the texture as a PNG
        byte[] bytes = outputTexture.EncodeToPNG();
        string filePath = Application.dataPath + "/SavedImage.png";
        System.IO.File.WriteAllBytes(filePath, bytes);
        Debug.Log("Image saved to: " + filePath);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(cameraPosition, 0.1f);
    }
}
