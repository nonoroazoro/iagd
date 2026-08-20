# IAGD Fork

[English](README.md) | [简体中文](README.zh-CN.md)

> This repository is a heavily modified fork of [Grim Dawn Item Assistant](https://github.com/marius00/iagd). Its architecture, behavior, packaging, and product direction have diverged so far that it can no longer be merged back into upstream. It is now maintained as a fully independent project.

A portable Grim Dawn item manager focused on localized game data, accurate duplicate handling, item comparison, and GrimTools build searches.

## Features

- English and Simplified Chinese UI.
- Game item names and stats follow the selected Grim Dawn data language independently of the UI language, with English fallback.
- Manual game database loading. Startup never parses or replaces game data automatically.
- GrimTools build URL and ID search resolved directly against the local Item Assistant database, without `gd-cli`.
- Duplicate grouping by canonical base item, including duplicate filtering and quantity sorting.
- Item comparison with deterministic max-roll values and property ranking.
- Transfer refresh, exact transfer notifications, and persistent mod-filter warning dismissal.
- Portable `UserData` stored beside the application.
- Independent release lifecycle with the original upstream auto-updater removed.

Existing Item Assistant databases and backups remain compatible with this fork. Data written by newer versions may not be backward-compatible with older upstream releases.

## Build

Windows is required. See [BUILDING.md](BUILDING.md) for prerequisites and options.

```powershell
.\build-package.ps1
```

Runnable files are written directly to `artifacts`.

## Credits

Based on the original [Grim Dawn Item Assistant](https://github.com/marius00/iagd). Credit and copyright for the original work remain with its authors and contributors.

See [LICENSE](LICENSE) for license details.
