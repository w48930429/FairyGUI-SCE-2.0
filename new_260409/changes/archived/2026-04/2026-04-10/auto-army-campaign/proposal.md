---
name: auto-army-campaign
status: archived
created: 2026-04-09T00:00:00.000Z
affects: []
flags:
  - complex_feature
  - architecture_change
  - important_decision
  - multi_phase
---

## 背景

这是一个新的 WasiCore 游戏项目。目标游戏方向是 2D 自动军团推图 RPG：玩家养成英雄和小兵兵种，进入关卡后双方阵容自动冲锋、自动索敌、自动攻击，直到分出胜负。

设计上采用 ECS-style 的组件拆分，但不要求纯 ECS；需要尊重 WasiCore 现有架构：

- 服务端负责权威战斗、关卡、结算、进度。
- 客户端负责 Canvas 2D 表现、UI、动画播放。
- 共享层保留纯数据、纯计算、快照 DTO。
- Entity / Unit 表示需要同步的权威游戏对象。
- Actor / Canvas / Visual / Animation 只做表现。

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
- 架构设计: [design.md](design.md)
- ADR 0001: [adr-0001-progress-repository.md](adr-0001-progress-repository.md)
- 实施计划: [docs/plans/2026-04-09-auto-army-campaign.md](../../../../../docs/plans/2026-04-09-auto-army-campaign.md)

## 目标

- 建立自动军团推图游戏的第一版架构和垂直切片。
- 支持固定测试阵容进入竖向 2D 战线：敌方从上向下，己方从下向上。
- 支持英雄和小兵单位参与自动战斗。
- 英雄定位为带技能的强大单位；英雄死亡不直接结束战斗。
- 第一版英雄技能包含被动技能，以及至少一个按冷却自动释放的简单主动技能。
- 战斗结束的唯一胜负条件是一方全灭。
- 支持前中后排职业式兵种，并通过可配置克制表计算克制倍率。
- 支持局外成长的架构边界：关卡胜利获得资源；资源用于提升英雄/兵种。
- 第一版局外经济只使用金币一种资源。
- 第一版进度由服务端内存仓储承载；仓储接口预留后续 CloudData 后端。
- 第一版客户端使用 Canvas 占位表现；预留序列帧 / Spine 动画组件接入点。

## 范围

**涉及：**
- 自动战斗核心循环。
- ECS-style 战斗组件和系统边界。
- 线性推图关卡；数据结构预留章节/地图节点演进空间。
- 固定测试阵容；数据结构预留赛前布阵和资源买兵。
- 英雄、前排/中排/后排小兵、兵种克制、基础属性成长。
- 服务端权威进度模型和内存 `PlayerProgress` 仓储。
- 客户端 2D 战场快照渲染的最小路径。

**不涉及：**
- 第一版不接 CloudData；后续替换进度仓储后端。
- 第一版不做赛前布阵 UI；后续补充阵容/站位编辑。
- 第一版不做资源买兵 UI；后续和局外养成一起补。
- 第一版不接正式序列帧或 Spine；先使用 Canvas 占位表现。
- 第一版不做玩家局内微操。
- 不创建新的 GameMode；继续使用项目现有 `MapGameMode`。

## 验收标准

- [ ] 需求/设计记录明确区分第一版、预留边界、后续阶段。
- [ ] 服务端能以固定己方/敌方阵容启动一场自动战斗。
- [ ] 战斗过程中单位能沿竖向战线接敌、攻击、受伤、死亡。
- [ ] 至少 3 个兵种有可配置克制关系，伤害计算能使用克制倍率。
- [ ] 胜利/失败能更新服务端内存进度。
- [ ] 英雄/兵种局外等级能影响下一场战斗属性。
- [ ] 客户端能看到 2D 战斗状态的占位表现。
- [ ] 双端构建通过：`Client-Debug` 和 `Server-Debug`。

## 已确认决策

- 2026-04-09：游戏形态选择自动军团推图；不是 MOBA、RTS 手操或局内幸存者操控。
- 2026-04-09：第一版开局阵容写成固定测试阵容；后续补赛前布阵和资源买兵。
- 2026-04-09：战线选择竖向 2D；上方敌军向下冲，下方己军向上冲。
- 2026-04-09：升级选择局外推图成长；暂不做局内击杀经验升级。
- 2026-04-09：推图第一版选择线性关卡；设计上预留章节/地图节点。
- 2026-04-09：兵种选择前中后排职业模型；克制关系用配置表/矩阵，不硬编码在攻击系统里。
- 2026-04-09：2D 表现第一版用 Canvas 占位；后续通过动画/视觉组件补序列帧或 Spine。
- 2026-04-09：进度第一版放在服务端内存仓储；Repository 边界必须允许后续迁移到 CloudData。
- 2026-04-09：英雄定位是带技能的强大小兵/特殊单位，不是基地或主将；战斗胜负唯一判定是一方全灭。
- 2026-04-09：英雄技能第一版做被动技能 + 一个自动冷却主动技；不做手动释放。
- 2026-04-09：局外经济第一版只做金币；胜利给金币，金币用于升级英雄等级和兵种等级。
