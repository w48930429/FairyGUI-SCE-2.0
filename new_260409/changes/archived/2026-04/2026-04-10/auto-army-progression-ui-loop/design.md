# Auto Army Progression UI Loop Design

## 1. 目标与边界

本设计用于补齐 AutoArmy 第一版可玩闭环：战前准备、开战、战后结算、升级再开战。  
核心原则：
- 服务端权威：战斗结果、升级扣费、关卡解锁只在服务端计算。
- 客户端展示：客户端只提交操作意图并渲染服务端回推状态。
- 最小侵入：复用现有 `BattleSnapshot`、`CampaignService`、区服选择通路。

不在本次设计范围：
- CloudData 持久化落地
- 新增复杂寻路或战斗机制
- 账号系统与真实跨服网络

## 2. 架构改动

### 2.1 服务端（`AutoArmyGameClass`）

新增战役流程控制器（可先以内聚私有方法实现）：
- 处理客户端请求：开战、升级英雄、升级兵种、查询进度、结算确认
- 维护玩家会话态：`Idle`、`InBattle`、`ResultPendingConfirm`
- 在非法状态请求时拒绝并回推错误码

状态机规则：
- `Idle` -> `InBattle`：仅 `StartStage` 且关卡已解锁
- `InBattle` -> `ResultPendingConfirm`：战斗结束后
- `ResultPendingConfirm` -> `Idle`：收到 `ConfirmResult`
- `Idle` 下允许升级；`InBattle` 下禁止升级

### 2.2 共享层（`Shared`）

新增/扩展 typed message DTO：
- `CampaignProgressQueryMessage`
- `CampaignProgressStateMessage`
- `StartStageRequestMessage`
- `StartStageResultMessage`
- `UpgradeHeroRequestMessage`
- `UpgradeTroopRequestMessage`
- `OperationResultMessage`（通用成功/失败 + 错误码）

错误码建议：
- `invalid_stage`
- `stage_locked`
- `battle_already_running`
- `insufficient_gold`
- `invalid_state`

### 2.3 客户端（`BattleCanvasView`）

在现有 Canvas 中新增三个 UI 区块：
- 战前面板：当前关卡、推荐战力、开始战斗按钮
- 升级面板：英雄/兵种等级、升级花费、点击升级
- 战后结算弹层：胜负、奖励、下一步操作（下一关或重开）

客户端逻辑：
- 启动时请求进度状态
- 用户点击操作发送 typed message
- 仅根据服务端回推状态更新 UI，不做本地权威结算

## 3. 数据流

1. 客户端发送 `CampaignProgressQueryMessage`  
2. 服务端回推 `CampaignProgressStateMessage`  
3. 客户端发送 `StartStageRequestMessage`  
4. 服务端校验后进入 battle loop 并持续广播 `BattleSnapshot`  
5. 战斗结束后服务端回推 `StartStageResultMessage` + 最新 `CampaignProgressStateMessage`  
6. 客户端确认结算，服务端会话回到 `Idle`

## 4. 测试策略

单元测试：
- 升级扣费与等级增长
- 状态机非法转移拒绝
- 未解锁关卡开战拒绝

集成测试：
- 消息请求-响应完整链路
- 战斗结束后结算与进度同步一致

回归验证：
- 现有战斗快照同步与区服选择不退化
- `Server-Debug` / `Client-Debug` 构建保持通过
