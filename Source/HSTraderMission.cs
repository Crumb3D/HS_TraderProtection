using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class HSTraderMission
{
    public const string LogPrefix = "[HSTraderProtection]";
    static readonly HashSet<string> placedBlocks = new HashSet<string>();
    static readonly HashSet<int> resultsClosed = new HashSet<int>();
    static bool ending;
    static bool eventsHooked;
    static float lastSave;
    static float lastHeartbeat;
    static World cachedWorld;

    public static bool IsTracking
    {
        get
        {
            return HSTraderNet.IsAuthority && (HSTraderSnapshot.Phase == HSTraderPhase.Preparing || HSTraderSnapshot.Phase == HSTraderPhase.Raiding);
        }
    }

    public static bool IsMissionZombie(int entityId)
    {
        return HSTraderSpawn.Contains(entityId);
    }

    public static EntityTrader GetTrader(World world)
    {
        if (world == null || HSTraderSnapshot.TraderEntityId <= 0)
            return null;
        return world.GetEntity(HSTraderSnapshot.TraderEntityId) as EntityTrader;
    }

    public static string Start(World world, EntityPlayer admin, int traderEntityId)
    {
        if (!HSTraderNet.IsAuthority)
            return "Trader Protection commands run on the server.";
        if (world == null)
            return "No world.";
        if (HSTraderSnapshot.Phase != HSTraderPhase.Idle)
            return "A mission is already running (" + HSTraderSnapshot.Phase + "). Stop it or close the results screen first.";
        EntityTrader trader = null;
        if (traderEntityId > 0)
            trader = world.GetEntity(traderEntityId) as EntityTrader;
        if (trader == null && admin != null)
        {
            var area = world.GetTraderAreaAt(World.worldToBlockPos(admin.position));
            if (area != null)
                trader = area.GetTrader();
        }
        if (trader == null || trader.NPCInfo == null || trader.NPCInfo.TraderID <= 0)
            return "Stand inside a trader compound, or pass a trader entity id.";
        var prefab = PrefabAt(world, trader.position);
        HSTraderStats.Reset();
        HSTraderSpawn.Clear();
        placedBlocks.Clear();
        resultsClosed.Clear();
        ending = false;
        HSTraderSnapshot.Clear();
        HSTraderSnapshot.Phase = HSTraderPhase.Preparing;
        HSTraderSnapshot.TraderEntityId = trader.entityId;
        HSTraderSnapshot.PrefabId = prefab != null ? prefab.id : -1;
        var bp = World.worldToBlockPos(trader.position);
        HSTraderSnapshot.TraderX = bp.x;
        HSTraderSnapshot.TraderY = bp.y;
        HSTraderSnapshot.TraderZ = bp.z;
        HSTraderSnapshot.PrepTicks = HSTraderConfig.PrepTicks;
        HSTraderSnapshot.ObjectiveTicks = HSTraderConfig.ObjectiveTicks;
        HSTraderSnapshot.RaidStartWorldTime = world.worldTime + HSTraderConfig.PrepTicks;
        HSTraderSnapshot.NowWorldTime = world.worldTime;
        HSTraderSnapshot.Health = trader.Health;
        HSTraderSnapshot.MaxHealth = trader.GetMaxHealth();
        trader.IsGodMode.Value = true;
        CloseTradeWindows(world);
        HookEvents();
        Save();
        HSTraderNet.BroadcastSnapshot();
        HSTraderNet.BroadcastTip("Trader Protection: preparation started. " + HSTraderSnapshot.RaidInString(HSTraderSnapshot.RemainingPrepTicks()));
        Log.Out(LogPrefix + " Mission started at trader " + trader.entityId + " raid at worldTime " + HSTraderSnapshot.RaidStartWorldTime);
        return "Preparation started. " + HSTraderSnapshot.RaidInString(HSTraderSnapshot.RemainingPrepTicks());
    }

    public static string Stop(World world)
    {
        if (!HSTraderNet.IsAuthority)
            return "Trader Protection commands run on the server.";
        if (HSTraderSnapshot.Phase == HSTraderPhase.Idle)
            return "No mission is running.";
        if (HSTraderSnapshot.Phase == HSTraderPhase.Results)
        {
            FinishIdle(world);
            return "Results closed. Mission idle.";
        }
        GameManager.Instance.StartCoroutine(EndRoutine(world, HSTraderOutcome.None, false));
        return "Stopping mission: compound will reload and the trader will be restored.";
    }

    public static string Status(World world)
    {
        if (world == null)
            world = GameManager.Instance != null ? GameManager.Instance.World : null;
        if (world != null)
            HSTraderSnapshot.NowWorldTime = world.worldTime;
        switch (HSTraderSnapshot.Phase)
        {
            case HSTraderPhase.Idle:
                return "No mission is running.";
            case HSTraderPhase.Preparing:
                return "PREPARING. " + HSTraderSnapshot.RaidInString(HSTraderSnapshot.RemainingPrepTicks());
            case HSTraderPhase.Raiding:
                return "RAIDING wave " + HSTraderSnapshot.Wave
                    + "  survived " + HSTraderSnapshot.SurvivalString(HSTraderSnapshot.SurvivalTicksNow())
                    + "  trader " + HSTraderSnapshot.Health + "/" + HSTraderSnapshot.MaxHealth;
            case HSTraderPhase.Cleanup:
                return "Cleaning up the compound.";
            case HSTraderPhase.Results:
                return "End screen is up (" + HSTraderSnapshot.Outcome + "). Close it to return to idle.";
            default:
                return HSTraderSnapshot.Phase.ToString();
        }
    }

    public static string ResetStats()
    {
        if (!HSTraderNet.IsAuthority)
            return "Trader Protection commands run on the server.";
        if (HSTraderSnapshot.Phase != HSTraderPhase.Idle)
            return "resetstats only works while idle.";
        HSTraderStats.Reset();
        return "Statistics cleared.";
    }

    public static string DebugRaid(World world)
    {
        if (HSTraderSnapshot.Phase != HSTraderPhase.Preparing)
            return "debug raid only works during preparation.";
        HSTraderSnapshot.RaidStartWorldTime = world.worldTime;
        BeginRaid(world);
        return "Raid started (debug).";
    }

    public static string DebugDamage(World world, int amount)
    {
        var trader = GetTrader(world);
        if (trader == null || HSTraderSnapshot.Phase != HSTraderPhase.Raiding)
            return "Need an active raid trader.";
        int srcId = FindAnyZombieId(world);
        var src = new DamageSourceEntity(EnumDamageSource.Internal, EnumDamageTypes.Slashing, srcId);
        var npc = trader.NPCInfo;
        int saved = npc != null ? npc.TraderID : 0;
        if (npc != null)
            npc.TraderID = -1;
        trader.IsGodMode.Value = false;
        HSTraderRuntime.Skip = true;
        int applied;
        try
        {
            applied = trader.DamageEntity(src, amount, false, 1f);
        }
        finally
        {
            HSTraderRuntime.Skip = false;
            if (npc != null)
                npc.TraderID = saved;
        }
        if (applied > 0)
            OnTraderDamaged(trader, applied, src);
        RefreshHealth(trader);
        HSTraderNet.BroadcastSnapshot();
        return "Applied " + applied + " damage. Health " + trader.Health + "/" + trader.GetMaxHealth();
    }

    public static string DebugKill(World world)
    {
        var trader = GetTrader(world);
        if (trader == null || HSTraderSnapshot.Phase != HSTraderPhase.Raiding)
            return "Need an active raid trader.";
        var src = new DamageSourceEntity(EnumDamageSource.Internal, EnumDamageTypes.Slashing, FindAnyZombieId(world));
        trader.Kill(DamageResponse.New(src, true));
        return "Fake lethal hit sent.";
    }

    public static void Tick()
    {
        if (!HSTraderNet.IsAuthority)
            return;
        var world = GameManager.Instance != null ? GameManager.Instance.World : null;
        if (world == null)
            return;
        cachedWorld = world;
        HSTraderSnapshot.NowWorldTime = world.worldTime;
        var phase = HSTraderSnapshot.Phase;
        if (phase == HSTraderPhase.Idle || phase == HSTraderPhase.Cleanup || phase == HSTraderPhase.Results)
            return;
        var trader = GetTrader(world);
        if (phase == HSTraderPhase.Preparing)
        {
            if (trader != null)
                trader.IsGodMode.Value = true;
            if (world.worldTime >= HSTraderSnapshot.RaidStartWorldTime)
                BeginRaid(world);
            else if (Time.unscaledTime >= lastHeartbeat + 2f)
            {
                lastHeartbeat = Time.unscaledTime;
                HSTraderNet.BroadcastSnapshot();
            }
        }
        else if (phase == HSTraderPhase.Raiding)
        {
            if (trader == null)
            {
                Log.Warning(LogPrefix + " Trader missing during raid, failing.");
                GameManager.Instance.StartCoroutine(EndRoutine(world, HSTraderOutcome.Failure, true));
                return;
            }
            RefreshHealth(trader);
            var list = world.GetPlayers();
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                    HSTraderStats.NotePlayerInRadius(list[i]);
            }
            HSTraderSpawn.Tick(world, trader);
            if (world.worldTime >= HSTraderSnapshot.RaidStartWorldTime + HSTraderSnapshot.ObjectiveTicks)
            {
                Log.Out(LogPrefix + " Objective reached.");
                GameManager.Instance.StartCoroutine(EndRoutine(world, HSTraderOutcome.Success, true));
                return;
            }
            if (Time.unscaledTime >= lastHeartbeat + 0.5f)
            {
                lastHeartbeat = Time.unscaledTime;
                HSTraderNet.BroadcastSnapshot();
            }
        }
        if (Time.unscaledTime >= lastSave + 60f)
        {
            lastSave = Time.unscaledTime;
            Save();
        }
    }

    public static void OnWorldStarted()
    {
        HookEvents();
        Load();
        if (HSTraderSnapshot.Phase != HSTraderPhase.Idle)
            HSTraderNet.BroadcastSnapshot();
    }

    public static void OnWorldShutdown()
    {
        if (HSTraderNet.IsAuthority && HSTraderSnapshot.Phase != HSTraderPhase.Idle)
            Save();
        UnhookEvents();
        HSTraderSpawn.Clear();
        ending = false;
    }

    public static void OnPlayerSpawned(int entityId)
    {
        if (!HSTraderNet.IsAuthority)
            return;
        if (HSTraderSnapshot.Phase == HSTraderPhase.Idle)
            return;
        HSTraderNet.SendSnapshotTo(entityId);
    }

    public static void OnTraderDamaged(EntityTrader trader, int applied, DamageSource source)
    {
        if (!HSTraderNet.IsAuthority || applied <= 0)
            return;
        HSTraderSnapshot.DamageTaken += applied;
        HSTraderSnapshot.Stats.DamageTaken = HSTraderSnapshot.DamageTaken;
        RefreshHealth(trader);
        RememberAttacker(trader.world, source);
    }

    public static void OnTraderLethal(EntityTrader trader, DamageResponse resp)
    {
        if (!HSTraderNet.IsAuthority)
            return;
        if (HSTraderSnapshot.Phase != HSTraderPhase.Raiding)
            return;
        RememberAttacker(trader.world, resp.Source);
        HSTraderSnapshot.Stats.CauseOfDeath = "Killed by zombie";
        RestoreTrader(trader);
        GameManager.Instance.StartCoroutine(EndRoutine(trader.world, HSTraderOutcome.Failure, true));
    }

    public static bool ShouldSuppressTeleport(EntityAlive entity)
    {
        if (!HSTraderConfig.SuppressTeleport)
            return false;
        if (!HSTraderSnapshot.IsLockdown)
            return false;
        if (!(entity is EntityPlayer))
            return false;
        return IsInMissionRadius(entity.position);
    }

    public static bool IsInMissionRadius(Vector3 pos)
    {
        if (!HSTraderSnapshot.IsActive)
            return false;
        Vector3 center = new Vector3(HSTraderSnapshot.TraderX + 0.5f, HSTraderSnapshot.TraderY, HSTraderSnapshot.TraderZ + 0.5f);
        float r = HSTraderConfig.MissionRadius;
        float dx = pos.x - center.x;
        float dz = pos.z - center.z;
        return dx * dx + dz * dz <= r * r;
    }

    public static bool IsInBuildRing(Vector3i pos)
    {
        if (!IsInMissionRadius(pos.ToVector3()))
            return false;
        var prefab = CurrentPrefab(cachedWorld ?? GameManager.Instance.World);
        if (prefab == null)
            return true;
        return !InsidePrefab(prefab, pos);
    }

    public static void RememberPlacedBlock(Vector3i pos)
    {
        placedBlocks.Add(Key(pos));
    }

    public static void ForgetPlacedBlock(Vector3i pos)
    {
        placedBlocks.Remove(Key(pos));
    }

    public static void ResultsClosedBy(int entityId)
    {
        if (!HSTraderNet.IsAuthority)
            return;
        if (HSTraderSnapshot.Phase != HSTraderPhase.Results)
            return;
        resultsClosed.Add(entityId);
        var world = GameManager.Instance.World;
        if (world == null)
            return;
        var players = world.GetPlayers();
        int need = 0;
        int closed = 0;
        if (players != null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == null)
                    continue;
                need++;
                if (resultsClosed.Contains(players[i].entityId))
                    closed++;
            }
        }
        if (need == 0 || closed >= need)
            FinishIdle(world);
    }

    static void BeginRaid(World world)
    {
        var trader = GetTrader(world);
        if (trader == null)
        {
            Log.Error(LogPrefix + " Cannot start raid: trader gone.");
            FinishIdle(world);
            return;
        }
        HSTraderSnapshot.Phase = HSTraderPhase.Raiding;
        HSTraderSnapshot.RaidStartWorldTime = world.worldTime;
        HSTraderSnapshot.NowWorldTime = world.worldTime;
        trader.IsGodMode.Value = false;
        RefreshHealth(trader);
        HSTraderSpawn.BeginRaid(world);
        CloseTradeWindows(world);
        Save();
        HSTraderNet.BroadcastSnapshot();
        HSTraderNet.BroadcastTip("Trader Protection: UNDER ATTACK. Keep the trader alive for 24 in-game hours.");
        Log.Out(LogPrefix + " Raid started.");
    }

    static IEnumerator EndRoutine(World world, HSTraderOutcome outcome, bool showResults)
    {
        if (ending)
            yield break;
        ending = true;
        HSTraderSnapshot.Phase = HSTraderPhase.Cleanup;
        HSTraderSnapshot.Outcome = outcome;
        var trader = GetTrader(world);
        HSTraderStats.Freeze(trader, outcome);
        HSTraderNet.BroadcastSnapshot();
        HSTraderSpawn.DespawnAll(world);
        RestoreTrader(trader);
        TeleportPlayersOut(world);
        yield return null;
        var prefab = CurrentPrefab(world);
        if (prefab != null)
        {
            var list = new List<PrefabInstance>();
            var deco = GameManager.Instance.GetDynamicPrefabDecorator();
            if (deco != null)
            {
                var intersecting = deco.GetPrefabsIntersecting(prefab);
                if (intersecting != null && intersecting.Count > 0)
                    list.AddRange(intersecting);
                else
                    list.Add(prefab);
            }
            else
                list.Add(prefab);
            yield return world.ResetPOIS(list, QuestEventManager.manualResetTag, -1, null, null);
        }
        ClearPlacedBlocks(world);
        yield return null;
        trader = GetTrader(world);
        if (trader == null)
        {
            var area = world.GetTraderAreaAt(new Vector3i(HSTraderSnapshot.TraderX, HSTraderSnapshot.TraderY, HSTraderSnapshot.TraderZ));
            if (area != null)
                trader = area.GetTrader();
            if (trader != null)
                HSTraderSnapshot.TraderEntityId = trader.entityId;
        }
        RestoreTrader(trader);
        RefreshHealth(trader);
        ending = false;
        if (showResults && outcome != HSTraderOutcome.None)
        {
            HSTraderSnapshot.Phase = HSTraderPhase.Results;
            resultsClosed.Clear();
            Save();
            HSTraderNet.BroadcastSnapshot();
            Log.Out(LogPrefix + " Results: " + outcome);
        }
        else
            FinishIdle(world);
    }

    static void FinishIdle(World world)
    {
        HSTraderSpawn.DespawnAll(world);
        RestoreTrader(GetTrader(world));
        HSTraderSnapshot.Clear();
        placedBlocks.Clear();
        resultsClosed.Clear();
        ending = false;
        DeleteSave();
        HSTraderNet.BroadcastSnapshot();
        Log.Out(LogPrefix + " Idle.");
    }

    static void RestoreTrader(EntityTrader trader)
    {
        if (trader == null)
            return;
        try
        {
            int max = trader.GetMaxHealth();
            trader.Stats.Health.Value = max;
            trader.IsGodMode.Value = true;
            HSTraderSnapshot.Health = trader.Health;
            HSTraderSnapshot.MaxHealth = max;
        }
        catch (Exception e)
        {
            Log.Warning(LogPrefix + " Restore trader failed: " + e.Message);
        }
    }

    static void RefreshHealth(EntityTrader trader)
    {
        if (trader == null)
            return;
        HSTraderSnapshot.Health = trader.Health;
        HSTraderSnapshot.MaxHealth = trader.GetMaxHealth();
    }

    static void RememberAttacker(World world, DamageSource source)
    {
        if (source == null)
            return;
        int id = source.getEntityId();
        Entity e = world != null ? world.GetEntity(id) : null;
        if (e == null)
            return;
        string className = "";
        try
        {
            var ec = EntityClass.list[e.entityClass];
            if (ec != null)
                className = ec.entityClassName;
        }
        catch { }
        HSTraderSnapshot.Stats.AttackerClass = className;
        HSTraderSnapshot.Stats.AttackerName = e is EntityPlayer p ? p.PlayerDisplayName : className;
        if (string.IsNullOrEmpty(HSTraderSnapshot.Stats.CauseOfDeath))
            HSTraderSnapshot.Stats.CauseOfDeath = "Damaged by " + className;
    }

    static void TeleportPlayersOut(World world)
    {
        var prefab = CurrentPrefab(world);
        if (prefab == null)
            return;
        var players = world.GetPlayers();
        if (players == null)
            return;
        Vector3 center = prefab.boundingBoxPosition.ToVector3() + prefab.boundingBoxSize.ToVector3() * 0.5f;
        int half = Math.Max(prefab.boundingBoxSize.x, prefab.boundingBoxSize.z) / 2 + 4;
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player == null)
                continue;
            var pos = World.worldToBlockPos(player.position);
            if (!InsidePrefab(prefab, pos))
                continue;
            Vector3 dest;
            if (!world.GetRandomSpawnPositionMinMaxToPosition(center, half, half + 8, 1, false, out dest, player.entityId, true, 20, true, EnumLandClaimOwner.Ally, true))
                dest = center + Vector3.forward * (half + 6);
            if (player.isEntityRemote)
            {
                var ci = SingletonMonoBehaviour<ConnectionManager>.Instance.Clients.ForEntityId(player.entityId);
                if (ci != null)
                    ci.SendPackage(NetPackageManager.GetPackage<NetPackageTeleportPlayer>().Setup(dest));
            }
            else
                player.Teleport(dest);
        }
    }

    static void ClearPlacedBlocks(World world)
    {
        if (placedBlocks.Count == 0)
            return;
        var changes = new List<BlockChangeInfo>();
        foreach (string key in placedBlocks)
        {
            Vector3i pos;
            if (!ParseKey(key, out pos))
                continue;
            changes.Add(new BlockChangeInfo(new BlockValueRef(pos), BlockValue.Air, true));
        }
        placedBlocks.Clear();
        if (changes.Count > 0)
            world.SetBlocksRPC(changes);
    }

    static void CloseTradeWindows(World world)
    {
        try
        {
            var trader = GetTrader(world);
            if (trader != null)
                LockManager.Instance.ForceUnlockLockTarget(trader);
        }
        catch { }
        HSTraderNet.BroadcastCloseTrader();
    }

    static PrefabInstance CurrentPrefab(World world)
    {
        if (world == null)
            return null;
        var deco = GameManager.Instance.GetDynamicPrefabDecorator();
        if (deco == null)
            return null;
        if (HSTraderSnapshot.PrefabId >= 0)
        {
            var byId = deco.GetPrefab(HSTraderSnapshot.PrefabId);
            if (byId != null)
                return byId;
        }
        return PrefabAt(world, new Vector3(HSTraderSnapshot.TraderX, HSTraderSnapshot.TraderY, HSTraderSnapshot.TraderZ));
    }

    static PrefabInstance PrefabAt(World world, Vector3 pos)
    {
        var deco = GameManager.Instance.GetDynamicPrefabDecorator();
        if (deco == null)
            return null;
        var p = deco.GetPrefabFromWorldPos((int)pos.x, (int)pos.z);
        if (p != null)
            return p;
        return deco.GetPrefabFromWorldPosInside((int)pos.x, (int)pos.z);
    }

    static bool InsidePrefab(PrefabInstance prefab, Vector3i pos)
    {
        if (prefab == null)
            return false;
        var min = prefab.boundingBoxPosition;
        var max = min + prefab.boundingBoxSize;
        return pos.x >= min.x && pos.x < max.x && pos.y >= min.y && pos.y < max.y && pos.z >= min.z && pos.z < max.z;
    }

    static int FindAnyZombieId(World world)
    {
        foreach (int id in HSTraderSpawn.SpawnIds)
            return id;
        var list = world.GetPlayers();
        return list != null && list.Count > 0 ? list[0].entityId : -1;
    }

    static string Key(Vector3i p)
    {
        return p.x + "," + p.y + "," + p.z;
    }

    static bool ParseKey(string key, out Vector3i pos)
    {
        pos = Vector3i.zero;
        if (string.IsNullOrEmpty(key))
            return false;
        var parts = key.Split(',');
        int x, y, z;
        if (parts.Length != 3 || !int.TryParse(parts[0], out x) || !int.TryParse(parts[1], out y) || !int.TryParse(parts[2], out z))
            return false;
        pos = new Vector3i(x, y, z);
        return true;
    }

    static string SavePath()
    {
        try
        {
            string dir = GameIO.GetSaveGameDir();
            if (string.IsNullOrEmpty(dir))
                return null;
            return Path.Combine(dir, "HS_TraderProtection.bin");
        }
        catch
        {
            return null;
        }
    }

    static void Save()
    {
        if (!HSTraderNet.IsAuthority)
            return;
        string path = SavePath();
        if (path == null)
            return;
        try
        {
            using (var fs = File.Create(path))
            using (var bw = new BinaryWriter(fs))
            {
                HSTraderSnapshot.Write(bw);
                bw.Write(placedBlocks.Count);
                foreach (string p in placedBlocks)
                    bw.Write(p);
            }
        }
        catch (Exception e)
        {
            Log.Warning(LogPrefix + " Save failed: " + e.Message);
        }
    }

    static void Load()
    {
        if (!HSTraderNet.IsAuthority)
            return;
        string path = SavePath();
        if (path == null || !File.Exists(path))
        {
            HSTraderSnapshot.Clear();
            return;
        }
        try
        {
            using (var fs = File.OpenRead(path))
            using (var br = new BinaryReader(fs))
            {
                HSTraderSnapshot.Read(br);
                placedBlocks.Clear();
                int n = br.ReadInt32();
                for (int i = 0; i < n; i++)
                    placedBlocks.Add(br.ReadString());
            }
            if (HSTraderSnapshot.Phase == HSTraderPhase.Cleanup)
                HSTraderSnapshot.Phase = HSTraderPhase.Idle;
            Log.Out(LogPrefix + " Restored mission phase " + HSTraderSnapshot.Phase);
        }
        catch (Exception e)
        {
            Log.Warning(LogPrefix + " Load failed: " + e.Message);
            HSTraderSnapshot.Clear();
        }
    }

    static void DeleteSave()
    {
        string path = SavePath();
        try
        {
            if (path != null && File.Exists(path))
                File.Delete(path);
        }
        catch { }
    }

    static void HookEvents()
    {
        if (eventsHooked)
            return;
        eventsHooked = true;
        ModEvents.EntityKilled.RegisterHandler(OnKilled);
        if (QuestEventManager.HasInstance)
        {
            QuestEventManager.Current.BlockPlace += OnPlace;
            QuestEventManager.Current.BlockUpgrade += OnUpgrade;
            QuestEventManager.Current.BlockDestroy += OnDestroy;
        }
    }

    static void UnhookEvents()
    {
        if (!eventsHooked)
            return;
        eventsHooked = false;
        ModEvents.EntityKilled.UnregisterHandler(OnKilled);
        if (QuestEventManager.HasInstance)
        {
            QuestEventManager.Current.BlockPlace -= OnPlace;
            QuestEventManager.Current.BlockUpgrade -= OnUpgrade;
            QuestEventManager.Current.BlockDestroy -= OnDestroy;
        }
    }

    static void OnKilled(ref ModEvents.SEntityKilledData data)
    {
        if (!HSTraderNet.IsAuthority)
            return;
        if (HSTraderSnapshot.Phase != HSTraderPhase.Preparing && HSTraderSnapshot.Phase != HSTraderPhase.Raiding)
            return;
        HSTraderStats.OnEntityKilled(data.KilledEntitiy, data.KillingEntity);
    }

    static void OnPlace(string name, Vector3i pos)
    {
        if (HSTraderNet.IsAuthority)
            HSTraderStats.OnBlockPlace(pos);
    }

    static void OnUpgrade(string name, Vector3i pos)
    {
        if (HSTraderNet.IsAuthority)
            HSTraderStats.OnBlockUpgrade(pos);
    }

    static void OnDestroy(Block block, Vector3i pos)
    {
        if (HSTraderNet.IsAuthority)
            HSTraderStats.OnBlockDestroy(pos);
    }
}
