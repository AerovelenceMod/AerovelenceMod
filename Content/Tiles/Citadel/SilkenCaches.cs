using System;
using System.Collections.Generic;
using System.Linq;
using AerovelenceMod.Common.Systems.Generation.CrystalCaverns;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Common.Utilities.Generation;
using AerovelenceMod.Common.Utilities.Generation.StructureStamper;
using AerovelenceMod.Content.NPCs.CrystalCaverns;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Furniture;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Rubble;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.WorldBuilding;

namespace AerovelenceMod.Content.Tiles.Citadel;

public class SilkenCacheTile : ModTile
{
    public override string Texture => "AerovelenceMod/Content/Tiles/CrystalCaverns/Furniture/CavernChestTile";
    protected virtual bool Container => true;

    public override void SetStaticDefaults()
    {
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
        this.SimpleFrameImportantTile(2, 5, SoundID.Grass, DustID.Web, new Color(165, 162, 194), solidTop: false,
            anchorTop: new AnchorData(AnchorType.SolidTile, 2, 0), mapName: CreateMapEntryName(), configure: data =>
            {
                data.AnchorBottom = AnchorData.Empty;
                data.StyleHorizontal = true;
                data.LavaDeath = data.WaterDeath = false;
                data.AnchorInvalidTiles = [TileID.MagicalIceBlock, TileID.Sand, TileID.Silt, TileID.Slush];
                if (!Container) return;
                data.HookCheckIfCanPlace = new PlacementHook(CheckStorage, -1, 0, true);
                data.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, false);
            });
        Main.tileBlockLight[Type] = false;
        Main.tileCut[Type] = !Container;
        Main.tileNoFail[Type] = !Container;
        Main.tileLavaDeath[Type] = false;
        Main.tileWaterDeath[Type] = false;
        Main.tileSpelunker[Type] = Container;
        Main.tileShine2[Type] = Container;
        Main.tileShine[Type] = Container ? 1200 : 0;
        Main.tileContainer[Type] = Container;
        Main.tileOreFinderPriority[Type] = (short)(Container ? 500 : 0);
        TileID.Sets.BasicChest[Type] = Container;
        TileID.Sets.IsAContainer[Type] = Container;
        TileID.Sets.DisableSmartCursor[Type] = true;
        TileID.Sets.DoesntGetReplacedWithTileReplacement[Type] = true;
        TileID.Sets.PreventsSandfall[Type] = true;
        if (Container) RegisterItemDrop(ModContent.ItemType<SilkenCacheItem>());
    }

    public static bool IsCache(int type) => type == ModContent.TileType<SilkenCacheTile>() || type == ModContent.TileType<SilkenCocoonTile>();
    private static int CheckStorage(int x, int y, int type, int style, int direction, int alternate)
    {
        int existing = Chest.FindChest(x, y);
        return existing >= 0 && Main.tile[x, y].HasTile && Main.tile[x, y].TileType == type ? existing : Chest.FindEmptyChest(x, y);
    }
    public static Point Root(int i, int j) => new(i - Main.tile[i, j].TileFrameX % 36 / 18, j - Main.tile[i, j].TileFrameY % 90 / 18);
    public static bool CanRelease(Point root)
    {
        if (Main.tile[root.X, root.Y].TileType != ModContent.TileType<SilkenCacheTile>()) return true;
        int chest = Chest.FindChest(root.X, root.Y);
        if (chest < 0) return true;
        return chest >= 0 && Main.chest[chest] != null && Main.chest[chest].item.All(item => item == null || item.IsAir) &&
            !Main.player.Any(player => player.active && player.chest == chest);
    }
    public override bool CanKillTile(int i, int j, ref bool blockDamaged) => CanRelease(Root(i, j));
    public override bool CanExplode(int i, int j) => CanRelease(Root(i, j));
    public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient && !CanRelease(Root(i, j))) fail = true;
    }
    public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => CanRelease(Root(i, j));
    public override bool Slope(int i, int j) => false;
    public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
    public override bool RightClick(int i, int j)
    {
        Point root = Root(i, j);
        if (Container) return CommonTileHelper.HandleRightClick(this, root.X, root.Y, Main.LocalPlayer, ItemID.None);
        WorldGen.KillTile(i, j);
        if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.TileManipulation, number: 0, number2: i, number3: j);
        return true;
    }
    public override void MouseOver(int i, int j)
    {
        Main.LocalPlayer.noThrow = 2;
        Main.LocalPlayer.cursorItemIconEnabled = true;
        Main.LocalPlayer.cursorItemIconID = Container ? ModContent.ItemType<SilkenCacheItem>() : ItemID.Silk;
    }
    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        SilkenCacheMotion.Forget(new Point(i, j));
        if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, i, j, 2, 5);
        if (Container)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) Chest.DestroyChestDirect(i, j, Chest.FindChest(i, j));
            else Chest.DestroyChest(i, j);
            return;
        }
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        int kind = frameX / 36;
        var source = new EntitySource_TileBreak(i, j);
        if (kind == 1) NPC.NewNPC(source, (i + 1) * 16, (j + 4) * 16, ModContent.NPCType<SilkenCacheMoth>());
        else if (kind == 2)
        {
            Item.NewItem(source, i * 16, j * 16 + 48, 32, 32, ItemID.SilverCoin, Main.rand.Next(1, 4));
            if (Main.rand.NextBool(3)) Item.NewItem(source, i * 16, j * 16 + 48, 32, 32, ItemID.HealingPotion);
        }
        Item.NewItem(source, i * 16, j * 16 + 32, 32, 32, ItemID.Cobweb, Main.rand.Next(1, 4));
    }
    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        if (Root(i, j) == new Point(i, j)) Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.CustomNonSolid);
        return false;
    }
    public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch) => SilkenCacheMotion.Draw(new Point(i, j), spriteBatch);
}

