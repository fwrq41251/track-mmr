# track-mmr

Dota 2 MMR 历史记录追踪工具，基于 [SteamKit2](https://github.com/SteamRE/SteamKit) 通过 Steam Game Coordinator 获取最近比赛及 MMR 变化。

## 功能

- **多端支持**：提供命令行 (CLI) 和图形界面 (Desktop) 两种版本。
- **数据获取**：直接连接 Steam 并在后台模拟 Dota 2 客户端获取精准的 MMR 变化记录。
- **持久化存储**：使用本地 SQLite 数据库存储完整的比赛历史。
- **安全验证**：支持 Steam Guard 二步验证，且支持 Refresh Token 持久化，首次登录后无需重复输入密码。
- **可视化**：Desktop 版本提供直观的 MMR 趋势图表（LiveCharts2）。

## 项目结构

- `TrackMmr.Cli`: 命令行工具，适合服务器或定时脚本使用。
- `TrackMmr.Desktop`: 基于 Avalonia 的桌面应用，提供可视化界面。
- `TrackMmr.Core`: 共享核心库，包含 Steam 通讯和数据库逻辑。

## 使用说明

### 命令行版本 (CLI)

```bash
# 获取最新比赛记录并更新数据库
dotnet run --project TrackMmr.Cli

# 查看所有历史记录
dotnet run --project TrackMmr.Cli -- history

# 查看最近 30 天记录
dotnet run --project TrackMmr.Cli -- history 30

# 重新登录（更新凭据）
dotnet run --project TrackMmr.Cli -- login
```

### 桌面版本 (Desktop)

```bash
# 启动图形界面
dotnet run --project TrackMmr.Desktop
```

## CLI 输出示例

```
Fetched 20 ranked matches, 2 new records saved.

Current MMR: 6500

MMR History (last 30 days)
==========================

Date                 | Match ID      | MMR    | Change  | Hero                | Result
-----------------------------------------------------------------------------------
2026-02-10 14:20:00  | 8123456789    | 6500   | +25     | Anti-Mage           | Win
2026-02-10 12:15:00  | 8123456700    | 6475   | -24     | Pudge               | Loss
```

## 定时追踪

CLI 版本可配合 cron 定时执行以持续自动记录：

```bash
# 每天上午 10 点记录一次（编辑 crontab -e）
0 10 * * * cd /path/to/track-mmr && dotnet run --project TrackMmr.Cli
```

## 开发环境

- .NET 10.0 SDK
- IDE: Visual Studio 2022 / JetBrains Rider / VS Code
