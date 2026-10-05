using System;
using System.Diagnostics;

using ModLanguage = AerovelenceMod.Common.Systems.Language.Language;



namespace AerovelenceMod.Common.Systems.Gas;

internal static class GasPerfControl
{
    internal static bool SimulationEnabled = true;
    internal static bool RenderingEnabled = true;
    internal static bool LightingEnabled = true;
    internal static double SimulationMs;
    internal static double RenderMs;
    internal static double LightingMs;
    private static double Ms(long start) => (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
    private static double Smooth(double oldValue, double value) => oldValue == 0 ? value : oldValue * 0.9 + value * 0.1;
    internal static void UpdateSimulation(long start) => SimulationMs = Smooth(SimulationMs, Ms(start));
    internal static void UpdateRender(long start) => RenderMs = Smooth(RenderMs, Ms(start));
    internal static void UpdateLighting(long start) => LightingMs = Smooth(LightingMs, Ms(start));
}

public sealed class GasPerfCommand : ModCommand
{
    private const string LocalizationKey = "Mods.AerovelenceMod.Commands.GasPerf.";

    public override void Load()
    {
        LocalizationManager.RegisterTranslation(LocalizationKey + "Usage", "/gasperf [status|sim|render|lights|off|on]", ModLanguage.Default);
        LocalizationManager.RegisterTranslation(LocalizationKey + "Description", "Profiles and isolates Aerovelence gas performance", ModLanguage.Default);
        LocalizationManager.RegisterTranslation(LocalizationKey + "Status", "Gas: simulation={0}, rendering={1}, lighting={2}", ModLanguage.Default);
        LocalizationManager.RegisterTranslation(LocalizationKey + "On", "on", ModLanguage.Default);
        LocalizationManager.RegisterTranslation(LocalizationKey + "Off", "off", ModLanguage.Default);
    }

    public override CommandType Type => CommandType.Chat;
    public override string Command => "gasperf";
    public override string Usage => LocalizationManager.GetTranslation(LocalizationKey + "Usage");
    public override string Description => LocalizationManager.GetTranslation(LocalizationKey + "Description");

    private static string Switch(bool value) => LocalizationManager.GetTranslation(LocalizationKey + (value ? "On" : "Off"));

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        string action = args.Length == 0 ? "status" : args[0].ToLowerInvariant();
        switch (action)
        {
            case "sim": GasPerfControl.SimulationEnabled = !GasPerfControl.SimulationEnabled; break;
            case "render": GasPerfControl.RenderingEnabled = !GasPerfControl.RenderingEnabled; break;
            case "lights": GasPerfControl.LightingEnabled = !GasPerfControl.LightingEnabled; break;
            case "off":
                GasPerfControl.SimulationEnabled = false;
                GasPerfControl.RenderingEnabled = false;
                GasPerfControl.LightingEnabled = false;
                break;
            case "on":
                GasPerfControl.SimulationEnabled = true;
                GasPerfControl.RenderingEnabled = true;
                GasPerfControl.LightingEnabled = true;
                break;
        }
        caller.Reply(string.Format(LocalizationManager.GetTranslation(LocalizationKey + "Status"), Switch(GasPerfControl.SimulationEnabled),
            Switch(GasPerfControl.RenderingEnabled), Switch(GasPerfControl.LightingEnabled)), Color.LightCyan);
    }
}
