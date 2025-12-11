using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public enum MarkerCategory { Buildings, Vegetation, Utilities, Unknown }

public class MarkerTrackerAndSpawner : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] ARTrackedImageManager imageManager;

    [Header("Per-category prefabs (3 each)")]
    [SerializeField] List<GameObject> buildings;
    [SerializeField] List<GameObject> vegetation;
    [SerializeField] List<GameObject> utilities;

    [Header("Spawn layout")]
    [SerializeField] float ringRadius = 0.12f;
    [SerializeField] float yOffset = 0.02f;
    [SerializeField] bool overridePrefabScale = true;
    [SerializeField] float defaultScale = 0.08f;
    [SerializeField] bool randomizeYaw = true;

    // Prevent duplicate spawns per marker name
    readonly HashSet<string> spawnedFor = new();
    // marker name -> beacon root (parented to the tracked image so it follows)
    readonly Dictionary<string, Transform> beaconsByName = new();
    // track all spawned item instances so Reset can nuke them fast
    readonly List<GameObject> spawnedItems = new();

    void OnEnable()
    {
        if (imageManager != null)
            imageManager.trackedImagesChanged += OnImagesChanged; // ARF6: deprecated but supported
    }

    void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackedImagesChanged -= OnImagesChanged;
    }

    void OnImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var img in args.added)   HandleVisible(img);
        foreach (var img in args.updated)
        {
            if (img.trackingState == TrackingState.Tracking) HandleVisible(img);
            else HandleLost(img);
        }
        foreach (var img in args.removed) HandleLost(img);
    }

    void HandleVisible(ARTrackedImage img)
    {
        string name = img.referenceImage.name;
        var cat = NameToCategory(name);
        if (cat == MarkerCategory.Unknown) return;

        // Ensure a beacon root that follows the marker
        if (!beaconsByName.TryGetValue(name, out var root) || root == null)
        {
            root = new GameObject($"{cat}BeaconRoot").transform;
            root.SetParent(img.transform, worldPositionStays: false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            beaconsByName[name] = root;
        }

        if (spawnedFor.Contains(name)) return; // spawn once per marker
        spawnedFor.Add(name);

        var list = cat switch
        {
            MarkerCategory.Buildings  => buildings,
            MarkerCategory.Vegetation => vegetation,
            MarkerCategory.Utilities  => utilities,
            _ => null
        };
        if (list == null || list.Count == 0) return;

        int count = Mathf.Min(3, list.Count);
        for (int i = 0; i < count; i++)
        {
            float ang = i * Mathf.PI * 2f / count;
            Vector3 pos = root.position + new Vector3(Mathf.Cos(ang) * ringRadius, yOffset, Mathf.Sin(ang) * ringRadius);

            var go = Instantiate(list[i], pos, root.rotation, root);
            if (randomizeYaw) go.transform.rotation *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            if (overridePrefabScale) go.transform.localScale = Vector3.one * defaultScale;

            // tappable/carryable
            if (!go.GetComponent<Collider>()) go.AddComponent<BoxCollider>();
            var rb = go.GetComponent<Rigidbody>(); if (!rb) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;

            spawnedItems.Add(go);
        }
    }

    void HandleLost(ARTrackedImage img)
    {
        // Allow re-spawn if the marker fully leaves view and returns later
        spawnedFor.Remove(img.referenceImage.name);
    }

    public Transform GetBeaconFor(GameObject item)
    {
        // Walk up parents to find the nearest BeaconRoot
        var t = item ? item.transform.parent : null;
        while (t != null)
        {
            if (t.name.EndsWith("BeaconRoot")) return t;
            t = t.parent;
        }
        return null;
    }

    // UI Button → drag this component → ResetAll
    public void ResetAll()
    {
        // 1) Destroy all spawned items
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            var go = spawnedItems[i];
            if (go) Destroy(go);
        }
        spawnedItems.Clear();

        // 2) Destroy all beacon roots
        foreach (var kv in beaconsByName)
        {
            if (kv.Value) Destroy(kv.Value.gameObject);
        }
        beaconsByName.Clear();

        // 3) Clear “spawn once” gates so scanning will repopulate
        spawnedFor.Clear();
    }

    MarkerCategory NameToCategory(string n)
    {
        var s = n.ToLowerInvariant();
        if (s.Contains("build")) return MarkerCategory.Buildings;
        if (s.Contains("veg"))   return MarkerCategory.Vegetation;
        if (s.Contains("util"))  return MarkerCategory.Utilities;
        return MarkerCategory.Unknown;
    }
}