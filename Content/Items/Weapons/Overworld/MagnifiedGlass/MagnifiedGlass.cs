using AerovelenceMod.Common.Globals.SkillStrikes;
using AerovelenceMod.Content.Dusts.GlowDusts;
using AerovelenceMod.Common.Systems;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace AerovelenceMod.Content.Items.Weapons.Overworld.MagnifiedGlass
{
    public sealed class MagnifiedGlass : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Magnified Glass", "Focuses light into a burning point\nOnly works where there is enough natural light")
                .AddSkillStrike(Language.Default, "Focusing beams leaves fire that Skill Strikes");
        }
        public override void SetDefaults()
        {
            Item.width = Item.height = 30;
            Item.damage = 8;
            Item.DamageType = DamageClass.Summon;
            Item.knockBack = .5f;
            Item.mana = 3;
            Item.useTime = Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = Item.noUseGraphic = Item.channel = true;
            Item.shoot = ModContent.ProjectileType<MagnifiedLens>();
            Item.shootSpeed = 1;
            Item.UseSound = SoundID.Item8;
            Item.rare = ItemRarities.EarlyPHM;
            Item.value = Item.sellPrice(silver: 10);
        }
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int lenses = MagnifiedLens.FreeLenses(player);
            Vector2 positionToAim = MagnifiedLens.AimLens(player, Main.MouseWorld, lenses);
            Projectile.NewProjectile(source, positionToAim, Vector2.Zero, type, damage, knockback, player.whoAmI,
                lenses, positionToAim.X, positionToAim.Y);
            return false;
        }
        public override void AddRecipes() => CreateRecipe()
            .AddRecipeGroup(RecipeGroupID.Wood, 12)
            .AddIngredient(ItemID.Glass, 6)
            .AddIngredient(ItemID.FallenStar, 2)
            .AddTile(TileID.WorkBenches).Register();
    }
    public sealed class MagnifiedLens : ModProjectile
    {
        private const int ChargeTime = 120;
        private readonly float[] raySamples = new float[3];
        private readonly Vector2[] lensJoints = new Vector2[3];
        private readonly Vector2[] previousJoints = new Vector2[3];
        private readonly List<DrawData> lensDraws = [];
        private readonly float[] beamStops = new float[17];
        private readonly float[] incomingStops = new float[5];
        private Vector2 tracedOrigin;
        private float tracedFocus;
        private Action drawLight;
        private bool lightRegistered;
        private Vector2 sentCurs;
        private Point checkedTile;
        private bool openSky;
        private int age;
        private float beamLength;
        private Vector2 lastBurn;
        private bool hasBurn;
        internal bool Ready => age >= ChargeTime;
        private float BeamReach => Math.Min(Math.Min(beamLength, FocusDistance), FocusDistance * MathHelper.SmoothStep(0f, 1f,
            Utils.GetLerpValue(ChargeTime, ChargeTime + 18f, age, true)));
        internal int Lenses => Math.Clamp((int)Projectile.ai[0], 0, Math.Max(0, Main.player[Projectile.owner].maxMinions));
        internal float FocusDistance => 160 + Lenses * 12;
        internal float BeamStrength => 1f + .5f * Lenses / (Lenses + 3f);
        internal bool Underworld => Projectile.Center.Y >= Main.UnderworldLayer * 16;
        internal bool Moonlight => !Main.dayTime || Projectile.Center.Y < Main.worldSurface * 16 * .35f;
        private int BeamDirection => Underworld ? -1 : 1;
        private Vector2 BeamOrigin => Projectile.Center + new Vector2(0, BeamDirection * 6);
        internal Vector2 Focus => BeamOrigin + new Vector2(0, BeamDirection * FocusDistance);
        internal bool Sunlight => openSky && !Underworld && !Moonlight;
        internal bool GroundedFocus => Ready && (Sunlight || Underworld) && Math.Abs(beamLength - FocusDistance) <= 10
            && BeamReach >= Math.Min(beamLength, FocusDistance) - .5f;
        private Color BeamColor => Underworld ? new Color(255, 130, 45, 0) : Moonlight ? new Color(205, 225, 255, 0) : new Color(255, 205, 80, 0);
        public override string Texture => "AerovelenceMod/Content/Items/Weapons/Overworld/MagnifiedGlass/MagnifiedGlassConjure";
        public override Terraria.Localization.LocalizedText DisplayName => ModContent.GetInstance<MagnifiedGlass>().DisplayName;
        public override bool ShouldUpdatePosition() => false;
        internal static int FreeLenses(Player player) => Math.Max(0, (int)MathF.Floor(player.maxMinions - player.slotsMinions));
        internal static Vector2 ReachableCursor(Player player, Vector2 cursor)
        {
            if (!float.IsFinite(cursor.X) || !float.IsFinite(cursor.Y)) return player.MountedCenter;
            Vector2 offset = cursor - player.MountedCenter;
            Vector2 result = player.MountedCenter + offset.SafeNormalize(Vector2.Zero) * Math.Min(560, offset.Length());
            return Vector2.Clamp(result, Vector2.One * 32, new Vector2(Main.maxTilesX * 16 - 32, Main.maxTilesY * 16 - 32));
        }
        internal static Vector2 AimLens(Player player, Vector2 cursor, int lenses, float[] samples = null)
        {
            cursor = ReachableCursor(player, cursor);
            Vector2 direction = cursor.Y >= Main.UnderworldLayer * 16 ? -Vector2.UnitY : Vector2.UnitY;
            Vector2 start = cursor - direction * 24f;
            float ground = MagnifiedLight.Cast(start, direction, 48f, samples);
            if (ground > 0f && ground < 48f) cursor = start + direction * ground;
            Vector2 destination = cursor - direction * (166f + lenses * 12f);
            return Vector2.Clamp(destination, Vector2.One * 32f, new Vector2(Main.maxTilesX * 16 - 32, Main.maxTilesY * 16 - 32));
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }
        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.CCed || player.noItems || player.HeldItem.type != ModContent.ItemType<MagnifiedGlass>()
                || Projectile.owner == Main.myPlayer && !player.channel)
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 2;
            if (Projectile.owner == Main.myPlayer)
            {
                int lenses = FreeLenses(player);
                Vector2 cursor = AimLens(player, Main.MouseWorld, lenses, raySamples);
                if (age == 0 || lenses != Lenses || age % 4 == 0 && Vector2.DistanceSquared(cursor, sentCurs) > 4)
                {
                    sentCurs = cursor;
                    Projectile.netUpdate = true;
                }
                Projectile.ai[0] = lenses;
                Projectile.ai[1] = cursor.X;
                Projectile.ai[2] = cursor.Y;
                if (age > 0 && age % 30 == 0 && !player.CheckMana(1, true))
                {
                    Projectile.Kill();
                    return;
                }
                player.manaRegenDelay = player.maxRegenDelay;
            }
            Vector2 destination = new(Projectile.ai[1], Projectile.ai[2]);
            Projectile.velocity = (destination - Projectile.Center) * (Projectile.owner == Main.myPlayer ? 1f : .35f);
            Projectile.Center += Projectile.velocity;
            player.ChangeDir(Projectile.Center.X >= player.Center.X ? 1 : -1);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation = 2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Projectile.Center - player.MountedCenter).ToRotation() - MathHelper.PiOver2);
            Point tile = Projectile.Center.ToTileCoordinates();
            if (age == 0 || tile != checkedTile || age % 12 == 0)
            {
                checkedTile = tile;
                openSky = !Underworld && Projectile.Center.Y < Main.worldSurface * 16
                    && Collision.CanHitLine(Projectile.Center - Vector2.UnitY * 12, 1, 1, new Vector2(Projectile.Center.X, Math.Max(16f, Projectile.Center.Y - 50f * 16f)), 1, 1);
            }
            beamLength = MagnifiedLight.Cast(BeamOrigin, new Vector2(0f, BeamDirection), FocusDistance + 16f, raySamples);
            if (!Main.dedServ && (age == 0 || age % 4 == 0 || Vector2.DistanceSquared(tracedOrigin, BeamOrigin) > .25f || tracedFocus != FocusDistance))
                TraceLight();
            if (Projectile.owner == Main.myPlayer)
            {
                if (age <= ChargeTime && age % 30 == 0) Projectile.netUpdate = true;
                if (!GroundedFocus) hasBurn = false;
                else if (age % 2 == 0) BurnGround();
            }
            if (!Main.dedServ && Ready && (openSky || Underworld))
            {
                Vector2 end = BeamOrigin + new Vector2(0, BeamDirection * BeamReach);
                Lighting.AddLight(end, BeamColor.ToVector3() * (GroundedFocus ? .55f : .2f));
                if (age % 6 == 0 && BeamReach > 40f)
                {
                    float distance = Main.rand.NextFloat(16f, BeamReach);
                    Vector2 point = BeamOrigin + new Vector2(Main.rand.NextFloat(-.6f, .6f) * HalfW(distance), BeamDirection * distance);
                    bool glorb = age % 12 == 0;
                    Dust mote = Dust.NewDustPerfect(point, glorb ? ModContent.DustType<GlowStrong>() : ModContent.DustType<GlowPixelCross>(),
                        (Focus - point).SafeNormalize(Vector2.UnitY * BeamDirection) * (glorb ? 1.6f : .7f), 0, BeamColor, glorb ? .1f : .08f);
                    mote.noLight = true;
                }
                if (GroundedFocus && age % 3 == 0)
                {
                    Dust spark = Dust.NewDustPerfect(end - Vector2.UnitY * (BeamDirection * 2f), DustID.Torch, new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-2.5f, -.5f) * BeamDirection), 0, default, .8f);
                    spark.noGravity = true;
                    if (age % 12 == 0)
                    {
                        Dust ember = Dust.NewDustPerfect(end - Vector2.UnitY * (BeamDirection * 2f), ModContent.DustType<GlowStrong>(), new Vector2(Main.rand.NextFloat(-.7f, .7f), -1.2f * BeamDirection), 0, new Color(255, 160, 45, 0), .1f);
                        ember.noLight = true;
                    }
                }
            }
            age++; //dont we all...
        }
        public override void SendExtraAI(BinaryWriter writer) => writer.Write(age);
        public override void ReceiveExtraAI(BinaryReader reader) => age = Math.Max(age, reader.ReadInt32());
        internal float HalfW(float distance) => Math.Max(1.5f, 32f * (1f - distance / FocusDistance));
        public override bool? CanDamage() => Ready && (openSky || Underworld) ? null : false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (BeamReach <= 0f) return false;
            float nearest = BeamDirection > 0 ? targetHitbox.Top - BeamOrigin.Y : BeamOrigin.Y - targetHitbox.Bottom;
            float farthest = BeamDirection > 0 ? targetHitbox.Bottom - BeamOrigin.Y : BeamOrigin.Y - targetHitbox.Top;
            if (farthest < 0 || nearest > BeamReach) return false;
            float top = Math.Max(0, nearest);
            float bottom = Math.Min(BeamReach, farthest);
            float width = Math.Max(HalfW(top), HalfW(bottom));
            if (targetHitbox.Right < BeamOrigin.X - width || targetHitbox.Left > BeamOrigin.X + width) return false;
            Vector2 contact = Vector2.Clamp(new Vector2(BeamOrigin.X, BeamOrigin.Y + BeamDirection * (top + bottom) * .5f), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Collision.CanHitLine(BeamOrigin, 1, 1, contact, 1, 1);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= BeamStrength;
            Projectile.GetGlobalProjectile<SkillStrikeGProj>().SkillStrike = false;
            Rectangle focalPoint = new((int)Focus.X - 10, (int)Focus.Y - (BeamDirection > 0 ? 14 : 10), 20, 24);
            if (GroundedFocus && target.Hitbox.Intersects(focalPoint))
                SkillStrikeUtil.setSkillStrike(Projectile, 1.6f, 1, .2f, .3f);
        }
        private void BurnGround()
        {
            Vector2 direction = Vector2.UnitY * BeamDirection;
            Vector2 ground = BeamOrigin + direction * beamLength;
            float distance = hasBurn ? Vector2.Distance(lastBurn, ground) : 0f;
            if (distance > 12f && distance <= 560f)
            {
                int steps = (int)MathF.Ceiling(distance / 12f);
                for (int i = 1; i < steps; i++)
                {
                    Vector2 start = Vector2.Lerp(lastBurn, ground, i / (float)steps) - direction * 24f;
                    float floor = MagnifiedLight.Cast(start, direction, 48f, raySamples);
                    if (floor > 0f && floor < 48f) BurnGroundAt(start + direction * floor);
                }
            }
            BurnGroundAt(ground);
            lastBurn = ground;
            hasBurn = true;
        }
        private void BurnGroundAt(Vector2 ground)
        {
            Vector2 direction = Vector2.UnitY * BeamDirection;
            int type = ModContent.ProjectileType<MagnifiedScorch>(); 
            if (Collision.WetCollision(ground - new Vector2(6f, 6f) - direction * 6f, 12, 12)) return;
            foreach (Projectile flame in Main.ActiveProjectiles)
            {
                if (flame.type != type || flame.owner != Projectile.owner || (flame.ai[0] < 0f) != Underworld
                    || Vector2.DistanceSquared(flame.Center + direction * (flame.height * .5f), ground) > 8f * 8f) continue;
                flame.timeLeft = 120;
                if (age % 20 == 0) flame.netUpdate = true;
                return;
            }
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), ground - direction * 10f, Vector2.Zero, type, Math.Max(1, (int)(Projectile.damage * BeamStrength * .4f)), 0f, Projectile.owner, BeamDirection);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float appear = MathHelper.SmoothStep(0, 1, Math.Min(1, age / 12f));
            if (!lightRegistered)
            {
                drawLight ??= () =>
                {
                    float opacity = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, age / 12f));
                    DrawIncomingLight(opacity);
                    DrawBeam(opacity);
                };
                lightRegistered = ModContent.GetInstance<PixelationSystem>().RegisterPersistentRenderAction(RenderLayer.BeforeSolidTiles,
                    () => Projectile.active && (openSky || Underworld), drawLight);
            }
            DrawLenses(lightColor, appear);
            return false;
        }
        private void TraceLight()
        {
            tracedOrigin = BeamOrigin;
            tracedFocus = FocusDistance;
            for (int i = 0; i < beamStops.Length; i++)
            {
                float fraction = i / (float)(beamStops.Length - 1) * 2f - 1f;
                Vector2 start = BeamOrigin + Vector2.UnitX * (fraction * 32f);
                Vector2 ray = (Focus - start).SafeNormalize(Vector2.UnitY * BeamDirection);
                float stop = MagnifiedLight.Cast(start, ray, FocusDistance / Math.Abs(ray.Y), raySamples) * Math.Abs(ray.Y);
                beamStops[i] = stop;
            }
            for (int i = 0; i < incomingStops.Length; i++)
            {
                float length = 190f + i * 14f + MathF.Sin(Main.GlobalTimeWrappedHourly * .8f + i) * 26f;
                incomingStops[i] = length;
                Vector2 previous = IncomingPoint(i, 0f);
                for (float distance = 0f; distance < length; distance += 12f)
                {
                    float next = Math.Min(distance + 12f, length);
                    Vector2 point = IncomingPoint(i, next);
                    Vector2 edge = point - previous;
                    float hit = MagnifiedLight.Cast(previous, edge.SafeNormalize(-Vector2.UnitY * BeamDirection), edge.Length(), raySamples);
                    if (hit < edge.Length() - .01f)
                    {
                        incomingStops[i] = distance + (next - distance) * hit / edge.Length();
                        break;
                    }
                    previous = point;
                }
            }
        }
        private Vector2 IncomingPoint(int index, float distance)
        {
            float fraction = Math.Min(1f, distance / 250f);
            float time = Main.GlobalTimeWrappedHourly;
            float drift = MathF.Sin(time * .65f + index * 2.3f) * 30f + (index - 2) * 9f;
            float wave = MathF.Sin(time * 1.4f + distance * .018f + index) * 10f * MathF.Sin(fraction * MathHelper.Pi);
            return Projectile.Center + new Vector2((index - 2) * 13f + drift * fraction + wave, -BeamDirection * (12f + distance));
        }
        private void DrawBeam(float appear)
        {
            if (!Ready || BeamReach <= 0f) return;
            Texture2D light = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Trails/Clear/ThinnerGlowTrailClear").Value;
            Texture2D ray = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Pixel/AnotherLineGlow").Value;
            Rectangle slice = new(light.Width / 2, light.Height / 4, 1, light.Height / 2);
            float reach = BeamReach;
            float power = (1f + Math.Min(.45f, Lenses * .055f)) * appear;
            Color volume = BeamColor with { A = 110 };
            for (float distance = 0; distance < reach; distance += 2f)
            {
                float width = HalfW(distance);
                float concentration = 1f / (1f + Math.Abs(distance - FocusDistance) * .12f);
                float fade = reach < Math.Min(beamLength, FocusDistance) - .5f
                    ? Utils.GetLerpValue(reach, reach - 16f, distance, true) : 1f;
                float wave = MathF.Pow(.5f + .5f * MathF.Cos(distance * .045f - (age - ChargeTime) * .12f), 5f);
                float shimmer = .7f + wave * .6f;
                Vector2 point = BeamOrigin + new Vector2(0, BeamDirection * distance) - Main.screenPosition;
                point = new Vector2(MathF.Round(point.X), MathF.Round(point.Y));
                int run = -1;
                for (int sample = 0; sample <= beamStops.Length; sample++)
                {
                    bool visible = sample < beamStops.Length && beamStops[sample] >= distance;
                    if (visible && run < 0) run = sample;
                    if (visible || run < 0) continue;
                    float left = Math.Max(0f, (run - .5f) / (beamStops.Length - 1));
                    float right = Math.Min(1f, (sample - .5f) / (beamStops.Length - 1));
                    int top = (int)(left * slice.Height);
                    int height = Math.Max(1, (int)MathF.Ceiling(right * slice.Height) - top);
                    Rectangle span = new(slice.X, slice.Y + top, 1, height);
                    Vector2 position = point + Vector2.UnitX * (width * (left + right - 1f));
                    Main.EntitySpriteDraw(light, position, span, volume * ((.14f + concentration * .25f) * power * fade * shimmer), MathHelper.PiOver2, new Vector2(.5f, height * .5f), new Vector2(2f, width * 2f * (right - left) / height), SpriteEffects.FlipVertically);
                    run = -1;
                }
            }
            Color edgeColor = Color.Lerp(BeamColor, new Color(255, 250, 230, 0), .5f);
            for (int side = -1; side <= 1; side += 2)
            {
                float edgeStop = Math.Min(reach, beamStops[side < 0 ? 0 : beamStops.Length - 1]);
                if (edgeStop <= 0f) continue;
                Vector2 start = BeamOrigin + Vector2.UnitX * (side * 32f);
                Vector2 end = Vector2.Lerp(start, Focus, edgeStop / FocusDistance);
                DrawRay(ray, start, end, BeamColor * (.28f * power), 10f);
                DrawRay(ray, start, end, edgeColor * (.36f * power), 3f);
            }
            float stop = reach;
            for (int i = -2; i <= 2; i++)
            {
                float aperture = i * 13f;
                Vector2 start = BeamOrigin + new Vector2(aperture, 0f);
                float rayStop = Math.Min(stop, beamStops[(i + 2) * 4]);
                Vector2 end = BeamOrigin + new Vector2(aperture * (1f - rayStop / FocusDistance), BeamDirection * rayStop);
                DrawRay(ray, start, end, BeamColor * (.12f * power), 2f);
                float travel = ((age - ChargeTime) * 2.8f + (i + 2) * 31f) % FocusDistance;
                float head = Math.Min(rayStop, travel);
                float tail = Math.Max(0f, travel - 28f);
                if (head > tail)
                {
                    Vector2 from = Vector2.Lerp(start, Focus, tail / FocusDistance);
                    Vector2 to = Vector2.Lerp(start, Focus, head / FocusDistance);
                    DrawRay(ray, from, to, Color.Lerp(BeamColor, new Color(255, 245, 215, 0), .35f) * (.4f * power), 4f);
                }
            }
            for (int i = 0; i < Lenses; i++)
            {
                Vector2 start = LensPosition(i);
                float distance = (start.Y - BeamOrigin.Y) * BeamDirection;
                if (distance >= stop) continue;
                float aperture = (start.X - BeamOrigin.X) / Math.Max(1f, 32f * (1f - distance / FocusDistance));
                int sample = Math.Clamp((int)MathF.Round((aperture + 1f) * 8f), 0, beamStops.Length - 1);
                float endDistance = Math.Min(stop, beamStops[sample]);
                if (endDistance <= distance) continue;
                Vector2 end = Vector2.Lerp(start, Focus, (endDistance - distance) / (FocusDistance - distance));
                DrawRay(ray, start, end, BeamColor * (.08f * appear), 2f);
            }
            if (reach < FocusDistance - .5f) return;
            Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64").Value;
            Texture2D flare = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Pixel/CrispStarPMA").Value;
            float pulse = .94f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * .06f;
            Vector2 focus = Focus - Main.screenPosition;
            Main.EntitySpriteDraw(glow, focus, null, BeamColor * ((GroundedFocus ? .32f : .16f) * power), 0f, glow.Size() * .5f, new Vector2(22f, 26f) * pulse / glow.Size(), SpriteEffects.None);
            Main.EntitySpriteDraw(flare, focus, null, BeamColor * ((GroundedFocus ? .65f : .35f) * appear), 0f, flare.Size() * .5f, new Vector2(16f, 20f) * pulse / flare.Size(), SpriteEffects.None);
        }
        private void DrawIncomingLight(float appear)
        {
            Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64").Value;
            float charge = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, age / (float)ChargeTime));
            for (int i = 0; i < incomingStops.Length; i++)
            {
                float stop = incomingStops[i];
                float travel = (1f - (Main.GlobalTimeWrappedHourly * .65f + i * .19f) % 1f) * stop;
                for (float distance = 0f; distance < stop; distance += 4f)
                {
                    Vector2 point = IncomingPoint(i, distance);
                    Vector2 tangent = IncomingPoint(i, Math.Min(distance + 4f, stop)) - point;
                    float fade = MathHelper.SmoothStep(1f, 0f, distance / stop);
                    float pulse = MathF.Exp(-MathF.Pow((distance - travel) / 22f, 2f));
                    float width = 10f + MathF.Sin(distance * .025f + Main.GlobalTimeWrappedHourly + i) * 2f;
                    Vector2 position = point - Main.screenPosition;
                    float rotation = tangent.ToRotation() - MathHelper.PiOver2;
                    Main.EntitySpriteDraw(glow, position, null, BeamColor * ((.025f + pulse * .05f) * appear * charge * fade), rotation, glow.Size() * .5f, new Vector2(width, 16f) / glow.Size(), SpriteEffects.None);
                    Main.EntitySpriteDraw(glow, position, null, BeamColor * (pulse * .08f * appear * charge * fade), rotation, glow.Size() * .5f, new Vector2(4f, 10f) / glow.Size(), SpriteEffects.None);
                }
            }
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, BeamColor * (.12f * appear * charge),
                0f, glow.Size() * .5f, new Vector2(66f, 18f) / glow.Size(), SpriteEffects.None);
        }
        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ && lightRegistered)
                ModContent.GetInstance<PixelationSystem>().UnregisterPersistentRenderAction(RenderLayer.BeforeSolidTiles, drawLight);
        }
        private Vector2 LensPosition(int index)
        {
            int row = index / 3;
            int column = index % 3;
            int count = Math.Min(3, Lenses - row * 3);
            Vector2 offset = new((column - (count - 1) * .5f) * 18f, BeamDirection * (22f + row * 14f + (column == 1 ? 2f : 0f)));
            float unfold = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(row * 2f, 12f + row * 2f, age, true));
            return Projectile.Center + offset * unfold;
        }
        private float LensGlow(int index)
        {
            float stage = index / (float)(Lenses + 1);
            if (!Ready)
            {
                float fill = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(stage * .8f, stage * .8f + .18f, age / (float)ChargeTime, true));
                float breathe = .5f + .5f * MathF.Sin(age * .07f - index * .6f);
                return fill * (.01f + breathe * .035f);
            }
            return LensPulse(index) * .08f;
        }
        private float LensPulse(int index) => MathF.Pow(.5f + .5f * MathF.Cos(((age - ChargeTime) / 60f - index / (float)(Lenses + 1)) * MathHelper.TwoPi), 8f);
        private float ReadyFlash => Ready && (openSky || Underworld) ? MathF.Exp(-(age - ChargeTime) / 8f) * .85f : 0f;
        private void DrawLenses(Color lightColor, float appear)
        {
            Texture2D lens = TextureAssets.Projectile[Type].Value;
            Texture2D bonus = ModContent.Request<Texture2D>(Texture + "Bonus").Value;
            Texture2D bonusStick = ModContent.Request<Texture2D>(Texture + "BonusStick").Value;
            Texture2D glow = ModContent.Request<Texture2D>(Texture + "Glowy").Value;
            Texture2D mask = ModContent.Request<Texture2D>(Texture + "_Glowmask").Value;
            Texture2D bonusGlow = ModContent.Request<Texture2D>(Texture + "BonusGlowy").Value;
            Texture2D bonusMask = ModContent.Request<Texture2D>(Texture + "Bonus_Glowmask").Value;
            bool lit = openSky || Underworld;
            float flash = ReadyFlash * appear;
            float tilt = MathHelper.Clamp(-Projectile.velocity.X * .004f, -.12f, .12f);
            Color wood = lightColor.MultiplyRGB(new Color(105, 72, 40)) * appear;
            Color outline = lightColor.MultiplyRGB(new Color(38, 25, 16)) * appear;
            lensDraws.Clear();
            for (int i = 0; i < Lenses; i++)
            {
                int row = i / 3;
                int column = i % 3;
                Vector2 position = LensPosition(i);
                bool right = position.X > Projectile.Center.X;
                int side = right ? -1 : 1;
                float rotation = tilt + MathF.Sin(Main.GlobalTimeWrappedHourly * 1.5f + row * .7f) * .025f;
                Vector2 root = position - new Vector2(side * 15f, BeamDirection * 10f).RotatedBy(rotation);
                if (row > 0 && column == 0) Array.Copy(lensJoints, previousJoints, lensJoints.Length);
                Vector2 attachment = Projectile.Center + new Vector2(MathHelper.Clamp(root.X - Projectile.Center.X, -26f, 26f), BeamDirection * 6f).RotatedBy(tilt);
                if (row > 0)
                {
                    attachment = previousJoints[0];
                    for (int joint = 1; joint < previousJoints.Length; joint++)
                        if (Math.Abs(previousJoints[joint].X - root.X) < Math.Abs(attachment.X - root.X)) attachment = previousJoints[joint];
                }
                DrawSupport(attachment, root, outline, 3f);
                DrawSupport(attachment, root, wood, 1f);
                SpriteEffects effects = (right ? SpriteEffects.FlipHorizontally : SpriteEffects.None) | (Underworld ? SpriteEffects.FlipVertically : SpriteEffects.None);
                Vector2 origin = new(right ? 25f : 3f, Underworld ? 14f : 2f);
                lensDraws.Add(new DrawData(bonusStick, root - Main.screenPosition, null, lightColor * appear, rotation, origin, 1f, effects));
                lensDraws.Add(new DrawData(bonus, root - Main.screenPosition, null, lightColor * (appear * .5f), rotation, origin, 1f, effects));
                lensJoints[column] = root + new Vector2(side * 5f, BeamDirection * 6f).RotatedBy(rotation);
                if (lit)
                {
                    Color color = Color.Lerp(BeamColor, new Color(255, 245, 230, 0), (i + 1f) / (Lenses + 1) * .3f);
                    lensDraws.Add(new DrawData(bonusGlow, root - Main.screenPosition, null, color * (LensGlow(i + 1) * appear), rotation, origin + (bonusGlow.Size() - bonus.Size()) * .5f, 1f, effects));
                    lensDraws.Add(new DrawData(bonusMask, root - Main.screenPosition, null, Color.Lerp(color, new Color(255, 255, 255, 0), .6f) * flash, rotation, origin, 1f, effects));
                }
            }
            foreach (DrawData draw in lensDraws) draw.Draw(Main.spriteBatch);
            SpriteEffects mainEffects = Underworld ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Main.EntitySpriteDraw(lens, Projectile.Center - Main.screenPosition, null, lightColor * (appear * .5f), tilt, lens.Size() * .5f,
                2f, mainEffects);
            if (lit)
            {
                Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, BeamColor * (LensGlow(0) * appear), tilt, glow.Size() * .5f, 2f, mainEffects);
                Main.EntitySpriteDraw(mask, Projectile.Center - Main.screenPosition, null, Color.Lerp(BeamColor, new Color(255, 255, 255, 0), .6f) * flash, tilt, mask.Size() * .5f, 2f, mainEffects);
            }
        }
        private static void DrawSupport(Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 edge = end - start;
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, edge.ToRotation(), new Vector2(0f, .5f), new Vector2(edge.Length(), width), SpriteEffects.None);
        }
        private static void DrawRay(Texture2D ray, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 edge = end - start;
            Main.EntitySpriteDraw(ray, start - Main.screenPosition, null, color, edge.ToRotation(), new Vector2(0f, ray.Height * .5f), new Vector2(edge.Length() / ray.Width, width / ray.Height), SpriteEffects.None);
        }
    }
    public sealed class MagnifiedScorch : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flames;
        public override Terraria.Localization.LocalizedText DisplayName => ModContent.GetInstance<MagnifiedGlass>().DisplayName;
        private readonly float[] raySamples = new float[3];
        private readonly Vector2[] roots = new Vector2[5];
        private readonly bool[] grounded = new bool[5];
        private readonly bool[] platforms = new bool[5];
        private Action drawFire;
        private bool fireRegistered;
        private bool Underworld => Projectile.ai[0] < 0f;
        private Vector2 SurfaceDirection => Underworld ? -Vector2.UnitY : Vector2.UnitY;
        private Vector2 Surface => Underworld ? Projectile.Top : Projectile.Bottom;
        public override bool ShouldUpdatePosition() => false;
        public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 120;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 30;
        }

        public override void AI()
        {
            Vector2 start = Surface - SurfaceDirection * 8f;
            float ground = MagnifiedLight.Cast(start, SurfaceDirection, 20f, raySamples);
            if (!Main.dayTime && !Underworld || ground >= 20f || Collision.WetCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = start + SurfaceDirection * (ground - Projectile.height * .5f);
            if (Main.dedServ) return;
            UpdateRoots();
            Lighting.AddLight(Projectile.Center, .4f, .16f, .03f);
            if (Projectile.timeLeft % 4 == 0)
            {
                int root = Main.rand.Next(roots.Length);
                if (!grounded[root]) return;
                Dust dust = Dust.NewDustPerfect(roots[root] - SurfaceDirection * 2f, DustID.Torch, new Vector2(Main.rand.NextFloat(-.6f, .6f), Main.rand.NextFloat(-2f, -.7f) * SurfaceDirection.Y), 0, default, .9f);
                dust.noGravity = true;
            }
        }
        private void UpdateRoots()
        {
            for (int i = 0; i < roots.Length; i++)
            {
                Vector2 start = Surface + Vector2.UnitX * ((i - 2) * 4f) - SurfaceDirection * 8f;
                float ground = MagnifiedLight.Cast(start, SurfaceDirection, 20f, raySamples);
                grounded[i] = ground < 20f;
                roots[i] = start + SurfaceDirection * ground;
                Tile tile = Main.tile[(roots[i] + SurfaceDirection * .5f).ToTileCoordinates()];
                platforms[i] = grounded[i] && tile.HasUnactuatedTile && Main.tileSolidTop[tile.TileType];
            }
        }
        public override bool? CanDamage() => (Main.dayTime || Underworld) && Projectile.timeLeft > 15 ? null : false;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire, 120);
        public override bool PreDraw(ref Color lightColor)
        {
            if (!fireRegistered)
            {
                drawFire ??= DrawFire;
                fireRegistered = ModContent.GetInstance<PixelationSystem>().RegisterPersistentRenderAction(RenderLayer.BeforeSolidTiles, () => Projectile.active, drawFire);
            }
            return false;
        }
        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ && fireRegistered)
                ModContent.GetInstance<PixelationSystem>().UnregisterPersistentRenderAction(RenderLayer.BeforeSolidTiles, drawFire);
        }
        private Vector2 FlamePosition(int index) => roots[index] + SurfaceDirection * (platforms[index] ? 0f : 6f);
        private float FlameHeight(int index)
        {
            bool platform = platforms[index];
            float height = platform ? 14f : 30f;
            if (index == 2) height += platform ? 4f : 6f;
            return height + MathF.Sin(Main.GlobalTimeWrappedHourly * 8f + Projectile.identity * .7f + index * 1.4f) * (platform ? 3f : 4f);
        }
        private void DrawFire()
        {
            float fade = Math.Min(1f, Projectile.timeLeft / 20f);
            Color outer = Underworld ? new Color(255, 65, 10, 0) : new Color(255, 100, 15, 0);
            Color core = Underworld ? new Color(255, 160, 45, 0) : new Color(255, 230, 120, 0);
            float rotation = Underworld ? MathHelper.Pi : 0f;
            Texture2D flame = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Trails/FlamesTextureButBlack").Value;
            Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64").Value;
            for (int i = 0; i < roots.Length; i++)
            {
                if (!grounded[i]) continue;
                float height = FlameHeight(i);
                int frame = ((int)(Main.GlobalTimeWrappedHourly * 18f) + Projectile.identity + i * 2) % 8;
                Rectangle source = new(frame * (flame.Width / 8), 0, flame.Width / 8, flame.Height / 2);
                Vector2 origin = new(source.Width * .5f, source.Height);
                Vector2 position = FlamePosition(i) - Main.screenPosition;
                Main.EntitySpriteDraw(glow, position - SurfaceDirection * 4f, null, new Color(255, 110, 20, 0) * (.2f * fade), 0f, glow.Size() * .5f, new Vector2(16f, 18f) / glow.Size(), SpriteEffects.None);
                Main.EntitySpriteDraw(flame, position, source, outer * (.8f * fade), rotation, origin, new Vector2(8f, height) / source.Size(), SpriteEffects.None);
                Main.EntitySpriteDraw(flame, position, source, core * fade, rotation, origin, new Vector2(4f, height * .6f) / source.Size(), SpriteEffects.None);
            }
        }
    }
    internal static class MagnifiedLight
    {
        internal static float Cast(Vector2 start, Vector2 direction, float range, float[] samples = null)
        {
            samples ??= new float[3];
            float edgeX = direction.X > 0f ? (Main.maxTilesX * 16f - 16f - start.X) / direction.X
                : direction.X < 0f ? (16f - start.X) / direction.X : float.PositiveInfinity;
            float edgeY = direction.Y > 0f ? (Main.maxTilesY * 16f - 16f - start.Y) / direction.Y
                : direction.Y < 0f ? (16f - start.Y) / direction.Y : float.PositiveInfinity;
            range = Math.Min(range, Math.Max(0f, Math.Min(edgeX, edgeY)));
            float travelled = 0f;
            while (travelled < range)
            {
                Collision.LaserScan(start + direction * travelled, direction, 0f, range - travelled, samples);
                float hit = Math.Min(samples[0], Math.Min(samples[1], samples[2]));
                float from = Math.Max(travelled, travelled + hit - 32f);
                float to = Math.Min(range, travelled + hit + 24f);
                for (float distance = from; distance <= to; distance += 1f)
                {
                    if (!Collision.IsWorldPointSolid(start + direction * distance)) continue;
                    float low = Math.Max(from, distance - 1f);
                    float high = distance;
                    for (int step = 0; step < 8; step++)
                    {
                        float middle = (low + high) * .5f;
                        if (Collision.IsWorldPointSolid(start + direction * middle)) high = middle;
                        else low = middle;
                    }
                    return high;
                }
                travelled = to;
            }
            return range;
        }
    }
}
