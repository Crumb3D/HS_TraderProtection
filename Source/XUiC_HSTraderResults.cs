using UnityEngine.Scripting;

[Preserve]
public class XUiC_HSTraderResults : XUiController
{
    public const string WindowId = "hsTraderResults";
    static bool openRequested;
    static bool closing;

    public override void Init()
    {
        base.Init();
        var btn = GetChildByType<XUiC_SimpleButton>();
        if (btn != null)
            btn.OnPressed += OnClosePressed;
        var named = GetChildById("btnClose") as XUiC_SimpleButton;
        if (named != null)
            named.OnPressed += OnClosePressed;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        RefreshBindings();
    }

    public override void OnClose()
    {
        base.OnClose();
        if (!closing)
            HSTraderNet.SendCloseResults();
        closing = false;
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        RefreshBindings();
    }

    void OnClosePressed(XUiController sender, int mouse)
    {
        CloseLocal();
    }

    public static void ApplySnapshot()
    {
        if (GameManager.IsDedicatedServer)
            return;
        var world = GameManager.Instance != null ? GameManager.Instance.World : null;
        var player = world != null ? world.GetPrimaryPlayer() as EntityPlayerLocal : null;
        if (player == null || player.PlayerUI == null)
            return;
        var wm = player.PlayerUI.windowManager;
        if (HSTraderSnapshot.Phase == HSTraderPhase.Results)
        {
            if (!wm.IsWindowOpen(WindowId) && !openRequested)
            {
                openRequested = true;
                wm.Open(WindowId, true);
            }
        }
        else
        {
            openRequested = false;
            if (wm.IsWindowOpen(WindowId))
            {
                closing = true;
                wm.Close(WindowId);
            }
        }
    }

    void CloseLocal()
    {
        if (xui != null && xui.playerUI != null)
            xui.playerUI.windowManager.Close(WindowId);
    }

    public override bool GetBindingValueInternal(ref string _value, string _bindingName)
    {
        var s = HSTraderSnapshot.Stats ?? new HSTraderStatsData();
        switch (_bindingName)
        {
            case "outcome":
                _value = HSTraderSnapshot.Outcome == HSTraderOutcome.Success
                    ? Localization.Get("hsTpSuccess")
                    : Localization.Get("hsTpFailure");
                return true;
            case "outcomecolor":
                _value = HSTraderSnapshot.Outcome == HSTraderOutcome.Success ? "80,220,80,255" : "220,60,60,255";
                return true;
            case "lead":
                _value = LeadText(s);
                return true;
            case "team":
                _value = TeamText(s);
                return true;
            case "players":
                _value = PlayersText(s);
                return true;
        }
        return base.GetBindingValueInternal(ref _value, _bindingName);
    }

    static string LeadText(HSTraderStatsData s)
    {
        return Localization.Get("hsTpSurvival") + "  " + HSTraderSnapshot.SurvivalString(s.SurvivalTicks) + "\n"
            + Localization.Get("hsTpHealth") + "  " + s.EndHealth + " / " + s.EndMaxHealth + "\n"
            + Localization.Get("hsTpDamageTaken") + "  " + s.DamageTaken + "\n"
            + Localization.Get("hsTpWaves") + "  " + s.WavesSurvived + "  (final " + s.FinalWave + ")\n"
            + Localization.Get("hsTpObjective") + "  " + (s.ObjectiveComplete ? Localization.Get("hsTpYes") : Localization.Get("hsTpNo")) + "\n"
            + Localization.Get("hsTpCause") + "  " + (string.IsNullOrEmpty(s.CauseOfDeath) ? "-" : s.CauseOfDeath) + "\n"
            + Localization.Get("hsTpAttacker") + "  " + (string.IsNullOrEmpty(s.AttackerClass) ? "-" : s.AttackerClass);
    }

    static string TeamText(HSTraderStatsData s)
    {
        return Localization.Get("hsTpTeam") + "\n"
            + Localization.Get("hsTpKills") + "  " + s.TotalKills + "\n"
            + "Normal " + s.NormalKills
            + "   Feral " + s.FeralKills
            + "   Radiated " + s.RadiatedKills + "\n"
            + "Charged " + s.ChargedKills
            + "   Infernal " + s.InfernalKills
            + "   Demolisher " + s.DemolisherKills + "\n"
            + Localization.Get("hsTpPlayerKills") + "  " + s.PlayerKills
            + "   " + Localization.Get("hsTpTurretKills") + "  " + s.TurretKills
            + "   " + Localization.Get("hsTpTrapKills") + "  " + s.TrapKills + "\n"
            + Localization.Get("hsTpHeadshots") + "  " + s.Headshots
            + "   " + Localization.Get("hsTpAmmo") + "  " + s.AmmoFired + " / " + s.Hits + "\n"
            + Localization.Get("hsTpDeaths") + "  " + s.PlayerDeaths + "\n"
            + Localization.Get("hsTpBlocks") + "  " + s.BlocksDestroyed + " dest / " + s.BlocksPlaced + " placed / " + s.BlocksUpgraded + " upgraded";
    }

    static string PlayersText(HSTraderStatsData s)
    {
        if (s.Players == null || s.Players.Count == 0)
            return Localization.Get("hsTpNoPlayers");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(Localization.Get("hsTpPlayers"));
        for (int i = 0; i < s.Players.Count; i++)
        {
            var p = s.Players[i];
            sb.Append(p.Name).Append("  K ").Append(p.Kills)
                .Append("  Dmg ").Append(p.DamageDealt)
                .Append("  HS ").Append(p.Headshots)
                .Append("  Ammo ").Append(p.AmmoFired).Append("/").Append(p.Hits)
                .Append("  Deaths ").Append(p.Deaths)
                .Append("  Turret ").Append(p.TurretKills)
                .Append('\n');
        }
        return sb.ToString();
    }
}
