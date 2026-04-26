#if CLIENT
namespace GameEntry;

internal static class FguiNotificationBridge
{
    public static void EnqueueSystemTip(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Pure-FGUI migration mode: avoid cross-system hard dependency.
        Game.Logger.LogInformation("[FGUI][TIP] {Message}", message);
    }
}
#endif
