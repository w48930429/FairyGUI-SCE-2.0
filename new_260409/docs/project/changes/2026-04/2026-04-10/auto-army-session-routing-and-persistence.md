---
name: "archived-change-auto-army-session-routing-and-persistence"
title: "auto-army-session-routing-and-persistence"
tags: [project, feature, completed, archive, ai-index]
features: ["auto-army-session-routing-and-persistence"]
archive: "changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence"
workflow_profile: "change"
completed_at: "2026-04-10T09:45:00Z"
affects: ["src/AutoArmy/AutoArmyGameClass.cs","src/AutoArmy/Server/","src/AutoArmy/Server/CampaignService.cs","src/AutoArmy/Server/InMemoryPlayerProgressRepository.cs","src/AutoArmy/Shared/ServerSelectionMessages.cs"]
target_files: []
verification_commands: []
project_documents: []
summary: "AutoArmy 当前已实现战役闭环，但消息主要采用全局广播，仓储仍为内存实现。   当前关键风险： - 多玩家并发时存在状态串流风险，无法保证每个玩家只收到自己的战役会话状态。 - 重启后进度丢失，难以支撑长期测试和真实玩家行为模拟。 - 会话路由与仓储边界尚未协议化，后续扩展（CloudData、鉴权）成本高。"
generated: true
generator: ospec-archive-knowledge
---

# auto-army-session-routing-and-persistence

> 由 OSpec 在归档时生成，供人和 AI 快速了解这个 change 做了什么以及去哪里查看证据。

## 功能摘要

AutoArmy 当前已实现战役闭环，但消息主要采用全局广播，仓储仍为内存实现。   当前关键风险： - 多玩家并发时存在状态串流风险，无法保证每个玩家只收到自己的战役会话状态。 - 重启后进度丢失，难以支撑长期测试和真实玩家行为模拟。 - 会话路由与仓储边界尚未协议化，后续扩展（CloudData、鉴权）成本高。

## 影响范围

- src/AutoArmy/AutoArmyGameClass.cs
- src/AutoArmy/Server/
- src/AutoArmy/Server/CampaignService.cs
- src/AutoArmy/Server/InMemoryPlayerProgressRepository.cs
- src/AutoArmy/Shared/ServerSelectionMessages.cs

## 实现文件

- 无

## 验证命令

- 无

## 长期项目文档

- 无

## 归档证据

- 完整归档: [changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence](../../../../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence)
- [proposal.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/proposal.md)
- [design.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/design.md)
- [tasks.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/tasks.md)
- [verification.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/verification.md)
- [review.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/review.md)
