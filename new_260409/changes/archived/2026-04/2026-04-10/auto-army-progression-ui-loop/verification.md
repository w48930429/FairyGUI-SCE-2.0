---
feature: auto-army-progression-ui-loop
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
- [x] 索引已重新生成（代码变更后）
- [x] spec-check 通过

## 可选步骤验证

- [x] design_doc: `changes/active/auto-army-progression-ui-loop/design.md` 已创建
- [x] plan_doc: `docs/plans/2026-04-10-auto-army-progression-ui-loop.md` 已创建
- [x] code_review: 实现完成后执行并记录
- [x] adr: `changes/active/auto-army-progression-ui-loop/adr-0002-campaign-flow-state-machine.md` 已创建并通过评审

## 项目联动检查

- [x] 项目概览: [docs/project/overview.md](../../../../../docs/project/overview.md)
- [x] 技术栈: [docs/project/tech-stack.md](../../../../../docs/project/tech-stack.md)
- [x] 架构说明: [docs/project/architecture.md](../../../../../docs/project/architecture.md)
- [x] 模块地图: [docs/project/module-map.md](../../../../../docs/project/module-map.md)
- [x] API 总览: [docs/project/api-overview.md](../../../../../docs/project/api-overview.md)

- [x] AutoArmy 模块技能: [src/AutoArmy/SKILL.md](../../../../../src/AutoArmy/SKILL.md)
- [x] 设计文档: [changes/active/auto-army-progression-ui-loop/design.md](./design.md)
- [x] 实施计划: [docs/plans/2026-04-10-auto-army-progression-ui-loop.md](../../../../../docs/plans/2026-04-10-auto-army-progression-ui-loop.md)

## 需求验收

- [x] 客户端可执行“选关 -> 开战 -> 结算确认 -> 下一关/重开”闭环
- [x] 客户端升级操作由服务端校验并同步到 UI
- [x] 服务端拦截非法流程请求并返回可观察反馈
- [x] 测试与双端构建通过

## 结果

- [x] 可以归档
