# WinCasc

Mount Blizzard CASC storages as read-only Windows drives. Any app can then read game files with normal file APIs.

## Install

Download the setup from [Releases](../../releases) and run it. It installs WinFsp if needed.

## Use

Right-click a game folder (the one with `.build.info`) → **Show more options** → **Mount CASC storage**.

Right-click the mounted drive → **Show more options** → **Unmount CASC storage**.

Command line:

```
WinCasc mount <game folder> [drive letter or empty folder] [--listfile <file>]
WinCasc unmount <mount point | --all>
WinCasc register | unregister
```

`--listfile` is only needed for storages without file names (e.g. WoW).

## Build

Requires .NET 10 SDK, CMake, Visual Studio C++ tools, WinFsp and Inno Setup 6.

```
git clone --recursive https://github.com/Scobalula/WinCasc
.\installer\build.ps1
```

The installer ends up in `artifacts\installer`.

## License

WinCasc is licensed under GPLv3, see [LICENSE](LICENSE).

- WinFsp - Windows File System Proxy, Copyright (C) Bill Zissimopoulos - https://github.com/winfsp/winfsp (GPLv3 with FLOSS exception)
- CascLib, Copyright (c) 2014 Ladislav Zezula - https://github.com/ladislav-zezula/CascLib (MIT)

Full texts in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).
