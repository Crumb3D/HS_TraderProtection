using System;
using System.Collections;
using System.IO;
using System.Reflection;
using HarmonyLib;

public static class HSTraderNet
{
    public const byte Snap = 1;
    public const byte CloseTrader = 2;
    public const byte CloseResults = 3;
    public const byte Tip = 4;

    public static bool IsAuthority
    {
        get
        {
            try
            {
                if (GameManager.IsDedicatedServer)
                    return true;
                var cm = ConnectionManager.Instance;
                if (cm != null)
                    return cm.IsServer;
            }
            catch { }
            return true;
        }
    }

    static Type pkgType;

    public static void RegisterPackage()
    {
        try
        {
            Type t = HSGameVersion.Is33
                ? HSGameApi.NetPackageType33("NetPackageHSTraderProtection", typeof(NetPackageHSTraderCore))
                : typeof(NetPackageHSTrader);
            pkgType = t;
            var f = typeof(NetPackageManager).GetField("knownPackageTypes", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null)
                return;
            var dict = f.GetValue(null) as IDictionary;
            if (dict == null)
                return;
            dict[t.Name] = t;
            if (HSGameVersion.Is33)
                MapEmitted33(t);
        }
        catch (Exception e)
        {
            Log.Error("[HSTraderProtection] Net package register failed. " + e);
        }
    }

    static void MapEmitted33(Type t)
    {
        var mapF = typeof(NetPackageManager).GetField("packageClassToPackageId", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (mapF == null)
            return;
        var map = mapF.GetValue(null) as IDictionary;
        if (map == null || map.Contains(t))
            return;
        var arrF = typeof(NetPackageManager).GetField("packageIdToClass", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (arrF == null)
            return;
        var arr = arrF.GetValue(null) as Type[];
        if (arr == null)
            return;
        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == null || arr[i] == t || arr[i].Name != t.Name)
                continue;
            map[t] = i;
            arr[i] = t;
            return;
        }
    }

    static NetPackageHSTraderCore Pkg()
    {
        if (pkgType == null)
            RegisterPackage();
        return (NetPackageHSTraderCore)HSGameApi.GetNetPackage(pkgType);
    }

    static void ToClients(NetPackage pkg)
    {
        var cm = ConnectionManager.Instance;
        if (cm == null || !cm.IsServer)
            return;
        cm.SendPackage(pkg);
    }

    static void ToServer(NetPackage pkg)
    {
        var cm = ConnectionManager.Instance;
        if (cm == null)
            return;
        cm.SendToClientsOrServer(pkg);
    }

    public static void BroadcastSnapshot()
    {
        if (!IsAuthority)
            return;
        var world = GameManager.Instance != null ? GameManager.Instance.World : null;
        if (world != null)
            HSTraderSnapshot.NowWorldTime = world.worldTime;
        var mem = new MemoryStream();
        using (var bw = new BinaryWriter(mem))
            HSTraderSnapshot.Write(bw);
        ToClients(Pkg().Of(Snap, mem.ToArray()));
        XUiC_HSTraderResults.ApplySnapshot();
    }

    public static void SendSnapshotTo(int entityId)
    {
        if (!IsAuthority)
            return;
        var cm = ConnectionManager.Instance;
        if (cm == null)
            return;
        var ci = cm.Clients.ForEntityId(entityId);
        if (ci == null)
            return;
        var mem = new MemoryStream();
        using (var bw = new BinaryWriter(mem))
            HSTraderSnapshot.Write(bw);
        ci.SendPackage(Pkg().Of(Snap, mem.ToArray()));
    }

    public static void BroadcastCloseTrader()
    {
        if (!IsAuthority)
            return;
        ToClients(Pkg().Of(CloseTrader, null));
        ApplyCloseTrader();
    }

    public static void BroadcastTip(string text)
    {
        if (!IsAuthority)
            return;
        ToClients(Pkg().Of(Tip, null, text));
        ApplyTip(text);
    }

