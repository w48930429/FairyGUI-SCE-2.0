# Auto Army Server Selector UI Plan

## Goal

为客户端战斗界面增加区服选择 UI，并通过服务端权威消息同步当前选中区服，支撑区服维度的进度隔离。

## Scope

- 共享层：区服消息 DTO
- 服务端：区服选择请求处理、查询处理、状态广播
- 客户端：Canvas 上绘制可点击 server chips（S1/S2/S3）并同步高亮状态
- 战斗流程：player progress key 改为 `debug-player@<serverId>`

## Implementation Steps

1. 新增 `ServerSelectionRequestMessage`、`ServerSelectionQueryMessage`、`ServerSelectionStateMessage`。  
2. `AutoArmyGameClass` 服务端注册 typed message handler：  
   - 请求：校验 server id、更新当前区服、广播状态；  
   - 查询：直接回推当前状态。  
3. 客户端在 UI 初始化时请求区服状态，点击区服 chip 时发送请求消息。  
4. `BattleCanvasView` 增加 server selector 绘制与点击命中逻辑。  
5. battle loop 读取当前区服拼接 player key，实现区服隔离进度。  
6. 运行测试与双端构建，更新 change 文档并 finalize。

## Non-goals

- 不实现真实跨服网络连接
- 不实现登录大厅与账号区服管理
