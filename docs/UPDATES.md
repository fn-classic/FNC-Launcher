# Application updates

The launcher, backend and native gameserver update as a single compatible release.
Fortnite game installations are not replaced. Each backend owner updates their own
installation; connecting to a remote backend never modifies its server files.

The launcher checks the latest published FNC-Launcher release at startup and every
15 minutes. It downloads in the background and shows a green download icon and
progress in the download menu. Automatic installation waits until Fortnite is
closed and Studio reports no connected or pending real players or client launches.
The Studio rechecks this condition before accepting shutdown. Unknown host state
blocks installation. Background-only Studio sessions also check automatically.
A standalone backend downloads updates, then waits until its owner stops services.

Downloads require HTTPS and the exact size and SHA-256 from the trusted release
manifest. The complete payload is staged and verified before stopping services.
Changed application files are backed up with a flushed journal; failure restores
the old files. An interrupted file installation is recovered before normal launcher
startup. Accounts, database data, user launcher settings and existing Studio/Openfire
configuration are retained. The launcher restarts after installation and brings its
services back normally. An error appears only if user attention is needed.

## Publish a release

1. Increment the launcher assembly version and build/package all three components.
2. Generate `update.json` with `tools/New-UpdateManifest.ps1 -Installer <setup.exe>
   -Version <assembly-version> -Tag <release-tag>`.
3. Upload both the installer and `update.json` to that tag's FNC-Launcher GitHub
   release and publish it as the latest stable release. Backend and gameserver
   source releases can use the same tag in their respective repositories.

The first updater-enabled installer must be distributed manually to users on older
builds. Their old launcher has no updater. Future bundled releases are automatic.
For this first upgrade, close Fortnite, select Open Studio > File > Exit to stop
the old background services, and close the launcher before running setup. Closing
the Studio window alone only hides it. Later automatic updates handle shutdown.
GitHub repositories/releases must be publicly downloadable; private release assets
need a public HTTPS distribution mirror. No GitHub token is embedded or requested.

Optional local settings go in `Launcher/update-settings.json`, following the example
in `config`. Setting `autoInstall` to false leaves updates in the menu for manual
installation; `autoDownload` false makes downloads manual. An empty `feedUrl`
disables checks. HTTP addresses are rejected. The default feed is
`https://github.com/fn-classic/FNC-Launcher/releases/latest/download/update.json`.
Only trusted publishers should control the configured feed: a checksum provides
integrity, while HTTPS and the publisher account provide distribution trust.

## Verification

`tests/Run-Updates.ps1` checks release validation, update file application, long
paths, account/settings preservation, rollback, recovery and Studio identity/idle
gates. `tests/Run-UpdateDownload.ps1` uses Python with `cryptography` for an isolated
HTTPS fixture; it tests real manifest/download requests plus corrupted/interrupted
downloads. Python is a developer test dependency, not an installed app dependency.
`tests/Run-UpdateApply.ps1` exercises an embedded full payload against an isolated
previous installation. The GameServer repository's `tests/Run-UpdateShutdown.ps1`
tests the live local Studio control endpoint and its shutdown refusal conditions.
