# Auto Army Pathfinding Plan

## Goal

在现有 AutoArmy 战斗循环中补齐第一版“局部寻路”：单位遇到前方友军阻挡时可侧移绕行，并保持服务端权威。

## Scope

- 服务端 `BattleSystems` 增强移动逻辑
- 目标选择与攻击判定统一到 2D 距离
- 增加对应单元测试

## Implementation Steps

1. 在移动系统增加“前方阻挡检测”和“侧向绕行”逻辑，单位在阻塞时优先侧移，非阻塞时回归职业车道并继续前进。  
2. 将最近目标搜索和攻击距离判定从 Y 轴绝对值改为 2D 欧式距离。  
3. 新增测试：  
   - 阻挡场景下单位产生侧向位移；  
   - 最近目标选择按 2D 距离工作。  
4. 运行 `dotnet test`、`Server-Debug`、`Client-Debug` 构建验证。  
5. 更新 change 文档与 `SKILL.md`，重建索引并执行 `ospec verify/finalize`。

## Non-goals

- 不做 A*、NavMesh、障碍物编辑工具
- 不改变客户端权威边界
