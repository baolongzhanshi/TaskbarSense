# SmartTaskbar.Win11 文档、安装包中文与 Release 设计

**日期：** 2026-07-11  
**仓库：** https://github.com/baolongzhanshi/SmartTaskbar.Win11  
**状态：** 已确认（方案 A）

## 1. 目标

在不改变核心功能的前提下，完成三件事：

1. 将 `README.md` 改写为面向本仓库的**中文说明**，并明确二开来源。
2. 为 Inno Setup 安装包增加**简体中文**语言支持。
3. 创建 GitHub Release **`v2.0.0`**，仅上传自包含安装包。

## 2. 范围

### 包含

- 重写根目录 `README.md`（中文）
- 安装脚本 `installer/SmartTaskbar.Win11.iss` 增加简体中文
- 引入简体中文语言文件 `installer/languages/ChineseSimplified.isl`
- 重新编译安装包 `SmartTaskbar.Win11_Setup_2.0.0.exe`
- 提交并推送到 `origin/master`
- 创建 Release `v2.0.0` 并上传安装包

### 不包含

- 不新增应用功能
- 不上传便携 zip / publish 目录
- 不改原版 `Sources/SmartTaskbar` 业务逻辑
- 不创建英文 README（本次仅中文）

## 3. README 设计

### 3.1 定位声明（必须）

README 顶部明确：

- 本项目名称：`SmartTaskbar.Win11`
- 本仓库：`https://github.com/baolongzhanshi/SmartTaskbar.Win11`
- 二开来源：基于 [ChanpleCai/SmartTaskbar](https://github.com/ChanpleCai/SmartTaskbar)（MIT）二次开发
- 目标平台：Windows 11

### 3.2 推荐章节结构

1. 标题与徽章（指向本仓库 Release）
2. 项目简介
3. 与原版的关系 / 二开说明
4. 主要功能
   - Auto 模式（继承）
   - MaximizeHide 最大化隐藏（新增）
   - 托盘菜单、动画开关、退出显示任务栏
5. 安装
   - 下载 `SmartTaskbar.Win11_Setup_2.0.0.exe`
   - 自包含，无需预装 .NET 8 Desktop Runtime
   - 系统要求：Windows 11（10.0.22000+）
6. 使用说明（托盘右键菜单）
7. 构建方式（.NET 8 SDK / Visual Studio 2022）
8. 已知限制
9. 致谢与许可证

### 3.3 链接规则

- 下载与徽章：本仓库 `baolongzhanshi/SmartTaskbar.Win11`
- 原版致谢：保留指向 `ChanpleCai/SmartTaskbar`
- 关于页跳转（应用内）：已为 `https://github.com/baolongzhanshi/SmartTaskbar.Win11`（无需在本任务中改代码，除非发现遗漏）

## 4. 安装包中文设计

### 4.1 语言文件

- 路径：`installer/languages/ChineseSimplified.isl`
- 来源：Inno Setup 官方仓库 Unofficial 简体中文语言包
- 安装脚本引用该相对路径，避免依赖本机 Inno 安装目录是否自带中文

### 4.2 脚本变更

文件：`installer/SmartTaskbar.Win11.iss`

```
[Languages]
Name: "chinesesimplified"; MessagesFile: "languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
```

- 中文放在第一位，安装向导默认优先中文
- 自定义任务文案改为中文，例如：
  - `开机自动启动`
  - `启动选项`
- 保留英文语言项，用户可切换

### 4.3 重新打包

1. 确认自包含发布目录 `publish-selfcontained` 可用；若过期则重新 `dotnet publish`
2. 用 Inno Setup 编译生成：
   - `D:\Downloads\SmartTaskbar.Win11_Setup_2.0.0.exe`
3. 同步复制到：
   - 桌面
   - 项目根目录（便于上传）

## 5. GitHub Release 设计

### 5.1 元数据

| 字段 | 值 |
|------|----|
| Tag | `v2.0.0` |
| 标题 | `SmartTaskbar.Win11 v2.0.0` |
| 目标分支 | `master` |
| 资产 | 仅 `SmartTaskbar.Win11_Setup_2.0.0.exe` |

### 5.2 Release 正文（中文）

应包含：

- 这是 ChanpleCai/SmartTaskbar 的 Windows 11 二开版本
- 新特性：MaximizeHide（最大化隐藏）
- 安装说明：自包含，无需 .NET 8 Runtime
- 系统要求：Windows 11
- 使用提示：安装后看系统托盘图标

### 5.3 发布方式

优先顺序：

1. 若本机可用 `gh`：`gh release create v2.0.0 ...`
2. 否则使用 GitHub Desktop / 浏览器创建 Release 并上传资产
3. 或使用 GitHub API + 本地凭据上传

无论哪种方式，最终都要验证：

- Release 页面可访问
- 安装包可下载
- README 中的下载链接指向该 Release

## 6. 提交与推送

建议提交拆分（也可合并为一个提交）：

1. `docs: rewrite README for SmartTaskbar.Win11 Chinese fork notice`
2. `build(installer): add Simplified Chinese language pack`
3. 推送 `master`
4. 创建 Release 上传安装包（安装包本身不进 Git）

安装包、`publish-selfcontained/`、本地 zip 等构建产物继续保持 untracked。

## 7. 验收标准

- [ ] `README.md` 为中文，并明确二开来源与本仓库地址
- [ ] 安装向导可选/默认简体中文
- [ ] 重新生成的 Setup 文件名仍为 `SmartTaskbar.Win11_Setup_2.0.0.exe`
- [ ] GitHub 存在 `v2.0.0` Release
- [ ] Release 仅包含安装包资产
- [ ] 变更已推送到 `origin/master`

## 8. 风险与处理

| 风险 | 处理 |
|------|------|
| 本机无 `gh` CLI | 回退到 GitHub Desktop 或浏览器上传 Release |
| 中文语言包缺失 | 从 Inno 官方 Unofficial 语言包获取并放入仓库 |
| 安装包路径被占用 | 先结束运行中的 `SmartTaskbar.Win11` / 安装程序再覆盖 |
| README 徽章链接失效 | Release 创建后再核对徽章 URL |

## 9. 非目标澄清

- 不把原版 1.4.5 安装包当作本项目产物
- 不在 README 中继续引导下载 `ChanpleCai/SmartTaskbar` 的 Setup 作为本项目安装方式
- 不强制重写应用内 UI 文案（应用已有中文资源）
