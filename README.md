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

### Configuration

Place a `.eyepatch.settings` JSON file in the root of user directory (%USERPROFILE%). This file is optional.

#### JSON Settings Reference

- **DiffApp**
    Type: `string`
    Default: `"windiff"`
    Program to be invoked for diff-ing files. It is expected that windiff or whatever tool is called is installed and on the path. The
    tool will be called with a text file containing a pair of original and modified file paths, a pair per line, such as:

```sh
a b
c d
```

- **PatchDirectory**
    Type: `string`
    Default: %ONEDRIVE%\\patches
    Absolute path to where patches should be saved. If not set, the tool will create (if it does not exist) a `patches` directory under %ONEDRIVE%.

#### Example `.eyepatch.settings`

```json
{
    "DiffApp": "joediff",
    "PatchDirectory": "C:\\patches_go_here"
}
```

### Usage

#### Save Patch

Save the resultant committed changes in a branch since it forked, plus any current changes (staged or not), producing a single patch.

Optionally specify a name for the patch file. If not file name is specified, the branch name will be used, choosing the string after `\`. This is meant to simplify the common pattern of naming branches `user\\alias\\name`. A time stamp with year, month, day, hours, minutes, seconds and milliseconds, using the `yyyyMMdd-HHmmss-fff` format is appended to provide uniqueness. Example patch file: `Feature1.20250409-194108-667.patch`.

Should be executed in the context of a branch being checked out.

```sh
eyepatch save [optional-patch-name]
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

### Status

Show the currently changes, added or removed files and identifies if they will conflicts with newer commits.

Should be executed in the context of a branch being checked out.

```sh
eyepatch status
```

#### Help

```sh
eyepatch
```

## Contributing

Contributions are welcome! Please open issues or submit pull requests for bug fixes, features, or documentation improvements.

## License

EyePatch is licensed under the MIT License. See [LICENSE](LICENSE) for details.
