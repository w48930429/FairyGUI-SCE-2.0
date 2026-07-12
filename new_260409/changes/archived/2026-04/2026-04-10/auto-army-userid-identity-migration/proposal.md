---
name: auto-army-userid-identity-migration
status: archived
created: 2026-04-10T00:00:00.000Z
affects:
  - src/AutoArmy/AutoArmyGameClass.cs
  - src/AutoArmy/Shared/CampaignFlowContracts.cs
  - src/AutoArmy/Server/UserIdentityMigrationService.cs
  - tests/AutoArmy.Shared.Tests/UserIdentityMigrationTests.cs
flags:
  - cross_module
  - architecture_change
  - important_decision
  - multi_file_change
---

## 背景

当前进度键在历史上混用了 `debug-player`、`p:<slotId>` 与 `u:<userId>`。  
问题：
- 账号身份不一致，导致数据归属不稳定。
- 多端/重连后可能读到非用户维度数据。
- 无法保证最终以平台用户身份做持久化。

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
- 设计文档: [changes/active/auto-army-userid-identity-migration/design.md](./design.md)
- ADR: [changes/active/auto-army-userid-identity-migration/adr-0004-userid-identity-migration.md](./adr-0004-userid-identity-migration.md)
- 实施计划: [docs/plans/2026-04-10-auto-army-userid-identity-migration.md](../../../../../docs/plans/2026-04-10-auto-army-userid-identity-migration.md)

## 目标

- 统一以 `Player.UserId` 作为战役身份主键。
- 对历史 `debug-player`/`p:<slotId>` 数据做一次性迁移。
- 无有效 `UserId` 时拒绝写入进度并返回明确错误码。

## 范围

**涉及：**
- 身份解析：仅接受 `UserId > 0` 的玩家身份
- 迁移服务：从 legacy key 迁移到 `u:<userId>@<server>`
- 操作反馈：新增 `unauthenticated_user` 错误码
- 回归测试：身份迁移成功与不覆盖已有数据

**不涉及：**
- 不改战斗数值和关卡配置
- 不改客户端画面结构
- 不引入新账号系统

## 验收标准

- [x] 进度 key 统一为 `u:<userId>@<server>`
- [x] 无有效 `UserId` 的请求被拒绝并返回 `unauthenticated_user`
- [x] legacy 数据可迁移，且不会覆盖已有用户进度
