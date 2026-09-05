---
name: "archived-change-auto-army-userid-identity-migration"
title: "auto-army-userid-identity-migration"
tags: [project, feature, completed, archive, ai-index]
features: ["auto-army-userid-identity-migration"]
archive: "changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration"
workflow_profile: "change"
completed_at: "2026-04-10T13:34:00Z"
affects: ["src/AutoArmy/AutoArmyGameClass.cs","src/AutoArmy/Server/UserIdentityMigrationService.cs","src/AutoArmy/Shared/CampaignFlowContracts.cs","tests/AutoArmy.Shared.Tests/UserIdentityMigrationTests.cs"]
target_files: []
verification_commands: []
project_documents: []
summary: "当前进度键在历史上混用了 `debug-player`、`p:<slotId>` 与 `u:<userId>`。   问题： - 账号身份不一致，导致数据归属不稳定。 - 多端/重连后可能读到非用户维度数据。 - 无法保证最终以平台用户身份做持久化。"
generated: true
generator: ospec-archive-knowledge
---

# auto-army-userid-identity-migration

> 由 OSpec 在归档时生成，供人和 AI 快速了解这个 change 做了什么以及去哪里查看证据。

## 功能摘要

当前进度键在历史上混用了 `debug-player`、`p:<slotId>` 与 `u:<userId>`。   问题： - 账号身份不一致，导致数据归属不稳定。 - 多端/重连后可能读到非用户维度数据。 - 无法保证最终以平台用户身份做持久化。

## 影响范围

- src/AutoArmy/AutoArmyGameClass.cs
- src/AutoArmy/Server/UserIdentityMigrationService.cs
- src/AutoArmy/Shared/CampaignFlowContracts.cs
- tests/AutoArmy.Shared.Tests/UserIdentityMigrationTests.cs

## 实现文件

- 无

## 验证命令

- 无

## 长期项目文档

- 无

## 归档证据

- 完整归档: [changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration](../../../../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration)
- [proposal.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/proposal.md)
- [design.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/design.md)
- [tasks.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/tasks.md)
- [verification.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/verification.md)
- [review.md](../../../../../changes/archived/2026-04/2026-04-10/auto-army-userid-identity-migration/review.md)
