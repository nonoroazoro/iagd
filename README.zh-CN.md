# IAGD Fork

[English](README.md) | [简体中文](README.zh-CN.md)

> 本项目是基于 [Grim Dawn Item Assistant](https://github.com/marius00/iagd) 深度魔改的 fork。架构、行为、打包方式和产品方向均已大幅分叉，无法再合入 upstream，现已作为完全独立的项目维护。

这是一个便携式《Grim Dawn》物品管理工具，主要改进了游戏数据本地化、重复装备处理、装备对比和 GrimTools build 搜索。

## 功能

- UI 支持 English 和简体中文。
- 装备名称与属性使用所选的 Grim Dawn 游戏数据语言，与 UI 语言互相独立；缺少对应数据时 fallback 到 English。
- 游戏数据库由用户手动加载，启动时不会自动解析或替换数据。
- 可通过 GrimTools build URL 或 ID 搜索装备，直接查询本地 Item Assistant game records。
- 按装备基底统一识别重复装备，并支持重复装备过滤和数量排序。
- 支持装备对比、确定性的 max-roll 数值计算和属性排序。
- 转移后自动刷新，通知准确显示物品名称和数量，并可永久关闭 Mod filter 提醒。
- `UserData` 存放在程序旁边，便于便携使用。
- 已删除原版自动更新功能，独立管理版本和发布。

原版 Item Assistant 的现有数据库和备份可以继续在本 fork 中使用。新版写入的数据不保证能被更老的 upstream 版本读取。

## Build

仅支持 Windows。环境要求和可选参数见 [BUILDING.md](BUILDING.md)。

```powershell
.\build-package.ps1
```

可运行文件会直接输出到 `artifacts`。

## 相关项目

- [gd-cli](https://github.com/nonoroazoro/gd-cli)：我维护的、面向 AI agent 的 Grim Dawn 游戏数据 CLI。

## 致谢

本项目基于原版 [Grim Dawn Item Assistant](https://github.com/marius00/iagd)。原始工作的署名与版权归原作者和贡献者所有。

许可信息见 [LICENSE](LICENSE)。