public sealed class SilkenCocoonTile : SilkenCacheTile
{
    protected override bool Container => false;
}

public sealed class SilkenCacheItem : ModItem
{
    public override string Texture => "AerovelenceMod/Content/Tiles/CrystalCaverns/Furniture/CavernChestItem";
    public override void SetDefaults() => Item.DefaultToPlaceableTile(ModContent.TileType<SilkenCacheTile>());
}

public sealed class SilkenCacheAnchor : GlobalTile
{
    public static bool LockedBelow(int i, int j)
    {
        if (!WorldGen.InWorld(i, j + 1)) return false;
        Tile below = Main.tile[i, j + 1];
        return below.HasTile && SilkenCacheTile.IsCache(below.TileType) && below.TileFrameY % 90 == 0 &&
            !SilkenCacheTile.CanRelease(SilkenCacheTile.Root(i, j + 1));
    }
    public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged) => !LockedBelow(i, j);
    public override bool CanExplode(int i, int j, int type) => !LockedBelow(i, j);
    public override bool Slope(int i, int j, int type) => !LockedBelow(i, j);
    public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced) => !LockedBelow(i, j);
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient && LockedBelow(i, j)) fail = true;
    }
}

public sealed class SilkenCacheMoth : Charger
{
    public override string Texture => "AerovelenceMod/Content/NPCs/CrystalCaverns/Charger";
    public override void SetDefaults()
    {
        base.SetDefaults();
        NPC.scale = .55f; NPC.width = NPC.height = 24;
        NPC.lifeMax = 35; NPC.damage = 12; NPC.value = 0;
    }
    public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0;
}

public sealed class SilkenCocoonWeapon : GlobalItem
{
    public override void MeleeEffects(Item item, Player player, Rectangle hitbox)
    {
        if (player.whoAmI == Main.myPlayer && item.damage > 0 && !item.noMelee) SilkenCacheMotion.Cut(hitbox);
    }
}

