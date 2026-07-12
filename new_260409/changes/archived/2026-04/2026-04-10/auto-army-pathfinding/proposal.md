---
name: auto-army-pathfinding
status: archived
created: 2026-04-10T00:00:00.000Z
affects: []
flags: []
---

## 背景

当前自动军团战斗只有“按目标直线推进”逻辑，单位在竖向战线发生同向拥堵时不会绕行。结果是后排单位容易被前排友军卡住，导致：

- 近战推进不稳定，部分单位长时间停滞在同一条线上；
- 目标选择和攻击距离虽然正常，但移动表现不符合“自动推图”预期；
- 客户端视觉上会看到单位重叠/排队，战斗节奏不自然。

本 change 需要补齐第一版寻路能力：在现有轻量 battle world 下增加“前方阻挡检测 + 侧向避让”的局部路径规划，不引入复杂网格寻路系统。

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
- 实施计划: [docs/plans/2026-04-10-auto-army-pathfinding.md](../../../../../docs/plans/2026-04-10-auto-army-pathfinding.md)

## 目标

- 当单位前方存在友军阻挡时，能够自动侧移绕行，而不是原地挤压。
- 目标选择和攻击距离改为 2D 距离判定，避免仅按 Y 轴导致的错误“最近目标”。
- 保持服务端权威逻辑和客户端快照渲染边界，不在客户端做本地寻路决策。

## 范围

**涉及：**
- `BattleSystems.UpdateMovement`：阻挡检测、侧移避让、回归职业车道
- `BattleSystems` 距离函数：目标选择与攻击范围改为 2D 距离
- 新增单元测试覆盖：阻挡绕行、2D 最近敌人选择

**不涉及：**
- 不实现网格/A* 全局寻路
- 不引入地形障碍、多层地图、导航网格编辑器
- 不修改数据编辑器 schema 与生成目录

## 验收标准

- [x] 前方被友军阻挡时，移动单位会产生侧向位移，能够绕行推进
- [x] 最近敌人判定采用 2D 距离，不再只看 Y 轴距离
- [x] `tests/AutoArmy.Shared.Tests` 新增寻路相关单测并通过
- [x] 双端构建通过：`Server-Debug` 和 `Client-Debug`
