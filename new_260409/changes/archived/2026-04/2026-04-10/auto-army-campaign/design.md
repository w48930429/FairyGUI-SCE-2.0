# 自动军团推图架构设计

## 目标

本 change 建立一个 2D 自动军团推图 RPG 的第一版垂直切片。玩家在局外升级英雄和兵种，进入关卡后双方阵容自动冲锋、自动索敌、自动攻击，直到一方全灭。

第一版重点是玩法骨架和架构边界，不追求正式美术、复杂布阵、长期存档或完整商业化养成。

## 当前决定

- 游戏形态：自动军团推图；玩家不做局内微操。
- 战场：竖向 2D 战线；敌方在上方向下，己方在下方向上。
- 阵容：第一版固定测试阵容；后续替换为赛前布阵和资源买兵。
- 推图：第一版线性关卡；关卡模型保留章节和节点字段。
- 胜负：唯一判定是一方全灭；英雄死亡不直接失败。
- 英雄：带技能的强大单位；第一版有被动技能和一个自动冷却主动技。
- 兵种：前中后排职业模型；克制表配置伤害倍率。
- 成长：局外金币升级英雄和兵种；局内不做击杀经验升级。
- 进度：服务端内存仓储先行；Repository 边界后续迁移到 CloudData。
- 表现：客户端 Canvas 占位渲染；后续接序列帧或 Spine 视觉组件。

## WasiCore 分层

继续使用项目现有 `ScopeData.GameDataGameMode.MapGameMode`。自动注册入口只在 `OnRegisterGameClass()` 中订阅初始化回调；需要分支游戏模式时，在回调内检查游戏模式，避免在注册阶段读取尚未可用的模式。

建议按条件编译划分：

- `SERVER`：战斗 tick、关卡生成、敌方阵容、进度、结算、升级、发出战斗快照。
- `CLIENT`：Canvas 战场、局外调试 UI、单位占位绘制、血条/等级/职业标识、技能提示。
- 共享：枚举、配置、属性公式、克制矩阵、组件数据、快照 DTO、消息 DTO。

## ECS-style 战斗核心

第一版可以在服务端实现轻量 battle world：用稳定的 `BattleUnitId` 标识战斗单位，用组件字典或单位状态对象承载组件。目标是获得 ECS 的边界，而不是把所有 WasiCore 对象改造成纯 ECS。

建议组件：

- `BattleTransform2D`：Canvas/战场坐标、朝向。
- `BattleTeamComponent`：己方/敌方。
- `BattleHealthComponent`：当前生命、最大生命、死亡标记。
- `BattleStatsComponent`：攻击、防御、射程、移速、攻速、技能强度。
- `BattleAttackComponent`：普攻计时、攻击间隔、当前目标。
- `BattleMovementComponent`：推进方向、停止距离、移动状态。
- `BattleTargetingComponent`：索敌半径、目标偏好、当前目标。
- `BattleRoleComponent`：英雄/小兵、职业、等级、配置 ID。
- `PassiveSkillComponent`：英雄或单位携带的被动效果 ID。
- `AutoCastSkillComponent`：自动主动技、冷却、范围、目标规则。
- `BattleVisualStateComponent`：表现状态，例如 Idle/Run/Attack/Cast/Hurt/Dead。

系统按固定顺序 tick：目标清理 -> 被动/光环刷新 -> 索敌 -> 移动 -> 普攻 -> 自动技能 -> 伤害/死亡 -> 胜负结算 -> 快照发布。

## 关卡和局外进度

`PlayerProgress` 第一版字段建议包括：金币、最高解锁线性关卡、当前英雄等级、兵种等级表、已部署固定测试阵容版本。

`StageDefinition` 第一版字段建议包括：章节 ID、线性关卡号、可选地图节点 ID、敌方阵容、敌方站位、奖励金币、推荐战力、后继关卡列表。第一版只使用线性下一关；不要提前实现分叉地图 UI。

升级接口只接受局外请求。升级英雄或兵种时，服务端读取进度、检查金币、扣费、提升等级、保存进度。战斗开始时把等级烘焙成该局的初始属性；战斗中不改变局外等级。

## 兵种和克制

第一版建议四个职业：

- `Guard`：前排抗伤，低输出，高生命/防御。
- `Striker`：近战输出，中生命，高近战伤害。
- `Ranger`：后排远程，低生命，远射程。
- `Caster`：技能输出，低普攻，依赖自动技能。

克制由 `RoleAdvantageTable` 提供 `GetDamageMultiplier(attackerRole, defenderRole)`。普攻和技能都走同一个伤害计算入口，入口接收攻击者、受击者、基础伤害、伤害标签，再叠加等级属性、克制、被动、护盾/减伤。

## 英雄技能

第一版需要验证两种技能通路：

- 被动：例如己方 `Ranger` 攻击提升，或英雄自己开场获得护盾。
- 自动主动：例如每 5 秒向当前目标释放一次火球，造成一次技能伤害。

技能目标由服务端选择。客户端只从快照看见 `VisualState = Cast` 或一个短暂技能事件；不要在客户端决定技能是否命中。

## 表现和动画演进

第一版 Canvas 绘制占位图形：不同职业不同颜色/形状，英雄加外框或图标，血条和等级贴近单位。渲染读取 `BattleSnapshot`，不要直接读取服务端 battle world。

后续替换路线：

- 序列帧：新增 `SpriteSheetAnimationComponent` 和客户端播放器。
- Spine：新增 `SpineAnimationComponent` 和资源/控件适配。
- 技能特效：新增瞬态 visual event，不创建权威战斗单位。

## 第一版不做

- 不做局内手操。
- 不把英雄死亡当成失败。
- 不接 CloudData。
- 不做赛前布阵 UI。
- 不做资源买兵 UI。
- 不接正式序列帧或 Spine。
- 不修改生成目录：`src/DataGenerated/`、`src/TriggerGenerated/`。
- 不创建新的 GameMode。
