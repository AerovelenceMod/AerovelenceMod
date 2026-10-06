using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


using AerovelenceMod.Content.Tiles.Traversal;

using Terraria.DataStructures;


using Terraria.ModLoader.IO;
using LocalizedText = Terraria.Localization.LocalizedText;

namespace AerovelenceMod.Common.Systems.Traversal
{
    public sealed class RopeSpanSystem : ModSystem
    {
        internal const byte Packet = 235;
        internal const int MinimumBridgeLength = 12;
        internal const int MaximumBridgeLength = 128;
        internal const int MaximumZiplineLength = 256;
        internal const int MaximumConnections = 4;
        internal const int MaximumBridgeHeight = 10;
        private const ulong SelectionDuration = 600;
        internal static readonly Dictionary<int, RopeSpan> Spans = new();
        private static readonly Dictionary<Point, HashSet<int>> tileSpans = new();
        private static readonly Dictionary<Point, HashSet<int>> postSpans = new();
        private static readonly Dictionary<int, (Point Point, bool Zipline, int Rope, ulong ExpiresAt)> selections = new();
        private static readonly Dictionary<int, Point> previewTargets = new();
        private static int nextId = 1;
        private static bool breaking;
        private static LocalizedText selectedText, neededText, tooShortText, tooLongText, tooSteepText;
        private static LocalizedText occupiedPostText, duplicateText, invalidPostText, outsideWorldText, blockedTileText, overlapText;
        private static LocalizedText bridgeClearanceText, ziplineClearanceText, ziplineSupportText, outOfReachText;

        internal enum ConnectionFailure
        {
            TooShort,
            TooLong,
            TooSteep,
            PostInUse,
            DuplicateConnection,
            InvalidPost,
            OutsideWorld,
            OccupiedTile,
            OverlappingSpan,
            BlockedClearance,
            PostSupportBlocked
        }

        internal readonly record struct ConnectionError(ConnectionFailure Reason, Point Position, int Actual = 0, int Limit = 0)
        {
            internal Point? Blocker => Reason is ConnectionFailure.PostInUse or ConnectionFailure.InvalidPost
                or ConnectionFailure.OccupiedTile or ConnectionFailure.OverlappingSpan or ConnectionFailure.BlockedClearance
                or ConnectionFailure.PostSupportBlocked ? Position : null;
        }

        public override void SetStaticDefaults()
        {
            selectedText = this.Localize("Post selected. Right-click the other post with the same rope within 10 seconds.");
            neededText = this.Localize("This span needs {0} rope; you have {1}. Add {2} more, then select both posts again.");
            tooShortText = this.Localize("Posts are {0} tiles apart. Move them at least {1} tiles apart.");
            tooLongText = this.Localize("Posts are {0} tiles apart. The maximum distance is {1} tiles.");
            tooSteepText = this.Localize("Posts differ by {0} tiles in height. The maximum for this span is {1} tiles.");
            occupiedPostText = this.Localize("The red outlined post already has {0} connections. Each post supports at most {1}.");
            duplicateText = this.Localize("These two posts already have a connection. Select a different post.");
            invalidPostText = this.Localize("The red outlined post is incomplete, the wrong kind, or is missing its floor/ceiling support.");
            outsideWorldText = this.Localize("This bridge or zipline would be too close to the world's edge.");
            blockedTileText = this.Localize("The red outlined tile blocks the planks or rope. Clear it before connecting the posts.");
            overlapText = this.Localize("The red outlined tile belongs to another bridge or zipline. Move the posts or remove that span.");
            bridgeClearanceText = this.Localize("The red outlined block leaves too little room to walk across the bridge. Clear it before connecting the posts.");
            ziplineClearanceText = this.Localize("The red outlined block leaves too little room to hang beneath the zipline. Clear it before connecting the posts.");
            ziplineSupportText = this.Localize("The red outlined block supports a zipline post, but your feet would hit it on this slope. Put that post on a platform, or use a gentler slope.");
            outOfReachText = this.Localize("Move closer to the post to connect it.");
        }

        public override void Load()
        {
            On_Player.SlopingCollision += SlopingCollision;
            On_Player.PlayerFrame += PlayerFrame;
            if (!Main.dedServ) On_PlayerDrawLayers.DrawPlayer_24_Pulley += DrawPulley;
        }

        public override void Unload()
        {
            On_Player.SlopingCollision -= SlopingCollision;
            On_Player.PlayerFrame -= PlayerFrame;
            if (!Main.dedServ) On_PlayerDrawLayers.DrawPlayer_24_Pulley -= DrawPulley;
            Reset();
        }

        private static void SlopingCollision(On_Player.orig_SlopingCollision orig, Player player, bool fallThrough, bool ignorePlats)
        {
            orig(player, fallThrough, ignorePlats);
            player.GetModPlayer<RopeSpanPlayer>().ResolveStanding(fallThrough || ignorePlats);
        }

        private static void PlayerFrame(On_Player.orig_PlayerFrame orig, Player player)
        {
            RopeSpanPlayer rider = player.GetModPlayer<RopeSpanPlayer>();
            if (rider.StandingId > 0) rider.ResolveStanding(player.controlDown);
            bool pulley = player.pulley;
            byte direction = player.pulleyDir;
            Vector2 velocity = player.velocity;
            bool riding = rider.Riding;
            if (riding)
            {
                player.pulley = true;
                player.pulleyDir = 1;
                player.gfxOffY = 0;
                player.velocity = new Vector2(0, Math.Abs(rider.Speed) < 0.001f ? 0.001f : rider.Speed);
                rider.AnimatePulley();
            }
            try { orig(player); }
            finally
            {
                player.pulley = pulley;
                player.pulleyDir = direction;
                if (riding) player.velocity = velocity;
            }
        }

