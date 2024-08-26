# Contributing to Files
Files thrives off of the contributions and input from users. There are several ways you can get involved.

We are happy to hear your feedback for the future of Files. Check the
[issues tab](https://github.com/files-community/Files/issues) to see if others have
submitted similar feedback. You can add your feedback to existing issues or you can open a new issue.

We always look at your feedback when we decide what to work on next and we look forward to hearing your ideas. Remember that
all community interactions must abide by the [Code of Conduct](./CODE_OF_CONDUCT.md).

## Finding issues you can help with
Looking for something to work on?
Issues marked as ready to build are a good place to start. If you're interested in working on an issue, leave a comment to let everyone know and to help
avoid duplicated effort from others.

## Contributions we accept
We welcome your contributions to the Files project, especially to fix bugs and to make
improvements which address the top issues reported by Files users. Some general guidelines:

* **DO** create one pull request per Issue, and ensure that the Issue is linked in the pull request.
* **DO** follow our [Coding and Style](https://files.community/docs/contributing/code-style) guidelines, and keep code changes as small as possible.
* **DO** include corresponding tests whenever possible.
* **DO** check for additional occurrences of the same problem in other parts of the codebase before submitting your PR.
* **DO** [link the issue](https://docs.github.com/en/github/managing-your-work-on-github/linking-a-pull-request-to-an-issue#manually-linking-a-pull-request-to-an-issue) you are addressing in the 
   pull request.
* **DO** write a good description for your pull request. More detail is better. Describe *why* the change is being 
   made and *why* you have chosen a particular solution. Describe any manual testing you performed to validate your change.
* **DO NOT** submit a PR unless it the connected issue is marked as ready to build or approved indirectly by an org member. 
   This enables us to have a discussion on the idea before anyone invests time on the implementation.
* **DO NOT** merge multiple changes into one PR unless they have the same root cause.

> Submitting a pull request for an approved Issue is not a guarantee it will be approved.
> The change must meet our high bar for code quality, architecture, and performance.

## Making changes to the code

If your change is complex, please clean up the branch history before submitting a pull request.
You can use [git rebase](https://docs.microsoft.com/en-us/azure/devops/repos/git/rebase#squash-local-commits)
to group your changes into a small number of commits which we can review one at a time.

When completing a pull request, we will generally squash your changes into a single commit. Please
let us know if your pull request needs to be merged as separate commits.

## Running interaction tests (optional)
First, you will need to install WinAppDriver. You can download the latest version from [here](https://github.com/microsoft/WinAppDriver/releases).
After installing WinAppDriver, you will need to run the Files.Package at least once to have the debug version be registered on your machine.
Once you have the debug version registered, you can run the tests from the test explorer in Visual Studio.

## Review Process
After submitting a pull request, members of the Files team will review your code. Any member of the community may
participate in the review, but at least one member of the Files team will ultimately approve
the request.

Often, multiple iterations will be needed to responding to feedback from reviewers. Try looking at
[past pull requests](https://github.com/files-community/Files/pulls?q=is%3Apr+is%3Aclosed) to see
what the experience might be like.
