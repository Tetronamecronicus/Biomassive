using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Stray.Utilitys.LogMessageSender;

namespace Content.Server.Stray.Utilitys.LogMessageSender;

public sealed class LogMessageSenderSystem : SharedLogMessageSenderSystem
{
    public override void Initialize()
    {
        base.Initialize();
    }
    public void LogError(string message)
    {
        Log.Error(message);
    }
    public void LogWarn(string message)
    {
        Log.Warning(message);
    }
    public void LogInfo(string message)
    {
        Log.Info(message);
    }
    public void LogFatal(string message)
    {
        Log.Fatal(message);
    }
    public void LogDebug(string message)
    {
        Log.Debug(message);
    }
    public void LogVerbose(string message)
    {
        Log.Verbose(message);
    }
}
