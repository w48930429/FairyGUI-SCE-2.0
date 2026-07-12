---
feature: auto-army-campaign
created: 2026-04-09
status: verifying
optional_steps: ["design_doc", "plan_doc", "adr"]
passed_optional_steps: ["design_doc", "plan_doc", "adr"]
---

## 自动验证

- [x] build 通过
- [x] lint 通过
- [x] test 通过
- [x] 索引已重新生成
- [x] spec-check 通过

## 项目联动检查

- [x] 项目概览: [docs/project/overview.md](../../../../../docs/project/overview.md)
- [x] 技术栈: [docs/project/tech-stack.md](../../../../../docs/project/tech-stack.md)
- [x] 架构说明: [docs/project/architecture.md](../../../../../docs/project/architecture.md)
- [x] 模块地图: [docs/project/module-map.md](../../../../../docs/project/module-map.md)
- [x] API 总览: [docs/project/api-overview.md](../../../../../docs/project/api-overview.md)

- [x] datagenerated 模块技能: [src/modules/datagenerated/SKILL.md](../../../../../src/modules/datagenerated/SKILL.md)
- [x] triggergenerated 模块技能: [src/modules/triggergenerated/SKILL.md](../../../../../src/modules/triggergenerated/SKILL.md)

- [x] module-datagenerated: [docs/api/module-datagenerated.md](../../../../../docs/api/module-datagenerated.md)
- [x] module-triggergenerated: [docs/api/module-triggergenerated.md](../../../../../docs/api/module-triggergenerated.md)

## 需求验收

- [x] 第一版 / 后续阶段 / 不做范围已经在 proposal 和设计文档中分开记录
- [x] `design_doc` 已完成并复核
- [x] `plan_doc` 已完成并复核
- [x] `adr` 已完成并复核
- [x] 已完成 Task 4：`PlayerProgress`、`StageDefinition`、`CampaignService`、`InMemoryPlayerProgressRepository`
- [x] 已补充 campaign 升级/关卡结算/属性继承单元测试，验证“升级影响下一场战斗初始属性”
- [x] 已完成 Task 5：服务端发布 `BattleSnapshot` TypedMessage，客户端注册消息处理并读取最新快照
- [x] 已完成 Task 6：客户端 `CanvasAnimated` 占位战场渲染（背景、战线、单位、血条、等级、职业、技能闪光）
- [x] 服务端/客户端双端构建通过

## Optional Steps

- [x] design_doc: [design.md](design.md)
- [x] plan_doc: [docs/plans/2026-04-09-auto-army-campaign.md](../../../../../docs/plans/2026-04-09-auto-army-campaign.md)
- [x] adr: [adr-0001-progress-repository.md](adr-0001-progress-repository.md)

## 结果

- [x] 可以归档

当前已执行：
- `dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj`
- `dotnet build src/GameEntry.csproj -c Server-Debug`
- `dotnet build src/GameEntry.csproj -c Client-Debug`
- `ospec verify changes/active/auto-army-campaign`
