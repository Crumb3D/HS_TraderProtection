using System;
using System.Collections.Generic;
using System.IO;

public enum HSTraderPhase : byte
{
    Idle = 0,
    Preparing = 1,
    Raiding = 2,
    Cleanup = 3,
    Results = 4
}

public enum HSTraderOutcome : byte
{
    None = 0,
    Success = 1,
    Failure = 2
}

[Serializable]
public class HSTraderPlayerStat
{
    public int EntityId;
    public string Name = "";
    public int Kills;
    public int DamageDealt;
    public int Headshots;
    public int AmmoFired;
    public int Hits;
    public int Deaths;
    public int TurretKills;
}

[Serializable]
public class HSTraderStatsData
{
    public int TotalKills;
    public int NormalKills;
    public int FeralKills;
    public int RadiatedKills;
    public int ChargedKills;
    public int InfernalKills;
    public int DemolisherKills;
    public int PlayerKills;
    public int TurretKills;
    public int TrapKills;
    public int PlayerDeaths;
    public int Headshots;
    public int AmmoFired;
    public int Hits;
    public int BlocksPlaced;
    public int BlocksUpgraded;
    public int BlocksDestroyed;
    public int WavesSurvived;
    public int FinalWave;
    public int DamageTaken;
    public int EndHealth;
    public int EndMaxHealth;
    public ulong SurvivalTicks;
    public bool ObjectiveComplete;
    public string CauseOfDeath = "";
    public string AttackerClass = "";
    public string AttackerName = "";
    public List<HSTraderPlayerStat> Players = new List<HSTraderPlayerStat>();
}

public static class HSTraderSnapshot
{
    public static HSTraderPhase Phase;
    public static int TraderEntityId = -1;
    public static int PrefabId = -1;
    public static int TraderX;
    public static int TraderY;
    public static int TraderZ;
    public static ulong RaidStartWorldTime;
    public static ulong PrepTicks;
    public static ulong ObjectiveTicks;
    public static ulong NowWorldTime;
    public static int Wave;
    public static ulong NextWaveWorldTime;
    public static int Health;
    public static int MaxHealth;
    public static int DamageTaken;
    public static HSTraderOutcome Outcome;
    public static HSTraderStatsData Stats = new HSTraderStatsData();

    public static bool IsActive
    {
        get { return Phase == HSTraderPhase.Preparing || Phase == HSTraderPhase.Raiding || Phase == HSTraderPhase.Cleanup || Phase == HSTraderPhase.Results; }
    }

    public static bool IsLockdown
    {
        get { return Phase == HSTraderPhase.Preparing || Phase == HSTraderPhase.Raiding || Phase == HSTraderPhase.Cleanup; }
    }

    public static bool IsRaid
    {
        get { return Phase == HSTraderPhase.Raiding; }
    }

    public static bool ShowHud
    {
        get { return Phase == HSTraderPhase.Preparing || Phase == HSTraderPhase.Raiding; }
    }

    public static void Clear()
    {
        Phase = HSTraderPhase.Idle;
        TraderEntityId = -1;
        PrefabId = -1;
        TraderX = TraderY = TraderZ = 0;
        RaidStartWorldTime = 0;
        PrepTicks = 0;
        ObjectiveTicks = 0;
        NowWorldTime = 0;
        Wave = 0;
        NextWaveWorldTime = 0;
        Health = 0;
        MaxHealth = 0;
        DamageTaken = 0;
        Outcome = HSTraderOutcome.None;
        Stats = new HSTraderStatsData();
    }

    public static bool IsMissionTrader(EntityTrader trader)
    {
        if (trader == null || TraderEntityId <= 0)
            return false;
        return trader.entityId == TraderEntityId && IsActive;
    }

    public static ulong RemainingPrepTicks()
    {
        if (Phase != HSTraderPhase.Preparing)
            return 0;
        ulong end = RaidStartWorldTime;
        if (NowWorldTime >= end)
            return 0;
        return end - NowWorldTime;
    }

    public static ulong SurvivalTicksNow()
    {
        if (Phase == HSTraderPhase.Results || Phase == HSTraderPhase.Cleanup)
            return Stats != null ? Stats.SurvivalTicks : 0;
        if (Phase != HSTraderPhase.Raiding)
            return 0;
        if (NowWorldTime <= RaidStartWorldTime)
            return 0;
        ulong elapsed = NowWorldTime - RaidStartWorldTime;
        if (ObjectiveTicks > 0 && elapsed > ObjectiveTicks)
            return ObjectiveTicks;
        return elapsed;
    }

    public static ulong RemainingWaveTicks()
    {
        if (Phase != HSTraderPhase.Raiding)
            return 0;
        if (NowWorldTime >= NextWaveWorldTime)
            return 0;
        return NextWaveWorldTime - NowWorldTime;
    }

    public static string RaidInString(ulong remainingTicks)
    {
        ulong days = remainingTicks / 24000UL;
        ulong hours = (remainingTicks % 24000UL) / 1000UL;
        ulong minutes = ((remainingTicks % 1000UL) * 60UL) / 1000UL;
        return "RAID IN " + days + " DAYS " + hours.ToString("00") + ":" + minutes.ToString("00");
    }

    public static string SurvivalString(ulong ticks)
    {
        ulong hours = ticks / 1000UL;
        ulong minutes = ((ticks % 1000UL) * 60UL) / 1000UL;
        return hours.ToString("00") + ":" + minutes.ToString("00");
    }

