# Auto Army Progression UI Loop Plan

## Goal

补齐 AutoArmy 可玩闭环：玩家可在客户端完成选关、开战、升级、结算确认，并由服务端权威维护进度。

## Scope

- 共享层：新增战役流程 typed message
- 服务端：新增战役状态机与请求校验
- 客户端：Canvas 增加战前面板、升级面板、结算弹层
- 测试：补流程与升级相关回归测试

## Implementation Steps

1. 在 `Shared` 新增战役请求/响应 DTO 与统一错误码。  
2. 在 `AutoArmyGameClass` 注册新消息处理器并实现状态机：`Idle`、`InBattle`、`ResultPendingConfirm`。  
3. 在 `CampaignService` 提供 UI 需要的进度聚合查询接口（金币、当前关卡、可升级项）。  
4. 在 `BattleCanvasView` 实现三块 UI：战前准备、升级入口、战后结算；点击事件全部走服务端消息。  
5. 补单测与集成测试，覆盖非法状态、防重复开战、金币不足升级、关卡解锁。  
6. 运行验证命令并更新 change 文档。  

## Verification Commands

```bash
dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj
dotnet build src/GameEntry.csproj -c Server-Debug
dotnet build src/GameEntry.csproj -c Client-Debug
ospec verify changes/active/auto-army-progression-ui-loop
```

## Non-goals

- 不替换为 CloudData 持久化
- 不引入高级寻路算法
- 不改动 MapGameMode 与 DataGenerated/TriggerGenerated
