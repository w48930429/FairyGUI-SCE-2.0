using GameEntry.AutoArmy.Shared;

namespace GameEntry.AutoArmy.Server;

public interface IPlayerProgressRepository
{
    PlayerProgress GetOrCreate(string playerId);

    void Save(PlayerProgress progress);
}

public sealed class CampaignService
{
    private const int HeroConfigId = 101;

    private readonly IPlayerProgressRepository _repository;
    private readonly Dictionary<int, StageDefinition> _stageDefinitions;

    public CampaignService(IPlayerProgressRepository repository, IEnumerable<StageDefinition>? stageDefinitions = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _stageDefinitions = (stageDefinitions ?? CreateDefaultStages())
            .Select(static stage => stage.Clone())
            .ToDictionary(static stage => stage.StageId);

        if (_stageDefinitions.Count == 0)
        {
            throw new ArgumentException("At least one stage must be configured.", nameof(stageDefinitions));
        }
    }

    public PlayerProgress GetProgress(string playerId)
    {
        return _repository.GetOrCreate(playerId);
    }

    public CampaignProgressSummary GetProgressSummary(string playerId)
    {
        var progress = _repository.GetOrCreate(playerId);
        var heroLevel = progress.GetHeroLevel(HeroConfigId);
        var guardLevel = progress.GetTroopLevel(BattleUnitRole.Guard);
        var rangerLevel = progress.GetTroopLevel(BattleUnitRole.Ranger);

        return new CampaignProgressSummary
        {
            Gold = progress.Gold,
            HighestUnlockedStageId = progress.HighestUnlockedStageId,
            HeroLevel = heroLevel,
            GuardLevel = guardLevel,
            RangerLevel = rangerLevel,
            HeroUpgradeCost = GetHeroUpgradeCost(heroLevel),
            GuardUpgradeCost = GetTroopUpgradeCost(guardLevel),
            RangerUpgradeCost = GetTroopUpgradeCost(rangerLevel),
        };
    }

    public IReadOnlyList<CampaignStageSummary> GetStageSummaries(string playerId)
    {
        var progress = _repository.GetOrCreate(playerId);
        var unlocked = Math.Max(1, progress.HighestUnlockedStageId);
        return _stageDefinitions.Values
            .OrderBy(static stage => stage.StageId)
            .Select(stage => new CampaignStageSummary
            {
                StageId = stage.StageId,
                NodeId = stage.NodeId,
                RecommendedPower = stage.RecommendedPower,
                RewardGold = stage.RewardGold,
                IsUnlocked = stage.StageId <= unlocked,
            })
            .ToArray();
    }

    public bool TryUpgradeHero(string playerId, int heroConfigId, out PlayerProgress updatedProgress)
    {
        var progress = _repository.GetOrCreate(playerId);
        var currentLevel = progress.GetHeroLevel(heroConfigId);
        var cost = GetHeroUpgradeCost(currentLevel);
        if (progress.Gold < cost)
        {
            updatedProgress = progress;
            return false;
        }

        progress.Gold -= cost;
        progress.SetHeroLevel(heroConfigId, currentLevel + 1);
        _repository.Save(progress);
        updatedProgress = progress;
        return true;
    }

    public bool TryUpgradeTroop(string playerId, BattleUnitRole troopRole, out PlayerProgress updatedProgress)
    {
        var progress = _repository.GetOrCreate(playerId);
        var currentLevel = progress.GetTroopLevel(troopRole);
        var cost = GetTroopUpgradeCost(currentLevel);
        if (progress.Gold < cost)
        {
            updatedProgress = progress;
            return false;
        }

        progress.Gold -= cost;
        progress.SetTroopLevel(troopRole, currentLevel + 1);
        _repository.Save(progress);
        updatedProgress = progress;
        return true;
    }

