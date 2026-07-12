# Auto Army Session Routing and Persistence Design

## 1. 目标

本设计聚焦两个 P0 问题：
- 多玩家并发时的消息隔离（避免广播串流）
- 进度可恢复（避免服务重启丢档）

约束：
- 继续保持服务端权威
- 保持现有 `MapGameMode` 和战斗快照模型不变
- 允许增量迁移，不一次性推翻当前实现

## 2. 方案对比与结论

### 方案 A：继续全局广播，客户端自行过滤  
优点：改动最小。  
缺点：安全性和一致性差，客户端过滤不可靠。  

### 方案 B：按玩家定向发送 + 会话路由表（推荐）  
优点：职责清晰、扩展性好、最符合权威模型。  
缺点：需要维护路由映射生命周期。  

### 方案 C：为每个玩家开独立频道对象  
优点：隔离彻底。  
缺点：系统复杂度和运维成本更高。  

结论：采用方案 B。

## 3. 会话路由设计

新增 `PlayerSessionContext`：
- `PlayerId`
- `ServerId`
- `SessionId`
- `ConnectedPlayerRef`
- `LastSeenUtc`

新增 `ISessionRouter`：
- `Bind(player, playerId, serverId)`
- `Resolve(playerId, serverId)`
- `Unbind(sessionId)`

消息发送策略：
- 状态消息默认走 `SendTo(session)`，禁止直接 `Broadcast`
- 仅公共信息（如全局系统公告）允许广播

## 4. 持久化仓储设计

抽象 `IPlayerProgressStore`（替代当前纯内存存储）：
- `GetOrCreateAsync(playerKey)`
- `SaveAsync(progress, expectedVersion)`

实现分层：
- `InMemoryPlayerProgressStore`（本地调试）
- `CloudDataPlayerProgressStore`（正式）

一致性策略：
- 读取失败：回退内存缓存 + 告警日志
- 写入失败：重试（指数退避）+ 失败计数指标
- 冲突写入：乐观锁版本号 + 读后重试

## 5. 迁移步骤

1. 引入路由接口和 in-memory 路由实现；消息发送先双写（定向 + 旧广播）用于观察。  
2. 新增持久化仓储接口；现有内存仓储适配新接口。  
3. 接入 CloudData 实现并通过配置切换。  
4. 观察稳定后移除旧广播路径。  

## 6. 测试策略

- 单元测试：路由绑定/解绑、会话过期清理、仓储冲突重试
- 集成测试：双玩家并发不串流、重启后进度恢复
- 回归测试：现有战役闭环与区服选择不退化
