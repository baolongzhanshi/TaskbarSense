# SmartTaskbar.Win11

[![Release](https://img.shields.io/github/v/release/baolongzhanshi/SmartTaskbar.Win11)](https://github.com/baolongzhanshi/SmartTaskbar.Win11/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/baolongzhanshi/SmartTaskbar.Win11/total.svg)](https://github.com/baolongzhanshi/SmartTaskbar.Win11/releases)
[![License](http://img.shields.io/:license-MIT-blue.svg?style=flat)](LICENSE)

**SmartTaskbar.Win11** 是一个面向 **Windows 11** 的轻量级任务栏自动隐藏工具。

> 本项目是 [ChanpleCai/SmartTaskbar](https://github.com/ChanpleCai/SmartTaskbar) 的二次开发版本（MIT 协议），在原版能力基础上增加了 Windows 11 适配与「最大化隐藏」模式。

仓库地址：https://github.com/baolongzhanshi/SmartTaskbar.Win11

## 与原版的关系

| 项目 | 说明 |
|------|------|
| 原版项目 | [ChanpleCai/SmartTaskbar](https://github.com/ChanpleCai/SmartTaskbar) |
| 本仓库 | [baolongzhanshi/SmartTaskbar.Win11](https://github.com/baolongzhanshi/SmartTaskbar.Win11) |
| 主要程序 | `Sources/SmartTaskbar.Win11` |
| 目标框架 | .NET 8 / Windows 11 |
| 新增能力 | MaximizeHide（同显示器存在最大化窗口时自动隐藏任务栏） |

原版代码仍保留在仓库中，便于对照；日常使用请安装本仓库发布的 **SmartTaskbar.Win11** 安装包。

## 功能特性

### Auto 模式（继承原版）

- 根据前台窗口与任务栏是否相交，自动切换任务栏显示 / 隐藏
- 双击托盘图标可快速切换任务栏显示状态

### MaximizeHide 模式（二开新增）

- 检测**同一显示器**上是否存在最大化窗口
- 有最大化窗口时自动隐藏任务栏
- 无最大化窗口时恢复显示
- 适合需要更接近「全屏沉浸」体验的 Windows 11 使用场景

### 其他

- 系统托盘菜单（动画开关、退出时显示任务栏、关于、退出）
- Windows 11 任务栏居中 / 左对齐检测
- 多显示器菜单定位改进
- 配置保存在本地 `%LocalAppData%\SmartTaskbar.Win11\settings.json`
- 安装包**自包含**，目标电脑无需预装 .NET 8 Desktop Runtime

## 安装

1. 打开 [Releases](https://github.com/baolongzhanshi/SmartTaskbar.Win11/releases)
2. 下载 `SmartTaskbar.Win11_Setup_2.0.0.exe`
3. 双击安装并启动

**系统要求：** Windows 11（10.0.22000 及以上）

安装后程序运行在系统托盘（通知区域）。如果看不到图标，请检查托盘溢出区。

## 使用说明

右键托盘图标：

| 菜单项 | 作用 |
|--------|------|
| 关于 | 打开本项目 GitHub 页面 |
| 动画 | 开关任务栏动画 |
| 自动模式 | 启用 / 关闭 Auto 模式 |
| 最大化隐藏模式 | 启用 / 关闭 MaximizeHide 模式 |
| 退出后显示任务栏 | 退出程序时是否恢复任务栏显示 |
| 退出 | 退出程序 |

说明：

- Auto 与 MaximizeHide 为互斥模式，同一时间只会启用一种
- 双击托盘图标会关闭自动模式，并切换任务栏显示状态

## 构建

### 环境

- Visual Studio 2022 或 .NET 8 SDK
- Windows 11 开发环境

### 命令行构建

```powershell
dotnet build Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj -c Release
dotnet test Sources/SmartTaskbar.Win11.Tests/SmartTaskbar.Win11.Tests.csproj
```

### 自包含发布

```powershell
dotnet publish Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj `
  -c Release -r win-x64 --self-contained true `
  -o publish-selfcontained
```

### 安装包

使用 Inno Setup 6 编译：

```text
installer/SmartTaskbar.Win11.iss
```

## 已知限制

- 主任务栏不在主显示器时，部分逻辑可能表现异常（继承自原版实现）
- 某些应用使用特殊最大化逻辑时，任务栏可能不会按预期弹出；可尝试 `Win + T`
- Auto-Hide 相关行为会受到 Windows 系统本身规则影响

## 致谢

- 原作者与原项目：[ChanpleCai/SmartTaskbar](https://github.com/ChanpleCai/SmartTaskbar)
- 本仓库在原版基础上完成 Windows 11 适配、MaximizeHide 模式、.NET 8 迁移与安装打包

## 许可证

本项目遵循 [MIT License](LICENSE)。  
二次开发请保留原作者版权声明，并遵守 MIT 许可要求。
