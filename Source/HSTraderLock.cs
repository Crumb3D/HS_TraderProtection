using System.Reflection;
using HarmonyLib;

public static class HSTraderLock
{
    public static void Patch(Harmony harmony)
    {
        var prefix = new HarmonyMethod(typeof(HSTraderLock), nameof(DenyLock));
        var m32 = AccessTools.Method(typeof(EntityTrader), "CanLockOnServer");
        if (m32 != null)
            harmony.Patch(m32, prefix: prefix);
        var m33 = AccessTools.Method(typeof(EntityTrader), "OnLockRequestServer");
        if (m33 != null)
            harmony.Patch(m33, prefix: prefix);
    }

    static bool DenyLock(EntityTrader __instance, ref bool __result)
    {
        if (!HSTraderSnapshot.IsLockdown || !HSTraderSnapshot.IsMissionTrader(__instance))
            return true;
        __result = false;
        return false;
    }
}