    public static void SendCloseResults()
    {
        ToServer(Pkg().Of(CloseResults, null));
        if (IsAuthority)
        {
            var world = GameManager.Instance.World;
            var local = world != null ? world.GetPrimaryPlayer() : null;
            HSTraderMission.ResultsClosedBy(local != null ? local.entityId : 0);
        }
    }

    public static void Handle(byte kind, byte[] payload, string text, int senderId)
    {
        switch (kind)
        {
            case Snap:
                if (IsAuthority)
                    return;
                if (payload != null)
                {
                    using (var br = new BinaryReader(new MemoryStream(payload)))
                        HSTraderSnapshot.Read(br);
                    XUiC_HSTraderResults.ApplySnapshot();
                }
                break;
            case CloseTrader:
                ApplyCloseTrader();
                break;
            case CloseResults:
                if (IsAuthority)
                    HSTraderMission.ResultsClosedBy(senderId);
                break;
            case Tip:
                ApplyTip(text);
                break;
        }
    }

    static void ApplyCloseTrader()
    {
        try
        {
            var world = GameManager.Instance != null ? GameManager.Instance.World : null;
            var local = world != null ? world.GetPrimaryPlayer() : null;
            if (local == null || local.PlayerUI == null)
                return;
            local.PlayerUI.windowManager.Close("trader");
            local.PlayerUI.windowManager.Close("dialog");
        }
        catch { }
    }

    static void ApplyTip(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        try
        {
            var world = GameManager.Instance != null ? GameManager.Instance.World : null;
            var local = world != null ? world.GetPrimaryPlayer() as EntityPlayerLocal : null;
            if (local != null)
                GameManager.ShowTooltip(local, text);
        }
        catch { }
    }

    public static void OnPlayerSpawned(ref ModEvents.SPlayerSpawnedInWorldData data)
    {
        HSTraderMission.OnPlayerSpawned(data.EntityId);
    }
}

public abstract class NetPackageHSTraderCore : NetPackage
{
    public byte kind;
    public byte[] payload;
    public string text = "";

    public NetPackageHSTraderCore Of(byte k, byte[] data, string msg = "")
    {
        kind = k;
        payload = data;
        text = msg ?? "";
        return this;
    }

    public override void read(PooledBinaryReader br)
    {
        kind = br.ReadByte();
        text = br.ReadString();
        int n = br.ReadInt32();
        payload = n > 0 ? br.ReadBytes(n) : null;
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(kind);
        bw.Write(text ?? "");
        int n = payload != null ? payload.Length : 0;
        bw.Write(n);
        if (n > 0)
            bw.Write(payload);
    }

    public override void ProcessPackage(World world, GameManager callbacks)
    {
        try
        {
            int senderId = Sender != null ? Sender.entityId : 0;
            HSTraderNet.Handle(kind, payload, text, senderId);
        }
        catch (Exception e)
        {
            Log.Error("[HSTraderProtection] Net package failed. " + e);
        }
    }
}

#if !HS_33_ONLY
public sealed class NetPackageHSTrader : NetPackageHSTraderCore
{
    public override int GetLength()
    {
        return 16 + (text != null ? text.Length : 0) + (payload != null ? payload.Length : 0);
    }
}
#endif

[HarmonyPatch(typeof(NetPackageManager), "SetupBaseMapping")]
public static class HSTraderNetRegister
{
    static void Postfix()
    {
        HSTraderNet.RegisterPackage();
    }
}

[HarmonyPatch(typeof(NetPackageManager), "StartServer")]
public static class HSTraderNetRegisterServer
{
    static void Prefix()
    {
        if (HSGameVersion.Is33)
            HSTraderNet.RegisterPackage();
    }
}

[HarmonyPatch(typeof(NetPackageManager), "StartClient")]
public static class HSTraderNetRegisterClient
{
    static void Prefix()
    {
        if (HSGameVersion.Is33)
            HSTraderNet.RegisterPackage();
    }
}
