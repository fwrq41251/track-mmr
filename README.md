# track-mmr

Dota 2 MMR 历史记录追踪工具，基于 [SteamKit2](https://github.com/SteamRE/SteamKit) 通过 Steam Game Coordinator 获取段位数据。

## 功能

- 连接 Steam 并获取 Dota 2 个人资料卡数据（段位、MMR、天梯排名）
- 本地 SQLite 数据库存储历史记录
- 支持 Steam Guard 二步验证
- Refresh Token 持久化，无需每次输入密码

## 使用

```bash
cd TrackMmr

# 首次运行 - 获取当前 MMR（会提示输入 Steam 账号密码）
dotnet run

# 查看所有历史记录
dotnet run -- history

# 查看最近 30 天记录
dotnet run -- history 30
```

## 输出示例

```
Current: Immortal | MMR: 6500 | Leaderboard: #150

MMR History (last 7 days)
=========================

Date                 | Medal      | MMR   | Leaderboard
-------------------------------------------------------
2025-01-15 10:30:00  | Immortal   | 6500  | #150
2025-01-14 09:15:00  | Immortal   | 6450  | #160
```

## 定时追踪

可配合 cron 定时执行以持续记录 MMR 变化：

```bash
# 每天上午 10 点记录一次（编辑 crontab -e）
0 10 * * * cd /path/to/track-mmr/TrackMmr && dotnet run
```
