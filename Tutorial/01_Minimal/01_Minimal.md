
# Minimal World Locking Tools (WLT) setup for a world-locked application

For streamlined installation using the latest available tools, refer to [this article](https://microsoft.github.io/MixedReality-WorldLockingTools-Unity/DocGen/Documentation/HowTos/UsingWLT/JustWorldLock.html#automated-setup) in the main World Locking Tools documentation.

This Tutorial will walk you through manually adding the minimum required subset of the WLT to a project, in order to get world-locked behavior out of all the content in that application, without the need for application maintained WorldAnchors, or any other special behavior.

This tutorial refers to the [01_Minimal](https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/tree/master/Tutorial/01_Minimal) sample for reference.

The simple application being world-locked here is the simplest possible Unity AR application. Note that the setup is even simpler for MRTK applications. Any differences between adding to a bare versus MRTK app are noted in the steps.

For tips on getting started with HoloLens development, see the [final section](#assumed-hololens-deployment-background) of this article.

## Project source assets

https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/tree/master/Tutorial/01_Minimal

## World Locking Tools specific setup

1. Add an Adjustment node, and attach the Main Camera object to it.
  * It should look like this:

![Adjustment node as camera parent](~/DocGen/Images/Minimal/Adjustment.png)

  * Or, if using MRTK, like this:

![Adjustment node with MRTK Playspace](~/DocGen/Images/Minimal/AdjustMRTK.png)
 
2. Install the FrozenWorld Engine DLL.

    a. Install [NuGet for Unity](https://github.com/GlitchEnzo/NuGetForUnity/releases). It's really a phenomenal piece of technology, you should have it anyway. If for any reason you can't use NuGet for Unity, follow the steps for [Manual FrozenWorld Engine DLL Installation](https://microsoft.github.io/MixedReality-WorldLockingTools-Unity/DocGen/Documentation/HowTos/InitialSetup.html#manual-frozen-world-engine-dll-installation), and proceed to step 3.

    b. With NuGet for Unity installed, in Unity, go to 

    `NuGet > Manage NuGet Packages`
 
    * Search for FrozenWorld.Engine, and install the latest version of Microsoft.MixedReality.Unity.FrozenWorld.Engine.
 
![NuGet for Unity with Frozen World Engine](~/DocGen/Images/Minimal/NuGetFWE.png)

3. Download and import the latest version of the Microsoft.MixedReality.Unity.WorldLockingTools.CoreEngine unity package from [releases](https://github.com/microsoft/MixedReality-WorldLockingTools-Unity/releases).

![World Locking Tools releases page](~/DocGen/Images/Minimal/WLTReleases.png)

4. In your Unity Project, go to 

    `Assets > WorldLocking.Core > Prefabs`

    * Drag the WorldLockingManager prefab into your scene.

![WorldLockingManager prefab in Project Assets](~/DocGen/Images/Minimal/WLTPrefab.png)

At this point your project should look a lot like `01_Minimal`. A few visual reference points (colored capsules) have been added to the MinimalScene in `01_Minimal`, just so you can see that it is working (the capsules remain fixed relative to real world features around them during the session, and even persisting across sessions).

## Bonus round - adding anchor graph visualization

The application as-is will now do basic world locking. Any objects you add to the scene will be placed in the physical world relative to the head position on first running the application. They will then remain in fixed pose relative to the physical world through the session, and even across sessions. That's all your application requires for basic world-locking.

It can be useful to visualize the world anchor graph which the World Locking Tools creates and maintains as the underlying stabilization structure. Doing so is quite easy.

* Download and import the  version of Microsoft.MixedReality.Unity.WorldLockingTools.Tools which corresponds with the CoreEngine package you previously imported.

* If you didn't already, you will now have a **Mixed Reality** dropdown menu. Within it, find the *World Locking Tools* section. The *Configure scene* option automates the configuration you did in the previous section. Now select *Add development visualizers*.

![Automated addition of development visualizers](~/DocGen/Images/Minimal/AddVisualizers.png)

That's it. Now when you deploy, you will see the graph of WorldAnchor nodes created under the hood, along with some other helpful visualizations. More description is available in the [Tools documentation](https://microsoft.github.io/MixedReality-WorldLockingTools-Unity/DocGen/Documentation/HowTos/Tools.html#anchor-graph-visualization).

When you decide you don't need the visualizations anymore, you can either disable the AnchorGraphVisual in the scene, delete it, or run the *Remove development visualizers* option from the same *World Locking Tools* menu.

  ## Assumed HoloLens deployment background

If unfamiliar with building, deploying, and running applications on the HoloLens family of devices, a good place to start is with [HoloLens](https://docs.microsoft.com/windows/mixed-reality/holograms-100) and [HoloLens 2](https://docs.microsoft.com/windows/mixed-reality/mrlearning-base) tutorials. 

In addition, here are some emphasized tips common to all HoloLens development. Note that these are handled automatically by MRTK.

* Check the camera clear color. The `Clear Flags` should be "Solid Color", and the color should be transparent black (0,0,0,0).

![Setting the camera clear color](~/DocGen/Images/Minimal/CameraClearColor.png)

* Check permissions. 
  `Project Settings > Player > Publishing Settings > Capabilities`
  You generally want the following enabled:
  * InternetClient
  * InternetClientServer
  * PrivateNetworkClientServer
  * WebCam
  * Microphone
  * GazeInput

![Setting routine application permissions](~/DocGen/Images/Minimal/Permissions.png)


