# Align Sub Scene

## Summary:

An independent AlignmentManager is created and maintained to pin a subset of the scene independently from the alignment of the global Unity coordinate space.

![](~/DocGen/Images/Screens/AlignCraft.jpg)

### Why that is useful

In its most general and powerful usage, World Locking Tools for Unity (WLT) is used to world-lock and pin the entire Unity coordinate system to a known relationship with the physical world.

There are situations in which it is preferable to pin a subset of the scene, represented by a subtree in the scene hierarchy. This subtree can be independently pinned, and will independently adjust to maintain optimal alignment at its space pins.

The accompanying video tries to make the distinction between pinning the model's coordinate space at strategic points versus manipulating the model into a new position and orientation.

## Project source assets

https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/tree/master/Advanced/AlignSubScene

## Dependencies:
* Built on WLT version 0.8.5.
* Built with MRTK version v2.4.
* No further dependencies.

## Additional setup:

No special setup is required, but to fully experience it, you should set up some sort of proxy in your physical space. Take the relative distances between the space pins in the model, and put physical markers in your scene matching those dimensions (e.g. bits of masking tape).

If necessary or convenient, the relative positions of the space pins can be changed to better fit your physical environment.

## Running the sample:

On startup, the nose of the space craft will be stuck in your head (at the origin). Take a step back.

Grab any of the spheres and drag it into position to move the model's coordinate space. After positioning the first sphere, try to keep the other sphere's in a plausible new position as you move them. For that, having a physical proxy setup to give target to your sphere movements is helpful.

## Special controls:

At any time, clicking on the scene (but not the model) will place a placard into the scene. The placard will be put in Unity global space, so will be unaffected by manipulations of the model's space. Clicking on a placard will delete it.


