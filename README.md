# MRI VR Prep

Unity app for Meta Quest that prepares patients for MRI scans with an immersive 360° walkthrough of the examination, with head-motion tracking in development.

DTU special course in collaboration with Rigshospitalet (TUH). The project continues the proof-of-concept developed by Mathias Alyas, Mehmet Erkul and Sami Faisal Sheikh (spring 2026), which recreated the MRI patient journey using 360° video. This iteration focuses on head-motion tracking and feedback, so patients can practise lying still before their scan.

## Status

- [x] Unity project configured for Meta Quest (OpenXR)
- [x] Head-movement mock scene (black background, fixation target, warnings, stillness score)
- [x] Editor head simulator for testing without a headset
- [x] CSV logging of head rotation and translation
- [ ] Test on headset and measure tracking noise
- [ ] Set movement thresholds based on measured data
- [ ] Integrate 360° MRI video
- [ ] Scan sequences with MRI scanner sound
- [ ] Patient testing

## Requirements

- **Unity 6.6** (6000.6.3f1) with **Android Build Support** (OpenJDK, Android SDK & NDK Tools)
- **Git LFS**
- **Meta Quest** headset with developer mode enabled (for device testing)
- **VS Code** with the Unity extension and the .NET 10 SDK (optional, for scripting)
- **Meta Horizon Link** app (optional, for Play mode in the headset via Quest Link; requires a dedicated GPU)

## Getting started

```bash
git lfs install
git clone https://github.com/YOUR-USERNAME/mri-vr.git
```

1. In Unity Hub: **Add → Add project from disk** and select the cloned folder.
2. Open the project with Unity 6.6 (first open takes a few minutes while `Library/` is built).
3. Open `Assets/Scenes/HeadMovementMock.unity` and press **Play**.

Avoid cloning into cloud-synced folders (OneDrive, Dropbox, iCloud) if you run into file-locking or sync errors.

## Project structure

```
Assets/
├── Scenes/
│   └── HeadMovementMock.unity   Head-movement warning prototype
├── Scripts/
│   ├── HeadMovementMonitor.cs   Calibration, movement detection, feedback, CSV logging
│   └── HeadSimulator.cs         Mouse/keyboard head control when no headset is active
└── XR/                          OpenXR settings
```

## Head-movement mock

After a 3-second countdown, the current head pose is stored as the baseline and a fixation dot is placed straight ahead. Every frame, the head's rotation (degrees) and translation (mm) relative to the baseline are compared with the thresholds. Exceeding either turns the dot red and shows a warning. The stillness score is the percentage of time spent within the thresholds.

Settings on the `HeadMonitor` object (Inspector):

| Setting | Default | Description |
|---|---|---|
| Max Rotation Deg | 3 | Rotation threshold (placeholder) |
| Max Translation Mm | 10 | Translation threshold (placeholder) |
| Calibration Delay | 3 s | Countdown before baseline is recorded |
| Target Distance | 2 m | Distance to the fixation dot |
| Editor Log Folder | `HeadLogs` | Log folder in the editor (relative to project root, or a full path) |

The thresholds are placeholders until the Quest's tracking noise has been measured.

### Editor controls (no headset)

| Input | Action |
|---|---|
| Right mouse button + drag | Rotate head |
| W / A / S / D / Q / E | Move head |
| Hold Shift | Fine movement (mm scale) |
| Space | Sudden twitch that returns to rest |
| R | Recalibrate |

The simulator disables itself automatically when a headset is active.

### Logging

Each session is saved as `headlog_YYYYMMDD_HHMMSS.csv`:

```
time_s,rotation_deg,translation_mm,moving
0.011,0.00,0.0,0
```

- **Editor / Quest Link:** `HeadLogs/` in the project root (ignored by Git)
- **Quest build:** `Android/data/<package name>/files/` on the headset, retrieved with `adb pull`

Participant data must not be committed to this repository. Store it only where approved by TUH/DTU.

## Building for Meta Quest

1. Connect the headset via USB-C and accept **Allow USB debugging**.
2. **File → Build Profiles → Meta Quest**, select the headset under **Run Device**.
3. **Build and Run**, saving the APK in `Builds/` (ignored by Git).

For faster iteration, use Quest Link: set Meta Horizon Link as the active OpenXR runtime (or choose it under **Project Settings → XR Plug-in Management → OpenXR → Play Mode OpenXR Runtime**) and press Play in Unity.

## Git workflow

- Large binary files (video, audio, images, models, APKs) are tracked with **Git LFS**. See `.gitattributes`.
- Commit `.meta` files together with their assets.
- When replacing a script or asset, overwrite the file instead of deleting and re-adding it, so references in scenes are preserved.
- Scenes are text-serialized; avoid editing the same scene in parallel.

## Team

- Rasmus (s224690)
- *Add team members*

Clinical collaborator: *Add name*, Rigshospitalet
Supervisor: *Add name*