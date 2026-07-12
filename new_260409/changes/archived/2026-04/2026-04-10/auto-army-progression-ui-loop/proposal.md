---
name: auto-army-progression-ui-loop
status: archived
created: 2026-04-10T00:00:00.000Z
affects:
  - src/AutoArmy/AutoArmyGameClass.cs
  - src/AutoArmy/Client/BattleCanvasView.cs
  - src/AutoArmy/Shared/ServerSelectionMessages.cs
  - src/AutoArmy/Server/CampaignService.cs
  - tests/AutoArmy.Shared.Tests/
flags:
  - cross_module
  - complex_feature
  - large_feature
  - multi_phase
  - multi_file_change
  - important_decision
---

## 背景

当前 AutoArmy 已具备服务端权威战斗、快照广播和区服选择能力，但缺少“战前准备 -> 战斗进行 -> 战后结算 -> 升级再开战”的玩家可操作闭环。  
现状主要问题：
- 客户端缺少开战、升级、选关的可点击入口，玩家无法主动推进战役。
- 服务端没有统一的战役流程消息协议，战役操作零散，后续扩展风险高。
- 进度与战斗之间的状态机未明确，容易出现重复开战、并发升级、结算与 UI 不一致的问题。

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
- 设计文档: [changes/active/auto-army-progression-ui-loop/design.md](./design.md)
- ADR: [changes/active/auto-army-progression-ui-loop/adr-0002-campaign-flow-state-machine.md](./adr-0002-campaign-flow-state-machine.md)
- 实施计划: [docs/plans/2026-04-10-auto-army-progression-ui-loop.md](../../../../../docs/plans/2026-04-10-auto-army-progression-ui-loop.md)

## 目标

- 建立可操作的战役闭环：玩家可在客户端完成选关、开战、升级和结算确认。
- 建立服务端权威的战役状态机与 typed message 协议，保证流程一致性。
- 为后续持久化仓储替换与更多关卡扩展保留清晰边界。

## 范围

**涉及：**
- 新增战役流程消息（开战、升级、查询、结算确认）及服务端处理逻辑
- 在 `AutoArmyGameClass` 增加战役会话状态机与消息路由
- 在 `BattleCanvasView` 增加战前面板、升级入口、战后结算弹层
- 在 `CampaignService` 暴露支撑 UI 所需的进度与关卡摘要数据
- 补齐对应单元测试/集成测试与回归用例

**不涉及：**
- 不实现真实跨服网络连接与账号体系
- 不引入 A* / NavMesh 等高级寻路
- 不切换到 CloudData 持久化（仅保留仓储接口可替换点）

## 验收标准

- [ ] 客户端可执行“选关 -> 开战 -> 结算确认 -> 下一关/重开”闭环操作
- [ ] 客户端可执行英雄/兵种升级，服务端完成扣费校验并广播最新进度
- [ ] 服务端拒绝非法流程请求（重复开战、越权关卡、金币不足升级）
- [ ] 战斗快照与战役进度 UI 保持一致，关键状态可追踪日志可读
- [ ] `dotnet test`、`Server-Debug`、`Client-Debug` 构建通过
