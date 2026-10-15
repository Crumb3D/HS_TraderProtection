using System.Collections.Generic;
using UnityEngine;

public static class HSTraderSpawn
{
    static readonly HashSet<int> spawnIds = new HashSet<int>();
    static readonly HashSet<string> missingLogged = new HashSet<string>();
    static int pending;
    static int lastClassId = -1;
    static ulong nextWaveTime;
    static int waveNumber;

    public static IEnumerable<int> SpawnIds { get { return spawnIds; } }

    public static bool Contains(int entityId)
    {
        return spawnIds.Contains(entityId);
    }

    public static void Clear()
    {
        spawnIds.Clear();
        pending = 0;
        lastClassId = -1;
        nextWaveTime = 0;
        waveNumber = 0;
    }

    public static void BeginRaid(World world)
    {
        spawnIds.Clear();
        pending = 0;
        waveNumber = 0;
        nextWaveTime = world.worldTime;
        missingLogged.Clear();
        QueueWave(world, 0);
    }

    public static void Tick(World world, EntityTrader trader)
    {
        if (world == null || trader == null)
            return;
        if (world.worldTime >= nextWaveTime)
            QueueWave(world, RaidHour(world));
        int perTick = HSTraderConfig.SpawnsPerTick;
        while (pending > 0 && perTick-- > 0)
        {
            if (!CanSpawn(world))
                break;
            if (!TrySpawn(world, trader))
                break;
            pending--;
        }
        PruneDead(world);
        KeepAggro(world, trader);
    }

    public static void DespawnAll(World world)
    {
        if (world == null)
            return;
        var ids = new List<int>(spawnIds);
        spawnIds.Clear();
        pending = 0;
        for (int i = 0; i < ids.Count; i++)
        {
            var e = world.GetEntity(ids[i]) as EntityAlive;
            if (e == null || e.IsDead())
                continue;
            try { e.Kill(DamageResponse.New(true)); }
            catch { try { world.RemoveEntity(ids[i], EnumRemoveEntityReason.Killed); } catch { } }
        }
    }

    static int RaidHour(World world)
    {
        if (world.worldTime <= HSTraderSnapshot.RaidStartWorldTime)
            return 0;
        ulong elapsed = world.worldTime - HSTraderSnapshot.RaidStartWorldTime;
        int hour = (int)(elapsed / 1000UL);
        if (hour < 0) hour = 0;
        if (hour > 23) hour = 23;
        return hour;
    }

    static void QueueWave(World world, int hour)
    {
        var band = HSTraderConfig.BandForHour(hour);
        pending += band.Count;
        waveNumber++;
        HSTraderSnapshot.Wave = waveNumber;
        nextWaveTime = world.worldTime + HSTraderConfig.WaveIntervalTicks;
        HSTraderSnapshot.NextWaveWorldTime = nextWaveTime;
        Log.Out("[HSTraderProtection] Wave " + waveNumber + " queued +" + band.Count + " (hour " + hour + ")");
    }

    static bool CanSpawn(World world)
    {
        int live = LiveCount(world);
        int cap = HSTraderConfig.MaxAlive;
        int maxZ = GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedZombies);
        if (maxZ > 0)
            cap = Mathf.Min(cap, Mathf.Max(1, maxZ - GameStats.GetInt(EnumGameStats.EnemyCount) + live));
        return live < cap;
    }

    static int LiveCount(World world)
    {
        int n = 0;
        foreach (int id in spawnIds)
        {
            var e = world.GetEntity(id) as EntityAlive;
            if (e != null && !e.IsDead())
                n++;
        }
        return n;
    }

    static bool TrySpawn(World world, EntityTrader trader)
    {
        int hour = RaidHour(world);
        var band = HSTraderConfig.BandForHour(hour);
        int classId = PickClass(world, band);
        if (classId < 0 || !EntityClass.list.ContainsKey(classId))
            return false;
        Vector3 pos;
        if (!world.GetRandomSpawnPositionMinMaxToPosition(trader.position, HSTraderConfig.SpawnMinRadius, HSTraderConfig.SpawnMaxRadius, 4, false, out pos, -1, true, 30))
        {
            Vector3 dir = new Vector3(world.GetGameRandom().RandomFloat * 2f - 1f, 0f, world.GetGameRandom().RandomFloat * 2f - 1f);
            if (dir.sqrMagnitude < 0.01f)
                dir = Vector3.forward;
            dir.Normalize();
            float r = HSTraderConfig.SpawnMinRadius + world.GetGameRandom().RandomFloat * (HSTraderConfig.SpawnMaxRadius - HSTraderConfig.SpawnMinRadius);
            pos = trader.position + dir * r;
            pos.y = world.GetHeight((int)pos.x, (int)pos.z) + 1.1f;
        }
        Entity entity;
        try
        {
            entity = EntityFactory.CreateEntity(classId, pos, Vector3.zero);
        }
        catch (System.Exception e)
        {
            LogMissing("create " + classId, e.Message);
            return false;
        }
        if (entity == null)
            return false;
        entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);
        entity.bIsChunkObserver = true;
        world.SpawnEntityInWorld(entity);
        spawnIds.Add(entity.entityId);
        var alive = entity as EntityAlive;
        if (alive != null)
        {
            alive.SetAttackTarget(trader, 12000);
            alive.SetInvestigatePosition(trader.position, 12000, true);
        }
        return true;
    }

    static int PickClass(World world, HSTraderBand band)
    {
        if (band.DemolisherChance > 0f && world.GetGameRandom().RandomFloat < band.DemolisherChance)
        {
            int demo = FromGroup("HS_TP_Demolisher");
            if (demo >= 0)
                return demo;
        }
        if (band.Groups == null || band.Groups.Length == 0)
            return FromGroup("HS_TP_Normal");
        string group = band.Groups[world.GetGameRandom().RandomRange(band.Groups.Length)];
        return FromGroup(group);
    }

    static int FromGroup(string group)
    {
        if (string.IsNullOrEmpty(group))
            return -1;
        try
        {
            if (EntityGroups.list == null || !EntityGroups.list.ContainsKey(group))
            {
                LogMissing(group, "entity group not loaded");
                return -1;
            }
            int id = EntityGroups.GetRandomFromGroup(group, ref lastClassId);
            if (id < 0 || !EntityClass.list.ContainsKey(id))
            {
                LogMissing(group, "no valid class");
                return -1;
            }
            return id;
        }
        catch (System.Exception e)
        {
            LogMissing(group, e.Message);
            return -1;
        }
    }

    static void LogMissing(string key, string why)
    {
        if (!missingLogged.Add(key))
            return;
        Log.Warning("[HSTraderProtection] Spawn skip '" + key + "': " + why);
    }

    static void PruneDead(World world)
    {
        var dead = new List<int>();
        foreach (int id in spawnIds)
        {
            var e = world.GetEntity(id) as EntityAlive;
            if (e == null || e.IsDead())
                dead.Add(id);
        }
        for (int i = 0; i < dead.Count; i++)
            spawnIds.Remove(dead[i]);
    }

    static void KeepAggro(World world, EntityTrader trader)
    {
        foreach (int id in spawnIds)
        {
            var e = world.GetEntity(id) as EntityAlive;
            if (e == null || e.IsDead())
                continue;
            var current = e.GetAttackTarget();
            if (current is EntityPlayer)
                continue;
            if (current != trader)
                e.SetAttackTarget(trader, 12000);
        }
    }
}
