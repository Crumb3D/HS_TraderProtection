using HarmonyLib;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class XUiC_HSTraderHud : XUiController
{
    Vector2i baseSize;
    bool haveBase;

    public override void Init()
    {
        base.Init();
        AlwaysUpdate = true;
        if (ViewComponent != null)
        {
            baseSize = ViewComponent.Size;
            haveBase = true;
        }
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        ApplyPulse();
        RefreshBindings();
    }

    void ApplyPulse()
    {
        if (!haveBase || ViewComponent == null)
            return;
        float t = 0f;
        if (HSTraderSnapshot.Phase == HSTraderPhase.Raiding && HSTraderSnapshot.ObjectiveTicks > 0)
            t = (float)HSTraderSnapshot.SurvivalTicksNow() / HSTraderSnapshot.ObjectiveTicks;
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;
        float start = HSTraderConfig.PulseStartPercent;
        float u = t <= start ? 0f : (t - start) / (1f - start);
        float phase = HSTraderSnapshot.SurvivalTicksNow() * 0.02f;
        float throb = u <= 0f ? 0f : u * (0.5f + 0.5f * Mathf.Sin(phase));
        float scale = 1f + throb * 0.18f;
        ViewComponent.Size = new Vector2i((int)(baseSize.x * scale), (int)(baseSize.y * scale));
    }

    public override bool GetBindingValueInternal(ref string _value, string _bindingName)
    {
        bool show = HSTraderSnapshot.ShowHud;
        switch (_bindingName)
        {
            case "show":
                _value = show ? "true" : "false";
                return true;
            case "title":
                _value = Localization.Get("hsTpTitle");
                return true;
            case "status":
                if (HSTraderSnapshot.Phase == HSTraderPhase.Preparing)
                    _value = Localization.Get("hsTpProtected");
                else if (HSTraderSnapshot.Phase == HSTraderPhase.Raiding)
                    _value = Localization.Get("hsTpUnderAttack") + " — WAVE " + HSTraderSnapshot.Wave
                        + "  " + HSTraderSnapshot.SurvivalString(HSTraderSnapshot.RemainingWaveTicks());
                else
                    _value = "";
                return true;
            case "timer":
                if (HSTraderSnapshot.Phase == HSTraderPhase.Preparing)
                    _value = HSTraderSnapshot.RaidInString(HSTraderSnapshot.RemainingPrepTicks());
                else if (HSTraderSnapshot.Phase == HSTraderPhase.Raiding)
                    _value = Localization.Get("hsTpSurvived") + " " + HSTraderSnapshot.SurvivalString(HSTraderSnapshot.SurvivalTicksNow());
                else
                    _value = "";
                return true;
            case "healthlabel":
                if (HSTraderSnapshot.Phase == HSTraderPhase.Raiding)
                    _value = Localization.Get("hsTpHealth") + "  " + HSTraderSnapshot.Health + " / " + HSTraderSnapshot.MaxHealth;
                else
                    _value = "";
                return true;
            case "healthfill":
                if (HSTraderSnapshot.MaxHealth > 0)
                    _value = ((float)HSTraderSnapshot.Health / HSTraderSnapshot.MaxHealth).ToString("0.###");
                else
                    _value = "0";
                return true;
            case "showhealth":
                _value = HSTraderSnapshot.Phase == HSTraderPhase.Raiding ? "true" : "false";
                return true;
        }
        return base.GetBindingValueInternal(ref _value, _bindingName);
    }
}

[HarmonyPatch(typeof(ReflectionHelpers), "GetTypeWithPrefix")]
static class HSTraderXuiTypePatch
{
    static void Postfix(string _prefix, string _name, ref System.Type __result)
    {
        if (__result != null || _prefix != "XUiC_" || string.IsNullOrEmpty(_name))
            return;
        if (_name == "HSTraderHud" || _name.StartsWith("HSTraderHud"))
            __result = typeof(XUiC_HSTraderHud);
        else if (_name == "HSTraderResults" || _name.StartsWith("HSTraderResults"))
            __result = typeof(XUiC_HSTraderResults);
    }
}
