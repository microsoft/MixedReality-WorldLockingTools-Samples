<img src="DocGen/Images/WorldLockingTools.svg">

# Welcome!

The World Locking Tools - Samples repository hopes to give you isolated independent examples, or even starting points, for anything you might do with the World Locking Tools for Unity.

## Useful links

[World Locking Tools for Unity (WLT) github repo](https://github.com/microsoft/MixedReality-WorldLockingTools-Unity).

[World Locking Tools for Unity (WLT) documentation landing page](https://microsoft.github.io/MixedReality-WorldLockingTools-Unity/README.html).

## About the Samples

Each sample is a self-contained Unity project. While that introduces a lot of redundancy between samples, it means that you don't have to wonder if something in a project is necessary. If the sample within that project doesn't use QR code scanning, then the QR code NuGet package won't be installed in that project.

General samples covering many scenarios can be found in the main WLT repo. This sibling repo allows the extension of sample coverage without bloating out the main repository. 

The samples can be thought of as forming two groups.

The first group is for very basic and slimmed down scenarios. The main repo's project can't show what a project looks like without adding the WorldLocking.Tools because the Tools are, of course, included in that project. This limits some of the tutorials we would like to cover.

The second group is for very advanced scenarios. More advanced scenarios often leverage the World Locking Tools capabilities to amplify the value from other, independent software and services. While these services might be only a NuGet feed away, bloating the core WLT repo with every NuGet package that might be useful for a sample is a frightening prospect.

## Contents

The two groups of samples are distributed in this repository as shown. This table will be updated as new Samples come online.

| Folder     | Project       | Description                                               |
|------------|---------------|-----------------------------------------------------------|
| `Tutorials`|               | Projects slimmed down to make a single point.             |
|            |               |                                                           |
| `Advanced` |               | Complex projects, often leveraging external dependencies. |
|            | [AlignSubScene](DocGen/Documentation/Advanced/AlignSubScene/AlignSubScene.md) | Aligning multiple independent spaces.                    |
|            | [QRSpacePins](DocGen/Documentation/Advanced/QRSpacePins/QRSpacePins.md)   | QR codes as external alignment markers.                  |

## Prerequisites

The basics of building and deploying a HoloLens application from Unity are assumed. If you aren't familiar with that, [here's a good place to start](https://docs.microsoft.com/en-us/windows/mixed-reality/holograms-101). Or if you want to go straight to HoloLens2, then [here](https://docs.microsoft.com/en-us/windows/mixed-reality/mrlearning-base).

For the Advanced samples, obviously familiarity with the basic World Locking Tools for Unity is assumed. Start [there](https://microsoft.github.io/MixedReality-WorldLockingTools-Unity/DocGen/Documentation/GettingStartedWithWorldLocking.html).

The Tutorials are most useful as auxiliary information for the documentation. Start with the documentation, and jump to the Tutorials when the documentation directs you there for illustration.

## Further documentation on the samples

Each sample contains enough documentation to run it, along with enough conceptual background to understand what it is doing, and enough motivational background to understand why one might be inclined to do so. 

  * [AlignSubScene](DocGen/Documentation/Advanced/AlignSubScene/AlignSubScene.md) 
  * [QRSpacePins](DocGen/Documentation/Advanced/QRSpacePins/QRSpacePins.md)

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