public sealed class SilkenCocoonProjectile : GlobalProjectile
{
    public override void PostAI(Projectile projectile)
    {
        if (!projectile.friendly || projectile.damage <= 0 || projectile.owner != Main.myPlayer || ProjectileLoader.CanDamage(projectile) == false) return;
        Rectangle hitbox = projectile.Hitbox;
        if (Vector2.DistanceSquared(projectile.position, projectile.oldPosition) < 128 * 128)
            hitbox = Rectangle.Union(hitbox, new Rectangle((int)projectile.oldPosition.X, (int)projectile.oldPosition.Y, projectile.width, projectile.height));
        SilkenCacheMotion.Cut(hitbox);
    }
}

public sealed class SilkenCachePass : GenPass
{
    public SilkenCachePass() : base("Weaving silken caches", 12) { }
    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        Rectangle bounds = SilkenCitadelWorld.Bounds;
        if (bounds.IsEmpty) return;
        progress.Message = "Weaving silken caches";
        int placed = 0, chests = 0, target = Math.Clamp(bounds.Width * bounds.Height / 6000, 16, 65);
        List<Point> anchors = new();
        for (int attempt = 0; attempt < target * 250 && placed < target; attempt++)
        {
            int x = WorldGen.genRand.Next(bounds.Left + 10, bounds.Right - 12), y = WorldGen.genRand.Next(bounds.Top + 12, bounds.Bottom - 15);
            Rectangle space = new(x - 1, y - 1, 4, 7);
            if (AeroStructure.ProtectedStructures.Any(area => area.Intersects(space)) || anchors.Any(p => Math.Abs(x - p.X) < 12 && Math.Abs(y - p.Y) < 10)) continue;
            if (!WorldGen.SolidTile(x, y - 1) || !WorldGen.SolidTile(x + 1, y - 1)) continue;
            ushort ceiling = Main.tile[x, y - 1].TileType;
            CCTerrainPass terrain = CCTerrainPass.Instance();
            if (ceiling != terrain.StoneTile && ceiling != terrain.DirtTile && ceiling != terrain.ChargedTile && ceiling != terrain.LushTile) continue;
            if (Enumerable.Range(0, 2).Any(dx => Enumerable.Range(0, 5).Any(dy => Main.tile[x + dx, y + dy].HasTile || Main.tile[x + dx, y + dy].LiquidAmount > 0))) continue;
            int roll = WorldGen.genRand.Next(100), kind = roll < 52 ? 0 : roll < 68 ? 1 : roll < 82 ? 2 : roll < 92 ? 3 : 4;
            if (!Place(x, y, kind, true)) continue;
            placed++; if (kind == 0) chests++;
            anchors.Add(new Point(x, y));
            new AeroStructure(new Vector2(x - 1, y - 1), 4, 7, "silkencache").ProtectStructure();
        }
        ModContent.GetInstance<AerovelenceMod>().Logger.Info($"Silken caches placed: {placed}, chests={chests}, cocoons and remnants={placed - chests}.");
    }
    public static bool Place(int x, int y, int kind, bool loot)
    {
        int index = kind == 0 ? Chest.CreateChest(x, y) : -1;
        if (kind == 0 && index < 0) return false;
        Stamp(x, y, kind);
        if (kind == 0 && loot)
            new AeroStructure(new Vector2(x, y), 2, 5, "silkencacheloot").ApplyItemConfigurationsToAll(WorldGen.genRand,
                HouseGenerator.CreatePrimaryLootPool(), HouseGenerator.CreateSecondaryLootPool());
        return true;
    }
    internal static void Stamp(int x, int y, int kind)
    {
        ushort type = (ushort)(kind == 0 ? ModContent.TileType<SilkenCacheTile>() : ModContent.TileType<SilkenCocoonTile>());
        for (int dx = 0; dx < 2; dx++)
            for (int dy = 0; dy < 5; dy++)
            {
                Tile tile = Main.tile[x + dx, y + dy]; tile.ResetToType(type);
                tile.TileFrameX = (short)(kind * 36 + dx * 18); tile.TileFrameY = (short)(dy * 18);
            }
    }
}

