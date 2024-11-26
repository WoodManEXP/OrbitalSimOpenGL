## Background

The OrbitalSIM produces an accurate 3D visualization of the interaction of bodies from their gravitational fields. It was inspired from Liu Cixin’s “Three Body Problem” sci fi novels. Cixin posits flinging primordial black holes into a solar system as a means of destroying it. He is right, it makes a mess of the system. 

(It is left to sci fi land to tell us of the Trisolarians achieving the energy levels and navigational  technology necessary to divert a black hole from far far away into someone’s solar system.)

With the sim working, it is a short hop to model various things of gravitational body interaction.

- When will Apophis come closest to Earth and what will that distance be?  
- What happens to Apohis after that encounter?  
- What happens if the sun in our solar system disappears?  
- What happens if a BH approaches at a high angle to the solar system plane?  
- What happens if a BH approaches at a low angle to the solar system plane?  
- What if the gravitational constant were to change (Remember the Star Trek episode with Q)  
- What if Jupiter vanished, or got heavier, or lighter  
- What happens to Earth’s moon if Earth vanishes  
- Closest or furthest approach between bodies in a system  
- Etc…

The sim will model any N-body system, so, for example, it is possible to model Cixin’s Trisolarian system with its three suns and a planet.

(There are sims out there that use techniques, not unlike this sim, that model billions of bodies and run across arrays of cooperating servers. They model galaxy and universe formation. This sim is computationally efficient, taking advantage quick lookups and available computational cores, but it is not set up to run such massive situations)

