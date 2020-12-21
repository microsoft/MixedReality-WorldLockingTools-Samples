# Direct to ARCore

## Summary:

This sample codes directly to Google's ARCore SDK for Unity.

## Dependencies:
* Built on WLT version 1.1.1.
* Incompatible with MRTK
* Built on Unity v2019.4.15f.
* Built on ARCore Unity SDK v1.22.0.
* Uses Frozen World Engine DLL v1.0.0 or later.

## Additional setup:

Building the sample requires installing Google's ARCore Unity SDK v1.22.0 or later, including dependencies.

These notes are intended to be helpful in setting up ARCore Unity SDK, but are no substitute for the official full documentation, which can be found at the following:

> https://developers.google.com/ar/develop/unity/quickstart-android

The instructions here are trying to be brief. If anything is unclear, please refer to the full documentation for details.

### Installing dependencies

From Unity Package Manager, install:

* Multiplayer HLAPI
* XR Legacy Input Helpers

### Installing ARCore Unity SDK

Download the ARCore Unity SDK unity package from [github](https://github.com/google-ar/arcore-unity-sdk/releases).

Import the .unitypackage into the Unity project. The examples may be imported or not, on your preference.

> [!NOTE]: ARCore as packaged is not in an assembly specialization, that is it compiles into the predefined Assembly-CSharp.dll. Unfortunately, [from the Unity documentation](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html):
>> Classes in assemblies created with an Assembly Definition cannot use types defined in the predefined assemblies.
> Therefore, in order for WLT to access and manage ARCore, ARCore must be moved into an Assembly Definition.
> This next step achieves that, by simply unpacking appropriate assembly definition (.asmdef) files onto the ARCore assets imported in the previous step.
> IMPORTANT: If the folder structure of the GoogleARCore assets is to be changed or renamed, first perform the following step.

Import arcore-1.22.0-asmdef-fixup.unitypackage into your project. This unity package can be found in the Misc folder of this sample. It contains only assembly definition (.asmdef) files.

* Once the ARCore unitypackage and the fixup unitypackage have been imported, you may safely rename or move the GoogleARCore folder to match your organization.

Ensure your build target is Android.

In your project's Player Settings, in the XR section at the end, ensure that "ARCore Supported" is enabled.

## Running the sample:
Brief walk through of:
Startup – where are you?
Next steps – how do you get to the good stuff?
Conclusion – what should you have experienced?

## Special controls:
Any speech commands, UX, etc. What does one need to know to fully experience the sample?

