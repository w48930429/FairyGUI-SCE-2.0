---
feature: auto-army-campaign
created: 2026-04-09
optional_steps: ["design_doc", "plan_doc", "adr"]
---

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

## 任务清单

- [x] 完成需求澄清，确认第一版战斗、成长、关卡、表现和存档边界
- [x] 编写架构设计文档，覆盖服务端 / 共享 / 客户端分层
- [x] 记录关键 ADR：服务端内存进度仓储先行，后续迁移 CloudData
- [x] 编写分阶段实施计划：固定阵容原型 -> 局外成长 -> 关卡推进 -> 表现替换
- [x] 完成实现（Task 1-6 已完成，含快照消息通路与客户端 Canvas 占位战场）
- [x] 对齐项目规划文档与本次 change 的边界
- [x] 更新涉及模块的 `SKILL.md`
- [x] 更新相关 API / 设计 / 计划文档
- [x] 重新生成 `SKILL.index.json`
- [x] 执行验证并更新 `verification.md`
