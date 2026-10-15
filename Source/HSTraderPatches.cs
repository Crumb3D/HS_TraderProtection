using System.Reflection;
using HarmonyLib;
using UnityEngine;

public static class HSTraderRuntime
{
    [System.ThreadStatic]
    static bool skip;

    public static bool Skip
    {
        get { return skip; }
        set { skip = value; }
    }

    public static bool IsEnemySource(int sourceEntityId)
    {
        var world = GameManager.Instance != null ? GameManager.Instance.World : null;
        if (world == null)
            return false;
        return world.GetEntity(sourceEntityId) is EntityEnemy;
    }

    public static bool IsEnemySource(DamageSource source)
    {
        if (source == null)
            return false;
        return IsEnemySource(source.getEntityId());
    }
}

[HarmonyPatch(typeof(EntityTrader), "CanDamageEntity")]
public static class HSTraderCanDamage
{
    static bool Prefix(EntityTrader __instance, int _sourceEntityId, ref bool __result)
    {
        if (HSTraderRuntime.Skip)
            return true;
        if (!HSTraderSnapshot.IsRaid || !HSTraderSnapshot.IsMissionTrader(__instance))
            return true;
        __result = HSTraderRuntime.IsEnemySource(_sourceEntityId);
        return false;
    }
}

[HarmonyPatch(typeof(EntityTrader), "DamageEntity")]
public static class HSTraderDamage
{
    static bool Prefix(EntityTrader __instance, DamageSource _damageSource, int _strength, bool _criticalHit, float _impulseScale, ref int __result)
    {
        if (HSTraderRuntime.Skip)
            return true;
        if (!HSTraderSnapshot.IsRaid || !HSTraderSnapshot.IsMissionTrader(__instance))
            return true;
        if (!HSTraderRuntime.IsEnemySource(_damageSource))
        {
            __result = 0;
            return false;
        }
        var npc = __instance.NPCInfo;
        int saved = npc != null ? npc.TraderID : 0;
        if (npc != null)
            npc.TraderID = -1;
        __instance.IsGodMode.Value = false;
        HSTraderRuntime.Skip = true;
        try
        {
            __result = __instance.DamageEntity(_damageSource, _strength, _criticalHit, _impulseScale);
        }
        finally
        {
            HSTraderRuntime.Skip = false;
            if (npc != null)
                npc.TraderID = saved;
        }
        if (__result > 0)
            HSTraderMission.OnTraderDamaged(__instance, __result, _damageSource);
        return false;
    }
}

[HarmonyPatch(typeof(EntityAlive), "Kill")]
public static class HSTraderKill
{
    static bool Prefix(EntityAlive __instance, DamageResponse _dmResponse)
    {
        var trader = __instance as EntityTrader;
        if (trader == null || !HSTraderSnapshot.IsRaid || !HSTraderSnapshot.IsMissionTrader(trader))
            return true;
        HSTraderMission.OnTraderLethal(trader, _dmResponse);
        return false;
    }
}

[HarmonyPatch(typeof(EntityTrader), "OnEntityActivated")]
public static class HSTraderActivated
{
    static bool Prefix(EntityTrader __instance, EntityPlayerLocal _playerFocusing)
    {
        if (!HSTraderSnapshot.IsLockdown || !HSTraderSnapshot.IsMissionTrader(__instance))
            return true;
        if (_playerFocusing != null)
            GameManager.ShowTooltip(_playerFocusing, Localization.Get("hsTpTraderBusy"), string.Empty, "ui_denied", null, true);
        return false;
    }
}

[HarmonyPatch(typeof(EntityTrader), "AllowActivationCommand")]
public static class HSTraderAllowActivate
{
    static bool Prefix(EntityTrader __instance, ref bool __result)
    {
        if (!HSTraderSnapshot.IsLockdown || !HSTraderSnapshot.IsMissionTrader(__instance))
            return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(EntityAlive), "checkForTeleportOutOfTraderArea")]
public static class HSTraderNoTeleport
{
    static bool Prefix(EntityAlive __instance)
    {
        return !HSTraderMission.ShouldSuppressTeleport(__instance);
    }
}

