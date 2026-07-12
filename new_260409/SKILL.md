---
name: new_260409
title: new_260409
tags: ["ospec", "project", "full"]
---

# new_260409

> 层级：第 1 层（项目根文档）

## 项目概述

- **项目名称**：new_260409
- **模式**：full
- **状态**：已完成 OSpec 初始化
- **简介**：WasiCore 空白游戏项目；从默认 MapGameMode、默认场景和 C# 游戏入口开始开发新游戏。

## 技术栈

- csharp dotnet wasicoresdk wasm

## 项目架构

WasiCore 游戏项目；src/GameEntry.csproj 为主工程；客户端/服务端使用条件编译；editor/data、editor/trigger、scene、res 存放编辑器数据和资源。

## 目录导航

- 文档中心：[docs/SKILL.md](docs/SKILL.md)
- 源码地图：[src/SKILL.md](src/SKILL.md)
- 测试入口：[tests/SKILL.md](tests/SKILL.md)
- 索引构建：`build-index-auto.js`（调用 `ospec index build .`）
- 自动化脚本：`scripts/`（`start-change.ps1`、`verify-change.ps1`、`install-git-hooks.ps1`）
- AI 指南：[for-ai/ai-guide.md](for-ai/ai-guide.md)

## 插件阻断

- 开始推进 active change 前先读取 `.skillrc`。
- 如果项目启用了 Stitch，且当前 change 激活了 `stitch_design_review`，先检查 `changes/active/<change>/artifacts/stitch/approval.json`。
- 当 Stitch 审批缺失或状态不是 `approved` 时，视为 change 仍被阻断，先完成设计审核再继续。
