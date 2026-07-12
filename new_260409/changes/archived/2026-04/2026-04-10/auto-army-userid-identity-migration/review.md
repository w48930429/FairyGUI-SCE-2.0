---
feature: auto-army-userid-identity-migration
created: 2026-04-10
status: pending_review
---

## 评审范围

- Change: `auto-army-userid-identity-migration`
- Mode: `full`
- Affects: `src/AutoArmy/AutoArmyGameClass.cs`, `src/AutoArmy/Shared/CampaignFlowContracts.cs`, `src/AutoArmy/Server/UserIdentityMigrationService.cs`, `tests/AutoArmy.Shared.Tests/UserIdentityMigrationTests.cs`

## 上下文引用

**项目文档：**
- 项目概览: [docs/project/overview.md](../../../../../docs/project/overview.md)
- 技术栈: [docs/project/tech-stack.md](../../../../../docs/project/tech-stack.md)
- 架构说明: [docs/project/architecture.md](../../../../../docs/project/architecture.md)
- 模块地图: [docs/project/module-map.md](../../../../../docs/project/module-map.md)
- API 总览: [docs/project/api-overview.md](../../../../../docs/project/api-overview.md)

**模块技能：**
- AutoArmy 模块技能: [src/AutoArmy/SKILL.md](../../../../../src/AutoArmy/SKILL.md)
- tests 模块技能: [tests/SKILL.md](../../../../../tests/SKILL.md)

**API / 设计 / 计划文档：**
- 设计文档: [changes/active/auto-army-userid-identity-migration/design.md](./design.md)
- ADR: [changes/active/auto-army-userid-identity-migration/adr-0004-userid-identity-migration.md](./adr-0004-userid-identity-migration.md)
- 实施计划: [docs/plans/2026-04-10-auto-army-userid-identity-migration.md](../../../../../docs/plans/2026-04-10-auto-army-userid-identity-migration.md)

## Review Checklist

- [x] 实现是否符合 proposal 中的背景、目标和范围
- [x] 关联模块技能是否已同步
- [x] API / 设计 / 计划文档是否需要更新
- [x] 验证项是否覆盖主要风险
- [x] 是否存在回归风险、边界遗漏或未决问题

## Findings

- [x] 无阻断问题。当前策略在 `UserId <= 0` 时统一拒绝请求，能避免匿名或临时身份污染用户进度。
- [x] 已补充迁移回归测试，覆盖“可迁移”和“不覆盖已有数据”两条关键边界。

## Decision

- [x] 可以继续实现
- [ ] 需要补充修改
- [x] 可以进入验证 / 归档流程