[HarmonyPatch(typeof(World), "IsWithinTraderPlacingProtection", new[] { typeof(Vector3i) })]
public static class HSTraderPlaceProtectPos
{
    static bool Prefix(World __instance, Vector3i _worldBlockPos, ref bool __result)
    {
        if (!HSTraderMission.ShouldDisableTraderProtect(__instance, _worldBlockPos))
            return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(World), "IsWithinTraderPlacingProtection", new[] { typeof(Bounds) })]
public static class HSTraderPlaceProtectBounds
{
    static bool Prefix(World __instance, Bounds _bounds, ref bool __result)
    {
        if (!HSTraderMission.ShouldDisableTraderProtect(__instance, _bounds))
            return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(World), "IsWithinTraderArea", new[] { typeof(Vector3i) })]
public static class HSTraderAreaPos
{
    static bool Prefix(World __instance, Vector3i _worldBlockPos, ref bool __result)
    {
        if (!HSTraderMission.ShouldDisableTraderProtect(__instance, _worldBlockPos))
            return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(World), "IsWithinTraderArea", new[] { typeof(Vector3i), typeof(Vector3i) })]
public static class HSTraderAreaBox
{
    static bool Prefix(World __instance, Vector3i _minPos, Vector3i _maxPos, ref bool __result)
    {
        if (!HSTraderMission.ShouldDisableTraderProtect(__instance, _minPos, _maxPos))
            return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(World), "CanPlaceBlockAt", new[] { typeof(Vector3i), typeof(PersistentPlayerData), typeof(bool) })]
public static class HSTraderWorldCanPlace
{
    static bool Prefix(World __instance, Vector3i blockPos, PersistentPlayerData lpRelative, bool traderAllowed, ref bool __result)
    {
        if (traderAllowed || !HSTraderMission.ShouldDisableTraderProtect(__instance, blockPos))
            return true;
        __result = __instance.CanPlaceBlockAt(blockPos, lpRelative, true);
        return false;
    }
}

[HarmonyPatch(typeof(World), "CanPickupBlockAt")]
public static class HSTraderWorldCanPickup
{
    static bool Prefix(World __instance, Vector3i blockPos, PersistentPlayerData lpRelative, ref bool __result)
    {
        if (!HSTraderMission.ShouldDisableTraderProtect(__instance, blockPos))
            return true;
        __result = __instance.CanPlaceBlockAt(blockPos, lpRelative, true);
        return false;
    }
}

[HarmonyPatch(typeof(EAIApproachAndAttackTarget), "CanExecute")]
public static class HSTraderAiAttack
{
    static void Postfix(EAIApproachAndAttackTarget __instance, ref bool __result)
    {
        if (__result)
            return;
        if (!HSTraderSnapshot.IsRaid)
            return;
        var zombie = __instance.theEntity;
        if (zombie == null || !HSTraderMission.IsMissionZombie(zombie.entityId))
            return;
        var target = zombie.GetAttackTarget();
        if (target is EntityTrader trader && HSTraderSnapshot.IsMissionTrader(trader))
            __result = true;
    }
}

[HarmonyPatch(typeof(EntityAlive), "ProcessDamageResponseLocal")]
public static class HSTraderDamageStats
{
    static void Postfix(EntityAlive __instance, DamageResponse _dmResponse)
    {
        if (!HSTraderNet.IsAuthority)
            return;
        HSTraderStats.OnDamageResponse(__instance, _dmResponse);
    }
}

[HarmonyPatch(typeof(ItemActionRanged), "ConsumeAmmo")]
public static class HSTraderAmmo
{
    static void Postfix(ItemActionData _actionData)
    {
        if (!HSTraderNet.IsAuthority || _actionData == null)
            return;
        HSTraderStats.OnAmmoFired(HoldingPlayer(_actionData));
    }

    // 3.2: field holdingEntity. 3.3: property holdingEntity. Reflection so both resolve.
    static EntityPlayer HoldingPlayer(ItemActionData action)
    {
        if (action.invData == null)
            return null;
        object data = action.invData;
        var t = data.GetType();
        object ent = null;
        var prop = t.GetProperty("holdingEntity", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null)
            ent = prop.GetValue(data, null);
        else
        {
            var field = t.GetField("holdingEntity", BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
                ent = field.GetValue(data);
        }
        return ent as EntityPlayer;
    }
}
