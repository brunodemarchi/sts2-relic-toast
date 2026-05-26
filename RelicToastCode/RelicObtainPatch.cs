using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace RelicToast;

[HarmonyPatch(typeof(RelicCmd), nameof(RelicCmd.Obtain), typeof(RelicModel), typeof(Player), typeof(int))]
internal static class RelicObtainPatch
{
    [HarmonyPostfix]
    private static void Postfix(Task<RelicModel> __result, Player player)
    {
        RelicToastDebugLog.Write("RelicCmd.Obtain postfix fired.");

        if (!RelicToastConfig.Enabled)
        {
            RelicToastDebugLog.Write("Relic toast skipped because it is disabled.");
            return;
        }

        if (LocalContext.NetId.HasValue && !LocalContext.IsMe(player))
        {
            RelicToastDebugLog.Write("Relic toast skipped for remote multiplayer player.");
            return;
        }

        _ = ShowWhenComplete(__result, player);
    }

    private static async Task ShowWhenComplete(Task<RelicModel> resultTask, Player player)
    {
        try
        {
            var relic = await resultTask;
            RelicToastDebugLog.Write($"Relic obtain completed: {relic.Id} / {relic.Title.GetFormattedText()}.");

            if (!RelicToastConfig.Enabled)
            {
                RelicToastDebugLog.Write("Relic toast skipped after await because it is disabled.");
                return;
            }

            CompactRelicToast.Show(relic);
        }
        catch (Exception ex)
        {
            RelicToastMod.Logger.Warn($"Relic toast failed: {ex.Message}");
            RelicToastDebugLog.Write($"Relic toast failed in obtain patch: {ex}");
        }
    }
}

[HarmonyPatch(typeof(RelicReward), "OnSelect")]
internal static class RelicRewardOnSelectPatch
{
    [HarmonyPostfix]
    private static void Postfix(RelicReward __instance, Task<bool> __result)
    {
        RelicToastDebugLog.Write($"RelicReward.OnSelect postfix fired: rewardRelic={__instance.Relic?.Id}; claimedRelic={__instance.ClaimedRelic?.Id}.");

        if (!RelicToastConfig.Enabled)
        {
            RelicToastDebugLog.Write("RelicReward.OnSelect toast skipped because it is disabled.");
            return;
        }

        if (LocalContext.NetId.HasValue && !LocalContext.IsMe(__instance.Player))
        {
            RelicToastDebugLog.Write("RelicReward.OnSelect toast skipped for remote multiplayer player.");
            return;
        }

        _ = ShowWhenSelected(__instance, __result);
    }

    private static async Task ShowWhenSelected(RelicReward reward, Task<bool> resultTask)
    {
        try
        {
            var selected = await resultTask;
            var relic = reward.ClaimedRelic ?? reward.Relic;
            RelicToastDebugLog.Write($"RelicReward.OnSelect completed: selected={selected}; relic={relic?.Id}.");

            if (!selected || relic == null)
            {
                return;
            }

            if (!RelicToastConfig.Enabled)
            {
                RelicToastDebugLog.Write("RelicReward.OnSelect toast skipped after await because it is disabled.");
                return;
            }

            CompactRelicToast.Show(relic);
        }
        catch (Exception ex)
        {
            RelicToastMod.Logger.Warn($"Relic reward toast failed: {ex.Message}");
            RelicToastDebugLog.Write($"Relic toast failed in reward select patch: {ex}");
        }
    }
}
