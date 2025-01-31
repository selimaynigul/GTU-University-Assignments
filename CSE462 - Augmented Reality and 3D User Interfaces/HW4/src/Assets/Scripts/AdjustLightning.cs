using UnityEngine;
using UnityEngine.UI;

public class AdjustLighting : MonoBehaviour
{
    public Light targetLight; 
    public Slider intensitySlider;

    void Start()
    {
        intensitySlider.onValueChanged.AddListener(UpdateIntensity);
    }

    void UpdateIntensity(float value)
    {
        if (targetLight != null)
            targetLight.intensity = value;
    }
}
