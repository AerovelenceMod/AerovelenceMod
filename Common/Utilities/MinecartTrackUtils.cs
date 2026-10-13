using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
namespace AerovelenceMod.Common.Utilities;
public static class MinecartTrackUtils
{
    private static readonly HashSet<int> trackTypes = new();
    private static readonly List<ILHook> hooks = new();
    public static void Register(int tileType) => trackTypes.Add(tileType);
    public static bool IsTrack(int tileType) => tileType == TileID.MinecartTrack || trackTypes.Contains(tileType);
    public static bool IsTrack(Tile tile) => tile.HasTile && IsTrack(tile.TileType);
    internal static void Load()
    {
        HookTrackChecks(typeof(Minecart), nameof(Minecart.IsPressurePlate), nameof(Minecart.TrackCollision),
            nameof(Minecart.FrameTrack), "GetNearbyTilesSetLookupIndex", nameof(Minecart.GetOnTrack),
            nameof(Minecart.OnTrack), nameof(Minecart.TrackColors));
        HookTrackChecks(typeof(Player), "QuickMinecart", "QuickMinecartSnap", "TryUsingDiggerCart",
            nameof(Player.GrappleMovement), "TryInteractingWithMinecartTrackInNearbyArea", "TileInteractionsUse",
            "TileInteractionsMouseOver", "PlaceThing_ValidTileForReplacement",
            "PlaceThing_Tiles_PlaceIt_GetLegacyTileStyle", "PlaceThing_Tiles_PlaceIt_AutoPaintAndActuate",
            "PlaceThing_Tiles_BlockPlacementForAssortedThings", "PlaceThing_Tiles_CheckRopeUsability",
            "IsTilePoundable", "ItemCheck_UseMiningTools_TryPoundingTile");
        HookTrackChecks(typeof(WorldGen), nameof(WorldGen.IsRope), nameof(WorldGen.WouldTileReplacementWork));
        On_WorldGen.PlaceTile += PlaceTrack;
    }
    internal static void Unload()
    {
        On_WorldGen.PlaceTile -= PlaceTrack;
        for (int i = hooks.Count - 1; i >= 0; i--)
            hooks[i].Dispose();
        hooks.Clear();
        trackTypes.Clear();
    }
    private static void HookTrackChecks(Type owner, params string[] methods)
    {
        foreach (string name in methods)
        {
            MethodInfo method = owner.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            if (method == null)
                throw new MissingMethodException(owner.FullName, name);
            hooks.Add(new ILHook(method, AllowModTracks));
        }
    }
    private static void AllowModTracks(ILContext il)
    {
        ILCursor cursor = new(il);
        int changes = 0;
        // vanilla only checks its own track ID so let our tracks through the same checks :))))))))))))))
        while (cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchLdcI4(TileID.MinecartTrack),
            instruction => instruction.OpCode.Code is Code.Ceq or Code.Beq or Code.Beq_S or Code.Bne_Un or Code.Bne_Un_S))
        {
            cursor.EmitDelegate<Func<int, int>>(tileType => IsTrack(tileType) ? TileID.MinecartTrack : tileType);
            cursor.Index++;
            changes++;
        }
        if (changes == 0)
            throw new InvalidOperationException($"Minecart track checks were not found in {il.Method.FullName}.");
    }
    private static bool PlaceTrack(On_WorldGen.orig_PlaceTile orig, int i, int j, int type, bool mute, bool forced, int player, int style)
    {
        bool placed = orig(i, j, type, mute, forced, player, style);
        if (!placed || !trackTypes.Contains(type) || !Main.tile[i, j].HasTile || Main.tile[i, j].TileType != type)
            return placed;
        Minecart.PlaceTrack(Main.tile[i, j], style);
        Main.tile[i, j].TileType = (ushort)type;
        WorldGen.SquareTileFrame(i, j);
        return true;
    }
}
