using System.Reflection;
using HarmonyLib;

public static class HSTraderLock
{
    public static void Patch(Harmony harmony)
    {
        var prefix = new HarmonyMethod(typeof(HSTraderLock), nameof(DenyLock));
        // AccessTools logs a warning when the method is missing. One of these exists per game version.
        var m32 = Find("CanLockOnServer");
        if (m32 != null)
            harmony.Patch(m32, prefix: prefix);
        var m33 = Find("OnLockRequestServer");
        if (m33 != null)
            harmony.Patch(m33, prefix: prefix);
    }

    static MethodInfo Find(string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        for (var t = typeof(EntityTrader); t != null; t = t.BaseType)
        {
            var methods = t.GetMethods(flags);
            for (int i = 0; i < methods.Length; i++)
                if (methods[i].Name == name)
                    return methods[i];
        }
        return null;
    }

    static bool DenyLock(EntityTrader __instance, ref bool __result)
    {
        if (!HSTraderSnapshot.IsLockdown || !HSTraderSnapshot.IsMissionTrader(__instance))
            return true;
        __result = false;
        return false;
    }
}
