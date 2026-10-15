using System.Collections.Generic;
using UnityEngine;

public static class HSTraderStats
{
    static FastTags<TagGroup.Global> chargedTag = FastTags<TagGroup.Global>.Parse("charged");
    static FastTags<TagGroup.Global> infernalTag = FastTags<TagGroup.Global>.Parse("infernal");
    static readonly Dictionary<int, HSTraderPlayerStat> players = new Dictionary<int, HSTraderPlayerStat>();
    static readonly HashSet<int> seenFatal = new HashSet<int>();

    public static void Reset()
    {
        players.Clear();
        seenFatal.Clear();
        if (HSTraderSnapshot.Stats == null)
            HSTraderSnapshot.Stats = new HSTraderStatsData();
        var s = HSTraderSnapshot.Stats;
        s.TotalKills = 0;
        s.NormalKills = 0;
        s.FeralKills = 0;
        s.RadiatedKills = 0;
        s.ChargedKills = 0;
        s.InfernalKills = 0;
        s.DemolisherKills = 0;
        s.PlayerKills = 0;
        s.TurretKills = 0;
        s.TrapKills = 0;
        s.PlayerDeaths = 0;
        s.Headshots = 0;
        s.AmmoFired = 0;
        s.Hits = 0;
        s.BlocksPlaced = 0;
        s.BlocksUpgraded = 0;
        s.BlocksDestroyed = 0;
        s.WavesSurvived = 0;
        s.FinalWave = 0;
        s.DamageTaken = 0;
        s.EndHealth = 0;
        s.EndMaxHealth = 0;
        s.SurvivalTicks = 0;
        s.ObjectiveComplete = false;
        s.CauseOfDeath = "";
        s.AttackerClass = "";
        s.AttackerName = "";
        s.Players = new List<HSTraderPlayerStat>();
    }

    public static HSTraderPlayerStat Player(EntityPlayer player)
    {
        if (player == null)
            return null;
        return Player(player.entityId, player.PlayerDisplayName);
    }

    public static HSTraderPlayerStat Player(int entityId, string name)
    {
        HSTraderPlayerStat p;
        if (!players.TryGetValue(entityId, out p))
        {
            p = new HSTraderPlayerStat();
            p.EntityId = entityId;
            p.Name = name ?? ("Player " + entityId);
            players[entityId] = p;
        }
        else if (!string.IsNullOrEmpty(name))
            p.Name = name;
        return p;
    }

    public static void NotePlayerInRadius(EntityPlayer player)
    {
        if (player == null)
            return;
        if (!HSTraderMission.IsInMissionRadius(player.position))
            return;
        Player(player);
    }

    public static void OnEntityKilled(Entity killed, Entity killer)
    {
        if (killed == null)
            return;
        if (killed is EntityPlayer victim && HSTraderMission.IsInMissionRadius(victim.position))
        {
            HSTraderSnapshot.Stats.PlayerDeaths++;
            var ps = Player(victim);
            if (ps != null)
                ps.Deaths++;
            return;
        }
        if (!HSTraderMission.IsMissionZombie(killed.entityId))
            return;
        string className = ClassName(killed);
        int bucket = Classify(killed, className);
        AddBucket(bucket);
        HSTraderSnapshot.Stats.TotalKills++;
        if (killer is EntityPlayer pk)
        {
            HSTraderSnapshot.Stats.PlayerKills++;
            Player(pk).Kills++;
        }
        else if (killer is EntityTurret turret)
        {
            HSTraderSnapshot.Stats.TurretKills++;
            int ownerId = turret.belongsPlayerId;
            EntityPlayer owner = ownerId != -1 ? GameManager.Instance.World.GetEntity(ownerId) as EntityPlayer : turret.Owner as EntityPlayer;
            if (owner != null)
                Player(owner).TurretKills++;
        }
        else if (killer == null || IsTrap(killer))
            HSTraderSnapshot.Stats.TrapKills++;
    }

    public static void OnDamageResponse(EntityAlive victim, DamageResponse resp)
    {
        if (victim == null || !HSTraderMission.IsMissionZombie(victim.entityId))
            return;
        if (resp.Source == null)
            return;
        int srcId = resp.Source.getEntityId();
        var world = GameManager.Instance != null ? GameManager.Instance.World : null;
        Entity src = world != null ? world.GetEntity(srcId) : null;
        EntityPlayer player = src as EntityPlayer;
        if (player == null && src is EntityTurret turret)
            player = turret.Owner as EntityPlayer;
        if (resp.Strength > 0 && player != null)
        {
            var ps = Player(player);
            ps.Hits++;
            ps.DamageDealt += resp.Strength;
            HSTraderSnapshot.Stats.Hits++;
        }
        if (!resp.Fatal)
            return;
        if (!seenFatal.Add(victim.entityId))
            return;
        if ((resp.HitBodyPart & EnumBodyPartHit.Head) != 0 && player != null)
        {
            Player(player).Headshots++;
            HSTraderSnapshot.Stats.Headshots++;
        }
        if (resp.Source.bTrapKillXP && player == null)
            HSTraderSnapshot.Stats.TrapKills++;
    }

