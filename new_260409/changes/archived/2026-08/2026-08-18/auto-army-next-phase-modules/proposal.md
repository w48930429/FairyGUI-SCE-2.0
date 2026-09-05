---
name: auto-army-next-phase-modules
status: archived
created: 2026-04-11T00:00:00.000Z
change_type: docs
documentation_impact: required
documentation_updates:
  - docs/project/next-phase-modules.md
  - docs/SKILL.md
affects:
  - changes/active/auto-army-next-phase-modules/proposal.md
  - changes/active/auto-army-next-phase-modules/tasks.md
  - changes/active/auto-army-next-phase-modules/verification.md
  - changes/active/auto-army-next-phase-modules/review.md
  - changes/active/auto-army-next-phase-modules/state.json
  - docs/project/next-phase-modules.md
  - docs/SKILL.md
  - SKILL.index.json
flags: []
archive_disposition: forced
completion_status: incomplete
accepted_risk: true
force_archive_record: artifacts/agents/force-archive.json
---

## 背景

当前 AutoArmy 已具备第一版可玩闭环（选服、开战、升级、结算、持久化），但仍处于“原型可玩”阶段，缺少一份可执行的后续功能交付文档。  
现阶段主要问题：

- 功能补充方向分散，缺少统一优先级与阶段边界；
- 开发任务、验收标准和验证口径未形成标准文档；
- 后续多人协作时，难以快速判断“本阶段做什么、不做什么”。

## 项目上下文

**项目文档：**
- 项目概览: [docs/project/overview.md](../../../../../docs/project/overview.md)
- 技术栈: [docs/project/tech-stack.md](../../../../../docs/project/tech-stack.md)
- 架构说明: [docs/project/architecture.md](../../../../../docs/project/architecture.md)
- 模块地图: [docs/project/module-map.md](../../../../../docs/project/module-map.md)
- API 总览: [docs/project/api-overview.md](../../../../../docs/project/api-overview.md)

**关联模块技能：**
- datagenerated 模块技能: [src/modules/datagenerated/SKILL.md](../../../../../src/modules/datagenerated/SKILL.md)
- triggergenerated 模块技能: [src/modules/triggergenerated/SKILL.md](../../../../../src/modules/triggergenerated/SKILL.md)

**关联 API 文档：**
- module-datagenerated: [docs/api/module-datagenerated.md](../../../../../docs/api/module-datagenerated.md)
- module-triggergenerated: [docs/api/module-triggergenerated.md](../../../../../docs/api/module-triggergenerated.md)

**关联设计 / 计划文档：**
- auto-army-design-overview: [docs/design/auto-army-design-overview.md](../../../../../docs/design/auto-army-design-overview.md)
- 后续功能路线图（本次新增）: [docs/project/next-phase-modules.md](../../../../../docs/project/next-phase-modules.md)

## 目标

- 输出一份可直接执行的“后续功能模块补充路线图”。
- 将优先级、里程碑、验收标准文档化，统一后续迭代口径。
- 让后续 change 可以直接引用本文档，减少重复讨论和方向偏差。

## 范围

**涉及：**
- 新增项目文档：`docs/project/next-phase-modules.md`
- 完善本 change 的 `proposal.md` / `tasks.md` / `verification.md`
- 更新文档导航索引（`docs/SKILL.md`）

**不涉及：**
- 不修改服务端战斗逻辑与客户端渲染逻辑
- 不新增网络协议或数据结构
- 不改 `DataGenerated/TriggerGenerated` 生成目录

## 验收标准

- [ ] 已形成“后续功能模块补充”正式文档，覆盖优先级、阶段目标、验收要点
- [ ] OSpec 三件套（proposal/tasks/verification）已完成并可供后续 change 复用
- [ ] 文档导航已挂载新文档入口，索引已重建
