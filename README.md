# ClassicFN Launcher

Authentication, game launching, account UI, installation, settings and the historical launcher presentation. This repository builds without the Game Server or Backend source checkouts.

## Structure

- `src/UI`: forms and painting/layout code.
- `src/Application`: application entry point and session controller.
- `src/Services`: accounts, connection handling, verification and installation coordination.
- `src/Installation`: Windows setup implementation.
- `assets`: existing launcher artwork and icons.
- `installer`: native prerequisite bootstrap and installation scripts.
- `config`: safe examples; actual settings are written to the installation at runtime.
- `vendor`: checksum-locked contract SDK package, produced by `Backend/sdk`.

## Build

On Windows 10/11 with .NET Framework 4.8, run `powershell -File Build.ps1`. The result is `build/Launcher/FNClassicLauncher.exe`. No sibling repository paths are read. The SDK source is restored locally from the pinned package and compiled into the executable; no additional SDK DLL is needed at runtime.

To build a distribution that can install the complete product, pass `-Payload <verified application zip>`. This explicit artifact contains Backend and Game Server release outputs; it is not a source dependency. Bundled Microsoft prerequisites and native bootstrap are a separate release-composition step. A development executable without a payload connects to an existing complete installation and cannot install missing components itself.

Start the executable with `--studio-root <installation directory>` to use a specific deployment. Preserve the established deployed filenames, protocol `FSS.STUDIO/1`, registry identity and data paths so existing accounts/settings remain compatible.

## Configuration and data

Private sessions, credentials, backend configuration, logs and account data are ignored by Git. `config/match-settings.example.xml` contains defaults only. Set `CLASSICFN_LEGACY_ROOT` explicitly when importing an older installation. Never put user credentials or game installations in this repository.

## Current verification scope

Phase 2 adds persisted installation selection for 1.7.2, 1.8, 1.8.2 and 3.5, executable changelist validation, starter resolution, and the supplied 2017 artwork. Version changes are blocked during gameplay and installation/verification operations. The three 2017 builds share the supplied historical reference; no separate Halloween artwork has been invented. Launcher friends are now implemented against the shared Backend records and TCP presence; actual game/external-PC tests and final clean-install verification remain pending. Run `tests/VersionSelection.ps1` with the four installed executable paths to verify identity, persistence, mismatched build rejection and gameplay locking and generate UI captures. Actual native host creation and reuse passed for all four supported builds. Local native client connections have passed on the three legacy builds; a 3.5 client also passed a loading-screen dismissal check. External multiplayer and clean-Windows installer tests remain unverified. Complete releases require pipeline 14; do not deploy this executable over an older Studio without the matching backend/game-server release.

## GitHub upload

This folder is the repository root. Commit the source, assets, examples, lockfile and `vendor` SDK package; do not upload `build`, release installers, runtimes or saved accounts. The SDK package is a required pinned build dependency, not a generated application release. Start with `Build.ps1` to verify a fresh checkout.

The Friends panel uses the existing backend friendship and presence records. It opens beneath the header as an owned, modeless panel. The Launch button uses the supplied reference colors, dimensions and gear asset.
