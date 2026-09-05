---
name: project-feature-index
title: 项目功能索引
tags: [project, features, archive, ai-index]
generated: true
---

# 项目功能索引

> 由 OSpec 自动生成。使用本文件定位归档记录；强制归档的未完成项会明确标记，不能视为已完成功能。

## auto-army-next-phase-modules

- 归档状态: FORCED / INCOMPLETE / ACCEPTED RISK
- 强制归档原因: Nested Git worktree root prefixes dirty paths with new_260409/, causing OSpec workspace scope false positives; historical generated knowledge files are unrelated to this docs change.
- 未通过门禁: change.workspace_scope
- 摘要: 当前 AutoArmy 已具备第一版可玩闭环（选服、开战、升级、结算、持久化），但仍处于“原型可玩”阶段，缺少一份可执行的后续功能交付文档。   现阶段主要问题：
- 影响范围: SKILL.index.json, changes/active/auto-army-next-phase-modules/proposal.md, changes/active/auto-army-next-phase-modules/review.md, changes/active/auto-army-next-phase-modules/state.json, changes/active/auto-army-next-phase-modules/tasks.md, changes/active/auto-army-next-phase-modules/verification.md, docs/SKILL.md, docs/project/next-phase-modules.md
- 归档: [changes/archived/2026-08/2026-08-18/auto-army-next-phase-modules](../../changes/archived/2026-08/2026-08-18/auto-army-next-phase-modules)
- change 功能文档: [docs/project/changes/2026-08/2026-08-18/auto-army-next-phase-modules.md](changes/2026-08/2026-08-18/auto-army-next-phase-modules.md)
- proposal.md: [打开](../../changes/archived/2026-08/2026-08-18/auto-army-next-phase-modules/proposal.md)
- tasks.md: [打开](../../changes/archived/2026-08/2026-08-18/auto-army-next-phase-modules/tasks.md)
- verification.md: [打开](../../changes/archived/2026-08/2026-08-18/auto-army-next-phase-modules/verification.md)
- review.md: [打开](../../changes/archived/2026-08/2026-08-18/auto-army-next-phase-modules/review.md)
- artifacts/agents/force-archive.json: [打开](../../changes/archived/2026-08/2026-08-18/auto-army-next-phase-modules/artifacts/agents/force-archive.json)

## auto-army-userid-identity-migration

- 摘要: 当前进度键在历史上混用了 `debug-player`、`p:<slotId>` 与 `u:<userId>`。   问题： - 账号身份不一致，导致数据归属不稳定。 - 多端/重连后可能读到非用户维度数据。 - 无法保证最终以平台用户身份做持久化。
- 影响范围: src/AutoArmy/AutoArmyGameClass.cs, src/AutoArmy/Server/UserIdentityMigrationService.cs, src/AutoArmy/Shared/CampaignFlowContracts.cs, tests/AutoArmy.Shared.Tests/UserIdentityMigrationTests.cs
- 归档: [changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration](../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration)
- change 功能文档: [docs/project/changes/2026-04/2026-04-10/auto-army-userid-identity-migration.md](changes/2026-04/2026-04-10/auto-army-userid-identity-migration.md)
- proposal.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/proposal.md)
- design.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/design.md)
- tasks.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/tasks.md)
- verification.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/verification.md)
- review.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/review.md)

## auto-army-session-routing-and-persistence

- 摘要: AutoArmy 当前已实现战役闭环，但消息主要采用全局广播，仓储仍为内存实现。   当前关键风险： - 多玩家并发时存在状态串流风险，无法保证每个玩家只收到自己的战役会话状态。 - 重启后进度丢失，难以支撑长期测试和真实玩家行为模拟。 - 会话路由与仓储边界尚未协议化，后续扩展（CloudData、鉴权）成本高。
- 影响范围: src/AutoArmy/AutoArmyGameClass.cs, src/AutoArmy/Server/, src/AutoArmy/Server/CampaignService.cs, src/AutoArmy/Server/InMemoryPlayerProgressRepository.cs, src/AutoArmy/Shared/ServerSelectionMessages.cs
- 归档: [changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence](../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence)
- change 功能文档: [docs/project/changes/2026-04/2026-04-10/auto-army-session-routing-and-persistence.md](changes/2026-04/2026-04-10/auto-army-session-routing-and-persistence.md)
- proposal.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/proposal.md)
- design.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/design.md)
- tasks.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/tasks.md)
- verification.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/verification.md)
- review.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/review.md)