The sim begins with Ephemerides which are the initial state of the system. The sim can optionally read ephemerides for the human solar system from JPL’s Horizons system ( [https://ssd.jpl.nasa.gov/horizons](https://ssd.jpl.nasa.gov/horizons) ). It can also read them from .json files for defining systems not available in Horizons. Horizons ephemerides can also be combined with bodies from the .json files (eg. injecting a black hole into the human solar system). Ephemerides supply each body’s initial state in terms of mass, color, size, location, and velocity vectors.

The sim is iterative and works well iterating at 1 minute intervals. It calculates cumulative effects on each body from all other bodies, applies those effects for a minute and continually repeats the process. Intervals can be collapsed, eg. calculate 100 1 minute intervals between each redraw. This creates the effect of speeding the sim. Because of the vast distances involved there is little gain in iterating at shorter intervals. With longer intervals, hour or more, accumulating rounding errors begin to appear more quickly. This is most noticeable in relatively close bodies such as the Earth and its Moon or Mars and its moons.

Over long periods of time the sim unavoidably devolves into chaos. Small errors are accumulating in the double precision math. If this was being used for navigation on a real spaceship, new observations would be taken along the way to correct the errors accumulating in the sim’s math. The sim is also unaware of the miniscule gravitational effects of far away bodies elsewhere in the galaxy and universe. No worries though, they can be safely ignored for the scales on which this operates.

## Videos

These are videos, hosted on YouTube, running various models showing the sims's capabilities.

- One PBH approaching orbital plane, shallow angle  https://youtu.be/Ce85Par18Kw
- Asteroid 99942 Apophis' encounter with Earth  https://youtu.be/nBchlIUUG1U
- Change the gravitational constant  https://youtu.be/LqO-uI6X6h4
- BH approaching, high angle  https://youtu.be/-5p1qsAGkE4
- Stable solar system, but what if Sol disappears  https://youtu.be/qN0Kaa3Ua0s

## Implementation

The sim is implemented for MS Windows 10+ in C\# using Microsoft’s Visual Studio 2022 IDE. The 3D graphics is implemented via OpenGL/OpenTK via an OpenGL WPF control available at [https://github.com/opentk/GLWpfControl](https://github.com/opentk/GLWpfControl). The installed OpenGL control comes from [https://www.nuget.org/packages/OpenTK.GLWpfControl](https://www.nuget.org/packages/OpenTK.GLWpfControl).  The non 3D controls and visualizations are implemented via MS WPF facilities.

OpenTK.Mathematics supplies definitions for datatypes such as Vector3d and Color4. These are used throughout the sim rather than WPF versions such as Vector3D or Color.

It uses Newtonian equations, rather than relativistic (which is nicely accurate for the speeds at which typical bodies move). In fact, speeds are limited to .2c.

Most math is performed in Double precision.

The sim has several coordinate systems.

- System’s origin is at (0,0,0). This is the same origin expressed for Horizons.  
- UCoords are body positions in space expressed in km.  
- World coords are scaled UCoords that are fed to OpenGL. OpenGL does not operate well at Double Precision scales.  
- Screen coordinates coming in from point/click operations

Quaternions are used for rotations.

There are two main threads of execution. They communicate via messages over the event queue and so do not interfere with each other's operation. One is the UI thread, accepting user inputs and writing received sim data to status areas. The other thread runs the sim calculations. The sim calculation thread also drives OpenGL rendering. 

The one camera can be moved U/D/L/R and Forward/Backwards. It can be set to follow a body and/or keep looking at a body. The camera can be made to orbit about a body. The Camera is implemented in SimCamera.cs (one of the larger classes in the Sim).

The sim and camera are independent in that the Sim can be paused with the camera remaining movable.

Sim can be reset to initial conditions w/out a full sim restart.

Various characteristics of bodies can be changed as the sim is running (mass, velocity, exclude body, etc…).

The sim performs collision detection. Collisions are rare, given the vast distances encountered between bodies. It is easy enough though to make a model to send a body into a system to collide with the Sun or to change the gravitational constant such that one of Mars’ moons falls into the planet. Collisions are handled using the equations of elastic collisions. Each of the colliding bodies will lose mass and the “new” body will have new mass and velocity vectors. Collision detection can be disabled in which case bodies will pass through one another. See CollisionDetector.cs and CollisionHighlighter.cs.

## 3D Graphics

MS’s WPF attempted to supply a 3D drawing package called WPF 3D. Unfortunately it is only partially baked and could not handle this use-case. Fortunately OpenGL is a mature, robust system for 3D graphics, and the OpenTK DLLs help make it convenient to use in a WPF framework. There is a Zen to OpenGL that must be mastered to get it to draw. It is a highly capable 3D system that works nicely with modern graphics cards (GPU). It is necessary to carefully ponder how to use and optimize it.

The OpenTK DLLs, available at [NuGet Gallery | OpenTK.GLWpfControl 4.3.3](https://www.nuget.org/packages/OpenTK.GLWpfControl), are added to the project like this

![OpenTK DLLs](https://github.com/user-attachments/assets/b1c31a65-b4ee-4757-9a36-b1e8df44d755)

Using OpenTK (aka OpenGL) involves the basic algorithm

while (\!done)  
{  
	Erase previous scene  
	Construct and render next scene  
}

With efficient algorithms, a modern capable multi-core processor and graphics processor (eg Nvidia) this can happen 60+ times per second, creating smooth animation. Busier calculations will lower the frame rate. Even below 30 fps the animation quality is good..

OpenTK can handle many operations, but there is a tradeoff between what is to be handled by algorithms running on the central processor and the efforts of OpenTK on the graphics processor. For example, consider OpenGL’s *Frustum Culling*.

### Frustum Culling

A fuller explanation can be found at [LearnOpenGL \- Frustum Culling](https://learnopengl.com/Guest-Articles/2021/Scene/Frustum-Culling). Whatever OpenGL renders on the 2D screen from the 3D model is determined from, among other things, the current frustum definition. Here is a depiction of a frustum.  

![VisualCameraFrustum](https://github.com/user-attachments/assets/e4a6e51d-27b7-4784-89d3-9f5f34d0909d)

The entire 3D scene could be constructed by the upper algorithms and loaded into the GPU pipeline. But no matter how much is in the 3D scene, OpenGL will render on the 2D scene only that visible in the frustum. With the nontrivial processing cost of a) scene construction, b) loading the scene into the GPU, and c) the controller processing and clipping \- frame rates are going to drop, and processors will get hotter, from unnecessary processing.

The sim uses the class FrustumCuller to decide whether or not to load material into the GPU rendering pipeline.

### Object size

With the distance scales involved, OpenGL would scale most bodies in a scene to below 1 pixel and therefore not render them. The sim draws all bodies to scale (eg. If they somehow got near one another Sol would appear many times larger than Earth). But it also wants all bodies to be rendered at a minimum size to remain visible wherever they appear in a scene. This happens in the method SimBody.KeepVisible. A body’s physical size is temporarily scaled up so that when scaled by OpenGL it will render at a minimum pixel size in 2D.

## ToDo

It has been possible to get the sim to perform almost all the tasks originally envisioned. However, one case has proven elusive.The incremental algorithm falls apart in the case of an oscillation. Imagine two bodies placed at rest a certain distance apart and collision detection disabled.They would oscillate back and forth passing through one another.

With the incremental algorithm though as the bodies draw really close to one another the dramatic effects of dividing by d-squared, when d is small, causes bodies to accelerate to incredible speeds. Then with asymmetries in the time intervals at close ranges the effect is never cancelled mathematically in the next interval and the bodies incorrectly slingshot away from one another.

Solving this in the general case, referred to as the degenerate case, has proven elusive. This would be implemented in NextPosition.cs. 

It is possible to detect that a degenerate case has occurred by monitoring force vectors. If the force vectors acting on a body become nearly opposite from one iteration to the next then a body has passed through another body (were collision detection enabled this case will not arise).

Got any ideas?
