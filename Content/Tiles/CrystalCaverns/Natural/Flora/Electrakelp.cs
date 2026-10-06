using System;
using AerovelenceMod.Common.Systems;
using System.Collections.Generic;
using AerovelenceMod.Content.NPCs.CrystalCaverns;



using Terraria.GameContent;
using Terraria.GameContent.Drawing;


using static Terraria.ModLoader.ModContent;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Natural.Flora
{
    internal class Electrakelp : ModTile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        private static readonly Dictionary<Point, Frond> fronds = new();
        public override void SetStaticDefaults()
        {
            Main.tileLavaDeath[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileFrameImportant[Type] = true;
            Main.tileCut[Type] = true;
            Main.tileNoFail[Type] = true;
            AddMapEntry(new Color(89, 161, 244));
            DustType = DustID.Grass;
            HitSound = SoundID.Grass;
        }

        private static bool Wet(int x, int y) => WorldGen.InWorld(x, y, 2) && Main.tile[x, y].LiquidAmount >= 160 && Main.tile[x, y].LiquidType == LiquidID.Water;
        private static bool Vine(int x, int y) => WorldGen.InWorld(x, y, 2) && Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileType<Electrakelp>();
        public static bool Plant(int x, int y, int length)
        {
            if (!Wet(x, y) || Main.tile[x, y].HasTile) return false;
            int direction = WorldGen.SolidTile(x, y + 1) ? -1 : WorldGen.SolidTile(x, y - 1) ? 1 : 0;
            if (direction == 0) return false;
            int count = 0;
            while (count < Math.Clamp(length, 3, 10) && Wet(x, y + count * direction) && !Main.tile[x, y + count * direction].HasTile) count++;
            if (count < 3) return false;
            for (int n = 0; n < count; n++)
            {
                Tile tile = Main.tile[x, y + n * direction];
                tile.ResetToType((ushort)TileType<Electrakelp>());
                tile.TileFrameY = (short)(direction < 0 ? 18 : 0);
                tile.LiquidAmount = 255;
                tile.LiquidType = LiquidID.Water;
            }
            return true;
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            int direction = Main.tile[i, j].TileFrameY == 18 ? -1 : 1;
            int rootY = j;
            for (int n = 0; n < 10 && Vine(i, rootY - direction); n++) rootY -= direction;
            //if (Framing.GetTileSafely(i, j).frameX == 0)
            //    Framing.GetTileSafely(i, j).frameY = (short)(Main.rand.Next(2) * 18);
            //else
            if (!WorldGen.gen && (!Wet(i, j) || !WorldGen.SolidOrSlopedTile(Main.tile[i, rootY - direction])))
                WorldGen.KillTile(i, j);
            return false;
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        { r = .015f; g = .19f; b = .27f; }
        public override void RandomUpdate(int i, int j)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            bool reset = false, noBreak = false;
            TileFrame(i, j, ref reset, ref noBreak);
            if (!Vine(i, j)) return;
            int direction = Main.tile[i, j].TileFrameY == 18 ? -1 : 1;
            int root = j, length = 1;
            while (length <= 10 && Vine(i, root - direction)) { root -= direction; length++; }
            int next = j + direction;
            if (length >= 10 || !Wet(i, next) || Main.tile[i, next].HasTile || !WorldGen.genRand.NextBool(5)) return;
            Tile tile = Main.tile[i, next];
            tile.ResetToType((ushort)Type);
            tile.TileFrameY = Main.tile[i, j].TileFrameY;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, i, next, 1);
        }
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            int direction = Main.tile[i, j].TileFrameY == 18 ? -1 : 1;
            if (!Vine(i, j - direction)) Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.CustomNonSolid);
            return false;
        }
        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            int direction = Main.tile[i, j].TileFrameY == 18 ? -1 : 1;
            if (!Vine(i, j) || Vine(i, j - direction) || !Wet(i, j)) return;
            int length = 1;
            while (length < 10 && Vine(i, j + length * direction) && Wet(i, j + length * direction)) length++;
            Point root = new(i, j);
            if (!fronds.TryGetValue(root, out Frond frond) || frond.Length != length || frond.Direction != direction)
                fronds[root] = frond = new Frond(root, length, direction);
            if (frond.Seen != Main.GameUpdateCount) frond.Update();
            frond.Seen = Main.GameUpdateCount;
        }
        internal static bool HasVisibleFronds()
        {
            if (Main.gameMenu) return false;
            foreach (Frond frond in fronds.Values)
                if (Main.GameUpdateCount - frond.Seen <= 2) return true;
            return false;
        }
        internal static void DrawFronds()
        {
            foreach (var pair in fronds)
            {
                Point root = pair.Key;
                Frond frond = pair.Value;
                if (Main.GameUpdateCount - frond.Seen > 2 || !Vine(root.X, root.Y) || Main.tile[root.X, root.Y].IsTileInvisible) continue;
                int direction = Main.tile[root.X, root.Y].TileFrameY == 18 ? -1 : 1;
                if (Vine(root.X, root.Y - direction)) continue;
                frond.Draw(Main.spriteBatch, Lighting.GetColor(root.X, root.Y));
            }
        }
        internal static void ClearFronds() => fronds.Clear();
        internal static void PruneFronds()
        {
            if (Main.GameUpdateCount % 180 != 0) return;
            List<Point> expired = new();
            foreach (var pair in fronds)
                if (Main.GameUpdateCount - pair.Value.Seen > 180 || !Vine(pair.Key.X, pair.Key.Y)) expired.Add(pair.Key);
            foreach (Point point in expired) fronds.Remove(point);
        }

        private sealed class Frond
        {
            public readonly int Length, Direction;
            public ulong Seen = ulong.MaxValue;
            private readonly List<VerletSegment>[] blades = new List<VerletSegment>[1];
            private readonly List<(Rectangle Bounds, Vector2 Velocity)> visitors = new();
            private readonly Vector2 anchor;
            private readonly float phase;
            public Frond(Point root, int length, int direction)
            {
                Length = length; Direction = direction;
                anchor = new Vector2(root.X * 16 + 8, root.Y * 16 + (direction > 0 ? 0 : 16));
                phase = ((root.X * 73471L ^ root.Y * 19391L) & 1023) * MathHelper.TwoPi / 1024;
                for (int b = 0; b < blades.Length; b++)
                {
                    blades[b] = new();
                    int nodes = Math.Max(5, length * 2 - b * 2);
                    for (int n = 0; n < nodes; n++) blades[b].Add(new VerletSegment(anchor + new Vector2(MathF.Sin(phase + n * .2f) * n * .08f, n * 7 * direction)));
                    blades[b][0].isFixed = true;
                }
            }
            public void Update()
            {
                visitors.Clear();
                float reach = Length * 16 + 60;
                foreach (Player player in Main.player)
                    if (player.active && !player.dead && Vector2.DistanceSquared(player.Center, anchor) < reach * reach)
                        visitors.Add((player.Hitbox, player.velocity));
                foreach (NPC npc in Main.ActiveNPCs)
                    if (Vector2.DistanceSquared(npc.Center, anchor) < (reach + npc.width) * (reach + npc.width))
                        visitors.Add((npc.Hitbox, npc.velocity));
                float time = Main.GameUpdateCount / 60f;
                for (int b = 0; b < blades.Length; b++)
                {
                    List<VerletSegment> nodes = blades[b];
                    for (int n = 1; n < nodes.Count; n++)
                    {
                        VerletSegment node = nodes[n];
                        Vector2 previous = node.currentPosition;
                        Vector2 flow = new(MathF.Sin(time * .22f + phase + n * .23f + b * .8f) * .022f, Direction * .055f);
                        node.currentPosition += (node.currentPosition - node.oldPosition) * .86f + flow + Wake(node.currentPosition) * (n / (float)nodes.Count);
                        node.oldPosition = previous;
                    }
                    for (int iteration = 0; iteration < 7; iteration++)
                        for (int n = 1; n < nodes.Count; n++)
                        {
                            VerletSegment a = nodes[n - 1], c = nodes[n];
                            Vector2 delta = c.currentPosition - a.currentPosition;
                            float distance = delta.Length();
                            if (distance < .001f) continue;
                            Vector2 correction = delta * ((distance - 7) / distance);
                            if (a.isFixed) c.currentPosition -= correction;
                            else { a.currentPosition += correction * .5f; c.currentPosition -= correction * .5f; }
                        }
                    for (int n = 1; n < nodes.Count; n++)
                    {
                        VerletSegment node = nodes[n];
                        Point tile = node.currentPosition.ToTileCoordinates();
                        if (!Wet(tile.X, tile.Y) || WorldGen.SolidTile(tile.X, tile.Y))
                            node.currentPosition = node.oldPosition;
                    }
                }
            }
            private Vector2 Wake(Vector2 position)
            {
                Vector2 wake = Vector2.Zero;
                foreach (var visitor in visitors) Add(visitor.Bounds, visitor.Velocity);
                return Vector2.Clamp(wake, new Vector2(-.65f), new Vector2(.65f));

                void Add(Rectangle bounds, Vector2 velocity)
                {
                    Vector2 closest = Vector2.Clamp(position, new Vector2(bounds.Left, bounds.Top), new Vector2(bounds.Right, bounds.Bottom));
                    float distance = Vector2.Distance(position, closest);
                    if (distance < 28) wake += Vector2.Clamp(velocity, new Vector2(-8), new Vector2(8)) * (.055f * (1 - distance / 28));
                }
            }

            public void Draw(SpriteBatch batch, Color light)
            {
                Vector2 offset = Main.screenPosition;
                Color body = Color.Lerp(light, new Color(25, 119, 120), .55f);
                Color glow = new Color(46, 185, 210, 0) * .32f;
                float time = Main.GameUpdateCount / 60f;
                for (int b = 0; b < blades.Length; b++)
                {
                    List<VerletSegment> nodes = blades[b];
                    for (int n = 1; n < nodes.Count; n++)
                    {
                        Vector2 a = nodes[n - 1].currentPosition, c = nodes[n].currentPosition;
                        float taper = 1 - n / (float)nodes.Count;
                        Line(a, c, body, 2.6f * taper + 1);
                        Line(a, c, glow, 1);
                        if (n % 2 == 0)
                            for (int side = -1; side <= 1; side += 2)
                            {
                                Vector2 tangent = (c - a).SafeNormalize(Vector2.UnitY * Direction);
                                Vector2 normal = new(-tangent.Y, tangent.X);
                                float size = (5 + 7 * taper) * (.8f + .2f * MathF.Sin(phase + n));
                                Vector2 old = c;
                                for (int step = 1; step <= 5; step++)
                                {
                                    float t = step / 5f;
                                    Vector2 leaf = c + normal * side * MathF.Sin(t * 2.2f) * size + tangent * (t * 9 + MathF.Sin(time * .25f + phase + n) * t * 2);
                                    Line(old, leaf, body * .9f, 1 + (1 - t) * 2.5f);
                                    Line(old, leaf, glow * (1 - t * .6f), 1);
                                    old = leaf;
                                }
                            }
                    }
                }
                void Line(Vector2 a, Vector2 b, Color color, float width)
                {
                    Vector2 delta = b - a;
                    batch.Draw(TextureAssets.MagicPixel.Value, a - offset, new Rectangle(0, 0, 1, 1), color,
                        delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
                }
            }
        }
    }
    public sealed class ElectrakelpSystem : ModSystem
    {
        private bool registered;
        public override void PostUpdateEverything()
        {
            if (Main.dedServ) return;
            if (!registered)
                registered = GetInstance<PixelationSystem>().RegisterPersistentRenderAction(RenderLayer.BeforeSolidTiles,
                    Electrakelp.HasVisibleFronds, Electrakelp.DrawFronds);
            Electrakelp.PruneFronds();
        }
        public override void ClearWorld() => Electrakelp.ClearFronds();
        public override void Unload()
        {
            if (registered) GetInstance<PixelationSystem>().UnregisterPersistentRenderAction(RenderLayer.BeforeSolidTiles, Electrakelp.DrawFronds);
            registered = false;
            Electrakelp.ClearFronds();
        }
    }
}
