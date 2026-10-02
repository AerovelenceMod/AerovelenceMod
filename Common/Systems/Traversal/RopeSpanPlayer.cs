using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Systems.Traversal
{
    public sealed class RopeSpanPlayer : ModPlayer
    {
        private const byte LeftInput = 1;
        private const byte RightInput = 2;
        private const byte DownInput = 4;
        private const byte JumpInput = 8;
        private const byte UpInput = 16;
        private const float RestSpeed = 0.4f;

        public int RideId { get; private set; } = -1;
        public int StandingId { get; private set; } = -1;
        public float Parameter { get; private set; }
        public float Speed { get; private set; }
        public byte RemoteControls { get; set; }
        public bool Riding => RideId > 0 && RopeSpanSystem.Get(RideId) != null;
        internal bool HoldingUp => (Controls() & UpInput) != 0;
        private Vector2 previousBottom;
        private Vector2 ridePosition;
        private int detachTimer;
        private byte sentControls;
        private byte rideControls;
        private bool controlsCaptured;
        private float pulleyCounter;
        private int pulleyFrame;
        private bool Simulate => Main.netMode != NetmodeID.MultiplayerClient || Player.whoAmI == Main.myPlayer;

        public override void OnEnterWorld()
        {
            RideId = StandingId = -1;
            Parameter = Speed = pulleyCounter = 0;
            RemoteControls = sentControls = rideControls = 0;
            controlsCaptured = false;
            detachTimer = pulleyFrame = 0;
        }

        public override void PreUpdate()
        {
            if (!Simulate) return;
            controlsCaptured = false;
            previousBottom = Player.Bottom;
            if (detachTimer > 0) detachTimer--;
            if (Player.dead || !Player.active || Player.gravDir < 0 || Player.mount.Active || Player.pulley)
            {
                Dismount(false);
                StandingId = -1;
                return;
            }
            if (Player.whoAmI == Main.myPlayer && !Main.gameMenu && !Main.playerInventory && !Player.mouseInterface && Main.mouseRight && Main.mouseRightRelease)
            {
                if (Riding)
                {
                    Dismount(true);
                    Main.mouseRightRelease = false;
                }
                else if (TryRideAt(Main.MouseWorld)) Main.mouseRightRelease = false;
            }
            RopeSpan span = RopeSpanSystem.Get(StandingId);
            if (span == null)
            {
                StandingId = -1;
                return;
            }
            if (Player.controlDown || Player.velocity.Y < -0.5f || Player.justJumped || Player.Bottom.X < span.Nodes[0].X || Player.Bottom.X > span.Nodes[^1].X)
            {
                StandingId = -1;
                detachTimer = Player.controlDown ? 15 : 3;
                return;
            }
            if (Math.Abs(Player.Bottom.Y - span.SurfaceY(Player.Left.X + 3, Player.Right.X - 3, true)) >= 18)
            {
                StandingId = -1;
                return;
            }
            Vector2 carry = new(0, span.SurfaceY(Player.Left.X + 3, Player.Right.X - 3)
                - span.SurfaceY(Player.Left.X + 3, Player.Right.X - 3, true));
            Player.position += Collision.TileCollision(Player.position, carry, Player.width, Player.height, true, true);
            previousBottom = Player.Bottom;
            Player.velocity.Y = 0;
            Player.jump = 0;
            Player.fallStart = Player.fallStart2 = (int)(Player.position.Y / 16);
        }

        public override void PostUpdateRunSpeeds()
        {
            if (StandingId > 0) ResolveStanding(Player.controlDown);
        }

        public override void SetControls()
        {
            rideControls = ReadControls();
            controlsCaptured = true;
            if (Simulate && !Riding && !Main.playerInventory && !Player.mouseInterface
                && (Main.netMode != NetmodeID.Server || Player.whoAmI == Main.myPlayer)) TryRideNearby();
            if (Riding)
            {
                Player.controlUp = false;
                Player.controlDown = false;
            }
        }

        public override void PostUpdateEquips()
        {
            if (Riding) Player.portableStoolInfo = default;
        }

        public bool TryRideAt(Vector2 position)
        {
            if (Riding || detachTimer > 0 || Player.dead || Player.mount.Active || Player.gravDir < 0 || Player.pulley) return false;
            return TrySelectZipline(Player, position, Main.SmartCursorIsUsed, out RopeSpan span, out float parameter)
                && TryMount(span, parameter);
        }

        internal static bool TrySelectZipline(Player player, Vector2 position, bool smartCursor, out RopeSpan selected, out float parameter)
        {
            selected = null;
            parameter = 0;
            float distance = float.MaxValue;
            foreach (RopeSpan span in RopeSpanSystem.Spans.Values)
            {
                if (!span.Zipline) continue;
                float t;
                if (smartCursor)
                {
                    if (!span.ClosestReachable(position, player.Center, 95.99f, out t)) continue;
                }
                else t = span.Closest(position);
                Vector2 point = span.At(t);
                float d = Vector2.DistanceSquared(position, point);
                if ((!smartCursor && d > 28 * 28) || Vector2.DistanceSquared(player.Center, point) > 96 * 96 || d >= distance) continue;
                selected = span;
                parameter = t;
                distance = d;
            }
            return selected != null;
        }

        private bool TryRideNearby()
        {
            byte controls = Controls();
            if (detachTimer > 0 || (controls & (UpInput | DownInput)) == 0 || (controls & JumpInput) != 0 || Player.grapCount > 0) return false;
            RopeSpan selected = null;
            float parameter = 0, distance = 32 * 32;
            foreach (RopeSpan span in RopeSpanSystem.Spans.Values)
            {
                if (!span.Zipline) continue;
                float t = span.Closest(Player.Top);
                float d = Vector2.DistanceSquared(Player.Top, span.At(t));
                if (d >= distance) continue;
                selected = span;
                parameter = t;
                distance = d;
            }
            return TryMount(selected, parameter);
        }

        internal bool TryMount(RopeSpan span, float parameter) => Mount(span, parameter, null);

        internal bool Mount(RopeSpan span, float parameter, float? momentum)
        {
            if (span == null || !span.Zipline || !float.IsFinite(parameter) || parameter < 0 || parameter > 1
                || !Player.active || Player.dead || Player.gravDir < 0 || Player.mount.Active || Player.pulley) return false;
            if (Vector2.DistanceSquared(Player.Center, span.At(parameter)) > 96 * 96) return false;
            Vector2 target = HangPosition(span, parameter);
            if (Collision.SolidCollision(target, Player.width, Player.height)) return false;
            RopeSpan previous = RopeSpanSystem.Get(RideId);
            Point post = parameter < 0.5f ? span.Left : span.Right;
            Vector2 anchor = parameter < 0.5f ? span.Nodes[0] : span.Nodes[^1];
            bool transferring = previous != null && previous.Id != span.Id
                && (previous.Left == post || previous.Right == post) && Vector2.DistanceSquared(span.At(parameter), anchor) <= 16 * 16;
            if (momentum.HasValue && (!transferring || !float.IsFinite(momentum.Value) || Math.Abs(momentum.Value) > 12
                || (parameter < 0.5f ? momentum.Value < 0 : momentum.Value > 0))) return false;
            RideId = span.Id;
            Parameter = parameter;
            Speed = momentum ?? MathHelper.Clamp(Vector2.Dot(Player.velocity, span.Tangent(parameter)), -12, 12);
            StandingId = -1;
            Player.RemoveAllGrapplingHooks();
            ridePosition = target;
            Player.position = target;
            Player.gfxOffY = 0;
            Player.legFrameCounter = 0;
            Player.velocity = Vector2.Zero;
            if (transferring && Math.Abs(Speed) > 0.1f) Player.direction = Math.Sign(Speed);
            if (Main.netMode != NetmodeID.Server || !transferring) RemoteControls = Controls();
            if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
            {
                ModPacket packet = RopeSpanSystem.NewPacket(3);
                packet.Write(RideId);
                packet.Write(Parameter);
                packet.Write(momentum.HasValue);
                packet.Write(Speed);
                packet.Send();
                SendControls();
            }
            else if (Main.netMode == NetmodeID.Server) SendRide();
            return true;
        }

        public void Dismount(bool notify)
        {
            RopeSpan span = RopeSpanSystem.Get(RideId);
            if (RideId < 0) return;
            if (span != null) Player.velocity = span.Tangent(Parameter) * Speed;
            RideId = -1;
            detachTimer = 20;
            if (!notify) return;
            if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
            {
                ModPacket packet = RopeSpanSystem.NewPacket(3);
                packet.Write(-1);
                packet.Write(Parameter);
                packet.Write(false);
                packet.Write(Speed);
                packet.Send();
            }
            else if (Main.netMode == NetmodeID.Server) SendRide();
        }

        private byte Controls() => controlsCaptured ? rideControls : ReadControls();

        private byte ReadControls() => (byte)((Player.controlLeft ? LeftInput : 0) | (Player.controlRight ? RightInput : 0)
            | (Player.controlDown ? DownInput : 0) | (Player.controlJump ? JumpInput : 0) | (Player.controlUp ? UpInput : 0));

        private static int Steering(byte controls) => ((controls & RightInput) != 0 ? 1 : 0) - ((controls & LeftInput) != 0 ? 1 : 0);

        private Vector2 HangPosition(RopeSpan span, float parameter) => span.At(parameter) + new Vector2(-Player.width * 0.5f, 6);

        private static RopeSpan SelectJunctionRoute(RopeSpan incoming, Point post, byte controls)
        {
            RopeSpan[] routes = RopeSpanSystem.JunctionRoutes(incoming, post);
            Vector2 approach = incoming.Tangent(post == incoming.Left ? 0 : 1) * (post == incoming.Left ? -1 : 1);
            int steering = Steering(controls);
            bool matchingSide = false;
            foreach (RopeSpan route in routes)
                if (steering != 0 && Math.Sign((route.Left == post ? route.Right : route.Left).X - post.X) == steering) matchingSide = true;
            RopeSpan selected = null;
            float best = float.NegativeInfinity, alignment = float.NegativeInfinity;
            foreach (RopeSpan route in routes)
            {
                bool startsHere = route.Left == post;
                if (matchingSide && Math.Sign((startsHere ? route.Right : route.Left).X - post.X) != steering) continue;
                Vector2 departure = route.Tangent(startsHere ? 0 : 1) * (startsHere ? 1 : -1);
                float dot = Vector2.Dot(approach, departure);
                float cross = approach.X * departure.Y - approach.Y * departure.X;
                float score = steering == 0 ? dot : MathF.Atan2(cross, dot) * steering;
                if (score < best || Math.Abs(score - best) < 0.0001f && dot <= alignment) continue;
                selected = route;
                best = score;
                alignment = dot;
            }
            return selected;
        }

        private bool RestAtJunction(RopeSpan span, Point post, byte controls)
        {
            if (Math.Abs(Speed) > RestSpeed || span.Left.X == span.Right.X && (controls & UpInput) != 0) return false;
            RopeSpan[] routes = RopeSpanSystem.JunctionRoutes(span, post);
            if (routes.Length == 0) return false;
            Vector2 departure = span.Tangent(Parameter) * (span.Left == post ? 1 : -1);
            if (departure.Y >= -0.001f) return false;
            foreach (RopeSpan route in routes)
                if ((route.Tangent(route.Left == post ? 0 : 1) * (route.Left == post ? 1 : -1)).Y >= -0.001f) return false;
            int steering = Steering(controls);
            if (steering != 0 && (span.Left == post ? 1 : -1) != steering)
            {
                RopeSpan route = SelectJunctionRoute(span, post, controls);
                if (route != null && Math.Sign((route.Left == post ? route.Right : route.Left).X - post.X) == steering)
                    Mount(route, route.Left == post ? 0 : 1, 0);
            }
            Speed = 0;
            return true;
        }

        private void SettleAtLowPoint(RopeSpan span)
        {
            if (Math.Abs(Speed) > RestSpeed) return;
            int node = (int)MathF.Round(Parameter * span.Segments);
            if (node <= 0 || node >= span.Segments) return;
            Vector2 point = span.Nodes[node];
            if (point.Y < span.Nodes[node - 1].Y || point.Y < span.Nodes[node + 1].Y
                || Vector2.DistanceSquared(span.At(Parameter), point) > RestSpeed * RestSpeed) return;
            Parameter = node / (float)span.Segments;
            Speed = 0;
        }

        internal void AnimatePulley()
        {
            pulleyCounter += Math.Abs(Speed);
            if (pulleyCounter > 10)
            {
                pulleyCounter %= 10;
                pulleyFrame = (pulleyFrame + 1) % 2;
            }
            Player.pulleyFrame = pulleyFrame;
            Player.pulleyFrameCounter = pulleyCounter;
        }

        private void SendControls()
        {
            sentControls = Controls();
            ModPacket packet = RopeSpanSystem.NewPacket(5);
            packet.Write(sentControls);
            packet.Send();
        }

        public override void PreUpdateMovement()
        {
            if (!Simulate) return;
            if (!Riding && (Main.netMode != NetmodeID.Server || Player.whoAmI == Main.myPlayer)) TryRideNearby();
            if (!Riding) return;
            if (Main.netMode == NetmodeID.MultiplayerClient && (sentControls != Controls() || Main.GameUpdateCount % 15 == 0)) SendControls();
            byte controls = Main.netMode == NetmodeID.Server ? RemoteControls : Controls();
            if ((controls & JumpInput) != 0 || Player.grapCount > 0 || Player.dead || Player.gravDir < 0 || Player.mount.Active)
            {
                Dismount(true);
                if ((controls & JumpInput) != 0) Player.velocity.Y = -5;
                return;
            }
            RopeSpan span = RopeSpanSystem.Get(RideId);
            Vector2 tangent = span.Tangent(Parameter);
            float drive = ((controls & RightInput) != 0 ? 0.12f : 0) - ((controls & LeftInput) != 0 ? 0.12f : 0);
            bool vertical = span.Left.X == span.Right.X;
            if (vertical) drive = (((controls & DownInput) != 0 ? 0.42f : 0) - ((controls & UpInput) != 0 ? 0.42f : 0)) * Math.Sign(tangent.Y);
            Speed = MathHelper.Clamp((Speed + tangent.Y * 0.28f + drive) * (!vertical && (controls & DownInput) != 0 ? 0.86f : 0.996f), -12, 12);
            Parameter = span.Advance(Parameter, Speed, out float remaining);
            if (!vertical && drive == 0) SettleAtLowPoint(span);
            ridePosition = HangPosition(span, Parameter);
            Player.velocity = Vector2.Zero;
            Player.jump = 0;
            Player.fallStart = (int)(Player.position.Y / 16);
            if (Math.Abs(Speed) > 0.1f) Player.direction = Math.Sign(Speed);
            if (Parameter == 0 || Parameter == 1)
            {
                Point post = Parameter == 0 ? span.Left : span.Right;
                if (RestAtJunction(span, post, controls)) return;
                RopeSpan route = SelectJunctionRoute(span, post, controls);
                if (route == null) Dismount(true);
                else
                {
                    float remainder = Math.Abs(remaining);
                    bool startsHere = route.Left == post;
                    Vector2 departure = route.Tangent(startsHere ? 0 : 1) * (startsHere ? 1 : -1);
                    float momentum = Math.Abs(Speed);
                    Vector2 approach = tangent * Math.Sign(Speed);
                    if (approach.Y > 0 && departure.Y < 0)
                    {
                        float alignment = Math.Max(0, Vector2.Dot(approach, departure));
                        momentum *= alignment;
                        remainder *= alignment;
                    }
                    float parameter = route.Advance(startsHere ? 0 : 1, remainder * (startsHere ? 1 : -1), out _);
                    if (!Mount(route, parameter, momentum * (startsHere ? 1 : -1))) Dismount(true);
                }
            }
        }

        public override void PostUpdate()
        {
            if (!Simulate) return;
            if (!Riding)
            {
                if (StandingId > 0) ResolveStanding(Player.controlDown);
                return;
            }
            Vector2 movement = ridePosition - Player.position;
            Vector2 allowed = Collision.TileCollision(Player.position, movement, Player.width, Player.height, true, true);
            if (Vector2.DistanceSquared(movement, allowed) > 1 || Collision.SolidCollision(ridePosition, Player.width, Player.height))
            {
                Dismount(true);
                return;
            }
            Player.position = ridePosition;
            Player.gfxOffY = 0;
            Player.velocity = Vector2.Zero;
            Player.fallStart = (int)(Player.position.Y / 16);
            Player.fallStart2 = Player.fallStart;
            if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 15 == 0) SendRide();
        }

        internal void ReceiveRide(int id, float parameter, float speed)
        {
            if (id < 0 || RopeSpanSystem.Get(id) == null)
            {
                Dismount(false);
                return;
            }
            RideId = id;
            StandingId = -1;
            Parameter = MathHelper.Clamp(parameter, 0, 1);
            Speed = MathHelper.Clamp(speed, -12, 12);
            if (Simulate)
            {
                ridePosition = HangPosition(RopeSpanSystem.Get(id), Parameter);
                Player.position = ridePosition;
            }
        }

        internal void SendRide(int toWho = -1)
        {
            ModPacket packet = RopeSpanSystem.NewPacket(4);
            packet.Write((byte)Player.whoAmI);
            packet.Write(RideId);
            packet.Write(Parameter);
            packet.Write(Speed);
            packet.Send(toWho);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            if (Main.netMode == NetmodeID.Server) SendRide(toWho);
        }
        public override void UpdateDead()
        {
            StandingId = -1;
            Dismount(true);
        }

        internal void ResolveStanding(bool fallThrough)
        {
            if (!Simulate) return;
            if (Riding || !Player.active || Player.dead || detachTimer > 0 || fallThrough || Player.controlDown || Player.gravDir < 0 || Player.pulley || Player.GoingDownWithGrapple || Player.velocity.Y < -0.1f || Player.justJumped)
            {
                StandingId = -1;
                return;
            }
            RopeSpan best = null;
            float surface = float.MaxValue;
            foreach (RopeSpan span in RopeSpanSystem.Spans.Values)
            {
                if (span.Zipline || Player.Right.X <= span.Nodes[0].X || Player.Left.X >= span.Nodes[^1].X) continue;
                float y = span.SurfaceY(Player.Left.X + 3, Player.Right.X - 3);
                bool attached = StandingId == span.Id && Math.Abs(Player.Bottom.Y - y) < 18;
                if (!attached && (previousBottom.Y > y + 6 || Player.Bottom.Y < y)) continue;
                if (y >= surface || Collision.SolidCollision(new Vector2(Player.position.X, y - Player.height), Player.width, Player.height)) continue;
                Vector2 movement = new(0, y - Player.Bottom.Y);
                Vector2 allowed = Collision.noSlopeCollision(Player.position, movement, Player.width, Player.height);
                if (Math.Abs(allowed.Y - movement.Y) > 0.01f) continue;
                best = span;
                surface = y;
            }
            StandingId = best?.Id ?? -1;
            if (best == null) return;
            Player.position.Y = surface - Player.height;
            Player.velocity.Y = 0;
            Player.fallStart = Player.fallStart2 = (int)(Player.position.Y / 16);
            Player.jump = 0;
        }
    }
}