    public bool TryCompleteStage(string playerId, int stageId, BattleOutcome outcome, out PlayerProgress updatedProgress)
    {
        var progress = _repository.GetOrCreate(playerId);
        if (!_stageDefinitions.TryGetValue(stageId, out var stage))
        {
            updatedProgress = progress;
            return false;
        }

        if (stageId > progress.HighestUnlockedStageId || outcome != BattleOutcome.Victory)
        {
            updatedProgress = progress;
            return false;
        }

        progress.Gold += Math.Max(0, stage.RewardGold);
        var nextLinearStageId = stage.NextStageIds.FirstOrDefault();
        if (nextLinearStageId > progress.HighestUnlockedStageId)
        {
            progress.HighestUnlockedStageId = nextLinearStageId;
        }

        _repository.Save(progress);
        updatedProgress = progress;
        return true;
    }

    public StageDefinition GetStageDefinition(int stageId)
    {
        if (!_stageDefinitions.TryGetValue(stageId, out var stage))
        {
            throw new KeyNotFoundException($"Stage {stageId} is not configured.");
        }

        return stage.Clone();
    }

    public IReadOnlyList<BattleUnitDefinition> BuildFixedAllyFormation(string playerId)
    {
        var progress = _repository.GetOrCreate(playerId);
        var heroLevel = progress.GetHeroLevel(HeroConfigId);
        var guardLevel = progress.GetTroopLevel(BattleUnitRole.Guard);
        var rangerLevel = progress.GetTroopLevel(BattleUnitRole.Ranger);

        return
        [
            CreateScaledAllyUnit(
                configId: HeroConfigId,
                role: BattleUnitRole.Caster,
                kind: BattleUnitKind.Hero,
                level: heroLevel,
                positionY: 10f,
                baseMaxHealth: 44f,
                baseStats: new BattleStatsComponent
                {
                    Attack = 4f,
                    Defense = 2f,
                    AttackRange = 3.5f,
                    MoveSpeed = 1.5f,
                    AttackIntervalSeconds = 1.4f,
                    SkillPower = 8f,
                },
                passiveSkill: new PassiveSkillComponent
                {
                    SkillId = BattleSystems.RangerAuraSkillId,
                    Potency = 1f,
                    IsEnabled = true,
                },
                autoCastSkill: new AutoCastSkillComponent
                {
                    SkillId = BattleSystems.FireballSkillId,
                    Potency = 1f,
                    CooldownSeconds = 4f,
                    CastRange = 4.5f,
                    TargetRule = BattleSkillTargetRule.CurrentTarget,
                },
                initialSkillCooldownSeconds: 1f),
            CreateScaledAllyUnit(
                configId: 102,
                role: BattleUnitRole.Guard,
                kind: BattleUnitKind.Soldier,
                level: guardLevel,
                positionY: 11.5f,
                baseMaxHealth: 60f,
                baseStats: new BattleStatsComponent
                {
                    Attack = 6f,
                    Defense = 4f,
                    AttackRange = 1.4f,
                    MoveSpeed = 1.1f,
                    AttackIntervalSeconds = 1f,
                    SkillPower = 0f,
                }),
            CreateScaledAllyUnit(
                configId: 103,
                role: BattleUnitRole.Ranger,
                kind: BattleUnitKind.Soldier,
                level: rangerLevel,
                positionY: 13f,
                baseMaxHealth: 38f,
                baseStats: new BattleStatsComponent
                {
                    Attack = 9f,
                    Defense = 1.5f,
                    AttackRange = 5.5f,
                    MoveSpeed = 1.4f,
                    AttackIntervalSeconds = 1.2f,
                    SkillPower = 0f,
                }),
        ];
    }

    public IReadOnlyList<BattleUnitDefinition> BuildEnemyFormation(int stageId)
    {
        var stage = GetStageDefinition(stageId);
        return stage.EnemyFormation
            .Select(static definition => ToBattleUnitDefinition(definition, BattleTeam.Enemy))
            .ToArray();
    }

    public BattleSession CreateBattleSession(string playerId, int stageId)
    {
        var allyFormation = BuildFixedAllyFormation(playerId);
        var enemyFormation = BuildEnemyFormation(stageId);
        return BattleSession.CreateSession(allyFormation.Concat(enemyFormation), stageId: stageId);
    }

