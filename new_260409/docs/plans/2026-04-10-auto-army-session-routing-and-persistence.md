# Auto Army Session Routing and Persistence Plan

## Goal

为 AutoArmy 增加“按玩家会话定向消息路由”和“可持久化进度仓储”，解决并发串流与重启丢档问题。

## Scope

- 服务端：会话路由索引与消息定向发送
- 服务端：进度仓储抽象与持久化实现
- 测试：并发隔离、故障降级、重启恢复
- 文档：设计、ADR、验证与迁移说明

## Implementation Steps

1. 新增 `ISessionRouter` 与 `PlayerSessionContext`，接入玩家绑定/解绑生命周期。  
2. 将战役核心消息从 `Broadcast` 迁移到定向发送（先双写观察，再移除广播）。  
3. 抽象 `IPlayerProgressStore`，将 `InMemory` 实现适配新接口。  
4. 增加持久化实现（CloudData），支持版本冲突重试与失败降级。  
5. 增加并发与重启恢复测试，覆盖错误路径。  
6. 更新 `SKILL.md`、重建索引、执行 OSpec 验证。  

## Verification Commands

```bash
dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj
dotnet build src/GameEntry.csproj -c Server-Debug
dotnet build src/GameEntry.csproj -c Client-Debug
node build-index-auto.js
ospec verify changes/active/auto-army-session-routing-and-persistence
```

## Non-goals

- 不实现登录鉴权系统
- 不调整战斗公式或关卡内容
- 不引入跨服网络连接
