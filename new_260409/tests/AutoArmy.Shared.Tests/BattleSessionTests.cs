using Xunit;
using GameEntry.AutoArmy.Server;
using GameEntry.AutoArmy.Shared;

namespace AutoArmy.Shared.Tests;

public class BattleSessionTests
{
    [Fact]
    public void FixedDebugBattle_ResolvesToSingleWinner()
    {
        var session = BattleSession.CreateFixedDebugSession();

        var snapshot = session.RunToCompletion(deltaTimeSeconds: 0.25f, maxTicks: 240);

        Assert.True(snapshot.IsFinished);
        Assert.Equal(BattleOutcome.Victory, snapshot.Outcome);
        Assert.Equal(0, snapshot.EnemyAliveCount);
        Assert.True(snapshot.AllyAliveCount > 0);
    }

    [Fact]
    public void Tick_GeneratesSnapshotForCurrentBattleState()
    {
        var session = BattleSession.CreateFixedDebugSession();

        var snapshot = session.Tick(0.25f);

        Assert.Equal(1, snapshot.Tick);
        Assert.Equal(0.25f, snapshot.ElapsedSeconds, 3);
        Assert.Equal(6, snapshot.Units.Length);
        Assert.Equal(BattleOutcome.InProgress, snapshot.Outcome);
    }
}
