# Portable Hub

<div align="center">

![Windows 11](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?logo=windows&logoColor=white)
![.NET 8.0](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?logo=dotnet&logoColor=white)
![UI](https://img.shields.io/badge/UI-WPF--UI%20(WinUI%203)-blue)
![Database](https://img.shields.io/badge/Database-SQLite-003B57?logo=sqlite&logoColor=white)
![Tests](https://img.shields.io/badge/Tests-61%20Passed-success)
![License](https://img.shields.io/badge/License-MIT-green)

**Windows 平台轻量级便携软件启动与管理中心**

让散落在硬盘、移动硬盘、U 盘与下载目录中的大量 Portable 软件井井有条，一键分类、毫秒搜索、极速启动。

</div>

---

## ✨ 核心特性

- 🎨 **微软 WinUI 3 官方规范设计 (WPF-UI)**
  - 支持 Windows 11 Mica 磨砂材质与暗色/亮色/跟随系统主题自适应；
  - 深度优化字体渲染与像素级图文对齐。

- ⚡ **全局快捷键与毫秒级快速启动**
  - 按下 `Ctrl+Alt+Space`（支持自定义）瞬间在当前鼠标屏幕中央呼出搜索窗口；
  - 内存级全文过滤 Top 10，上下键选择或 `Enter` 即刻启动软件。

- 📂 **智能软件拖入与解析**
  - 支持从桌面或资源管理器直接拖入 `.exe`、`.lnk` 快捷方式、`.bat`/`.cmd` 或软件主文件夹；
  - 自动解析快捷方式真实物理目标，智能嗅探主程序并自动抓取高清图标。

- 🏷️ **分类体系与拖拽排序**
  - 纯净分类管理，支持鼠标直接拖拽分类上下调整顺序，实时防抖持久化；
  - 内置 36 款官方 Fluent 矢量图标选择器与专业调色板（支持 Windows 系统取色器与 Hex 拾色）。

- 🛡️ **纯净管理与零破坏原则**
  - 管理器与便携软件本体彻底解耦，绝不修改、迁移或破坏软件自身目录与配置文件；
  - 从管理器移除仅删除索引记录，绝不误删任何物理文件。

- 💾 **离线本地数据快照与备份**
  - 基于轻量级 SQLite 数据库保存元数据；
  - 支持一键导出 `.phbackup` 本地快照及全量恢复，支持导出/导入通用 JSON 数据。

- 🔔 **原生托盘与开机静默常驻**
  - 点击关闭/最小化自动收起至系统通知托盘；
  - 支持开机自启静默常驻，单实例互斥唤醒。

---

## 🏗️ 技术架构

```text
PortableHub
├── PortableHub.App            # WPF 前端应用（基于 lepoco/wpfui 官方 WinUI 3 控件库）
│   ├── Views                 # 视图层 (MainWindow, QuickLauncher, Scanner, Settings)
│   ├── ViewModels            # MVVM 视图模型（CommunityToolkit.Mvvm）
│   ├── Converters            # XAML 值转换器
│   └── Services              # 前端专有服务 (Theme, Tray, SingleInstance)
├── PortableHub.Core           # 核心领域模型与接口定义
│   ├── Models                # Software, Category, RootDirectory, AppSettings
│   └── Interfaces            # 仓储与服务契约规范
├── PortableHub.Infrastructure # 基础设施与底层实现
│   ├── Data                  # SQLite 连接工厂与版本化数据库迁移器 (Migration V1, V2)
│   ├── Repositories          # Dapper 高性能仓储实现
│   ├── Services              # 软件启动、图标缓存、备份、热键与托盘服务
│   └── Windows               # Windows 原生 Win32 API 交互与快捷方式解析
└── PortableHub.Tests          # 自动化单元测试套件（61 项全量覆盖）
```

---

## 🚀 编译与构建

### 运行环境
- Windows 10 (1809+) 或 Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 快速开始

1. **克隆仓库**
   ```bash
   git clone https://github.com/candy-blue/PortableHub.git
   cd PortableHub
   ```

2. **恢复依赖与编译**
   ```bash
   dotnet build
   ```

3. **执行全量自动化测试**
   ```bash
   dotnet test
   ```

4. **发布免安装单文件绿色版**
   ```bash
   dotnet publish PortableHub.App/PortableHub.App.csproj -c Release -r win-x64 --self-contained false -o release/standalone
   ```

---

## 📄 开源许可

本项目基于 [MIT License](LICENSE) 开源协议发布。
