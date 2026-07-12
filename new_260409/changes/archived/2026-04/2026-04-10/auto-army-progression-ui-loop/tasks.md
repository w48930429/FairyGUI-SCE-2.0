---
feature: auto-army-progression-ui-loop
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

- [x] 完成设计文档 `changes/active/auto-army-progression-ui-loop/design.md`
- [x] 完成实施计划 `docs/plans/2026-04-10-auto-army-progression-ui-loop.md`
- [x] 完成 ADR `changes/active/auto-army-progression-ui-loop/adr-0002-campaign-flow-state-machine.md`
- [x] 新增战役流程 typed message（开战/升级/进度查询/结算确认）
- [x] 在 `AutoArmyGameClass` 实现战役流程状态机与请求校验
- [x] 在 `BattleCanvasView` 实现战前准备面板、升级面板、结算弹层
- [x] 在 `CampaignService` 增加关卡摘要与可展示进度聚合接口
- [x] 增加测试：流程校验、升级扣费、关卡解锁、消息处理回归
- [x] 通过 `dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj`
- [x] 通过 `dotnet build src/GameEntry.csproj -c Server-Debug`
- [x] 通过 `dotnet build src/GameEntry.csproj -c Client-Debug`
- [x] 更新涉及模块 `SKILL.md` 并执行 `node build-index-auto.js`
- [x] 执行 `ospec verify changes/active/auto-army-progression-ui-loop`