    public static IEnumerable<StageDefinition> CreateDefaultStages()
    {
        return
        [
            new StageDefinition
            {
                ChapterId = 1,
                StageId = 1,
                StageOrder = 1,
                NodeId = "1-1",
                RewardGold = 60,
                RecommendedPower = 110,
                NextStageIds = [2],
                EnemyFormation =
                [
                    new StageEnemyUnitDefinition
                    {
                        ConfigId = 201,
                        Role = BattleUnitRole.Guard,
                        Kind = BattleUnitKind.Soldier,
                        Level = 2,
                        PositionY = 0f,
                        MaxHealth = 56f,
                        Stats = new BattleStatsComponent
                        {
                            Attack = 6f,
                            Defense = 3.5f,
                            AttackRange = 1.4f,
                            MoveSpeed = 1.1f,
                            AttackIntervalSeconds = 1f,
                            SkillPower = 0f,
                        },
                    },
                    new StageEnemyUnitDefinition
                    {
                        ConfigId = 202,
                        Role = BattleUnitRole.Striker,
                        Kind = BattleUnitKind.Soldier,
                        Level = 2,
                        PositionY = -1.5f,
                        MaxHealth = 46f,
                        Stats = new BattleStatsComponent
                        {
                            Attack = 10f,
                            Defense = 2f,
                            AttackRange = 1.3f,
                            MoveSpeed = 1.8f,
                            AttackIntervalSeconds = 0.9f,
                            SkillPower = 0f,
                        },
                    },
                    new StageEnemyUnitDefinition
                    {
                        ConfigId = 203,
                        Role = BattleUnitRole.Caster,
                        Kind = BattleUnitKind.Soldier,
                        Level = 2,
                        PositionY = 1.5f,
                        MaxHealth = 36f,
                        Stats = new BattleStatsComponent
                        {
                            Attack = 4f,
                            Defense = 1.5f,
                            AttackRange = 3.8f,
                            MoveSpeed = 1.2f,
                            AttackIntervalSeconds = 1.5f,
                            SkillPower = 6f,
                        },
                        AutoCastSkill = new AutoCastSkillComponent
                        {
                            SkillId = BattleSystems.FireballSkillId,
                            Potency = 1f,
                            CooldownSeconds = 5f,
                            CastRange = 4f,
                            TargetRule = BattleSkillTargetRule.CurrentTarget,
                        },
                        InitialSkillCooldownSeconds = 2f,
                    },
                ],
            },
            new StageDefinition
            {
                ChapterId = 1,
                StageId = 2,
                StageOrder = 2,
                NodeId = "1-2",
                RewardGold = 90,
                RecommendedPower = 150,
                NextStageIds = [3],
                EnemyFormation =
                [
                    new StageEnemyUnitDefinition
                    {
                        ConfigId = 211,
                        Role = BattleUnitRole.Guard,
                        Kind = BattleUnitKind.Soldier,
                        Level = 3,
                        PositionY = -0.2f,
                        MaxHealth = 64f,
                        Stats = new BattleStatsComponent
                        {
                            Attack = 7f,
                            Defense = 4.2f,
                            AttackRange = 1.5f,
                            MoveSpeed = 1.15f,
                            AttackIntervalSeconds = 0.95f,
                            SkillPower = 0f,
                        },
                    },
                    new StageEnemyUnitDefinition
                    {
                        ConfigId = 212,
                        Role = BattleUnitRole.Striker,
                        Kind = BattleUnitKind.Soldier,
                        Level = 3,
                        PositionY = -1.8f,
                        MaxHealth = 52f,
                        Stats = new BattleStatsComponent
                        {
                            Attack = 11f,
                            Defense = 2.2f,
                            AttackRange = 1.35f,
                            MoveSpeed = 1.9f,
                            AttackIntervalSeconds = 0.85f,
                            SkillPower = 0f,
                        },
                    },
                    new StageEnemyUnitDefinition
                    {
                        ConfigId = 213,
                        Role = BattleUnitRole.Ranger,
                        Kind = BattleUnitKind.Soldier,
                        Level = 3,
                        PositionY = 1.8f,
                        MaxHealth = 42f,
                        Stats = new BattleStatsComponent
                        {
                            Attack = 10.5f,
                            Defense = 1.8f,
                            AttackRange = 5.6f,
                            MoveSpeed = 1.45f,
                            AttackIntervalSeconds = 1.15f,
                            SkillPower = 0f,
                        },
                    },
                ],
            },
            new StageDefinition
            {
                ChapterId = 1,
                StageId = 3,
                StageOrder = 3,
                NodeId = "1-3",
                RewardGold = 120,
                RecommendedPower = 190,
                NextStageIds = [],
                EnemyFormation =
                [
                    new StageEnemyUnitDefinition
                    {
                        ConfigId = 221,
                        Role = BattleUnitRole.Guard,
                        Kind = BattleUnitKind.Soldier,
                        Level = 4,
                        PositionY = -0.3f,
                        MaxHealth = 72f,
                        Stats = new BattleStatsComponent
                        {
                            Attack = 8f,
                            Defense = 5f,
                            AttackRange = 1.5f,
                            MoveSpeed = 1.2f,
                            AttackIntervalSeconds = 0.9f,
                            SkillPower = 0f,
                        },
                    },
                    new StageEnemyUnitDefinition
                    {
                        ConfigId = 222,
                        Role = BattleUnitRole.Caster,
                        Kind = BattleUnitKind.Hero,
                        Level = 4,
                        PositionY = 1.3f,
                        MaxHealth = 54f,
                        Stats = new BattleStatsComponent
                        {
                            Attack = 5f,
                            Defense = 2.5f,
                            AttackRange = 4f,
                            MoveSpeed = 1.3f,
                            AttackIntervalSeconds = 1.4f,
                            SkillPower = 11f,
                        },
                        AutoCastSkill = new AutoCastSkillComponent
                        {
                            SkillId = BattleSystems.FireballSkillId,
                            Potency = 1.2f,
                            CooldownSeconds = 3.8f,
                            CastRange = 4.8f,
                            TargetRule = BattleSkillTargetRule.LowestHealthEnemy,
                        },
                        InitialSkillCooldownSeconds = 1.5f,
                    },
                    new StageEnemyUnitDefinition
                    {
                        ConfigId = 223,
                        Role = BattleUnitRole.Striker,
                        Kind = BattleUnitKind.Soldier,
                        Level = 4,
                        PositionY = -2.2f,
                        MaxHealth = 58f,
                        Stats = new BattleStatsComponent
                        {
                            Attack = 12f,
                            Defense = 2.5f,
                            AttackRange = 1.35f,
                            MoveSpeed = 2f,
                            AttackIntervalSeconds = 0.8f,
                            SkillPower = 0f,
                        },
                    },
                ],
            },
        ];
    }

