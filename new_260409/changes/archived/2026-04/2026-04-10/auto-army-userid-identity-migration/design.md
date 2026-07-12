# Auto Army UserId Identity Migration Design

## 1. 目标

本设计用于收敛 AutoArmy 战役进度的身份主键，解决 legacy key 与用户身份并存导致的数据归属不稳定问题。

目标：
- 服务端统一使用 `Player.UserId` 作为战役身份来源
- 对历史 `debug-player`/`p:<slotId>` 进度做一次性迁移
- 未登录或无有效 `UserId` 时拒绝请求并返回明确错误

## 2. 方案对比与结论

### 方案 A：继续兼容多种身份键（debug/slot/user）
优点：改动小。  
缺点：身份边界持续模糊，后续鉴权与运营统计无法统一。

### 方案 B：仅允许 UserId，并提供 legacy 到 user 的迁移（推荐）
优点：身份单一、长期成本低、与平台账号体系一致。  
缺点：需要处理迁移时机与覆盖策略。

### 方案 C：先维持双写，后续再切换
优点：短期风险较低。  
缺点：双写窗口复杂，容易造成冲突和回滚成本。

结论：采用方案 B。

## 3. 身份与迁移设计

### 3.1 身份解析

- 服务端统一通过 `TryBuildUserIdentity(...)` 解析身份
- 只有 `sender.UserId > 0` 才构建 `u:<userId>` 身份
- 无有效 `UserId` 时立即返回 `OperationResultMessage`，错误码 `unauthenticated_user`

### 3.2 迁移触发

- 在关键战役请求入口（选服、进度查询、开战、结算确认、升级）中，身份解析通过后执行 `EnsureLegacyProgressMigrated(...)`
- 迁移目标键：`u:<userId>@<server>`
- legacy 候选键：
  - `p:<slotId>@<server>`
  - `debug-player@<server>`

### 3.3 覆盖保护

- 仅当目标用户进度仍为默认初始状态时才迁移
- 若目标已有有效进度，迁移跳过且不覆盖
- 迁移成功后写日志，便于排查线上迁移行为

## 4. 测试策略

- 新增单测覆盖：
  - legacy 有进度时可迁移到用户键
  - 用户键已有进度时不覆盖
- 回归执行：
  - `dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj`
  - `dotnet build src/GameEntry.csproj -c Server-Debug`
  - `dotnet build src/GameEntry.csproj -c Client-Debug`

## 5. 非目标

- 不改战斗公式与关卡配置
- 不引入新的账号系统或登录流程
- 不调整客户端战斗画面结构