    public static void Write(BinaryWriter bw)
    {
        bw.Write((byte)1);
        bw.Write((byte)Phase);
        bw.Write(TraderEntityId);
        bw.Write(PrefabId);
        bw.Write(TraderX);
        bw.Write(TraderY);
        bw.Write(TraderZ);
        bw.Write(RaidStartWorldTime);
        bw.Write(PrepTicks);
        bw.Write(ObjectiveTicks);
        bw.Write(NowWorldTime);
        bw.Write(Wave);
        bw.Write(NextWaveWorldTime);
        bw.Write(Health);
        bw.Write(MaxHealth);
        bw.Write(DamageTaken);
        bw.Write((byte)Outcome);
        WriteStats(bw, Stats);
    }

    public static void Read(BinaryReader br)
    {
        br.ReadByte();
        Phase = (HSTraderPhase)br.ReadByte();
        TraderEntityId = br.ReadInt32();
        PrefabId = br.ReadInt32();
        TraderX = br.ReadInt32();
        TraderY = br.ReadInt32();
        TraderZ = br.ReadInt32();
        RaidStartWorldTime = br.ReadUInt64();
        PrepTicks = br.ReadUInt64();
        ObjectiveTicks = br.ReadUInt64();
        NowWorldTime = br.ReadUInt64();
        Wave = br.ReadInt32();
        NextWaveWorldTime = br.ReadUInt64();
        Health = br.ReadInt32();
        MaxHealth = br.ReadInt32();
        DamageTaken = br.ReadInt32();
        Outcome = (HSTraderOutcome)br.ReadByte();
        Stats = ReadStats(br);
    }

    static void WriteStats(BinaryWriter bw, HSTraderStatsData s)
    {
        if (s == null)
            s = new HSTraderStatsData();
        bw.Write(s.TotalKills);
        bw.Write(s.NormalKills);
        bw.Write(s.FeralKills);
        bw.Write(s.RadiatedKills);
        bw.Write(s.ChargedKills);
        bw.Write(s.InfernalKills);
        bw.Write(s.DemolisherKills);
        bw.Write(s.PlayerKills);
        bw.Write(s.TurretKills);
        bw.Write(s.TrapKills);
        bw.Write(s.PlayerDeaths);
        bw.Write(s.Headshots);
        bw.Write(s.AmmoFired);
        bw.Write(s.Hits);
        bw.Write(s.BlocksPlaced);
        bw.Write(s.BlocksUpgraded);
        bw.Write(s.BlocksDestroyed);
        bw.Write(s.WavesSurvived);
        bw.Write(s.FinalWave);
        bw.Write(s.DamageTaken);
        bw.Write(s.EndHealth);
        bw.Write(s.EndMaxHealth);
        bw.Write(s.SurvivalTicks);
        bw.Write(s.ObjectiveComplete);
        bw.Write(s.CauseOfDeath ?? "");
        bw.Write(s.AttackerClass ?? "");
        bw.Write(s.AttackerName ?? "");
        int n = s.Players != null ? s.Players.Count : 0;
        bw.Write(n);
        for (int i = 0; i < n; i++)
        {
            var p = s.Players[i];
            bw.Write(p.EntityId);
            bw.Write(p.Name ?? "");
            bw.Write(p.Kills);
            bw.Write(p.DamageDealt);
            bw.Write(p.Headshots);
            bw.Write(p.AmmoFired);
            bw.Write(p.Hits);
            bw.Write(p.Deaths);
            bw.Write(p.TurretKills);
        }
    }

    static HSTraderStatsData ReadStats(BinaryReader br)
    {
        var s = new HSTraderStatsData();
        s.TotalKills = br.ReadInt32();
        s.NormalKills = br.ReadInt32();
        s.FeralKills = br.ReadInt32();
        s.RadiatedKills = br.ReadInt32();
        s.ChargedKills = br.ReadInt32();
        s.InfernalKills = br.ReadInt32();
        s.DemolisherKills = br.ReadInt32();
        s.PlayerKills = br.ReadInt32();
        s.TurretKills = br.ReadInt32();
        s.TrapKills = br.ReadInt32();
        s.PlayerDeaths = br.ReadInt32();
        s.Headshots = br.ReadInt32();
        s.AmmoFired = br.ReadInt32();
        s.Hits = br.ReadInt32();
        s.BlocksPlaced = br.ReadInt32();
        s.BlocksUpgraded = br.ReadInt32();
        s.BlocksDestroyed = br.ReadInt32();
        s.WavesSurvived = br.ReadInt32();
        s.FinalWave = br.ReadInt32();
        s.DamageTaken = br.ReadInt32();
        s.EndHealth = br.ReadInt32();
        s.EndMaxHealth = br.ReadInt32();
        s.SurvivalTicks = br.ReadUInt64();
        s.ObjectiveComplete = br.ReadBoolean();
        s.CauseOfDeath = br.ReadString();
        s.AttackerClass = br.ReadString();
        s.AttackerName = br.ReadString();
        int n = br.ReadInt32();
        s.Players = new List<HSTraderPlayerStat>(n);
        for (int i = 0; i < n; i++)
        {
            var p = new HSTraderPlayerStat();
            p.EntityId = br.ReadInt32();
            p.Name = br.ReadString();
            p.Kills = br.ReadInt32();
            p.DamageDealt = br.ReadInt32();
            p.Headshots = br.ReadInt32();
            p.AmmoFired = br.ReadInt32();
            p.Hits = br.ReadInt32();
            p.Deaths = br.ReadInt32();
            p.TurretKills = br.ReadInt32();
            s.Players.Add(p);
        }
        return s;
    }
}
