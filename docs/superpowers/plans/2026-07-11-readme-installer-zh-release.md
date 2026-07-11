# README / 中文安装包 / Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用中文 README 说明本仓库是 ChanpleCai/SmartTaskbar 的 Win11 二开，为安装包增加简体中文，并创建 GitHub Release `v2.0.0` 上传安装包。

**Architecture:** 文档与发布流程改造，不改应用核心逻辑。README 重写为中文；Inno Setup 引入仓库内中文语言文件并重打自包含 Setup；通过 git push + GitHub Release API/Desktop/`gh` 发布资产。

**Tech Stack:** Markdown, Inno Setup 6, .NET 8 self-contained publish, Git, GitHub Releases

**Spec:** `docs/superpowers/specs/2026-07-11-readme-installer-zh-release-design.md`

---

### Task 1: 重写中文 README

**Files:**
- Modify: `README.md`

- [ ] **Step 1: 用中文内容覆盖 `README.md`**

内容必须包含：
- 项目名 `SmartTaskbar.Win11`
- 二开来源 `ChanpleCai/SmartTaskbar`
- 本仓库链接
- MaximizeHide 功能
- 安装说明（自包含、无需 .NET 8）
- 使用 / 构建 / 已知限制 / 致谢与许可证

- [ ] **Step 2: 提交 README**

```powershell
git add README.md
git commit -m "docs: rewrite README in Chinese for SmartTaskbar.Win11 fork"
```

### Task 2: 安装包增加简体中文

**Files:**
- Create: `installer/languages/ChineseSimplified.isl`
- Modify: `installer/SmartTaskbar.Win11.iss`

- [ ] **Step 1: 获取简体中文语言包到仓库**

优先下载：
`https://raw.githubusercontent.com/jrsoftware/issrc/main/Files/Languages/Unofficial/ChineseSimplified.isl`

保存为：`installer/languages/ChineseSimplified.isl`

- [ ] **Step 2: 修改 iss 语言与中文任务文案**

```iss
[Languages]
Name: "chinesesimplified"; MessagesFile: "languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "开机自动启动"; GroupDescription: "启动选项:"; Flags: unchecked
```

- [ ] **Step 3: 重新自包含发布并编译安装包**

```powershell
dotnet publish Sources/SmartTaskbar.Win11/SmartTaskbar.Win11.csproj -c Release -r win-x64 --self-contained true -o publish-selfcontained
& 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' installer\SmartTaskbar.Win11.iss
```

Expected: 生成 `D:\Downloads\SmartTaskbar.Win11_Setup_2.0.0.exe`

- [ ] **Step 4: 同步安装包副本**

复制到：
- `D:\Desktop\SmartTaskbar.Win11_Setup_2.0.0.exe`
- `d:\Desktop\SmartTaskbar\SmartTaskbar.Win11_Setup_2.0.0.exe`

- [ ] **Step 5: 提交安装脚本与语言包**

```powershell
git add installer/
git commit -m "build(installer): add Simplified Chinese language support"
```

### Task 3: 推送并创建 Release

**Files:**
- None in repo for binary asset (installer stays untracked)

- [ ] **Step 1: 推送 master**

```powershell
git push origin master
```

- [ ] **Step 2: 创建 `v2.0.0` Release 并上传安装包**

优先 `gh release create`；若无 `gh`，用 GitHub API 或 GitHub Desktop 上传：
- Tag: `v2.0.0`
- Title: `SmartTaskbar.Win11 v2.0.0`
- Asset: `SmartTaskbar.Win11_Setup_2.0.0.exe`
- Notes: 中文更新说明 + 二开声明 + 安装说明

- [ ] **Step 3: 验证**

- Release 页面可访问
- 安装包可下载
- README 下载链接可用

---

## Spec Coverage Checklist

- [x] 中文 README + 二开声明 → Task 1
- [x] 安装包简体中文 → Task 2
- [x] Release 仅上传 Setup → Task 3
- [x] 构建产物不进 Git → Task 3 明确 untracked
