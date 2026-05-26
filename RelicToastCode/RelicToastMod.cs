using System.Reflection;
using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace RelicToast;

[ModInitializer(nameof(Initialize))]
public static class RelicToastMod
{
    public const string ModId = "RelicToast";

    internal static Logger Logger { get; } = new(ModId, LogType.Generic);
    internal static RelicToastConfig Config { get; } = new();

    private static Harmony? _harmony;

    public static void Initialize()
    {
        if (_harmony != null)
        {
            return;
        }

        Logger.Info("Initializing Relic Toast.");
        RelicToastDebugLog.Write("Initializing Relic Toast.");

        _harmony = new Harmony(ModId);
        _harmony.PatchAll(Assembly.GetExecutingAssembly());
        LogAppliedPatches();
        ModConfigRegistry.Register(ModId, Config);

        Logger.Info("Relic Toast initialized.");
        RelicToastDebugLog.Write("Relic Toast initialized.");
    }

    private static void LogAppliedPatches()
    {
        var patchedMethods = (_harmony?.GetPatchedMethods() ?? [])
            .Where(method => Harmony.GetPatchInfo(method)?.Owners.Contains(ModId) == true)
            .ToArray();
        var patchedRelicObtain = patchedMethods.Any(method =>
            method.DeclaringType == typeof(RelicCmd)
            && method.Name == nameof(RelicCmd.Obtain)
            && method.GetParameters() is
            [
                { ParameterType: var relicType },
                { ParameterType: var playerType },
                { ParameterType: var indexType }
            ]
            && relicType == typeof(RelicModel)
            && playerType == typeof(Player)
            && indexType == typeof(int)
        );
        var patchedRelicReward = patchedMethods.Any(method =>
            method.DeclaringType == typeof(RelicReward)
            && method.Name == "OnSelect"
        );

        Logger.Info($"Relic Toast applied {patchedMethods.Length} Harmony patch(es).");
        RelicToastDebugLog.Write($"Applied {patchedMethods.Length} Harmony patch(es); patched relic obtain: {patchedRelicObtain}; patched relic reward select: {patchedRelicReward}.");

        if (!patchedRelicObtain || !patchedRelicReward)
        {
            Logger.Warn("Relic Toast did not patch every expected relic hook.");
            RelicToastDebugLog.Write("WARNING: did not patch every expected relic hook.");
        }
    }
}
