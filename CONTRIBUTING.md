# Contributing

## Contributing ideas

Suggestions for further samples, fixes for the current samples, clarification of documentation, or extension of the samples included are all very welcome. You are, of course, free and encouraged to develop your own samples as well. Be aware that there are restrictions on what samples can be hosted as part of this site, so if your sole intention is to put a sample here, it's best to check (by issuing a [proposal](https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/issues)) first before investing a lot of time.

But if you have made something you think others might find useful, we would be happy to have a look. See the guidelines below for a smoother experience for yourself and reviewers.

## Contributing samples

The most stringent requirement on samples is that they are well documented. Any scripts should be clear, with [docfx](https://dotnet.github.io/docfx/index.html) compliant comments. This generally just means following [C#'s XML code documentation practices](https://docs.microsoft.com/en-us/dotnet/csharp/codedoc), which many would say you should be doing anyway. 

Further, a detailed writeup should accompany the sample, as a markdown file in the sample's project root. For example, if your sample is in Advanced/NicelyDone, then you should have accompanying documentation in Advanced/NicelyDone/NicelyDone.md. Other supporting documentation you wish to include can be placed in the same folder as the markdown file. Images may be placed in a subfolder of DocGen/Images.

At a minimum, the documentation markdown file should include all of the information in [this template](DocGen/Documentation/SampleDocTemplate.md).

## Submitting a Pull Request

In general, working from a fork of the repo is preferable over just creating a feature branch. But we all try to remain flexible in our workflows.

Once you have a cleaned up working sample and appropriate documentation, you can submit a [pull request](https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/pulls) for it to be merged into the master branch. Any pull request will require review before acceptance. Depending on the complexity of your contribution, and the clarity of the code and documentation, as well as the current workload of potential reviewers, the time for review may vary widely. Every effort will be made to provide feedback within 48 hours of submission. If you've had no response within a week, it is perfectly reasonable to reach out to one of the administrators to check the status.

## Additional notes

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
