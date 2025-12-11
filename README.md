# SpatialSight-AR-Planner: Interactive Augmented Reality Urban Planning Tool

CityAR is an interactive augmented-reality sandbox for rapid city-layout exploration. It blends plane detection, image-based beacons, and physics-aware object placement to let users design miniature urban scenes directly on real-world surfaces. The system supports dynamic spawning, marker-anchored assets, intuitive item manipulation, and full placement validation through spatial tracking.

---
<img width="300" height="168" alt="image" src="https://github.com/user-attachments/assets/6797c316-d874-408c-8c72-ddd45ee5f2da" />



## Core Capabilities

### Surface Acquisition  
- Detects horizontal planes in the physical environment.  
- Allows users to select a desired build surface with a tap, generating a stable AR proxy.

### Marker-Driven Content Spawning  
- Uses three unique category markers (Buildings, Vegetation, Utilities).  
- Each marker spawns a curated set of three themed assets that track the marker until picked up.  
- Prevents duplicate spawns through a “spawn-once-per-marker” gate.

### Intelligent Object Manipulation  
- Tap any item to pick it up; it floats 0.5m in front of the camera.  
- Smart center-ray snapping aligns the item with the selected plane when the ray hits the surface.  
- Confirmation logic finalizes placement; failed placements trigger a physics-based fall and automatic respawn.  
- Y-threshold guard prevents infinite falling loops.

---

## Interaction Features

- **Rotation Controls**:  
  - UI buttons for incremental rotation.  
  - Gesture support including two-finger twist rotation.
  
- **Scaling Gestures**:  
  - Pinch-to-scale for precise adjustments.

- **Scene Reset System**:  
  - Instantly clears items, proxies, and markers.  
  - Rescanning brings assets back seamlessly.

---

## Platform & Build Details

- **Engine**: Unity 6000.2.4f1  
- **AR Platform**: ARCore XR Plugin  
- **Render Pipeline**: Built-in / URP  
- **Scripting Backend**: IL2CPP, ARM64  
- **Minimum Android API**: 26+  
- **Target Device**: Android smartphones

---

## 📂 Project Structure
<img width="130" height="192" alt="image" src="https://github.com/user-attachments/assets/20d54fb2-0535-4839-b3c6-31cf66a7e240" />

