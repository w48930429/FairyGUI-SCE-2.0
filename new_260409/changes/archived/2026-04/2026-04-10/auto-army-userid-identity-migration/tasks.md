---
feature: auto-army-userid-identity-migration
created: 2026-04-10
optional_steps:
  - design_doc
  - plan_doc
  - code_review
  - adr
---

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

## 任务清单

- [x] 完成设计文档 `changes/active/auto-army-userid-identity-migration/design.md`
- [x] 完成实施计划 `docs/plans/2026-04-10-auto-army-userid-identity-migration.md`
- [x] 完成 ADR `changes/active/auto-army-userid-identity-migration/adr-0004-userid-identity-migration.md`
- [x] 实现 `UserIdentityMigrationService`，支持 legacy key 一次性迁移
- [x] 将 AutoArmy 服务端身份解析改为只接受 `UserId > 0`
- [x] 新增 `unauthenticated_user` 错误码并接入战役请求处理
- [x] 增加迁移单测：成功迁移与不覆盖已有用户进度
- [x] 更新 `src/AutoArmy/SKILL.md` 与 `tests/SKILL.md`
- [x] 执行 `node build-index-auto.js`
- [x] 通过 `dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj`
- [x] 通过 `dotnet build src/GameEntry.csproj -c Server-Debug`
- [x] 通过 `dotnet build src/GameEntry.csproj -c Client-Debug`
- [x] 执行 `ospec verify changes/active/auto-army-userid-identity-migration`
