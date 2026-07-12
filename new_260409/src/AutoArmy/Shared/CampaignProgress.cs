namespace GameEntry.AutoArmy.Shared;

public sealed class PlayerProgress
{
    public string PlayerId { get; set; } = string.Empty;

    public int Gold { get; set; }

    public int HighestUnlockedStageId { get; set; } = 1;

    public Dictionary<int, int> HeroLevels { get; } = new();

    public Dictionary<BattleUnitRole, int> TroopLevels { get; } = new();

    public int GetHeroLevel(int heroConfigId)
    {
        return HeroLevels.TryGetValue(heroConfigId, out var level) ? NormalizeLevel(level) : 1;
    }

    public int GetTroopLevel(BattleUnitRole role)
    {
        return TroopLevels.TryGetValue(role, out var level) ? NormalizeLevel(level) : 1;
    }

    public void SetHeroLevel(int heroConfigId, int level)
    {
        HeroLevels[heroConfigId] = NormalizeLevel(level);
    }

    public void SetTroopLevel(BattleUnitRole role, int level)
    {
        TroopLevels[role] = NormalizeLevel(level);
    }

    public PlayerProgress Clone()
    {
        var clone = new PlayerProgress
        {
            PlayerId = PlayerId,
            Gold = Gold,
            HighestUnlockedStageId = HighestUnlockedStageId,
        };

        foreach (var pair in HeroLevels)
        {
            clone.HeroLevels[pair.Key] = NormalizeLevel(pair.Value);
        }

        foreach (var pair in TroopLevels)
        {
            clone.TroopLevels[pair.Key] = NormalizeLevel(pair.Value);
        }

        return clone;
    }

    private static int NormalizeLevel(int level)
    {
        return Math.Max(1, level);
    }
}

public sealed class StageDefinition
{
    public int ChapterId { get; set; }

    public int StageId { get; set; }

    public int StageOrder { get; set; }

    public string NodeId { get; set; } = string.Empty;

    public StageEnemyUnitDefinition[] EnemyFormation { get; set; } = [];

    public int RewardGold { get; set; }

    public int RecommendedPower { get; set; }

    public int[] NextStageIds { get; set; } = [];

    public StageDefinition Clone()
    {
        return new StageDefinition
        {
            ChapterId = ChapterId,
            StageId = StageId,
            StageOrder = StageOrder,
            NodeId = NodeId,
            EnemyFormation = EnemyFormation.Select(static unit => unit.Clone()).ToArray(),
            RewardGold = RewardGold,
            RecommendedPower = RecommendedPower,
            NextStageIds = [.. NextStageIds],
        };
    }
}

public sealed class StageEnemyUnitDefinition
{
    public int ConfigId { get; set; }

    public BattleUnitRole Role { get; set; }

    public BattleUnitKind Kind { get; set; } = BattleUnitKind.Soldier;

    public int Level { get; set; } = 1;

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float MaxHealth { get; set; }

    public float TargetSearchRadius { get; set; } = 20f;

    public float InitialAttackCooldownSeconds { get; set; }

    public float InitialSkillCooldownSeconds { get; set; }

    public BattleStatsComponent Stats { get; set; } = new();

    public PassiveSkillComponent? PassiveSkill { get; set; }

    public AutoCastSkillComponent? AutoCastSkill { get; set; }

    public StageEnemyUnitDefinition Clone()
    {
        return new StageEnemyUnitDefinition
        {
            ConfigId = ConfigId,
            Role = Role,
            Kind = Kind,
            Level = Math.Max(1, Level),
            PositionX = PositionX,
            PositionY = PositionY,
            MaxHealth = MaxHealth,
            TargetSearchRadius = TargetSearchRadius,
            InitialAttackCooldownSeconds = InitialAttackCooldownSeconds,
            InitialSkillCooldownSeconds = InitialSkillCooldownSeconds,
            Stats = CloneStats(Stats),
            PassiveSkill = ClonePassiveSkill(PassiveSkill),
            AutoCastSkill = CloneAutoCastSkill(AutoCastSkill),
        };
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

public sealed class CampaignStageSummary
{
    public int StageId { get; set; }

    public string NodeId { get; set; } = string.Empty;

    public int RecommendedPower { get; set; }

    public int RewardGold { get; set; }

    public bool IsUnlocked { get; set; }
}

public sealed class CampaignProgressSummary
{
    public int Gold { get; set; }

    public int HighestUnlockedStageId { get; set; }

    public int HeroLevel { get; set; }

    public int GuardLevel { get; set; }

    public int RangerLevel { get; set; }

    public int HeroUpgradeCost { get; set; }

    public int GuardUpgradeCost { get; set; }

    public int RangerUpgradeCost { get; set; }
}
