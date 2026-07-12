# Auto Army UserId Identity Migration Plan

## Goal

统一 AutoArmy 战役进度身份为 `UserId`，并将历史 `debug-player`/`p:<slotId>` 数据安全迁移到 `u:<userId>@<server>`。

## Scope

- 服务端：身份解析改为 `UserId > 0` 强校验
- 服务端：legacy 进度一次性迁移服务
- 协议：新增 `unauthenticated_user` 错误码
- 测试：迁移成功与不覆盖已有数据
- 文档：design、ADR、verification、SKILL 索引同步

## Implementation Steps

1. 在 `AutoArmyGameClass` 中集中身份解析，统一 `TryBuildUserIdentity(...)`。  
2. 新增 `UserIdentityMigrationService`，封装 legacy 候选键与迁移保护策略。  
3. 在关键战役请求入口接入身份检查与迁移触发。  
4. 扩展 `CampaignErrorCodes`，增加 `unauthenticated_user`。  
5. 编写/更新单测，覆盖迁移边界。  
6. 更新 `src/AutoArmy/SKILL.md`、`tests/SKILL.md`，重建索引并执行 OSpec 验证。  

## Verification Commands

```bash
dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj
dotnet build src/GameEntry.csproj -c Server-Debug
dotnet build src/GameEntry.csproj -c Client-Debug
node build-index-auto.js
ospec verify changes/active/auto-army-userid-identity-migration
```

## Non-goals

- 不改战斗公式、关卡配置、UI 布局
- 不引入新登录系统
- 不改动 `MapGameMode` 与项目全局游戏模式绑定
