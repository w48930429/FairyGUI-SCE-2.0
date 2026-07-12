---
feature: auto-army-server-selector-ui
created: 2026-04-10
status: verifying
optional_steps: []
passed_optional_steps: []
---

## 自动验证

- [x] build 通过
- [x] lint 通过
- [x] test 通过
- [x] 索引已重新生成
- [x] spec-check 通过

## 项目联动检查

- [x] 项目概览: [docs/project/overview.md](../../../../../docs/project/overview.md)
- [x] 技术栈: [docs/project/tech-stack.md](../../../../../docs/project/tech-stack.md)
- [x] 架构说明: [docs/project/architecture.md](../../../../../docs/project/architecture.md)
- [x] 模块地图: [docs/project/module-map.md](../../../../../docs/project/module-map.md)
- [x] API 总览: [docs/project/api-overview.md](../../../../../docs/project/api-overview.md)

- [x] datagenerated 模块技能: [src/modules/datagenerated/SKILL.md](../../../../../src/modules/datagenerated/SKILL.md)
- [x] triggergenerated 模块技能: [src/modules/triggergenerated/SKILL.md](../../../../../src/modules/triggergenerated/SKILL.md)

- [x] module-datagenerated: [docs/api/module-datagenerated.md](../../../../../docs/api/module-datagenerated.md)
- [x] module-triggergenerated: [docs/api/module-triggergenerated.md](../../../../../docs/api/module-triggergenerated.md)

## 需求验收

- [x] 客户端可在 UI 中点击选择 `S1/S2/S3`
- [x] 服务端校验后广播当前区服状态
- [x] 战斗流程使用 `debug-player@<serverId>` 作为进度 key
- [x] 测试与双端构建通过

## 结果

- [x] 可以归档

当前已执行：
- `dotnet test tests/AutoArmy.Shared.Tests/AutoArmy.Shared.Tests.csproj`
- `dotnet build src/GameEntry.csproj -c Server-Debug`
- `dotnet build src/GameEntry.csproj -c Client-Debug`
