# Auto Army Campaign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build the first vertical slice of a server-authoritative 2D auto-army campaign game on the existing WasiCore `MapGameMode`.

**Architecture:** Keep battle math in a small ECS-style battle world on the server, publish serializable snapshots, and render those snapshots with a client Canvas placeholder view. Keep progression behind a repository boundary: first implementation is server memory, later implementation can use CloudData.

**Tech Stack:** C# / .NET 9 / WasiCore / `Server-Debug` / `Client-Debug` / Canvas / OSpec.

---

## Guardrails

- Do not edit `src/DataGenerated/`.
- Do not edit `src/TriggerGenerated/`.
- Do not create a new game mode.
- Do not read `Game.GameModeLink` inside `OnRegisterGameClass()`.
- Do not use `Task.Run()`, `Task.Delay()`, `Thread`, or `Console.WriteLine`.
- Use `Game.Delay()` for async loops and `Game.Logger` for logs.
- Wrap server-only code with `#if SERVER`.
- Wrap client-only code with `#if CLIENT`.
- After code changes, update the affected `SKILL.md` and run `node build-index-auto.js`.

## Proposed Files

- Create: `src/AutoArmy/SKILL.md`
- Create: `src/AutoArmy/AutoArmyGameClass.cs`
- Create: `src/AutoArmy/Shared/BattleEnums.cs`
- Create: `src/AutoArmy/Shared/BattleComponents.cs`
- Create: `src/AutoArmy/Shared/BattleSnapshot.cs`
- Create: `src/AutoArmy/Shared/BattleBalance.cs`
- Create: `src/AutoArmy/Shared/CampaignProgress.cs`
- Create: `src/AutoArmy/Server/BattleSession.cs`
- Create: `src/AutoArmy/Server/BattleWorld.cs`
- Create: `src/AutoArmy/Server/BattleSystems.cs`
- Create: `src/AutoArmy/Server/CampaignService.cs`
- Create: `src/AutoArmy/Server/InMemoryPlayerProgressRepository.cs`
- Create: `src/AutoArmy/Client/BattleCanvasView.cs`
- Modify: `src/SKILL.md`
- Modify: `SKILL.md` if project-level navigation or scope changes.

## Task 1：冻结入口和生命周期

**Files:**
- Create: `src/AutoArmy/AutoArmyGameClass.cs`

**Steps:**
1. Read the current `src/TestTriggers.cs` once and decide whether it stays as a hello-world trigger for now.
2. Create an `IGameClass` entry file under `src/AutoArmy/`.
3. In `OnRegisterGameClass()`, subscribe to trigger/UI initialization callbacks only; do not read `Game.GameModeLink` there.
4. In the callback, return unless current game mode is the existing `ScopeData.GameDataGameMode.MapGameMode`.
5. Add server/client logs with `Game.Logger.LogInformation`.
6. Build server: `dotnet build src/GameEntry.csproj -c Server-Debug`.
7. Build client: `dotnet build src/GameEntry.csproj -c Client-Debug`.

Expected: both builds pass, and no generated directory is edited.

## Task 2：实现共享配置和组件数据

**Files:**
- Create: `src/AutoArmy/Shared/BattleEnums.cs`
- Create: `src/AutoArmy/Shared/BattleComponents.cs`
- Create: `src/AutoArmy/Shared/BattleBalance.cs`
- Create: `src/AutoArmy/Shared/BattleSnapshot.cs`

**Steps:**
1. Define `BattleTeam`, `BattleUnitRole`, `BattleUnitKind`, `BattleVisualState`, `BattleOutcome`.
2. Define simple component structs/classes for transform, health, stats, attack, movement, targeting, role, passive skill, auto-cast skill, visual state.
3. Add `RoleAdvantageTable` with a small configurable multiplier table.
4. Add a pure damage formula that receives attacker role, defender role, base damage, attack, defense, and multiplier.
5. Add `BattleSnapshot` and `BattleUnitSnapshot` DTOs with only primitive/serializable fields.
6. Build both configurations.

Expected: shared code compiles in both `SERVER` and `CLIENT`.

## Task 3：实现服务端战斗 world 和系统

**Files:**
- Create: `src/AutoArmy/Server/BattleWorld.cs`
- Create: `src/AutoArmy/Server/BattleSystems.cs`
- Create: `src/AutoArmy/Server/BattleSession.cs`

