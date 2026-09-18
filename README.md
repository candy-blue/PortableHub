<div align="center">

<img src="PortableHub.App/Assets/app.png" alt="Portable Hub Logo" width="128" height="128" />

# Portable Hub

**Windows 平台现代化轻量级便携软件启动与管理中心**

让散落在硬盘、移动硬盘、U 盘与下载目录中的大量绿色/便携（Portable）软件井井有条，一键分类、毫秒搜索、极速启动。

[![GitHub release](https://img.shields.io/github/v/release/candy-blue/PortableHub?color=0078D6&logo=github)](https://github.com/candy-blue/PortableHub/releases/latest)
![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?logo=windows&logoColor=white)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?logo=dotnet&logoColor=white)
![UI](https://img.shields.io/badge/UI-iNKORE.UI.WPF.Modern%20(WinUI%203)-blue)
![Database](https://img.shields.io/badge/Database-SQLite-003B57?logo=sqlite&logoColor=white)
![Tests](https://img.shields.io/badge/Tests-189%20Passed-success)
![License](https://img.shields.io/badge/License-MIT-green)

[✨ 核心特性](#-核心特性) • [📦 下载安装](#-下载与安装) • [🚀 编译构建](#-编译与构建) • [🏗️ 技术架构](#-技术架构) • [📄 开源许可](#-开源许可)

</div>

---

## 📦 下载与安装

请前往 [GitHub Releases](https://github.com/candy-blue/PortableHub/releases/latest) 下载最新发布的构建版本：

| 发布版本 | 文件名 | 说明 | 运行需求 |
| :--- | :--- | :--- | :--- |
| **独立单文件版** (推荐) | [`PortableHub.exe`](https://github.com/candy-blue/PortableHub/releases/latest) | 单文件便携版，内嵌完整运行时，双击直接运行 | 仅需 64 位 Windows 10/11 |
| **标准便携压缩包** | [`PortableHub-Portable.zip`](https://github.com/candy-blue/PortableHub/releases/latest) | 包含完整依赖与免安装绿色目录，解压即可使用 | 仅需 64 位 Windows 10/11 |

> [!TIP]
> Portable Hub 是完全绿色纯净的软件，解压后即可放在任何目录（支持 U 盘或移动硬盘随身携带），所有数据默认保存在本地 `data/` 目录中。

---

## ✨ 核心特性

- 🎨 **微软 WinUI 3 & Fluent 2 规范设计**
  - 基于 iNKORE.UI.WPF.Modern 打造原生 Windows 11 视觉质感；
  - 深度支持 Mica 磨砂材质、深色/浅色/跟随系统主题自适应；
  - 沉浸式标题栏设计，消除原生窗口控件冗余，微动效流畅优雅。

- 🌐 **完备的多语言国际化支持 (I18n)**
  - 原生内置简体中文（zh-CN）、繁体中文（zh-TW）、英语（en-US）；
  - 支持设置中即时热切换，所有视图、对话框与动态提示 100% 对应。

- ⚡ **全局快捷键与毫秒级快速启动器**
  - 默认快捷键 `Ctrl+Alt+Space`（支持在设置中自定义），瞬间于当前光标所在屏幕正中呼出；
  - 内存级全文模糊评分与拼音匹配，按上下键选择或直接回车即刻启动。

- 📂 **智能文件嗅探与快捷方式解析**
  - 支持直接拖入 `.exe`、`.lnk`、`.bat`/`.cmd` 或软件所属目录；
  - 自动穿透快捷方式解析底层物理真实路径，智能识别核心主程序并高保真提取图标。

- 🏷️ **灵活的分类管理与可视化排序**
  - 支持拖拽卡片或分类直接排序，实时防抖持久化；
  - 内置 36 款 Fluent 矢量图标选择器与专业调色板（支持 Windows 系统取色器与 Hex 自定义）。

- 🛡️ **纯净管理与零侵入原则**
  - 管理器与目标便携软件彻底解耦，绝不修改、迁移或污染目标程序及其运行目录；
  - 从管理器移除仅删除启动索引记录，绝不误伤物理文件。

- 💾 **离线 SQLite 存储与安全备份机制**
  - 采用轻量级嵌入式 SQLite 数据库管理软件元数据，自带版本迁移器；
  - 支持一键导出 `.phbackup` 本地快照及全量恢复，并提供通用 JSON 格式导入/导出。

- 🔔 **系统托盘与开机静默常驻**
  - 支持关闭/最小化自动缩入 Windows 系统托盘；
  - 支持开机自启静默常驻后台，单实例互斥唤醒。

---

## 🏗️ 技术架构

```text
PortableHub
├── PortableHub.App            # WPF 前端应用（基于 iNKORE.UI.WPF.Modern 官方 Fluent 2 控件库）
│   ├── Assets                # 应用程序高清矢量与位图资产 (app.ico, app.png)
│   ├── Views                 # 视图层 (MainWindow, QuickLauncher, Scanner, Settings, Dialogs)
│   ├── ViewModels            # MVVM 视图模型（CommunityToolkit.Mvvm）
│   ├── Converters            # XAML 值转换器
│   ├── Helpers               # 辅助工具（TitleBarHelper, SymbolHelper 等）
│   ├── Themes                # Fluent 设计样式、控件模板与多语言字典 (Strings.xx-XX.xaml)
│   └── Services              # 前端专有服务 (Theme, Tray, Localization, SingleInstance)
├── PortableHub.Core           # 核心领域模型与接口契约
│   ├── Models                # Software, Category, RootDirectory, AppSettings
│   ├── Enums                 # 枚举定义
│   └── Interfaces            # 仓储与服务契约规范
├── PortableHub.Infrastructure # 基础设施与底层实现
│   ├── Data                  # SQLite 连接工厂与版本化数据库迁移器 (DatabaseMigrator)
│   ├── Repositories          # Dapper 高性能仓储实现
│   ├── Services              # 启动、图标提取、备份、热键、更新与自动路径修复服务
│   └── Windows               # Windows 原生 Win32 API 交互与快捷方式解析
└── PortableHub.Tests          # 自动化单元测试与 UI 测试套件（189+ 项全量覆盖，100% 通过）
```

---

## 🚀 编译与构建

### 运行环境需求
- Windows 10 (1809+) 或 Windows 11 (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 源码编译步骤

1. **克隆仓库**
   ```bash
   git clone https://github.com/candy-blue/PortableHub.git
   cd PortableHub
   ```

2. **恢复依赖并编译**
   ```bash
   dotnet build
   ```

3. **运行全量自动化测试套件**
   ```bash
   dotnet test
   ```

4. **打包发布**
   - **独立单文件版 (包含完整运行时，约 73MB)**：
     ```bash
     dotnet publish PortableHub.App/PortableHub.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o release/singlefile
     ```
   - **轻量框架依赖版 (需目标机预装 .NET 8 桌面运行时，约 12MB)**：
     ```bash
     dotnet publish PortableHub.App/PortableHub.App.csproj -c Release -r win-x64 --self-contained false -o release/lightweight
     ```

---

## 📄 开源许可

本项目基于 [MIT License](LICENSE) 开源协议发布。
