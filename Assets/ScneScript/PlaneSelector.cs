using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaneSelector : MonoBehaviour
{
    [SerializeField] Camera arCamera;
    [SerializeField] ARRaycastManager raycaster;
    [SerializeField] ARPlaneManager planeManager;
    [SerializeField] GameObject proxyVisual; // optional

    public Transform Proxy { get; private set; }
    public bool Chosen { get; private set; }

    static readonly List<ARRaycastHit> hits = new();

    void Awake(){ if(!arCamera) arCamera = Camera.main; }

    void Update()
    {
        if (Chosen || Input.touchCount == 0) return;
        var t = Input.GetTouch(0);
        if (t.phase != TouchPhase.Ended) return;

        if (!raycaster.Raycast(t.position, hits, TrackableType.PlaneWithinPolygon)) return;

        var pose = hits[0].pose;
        Proxy = new GameObject("BuildPlaneProxy").transform;
        Proxy.position = pose.position;
        Proxy.rotation = Quaternion.Euler(0, arCamera.transform.eulerAngles.y, 0);

        if (proxyVisual) Instantiate(proxyVisual, Proxy.position, Proxy.rotation, Proxy);

        var selected = planeManager.GetPlane(hits[0].trackableId);
        foreach (var p in planeManager.trackables) if (p != selected) p.gameObject.SetActive(false);
        planeManager.enabled = false;
        Chosen = true;
    }
}