    public static int GetHeroUpgradeCost(int currentLevel)
    {
        return 40 + (Math.Max(1, currentLevel) - 1) * 20;
    }

    public static int GetTroopUpgradeCost(int currentLevel)
    {
        return 30 + (Math.Max(1, currentLevel) - 1) * 15;
    }

    private static BattleUnitDefinition CreateScaledAllyUnit(
        int configId,
        BattleUnitRole role,
        BattleUnitKind kind,
        int level,
        float positionY,
        float baseMaxHealth,
        BattleStatsComponent baseStats,
        PassiveSkillComponent? passiveSkill = null,
        AutoCastSkillComponent? autoCastSkill = null,
        float initialSkillCooldownSeconds = 0f)
    {
        var normalizedLevel = Math.Max(1, level);
        return new BattleUnitDefinition
        {
            ConfigId = configId,
            Team = BattleTeam.Ally,
            Role = role,
            Kind = kind,
            Level = normalizedLevel,
            PositionY = positionY,
            MaxHealth = ScaleHealth(baseMaxHealth, normalizedLevel, kind),
            Stats = ScaleStats(baseStats, normalizedLevel, kind),
            PassiveSkill = ClonePassiveSkill(passiveSkill),
            AutoCastSkill = CloneAutoCastSkill(autoCastSkill),
            InitialSkillCooldownSeconds = initialSkillCooldownSeconds,
        };
    }

