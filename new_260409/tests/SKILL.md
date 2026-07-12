---
name: tests
title: "new_260409 测试说明"
tags: ["tests", "quality", "verification"]
---

# 测试说明

## 测试策略

- 共享层单元测试：`AutoArmy.Shared.Tests/`，覆盖克制表、伤害公式、固定战斗 session、campaign 升级与关卡结算、局部寻路逻辑、战役流程状态机、会话路由隔离、文件持久化恢复
- 共享层单元测试：`AutoArmy.Shared.Tests/`，覆盖 UserId 身份迁移（legacy 可迁移、已有用户进度不覆盖）
- 单元测试：待补充
- 集成测试：待补充
- 端到端测试：待补充