        private static void DrawPulley(On_PlayerDrawLayers.orig_DrawPlayer_24_Pulley orig, ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            bool pulley = player.pulley;
            byte direction = player.pulleyDir;
            if (player.GetModPlayer<RopeSpanPlayer>().Riding)
            {
                player.pulley = true;
                player.pulleyDir = 1;
            }
            try { orig(ref drawInfo); }
            finally
            {
                player.pulley = pulley;
                player.pulleyDir = direction;
            }
        }

        public override void ClearWorld() => Reset();
        private static void Reset()
        {
            Spans.Clear();
            tileSpans.Clear();
            postSpans.Clear();
            selections.Clear();
            previewTargets.Clear();
            nextId = 1;
            breaking = false;
        }

        public static bool IsRope(Item item) => item.stack > 0 && (uint)item.createTile < Main.tileRope.Length && Main.tileRope[item.createTile];
        internal static int RopeTileType(int ropeType)
            => ContentSamples.ItemsByType.TryGetValue(ropeType, out Item item) && IsRope(item) ? item.createTile : TileID.Rope;

        internal static int RopeStyle(int ropeType) => RopeTileType(ropeType) switch
        {
            TileID.VineRope => 1,
            TileID.SilkRope => 2,
            TileID.WebRope => 3,
            _ => 0
        };

        public static RopeSpan Get(int id) => Spans.TryGetValue(id, out RopeSpan span) ? span : null;
        public static RopeSpan AtTile(Point point) => tileSpans.TryGetValue(point, out HashSet<int> ids) || postSpans.TryGetValue(point, out ids)
            ? ids.Select(Get).FirstOrDefault(span => span != null) : null;

        internal static int ConnectionCount(Point point) => postSpans.TryGetValue(point, out HashSet<int> ids) ? ids.Count : 0;

        internal static Vector2 JunctionAnchor(RopeSpan span, bool right)
        {
            Point post = right ? span.Right : span.Left;
            Vector2 anchor = span.HookAnchor(right);
            float leftX = anchor.X, rightX = anchor.X;
            if (postSpans.TryGetValue(post, out HashSet<int> ids))
                foreach (int id in ids)
                {
                    RopeSpan connected = Get(id);
                    if (connected == null || !connected.Zipline) continue;
                    float x = connected.HookAnchor(connected.Right == post).X;
                    leftX = Math.Min(leftX, x);
                    rightX = Math.Max(rightX, x);
                }
            return new Vector2((leftX + rightX) * 0.5f, anchor.Y);
        }

        internal static RopeSpan[] JunctionRoutes(RopeSpan incoming, Point point)
        {
            if (!postSpans.TryGetValue(point, out HashSet<int> ids)) return Array.Empty<RopeSpan>();
            return ids.Select(Get).Where(span => span != null && span.Zipline && span.Id != incoming.Id)
                .OrderBy(span => (span.Left == point ? span.Right : span.Left).X)
                .ThenBy(span => (span.Left == point ? span.Right : span.Left).Y)
                .ThenBy(span => span.Id).ToArray();
        }

        private static void AddOwner(Dictionary<Point, HashSet<int>> owners, Point point, int id)
        {
            if (!owners.TryGetValue(point, out HashSet<int> ids)) owners[point] = ids = new HashSet<int>();
            ids.Add(id);
        }

        private static bool RemoveOwner(Dictionary<Point, HashSet<int>> owners, Point point, int id)
        {
            if (!owners.TryGetValue(point, out HashSet<int> ids)) return false;
            ids.Remove(id);
            if (ids.Count > 0) return true;
            owners.Remove(point);
            return false;
        }

        internal static bool CeilingPost(Point point)
            => WorldGen.InWorld(point.X, point.Y) && Main.tile[point.X, point.Y].HasTile
                && Main.tile[point.X, point.Y].TileType == ModContent.TileType<ZiplinePostTile>()
                && Main.tile[point.X, point.Y].TileFrameX == 18;

        internal static bool ValidPost(Point point, bool zipline)
        {
            int height = zipline ? 4 : 3;
            int type = zipline ? ModContent.TileType<ZiplinePostTile>() : ModContent.TileType<RopeBridgePostTile>();
            if (!WorldGen.InWorld(point.X, point.Y, 10) || point.Y < height) return false;
            bool ceiling = zipline && CeilingPost(point);
            int frameX = Main.tile[point.X, point.Y].TileFrameX;
            if (zipline ? frameX != (ceiling ? 18 : 0) : frameX < 0 || frameX >= 144 || frameX % 18 != 0) return false;
            for (int row = 0; row < height; row++)
            {
                Tile tile = Main.tile[point.X, point.Y - height + 1 + row];
                if (!tile.HasTile || tile.TileType != type || tile.TileFrameY / 18 % height != row
                    || tile.TileFrameX != frameX) return false;
            }
            if (ceiling) return WorldGen.SolidTile(point.X, point.Y - height);
            return WorldGen.SolidTile(point.X, point.Y + 1) || Main.tile[point.X, point.Y + 1].HasTile && Main.tileSolidTop[Main.tile[point.X, point.Y + 1].TileType];
        }

