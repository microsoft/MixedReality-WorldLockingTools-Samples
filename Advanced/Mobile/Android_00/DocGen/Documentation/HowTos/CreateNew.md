# Adding World Locking Tools to a HoloLens application

Naturally, to begin we'll need a HoloLens application to which we plan to add the World Locking Tools.  

## Getting started with HoloLens development

This tutorial covers integrating the World Locking Tools into HoloLens UWP applications. Some proficiency in HoloLens development is assumed.

If unfamiliar with HoloLens development in Unity, here are some resources to help get started. 

* Great collection of materials for getting started with HoloLens 1 and HoloLens 2. When building a first ever application for HoloLens, [this tutorial](https://docs.microsoft.com/en-us/windows/mixed-reality/holograms-100) is highly recommended.
   
  > https://docs.microsoft.com/en-us/windows/mixed-reality/tutorials

* Some background on the concepts of spatial anchors in Mixed Reality, before the introduction of World Locking Tools.
  
  > https://docs.microsoft.com/en-us/windows/mixed-reality/spatial-anchors

* Requisite concepts and terminology for coordinate spaces in the general context of Mixed Reality experiences. For Unity specific discussion of the same, see below. Note that the World Locking Tools enable the [Stage Frame of Reference](https://docs.microsoft.com/en-us/windows/mixed-reality/coordinate-systems#stage-frame-of-reference) described, as well as making other experiences much simpler and robust to achieve.
  > https://docs.microsoft.com/en-us/windows/mixed-reality/coordinate-systems

* Discussion of the coordinate systems above, but in the context of implementation in a Unity application.
 
  > https://docs.microsoft.com/en-us/windows/mixed-reality/coordinate-systems-in-unity

Once able to create, deploy, run, and debug an application on the HoloLens, resume this tutorial from here.

## Create a HoloLens application

MMM Can create a new basic application to grow from later, or start with an existing (working) HoloLens application.

Create or open a HoloLens application. The application can be pretty much anything, but it should have a few fixed visible assets near the startup position in it. World Locking Tools will be trying to world-lock the objects in your scene to the physical world. This will be easiest to verify if there are visible objects, ideally around the viewer's starting position, whose registration with the physical world can be marked.

MMM Point to sample basic HoloLens app, just a couple of capsules surrounding the origin?

#### What is the behavior that we're trying to improve?

Before applying the World Locking Tools, we will take note of the default behavior without them. In this walkthrough, reference objects within the scene are referred to as "the capsules". This is only because capsules are included in the sample simple project above. Any visual holograms near the viewer will work. 

1) Startup the app
2) Note where one or more of the capsules are. For example, place paperweights under two of them.
3) Wearing the HoloLens, walk around, possibly leaving the room.
4) Return and check the placement of the capsules. They will probably have moved.
5) Quit the application. Make sure to close its slate as well to fully close it.
6) Restart the application from a different spot than the first time. The capsules will have moved.

## Add World Locking Tools

We'll now add the World Locking Tools in a series of steps, going from simplest working scenario up through more complex setups.

### Core function

Import the core engine package, Microsoft.WorldLockingTools.Unity.CoreEngine.unitypackage.

Create an empty object in the scene and name it "Adjustment". Parent the main camera to the Adjustment object. This parent of the main camera's transform will be used by the World Locking Tools to stabilize the global coordinate space.

Create an empty object in the scene at the root level named "WorldLocking". This will help keep things organized later.

Look in the project folder WorldLocking.Core/Prefabs for the WorldLockingManager prefab. Drag it into the scene as a child of the WorldLocking object created above.

Examine the WorldLockingContext script on the WorldLockingManager object. Note the version info on the top of the script. This can be useful in verifying what version of WorldLockingTools is being used in a project.

For the Automation Settings, leave them with "Use Defaults". 

For the Camera Transform Links, the system needs to know two transforms in the scene. The first, the "Adjustment Frame", is the transform it will be applying its corrections to. Set that to the "Adjustment" object created above. 

The second, the "Camera Parent", is the object which is the parent of the scene's main camera. As the scene is currently set up, that is also the "Adjustment" object created above. 

> Note that this setup will generate a warning, because with the single parent of the main camera, teleport and related camera controls are not possible. Since this sample won't be implementing teleport, we can ignore this warning.

> Note that if these aren't set, the system can generally guess what objects to use. But when possible, it's best to not make the system guess.

That's it for setup, now build and deploy to the HoloLens device.

#### What's different now? 

1) Startup the app
2) Note where one or more of the capsules are. For example, place paperweights under two of them.
3) Wearing the HoloLens, walk around, possibly leaving the room.
4) Return and check the placement of the capsules. They should not have moved.
5) Quit the application. Make sure to close its slate as well to fully close it.
6) Restart the application from a different spot than the first time. The capsules should not have moved.

### Visualizing what's happening

The World Locking Tools provides some helpers for visualizing what's going on during development. A shipping product should only require the contents of the Core Engine package that was imported above. But now import the visualization tools from Microsoft.WorldLockingTools.Unity.Tools.unitypackage.

From WorldLocking.Tools/Prefabs, drag the AnchorGraphVisualizer prefab onto the WorldLocking object created above. Now build and deploy to HoloLens.

#### What visualizations should be seen now?

1) Startup the app
2) Move around a bit. A couple of things to notice:
    * The marker for the Spongy origin (wherever your head was on startup).
    * The marker for the Frozen origin (wherever your head was on original startup).
    * The trail of connected markers left as you move around (about 1 meter spacing).
    * Notice that every time you start the app, the Spongy origin is wherever your head was at startup, but the Frozen origin remains constant from session to session.

MMM Screenshot

## Adding MRTK

The World Locking Tools have no dependency on [MRTK](https://microsoft.github.io/MixedRealityToolkit-Unity/README.html). The application is free to use any UX package or strategy available. 

That said, the World Locking Tools are entirely compatible with MRTK, and the power and popularity of MRTK makes it a clear choice for supplying the UI layer in samples and tutorials such as this one.

Import the following .unitypackages from [MRTK Release](https://github.com/microsoft/MixedRealityToolkit-Unity/releases):

* Microsoft.MixedReality.Toolkit.Unity.Foundation.2.3.0.unitypackage
* [Optional] Microsoft.MixedReality.Toolkit.Unity.Tools.2.3.0.unitypackage

> Note that this documentation was written using MRTK v2.3.0. There may be variations if using a different version of MRTK.



