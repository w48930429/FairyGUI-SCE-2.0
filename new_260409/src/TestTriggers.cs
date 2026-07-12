using GameCore.GameSystem.Data;

namespace GameEntry;

public class TestTriggers : IGameClass
{
    private static void Game_OnGameTriggerInitialization()
    {
        new Trigger<EventGameStart>(static async (s, d) =>
        {
            Game.Logger.LogInformation("Hello World!");
#if SERVER
            Game.Logger.LogInformation("This is running on the server side.");
#elif CLIENT
            Game.Logger.LogInformation("This is running on the client side.");
#endif
        }).Register(Game.Instance);
    }

    public static void OnRegisterGameClass()
    {
        // GameCore.ScopeData 是框架内置数据，非 GSC 别名（GSC = gamesparkcore.ScopeData）
        if (GameDataGlobalConfig.TestGameMode != GameCore.ScopeData.GameMode.Default)
        {
            return;
        }
        Game.OnGameTriggerInitialization += Game_OnGameTriggerInitialization;
    }
}