        public static bool CanCreate(Point a, Point b, bool zipline, out RopeSpan span, int ropeType = ItemID.Rope)
            => CheckConnection(a, b, zipline, out span, ropeType) == null;

        internal static ConnectionError? CheckConnection(Point a, Point b, bool zipline, out RopeSpan span, int ropeType = ItemID.Rope)
        {
            span = null;
            if (a.X > b.X || a.X == b.X && a.Y > b.Y) (a, b) = (b, a);
            int width = b.X - a.X;
            int minimumWidth = zipline ? 1 : MinimumBridgeLength;
            int maximumWidth = zipline ? MaximumZiplineLength : MaximumBridgeLength;
            int height = Math.Abs(a.Y - b.Y);
            int maximumHeight = MaximumBridgeHeight;
            if (zipline ? a == b : width < minimumWidth) return new(ConnectionFailure.TooShort, default, width, minimumWidth);
            if (width > maximumWidth) return new(ConnectionFailure.TooLong, default, width, maximumWidth);
            if (!zipline && height > maximumHeight) return new(ConnectionFailure.TooSteep, default, height, maximumHeight);
            if (ConnectionCount(a) >= MaximumConnections) return new(ConnectionFailure.PostInUse, a, ConnectionCount(a), MaximumConnections);
            if (ConnectionCount(b) >= MaximumConnections) return new(ConnectionFailure.PostInUse, b, ConnectionCount(b), MaximumConnections);
            if (postSpans.TryGetValue(a, out HashSet<int> connections)
                && connections.Any(id => Get(id)?.Left == b || Get(id)?.Right == b)) return new(ConnectionFailure.DuplicateConnection, b);
            if (!ValidPost(a, zipline)) return new(ConnectionFailure.InvalidPost, a);
            if (!ValidPost(b, zipline)) return new(ConnectionFailure.InvalidPost, b);
            RopeSpan candidate = new(nextId, a, b, zipline, ropeType);
            HashSet<Point> markers = new();
            foreach (Point point in candidate.Tiles())
            {
                if (!WorldGen.InWorld(point.X, point.Y, 10)) return new(ConnectionFailure.OutsideWorld, point);
                if (!markers.Add(point)) return new(ConnectionFailure.OverlappingSpan, point);
                bool shared = tileSpans.TryGetValue(point, out HashSet<int> owners);
                if (shared && owners.Any(id => !CanShareMarker(candidate, Get(id))))
                    return new(ConnectionFailure.OverlappingSpan, point);
                Tile tile = Main.tile[point.X, point.Y];
                int expected = zipline ? ModContent.TileType<ZiplineRopeTile>()
                    : Array.IndexOf(candidate.DeckTiles, point) >= 0 ? ModContent.TileType<RopeBridgeDeckTile>() : ModContent.TileType<RopeBridgeRopeTile>();
                bool matchingMarker = tile.TileType == expected || !zipline
                    && (tile.TileType == ModContent.TileType<RopeBridgeDeckTile>() || tile.TileType == ModContent.TileType<RopeBridgeRopeTile>());
                if (tile.HasTile && !CuttablePlant(tile) && (!shared || !matchingMarker)) return new(ConnectionFailure.OccupiedTile, point);
            }
            for (int i = 0; i < candidate.Rest.Length - 1; i++)
            {
                int samples = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(candidate.Rest[i], candidate.Rest[i + 1]) / 8));
                for (int sample = 1; sample <= samples; sample++)
                {
                    if (i == candidate.Rest.Length - 2 && sample == samples) continue;
                    Vector2 position = Vector2.Lerp(candidate.Rest[i], candidate.Rest[i + 1], sample / (float)samples)
                        + new Vector2(-10, zipline ? 2 : -46);
                    int clearanceWidth = 20;
                    if (!zipline)
                    {
                        float rightEdge = Math.Min(position.X + clearanceWidth, b.X * 16);
                        position.X = Math.Max(position.X, (a.X + 1) * 16);
                        clearanceWidth = (int)(rightEdge - position.X);
                    }
                    if (Collision.SolidCollision(position, clearanceWidth, 46))
                    {
                        Point blocked = FindSolidBlocker(position, clearanceWidth, 46);
                        bool postSupport = zipline && (blocked == new Point(a.X, a.Y + 1) || blocked == new Point(b.X, b.Y + 1));
                        return new(postSupport ? ConnectionFailure.PostSupportBlocked : ConnectionFailure.BlockedClearance, blocked);
                    }
                }
            }
            span = candidate;
            return null;
        }

        private static bool CuttablePlant(Tile tile)
            => tile.HasTile && Main.tileCut[tile.TileType] && !Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];

        private static bool CanShareMarker(RopeSpan candidate, RopeSpan existing)
            => existing != null && existing.Zipline == candidate.Zipline
                && (existing.Left == candidate.Left || existing.Left == candidate.Right || existing.Right == candidate.Left || existing.Right == candidate.Right);

        private static Point FindSolidBlocker(Vector2 position, int width, int height)
        {
            Point start = position.ToTileCoordinates();
            Vector2 end = position + new Vector2(width, height);
            Point last = new((int)MathF.Ceiling(end.X / 16) - 1, (int)MathF.Ceiling(end.Y / 16) - 1);
            for (int x = start.X; x <= last.X; x++)
            {
                for (int y = start.Y; y <= last.Y; y++)
                {
                    if (!WorldGen.InWorld(x, y)) continue;
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]) continue;
                    Vector2 corner = Vector2.Max(position, new Vector2(x * 16, y * 16));
                    Vector2 extent = Vector2.Min(end, new Vector2((x + 1) * 16, (y + 1) * 16)) - corner;
                    if (extent.X > 0 && extent.Y > 0 && Collision.SolidCollision(corner, (int)MathF.Ceiling(extent.X), (int)MathF.Ceiling(extent.Y)))
                        return new Point(x, y);
                }
            }
            return start;
        }

        public static bool Create(Point a, Point b, bool zipline, int ropeType = ItemID.Rope)
        {
            if (!CanCreate(a, b, zipline, out RopeSpan span, ropeType)) return false;
            Add(span);
            nextId++;
            int deckType = ModContent.TileType<RopeBridgeDeckTile>();
            int ropeTile = zipline ? ModContent.TileType<ZiplineRopeTile>() : ModContent.TileType<RopeBridgeRopeTile>();
            foreach (Point point in span.DeckTiles) PlaceMarker(point, deckType);
            foreach (Point point in span.RopeTiles) PlaceMarker(point, ropeTile);
            if (Main.netMode == NetmodeID.Server)
            {
                SendTiles(span);
                ModPacket packet = NewPacket(1);
                WriteSpan(packet, span);
                packet.Send();
            }
            return true;
        }

        private static void PlaceMarker(Point point, int type)
        {
            Tile tile = Main.tile[point.X, point.Y];
            if (CuttablePlant(tile)) WorldGen.KillTile(point.X, point.Y);
            tile.ClearTile();
            tile.HasTile = true;
            tile.TileType = (ushort)type;
            tile.TileFrameX = tile.TileFrameY = 0;
        }

        private static void Add(RopeSpan span)
        {
            Spans[span.Id] = span;
            AddOwner(postSpans, span.Left, span.Id);
            AddOwner(postSpans, span.Right, span.Id);
            foreach (Point point in span.Tiles()) AddOwner(tileSpans, point, span.Id);
            RefreshPost(span.Left);
            RefreshPost(span.Right);
            nextId = Math.Max(nextId, span.Id + 1);
        }

        public static void BreakAt(Point point)
        {
            if (breaking || Main.netMode == NetmodeID.MultiplayerClient) return;
            if (!tileSpans.TryGetValue(point, out HashSet<int> ids) && !postSpans.TryGetValue(point, out ids)) return;
            foreach (int id in ids.ToArray())
            {
                RopeSpan span = Get(id);
                if (span != null) Remove(span, true);
            }
        }

        private static void Remove(RopeSpan span, bool drop)
        {
            if (!Spans.Remove(span.Id)) return;
            RemoveOwner(postSpans, span.Left, span.Id);
            RemoveOwner(postSpans, span.Right, span.Id);
            RefreshPost(span.Left);
            RefreshPost(span.Right);
            breaking = true;
            try
            {
                foreach (Point point in span.Tiles())
                {
                    bool shared = RemoveOwner(tileSpans, point, span.Id);
                    Tile tile = Main.tile[point.X, point.Y];
                    bool deck = Array.IndexOf(span.DeckTiles, point) >= 0;
                    int expected = span.Zipline ? ModContent.TileType<ZiplineRopeTile>()
                        : deck ? ModContent.TileType<RopeBridgeDeckTile>() : ModContent.TileType<RopeBridgeRopeTile>();
                    if (!shared && tile.HasTile && (tile.TileType == expected || !span.Zipline
                        && (tile.TileType == ModContent.TileType<RopeBridgeDeckTile>() || tile.TileType == ModContent.TileType<RopeBridgeRopeTile>()))) tile.ClearTile();
                    if (!Main.dedServ)
                    {
                        Vector2 dustPosition = span.At(span.Closest(new Vector2(point.X * 16 + 8, (point.Y + (deck || span.Zipline ? 0 : 2)) * 16 + 8))) - new Vector2(8, deck || span.Zipline ? 4 : 36);
                        for (int i = 0; i < 3; i++) Dust.NewDust(dustPosition, 16, 8, DustID.WoodFurniture);
                    }
                }
                foreach (Player player in Main.player)
                    if (player.active && player.GetModPlayer<RopeSpanPlayer>().RideId == span.Id) player.GetModPlayer<RopeSpanPlayer>().Dismount(false);
            }
            finally { breaking = false; }
            if (drop && Main.netMode != NetmodeID.MultiplayerClient)
                Item.NewItem(new EntitySource_TileBreak(span.Left.X, span.Left.Y), span.Bounds, span.RopeType, span.Sections * 2);
            if (Main.netMode == NetmodeID.Server)
            {
                SendTiles(span);
                ModPacket packet = NewPacket(2);
                packet.Write(span.Id);
                packet.Send();
            }
        }

        private static void SendTiles(RopeSpan span)
        {
            int top = Math.Min(span.Left.Y, span.Right.Y) - 7;
            int bottom = Math.Max(span.Left.Y, span.Right.Y) + 4;
            NetMessage.SendTileSquare(-1, span.Left.X, top, span.Right.X - span.Left.X + 1, bottom - top + 1);
        }

        public static void ClickPost(Point point, bool zipline)
        {
            RememberPreviewTarget(Main.myPlayer, point, zipline, Main.LocalPlayer.HeldItem.type);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = NewPacket(0);
                packet.Write((short)point.X);
                packet.Write((short)point.Y);
                packet.Write(zipline);
                packet.Send();
            }
            else Link(Main.myPlayer, point, zipline);
        }

        internal static void RefreshPost(Point point)
        {
            RopeSpan span = postSpans.TryGetValue(point, out HashSet<int> ids) ? ids.OrderBy(id => id).Select(Get).FirstOrDefault() : null;
            if (span != null && span.Zipline)
            {
                Vector2 anchor = JunctionAnchor(span, span.Right == point);
                foreach (int id in ids) Get(id)?.SetJunction(point, anchor);
                return;
            }
            if (!WorldGen.InWorld(point.X, point.Y) || !Main.tile[point.X, point.Y].HasTile
                || Main.tile[point.X, point.Y].TileType != ModContent.TileType<RopeBridgePostTile>()) return;
            int style = span == null ? Main.tile[point.X, point.Y].TileFrameX / 36 : RopeStyle(span.RopeType);
            short frameX = (short)((style * 2 + (span == null ? 0 : 1)) * 18);
            for (int row = 0; row < 3; row++)
            {
                Tile tile = Main.tile[point.X, point.Y - row];
                if (tile.HasTile && tile.TileType == ModContent.TileType<RopeBridgePostTile>()) tile.TileFrameX = frameX;
            }
        }

        public static bool TryChangeRopeAt(Vector2 position)
        {
            Player player = Main.LocalPlayer;
            if (!IsRope(player.HeldItem)) return false;
            Point point = position.ToTileCoordinates();
            if (!WorldGen.InWorld(point.X, point.Y)) return false;
            Tile tile = Main.tile[point.X, point.Y];
            if (tile.HasTile && TileLoader.GetTile(tile.TileType) is RopePost) return false;
            RopeSpan selected = null;
            float distance = 20 * 20;
            foreach (RopeSpan span in Spans.Values)
            {
                float parameter = span.Closest(position);
                float d = Vector2.DistanceSquared(position, span.At(parameter));
                if (!span.Zipline)
                {
                    Vector2 railPosition = position + new Vector2(0, 32);
                    d = Math.Min(d, Vector2.DistanceSquared(railPosition, span.At(span.Closest(railPosition))));
                }
                if (d >= distance) continue;
                selected = span;
                distance = d;
            }
            return selected != null && ClickSpan(selected.Id);
        }

        public static bool ClickSpan(int id)
        {
            if (Get(id) == null || !IsRope(Main.LocalPlayer.HeldItem)) return false;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = NewPacket(9);
                packet.Write(id);
                packet.Send();
                return true;
            }
            return ChangeRope(Main.myPlayer, id);
        }

        private static bool ChangeRope(int playerId, int id)
        {
            if (playerId < 0 || playerId >= Main.maxPlayers) return false;
            Player player = Main.player[playerId];
            RopeSpan span = Get(id);
            if (span == null || !player.active || player.dead || !IsRope(player.HeldItem)) return false;
            Point point = span.At(span.Closest(player.Center)).ToTileCoordinates();
            if (!player.IsInTileInteractionRange(point.X, point.Y, TileReachCheckSettings.Simple)) return false;
            SetSelection(playerId, null);
            if (span.RopeType == player.HeldItem.type) return true;
            SetRopeType(span, player.HeldItem.type);
            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = NewPacket(10);
                packet.Write(id);
                packet.Write(span.RopeType);
                packet.Send();
            }
            return true;
        }

        private static void SetRopeType(RopeSpan span, int ropeType)
        {
            span.RopeType = ropeType;
            RefreshPost(span.Left);
            RefreshPost(span.Right);
        }

        private static void RememberPreviewTarget(int player, Point point, bool zipline, int rope)
        {
            if (selections.TryGetValue(player, out var first) && first.Zipline == zipline && first.Rope == rope && first.Point != point)
                previewTargets[player] = point;
        }

        private static void SetSelection(int player, Point? point, bool zipline = false, int rope = ItemID.Rope)
        {
            if (point.HasValue) selections[player] = (point.Value, zipline, rope, Main.GameUpdateCount + SelectionDuration);
            else selections.Remove(player);
            previewTargets.Remove(player);
            if (Main.netMode != NetmodeID.Server) return;
            ModPacket packet = NewPacket(8);
            packet.Write(point.HasValue);
            if (point.HasValue)
            {
                packet.Write((short)point.Value.X);
                packet.Write((short)point.Value.Y);
                packet.Write(zipline);
                packet.Write(rope);
            }
            packet.Send(player);
        }

        internal static bool TryGetPreviewTarget(int playerId, Vector2 cursor, out Point a, out Point b, out bool zipline, out int rope)
        {
            a = b = default;
            zipline = false;
            rope = ItemID.Rope;
            if (playerId < 0 || playerId >= Main.maxPlayers || !selections.TryGetValue(playerId, out var first)) return false;
            Player player = Main.player[playerId];
            if (!player.active || player.dead || player.mouseInterface || Main.playerInventory || Main.mapFullscreen
                || Main.GameUpdateCount >= first.ExpiresAt || !IsRope(player.HeldItem) || player.HeldItem.type != first.Rope
                || !ValidPost(first.Point, first.Zipline)) return false;
            a = first.Point;
            zipline = first.Zipline;
            rope = first.Rope;
            Point mouse = cursor.ToTileCoordinates();
            if (WorldGen.InWorld(mouse.X, mouse.Y))
            {
                Tile tile = Main.tile[mouse.X, mouse.Y];
                int postType = zipline ? ModContent.TileType<ZiplinePostTile>() : ModContent.TileType<RopeBridgePostTile>();
                if (tile.HasTile && tile.TileType == postType && TileLoader.GetTile(tile.TileType) is RopePost post)
                {
                    Point bottom = post.Bottom(mouse.X, mouse.Y);
                    if (bottom != a) { b = bottom; return WorldGen.InWorld(b.X, b.Y, 10); }
                }
            }
            return previewTargets.TryGetValue(playerId, out b) && b != a && WorldGen.InWorld(b.X, b.Y, 10);
        }

        private static void Link(int playerId, Point point, bool zipline)
        {
            if (playerId < 0 || playerId >= Main.maxPlayers) return;
            Player player = Main.player[playerId];
            if (!player.active || player.dead || !IsRope(player.HeldItem)) return;
            if (selections.TryGetValue(playerId, out var previous) && Main.GameUpdateCount >= previous.ExpiresAt)
                SetSelection(playerId, null);
            RememberPreviewTarget(playerId, point, zipline, player.HeldItem.type);
            if (!player.IsInTileInteractionRange(point.X, point.Y, TileReachCheckSettings.Simple))
            {
                SetSelection(playerId, null);
                Tell(playerId, outOfReachText);
                return;
            }
            if (!ValidPost(point, zipline))
            {
                ReportConnectionError(playerId, new ConnectionError(ConnectionFailure.InvalidPost, point), zipline);
                return;
            }
            if (ConnectionCount(point) >= MaximumConnections)
            {
                ReportConnectionError(playerId, new ConnectionError(ConnectionFailure.PostInUse, point, ConnectionCount(point), MaximumConnections), zipline);
                return;
            }
            if (!selections.TryGetValue(playerId, out var first) || first.Zipline != zipline || first.Rope != player.HeldItem.type || !ValidPost(first.Point, zipline))
            {
                SetSelection(playerId, point, zipline, player.HeldItem.type);
                ShowConnectionBlocker(playerId, null);
                Tell(playerId, selectedText);
                return;
            }
            if (first.Point == point) return;
            ConnectionError? error = CheckConnection(first.Point, point, zipline, out RopeSpan span, first.Rope);
            if (error.HasValue)
            {
                ReportConnectionError(playerId, error.Value, zipline);
                return;
            }
            int cost = span.Sections * 2;
            int available = player.inventory.Take(58).Where(item => item.type == first.Rope).Sum(item => item.stack);
            if (available < cost)
            {
                SetSelection(playerId, null);
                ShowConnectionBlocker(playerId, null);
                Tell(playerId, neededText, cost, available, cost - available);
                return;
            }
            if (!Create(first.Point, point, zipline, first.Rope))
            {
                SetSelection(playerId, null);
                return;
            }
            SetSelection(playerId, null);
            ShowConnectionBlocker(playerId, null);
            for (int slot = 0; slot < 58 && cost > 0; slot++)
            {
                Item item = player.inventory[slot];
                if (item.type != first.Rope) continue;
                int take = Math.Min(cost, item.stack);
                item.stack -= take;
                cost -= take;
                if (item.stack == 0) item.TurnToAir();
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncEquipment, playerId, -1, null, playerId, slot);
            }
        }

        private static void ReportConnectionError(int player, ConnectionError error, bool zipline)
        {
            SetSelection(player, null);
            LocalizedText message = error.Reason switch
            {
                ConnectionFailure.TooShort => tooShortText,
                ConnectionFailure.TooLong => tooLongText,
                ConnectionFailure.TooSteep => tooSteepText,
                ConnectionFailure.PostInUse => occupiedPostText,
                ConnectionFailure.DuplicateConnection => duplicateText,
                ConnectionFailure.InvalidPost => invalidPostText,
                ConnectionFailure.OutsideWorld => outsideWorldText,
                ConnectionFailure.OccupiedTile => blockedTileText,
                ConnectionFailure.OverlappingSpan => overlapText,
                ConnectionFailure.BlockedClearance => zipline ? ziplineClearanceText : bridgeClearanceText,
                ConnectionFailure.PostSupportBlocked => ziplineSupportText,
                _ => throw new ArgumentOutOfRangeException(nameof(error))
            };
            ShowConnectionBlocker(player, error.Blocker);
            Tell(player, message, error.Actual, error.Limit);
        }

        private static void ShowConnectionBlocker(int player, Point? point)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = NewPacket(7);
                packet.Write(point.HasValue);
                if (point.HasValue)
                {
                    packet.Write((short)point.Value.X);
                    packet.Write((short)point.Value.Y);
                }
                packet.Send(player);
            }
            else if (player == Main.myPlayer) RopeSpanDrawing.ShowBlocker(point);
        }

        private static void Tell(int player, LocalizedText text, params object[] arguments)
        {
            if (Main.netMode == NetmodeID.Server)
                Terraria.Chat.ChatHelper.SendChatMessageToClient(Terraria.Localization.NetworkText.FromKey(text.Key, arguments), new Color(245, 220, 140), player);
            else Main.NewText(text.Format(arguments), new Color(245, 220, 140));
        }

        internal static ModPacket NewPacket(byte operation)
        {
            ModPacket packet = ModContent.GetInstance<global::AerovelenceMod.AerovelenceMod>().GetPacket();
            packet.Write(Packet);
            packet.Write(operation);
            return packet;
        }

        public static void Receive(BinaryReader reader, int sender)
        {
            byte operation = reader.ReadByte();
            if (Main.netMode == NetmodeID.Server)
            {
                if (operation == 0) Link(sender, new Point(reader.ReadInt16(), reader.ReadInt16()), reader.ReadBoolean());
                else if (operation == 9) ChangeRope(sender, reader.ReadInt32());
                else if (operation == 3)
                {
                    int id = reader.ReadInt32();
                    float parameter = reader.ReadSingle();
                    bool transfer = reader.ReadBoolean();
                    float momentum = reader.ReadSingle();
                    if (sender < 0 || sender >= Main.maxPlayers) return;
                    RopeSpanPlayer rider = Main.player[sender].GetModPlayer<RopeSpanPlayer>();
                    if (id < 0) rider.Dismount(false);
                    else rider.Mount(Get(id), parameter, transfer ? momentum : null);
                    rider.SendRide();
                }
                else if (operation == 5 && sender >= 0 && sender < Main.maxPlayers)
                    Main.player[sender].GetModPlayer<RopeSpanPlayer>().RemoteControls = (byte)(reader.ReadByte() & 31);
                return;
            }
            if (operation == 1)
            {
                RopeSpan span = ReadSpan(reader);
                if (span != null && !Spans.ContainsKey(span.Id)) Add(span);
            }
            else if (operation == 2)
            {
                RopeSpan span = Get(reader.ReadInt32());
                if (span != null) Remove(span, false);
            }
            else if (operation == 10)
            {
                RopeSpan span = Get(reader.ReadInt32());
                int rope = reader.ReadInt32();
                if (span != null && ContentSamples.ItemsByType.TryGetValue(rope, out Item item) && IsRope(item)) SetRopeType(span, rope);
            }
            else if (operation == 4)
            {
                int playerId = reader.ReadByte();
                int id = reader.ReadInt32();
                float parameter = reader.ReadSingle(), speed = reader.ReadSingle();
                if (playerId < Main.maxPlayers && float.IsFinite(parameter) && float.IsFinite(speed))
                    Main.player[playerId].GetModPlayer<RopeSpanPlayer>().ReceiveRide(id, parameter, speed);
            }
            else if (operation == 7)
            {
                Point? point = reader.ReadBoolean() ? new Point(reader.ReadInt16(), reader.ReadInt16()) : null;
                if (!point.HasValue || WorldGen.InWorld(point.Value.X, point.Value.Y)) RopeSpanDrawing.ShowBlocker(point);
            }
            else if (operation == 8)
            {
                Point? point = reader.ReadBoolean() ? new Point(reader.ReadInt16(), reader.ReadInt16()) : null;
                bool zipline = point.HasValue && reader.ReadBoolean();
                int rope = point.HasValue ? reader.ReadInt32() : ItemID.Rope;
                if (!point.HasValue || WorldGen.InWorld(point.Value.X, point.Value.Y, 10) && rope > 0 && rope < ItemLoader.ItemCount)
                    SetSelection(Main.myPlayer, point, zipline, rope);
            }
            else if (operation == 6)
            {
                RopeSpan span = Get(reader.ReadInt32());
                int count = reader.ReadUInt16();
                for (int i = 0; i < count; i++)
                {
                    Vector2 point = new(reader.ReadSingle(), reader.ReadSingle());
                    if (span != null && count == span.Nodes.Length && float.IsFinite(point.X) && float.IsFinite(point.Y)
                        && Vector2.DistanceSquared(point, span.Rest[i]) <= 20 * 20) span.Nodes[i] = point;
                }
            }
        }

        private static void WriteSpan(BinaryWriter writer, RopeSpan span)
        {
            writer.Write(span.Id);
            writer.Write((short)span.Left.X);
            writer.Write((short)span.Left.Y);
            writer.Write((short)span.Right.X);
            writer.Write((short)span.Right.Y);
            writer.Write(span.Zipline);
            writer.Write(span.RopeType);
            writer.Write(span.Sag);
            writer.Write((ushort)span.Segments);
            writer.Write(span.LeftCeiling);
            writer.Write(span.RightCeiling);
        }

        private static RopeSpan ReadSpan(BinaryReader reader)
        {
            int id = reader.ReadInt32();
            Point a = new(reader.ReadInt16(), reader.ReadInt16()), b = new(reader.ReadInt16(), reader.ReadInt16());
            bool zip = reader.ReadBoolean();
            int rope = reader.ReadInt32();
            float sag = reader.ReadSingle();
            int segments = reader.ReadUInt16();
            bool leftCeiling = reader.ReadBoolean(), rightCeiling = reader.ReadBoolean();
            return ValidSegments(a, b, zip, segments) && ValidRecord(id, a, b, zip, rope) && float.IsFinite(sag) && sag >= 0 && sag <= 64
                ? new RopeSpan(id, a, b, zip, rope, sag, segments, leftCeiling, rightCeiling) : null;
        }

        private static bool ValidRecord(int id, Point a, Point b, bool zip, int rope) => id > 0 && rope > 0 && rope < ItemLoader.ItemCount
            && WorldGen.InWorld(a.X, a.Y, 10) && WorldGen.InWorld(b.X, b.Y, 10)
            && (zip ? a != b && (a.X < b.X || a.X == b.X && a.Y < b.Y) : b.X - a.X >= MinimumBridgeLength)
            && b.X - a.X <= (zip ? MaximumZiplineLength : MaximumBridgeLength)
            && (zip || Math.Abs(a.Y - b.Y) <= MaximumBridgeHeight);

        private static bool ValidSegments(Point a, Point b, bool zip, int segments)
            => zip ? segments >= 2 && segments <= Math.Max(2, Math.Max(b.X - a.X, Math.Abs(b.Y - a.Y)))
                : segments == b.X - a.X;

        public override void SaveWorldData(TagCompound tag)
        {
            tag["RopeSpans"] = Spans.Values.Select(span => new TagCompound {
                ["Id"] = span.Id, ["LeftX"] = span.Left.X, ["LeftY"] = span.Left.Y,
                ["RightX"] = span.Right.X, ["RightY"] = span.Right.Y, ["Zipline"] = span.Zipline, ["Sag"] = span.Sag, ["Segments"] = span.Segments,
                ["LeftCeiling"] = span.LeftCeiling, ["RightCeiling"] = span.RightCeiling,
                ["RopeName"] = ItemLoader.GetItem(span.RopeType)?.FullName ?? "Terraria/" + ItemID.Search.GetName(span.RopeType) }).ToList();
        }

        public override void LoadWorldData(TagCompound tag)
        {
            Reset();
            foreach (TagCompound data in tag.GetList<TagCompound>("RopeSpans"))
            {
                int id = data.GetInt("Id"), rope = ItemID.Rope;
                string name = data.GetString("RopeName");
                if (name.StartsWith("Terraria/") && ItemID.Search.TryGetId(name[9..], out int vanilla)) rope = vanilla;
                else if (ModContent.TryFind(name, out ModItem item)) rope = item.Type;
                Point a = new(data.GetInt("LeftX"), data.GetInt("LeftY")), b = new(data.GetInt("RightX"), data.GetInt("RightY"));
                bool zip = data.GetBool("Zipline");
                float sag = data.ContainsKey("Sag") ? data.GetFloat("Sag") : Math.Min(zip ? 20f : 12f, (b.X - a.X + 1) * 0.3f);
                int segments = data.ContainsKey("Segments") ? data.GetInt("Segments") : Math.Max(2, b.X - a.X);
                if (ValidSegments(a, b, zip, segments) && float.IsFinite(sag) && sag >= 0 && sag <= 64 && ValidRecord(id, a, b, zip, rope) && !Spans.ContainsKey(id) && ConnectionCount(a) < MaximumConnections && ConnectionCount(b) < MaximumConnections)
                    Add(new RopeSpan(id, a, b, zip, rope, sag, segments, data.GetBool("LeftCeiling"), data.GetBool("RightCeiling")));
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(Spans.Count);
            foreach (RopeSpan span in Spans.Values) WriteSpan(writer, span);
        }
        public override void NetReceive(BinaryReader reader)
        {
            Reset();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                RopeSpan span = ReadSpan(reader);
                if (span != null) Add(span);
            }
        }

        private static bool ValidMarker(Point point, bool zipline)
        {
            Tile tile = Main.tile[point.X, point.Y];
            return tile.HasTile && (zipline ? tile.TileType == ModContent.TileType<ZiplineRopeTile>()
                : tile.TileType == ModContent.TileType<RopeBridgeDeckTile>() || tile.TileType == ModContent.TileType<RopeBridgeRopeTile>());
        }

        public override void PreUpdateEntities()
        {
            for (int playerId = 0; playerId < Main.maxPlayers; playerId++)
            {
                Player player = Main.player[playerId];
                if (selections.TryGetValue(playerId, out var selection)
                    && (!player.active || player.dead || Main.GameUpdateCount >= selection.ExpiresAt
                        || !ValidPost(selection.Point, selection.Zipline))) SetSelection(playerId, null);
                if (!player.active || player.dead) continue;
                RopeSpanPlayer rider = player.GetModPlayer<RopeSpanPlayer>();
                RopeSpan span = Get(rider.RideId);
                if (span != null) span.AddLoad(rider.Parameter, 1);
                span = Get(rider.StandingId);
                if (span != null) span.AddLoad(span.Parameter(player.Center.X), 1);
            }
            foreach (RopeSpan span in Spans.Values) span.Update();
            if (Main.netMode == NetmodeID.MultiplayerClient || Main.GameUpdateCount % 120 != 0) return;
            foreach (RopeSpan span in Spans.Values.ToArray())
            {
                if (!ValidPost(span.Left, span.Zipline) || !ValidPost(span.Right, span.Zipline) || span.Tiles().Any(p => !ValidMarker(p, span.Zipline)))
                {
                    Remove(span, true);
                    continue;
                }
                if (Main.netMode != NetmodeID.Server) continue;
                ModPacket packet = NewPacket(6);
                packet.Write(span.Id);
                packet.Write((ushort)span.Nodes.Length);
                foreach (Vector2 node in span.Nodes)
                {
                    packet.Write(node.X);
                    packet.Write(node.Y);
                }
                packet.Send();
            }
        }
    }
}
