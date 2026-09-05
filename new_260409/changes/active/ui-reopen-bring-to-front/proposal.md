---
name: ui-reopen-bring-to-front
status: active
created: 2026-08-17
affects: ["src/FGUI/FGUIManager.cs", "tests/FairyGUI.Tests/UIRuntimeRootOrderingTests.cs", "changes/active/ui-reopen-bring-to-front/proposal.md", "changes/active/ui-reopen-bring-to-front/tasks.md", "changes/active/ui-reopen-bring-to-front/verification.md", "changes/active/ui-reopen-bring-to-front/review.md", "changes/active/ui-reopen-bring-to-front/state.json"]
flags: []
change_type: bugfix
documentation_impact: none
documentation_updates: []
documentation_reason: "修复仅恢复现有顶层 UI 的重复激活层级语义，不改变公开 API、模块规则或用户操作流程。"
---

## 背景

顶层 UI 复用同一实例时不会明确重新挂载原生根节点。顺序为 A 打开、B 打开、再打开 A 后，A 仍可能位于 B 下方，导致用户无法回到最近激活的窗口或全屏页面。

本 change 在不改变首次创建、关闭销毁、遮罩、输入和组件内部层级的前提下，恢复重复激活时的顶层置顶行为。
## 项目上下文

**项目文档：**
- 项目概览: [docs/project/overview.md](../../../docs/project/overview.md)
- 技术栈: [docs/project/tech-stack.md](../../../docs/project/tech-stack.md)
- 架构说明: [docs/project/architecture.md](../../../docs/project/architecture.md)
- 模块地图: [docs/project/module-map.md](../../../docs/project/module-map.md)
- API 总览: [docs/project/api-overview.md](../../../docs/project/api-overview.md)

**关联模块技能：**
- 无：`src/FGUI` 未定义独立 `SKILL.md`，遵循项目级开发与工作流规范。

**关联 API 文档：**
- 无：不改变公开 API。

**关联设计 / 计划文档：**
- 无：这是明确根因的局部 bugfix，不需要设计或计划文档。

## 目标

- 对同一 `Window` 重复调用 `Show()` 时，第二次显示会重新挂载其原生根控件，使其成为最近打开的顶层 UI。
- 对同一 `GComponent` 重复调用 `AddToFullScreenRoot()` 时，返回同一个 `FGUIRoot` 并将该根重新挂载至顶层。
- 保持全屏内容到 `FGUIRoot` 的注册映射；关闭和销毁路径不变。

## 范围

**涉及：**
- `src/FGUI/FGUIManager.cs`：全屏根复用时的顶层原生重挂载。
- `tests/FairyGUI.Tests/UIRuntimeRootOrderingTests.cs`：记录原生挂载顺序及关闭解除注册的回归测试。

**不涉及：**
- 不引入全局 UI 栈、窗口层级枚举或新的公开 API。
- 不修改组件内部 `SetChildIndex`、Popup、拖拽、遮罩或输入处理。
- 不调整关闭、销毁及全屏根注册/注销语义。

## 验收标准

- [x] 已显示的 `Window` 再次调用 `Show()` 时，测试确认原生根执行重新挂载并保持显示状态。
- [x] 已注册的全屏内容再次调用 `AddToFullScreenRoot()` 时，测试确认复用同一个根并执行原生重新挂载。
- [x] 全屏内容仍可由 `RemoveFromRoot(content)` 正常解除注册和销毁。
- [ ] `FairyGUI.Tests` 通过，OSpec 验证与技能索引更新完成。
