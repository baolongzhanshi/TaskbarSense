# TaskbarSense

[![Release](https://img.shields.io/github/v/release/baolongzhanshi/TaskbarSense)](https://github.com/baolongzhanshi/TaskbarSense/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/baolongzhanshi/TaskbarSense/total.svg)](https://github.com/baolongzhanshi/TaskbarSense/releases)
[![License](http://img.shields.io/:license-MIT-blue.svg?style=flat)](LICENSE)

**TaskbarSense** — Windows 11 任务栏智能隐藏小工具（托盘运行）。

> 基于 [ChanpleCai/SmartTaskbar](https://github.com/ChanpleCai/SmartTaskbar)（MIT）二次开发。

**仓库：** https://github.com/baolongzhanshi/TaskbarSense

---

## 下载（不会选就看这里）

| 你的情况 | 下这个 |
|----------|--------|
| **大多数人 / 懒得装运行时** | **`TaskbarSense_Setup_2.2.0_SelfContained.exe`**（约 50MB+，推荐） |
| 已安装 .NET 8 桌面运行时，想要小包 | `TaskbarSense_Setup_2.2.0_Framework.exe`（约数 MB） |

- 系统：Windows 11（10.0.22000+）  
- 安装后：**没有主窗口**，请到右下角托盘（可能在 `^` 里）找图标  
- 卸载：开始菜单 → TaskbarSense → 卸载，或「设置 → 应用」  

[打开 Releases 下载](https://github.com/baolongzhanshi/TaskbarSense/releases)

---

## 怎么用

1. 安装并运行后，右下角出现托盘图标（首次会气泡提示）  
2. **右键图标**打开菜单：  
   - **靠近任务栏时自动隐藏**  
   - **窗口最大化时隐藏任务栏**  
   - **开机自动启动**  
   - **退出软件时恢复任务栏**  
3. **双击托盘图标** = 关闭智能隐藏并恢复普通任务栏  

配置：`%LocalAppData%\TaskbarSense\settings.json`

---

## 功能摘要

- 两种隐藏策略（互斥）  
- 开机自启、退出恢复任务栏  
- 托盘 Tooltip 显示当前状态  
- 显示设置变化 / 解锁后自动刷新  

---

## 构建（开发者）

```powershell
dotnet build Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj -c Release
dotnet test Sources/SmartTaskbar.Win11.Tests/SmartTaskbar.Win11.Tests.csproj

dotnet publish Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj -c Release -r win-x64 --self-contained false -o publish-framework
dotnet publish Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj -c Release -r win-x64 --self-contained true -o publish-selfcontained
```

Inno Setup：`installer/SmartTaskbar.Win11.iss`

---

## 致谢与许可

- 原版：[ChanpleCai/SmartTaskbar](https://github.com/ChanpleCai/SmartTaskbar)  
- 本项目：TaskbarSense / baolongzhanshi  
- [MIT License](LICENSE)
