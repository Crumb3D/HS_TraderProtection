using System;
using System.Collections.Generic;

public class ConsoleCmdHSTraderProtection : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new[] { "hs_tp", "hstraderprotection" };
    }

    public override string getDescription()
    {
        return "HS Trader Protection admin: start, stop, status, resetstats.";
    }

    public override int DefaultPermissionLevel { get { return 1000; } }

    public override string getHelp()
    {
        return
            "On a dedicated server, type these in the in-game F1 console after you join.\n" +
            "hs_tp start [traderEntityId]  - start preparation at the trader you are standing with\n" +
            "hs_tp stop                    - reload the compound, restore the trader, idle (no results)\n" +
            "hs_tp status                  - countdown or raid state\n" +
            "hs_tp resetstats              - clear stats while idle\n" +
            "hs_tp debug raid|damage|kill  - test helpers";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            if (!HSTraderNet.IsAuthority)
            {
                Out("Trader Protection commands run on the server.");
                return;
            }
            var world = GameManager.Instance != null ? GameManager.Instance.World : null;
            string sub = _params != null && _params.Count > 0 ? _params[0].ToLowerInvariant() : "status";
            var player = ResolvePlayer(world, _senderInfo);
            Out(Run(sub, world, player, _params));
        }
        catch (Exception e)
        {
            Log.Error("[HSTraderProtection] Command failed. " + e);
            Out("Command failed: " + e.Message);
        }
    }

    static string Run(string sub, World world, EntityPlayer player, List<string> p)
    {
        switch (sub)
        {
            case "start":
                int id = 0;
                if (p != null && p.Count > 1)
                    int.TryParse(p[1], out id);
                return HSTraderMission.Start(world, player, id);
            case "stop":
                return HSTraderMission.Stop(world);
            case "status":
                return HSTraderMission.Status(world);
            case "resetstats":
                return HSTraderMission.ResetStats();
            case "debug":
                string d = p != null && p.Count > 1 ? p[1].ToLowerInvariant() : "";
                if (d == "raid")
                    return HSTraderMission.DebugRaid(world);
                if (d == "damage")
                {
                    int amt = 1000;
                    if (p != null && p.Count > 2)
                        int.TryParse(p[2], out amt);
                    return HSTraderMission.DebugDamage(world, amt);
                }
                if (d == "kill")
                    return HSTraderMission.DebugKill(world);
                return "debug raid | damage [amount] | kill";
            default:
                return getHelpStatic();
        }
    }

    static string getHelpStatic()
    {
        return "hs_tp start | stop | status | resetstats | debug raid|damage|kill";
    }

    static EntityPlayer ResolvePlayer(World world, CommandSenderInfo sender)
    {
        if (world == null)
            return null;
        if (sender.RemoteClientInfo != null)
        {
            var fromNet = world.GetEntity(sender.RemoteClientInfo.entityId) as EntityPlayer;
            if (fromNet != null)
                return fromNet;
        }
        var local = world.GetPrimaryPlayer();
        if (local != null)
            return local;
        var locals = world.GetLocalPlayers();
        if (locals != null && locals.Count > 0)
            return locals[0] as EntityPlayer;
        return null;
    }

    static void Out(string msg)
    {
        SdtdConsole.Instance.Output(msg);
    }
}
