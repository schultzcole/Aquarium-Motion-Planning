# 5611-A3-Motion-Planning

Unity version 2018.3.12f1. Only tested on Windows. Should work out of the box on Mac, might work on the beta Linux Unity editor.

## Overview

[S-Path Demo Video<br> <img src="https://img.youtube.com/vi/8uT3X8Yq2lE/maxresdefault.jpg" width=500>](https://youtu.be/8uT3X8Yq2lE)


[Checkerboard Demo Video<br> <img src="https://img.youtube.com/vi/XsssKV2h3aU/maxresdefault.jpg" width=500>](https://youtu.be/XsssKV2h3aU)

This is my 3D simulation of fish in an aquarium, implemented in Unity 3D (version 2018.3.12f1).
Obstacles can technically be any 3D mesh, however with some caveats (explained below).
The "fish" agents use the Boids algorithm for flocking behavior, and the simulation uses a probabilistic road map for pathfinding.

### Roadmap and Pathfinding

The PRM nodes and edges are generated statically when the simulation begins.
For a typical level, generating 1000 PRM nodes takes about .2 seconds, and checking visibility and connecting edges between points takes about .1 seconds.
Points are generated in approximate poisson disc distribution, using Mitchell's best candidate algorithm.

Rather than pathfinding from each agent to the goal to develop a unique path for each agent, I instead use Djikstra's algorithm to search in reverse from the goal to find all optimal paths from the goal to every PRM node.
In this case, Djikstra's is a better choice than A*, because it will find optimal paths to every node, rather than an optimal path to a particular goal node.
The user can press the spacebar to choose a new random goal at runtime, and the search is run in a background thread to maintain a reasonable framerate.
On a 1000 node PRM, it takes about 100-150ms to run the search.

Once these optimal paths have been found, an agent can query the PRM for the optimal direction to the goal from its location.
This is accomplished by averaging the optimal direction from nearby visible nodes, similar to a vector field.
This is a great solution because it requires relatively few PRM nodes to produce a good result for every point in the bounds of the simulation.
This means that any agent, regardless of where it is, can get a natural looking path to the goal, and it avoids conga-line situations because agents don't need to follow a prescribed path along PRM edges.

### Boids

As mentioned, agents use boids for flocking behavior.
This includes a target vector (the optimal direction to the goal from the PRM), as well as the typical 3 boids impulses: separation, alignment, and cohesion, but it also includes a couple of custom terms: obstacle avoidance, obstacle "slide", a random impulse, and a centering impulse to keep focus the agents in the center of the bounds when they do not have a target.

The obstacle avoidance impulse simply adds a bit of velocity pointing away from the nearest point on every obstacle within a given radius of the agent.
The obstacle "slide" impulse acts as a multiplier for the component of the velocity parallel to an obstacle.
This encourages agents to move along obstacles rather than "bounce" off of them repeatedly.
The obstacle avoidance and slide terms are not available for arbitrary mesh obstacles, though they are available for strictly convex mesh obstacles as well as primitive obstacles such as boxes, spheres, and capsules.
This does cause some issues for non-convex obstacles, as seen in the case of the monkey head demo video below;
agents often catch on edges of the mesh briefly before resuming a more normal path.

The random impulse uses perlin noise to inject continuous, fluid random motion into the paths of the agents.
This produces much more lifelike motion in the flock as without the random motion, the alignment term dominated and all of the agents eventually became one cohesive ball.

A final, purely cosmetic oscillation impulse is added, which wiggles the agent side to side as it moves, and, in my opinion, provides a pretty convincing swimming animation for the "fish".

There are a few points where the boids impulses make the motion appear non-optimal, especially when the goal is near an obstacle or the boundaries, evidenced by the agents alternating between moving towards and away from the goal (this is especially noticeable in the cylinder demo video I think). While this is clearly non-optimal, I do think it feels natural for a school fish, so I am not too displeased with it
With the narrow corridor scenario I was attempting to create a scenario where the obstacle avoidance term would cause issues, but it actually ended up working surprisingly well.

### Other demo videos

- [Cylinder Demo](https://www.youtube.com/watch?v=ySiT1d9AjxM)
- [Narrow Corridor Demo](https://www.youtube.com/watch?v=Acqr9srtxZw)
- [Monkey Head Demo](https://www.youtube.com/watch?v=7WWScAzcOK8)

## Usage Instructions

### Unity Hub

1. Clone the repo.
2. Click "Open".
3. Navigate to `\Aquarium-Motion-Planning` and select the `Motion-Planning` directory and open.
4. Wait for asset import and compilation.
5. Open desired example scene and run.

### Unity Editor

1. Clone the repo.
2. File > "Open Project...".
3. Navigate to `\Aquarium-Motion-Planning` and select the `Motion-Planning` directory and open.
4. Wait for asset import and compilation.
5. Open desired example scene and run.

## Settings

The PRM and path debug display uses editor Gizmos. By default this isn't enabled in game view, but it can be enabled by clicking the Gizmos button in th game view header.

## Other Notes

On first importing the project, you may get a warning that Blender could not be found. This is normal if you do not have Blender installed, or if Blender is not the default program for opening .blend files. This is fine, as there is already a fish.fbx exported.

There may also be several warnings saying that fields are never assigned to. These fields are set in the Unity editor rather than in code so the warnings are safe to ignore.
