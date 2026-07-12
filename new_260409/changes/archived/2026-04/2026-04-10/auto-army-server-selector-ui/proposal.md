---
name: auto-army-server-selector-ui
status: archived
created: 2026-04-10T00:00:00.000Z
affects: []
flags: []
---

## 背景

当前战斗 UI 没有“选择服务器（区服）”入口，玩家无法在客户端显式选择 `S1/S2/S3`，也无法看到服务端当前认可的区服状态。  
这会导致调试和多区服进度隔离场景不可用：服务端只能用固定 player key，客户端对当前区服无感知。

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
- 实施计划: [docs/plans/2026-04-10-auto-army-server-selector-ui.md](../../../../../docs/plans/2026-04-10-auto-army-server-selector-ui.md)

## 目标

- 客户端战斗界面提供区服选择 UI（`S1/S2/S3`）并可点击切换。
- 服务端校验区服选择请求，广播当前区服状态给客户端。
- 战斗流程按当前区服构造 player progress key，实现区服维度的进度隔离。

## 范围

**涉及：**
- 新增区服选择消息 DTO（请求/查询/状态）
- 服务端消息处理与区服状态广播
- 客户端 Canvas 服务器选择控件（点击、状态高亮、同步显示）
- 服务器侧 battle loop 使用 `debug-player@<serverId>` 作为进度键

**不涉及：**
- 不做真实跨服连接/重连
- 不做账号级区服列表拉取
- 不做区服容量、延迟检测和登录大厅

## 验收标准

- [x] 客户端可在 UI 中点击选择 `S1/S2/S3`
- [x] 服务端只接受合法 server id，并回推当前区服状态
- [x] 当前区服能体现在服务端 player progress key 上
- [x] `dotnet test` 与双端构建通过
