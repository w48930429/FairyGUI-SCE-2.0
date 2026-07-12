---
feature: auto-army-session-routing-and-persistence
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

## 任务清单

- [x] 完成设计文档 `changes/active/auto-army-session-routing-and-persistence/design.md`
- [x] 完成实施计划 `docs/plans/2026-04-10-auto-army-session-routing-and-persistence.md`
- [x] 完成 ADR `changes/active/auto-army-session-routing-and-persistence/adr-0003-session-routing-and-storage.md`
- [x] 设计并实现会话路由索引（player/session -> connection target）
- [x] 将关键消息从 Broadcast 迁移到按玩家定向发送
- [x] 扩展仓储接口并实现持久化仓储（保留 InMemory fallback）
- [x] 增加迁移保护：读写失败重试、降级、日志与指标
- [x] 增加测试：并发会话隔离、重启后进度恢复、异常路径
- [x] 通过 `dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj`
- [x] 通过 `dotnet build src/GameEntry.csproj -c Server-Debug`
- [x] 通过 `dotnet build src/GameEntry.csproj -c Client-Debug`
- [x] 更新涉及模块 `SKILL.md` 并执行 `node build-index-auto.js`
- [x] 执行 `ospec verify changes/active/auto-army-session-routing-and-persistence`
