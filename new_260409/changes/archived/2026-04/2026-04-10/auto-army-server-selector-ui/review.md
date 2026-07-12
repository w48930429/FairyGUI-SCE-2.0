---
feature: auto-army-server-selector-ui
created: 2026-04-10
status: pending_review
---

## 评审范围

- Change: `auto-army-server-selector-ui`
- Mode: `full`
- Affects: `src/AutoArmy/AutoArmyGameClass.cs`、`src/AutoArmy/Client/BattleCanvasView.cs`、`src/AutoArmy/Shared/ServerSelectionMessages.cs`

## 上下文引用

**项目文档：**
- 项目概览: [docs/project/overview.md](../../../../../docs/project/overview.md)
- 技术栈: [docs/project/tech-stack.md](../../../../../docs/project/tech-stack.md)
- 架构说明: [docs/project/architecture.md](../../../../../docs/project/architecture.md)
- 模块地图: [docs/project/module-map.md](../../../../../docs/project/module-map.md)
- API 总览: [docs/project/api-overview.md](../../../../../docs/project/api-overview.md)

**模块技能：**
- datagenerated 模块技能: [src/modules/datagenerated/SKILL.md](../../../../../src/modules/datagenerated/SKILL.md)
- triggergenerated 模块技能: [src/modules/triggergenerated/SKILL.md](../../../../../src/modules/triggergenerated/SKILL.md)

**API / 设计 / 计划文档：**
- module-datagenerated: [docs/api/module-datagenerated.md](../../../../../docs/api/module-datagenerated.md)
- module-triggergenerated: [docs/api/module-triggergenerated.md](../../../../../docs/api/module-triggergenerated.md)

## Review Checklist

- [x] 实现是否符合 proposal 中的背景、目标和范围
- [x] 关联模块技能是否已同步
- [x] API / 设计 / 计划文档是否需要更新
- [x] 验证项是否覆盖主要风险
- [x] 是否存在回归风险、边界遗漏或未决问题

## Findings

- [x] 客户端 Canvas 增加 server selector，支持点击选择并高亮当前区服。
- [x] 服务端新增区服选择请求/查询处理与状态广播，非法区服会被拒绝。
- [x] battle loop 使用区服维度 player key，支持区服隔离进度。

## Decision

- [x] 可以继续实现
- [ ] 需要补充修改
- [x] 可以进入验证 / 归档流程