## auto-army-server-selector-ui

- 摘要: 当前战斗 UI 没有“选择服务器（区服）”入口，玩家无法在客户端显式选择 `S1/S2/S3`，也无法看到服务端当前认可的区服状态。   这会导致调试和多区服进度隔离场景不可用：服务端只能用固定 player key，客户端对当前区服无感知。
- 归档: [changes/archived/2026-04/2026-04-10/auto-army-server-selector-ui](../../changes/archived/2026-04/2026-04-10/auto-army-server-selector-ui)
- change 功能文档: [docs/project/changes/2026-04/2026-04-10/auto-army-server-selector-ui.md](changes/2026-04/2026-04-10/auto-army-server-selector-ui.md)
- proposal.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-server-selector-ui/proposal.md)
- tasks.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-server-selector-ui/tasks.md)
- verification.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-server-selector-ui/verification.md)
- review.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-server-selector-ui/review.md)

## auto-army-progression-ui-loop

- 摘要: 当前 AutoArmy 已具备服务端权威战斗、快照广播和区服选择能力，但缺少“战前准备 -> 战斗进行 -> 战后结算 -> 升级再开战”的玩家可操作闭环。   现状主要问题： - 客户端缺少开战、升级、选关的可点击入口，玩家无法主动推进战役。 - 服务端没有统一的战役流程消息协议，战役操作零散，后续扩展风险高。 - 进度与战斗之间的状态机未明确，容易出现重复开战、并发升级、结算与 UI 不一致的问题。
- 影响范围: src/AutoArmy/AutoArmyGameClass.cs, src/AutoArmy/Client/BattleCanvasView.cs, src/AutoArmy/Server/CampaignService.cs, src/AutoArmy/Shared/ServerSelectionMessages.cs, tests/AutoArmy.Shared.Tests/
- 归档: [changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop](../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop)
- change 功能文档: [docs/project/changes/2026-04/2026-04-10/auto-army-progression-ui-loop.md](changes/2026-04/2026-04-10/auto-army-progression-ui-loop.md)
- proposal.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/proposal.md)
- design.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/design.md)
- tasks.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/tasks.md)
- verification.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/verification.md)
- review.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/review.md)

## auto-army-pathfinding

- 摘要: 当前自动军团战斗只有“按目标直线推进”逻辑，单位在竖向战线发生同向拥堵时不会绕行。结果是后排单位容易被前排友军卡住，导致：
- 归档: [changes/archived/2026-04/2026-04-10/auto-army-pathfinding](../../changes/archived/2026-04/2026-04-10/auto-army-pathfinding)
- change 功能文档: [docs/project/changes/2026-04/2026-04-10/auto-army-pathfinding.md](changes/2026-04/2026-04-10/auto-army-pathfinding.md)
- proposal.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-pathfinding/proposal.md)
- tasks.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-pathfinding/tasks.md)
- verification.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-pathfinding/verification.md)
- review.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-pathfinding/review.md)

## auto-army-campaign

- 摘要: 这是一个新的 WasiCore 游戏项目。目标游戏方向是 2D 自动军团推图 RPG：玩家养成英雄和小兵兵种，进入关卡后双方阵容自动冲锋、自动索敌、自动攻击，直到分出胜负。
- 归档: [changes/archived/2026-04/2026-04-10/auto-army-campaign](../../changes/archived/2026-04/2026-04-10/auto-army-campaign)
- change 功能文档: [docs/project/changes/2026-04/2026-04-10/auto-army-campaign.md](changes/2026-04/2026-04-10/auto-army-campaign.md)
- proposal.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-campaign/proposal.md)
- design.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-campaign/design.md)
- tasks.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-campaign/tasks.md)
- verification.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-campaign/verification.md)
- review.md: [打开](../../changes/archived/2026-04/2026-04-10/auto-army-campaign/review.md)
