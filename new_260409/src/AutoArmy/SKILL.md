---
name: autoarmy
title: "new_260409 AutoArmy 模块"
tags: ["module", "autoarmy", "campaign", "battle"]
---

# AutoArmy 模块

> 层级：第 3 层（业务模块文档）
> 上层：[src/SKILL.md](../SKILL.md)

## 模块概述

- **项目**：new_260409
- **模块名**：AutoArmy
- **路径**：src/AutoArmy

## 主要职责

- 提供自动军团推图的游戏入口与生命周期挂载
- 承载共享战斗模型：单位快照、战斗状态、视觉事件、克制与伤害公式
- 承载服务端权威逻辑：battle world、战斗系统、campaign 进度服务、内存仓储
- 承载战役流程状态机：`Idle -> InBattle -> ResultPendingConfirm`，约束开战、升级与结算确认顺序
- 承载会话路由：按 `playerIdentity + serverId` 维护定向发送通道，避免多玩家消息串流
- 承载持久化仓储：默认文件持久化（`FileBackedPlayerProgressRepository`），保留内存仓储 fallback
- 承载身份统一策略：仅接受 `UserId > 0`，战役进度键统一为 `u:<userId>@<server>`
- 承载 legacy 迁移策略：在用户键为空档时将 `debug-player`/`p:<slotId>` 进度迁移到用户身份
- 承载客户端占位表现：CanvasAnimated 战场渲染与快照可视化
- 承载客户端 Canvas 字体加载策略：初始化时尝试加载默认字体并在每帧绑定 `FontFaceId`，避免文字不显示
- 承载客户端专用控件 HUD：战役面板与区服选择改用 `Panel/Label/Button` 控件树，减少 Canvas 命中检测逻辑
- 承载客户端战役交互面板：选关、开战、升级、结算确认
- 承载客户端 UI 初始化兜底：`OnGameUIInitialization` 首选，`OnGameStart` 作为幂等补偿触发，降低 UI 未创建风险
- 提供第一版局部寻路：前方友军阻挡检测、侧移避让、2D 距离判定
- 提供区服选择 UI 与消息同步：`S1/S2/S3` 选择、服务端校验与状态广播

## 边界约束

- 游戏模式继续使用 `ScopeData.GameDataGameMode.MapGameMode`，不新建 GameMode
- 不修改 `src/DataGenerated/` 与 `src/TriggerGenerated/`
- 服务端负责权威状态，客户端只消费快照并渲染
- 第一版进度仓储为内存实现，CloudData 后端留作后续替换

## API / 通信

- 强类型消息：`TypedMessage<BattleSnapshot>`（服务端广播，客户端接收）
- 强类型消息：`CampaignProgressQueryMessage` / `CampaignProgressStateMessage`
- 强类型消息：`StartStageRequestMessage` / `StartStageResultMessage` / `ConfirmBattleResultMessage`
- 强类型消息：`UpgradeHeroRequestMessage` / `UpgradeTroopRequestMessage` / `OperationResultMessage`
- 统一错误码：`CampaignErrorCodes.UnauthenticatedUser = "unauthenticated_user"`
- 服务端推送策略：关键战役消息优先 `SendTo(Player)`，仅保留必要广播场景
- 战斗快照包含：`Status`、`WinnerTeam`、`ElapsedSeconds`、`Units`、`VisualEvents`
- Campaign 关键模型：`PlayerProgress`、`StageDefinition`

## 测试要求

- 共享层单测覆盖克制表、伤害公式、固定战斗 session
- campaign 单测覆盖升级扣费、金币不足、胜利解锁、升级影响下一场属性
- 战役流程单测覆盖状态机转移与非法状态拦截
- 路由与仓储单测覆盖会话隔离与文件持久化恢复
- 身份迁移单测覆盖 legacy 迁移成功与“目标已有进度不覆盖”边界
- 变更后至少通过：
  - `dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj`
  - `dotnet build src/GameEntry.csproj -c Server-Debug`
  - `dotnet build src/GameEntry.csproj -c Client-Debug`

## 关联文档

- 项目模块地图：[../../docs/project/module-map.md](../../docs/project/module-map.md)
- API 总览：[../../docs/project/api-overview.md](../../docs/project/api-overview.md)
- 当前 change：[../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/proposal.md](../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/proposal.md)
