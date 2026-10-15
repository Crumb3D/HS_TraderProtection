using System.Reflection;
using HarmonyLib;

public class HSTraderProtectionMod : IModApi
{
    public static string ModPath;

    public void InitMod(Mod _modInstance)
    {
        if (!HSGameVersion.AllowLoad("[HSTraderProtection]"))
            return;
        ModPath = _modInstance.Path;
        HSTraderConfig.Load(ModPath);
        HSTraderNet.RegisterPackage();
        ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
        ModEvents.WorldShuttingDown.RegisterHandler(OnWorldShuttingDown);
        ModEvents.PlayerSpawnedInWorld.RegisterHandler(HSTraderNet.OnPlayerSpawned);
        ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);
        try
        {
            var harmony = new Harmony("HSTraderProtection");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            HSTraderLock.Patch(harmony);
        }
        catch (System.Exception e)
        {
            Log.Error("[HSTraderProtection] Harmony patch failed. " + e);
        }
        Log.Out("[HSTraderProtection] Init v0.1.0; admin: hs_tp start | stop | status");
    }

    static void OnGameStartDone(ref ModEvents.SGameStartDoneData data)
    {
        try
        {
            HSTraderNet.RegisterPackage();
            HSTraderConfig.Load(ModPath);
            HSTraderMission.OnWorldStarted();
        }
        catch (System.Exception e)
        {
            Log.Error("[HSTraderProtection] Start failed. " + e);
        }
    }

    static void OnWorldShuttingDown(ref ModEvents.SWorldShuttingDownData data)
    {
        try
        {
            HSTraderMission.OnWorldShutdown();
        }
        catch (System.Exception e)
        {
            Log.Error("[HSTraderProtection] Shutdown failed. " + e);
        }
    }

    static void OnGameUpdate(ref ModEvents.SGameUpdateData data)
    {
        try
        {
            HSTraderMission.Tick();
        }
        catch (System.Exception e)
        {
            Log.Error("[HSTraderProtection] Tick failed. " + e);
        }
    }
}