**Steps:**
1. Build a battle world that can spawn units from initial definitions and assign stable unit IDs.
2. Implement target cleanup and nearest-enemy targeting.
3. Implement vertical movement: top team moves toward increasing Y; bottom team moves toward decreasing Y.
4. Implement attack range stop and basic attack cooldown.
5. Implement damage, death marking, and all-dead victory condition.
6. Implement passive application for one hero passive.
7. Implement one auto-cast skill with cooldown and current-target selection.
8. Generate a snapshot after each tick.
9. Add a deterministic smoke method or debug path that simulates one fixed battle and logs winner.
10. Build server after each coherent system.

Expected: fixed formations can resolve to exactly one winner; outcome only depends on all-dead checks.

## Task 4：实现推图进度和金币升级

**Files:**
- Create: `src/AutoArmy/Shared/CampaignProgress.cs`
- Create: `src/AutoArmy/Server/CampaignService.cs`
- Create: `src/AutoArmy/Server/InMemoryPlayerProgressRepository.cs`

**Steps:**
1. Define `PlayerProgress`: gold, highest unlocked stage, hero levels, troop levels.
2. Define `StageDefinition`: chapter, stage, optional node, enemy formation, reward gold, next stage IDs.
3. Create a repository interface and in-memory implementation.
4. Implement hero upgrade with gold check, cost, level increment, and save.
5. Implement troop upgrade with gold check, cost, level increment, and save.
6. Implement stage completion: award gold and unlock next linear stage only on victory.
7. Keep CloudData out of the implementation.
8. Build server.

Expected: progression state changes on the server only and can affect the next battle's initial stats.

## Task 5：发布/读取战斗快照的最小通路

**Files:**
- Modify: `src/AutoArmy/AutoArmyGameClass.cs`
- Modify: `src/AutoArmy/Server/BattleSession.cs`
- Create or modify a shared DTO file if the chosen messaging path requires it.

**Steps:**
1. Read `docs/sdk/systems/typedmessagesystem.md` and the relevant API declarations before choosing message calls.
2. Prefer typed messages or an existing WasiCore sync primitive; avoid ad-hoc string serialization.
3. Publish snapshots at a controlled rate, not every tiny internal operation.
4. Include battle status, winner, elapsed time, unit snapshots, and short visual events.
5. Build both configurations.

Expected: client has access to the latest authoritative battle snapshot.

## Task 6：实现客户端 Canvas 占位战场

**Files:**
- Create: `src/AutoArmy/Client/BattleCanvasView.cs`
- Modify: `src/AutoArmy/AutoArmyGameClass.cs`

**Steps:**
1. Read `docs/sdk/ai/skills/canvas-2d-game/skill.md` and `docs/sdk/systems/canvasdrawingsystem.md` before writing Canvas calls.
2. Create a full-screen `CanvasAnimated`.
3. Cache the latest `BattleSnapshot`.
4. Draw background bands, center battle lane, unit markers, heroes, health bars, levels, role labels, and cast flashes.
5. Use `ResetState()` before dynamic drawing.
6. Do not implement local combat authority on the client.
7. Build client.

Expected: the client can display the current battle with placeholders.

## Task 7：更新项目 knowledge 和 OSpec closeout files

**Files:**
- Modify: `src/SKILL.md`
- Create: `src/AutoArmy/SKILL.md`
- Modify: `changes/active/auto-army-campaign/verification.md`
- Modify: `changes/active/auto-army-campaign/review.md`

**Steps:**
1. Document the new AutoArmy module and its server/shared/client boundaries.
2. Add navigation from `src/SKILL.md`.
3. Run `node build-index-auto.js`.
4. Run `ospec index check .`.
5. Run `ospec verify changes/active/auto-army-campaign`.

Expected: OSpec knows about the new module, index is current, verification notes match reality.

## Task 8：Final verification

**Commands:**

```bash
dotnet build src/GameEntry.csproj -c Server-Debug
dotnet build src/GameEntry.csproj -c Client-Debug
ospec verify changes/active/auto-army-campaign
ospec changes status .
```

Expected: both builds pass. Any remaining OSpec warning is documented and is not misreported as complete.
