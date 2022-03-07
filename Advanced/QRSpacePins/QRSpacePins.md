# QR Space Pins sample

## Summary

The Space Pins feature of World Locking Tools is combined with QR code scanning to provide automatic snapping of a Unity scene to a physical environment. As the QR codes are physical features, this is an intrinsically shared experience.

![](~/DocGen/Images/Screens/QRScanCabinet.jpg)

See also the [accompanying video](https://youtu.be/OjmR2KfVUn8) for some visual clarification.

## Project source assets

https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/tree/master/Advanced/QRSpacePins

## Dependencies

* Built on WLT v1.5.8.
* Built with MRTK v2.7.2.
* QR code from NuGet package 0.5.2112.
* VCRTForwarders.140 (QR code dependency) from NuGet package 1.0.7.
* Built with Unity 2019.4.2f1.

Requires **HoloLens2** for QR code scanning.

## Additional setup

The QR code placement in the physical environment should match the QR code placement within the scene. For the virtual markers, look at objects "HouseHold > BarnMarkers > Markers > QRCode_N", where N is the integers 1 through 4.

Sample QR codes as .png files, suitable for printing and scanning, are in the folder Assets/QRSpacePin/SampleQRCodes. Note that the data embedded in each QR code is the file name without the extension. For example, the data embedded in QRCode_01.png is the string "QRCode_01". We'll talk more about the QR code data later.

You will need to either hang your printed QR codes so that their relative poses match the relative poses in the sample Unity scene, or adjust the poses of the SpacePin objects in the scene to match where you have placed the QR code printouts in your physical environment. It is very unlikely that you have a suitable physical environment to match the layout in the Unity scene.

Therefore, you will probably need to adjust the poses of the SpacePin objects in the Unity scene to match where you've put your printed QR codes. I recommend that you affix the printouts to walls surrounding your workspace, because they are easier to scan at eye level, and you will get the highest quality when you are between two or more SpacePins.

## A hypothetical example of scanned QR code position and orientation (not reflecting either of the contained samples)

Consider the following configuration.

### Unity scene

The position of QRCode_1 is (0,0,0), and its orientation is horizontal with the top pointed along positive X.

### Physical environment

The printed QR code is at your feet, with the top pointed north.

### Result

The Unity scene will appear with the origin at your feet, and the positive X axis pointed north.

## More about the QR code data

### Inputs for the SpacePin feature

The SpacePin feature repositions and reorients the coordinate space to match your specification. For the system to align the coordinate space the way you want, you need to tell it what you want.

To indicate the alignment you want, you tell the system the current pose at some position in space, and the pose that you want it to have there. The system then does whatever it needs to make it so.

In a perfect world, we would be done. But because of tracking error, numerical precision, and the rest of the usual gang of mathematical problems, you would find that the farther you got from the position you specified, the less accurate the alignment would be.

To avoid this problem, the system lets you give current/desired position pairs at multiple points in space. See the [SpacePin documentation](https://docs.microsoft.com/mixed-reality/world-locking-tools/documentation/concepts/advanced/spacepins), and elsewhere in the [World Locking Tools conceptual documentation](https://docs.microsoft.com/mixed-reality/world-locking-tools/documentation/concepts) for further details.

### Getting SpacePin inputs from QR codes

Having established that we need two pieces of information, the current pose at a point in space, and the pose we'd like our coordinate system to have at the point in space.

For the current pose, if we place a printed QR code somewhere in our physical environment, and scan it, the QR code system will tell us what its current pose is. That's half of the problem solved.

For the second half, we place corresponding proxies in our Unity scene in such a way that the pose of the proxy is the desired pose in our coordinate space.

For example, if you want the origin of your coordinate space (the (0,0,0) point) to be at the center of your physical room, you could print a QR code and tape it down in the center of your room. Then place a SpacePin at the origin of your Unity scene. At runtime, when you scan your physical QR code, you get back a pose in Unity's global coordinate space. You pass that to the SpacePin as the current pose, and the SpacePin takes its pose in Unity space (0,0,0) as the desired pose, and moves the coordinate space so that the origin is aligned with the printed QR code in the center of the room.

You can then repeat this process with more printed QR codes about your physical space and more SpacePins about your Unity scene to achieve the desired stability and accuracy.

### The QR code data string

You may have noticed a problem. If you have multiple printed QR codes, and multiple SpacePins in the scene, then when you scan a QR code, how do you know which SpacePin to feed the scanned pose into?

There are a lot of ways you could address this problem, and which way is best will depend on your application. The sample embeds the index of the corresponding SpacePin into the printed QR code's data string. The encoding is exceedingly simple, and in fact was chosen for its simplicity. Basically, the application finds the last underscore '_' character in the data string, and tries to interpret the substring after it as an integer. If it succeeds, and if the integer is a valid index, then it associates the scanned QR code's pose with the indexed SpacePin.

See QRSpacePinGroup.ExtractIndex() for the implementation. It is expected that you would replace this member with something more tuned to your application.

Note that in this simple implementation, the number in the QR code data string is one higher than the zero based index. For example, "IgnoredString_01" would resolve to the index 0, which is the first SpacePin in the QRSpacePinGroup. In the samples, that happens to be a SpacePin named "QRSpacePin_1".

## Running the sample

Deploy the scene QRSpacePins.unity and start the application. Scan your printed QR codes by walking to them. The smaller the printed QR code the closer you'll need to get to it to scan it. A 5x5cm printed code will need to be within about 20 cm to scan.

The codes can be scanned in any order.

The scanned codes can be cleared at any time with the voice command "Clear Space Pins". You can then start over scanning.

### The 2nd scene

QRSubScene is identical to QRSpacePins, with one significant exception. Whereas QRSpacePins aligns the entire Unity coordinate space to put the virtual QR codes at the physical QR markers, QRSubScene applies that transformation only to the root of the Household subtree of the scene graph.

It accomplishes this using the AlignSubtree script placed on the Household node. See the [AlignSubScene sample](../AlignSubScene/AlignSubScene.md), which explores alignment of independent coordinate spaces with the physical world in greater detail. For further details on the AlignSubtree script, consult the [World Locking Tools for Unity documentation](https://docs.microsoft.com/dotnet/api/microsoft.mixedreality.worldlocking.core.alignsubtree?view=wlt-unity-1.5).

## Special controls

### Speech commands

| Command              | Result
|----------------------|------------------------------------------------------
| Clear Space Pins     | Resets all QR codes and pins to startup
| Toggle World Lock    | Toggles whether the World Locking Tools are active

## Known issues

None

## See Also

* [QR code tracking overview](https://docs.microsoft.com/windows/mixed-reality/develop/advanced-concepts/qr-code-tracking-overview)
* [Unity QR code sample](https://github.com/microsoft/MixedReality-QRCode-Sample)
* [SpacePin discussion](https://docs.microsoft.com/mixed-reality/world-locking-tools/documentation/concepts/advanced/spacepins)