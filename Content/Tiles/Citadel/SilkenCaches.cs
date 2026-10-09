using System;
using System.Collections.Generic;
using System.Linq;
using AerovelenceMod.Common.Systems.Generation.CrystalCaverns;

using AerovelenceMod.Common.Utilities.Generation;
using AerovelenceMod.Common.Utilities.Generation.StructureStamper;
using AerovelenceMod.Content.NPCs.CrystalCaverns;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Furniture;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Rubble;



using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;

using Terraria.IO;

using Terraria.ObjectData;
using Terraria.WorldBuilding;
using Terraria.Utilities;
using LocalizedText = Terraria.Localization.LocalizedText;

namespace AerovelenceMod.Content.Tiles.Citadel;

public class SilkenCacheTile : ModTile
{
    internal const int MinHeight = 5;
    internal const int MaxHeight = 12;
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
                data.CoordinatePaddingFix = new Point16(0, (MaxHeight - MinHeight) * 18);
                data.LavaDeath = data.WaterDeath = false;
                data.AnchorInvalidTiles = [TileID.MagicalIceBlock, TileID.Sand, TileID.Silt, TileID.Slush];
                if (Container)
                {
                    data.HookCheckIfCanPlace = new PlacementHook(CheckStorage, -1, 0, true);
                    data.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, false);
                }
                int styles = Container ? 1 : 6;
                for (int style = styles; style < styles * (MaxHeight - MinHeight + 1); style++)
                {
                    int height = MinHeight + style / styles;
                    TileObjectData.newSubTile.CopyFrom(data);
                    TileObjectData.newSubTile.Height = height;
                    TileObjectData.newSubTile.CoordinateHeights = Enumerable.Repeat(16, height).ToArray();
                    TileObjectData.newSubTile.CoordinatePaddingFix = new Point16(0, (MaxHeight - height) * 18);
                    TileObjectData.addSubTile(style);
                }
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
    public static Point Root(int i, int j) => new(i - Main.tile[i, j].TileFrameX % 36 / 18, j - Main.tile[i, j].TileFrameY / 18);
    internal static int Height(Tile tile) => MinHeight + tile.TileFrameX / (tile.TileType == ModContent.TileType<SilkenCacheTile>() ? 36 : 216);
    internal static int Kind(Tile tile) => tile.TileType == ModContent.TileType<SilkenCacheTile>() ? 0 : tile.TileFrameX / 36 % 6;
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
    internal static bool InReach(int i, int j) => InReach(Main.LocalPlayer, Root(i, j));
    internal static bool InReach(Player player, Point root)
    {
        Point position = player.Center.ToTileCoordinates();
        int height = Height(Main.tile[root.X, root.Y]);
        return player.InInteractionRange(Math.Clamp(position.X, root.X, root.X + 1),
            Math.Clamp(position.Y, root.Y, root.Y + height - 1), TileReachCheckSettings.Simple);
    }
    public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => InReach(i, j);
    public override bool RightClick(int i, int j)
    {
        if (!InReach(i, j)) return false;
        Point root = Root(i, j);
        if (Container) return CommonTileHelper.HandleRightClick(this, root.X, root.Y, Main.LocalPlayer, ItemID.None);
        WorldGen.KillTile(i, j);
        if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.TileManipulation, number: 0, number2: i, number3: j);
        return true;
    }
    public override void MouseOver(int i, int j)
    {
        if (!InReach(i, j)) { MouseOverFar(i, j); return; }
        Main.LocalPlayer.cursorItemIconText = string.Empty;
        Main.LocalPlayer.noThrow = 2;
        Main.LocalPlayer.cursorItemIconEnabled = true;
        Main.LocalPlayer.cursorItemIconID = Container ? ModContent.ItemType<SilkenCacheItem>() : ItemID.Silk;
    }
    public override void MouseOverFar(int i, int j)
    {
        Main.LocalPlayer.cursorItemIconEnabled = false;
        Main.LocalPlayer.cursorItemIconID = 0;
        Main.LocalPlayer.cursorItemIconText = string.Empty;
    }
    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        SilkenCacheMotion.Forget(new Point(i, j));
        int height = MinHeight + frameX / (Container ? 36 : 216);
        if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, i, j, 2, height);
        if (Container)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) Chest.DestroyChestDirect(i, j, Chest.FindChest(i, j));
            else Chest.DestroyChest(i, j);
            return;
        }
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        int kind = frameX / 36 % 6;
        var source = new EntitySource_TileBreak(i, j);
        if (kind == 1) NPC.NewNPC(source, (i + 1) * 16, (j + height - 1) * 16, ModContent.NPCType<SilkenCacheMoth>());
        else if (kind == 5 && !NPC.savedStylist && !NPC.AnyNPCs(NPCID.WebbedStylist) && !NPC.AnyNPCs(NPCID.Stylist))
            NPC.NewNPC(source, (i + 1) * 16, (j + height - 1) * 16, NPCID.WebbedStylist);
        else if (kind == 2)
        {
            Item.NewItem(source, i * 16, (j + height - 2) * 16, 32, 32, ItemID.SilverCoin, Main.rand.Next(1, 4));
            if (Main.rand.NextBool(3)) Item.NewItem(source, i * 16, (j + height - 2) * 16, 32, 32, ItemID.HealingPotion);
        }
        Item.NewItem(source, i * 16, (j + height - 3) * 16, 32, 32, ItemID.Cobweb, Main.rand.Next(1, 4));
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
        return below.HasTile && SilkenCacheTile.IsCache(below.TileType) && below.TileFrameY == 0 &&
            !SilkenCacheTile.CanRelease(SilkenCacheTile.Root(i, j + 1));
    }
    public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged) => WorldGen.destroyObject || !LockedBelow(i, j);
    public override bool CanExplode(int i, int j, int type) => !LockedBelow(i, j);
    public override bool Slope(int i, int j, int type) => !LockedBelow(i, j);
    public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced) => !LockedBelow(i, j);
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (!WorldGen.destroyObject && Main.netMode != NetmodeID.MultiplayerClient && LockedBelow(i, j)) fail = true;
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

