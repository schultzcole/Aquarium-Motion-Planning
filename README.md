# 5611-A3-Motion-Planning

Unity version 2018.3.12f1. Only tested on Windows. Should work out of the box on Mac, might work on the beta Linux Unity editor.

Full write up at https://sites.google.com/umn.edu/schu2808csci5611submissions/hw3-flocking-and-pathfinding

## Usage Instructions

### Unity Hub

1. Clone the repo.
2. Click "Open".
3. Navigate to `\5611-A3-Motion-Planning` and select the `Motion-Planning` directory and open.
4. Wait for asset import and compilation.
5. Open desired example scene and run.

### Unity Editor

1. Clone the repo.
2. File > "Open Project...".
3. Navigate to `\5611-A3-Motion-Planning` and select the `Motion-Planning` directory and open.
4. Wait for asset import and compilation.
5. Open desired example scene and run.

## Settings

The PRM and path debug display uses editor Gizmos. By default this isn't enabled in game view, but it can be enabled by clicking the Gizmos button in th game view header.

## Other Notes

On first importing the project, you may get a warning that Blender could not be found. This is normal if you do not have Blender installed, or if Blender is not the default program for opening .blend files. This is fine, as there is already a fish.fbx exported.

There may also be several warnings saying that fields are never assigned to. These fields are set in the Unity editor rather than in code so the warnings are safe to ignore.
