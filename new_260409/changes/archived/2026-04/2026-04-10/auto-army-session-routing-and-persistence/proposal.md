---
name: auto-army-session-routing-and-persistence
status: archived
created: 2026-04-10T00:00:00.000Z
affects:
  - src/AutoArmy/AutoArmyGameClass.cs
  - src/AutoArmy/Shared/ServerSelectionMessages.cs
  - src/AutoArmy/Server/CampaignService.cs
  - src/AutoArmy/Server/InMemoryPlayerProgressRepository.cs
  - src/AutoArmy/Server/
flags:
  - cross_module
  - architecture_change
  - important_decision
  - multi_phase
  - large_feature
  - multi_file_change
---

## 背景

AutoArmy 当前已实现战役闭环，但消息主要采用全局广播，仓储仍为内存实现。  
当前关键风险：
- 多玩家并发时存在状态串流风险，无法保证每个玩家只收到自己的战役会话状态。
- 重启后进度丢失，难以支撑长期测试和真实玩家行为模拟。
- 会话路由与仓储边界尚未协议化，后续扩展（CloudData、鉴权）成本高。

## 项目上下文

**项目文档：**
- 项目概览: [docs/project/overview.md](../../../../../docs/project/overview.md)
- 技术栈: [docs/project/tech-stack.md](../../../../../docs/project/tech-stack.md)
- 架构说明: [docs/project/architecture.md](../../../../../docs/project/architecture.md)
- 模块地图: [docs/project/module-map.md](../../../../../docs/project/module-map.md)
- API 总览: [docs/project/api-overview.md](../../../../../docs/project/api-overview.md)

**关联模块技能：**
- AutoArmy 模块技能: [src/AutoArmy/SKILL.md](../../../../../src/AutoArmy/SKILL.md)

**关联 API 文档：**
- 项目 API 总览: [docs/project/api-overview.md](../../../../../docs/project/api-overview.md)

**关联设计 / 计划文档：**
- 设计文档: [changes/active/auto-army-session-routing-and-persistence/design.md](./design.md)
- ADR: [changes/active/auto-army-session-routing-and-persistence/adr-0003-session-routing-and-storage.md](./adr-0003-session-routing-and-storage.md)
- 实施计划: [docs/plans/2026-04-10-auto-army-session-routing-and-persistence.md](../../../../../docs/plans/2026-04-10-auto-army-session-routing-and-persistence.md)

## 目标

- 建立“按玩家会话定向下发”的消息路由层，消除全局广播串流风险。
- 将进度仓储从纯内存提升为可持久化实现（保留内存实现用于本地调试）。
- 输出可执行的迁移方案，保证业务迭代时不会中断现有战役闭环。

## 范围

**涉及：**
- 会话键设计（playerId/serverId/sessionId）与路由映射
- 消息发送策略从 broadcast 升级为会话定向
- 仓储接口扩展与持久化实现落地（含失败重试策略）
- 兼容迁移策略：内存仓储作为 fallback/测试桩

**不涉及：**
- 不做账号登录系统与跨服网络连接
- 不改战斗数值/技能系统
- 不改变 MapGameMode 和 DataGenerated/TriggerGenerated

## 验收标准

- [ ] 同一服务器的多个玩家并发战斗时，消息不串流
- [ ] 进度在服务重启后可恢复
- [ ] 会话路由、仓储失败重试、回退策略在文档和测试中可验证
