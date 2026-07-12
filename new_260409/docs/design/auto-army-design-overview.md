---
name: auto-army-design-overview
title: "AutoArmy 设计总览"
tags: ["docs", "design", "autoarmy", "adr"]
---

# AutoArmy 设计总览

## 文档目的

本文档统一整理当前 AutoArmy 的核心设计与 ADR，作为单一入口，便于后续迭代时快速对齐边界、决策和演进方向。

## 设计演进概览

1. **推图架构基线（Campaign）**
- 建立 2D 自动军团推图第一版垂直切片
- 明确服务端权威、客户端展示、共享消息与模型分层
- 战斗系统按固定 tick 顺序执行，关卡与局外成长解耦

2. **战役闭环与 UI 流程（Progression UI Loop）**
- 引入服务端流程状态机：`Idle -> InBattle -> ResultPendingConfirm`
- 统一 typed message 协议，标准化请求/回推与错误码
- 客户端只发意图与渲染回推，不做本地权威结算

3. **会话路由与持久化（Session Routing & Persistence）**
- 战役关键消息从广播迁移到按玩家定向发送
- 抽象进度仓储，支持 `InMemory` 与持久化实现分层
- 为并发隔离、重启恢复、冲突重试提供基础能力

4. **UserId 身份统一与历史迁移（UserId Identity Migration）**
- 战役进度主键统一为 `u:<userId>@<server>`
- 对 `debug-player` / `p:<slotId>` 执行一次性迁移
- 无有效 `UserId` 请求统一拒绝并返回 `unauthenticated_user`

## 当前架构基线

### 分层边界

- `SERVER`：战斗权威逻辑、战役流程状态机、进度读写、身份校验、迁移策略
- `CLIENT`：Canvas 占位渲染、交互面板、消息发送与状态显示
- `Shared`：DTO、错误码、战斗模型、关卡与成长核心结构

### 核心流程

1. 客户端请求进度 -> 服务端返回战役状态  
2. 客户端请求开战 -> 服务端校验状态与解锁条件后进入战斗  
3. 服务端持续推送 `BattleSnapshot`  
4. 战斗结束 -> 服务端回推结果与最新进度  
5. 客户端确认结算 -> 服务端回到 `Idle`

### 身份与键空间

- 唯一战役身份：`u:<userId>`
- 最终存储键：`u:<userId>@<server>`
- legacy 候选键：`p:<slotId>@<server>`、`debug-player@<server>`
- 迁移保护：目标已有有效进度时不覆盖

## ADR 汇总

### ADR 0001：进度仓储先用服务端内存实现（Accepted）
- 决策：先建立仓储边界并采用内存实现，业务层不直接耦合 CloudData
- 价值：先验证玩法与流程，再分阶段接入持久化

### ADR 0002：战役流程采用服务端状态机 + Typed Message（已落地）
- 决策：用统一状态机和强类型消息收敛战役流程
- 价值：避免并发状态竞争，提升可测试性与一致性

### ADR 0003：会话路由定向发送 + 可持久化仓储抽象（已落地）
- 决策：默认按玩家会话定向发送；仓储采用可替换抽象并保留内存兜底
- 价值：并发隔离清晰，支持重启恢复与后续账号体系扩展

### ADR 0004：战役进度身份统一为 UserId 并执行 legacy 迁移（已落地）
- 决策：只接受 `UserId > 0`，并对历史键执行一次性迁移
- 价值：身份体系单一，数据归属稳定，后续鉴权/统计边界清晰

## 现阶段非目标

- 不改战斗公式与关卡内容
- 不引入新的登录系统
- 不扩展跨服网络模型
- 不调整 `MapGameMode` 既有项目绑定

## 源文档索引

- 推图架构设计：`changes/archived/2026-04/2026-04-10/auto-army-campaign/design.md`
- ADR 0001：`changes/archived/2026-04/2026-04-10/auto-army-campaign/adr-0001-progress-repository.md`
- 战役闭环与 UI 设计：`changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/design.md`
- ADR 0002：`changes/archived/2026-04/2026-04-10/auto-army-progression-ui-loop/adr-0002-campaign-flow-state-machine.md`
- 会话路由与持久化设计：`changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/design.md`
- ADR 0003：`changes/archived/2026-04/2026-04-10/auto-army-session-routing-and-persistence/adr-0003-session-routing-and-storage.md`
- UserId 身份迁移设计：`changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/design.md`
- ADR 0004：`changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/adr-0004-userid-identity-migration.md`
