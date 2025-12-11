using UnityEngine;

public class RotateUIController : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] Transform target;        // set this when you place/select an item
    [SerializeField] Transform planeUpSource; // set to PlaneSelector.Proxy (or leave null to use world up)

    [Header("Tuning")]
    [SerializeField] float stepDegrees = 10f; // button step
    [SerializeField] float sliderMin = -180f; // slider range
    [SerializeField] float sliderMax =  180f;

    float sliderZeroRef = 0f;

    public void SetTarget(Transform t) { target = t; }
    public void SetPlaneUpSource(Transform t) { planeUpSource = t; }

    public void RotateLeft()  { RotateBy(-stepDegrees); }
    public void RotateRight() { RotateBy(+stepDegrees); }

    public void RotateBy(float degrees)
    {
        if (!target) return;
        Vector3 up = planeUpSource ? planeUpSource.up : Vector3.up;
        target.RotateAround(target.position, up, degrees);
    }

    // Slider OnValueChanged(float)
    public void OnSliderChanged(float value)
    {
        if (!target) return;

        float v = Mathf.Clamp(value, sliderMin, sliderMax);
        float delta = v - sliderZeroRef;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            RotateBy(delta);
            sliderZeroRef = v;
        }
    }

    // Call when changing target; also set the UI Slider back to 0
    public void ResetSliderZero(float currentSliderValue = 0f)
    {
        sliderZeroRef = currentSliderValue;
    }
}