public sealed class SilkenCacheMotion : ModSystem
{
    private sealed class Strand
    {
        public readonly List<VerletSegment> Nodes = new();
        public readonly float Phase;
        public ulong Seen;
        public Strand(Point root)
        {
            Phase = ((root.X * 73856093L ^ root.Y * 19349663L) & 1023) / 1024f * MathHelper.TwoPi;
            Vector2 anchor = new(root.X * 16 + 16, root.Y * 16 + 17);
            for (int n = 0; n < 10; n++) Nodes.Add(new VerletSegment(anchor + new Vector2(0, n * 4.8f)));
            Nodes[0].isFixed = true;
        }
        public void Update()
        {
            float time = Main.GameUpdateCount / 60f;
            for (int n = 1; n < Nodes.Count; n++)
            {
                VerletSegment node = Nodes[n]; Vector2 previous = node.currentPosition;
                Vector2 wind = new(MathF.Sin(time * 1.3f + Phase + n * .11f) * .014f + Main.windSpeedCurrent * .035f, .15f);
                foreach (Player player in Main.player)
                    if (player.active && !player.dead)
                    {
                        float influence = MathHelper.Clamp(1 - Vector2.Distance(player.Center, node.currentPosition) / 105, 0, 1);
                        wind.X += MathHelper.Clamp(player.velocity.X, -12, 12) * influence * .027f;
                        wind.Y += MathHelper.Clamp(player.velocity.Y, -8, 8) * influence * .006f;
                    }
                node.currentPosition += (node.currentPosition - node.oldPosition) * .972f + wind;
                node.oldPosition = previous;
            }
            for (int iteration = 0; iteration < 12; iteration++)
                for (int n = 1; n < Nodes.Count; n++)
                {
                    VerletSegment a = Nodes[n - 1], b = Nodes[n]; Vector2 delta = b.currentPosition - a.currentPosition;
                    float length = delta.Length();
                    if (length < .001f) continue;
                    Vector2 correction = delta * ((length - 4.8f) / length);
                    if (a.isFixed) b.currentPosition -= correction;
                    else { a.currentPosition += correction * .5f; b.currentPosition -= correction * .5f; }
                }
        }
    }
    private static readonly Dictionary<Point, Strand> strands = new();
    internal static void Cut(Rectangle hitbox)
    {
        HashSet<Point> hits = null;
        int type = ModContent.TileType<SilkenCocoonTile>();
        for (int x = Math.Max(1, hitbox.Left / 16); x <= Math.Min(Main.maxTilesX - 2, hitbox.Right / 16); x++)
            for (int y = Math.Max(1, hitbox.Top / 16); y <= Math.Min(Main.maxTilesY - 2, hitbox.Bottom / 16); y++)
                if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == type)
                    (hits ??= new()).Add(SilkenCacheTile.Root(x, y));
        foreach (var (root, strand) in strands)
        {
            if (!hitbox.Intersects(new Rectangle(root.X * 16 - 56, root.Y * 16 - 40, 144, 144))) continue;
            Tile tile = Main.tile[root.X, root.Y];
            if (!tile.HasTile || tile.TileType != type) continue;
            int end = tile.TileFrameX / 36 == 4 ? 5 : strand.Nodes.Count;
            Vector2 tip = strand.Nodes[end - 1].currentPosition;
            bool hit = hitbox.Intersects(new Rectangle((int)tip.X - 16, (int)tip.Y - 4, 32, end == 5 ? 14 : 32));
            float distance = 0;
            for (int n = 1; n < end && !hit; n++)
                hit = Collision.CheckAABBvLineCollision(new Vector2(hitbox.X, hitbox.Y), new Vector2(hitbox.Width, hitbox.Height),
                    strand.Nodes[n - 1].currentPosition, strand.Nodes[n].currentPosition, 6, ref distance);
            if (hit) (hits ??= new()).Add(root);
        }
        if (hits == null) return;
        foreach (Point root in hits)
        {
            WorldGen.KillTile(root.X, root.Y);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.TileManipulation, number: 0, number2: root.X, number3: root.Y);
        }
    }
    public override void Load() => On_WorldGen.PlaceChestDirect += ReceivePlacement;
    private static void ReceivePlacement(On_WorldGen.orig_PlaceChestDirect orig, int x, int y, ushort type, int style, int id)
    {
        if (type != ModContent.TileType<SilkenCacheTile>()) { orig(x, y, type, style, id); return; }
        Chest.CreateChest(x, y, id);
        SilkenCachePass.Stamp(x, y, 0);
    }
    public static void Forget(Point root) => strands.Remove(root);
    public override void ClearWorld() => strands.Clear();
    public override void Unload()
    {
        On_WorldGen.PlaceChestDirect -= ReceivePlacement;
        strands.Clear();
    }
    public override void PostUpdateInput()
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused || Main.LocalPlayer.mouseInterface || Main.LocalPlayer.dead) return;
        Point mouse = Main.MouseWorld.ToTileCoordinates();
        if (!Main.LocalPlayer.InInteractionRange(mouse.X, mouse.Y, TileReachCheckSettings.Simple)) return;
        foreach (var (root, strand) in strands)
        {
            if (!Main.tile[root.X, root.Y].HasTile || Main.GameUpdateCount - strand.Seen > 2) continue;
            int end = Main.tile[root.X, root.Y].TileFrameX / 36 == 4 ? 5 : strand.Nodes.Count;
            bool hit = end == strand.Nodes.Count && Vector2.DistanceSquared(Main.MouseWorld, strand.Nodes[^1].currentPosition + new Vector2(0, 8)) < 18 * 18;
            for (int n = 1; n < end && !hit; n++)
            {
                Vector2 a = strand.Nodes[n - 1].currentPosition, delta = strand.Nodes[n].currentPosition - a;
                float t = MathHelper.Clamp(Vector2.Dot(Main.MouseWorld - a, delta) / Math.Max(.001f, delta.LengthSquared()), 0, 1);
                hit = Vector2.DistanceSquared(Main.MouseWorld, a + delta * t) < 6 * 6;
            }
            if (!hit) continue;
            ModTile tile = TileLoader.GetTile(Main.tile[root.X, root.Y].TileType);
            tile.MouseOver(root.X, root.Y);
            if (Main.mouseRight && Main.mouseRightRelease)
            {
                tile.RightClick(root.X, root.Y);
                Main.mouseRightRelease = false;
            }
            break;
        }
    }
    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        foreach (Point root in strands.Keys.ToArray())
        {
            Strand strand = strands[root];
            if (Main.GameUpdateCount - strand.Seen > 180 || !Main.tile[root.X, root.Y].HasTile || !SilkenCacheTile.IsCache(Main.tile[root.X, root.Y].TileType))
            { strands.Remove(root); continue; }
            if (!Main.gamePaused) strand.Update();
        }
    }
    public static void Draw(Point root, SpriteBatch batch)
    {
        if (!strands.TryGetValue(root, out Strand strand)) strands[root] = strand = new Strand(root);
        strand.Seen = Main.GameUpdateCount;
        Tile tile = Main.tile[root.X, root.Y]; int kind = tile.TileType == ModContent.TileType<SilkenCacheTile>() ? 0 : tile.TileFrameX / 36;
        float time = Main.GlobalTimeWrappedHourly, phase = strand.Phase;
        Vector2 anchor = strand.Nodes[0].currentPosition, tip = strand.Nodes[^1].currentPosition;
        Vector2 tangent = tip - strand.Nodes[^2].currentPosition;
        float rotation = MathHelper.Clamp(tangent.ToRotation() - MathHelper.PiOver2 + MathF.Sin(phase) * .13f, -.6f, .6f);
        Vector2 body = tip + new Vector2(0, 8).RotatedBy(rotation);
        Color light = Lighting.GetColor(root.X, root.Y + 3);
        Color silk = Color.Lerp(light, new Color(214, 211, 241), .16f);
        bool treasure = kind == 0 && Main.LocalPlayer.findTreasure;
        if (treasure) silk = Color.Lerp(silk, new Color(255, 222, 117), .65f);
        Vector2 offset = Main.screenPosition;
        Texture2D pixel = TextureAssets.MagicPixel.Value;

        void Line(Vector2 a, Vector2 b, Color color, float width = 1)
        {
            Vector2 delta = b - a;
            if (delta.LengthSquared() < .001f) return;
            batch.Draw(pixel, a - offset, new Rectangle(0, 0, 1, 1), color, delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
        }
        Vector2 Curve(Vector2 a, Vector2 bend, Vector2 b, float t) => Vector2.Lerp(Vector2.Lerp(a, bend, t), Vector2.Lerp(bend, b, t), t);
        void Thread(Vector2 a, Vector2 bend, Vector2 b, Color color, int steps = 10)
        {
            Vector2 old = a;
            for (int n = 1; n <= steps; n++) { Vector2 next = Curve(a, bend, b, n / (float)steps); Line(old, next, color); old = next; }
        }
        Vector2[] roots = new Vector2[7];
        for (int n = 0; n < roots.Length; n++)
        {
            float u = n / 6f;
            roots[n] = new Vector2(root.X * 16 + 2 + u * 28, root.Y * 16 - 1 + MathF.Sin(phase + n * 2.7f) * 2);
            Vector2 bend = Vector2.Lerp(roots[n], anchor, .55f) + new Vector2(MathF.Sin(time * .7f + phase + n) * 1.8f, 3 + MathF.Sin(n + phase) * 2);
            Thread(roots[n], bend, anchor, silk * (.35f + .05f * (n % 3)));
        }
        for (int row = 1; row <= 4; row++)
            for (int n = 0; n < roots.Length - 1; n++)
            {
                float t = row / 5f + .025f * MathF.Sin(n * 2 + phase);
                Vector2 a = Vector2.Lerp(roots[n], anchor, t), b = Vector2.Lerp(roots[n + 1], anchor, t);
                Thread(a, (a + b) * .5f + new Vector2(0, 1.7f + .8f * MathF.Sin(time + phase)), b, silk * .3f, 4);
            }
        int end = kind == 4 ? 5 : strand.Nodes.Count;
        for (int n = 1; n < end; n++)
        {
            Vector2 a = strand.Nodes[n - 1].currentPosition, b = strand.Nodes[n].currentPosition;
            Line(a, b, silk * .18f, 3);
            for (int ply = 0; ply < 3; ply++)
            {
                float wa = MathF.Sin(n * 1.1f + phase + ply * 2.1f + time * .5f) * .9f;
                float wb = MathF.Sin((n + 1) * 1.1f + phase + ply * 2.1f + time * .5f) * .9f;
                Line(a + new Vector2(wa, 0), b + new Vector2(wb, 0), silk * (.38f + ply * .14f));
            }
            float glint = MathF.Pow(Math.Max(0, MathF.Sin(n * .71f - time * 1.1f + phase)), 16);
            if (glint > .3f) Line(b - Vector2.UnitX, b + Vector2.UnitX, silk * glint);
        }
        if (kind == 4)
        {
            Vector2 torn = strand.Nodes[4].currentPosition;
            for (int n = 0; n < 4; n++) Thread(torn, torn + new Vector2((n - 1.5f) * 3, 4), torn + new Vector2((n - 1.5f) * 3 + MathF.Sin(time + phase + n) * 2, 8 - n), silk * .45f, 5);
            return;
        }
        if (kind == 0)
        {
            Texture2D chest = ModContent.Request<Texture2D>("AerovelenceMod/Content/Tiles/CrystalCaverns/Furniture/CavernChestTile").Value;
            int index = Chest.FindChest(root.X, root.Y), frame = index >= 0 ? Math.Clamp(Main.chest[index].frame, 0, 2) : 0;
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                {
                    Vector2 local = new Vector2(x * 16 - 8, y * 16 - 8) * .74f;
                    Rectangle source = new(x * 18, frame * 38 + y * 18, 16, 16);
                    if (source.Bottom > chest.Height) source.Y = y * 18;
                    Vector2 position = body + local.RotatedBy(rotation) - offset;
                    if (treasure)
                        for (int glow = 0; glow < 4; glow++) batch.Draw(chest, position + new Vector2(1.5f, 0).RotatedBy(glow * MathHelper.PiOver2), source, new Color(255, 205, 90, 0) * .7f, rotation, new Vector2(8), .74f, SpriteEffects.None, 0);
                    batch.Draw(chest, position, source, light, rotation, new Vector2(8), .74f, SpriteEffects.None, 0);
                }
        }
        else
        {
            if (kind == 1)
            {
                Texture2D moth = ModContent.Request<Texture2D>("AerovelenceMod/Content/NPCs/CrystalCaverns/Charger").Value;
                int height = moth.Height / 10, frame = (int)(time * 8 + phase) % 4;
                batch.Draw(moth, body - offset, new Rectangle(0, frame * height, moth.Width, height), light * .7f, rotation, new Vector2(moth.Width / 2f, height / 2f), .28f, SpriteEffects.None, 0);
            }
            if (kind == 2)
            {
                string texture = ModContent.GetInstance<CavernPot2x2Rubble>().Texture;
                Texture2D pot = ModContent.Request<Texture2D>(texture).Value;
                Texture2D glow = ModContent.Request<Texture2D>(texture + "_Glowmask").Value;
                int style = (root.X * 17 + root.Y) % 9;
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                    {
                        Rectangle source = new(style % 3 * 36 + x * 18, style / 3 * 36 + y * 18, 16, 16);
                        Vector2 position = body + (new Vector2(x * 16 - 8, y * 16 - 8) * .65f).RotatedBy(rotation) - offset;
                        batch.Draw(pot, position, source, light, rotation, new Vector2(8), .65f, SpriteEffects.None, 0);
                        batch.Draw(glow, position, source, Color.White * (.35f + .2f * MathF.Sin(time * .7f + phase)), rotation, new Vector2(8), .65f, SpriteEffects.None, 0);
                    }
            }
        }
        for (int strandIndex = 0; strandIndex < 17; strandIndex++)
        {
            float angle = strandIndex / 17f * MathHelper.TwoPi + phase, radius = kind == 0 ? 14 : kind == 3 ? 7 : 10;
            Vector2 a = kind == 0 ? tip - new Vector2(0, 7).RotatedBy(rotation) : tip;
            Vector2 b = body + new Vector2(MathF.Sin(angle) * radius, 4 + MathF.Cos(angle) * 6).RotatedBy(rotation);
            Vector2 c = body + new Vector2(MathF.Sin(angle + 1) * 3, 12).RotatedBy(rotation);
            if (kind == 3 && strandIndex % 3 == 0) continue;
            Thread(a, body + new Vector2(MathF.Sin(angle) * radius * 1.4f, -4).RotatedBy(rotation), b, silk * .28f, 7);
            Thread(b, body + new Vector2(MathF.Sin(angle + .6f) * radius, 11).RotatedBy(rotation), c, silk * .32f, 6);
        }
        for (int n = 0; n < 6; n++)
        {
            float y = -6 + n * 3, width = kind == 0 ? 11 : 7;
            Thread(body + new Vector2(-width, y).RotatedBy(rotation), body + new Vector2(0, y + 4).RotatedBy(rotation), body + new Vector2(width, y + 1).RotatedBy(rotation), silk * .25f, 6);
        }
    }
}
