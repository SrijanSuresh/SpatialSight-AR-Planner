using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;

public class SimpleTouchManipulator : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] Camera arCamera;            // AR Camera
    [SerializeField] PlaneSelector planeSelector; // your plane proxy provider
    [SerializeField] ARRaycastManager raycaster;  // optional if you later want drag-on-plane

    [Header("Target control")]
    [SerializeField] LayerMask itemLayer = ~0;    // set to your Items layer, or leave as default
    [SerializeField] float minScale = 0.03f;      // 3 cm
    [SerializeField] float maxScale = 0.30f;      // 30 cm
    [SerializeField] float rotateSpeed = 1.0f;    // multiply twist angle
    [SerializeField] bool requireSelection = true;// if true, gestures only affect the last tapped item

    Transform selected;       // last tapped item
    float lastPinchDist = -1; // previous distance between two touches
    float lastTwistAngle = 0; // previous two-finger angle

    void Awake()
    {
        if (!arCamera) arCamera = Camera.main;
    }

    void Update()
    {
        if (EventSystem.current && Input.touchCount > 0)
        {
            // ignore UI touches
            for (int i = 0; i < Input.touchCount; i++)
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId)) return;
        }

        // 1-finger tap to select target
        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Ended) TrySelectAt(t.position);
            // reset two-finger state when back to one finger
            lastPinchDist = -1; lastTwistAngle = 0;
        }
        // 2-finger rotate + scale
        else if (Input.touchCount >= 2)
        {
            if (requireSelection && !selected) return;

            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);

            // current pinch distance
            float curDist = Vector2.Distance(t0.position, t1.position);

            // current twist angle (relative to screen x axis)
            float curAngle = Mathf.Atan2(t1.position.y - t0.position.y,
                                         t1.position.x - t0.position.x) * Mathf.Rad2Deg;

            if (lastPinchDist > 0)
            {
                // Scale
                float scaleFactor = curDist / lastPinchDist;
                if (selected)
                {
                    // uniform scale, clamp to bounds
                    float s = Mathf.Clamp(selected.localScale.x * scaleFactor, minScale, maxScale);
                    selected.localScale = new Vector3(s, s, s);
                }
            }

            if (lastTwistAngle != 0)
            {
                // Rotate around plane up (or world up if plane not chosen yet)
                float deltaAngle = Mathf.DeltaAngle(lastTwistAngle, curAngle) * rotateSpeed;
                var axisUp = (planeSelector && planeSelector.Chosen && planeSelector.Proxy)
                            ? planeSelector.Proxy.up : Vector3.up;

                if (selected)
                    selected.RotateAround(selected.position, axisUp, deltaAngle);
            }

            lastPinchDist = curDist;
            lastTwistAngle = curAngle;
        }
        else
        {
            // no touches
            lastPinchDist = -1; lastTwistAngle = 0;
        }
    }

    void TrySelectAt(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, 25f, itemLayer))
        {
            var go = hit.rigidbody ? hit.rigidbody.gameObject : hit.collider.gameObject;
            selected = go.transform;
        }
    }

    // Optional public API if you want UI buttons too
    public void RotateSelected(float degrees)
    {
        if (!selected) return;
        var axisUp = (planeSelector && planeSelector.Chosen && planeSelector.Proxy)
                    ? planeSelector.Proxy.up : Vector3.up;
        selected.RotateAround(selected.position, axisUp, degrees);
    }
    public void ScaleSelected(float target)
    {
        if (!selected) return;
        float s = Mathf.Clamp(target, minScale, maxScale);
        selected.localScale = new Vector3(s, s, s);
    }
}