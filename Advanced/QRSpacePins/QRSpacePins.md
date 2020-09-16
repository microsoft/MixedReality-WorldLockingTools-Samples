# QR Space Pins sample

## Summary:

The Space Pins feature of World Locking Tools is combined with QR code scanning to provide automatic snapping of a Unity scene to a physical environment. As the QR codes are physical features, this is an intrinsically shared experience. 

![](~/DocGen/Images/Screens/QRScanCabinet.jpg)

## Project source assets

https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/tree/master/Advanced/QRSpacePins

## Dependencies:
* Built on WLT v1.0.0.
* Built with MRTK v2.4.
* QR code from NuGet package 0.5.2102.
* VCRTForwarders.140 (QR code dependency) from NuGet package 1.0.6. 
* Built with Unity 2019.4.2f1.

Requires **HoloLens2** for QR code scanning.

## Additional setup:

The QR code placement in the physical environment should match the QR code placement within the scene. For the virtual markers, look at objects "HouseHold > BarnMarkers > Markers > QRCode_N", where N is the integers 1 through 4. 

For example, consider the following configuration.

### Unity scene:
The position of QRCode_1 is (0,0,0), and its orientation is horizontal with the top pointed along positive X.

### Physical environment:
The printed QR code is at your feet, with the top pointed north.

### Result:
The Unity scene will appear with the origin at your feet, and the positive X axis pointed north.

## Running the sample:

Deploy the scene QRSpacePins.unity and start the application. Scan your printed QR codes by walking to them. The smaller the printed QR code the closer you'll need to get to it to scan it. A 5x5cm printed code will need to be within about 20 cm to scan.

The codes can be scanned in any order.

The scanned codes can be cleared at any time with the voice command "Clear Space Pins". You can then start over scanning.

### The 2nd scene

QRSubScene is identical to QRSpacePins, with one significant exception. Whereas QRSpacePins aligns the entire Unity coordinate space to put the virtual QR codes at the physical QR markers, QRSubScene applies that transformation only to the root of the Household subtree of the scene graph.

It accomplishes this using the AlignSubtree script placed on the Household node. See the [AlignSubScene sample](../AlignSubScene/AlignSubScene.md), which explores alignment of independent coordinate spaces with the physical world in greater detail. For further details on the AlignSubtree script, consult the [World Locking Tools for Unity documentaton](https://microsoft.github.io/MixedReality-WorldLockingTools-Unity/DocGen/Temp/api/Microsoft.MixedReality.WorldLocking.Examples.AlignSubtree.html?q=alignsubtree).

## Special controls:

### Speech commands:

| Command              | Result
|----------------------|------------------------------------------------------
| Clear Space Pins     | Resets all QR codes and pins to startup
| Toggle World Lock    | Toggles whether the World Locking Tools are active

## Known issues

### No QR codes scanned on first run

The first time running the application after initial deployment, the enumeration of the QR watcher never completes. This means no QR codes are ever scanned.

Closing the application and running a second time fixes the problem, and the 2nd run and thereafter QR codes scan fine.

This is probably an issue with the permissions setup on the first run, but since it is a QR scanning issue, and not related to world-locking, it hasn't been a priority to track down. Tracked as [issue #20](https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/issues/20).