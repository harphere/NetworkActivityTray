# Network Activity Tray v0.5

A small Windows tray network-activity indicator inspired by PENM.

## Why v0.5 exists

v0.5 adds a GitHub-friendly, folder-based release model.

Instead of publishing a single bundled EXE, the app is published as a normal folder:

```text
NetworkActivityTray\
    NetworkActivityTray.exe
    NetworkActivityTray.dll
    *.dll
    *.json
    portable.txt
```

This is intentionally similar in spirit to other portable Windows utilities that distribute
a folder rather than a self-extracting single-file executable.

This does **not** guarantee that Microsoft Defender or an enterprise ASR policy will allow it.
It simply avoids relying on single-file bundling and provides a reproducible public build path.

## Portable mode

If `portable.txt` exists beside `NetworkActivityTray.exe`, settings and logs are kept in:

```text
NetworkActivityTray\Data\
```

Delete `portable.txt` to use the standard `%APPDATA%` / `%LOCALAPPDATA%` locations instead.

## Exact PENM icons

The application looks for:

```text
PENetwork_W10_GUI_Colored.icl
```

beside the executable.

The icon library is deliberately **not included** in this source package or GitHub release workflow.

If you already have the library and are permitted to use it, copy it beside
`NetworkActivityTray.exe` after extracting the release ZIP. This does not alter the EXE itself.

Resources used:

- 100 — white triangle
- 88 — send + receive
- 89 — send
- 90 — receive
- 91 — idle
- 80 — disconnected

Without the `.icl`, the built-in fallback icon renderer is used.

## Local portable build

From PowerShell:

```powershell
.\publish-portable.ps1
```

This creates:

```text
dist\NetworkActivityTray-0.5.0-win-x64-portable.zip
dist\NetworkActivityTray-0.5.0-win-x64-portable.zip.sha256
```

The publish is:

- Windows x64
- self-contained .NET 8
- folder-based
- not single-file
- not trimmed

## GitHub Actions release

The repository includes:

```text
.github\workflows\release.yml
```

To produce a public reproducible build:

1. Create a GitHub repository.
2. Push this project.
3. Confirm the Actions workflow succeeds.
4. Create and push a tag:

```powershell
git tag v0.5.0
git push origin v0.5.0
```

GitHub Actions will:

1. restore and build the project;
2. publish a folder-based self-contained Windows x64 build;
3. create `portable.txt`;
4. ZIP the complete application folder;
5. generate a SHA-256 checksum;
6. upload both as workflow artifacts;
7. attach them to the GitHub Release when triggered by a tag.

## Corporate-machine test

If your organization permits you to run unapproved portable utilities, test only the exact
artifact produced by GitHub Actions / GitHub Releases.

Do not rename, repack, patch, obfuscate, or otherwise alter the executable in an attempt to
evade Defender or organizational application-control policy.

If Defender ASR blocks it, stop there and use your organization's software approval process.

The fact that another unsigned utility runs on the same computer does not guarantee this
binary will satisfy Defender's reputation, age, prevalence, or trusted-list criteria.

## Existing features

- PENM-style send/receive tray indicator
- automatic/manual adapter selection
- activity thresholds
- configurable polling interval
- Start with Windows
- Test Icons submenu
- rotating logs
- verbose diagnostic logging
- portable mode
