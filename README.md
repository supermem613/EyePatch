# EyePatch

EyePatch is a command-line tool designed to simplify the management of git patches derived from branches. It streamlines the process of creating, applying, and organizing patches, making it easier for teams and individuals to maintain clean and reproducible code changes across different environments or repositories.

Specifically, it is meant to supplement general git usage of branches to allow visualizing and managing commits over time for the branch and understanding the overall payload that a branch contains.

It is built specifically with Windows 10 and newer versions in mind.

## Features

- Generate patches from any branch, from its creation to tip
- Produces visual diffs (using windiff or similar tools) for patches
- Visualizes diffs for all changes in the branch, from its creation to tip
- Integrates with standard git workflows
- Simple CLI interface

## Getting Started

### Prerequisites

- [Git](https://git-scm.com/) (version 2.20 or higher recommended)
- [WinDiff](https://learn.microsoft.com/en-us/sysinternals/downloads/windiff) (for visual diffs) or similar tool

### Installation

#### Clone the Repository

```sh
git clone https://github.com/yourusername/EyePatch.git
cd EyePatch
```

#### Restore and Build

```sh
dotnet restore
dotnet publish
```

### Usage

#### Save Patch

Save the resultant committed changes in a branch since it forked, plus any current changes (staged or not), producing a single patch. Optionally specify a name for the patch file.

Should be executed in the context of a branch being checked out.

```sh
eyepatch save [patch-name]
```

### Diff Branch

Show the differences of the resultant committed changes in a branch since it forked, plus any current changes (staged or not).

Should be executed in the context of a branch being checked out.

```sh
eyepatch diff
```

### View Patch

Show the differences of a given patch file against its base commit. Requires a patch-file argument.

Should be executed in the context of a branch being checked out.

```sh
eyepatch view <patch-file>
```

#### Help

```sh
eyepatch
```

## Contributing

Contributions are welcome! Please open issues or submit pull requests for bug fixes, features, or documentation improvements.

## License

EyePatch is licensed under the MIT License. See [LICENSE](LICENSE) for details.
