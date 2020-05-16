# Introduction

## About the Samples

Each sample is a self-contained Unity project. While that introduces a lot of redundancy between samples, it means that you don't have to wonder if something in a project is necessary. If the sample within that project doesn't use QR code scanning, then the QR code NuGet package won't be installed in that project.

General samples covering many scenarios can be found in the main WLT repo. This sibling repository allows the extension of sample coverage without bloating out the main repository. 

## Grouping

The samples can be thought of as forming two groups.

The first group is for very basic and slimmed down scenarios. The main repo's project can't show what a project looks like without adding the WorldLocking.Tools package because the Tools are, of course, included in that project. This limits some of the tutorials we would like to cover.

The second group is for advanced scenarios. More advanced usages often leverage the World Locking Tools capabilities to amplify the value from other, independent software and services. While these services might be only a NuGet feed away, bloating the core WLT repo with every NuGet package that might be useful for a sample is a frightening prospect.

## Organization

The two groups of samples are distributed in this repository as shown. This table will be updated as new Samples come online. Links to documentation for each is included in the table.

Each sample contains enough documentation to run it, along with enough conceptual background to understand what it is doing, and enough motivational background to understand why one might be inclined to do so. 

| Folder     | Project       | Description                                               |
|------------|---------------|-----------------------------------------------------------|
| `Tutorials`|               | Projects slimmed down to make a single point.             |
|            |               |                                                           |
| [Advanced](../../Advanced/Advanced.md) |               | Complex projects, often leveraging external dependencies. |
|            | [AlignSubScene](../../Advanced/AlignSubScene/AlignSubScene.md) | Aligning multiple independent spaces.                    |
|            | [QRSpacePins](../../Advanced/QRSpacePins/QRSpacePins.md)   | QR codes as external alignment markers.                  |