public sealed class SilkenCacheStylist : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type == NPCID.WebbedStylist;
    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || source is not EntitySource_SpawnNPC || NPC.savedStylist) return;
        SilkenCacheMotion.WebStylist(npc.Center.ToTileCoordinates());
        npc.active = false;
        npc.netUpdate = true;
    }
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
        SilkenCitadelWorld.FinishSettlement();
        progress.Message = SilkenCacheMotion.GenerationMessage.Value;
        GenerateCitadel();
        GenerateSpiderNests(progress);
        progress.Set(1);
    }
    private static void GenerateCitadel()
    {
        Rectangle bounds = SilkenCitadelWorld.Bounds;
        if (bounds.IsEmpty) return;
        int placed = 0, chests = 0, target = Math.Clamp(bounds.Width * bounds.Height / 6000, 16, 65);
        List<Point> anchors = new();
        Rectangle nursery = SilkenCitadelWorld.NestBounds;
        int nestCaches = 0;
        for (int attempt = 0; attempt < target * 250 && placed < target; attempt++)
        {
            Rectangle search = nestCaches < 3 && !nursery.IsEmpty && attempt < target * 100 ? nursery : bounds;
            int x = WorldGen.genRand.Next(search.Left + 10, search.Right - 12), y = WorldGen.genRand.Next(search.Top + 12, search.Bottom - 15);
            if (!WorldGen.SolidTile(x, y - 1) || !WorldGen.SolidTile(x + 1, y - 1)) continue;
            ushort ceiling = Main.tile[x, y - 1].TileType;
            CCTerrainPass terrain = CCTerrainPass.Instance();
            if (ceiling != terrain.StoneTile && ceiling != terrain.DirtTile && ceiling != terrain.ChargedTile && ceiling != terrain.LushTile) continue;
            int available = Clearance(x, y, 9);
            if (available < SilkenCacheTile.MinHeight) continue;
            int height = WorldGen.genRand.Next(SilkenCacheTile.MinHeight, available + 1);
            Rectangle space = new(x - 1, y - 1, 4, height + 2);
            if (AeroStructure.ProtectedStructures.Any(area => area.Intersects(space)) || anchors.Any(p => Math.Abs(x - p.X) < 12 && Math.Abs(y - p.Y) < SilkenCacheTile.MaxHeight + 2)) continue;
            int roll = WorldGen.genRand.Next(100), kind = roll < 52 ? 0 : roll < 68 ? 1 : roll < 82 ? 2 : roll < 92 ? 3 : 4;
            if (placed == 0) kind = 5;
            if (!Place(x, y, kind, true, height)) continue;
            placed++; if (kind == 0) chests++;
            if (nursery.Contains(x, y)) nestCaches++;
            anchors.Add(new Point(x, y));
            new AeroStructure(new Vector2(x - 1, y - 1), 4, height + 2, "silkencache").ProtectStructure();
        }
        ModContent.GetInstance<AerovelenceMod>().Logger.Info($"Silken caches placed: {placed}, chests={chests}, cocoons and remnants={placed - chests}.");
    }
    private static void GenerateSpiderNests(GenerationProgress progress)
    {
        HashSet<Point> visited = new();
        Stack<Point> pending = new();
        List<Point> ceilings = new();
        List<Point> anchors = new();
        Point[] neighbors = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];
        int total = 0;
        for (int x = 40; x < Main.maxTilesX - 40; x++)
        {
            if (x % 100 == 0) progress.Set(.35 + .6 * x / Main.maxTilesX);
            for (int y = (int)Main.worldSurface; y < Main.UnderworldLayer - 30; y++)
            {
                Point start = new(x, y);
                if (Main.tile[x, y].WallType != WallID.SpiderUnsafe || !visited.Add(start)) continue;
                ceilings.Clear();
                pending.Push(start);
                while (pending.Count > 0)
                {
                    Point point = pending.Pop();
                    if (WorldGen.SolidTile(point.X, point.Y - 1) && WorldGen.SolidTile(point.X + 1, point.Y - 1) &&
                        Clearance(point.X, point.Y, SilkenCacheTile.MaxHeight) >= SilkenCacheTile.MinHeight)
                        ceilings.Add(point);
                    foreach (Point offset in neighbors)
                    {
                        Point next = new(point.X + offset.X, point.Y + offset.Y);
                        if (WorldGen.InWorld(next.X, next.Y, 30) && Main.tile[next.X, next.Y].WallType == WallID.SpiderUnsafe && visited.Add(next))
                            pending.Push(next);
                    }
                }
                int count = 0, target = Math.Clamp(ceilings.Count / 8, 2, 6);
                while (ceilings.Count > 0 && count < target)
                {
                    int choice = WorldGen.genRand.Next(ceilings.Count);
                    Point point = ceilings[choice];
                    ceilings.RemoveAt(choice);
                    if (anchors.Any(p => Math.Abs(point.X - p.X) < 10 && Math.Abs(point.Y - p.Y) < SilkenCacheTile.MaxHeight + 2)) continue;
                    int available = Clearance(point.X, point.Y, SilkenCacheTile.MaxHeight);
                    if (available < SilkenCacheTile.MinHeight) continue;
                    int height = WorldGen.genRand.Next(Math.Min(7, available), available + 1);
                    Rectangle space = new(point.X - 1, point.Y - 1, 4, height + 2);
                    if (AeroStructure.ProtectedStructures.Any(area => area.Intersects(space))) continue;
                    int roll = WorldGen.genRand.Next(100);
                    int kind = count == 0 ? 5 : count == 1 ? 0 : roll < 45 ? 0 : roll < 70 ? 2 : roll < 90 ? 3 : 4;
                    if (!Place(point.X, point.Y, kind, true, height)) continue;
                    anchors.Add(point);
                    new AeroStructure(new Vector2(space.X, space.Y), space.Width, space.Height, "spidercache").ProtectStructure();
                    count++;
                    total++;
                }
            }
        }
        ModContent.GetInstance<AerovelenceMod>().Logger.Info($"Spider's Nest silken caches placed: {total}.");
    }
    internal static int Clearance(int x, int y, int maximum)
    {
        for (int row = 0; row <= maximum; row++)
            for (int column = -1; column <= 2; column++)
            {
                if (!WorldGen.InWorld(x + column, y + row, 2)) return Math.Max(0, row - 1);
                Tile tile = Main.tile[x + column, y + row];
                if (tile.LiquidAmount > 0 || tile.HasTile && tile.TileType != TileID.Cobweb) return Math.Max(0, row - 1);
            }
        return maximum;
    }
    public static bool Place(int x, int y, int kind, bool loot, int height = SilkenCacheTile.MinHeight)
    {
        height = Math.Clamp(height, SilkenCacheTile.MinHeight, SilkenCacheTile.MaxHeight);
        int index = kind == 0 ? Chest.CreateChest(x, y) : -1;
        if (kind == 0 && index < 0) return false;
        Stamp(x, y, kind, height);
        if (kind == 0 && loot)
        {
            WeightedRandom<PrimaryItemConfiguration> pool = new(WorldGen.genRand);
            foreach (PrimaryItemConfiguration item in CCLoot.CreatePrimaryLootPool()) pool.Add(item, (int)(item.Weight * 100));
            ChestConfiguration contents = new();
            contents.AddPrimaryItemConfiguration(pool.Get());
            foreach (ItemConfiguration item in CCLoot.CreateSecondaryLootPool()) contents.AddItemConfiguration(item);
            ChestConfigurator.ApplyConfiguration(x, y, contents);
        }
        return true;
    }
    internal static void Stamp(int x, int y, int kind, int height = SilkenCacheTile.MinHeight)
    {
        ushort type = (ushort)(kind == 0 ? ModContent.TileType<SilkenCacheTile>() : ModContent.TileType<SilkenCocoonTile>());
        int style = kind == 0 ? height - SilkenCacheTile.MinHeight : kind + (height - SilkenCacheTile.MinHeight) * 6;
        for (int dx = 0; dx < 2; dx++)
            for (int dy = 0; dy < height; dy++)
            {
                Tile tile = Main.tile[x + dx, y + dy]; tile.ResetToType(type);
                tile.TileFrameX = (short)(style * 36 + dx * 18); tile.TileFrameY = (short)(dy * 18);
            }
    }
}