    public static void OnAmmoFired(EntityPlayer player)
    {
        if (player == null || !HSTraderMission.IsTracking)
            return;
        if (!HSTraderMission.IsInMissionRadius(player.position))
            return;
        Player(player).AmmoFired++;
        HSTraderSnapshot.Stats.AmmoFired++;
    }

    public static void OnBlockPlace(Vector3i pos)
    {
        if (!HSTraderMission.IsTracking || !HSTraderMission.IsInBuildRing(pos))
            return;
        HSTraderSnapshot.Stats.BlocksPlaced++;
        HSTraderMission.RememberPlacedBlock(pos);
    }

    public static void OnBlockUpgrade(Vector3i pos)
    {
        if (!HSTraderMission.IsTracking || !HSTraderMission.IsInBuildRing(pos))
            return;
        HSTraderSnapshot.Stats.BlocksUpgraded++;
    }

    public static void OnBlockDestroy(Vector3i pos)
    {
        if (!HSTraderMission.IsTracking || !HSTraderMission.IsInBuildRing(pos))
            return;
        HSTraderSnapshot.Stats.BlocksDestroyed++;
        HSTraderMission.ForgetPlacedBlock(pos);
    }

    public static void Freeze(EntityTrader trader, HSTraderOutcome outcome)
    {
        var s = HSTraderSnapshot.Stats;
        if (s == null)
            s = HSTraderSnapshot.Stats = new HSTraderStatsData();
        s.SurvivalTicks = HSTraderSnapshot.SurvivalTicksNow();
        s.FinalWave = HSTraderSnapshot.Wave;
        s.WavesSurvived = Mathf.Max(0, HSTraderSnapshot.Wave - 1);
        s.ObjectiveComplete = outcome == HSTraderOutcome.Success;
        s.DamageTaken = HSTraderSnapshot.DamageTaken;
        if (trader != null)
        {
            s.EndHealth = trader.Health;
            s.EndMaxHealth = trader.GetMaxHealth();
        }
        else
        {
            s.EndHealth = HSTraderSnapshot.Health;
            s.EndMaxHealth = HSTraderSnapshot.MaxHealth;
        }
        s.Players = new List<HSTraderPlayerStat>(players.Values);
    }

    static bool IsTrap(Entity e)
    {
        if (e == null)
            return false;
        string n = e.GetType().Name;
        return n.IndexOf("Trap", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static string ClassName(Entity e)
    {
        try
        {
            var ec = EntityClass.list[e.entityClass];
            if (ec != null && !string.IsNullOrEmpty(ec.entityClassName))
                return ec.entityClassName;
        }
        catch { }
        return e.GetType().Name;
    }

    public static int Classify(Entity e, string className)
    {
        if (!string.IsNullOrEmpty(className) && className == "zombieDemolition")
            return 6;
        EntityClass ec = null;
        try { ec = EntityClass.list[e.entityClass]; }
        catch { }
        if (ec != null)
        {
            if (ec.Tags.Test_AnySet(infernalTag))
                return 5;
            if (ec.Tags.Test_AnySet(chargedTag))
                return 4;
            if (ec.Tags.Test_AnySet(EntityClass.radiatedTag))
                return 3;
            if (ec.Tags.Test_AnySet(EntityClass.feralTag))
                return 2;
        }
        if (!string.IsNullOrEmpty(className))
        {
            if (className.IndexOf("Infernal") >= 0) return 5;
            if (className.IndexOf("Charged") >= 0) return 4;
            if (className.IndexOf("Radiated") >= 0) return 3;
            if (className.IndexOf("Feral") >= 0) return 2;
        }
        return 1;
    }

    static void AddBucket(int bucket)
    {
        var s = HSTraderSnapshot.Stats;
        switch (bucket)
        {
            case 6: s.DemolisherKills++; break;
            case 5: s.InfernalKills++; break;
            case 4: s.ChargedKills++; break;
            case 3: s.RadiatedKills++; break;
            case 2: s.FeralKills++; break;
            default: s.NormalKills++; break;
        }
    }
}