    private static BattleUnitDefinition ToBattleUnitDefinition(StageEnemyUnitDefinition definition, BattleTeam team)
    {
        return new BattleUnitDefinition
        {
            ConfigId = definition.ConfigId,
            Team = team,
            Role = definition.Role,
            Kind = definition.Kind,
            Level = Math.Max(1, definition.Level),
            PositionX = definition.PositionX,
            PositionY = definition.PositionY,
            MaxHealth = Math.Max(1f, definition.MaxHealth),
            TargetSearchRadius = definition.TargetSearchRadius,
            InitialAttackCooldownSeconds = definition.InitialAttackCooldownSeconds,
            InitialSkillCooldownSeconds = definition.InitialSkillCooldownSeconds,
            Stats = CloneStats(definition.Stats),
            PassiveSkill = ClonePassiveSkill(definition.PassiveSkill),
            AutoCastSkill = CloneAutoCastSkill(definition.AutoCastSkill),
        };
    }

    private static BattleStatsComponent ScaleStats(BattleStatsComponent baseStats, int level, BattleUnitKind kind)
    {
        var normalizedLevel = Math.Max(1, level);
        var attackGrowth = kind == BattleUnitKind.Hero ? 0.18f : 0.12f;
        var defenseGrowth = kind == BattleUnitKind.Hero ? 0.14f : 0.1f;
        var skillGrowth = kind == BattleUnitKind.Hero ? 0.2f : 0.08f;
        var scale = normalizedLevel - 1;
        return new BattleStatsComponent
        {
            Attack = baseStats.Attack * (1f + attackGrowth * scale),
            Defense = baseStats.Defense * (1f + defenseGrowth * scale),
            AttackRange = baseStats.AttackRange,
            MoveSpeed = baseStats.MoveSpeed,
            AttackIntervalSeconds = baseStats.AttackIntervalSeconds,
            SkillPower = baseStats.SkillPower * (1f + skillGrowth * scale),
        };
    }

    private static float ScaleHealth(float baseHealth, int level, BattleUnitKind kind)
    {
        var normalizedLevel = Math.Max(1, level);
        var healthGrowth = kind == BattleUnitKind.Hero ? 0.2f : 0.14f;
        return Math.Max(1f, baseHealth * (1f + healthGrowth * (normalizedLevel - 1)));
    }

    private static BattleStatsComponent CloneStats(BattleStatsComponent source)
    {
        return new BattleStatsComponent
        {
            Attack = source.Attack,
            Defense = source.Defense,
            AttackRange = source.AttackRange,
            MoveSpeed = source.MoveSpeed,
            AttackIntervalSeconds = source.AttackIntervalSeconds,
            SkillPower = source.SkillPower,
        };
    }

    private static PassiveSkillComponent? ClonePassiveSkill(PassiveSkillComponent? source)
    {
        if (source is null)
        {
            return null;
        }

        return new PassiveSkillComponent
        {
            SkillId = source.SkillId,
            Potency = source.Potency,
            IsEnabled = source.IsEnabled,
        };
    }

    private static AutoCastSkillComponent? CloneAutoCastSkill(AutoCastSkillComponent? source)
    {
        if (source is null)
        {
            return null;
        }

        return new AutoCastSkillComponent
        {
            SkillId = source.SkillId,
            Potency = source.Potency,
            CooldownSeconds = source.CooldownSeconds,
            CooldownRemainingSeconds = source.CooldownRemainingSeconds,
            CastRange = source.CastRange,
            TargetRule = source.TargetRule,
        };
    }
}
