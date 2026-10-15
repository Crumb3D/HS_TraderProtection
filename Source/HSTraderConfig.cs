using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

[Serializable]
public class HSTraderBand
{
    public int FromHour;
    public int ToHour = 24;
    public int Count = 6;
    public float DemolisherChance;
    public string[] Groups;
}

[Serializable]
public class HSTraderSettingsData
{
    public int PrepDays = 7;
    public int ObjectiveHours = 24;
    public float WaveIntervalHours = 1f;
    public int MaxAlive = 24;
    public int SpawnMinRadius = 20;
    public int SpawnMaxRadius = 50;
    public int MissionRadius = 64;
    public float PulseStartPercent = 75f;
    public bool SuppressTeleport = true;
    public int SpawnsPerTick = 2;
    public HSTraderBand[] Bands;
}

public static class HSTraderConfig
{
    public const string FileName = "HSTraderProtectionSettings.json";

    public static ulong PrepTicks = 7UL * 24000UL;
    public static ulong ObjectiveTicks = 24UL * 1000UL;
    public static ulong WaveIntervalTicks = 1000UL;
    public static int MaxAlive = 24;
    public static int SpawnMinRadius = 20;
    public static int SpawnMaxRadius = 50;
    public static int MissionRadius = 64;
    public static float PulseStartPercent = 0.75f;
    public static bool SuppressTeleport = true;
    public static int SpawnsPerTick = 2;
    public static List<HSTraderBand> Bands = DefaultBands();

    static List<HSTraderBand> DefaultBands()
    {
        return new List<HSTraderBand>
        {
            Band(0, 4, 6, 0f, "HS_TP_Normal"),
            Band(4, 8, 8, 0f, "HS_TP_Normal", "HS_TP_Feral"),
            Band(8, 12, 10, 0f, "HS_TP_Feral", "HS_TP_Radiated"),
            Band(12, 16, 12, 0.02f, "HS_TP_Radiated", "HS_TP_Charged"),
            Band(16, 20, 14, 0.08f, "HS_TP_Charged", "HS_TP_Infernal"),
            Band(20, 24, 16, 0.15f, "HS_TP_Charged", "HS_TP_Infernal", "HS_TP_Demolisher")
        };
    }

    static HSTraderBand Band(int from, int to, int count, float demo, params string[] groups)
    {
        return new HSTraderBand
        {
            FromHour = from,
            ToHour = to,
            Count = count,
            DemolisherChance = demo,
            Groups = groups
        };
    }

    public static void Load(string modPath)
    {
        Apply(new HSTraderSettingsData { Bands = DefaultBands().ToArray() });
        if (!string.IsNullOrEmpty(modPath))
            TryRead(Path.Combine(modPath, FileName));
        try
        {
            string user = GameIO.GetUserGameDataDir();
            if (!string.IsNullOrEmpty(user))
                TryRead(Path.Combine(user, "HSTraderProtection", FileName));
        }
        catch { }
        Log.Out("[HSTraderProtection] PrepDays=" + (PrepTicks / 24000UL)
            + " ObjectiveHours=" + (ObjectiveTicks / 1000UL)
            + " MaxAlive=" + MaxAlive
            + " PulseStart=" + (PulseStartPercent * 100f).ToString("0") + "%");
    }

    static void TryRead(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return;
        try
        {
            var data = JsonConvert.DeserializeObject<HSTraderSettingsData>(File.ReadAllText(path));
            if (data == null)
                return;
            Apply(data);
            Log.Out("[HSTraderProtection] Read " + path);
        }
        catch (Exception e)
        {
            Log.Error("[HSTraderProtection] Could not read " + path + ": " + e.Message);
        }
    }

    static void Apply(HSTraderSettingsData data)
    {
        int prepDays = Math.Max(1, data.PrepDays);
        int objHours = Math.Max(1, data.ObjectiveHours);
        float waveHours = data.WaveIntervalHours > 0f ? data.WaveIntervalHours : 1f;
        PrepTicks = (ulong)prepDays * 24000UL;
        ObjectiveTicks = (ulong)objHours * 1000UL;
        WaveIntervalTicks = (ulong)Math.Max(1, (int)(waveHours * 1000f));
        MaxAlive = Math.Max(1, data.MaxAlive);
        SpawnMinRadius = Math.Max(8, data.SpawnMinRadius);
        SpawnMaxRadius = Math.Max(SpawnMinRadius + 1, data.SpawnMaxRadius);
        MissionRadius = Math.Max(16, data.MissionRadius);
        float pulse = data.PulseStartPercent;
        if (pulse > 1f)
            pulse /= 100f;
        PulseStartPercent = Math.Max(0f, Math.Min(0.99f, pulse));
        SuppressTeleport = data.SuppressTeleport;
        SpawnsPerTick = Math.Max(1, data.SpawnsPerTick);
        if (data.Bands != null && data.Bands.Length > 0)
        {
            var loaded = new List<HSTraderBand>();
            for (int i = 0; i < data.Bands.Length; i++)
            {
                var b = data.Bands[i];
                if (b == null)
                    continue;
                if (b.Count < 1)
                    b.Count = 1;
                if (b.Groups == null || b.Groups.Length == 0)
                    b.Groups = new[] { "HS_TP_Normal" };
                loaded.Add(b);
            }
            if (loaded.Count > 0)
                Bands = loaded;
        }
    }

    public static HSTraderBand BandForHour(int hour)
    {
        if (Bands == null || Bands.Count == 0)
            Bands = DefaultBands();
        for (int i = 0; i < Bands.Count; i++)
        {
            var b = Bands[i];
            if (hour >= b.FromHour && hour < b.ToHour)
                return b;
        }
        return Bands[Bands.Count - 1];
    }
}