public sealed class SilkenCacheMotion : ModSystem
{
    internal static LocalizedText GenerationMessage { get; private set; }
    public override void SetStaticDefaults() => GenerationMessage = this.Localize("Weaving silken caches");
    private sealed class Strand
    {
        public readonly List<VerletSegment> Nodes = new();
        public readonly float Phase;
        public readonly int Height;
        private readonly float segmentLength;
        public ulong Seen;
        public Strand(Point root)
        {
            Height = SilkenCacheTile.Height(Main.tile[root.X, root.Y]);
            Phase = ((root.X * 73856093L ^ root.Y * 19349663L) & 1023) / 1024f * MathHelper.TwoPi;
            Vector2 anchor = new(root.X * 16 + 16, root.Y * 16 + 17);
            float length = Height * 16f - 45f;
            int segments = (int)MathF.Ceiling(length / 4.8f);
            segmentLength = length / segments;
            for (int n = 0; n <= segments; n++) Nodes.Add(new VerletSegment(anchor + new Vector2(0, n * segmentLength)));
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
                    Vector2 correction = delta * ((length - segmentLength) / length);
                    if (a.isFixed) b.currentPosition -= correction;
                    else { a.currentPosition += correction * .5f; b.currentPosition -= correction * .5f; }
                }
        }
    }
    private static readonly Dictionary<Point, Strand> strands = new();
    internal static void WebStylist(Point spawn)
    {
        Point nearest = Point.Zero, ceiling = Point.Zero;
        int nearestDistance = int.MaxValue, ceilingDistance = int.MaxValue;
        int type = ModContent.TileType<SilkenCocoonTile>();
        for (int x = Math.Max(30, spawn.X - 50); x < Math.Min(Main.maxTilesX - 30, spawn.X + 50); x++)
            for (int y = Math.Max(30, spawn.Y - 50); y < Math.Min(Main.maxTilesY - 30, spawn.Y + 25); y++)
            {
                Tile tile = Main.tile[x, y];
                int distance = (x - spawn.X) * (x - spawn.X) + (y - spawn.Y) * (y - spawn.Y);
                if (tile.HasTile && tile.TileType == type && tile.TileFrameY == 0 && tile.TileFrameX % 36 == 0 &&
                    SilkenCacheTile.Kind(tile) is 3 or 5 && distance < nearestDistance)
                {
                    nearest = new Point(x, y);
                    nearestDistance = distance;
                }
                if (tile.WallType == WallID.SpiderUnsafe && distance < ceilingDistance &&
                    WorldGen.SolidTile(x, y - 1) && WorldGen.SolidTile(x + 1, y - 1) &&
                    SilkenCachePass.Clearance(x, y, SilkenCacheTile.MaxHeight) >= SilkenCacheTile.MinHeight)
                {
                    ceiling = new Point(x, y);
                    ceilingDistance = distance;
                }
            }
        if (nearest != Point.Zero)
        {
            Tile tile = Main.tile[nearest.X, nearest.Y];
            if (SilkenCacheTile.Kind(tile) == 5) return;
            int height = SilkenCacheTile.Height(tile);
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < height; y++) Main.tile[nearest.X + x, nearest.Y + y].TileFrameX += 36 * 2;
            Forget(nearest);
            if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, nearest.X, nearest.Y, 2, height);
            return;
        }
        if (ceiling == Point.Zero) return;
        int length = Math.Min(9, SilkenCachePass.Clearance(ceiling.X, ceiling.Y, SilkenCacheTile.MaxHeight));
        SilkenCachePass.Place(ceiling.X, ceiling.Y, 5, false, length);
        if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, ceiling.X, ceiling.Y, 2, length);
    }
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
            if (!hitbox.Intersects(new Rectangle(root.X * 16 - 56, root.Y * 16 - 40, 144, strand.Height * 16 + 80))) continue;
            Tile tile = Main.tile[root.X, root.Y];
            if (!tile.HasTile || tile.TileType != type) continue;
            int end = SilkenCacheTile.Kind(tile) == 4 ? Math.Max(2, strand.Nodes.Count / 2) : strand.Nodes.Count;
            Vector2 tip = strand.Nodes[end - 1].currentPosition;
            bool hit = hitbox.Intersects(new Rectangle((int)tip.X - 18, (int)tip.Y - 10, 36, end < strand.Nodes.Count ? 14 : 40));
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
    public override void Load()
    {
        On_WorldGen.PlaceChestDirect += ReceivePlacement;
        On_Player.IsInInteractionRangeToMultiTileHitbox += ChestInReach;
    }
    private static bool ChestInReach(On_Player.orig_IsInInteractionRangeToMultiTileHitbox orig, Player player, int x, int y)
    {
        if (WorldGen.InWorld(x, y) && Main.tile[x, y].HasTile && Main.tile[x, y].TileType == ModContent.TileType<SilkenCacheTile>())
            return SilkenCacheTile.InReach(player, SilkenCacheTile.Root(x, y));
        return orig(player, x, y);
    }
    private static void ReceivePlacement(On_WorldGen.orig_PlaceChestDirect orig, int x, int y, ushort type, int style, int id)
    {
        if (type != ModContent.TileType<SilkenCacheTile>()) { orig(x, y, type, style, id); return; }
        Chest.CreateChest(x, y, id);
        SilkenCachePass.Stamp(x, y, 0, Math.Clamp(SilkenCacheTile.MinHeight + style, SilkenCacheTile.MinHeight, SilkenCacheTile.MaxHeight));
    }
    public static void Forget(Point root) => strands.Remove(root);
    public override void ClearWorld() => strands.Clear();
    public override void Unload()
    {
        On_WorldGen.PlaceChestDirect -= ReceivePlacement;
        On_Player.IsInInteractionRangeToMultiTileHitbox -= ChestInReach;
        strands.Clear();
    }
    public override void PostUpdateInput()
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused || Main.LocalPlayer.mouseInterface || Main.LocalPlayer.dead) return;
        Point mouse = Main.MouseWorld.ToTileCoordinates();
        if (!WorldGen.InWorld(mouse.X, mouse.Y)) return;
        if (!Main.LocalPlayer.InInteractionRange(mouse.X, mouse.Y, TileReachCheckSettings.Simple)) return;
        foreach (var (root, strand) in strands)
        {
            if (!WorldGen.InWorld(root.X, root.Y)) continue;
            Tile tileData = Main.tile[root.X, root.Y];
            if (!tileData.HasTile || Main.GameUpdateCount - strand.Seen > 2) continue;
            int kind = SilkenCacheTile.Kind(tileData);
            int end = kind == 4 ? Math.Max(2, strand.Nodes.Count / 2) : strand.Nodes.Count;
            Vector2 tip = strand.Nodes[^1].currentPosition;
            Vector2 tangent = tip - strand.Nodes[^2].currentPosition;
            float rotation = MathHelper.Clamp(tangent.ToRotation() - MathHelper.PiOver2 + MathF.Sin(strand.Phase) * .13f, -.6f, .6f);
            Vector2 local = (Main.MouseWorld - tip).RotatedBy(-rotation) - new Vector2(0, 8);
            bool hit = end == strand.Nodes.Count && Math.Abs(local.X) < 18f && Math.Abs(local.Y) < (kind == 5 ? 22f : 18f);
            for (int n = 1; n < end && !hit; n++)
            {
                Vector2 a = strand.Nodes[n - 1].currentPosition;
                Vector2 delta = strand.Nodes[n].currentPosition - a;
                float t = MathHelper.Clamp(Vector2.Dot(Main.MouseWorld - a, delta) / Math.Max(.001f, delta.LengthSquared()), 0, 1);
                hit = Vector2.DistanceSquared(Main.MouseWorld, a + delta * t) < 6 * 6;
            }
            if (!hit) continue;
            ModTile tile = TileLoader.GetTile(tileData.TileType);
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
        Tile tile = Main.tile[root.X, root.Y]; int kind = SilkenCacheTile.Kind(tile);
        float time = Main.GlobalTimeWrappedHourly, phase = strand.Phase;
        Vector2 anchor = strand.Nodes[0].currentPosition, tip = strand.Nodes[^1].currentPosition;
        Vector2 tangent = tip - strand.Nodes[^2].currentPosition;
        float rotation = MathHelper.Clamp(tangent.ToRotation() - MathHelper.PiOver2 + MathF.Sin(phase) * .13f, -.6f, .6f);
        Vector2 body = tip + new Vector2(0, 8).RotatedBy(rotation);
        Color light = Lighting.GetColor(body.ToTileCoordinates());
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
        int end = kind == 4 ? Math.Max(2, strand.Nodes.Count / 2) : strand.Nodes.Count;
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
            Vector2 torn = strand.Nodes[end - 1].currentPosition;
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
                    Vector2 local = new(x * 16 - 8, y * 16 - 8);
                    Rectangle source = new(x * 18, frame * 38 + y * 18, 16, 16);
                    if (source.Bottom > chest.Height) source.Y = y * 18;
                    Vector2 position = body + local.RotatedBy(rotation) - offset;
                    if (treasure)
                        for (int glow = 0; glow < 4; glow++) batch.Draw(chest, position + new Vector2(1.5f, 0).RotatedBy(glow * MathHelper.PiOver2), source, new Color(255, 205, 90, 0) * .7f, rotation, new Vector2(8), 1f, SpriteEffects.None, 0);
                    batch.Draw(chest, position, source, light, rotation, new Vector2(8), 1f, SpriteEffects.None, 0);
                }
        }
        else
        {
            if (kind == 5 && !NPC.savedStylist)
            {
                Main.instance.LoadNPC(NPCID.WebbedStylist);
                Texture2D stylist = TextureAssets.Npc[NPCID.WebbedStylist].Value;
                Rectangle frame = stylist.Frame(1, Main.npcFrameCount[NPCID.WebbedStylist]);
                batch.Draw(stylist, body - offset, frame, light, rotation, frame.Size() * .5f, .75f, SpriteEffects.None, 0);
            }
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
            float angle = strandIndex / 17f * MathHelper.TwoPi + phase, radius = kind == 0 ? 19 : kind == 3 ? 7 : 10;
            Vector2 a = kind == 0 ? tip - new Vector2(0, 7).RotatedBy(rotation) : tip;
            Vector2 b = body + new Vector2(MathF.Sin(angle) * radius, 4 + MathF.Cos(angle) * 6).RotatedBy(rotation);
            Vector2 c = body + new Vector2(MathF.Sin(angle + 1) * 3, 12).RotatedBy(rotation);
            if (kind == 3 && strandIndex % 3 == 0) continue;
            Thread(a, body + new Vector2(MathF.Sin(angle) * radius * 1.4f, -4).RotatedBy(rotation), b, silk * .28f, 7);
            Thread(b, body + new Vector2(MathF.Sin(angle + .6f) * radius, 11).RotatedBy(rotation), c, silk * .32f, 6);
        }
        for (int n = 0; n < 6; n++)
        {
            float y = -9 + n * 4, width = kind == 0 ? 16 : 7;
            Thread(body + new Vector2(-width, y).RotatedBy(rotation), body + new Vector2(0, y + 4).RotatedBy(rotation), body + new Vector2(width, y + 1).RotatedBy(rotation), silk * .25f, 6);
        }
    }
}
