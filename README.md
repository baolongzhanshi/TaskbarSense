# TaskbarSense

[![Release](https://img.shields.io/github/v/release/baolongzhanshi/TaskbarSense)](https://github.com/baolongzhanshi/TaskbarSense/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/baolongzhanshi/TaskbarSense/total.svg)](https://github.com/baolongzhanshi/TaskbarSense/releases)
[![License](http://img.shields.io/:license-MIT-blue.svg?style=flat)](LICENSE)

**TaskbarSense** 是面向 **Windows 11** 的轻量任务栏智能隐藏工具。

> 本项目基于 [ChanpleCai/SmartTaskbar](https://github.com/ChanpleCai/SmartTaskbar)（MIT）二次开发。仓库与产品名：**TaskbarSense**；工程目录仍为 `Sources/SmartTaskbar.Win11`，主程序为 **`TaskbarSense.exe`**。

仓库：https://github.com/baolongzhanshi/TaskbarSense

## 功能

- **Auto**：前台窗口与任务栏相交时自动隐藏
- **MaximizeHide**：同显示器存在最大化 / 无边框全屏窗口时隐藏任务栏
- 托盘菜单：动画、开机自启、退出后恢复任务栏
- 托盘 Tooltip 本地化显示当前模式
- 显示设置变化 / 解锁会话后自动刷新（UI 线程安全）
- 配置：`%LocalAppData%\TaskbarSense\settings.json`（会从旧路径自动迁移）

## 安装

| 安装包 | 体积 | .NET 8 |
|--------|------|--------|
| `TaskbarSense_Setup_2.1.4_Framework.exe` | 约 6 MB（推荐） | **需要** [Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0) |
| `TaskbarSense_Setup_2.1.4_SelfContained.exe` | 约 52 MB | **不需要** |

从 [Releases](https://github.com/baolongzhanshi/TaskbarSense/releases) 下载。系统要求：Windows 11（10.0.22000+）。

> 开机自启请在托盘菜单中开启（安装器不再单独创建启动项，避免重复启动）。

## 使用

右键托盘图标：

| 菜单 | 说明 |
|------|------|
| 智能模式 | Auto（与最大化隐藏互斥） |
| 最大化隐藏模式 | MaximizeHide |
| 开机自启 | 写入当前用户 Run 注册表（并清理旧启动项） |
| 退出后显示任务栏 | 退出时是否恢复普通任务栏 |
| 双击托盘 | 关闭智能模式并恢复普通任务栏（不会先闪菜单） |

## 构建

```powershell
dotnet build Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj -c Release
dotnet test Sources/SmartTaskbar.Win11.Tests/SmartTaskbar.Win11.Tests.csproj

dotnet publish Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj -c Release -r win-x64 --self-contained false -o publish-framework
dotnet publish Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj -c Release -r win-x64 --self-contained true -o publish-selfcontained
```

Inno Setup：`installer/SmartTaskbar.Win11.iss`

## 致谢与许可

- 原版：[ChanpleCai/SmartTaskbar](https://github.com/ChanpleCai/SmartTaskbar)
- 本项目：TaskbarSense / baolongzhanshi
- [MIT License](LICENSE)
