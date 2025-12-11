using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlacementManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Camera arCamera;
    [SerializeField] ARRaycastManager raycaster;
    [SerializeField] PlaneSelector planeSelector;
    [SerializeField] MarkerTrackerAndSpawner markerSystem;

    [Header("Tuning")]
    [SerializeField] float carryDistance = 0.5f;
    [SerializeField] float followLerp = 18f;
    [SerializeField] float fallYThreshold = -0.3f; // relative to proxy.y
    [SerializeField] LayerMask itemLayer = ~0;

    static readonly List<ARRaycastHit> hits = new();
    GameObject carried;
    Transform carriedHome;
    Vector3 carriedHomeLocal;

    void Awake()
    {
        if (!arCamera) arCamera = Camera.main;
    }

    void Update()
    {
        // One-tap select or confirm, but ignore UI touches
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            if (IsTouchOverUI()) return;

            var t = Input.GetTouch(0);

            if (carried == null) TrySelectItem(t.position);
            else ConfirmOrDrop();
        }

        if (carried) UpdateCarried();
    }

    bool IsTouchOverUI()
    {
        if (EventSystem.current == null) return false;

        // Touch devices
        for (int i = 0; i < Input.touchCount; i++)
        {
            int id = Input.GetTouch(i).fingerId;
            if (EventSystem.current.IsPointerOverGameObject(id)) return true;
        }

        // Mouse/editor fallback
        return EventSystem.current.IsPointerOverGameObject();
    }

    void TrySelectItem(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, 15f, itemLayer)) return;

        carried = hit.rigidbody ? hit.rigidbody.gameObject : hit.collider.gameObject;

        // Remember beacon root and original local pos for respawn
        carriedHome = markerSystem.GetBeaconFor(carried);
        if (carriedHome) carriedHomeLocal = carried.transform.localPosition;

        var rb = carried.GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.useGravity = false; }
    }

    void UpdateCarried()
    {
        if (!planeSelector.Chosen || planeSelector.Proxy == null)
        {
            FloatInFront();
            return;
        }

        Vector2 center = new(Screen.width * 0.5f, Screen.height * 0.5f);
        if (raycaster.Raycast(center, hits, TrackableType.PlaneWithinPolygon))
        {
            var origin = planeSelector.Proxy.position;
            var up = planeSelector.Proxy.up;
            var target = hits[0].pose.position;

            var flat = Vector3.ProjectOnPlane(target - origin, up);
            var snapped = origin + flat;

            carried.transform.position = Vector3.Lerp(
                carried.transform.position, snapped, Time.deltaTime * followLerp);

            var desiredRot = Quaternion.LookRotation(planeSelector.Proxy.forward, up);
            carried.transform.rotation = Quaternion.Slerp(
                carried.transform.rotation, desiredRot, Time.deltaTime * 10f);
        }
        else
        {
            FloatInFront();
        }
    }

    void FloatInFront()
    {
        var target = arCamera.transform.position + arCamera.transform.forward * carryDistance;
        carried.transform.position = Vector3.Lerp(
            carried.transform.position, target, Time.deltaTime * followLerp);

        var look = Quaternion.LookRotation(arCamera.transform.forward, Vector3.up);
        carried.transform.rotation = Quaternion.Slerp(
            carried.transform.rotation, look, Time.deltaTime * 10f);
    }

    void ConfirmOrDrop()
    {
        Vector2 center = new(Screen.width * 0.5f, Screen.height * 0.5f);
        bool onPlane = raycaster.Raycast(center, hits, TrackableType.PlaneWithinPolygon);

        var rb = carried.GetComponent<Rigidbody>();
        if (onPlane)
        {
            if (rb) { rb.isKinematic = true; rb.useGravity = false; }
            carried = null;
            carriedHome = null;
        }
        else
        {
            if (rb) { rb.isKinematic = false; rb.useGravity = true; }
            StartCoroutine(RespawnAfterFall(_obj: carried, home: carriedHome, local: carriedHomeLocal));
            carried = null; // coroutine will handle the object by captured reference
        }
    }

    IEnumerator RespawnAfterFall(GameObject _obj, Transform home, Vector3 local)
    {
        var proxy = planeSelector.Proxy;
        float t = 0f;
        while (_obj && proxy && (_obj.transform.position.y - proxy.position.y) > fallYThreshold && t < 2f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (_obj && home)
        {
            var rb = _obj.GetComponent<Rigidbody>();
            if (rb) { rb.isKinematic = true; rb.useGravity = false; }
            _obj.transform.SetParent(home, worldPositionStays: false);
            _obj.transform.localPosition = local;
            _obj.transform.localRotation = Quaternion.identity;
        }
    }
}