---
feature: ui-reopen-bring-to-front
created: 2026-08-17
status: blocked
optional_steps: []
passed_optional_steps: []
---

## 相关验证

- [x] 已记录实际运行的测试或检查命令及其结果
- [x] 验收标准已逐项确认
- [x] proposal 声明的真实文档更新已完成，或已确认 `documentation_impact: none`
- [ ] 没有未解决的阻塞问题
- [ ] 可以归档

## 命令与结果

- `dotnet test tests/FairyGUI.Tests/FairyGUI.Tests.csproj --filter FullyQualifiedName~UIRuntimeRootOrderingTests`：RED 时 1 失败、1 通过；加入全屏根复用重挂载后 GREEN，2 通过。
- `dotnet test tests/FairyGUI.Tests/FairyGUI.Tests.csproj`：4 通过，0 失败，0 跳过。
- `dotnet build src/GameEntry.csproj -c Client-Debug`：成功，0 警告，0 错误；构建目标已同步 `ui/AppBundle/managed/GameEntry.dll`。

## 验收映射

- Window 重开：`Show_ReopensWindowByRemountingItsNativeRoot` 断言相同原生根有两次固定尺寸挂载。
- 全屏根重开：`AddToFullScreenRoot_ReopensExistingContentByRemountingItsRoot` 断言复用同一根并有两次根挂载。
- 关闭路径：`RemoveFromRoot_DisposesFullScreenContentAndUnregistersItsRoot` 断言全屏注册与顶层根均被移除。
- 文档：`documentation_impact: none`，因为修复未改变公开 API、模块规则或用户操作流程；无需更新真实项目文档。

## 阻塞项

- `ospec verify changes/active/ui-reopen-bring-to-front` 的内容、任务、验证、文档合同和 review 检查均通过；仅 `change.workspace_scope` 失败。项目 `new_260409` 嵌套在父级 Git 根目录下，OSpec 将脏路径表示为 `new_260409/...`，但以项目根相对的 proposal `affects` 匹配，导致当前 change 的文件与历史未提交文件均被误判为范围外。未执行强制归档；需清理、提交或隔离父级 Git 工作区的无关脏文件，或由用户显式授权强制归档。
