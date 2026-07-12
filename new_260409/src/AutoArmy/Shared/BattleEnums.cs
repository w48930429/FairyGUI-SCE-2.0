namespace GameEntry.AutoArmy.Shared;

public enum BattleTeam
{
    Ally = 0,
    Enemy = 1,
}

public enum BattleUnitRole
{
    Guard = 0,
    Striker = 1,
    Ranger = 2,
    Caster = 3,
}

public enum BattleUnitKind
{
    Soldier = 0,
    Hero = 1,
}

public enum BattleVisualState
{
    Idle = 0,
    Moving = 1,
    Attacking = 2,
    Casting = 3,
    Hurt = 4,
    Dead = 5,
}

public enum BattleOutcome
{
    InProgress = 0,
    Victory = 1,
    Defeat = 2,
    Draw = 3,
}

public enum BattleStatus
{
    Running = 0,
    Finished = 1,
}

public enum BattleVisualEventType
{
    AttackHit = 0,
    SkillCast = 1,
    SkillHit = 2,
    UnitDied = 3,
}

public enum BattleTargetPreference
{
    Nearest = 0,
    LowestHealth = 1,
    Frontline = 2,
}

public enum BattleSkillTargetRule
{
    CurrentTarget = 0,
    NearestEnemy = 1,
    LowestHealthEnemy = 2,
}
