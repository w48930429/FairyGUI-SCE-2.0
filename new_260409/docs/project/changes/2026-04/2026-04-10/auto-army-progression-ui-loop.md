---
name: "archived-change-auto-army-progression-ui-loop"
title: "auto-army-progression-ui-loop"
tags: [project, feature, completed, archive, ai-index]
features: ["auto-army-progression-ui-loop"]
archive: "changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop"
workflow_profile: "change"
completed_at: "2026-04-10T09:05:00Z"
affects: ["src/AutoArmy/AutoArmyGameClass.cs","src/AutoArmy/Client/BattleCanvasView.cs","src/AutoArmy/Server/CampaignService.cs","src/AutoArmy/Shared/ServerSelectionMessages.cs","tests/AutoArmy.Shared.Tests/"]
target_files: []
verification_commands: []
project_documents: []
summary: "当前 AutoArmy 已具备服务端权威战斗、快照广播和区服选择能力，但缺少“战前准备 -> 战斗进行 -> 战后结算 -> 升级再开战”的玩家可操作闭环。   现状主要问题： - 客户端缺少开战、升级、选关的可点击入口，玩家无法主动推进战役。 - 服务端没有统一的战役流程消息协议，战役操作零散，后续扩展风险高。 - 进度与战斗之间的状态机未明确，容易出现重复开战、并发升级、结算与 UI 不一致的问题。"
generated: true
generator: ospec-archive-knowledge
---

# auto-army-progression-ui-loop

> 由 OSpec 在归档时生成，供人和 AI 快速了解这个 change 做了什么以及去哪里查看证据。

## 功能摘要

当前 AutoArmy 已具备服务端权威战斗、快照广播和区服选择能力，但缺少“战前准备 -> 战斗进行 -> 战后结算 -> 升级再开战”的玩家可操作闭环。   现状主要问题： - 客户端缺少开战、升级、选关的可点击入口，玩家无法主动推进战役。 - 服务端没有统一的战役流程消息协议，战役操作零散，后续扩展风险高。 - 进度与战斗之间的状态机未明确，容易出现重复开战、并发升级、结算与 UI 不一致的问题。

## 影响范围

- src/AutoArmy/AutoArmyGameClass.cs
- src/AutoArmy/Client/BattleCanvasView.cs
- src/AutoArmy/Server/CampaignService.cs
- src/AutoArmy/Shared/ServerSelectionMessages.cs
- tests/AutoArmy.Shared.Tests/

## 实现文件

- 无

## 验证命令

- 无

## 长期项目文档

- 无

## 归档证据

- 完整归档: [changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop](../../../../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop)
- [proposal.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/proposal.md)
- [design.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/design.md)
- [tasks.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/tasks.md)
- [verification.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/verification.md)
- [review.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/review.md)
