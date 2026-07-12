---
name: ui-constraints
title: "new_260409 UI 规范约束"
tags: ["project", "ui", "constraints", "client"]
---

# UI 规范约束

## 适用范围

- 适用于 `src/AutoArmy/Client/` 下所有客户端 UI 与交互实现。
- 目标是统一“战场渲染”与“业务交互 HUD”边界，避免后续实现混乱。

## 强制约束

- 战场可视化（背景、战线、单位）使用 `CanvasAnimated` 绘制。
- 业务交互 HUD（区服选择、选关、开战、升级、确认、反馈）使用专用 UI 控件：`Panel / Label / Button`。
- 不再新增 Canvas 命中框作为主交互方案；点击交互默认由 `Button.OnPointerClicked` 承担。
- 所有 UI 代码必须放在 `#if CLIENT` 侧；服务端不得直接依赖客户端 UI 类型。
- UI 初始化必须是幂等的：`OnGameUIInitialization` 为主路径，`OnGameStart` 可做补偿触发，但不得重复创建根控件。
- 新增 Canvas 文本时，必须保证字体已加载并绑定 `FontFaceId`，避免“文字不显示”回归。
- 所有根 UI 控件必须进入可视树（`AddToVisualTree()`）；不再使用时要释放（`Dispose`/解绑事件）。

## 推荐实践

- 视图层只做展示和输入转发；业务状态以服务端消息为准。
- `Update*State` 只更新本地快照，渲染/控件刷新统一在帧回调里执行。
- 按钮可用状态（禁用/高亮）由当前 `CampaignFlowState` 与关卡解锁状态驱动，避免本地硬编码流程。
- UI 变更优先保持小步迭代：先保留现有战场 Canvas，再替换 HUD 控件，避免一次性重构风险。

## 提交前检查

- `dotnet build src/GameEntry.csproj -c Client-Debug`
- `dotnet build src/GameEntry.csproj -c Server-Debug`
- 若改动共享逻辑：`dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj`

