---
feature: auto-army-userid-identity-migration
created: 2026-04-10
status: verifying
optional_steps:
  - design_doc
  - plan_doc
  - code_review
  - adr
passed_optional_steps:
  - design_doc
  - plan_doc
  - code_review
  - adr
---

## 自动验证

- [x] build 通过
- [x] lint 通过（若项目启用）
- [x] test 通过
- [x] 索引已重新生成
- [x] spec-check 通过

## 可选步骤验证

- [x] design_doc: `changes/active/auto-army-userid-identity-migration/design.md` 已创建
- [x] plan_doc: `docs/plans/2026-04-10-auto-army-userid-identity-migration.md` 已创建
- [x] adr: `changes/active/auto-army-userid-identity-migration/adr-0004-userid-identity-migration.md` 已创建
- [x] code_review: 已完成自检并记录在 `review.md`

## 项目联动检查

- [x] 项目概览: [docs/project/overview.md](../../../../../docs/project/overview.md)
- [x] 技术栈: [docs/project/tech-stack.md](../../../../../docs/project/tech-stack.md)
- [x] 架构说明: [docs/project/architecture.md](../../../../../docs/project/architecture.md)
- [x] 模块地图: [docs/project/module-map.md](../../../../../docs/project/module-map.md)
- [x] API 总览: [docs/project/api-overview.md](../../../../../docs/project/api-overview.md)

- [x] AutoArmy 模块技能: [src/AutoArmy/SKILL.md](../../../../../src/AutoArmy/SKILL.md)
- [x] tests 模块技能: [tests/SKILL.md](../../../../../tests/SKILL.md)

- [x] 设计文档: [changes/active/auto-army-userid-identity-migration/design.md](./design.md)
- [x] ADR: [changes/active/auto-army-userid-identity-migration/adr-0004-userid-identity-migration.md](./adr-0004-userid-identity-migration.md)
- [x] 实施计划: [docs/plans/2026-04-10-auto-army-userid-identity-migration.md](../../../../../docs/plans/2026-04-10-auto-army-userid-identity-migration.md)

## 需求验收

- [x] 服务端身份主键统一为 `u:<userId>@<server>`，不再接受 `debug-player` 与 `p:<slot>`
- [x] 无有效 `UserId` 请求会返回 `unauthenticated_user` 并拒绝执行
- [x] legacy 进度在目标用户默认进度时可迁移，且不会覆盖已有用户数据

## 结果

- [x] 可以归档
