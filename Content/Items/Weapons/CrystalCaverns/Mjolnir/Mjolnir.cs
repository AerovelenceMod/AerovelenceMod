using System;
using System.IO;
using AerovelenceMod.Common.Systems;
using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Dusts.GlowDusts;
using AerovelenceMod.Content.Items.Weapons.CrystalCaverns;
using AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler;
using AerovelenceMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Weapons.CrystalCaverns.Mjolnir;

public class Mjolnir : TranslatableModItem
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/CrystalCaverns/Mjolnir/Mjolnir";

     public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Mjolnir", "\"Meow-Meow?\"");
	}

    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 44;
        Item.damage = 88;
        Item.DamageType = DamageClass.Melee;
        Item.knockBack = 7f;
        Item.useTime = Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = true;
        Item.rare = ItemRarityID.Yellow;
        Item.value = Item.sellPrice(gold: 12);
    }

    public override bool AltFunctionUse(Player player) => true;
    public override bool CanUseItem(Player player) => false;

    public override void HoldItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer && !player.dead && !player.CCed && !player.noItems && player.ownedProjectileCounts[ModContent.ProjectileType<MjolnirHammer>()] == 0)
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<MjolnirHammer>(), player.GetWeaponDamage(Item), Item.knockBack, player.whoAmI);
    }

}

public class MjolnirPlayer : ModPlayer
{
    public int FlightCooldown;
    internal MjolnirHammer Hammer;
    private bool HoldingHammer => Hammer != null && Hammer.Projectile.active && Hammer.Projectile.type == ModContent.ProjectileType<MjolnirHammer>() && ReferenceEquals(Hammer.Projectile.ModProjectile, Hammer) && Player.HeldItem.type == ModContent.ItemType<Mjolnir>() && !Player.dead && !Player.CCed && !Player.noItems;

    public override void ResetEffects() => Hammer = null;
    public override void PostUpdate() => FlightCooldown = Math.Max(0, FlightCooldown - 1);
    public override void UpdateDead() { FlightCooldown = 0; Hammer = null; }

    public override void FrameEffects()
    {
        if (HoldingHammer) Hammer.PosePlayer();
    }

    public override void TransformDrawData(ref PlayerDrawSet drawInfo)
    {
        if (!HoldingHammer || !Hammer.IsSweeping || Player.mount.Active) return;
        float progress = Hammer.SweepProgress;
        float turn = MjolnirEffects.Ease(progress) * MathHelper.TwoPi;
        float envelope = MathF.Sin(progress * MathHelper.Pi);
        Vector2 scale = new(MathHelper.Lerp(1, .24f + .76f * MathF.Abs(MathF.Cos(turn)), Math.Min(1, envelope * 4)), 1 - envelope * .18f);
        Vector2 pivot = drawInfo.Position + new Vector2(Player.width * .5f, Player.height) - Main.screenPosition;
        float lean = MathF.Sin(turn) * .12f * Hammer.SweepDirection;
        for (int i = 0; i < drawInfo.DrawDataCache.Count; i++)
        {
            DrawData data = drawInfo.DrawDataCache[i];
            data.position = pivot + ((data.position - pivot) * scale).RotatedBy(lean);
            data.scale *= scale;
            data.rotation += lean;
            drawInfo.DrawDataCache[i] = data;
        }
    }
}

public class MjolnirHeldHammerLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.ArmOverItem);

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        MjolnirHammer hammer = player.GetModPlayer<MjolnirPlayer>().Hammer;
        if (drawInfo.shadow != 0f || player.dead || player.invis || player.HeldItem.type != ModContent.ItemType<Mjolnir>() || hammer == null || !hammer.Projectile.active || hammer.Projectile.type != ModContent.ProjectileType<MjolnirHammer>() || !ReferenceEquals(hammer.Projectile.ModProjectile, hammer) || !hammer.DrawHeldLayer)
            return;
        hammer.DrawHeld(ref drawInfo);
    }
}

public class MjolnirChannelSystem : ModSystem
{
    private const string SkyKey = "AerovelenceMod:MjolnirChannel";
    private MjolnirChannelSky sky;

    public override void Load()
    {
        if (Main.dedServ) return;
        sky = new MjolnirChannelSky();
        SkyManager.Instance[SkyKey] = sky;
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ || sky == null) return;
        Player player = Main.LocalPlayer;
        MjolnirHammer hammer = player.GetModPlayer<MjolnirPlayer>().Hammer;
        sky.Charge = !Main.gameMenu && player.active && !player.dead && player.HeldItem.type == ModContent.ItemType<Mjolnir>() &&
            hammer != null && hammer.Projectile.active ? hammer.SkyCharge : 0;
        if (sky.Charge > 0)
            SkyManager.Instance.Activate(SkyKey, player.Center);
        else
            SkyManager.Instance.Deactivate(SkyKey);
    }

    public override void OnWorldUnload()
    {
        if (!Main.dedServ) SkyManager.Instance.Deactivate(SkyKey);
        sky?.Reset();
    }

    public override void Unload()
    {
        if (!Main.dedServ) SkyManager.Instance.Deactivate(SkyKey);
        sky?.Reset();
        sky = null;
    }
}

public class MjolnirChannelSky : CustomSky
{
    internal float Charge;
    private bool active;
    private float opacity;

    public override void Activate(Vector2 position, params object[] args) => active = true;
    public override void Deactivate(params object[] args) => active = false;
    public override bool IsActive() => active || opacity > 0;
    public override void Reset() { active = false; opacity = Charge = 0; }
    public override float GetCloudAlpha() => 1 - opacity * .65f;

    public override void Update(GameTime gameTime)
    {
        float target = active && !Main.gameMenu ? Charge : 0;
        opacity += MathHelper.Clamp(target - opacity, -.025f, .04f);
        if (opacity < .001f) opacity = 0;
    }

    public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
    {
        if (opacity <= 0 || minDepth >= 0 || maxDepth < 0) return;
        Texture2D gradient = ModContent.Request<Texture2D>("AerovelenceMod/Assets/BlackGradient").Value;
        spriteBatch.Draw(gradient, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), null,
            Color.White * opacity * .85f, 0, Vector2.Zero, SpriteEffects.FlipVertically, 0);
    }
}

public class MjolnirHammer : ModProjectile
{
    private enum HammerState { Ready, Spinning, Throwing, Thrown, Lodged, Recalling, Catching, Sweeping, Charging, Flying, Recovering, RightWindup, Slamming, GroundImpact }

    private const int SpinThreshold = 14;
    private const int ChargeTime = 60;
    private const int SweepTime = 40;
    private const int RightHoldThreshold = 14;
    private const int SlamTime = 36;
    private static readonly Vector2 GripOffset = new(0, 18);

    private HammerState State => (HammerState)(int)Projectile.ai[0];
    private ref float Timer => ref Projectile.ai[1];
    private bool IsOwner => Projectile.owner == Main.myPlayer;
    private Player Owner => Main.player[Projectile.owner];
    private Vector2 aim = Vector2.UnitX;
    private Vector2 poseStartOffset;
    private float poseStartRotation;
    private float poseStartArm;
    private float armAngle;
    private float spinVisual;
    private float spinPhase;
    private float sweepStartAngle;
    private float sweepStartRadius;
    private int sweepDirection = 1;
    private Vector2 previousCenter;
    private Vector2 previousHead;
    private int bufferedInput;
    private int bufferTime;
    private bool initialized;
    private bool leftWasDown;
    private bool rightWasDown;
    private bool empowered;
    private bool empoweredAttack;
    private bool recallHeld;
    private int rightHeldTicks;
    private int pulseCooldown;
    private int pulseDelay;
    private bool pulseEmpowered;
    private int reflectedCooldown;
    private readonly LightningUtils.LightningData tether = MjolnirEffects.Lightning(24, 3f);
    private readonly LightningUtils.LightningData skyLeader = MjolnirEffects.Lightning(40, 1.5f);
    private readonly LightningStrokeRenderer tetherRenderer = new();
    private readonly LightningStrokeRenderer skyRenderer = new();

    internal bool IsSweeping => State == HammerState.Sweeping;
    internal bool DrawHeldLayer => State is HammerState.Ready or HammerState.Recovering || State == HammerState.Spinning && Timer <= 10;
    internal float SweepProgress => MathHelper.Clamp(Timer / SweepTime, 0, 1);
    internal int SweepDirection => sweepDirection;
    internal Vector2 Head => Projectile.Center - new Vector2(0, 12).RotatedBy(Projectile.rotation);
    internal bool ReceivingCharge => State == HammerState.Charging && Timer >= ChargeTime;
    internal float SkyCharge => State == HammerState.Charging ? MjolnirEffects.Ease(Timer / ChargeTime) : 0;
    private bool Powered => empowered || empoweredAttack || pulseEmpowered;
    private bool InHand => State is HammerState.Ready or HammerState.Spinning or HammerState.Throwing or HammerState.Charging or HammerState.Catching or HammerState.Flying or HammerState.Sweeping or HammerState.Recovering or HammerState.RightWindup or HammerState.Slamming or HammerState.GroundImpact;
	
    private float HeldRotation(float arm)
    {
        float localArm = MathF.Atan2(MathF.Sin(arm), MathF.Abs(MathF.Cos(arm)));
        return Owner.direction * (localArm * .85f + .10f);
    }
	
    private float SpinAmount => State == HammerState.Spinning ? MjolnirEffects.Ease((Timer - 6) / 16f) : 0;

    public override string Texture => "AerovelenceMod/Content/Items/Weapons/CrystalCaverns/Mjolnir/Mjolnir";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 14;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 30;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.hide = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 24;
    }

    public override bool ShouldUpdatePosition() => State == HammerState.Thrown || State == HammerState.Recalling;

    public override void OnKill(int timeLeft)
    {
        tetherRenderer.Dispose();
        skyRenderer.Dispose();
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(aim);
        writer.WriteVector2(poseStartOffset);
        writer.Write(poseStartRotation);
        writer.Write(poseStartArm);
        writer.Write(sweepStartAngle);
        writer.Write(sweepStartRadius);
        writer.Write((sbyte)sweepDirection);
        writer.Write(empowered);
        writer.Write(empoweredAttack);
        writer.Write(recallHeld);
        writer.Write((short)pulseDelay);
        writer.Write(pulseEmpowered);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        aim = reader.ReadVector2();
        poseStartOffset = reader.ReadVector2();
        poseStartRotation = reader.ReadSingle();
        poseStartArm = reader.ReadSingle();
        sweepStartAngle = reader.ReadSingle();
        sweepStartRadius = reader.ReadSingle();
        sweepDirection = reader.ReadSByte();
        empowered = reader.ReadBoolean();
        empoweredAttack = reader.ReadBoolean();
        recallHeld = reader.ReadBoolean();
        pulseDelay = reader.ReadInt16();
        pulseEmpowered = reader.ReadBoolean();
    }

    private void ChangeState(HammerState state)
    {
        poseStartOffset = Projectile.Center - Owner.MountedCenter;
        poseStartRotation = Projectile.rotation;
        poseStartArm = armAngle;
        if (state == HammerState.Slamming) sweepDirection = Owner.direction;
        if (state == HammerState.Sweeping)
        {
            Vector2 ellipseOffset = poseStartOffset / new Vector2(1, .36f);
            sweepStartAngle = ellipseOffset.ToRotation();
            sweepStartRadius = Math.Max(12, ellipseOffset.Length());
            sweepDirection = Owner.direction;
        }
        Projectile.ai[0] = (float)state;
        Timer = 0;
        if (state != HammerState.Recalling) Projectile.velocity = Vector2.Zero;
        Projectile.tileCollide = state == HammerState.Thrown;
        Array.Clear(Projectile.localNPCImmunity);
        Projectile.netUpdate = true;
    }

    private void HeldPose(float targetArm, float targetRotation, float duration = 10)
    {
        float blend = MjolnirEffects.Ease(Timer / duration);
        armAngle = poseStartArm.AngleLerp(targetArm, blend);
        Projectile.rotation = poseStartRotation.AngleLerp(targetRotation, blend);
        Vector2 hand = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armAngle - MathHelper.PiOver2);
        Vector2 center = hand - GripOffset.RotatedBy(Projectile.rotation);
        Projectile.Center = Vector2.Lerp(Owner.MountedCenter + poseStartOffset, center, blend);
    }

    internal void DrawHeld(ref PlayerDrawSet drawInfo)
    {
        float power = empowered || empoweredAttack ? 1f : .15f;
        float transition = State == HammerState.Spinning ? 1f - MjolnirEffects.Ease(Timer / 10f) : 1f;
        Vector2 center = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
        float scale = MathHelper.Lerp(.82f, 1f, transition);
        MjolnirEffects.AddHeldHammer(ref drawInfo, center, Projectile.rotation, scale, power, Color.White * transition, Owner.direction, Powered);
    }

    internal void PosePlayer()
    {
        if (!InHand && State != HammerState.Recalling) return;
        Player player = Owner;
        float frontArm = IsSweeping ? (player.direction > 0 ? -.15f : MathHelper.Pi + .15f) : armAngle;
        player.SetCompositeArmFront(true, IsSweeping ? Player.CompositeArmStretchAmount.ThreeQuarters : Player.CompositeArmStretchAmount.Full,
            frontArm - MathHelper.PiOver2);
        if (IsSweeping)
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, -player.direction * .8f);
        else if (State is HammerState.Slamming or HammerState.GroundImpact)
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, frontArm - MathHelper.PiOver2 + sweepDirection * .2f);
    }

    private void ReadyInput()
    {
        if (!IsOwner || bufferTime <= 0) return;
        empoweredAttack = false;
        if (bufferedInput == 1) ChangeState(HammerState.Spinning);
        else if (bufferedInput == 2) ChangeState(HammerState.RightWindup);
        bufferedInput = bufferTime = 0;
    }

    private Vector2 SweepPosition(float time)
    {
        float progress = MathHelper.Clamp(time / SweepTime, 0, 1);
        float angle = sweepStartAngle + MjolnirEffects.Ease(progress) * MathHelper.TwoPi * sweepDirection;
        float reach = MjolnirEffects.Ease(time / 9f) * MjolnirEffects.Ease((SweepTime - time) / 10f);
        float radius = MathHelper.Lerp(sweepStartRadius, empoweredAttack ? 84 : 56, reach);
        return Owner.MountedCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .36f) * radius;
    }

    private void BeginAttack()
    {
        empoweredAttack = empowered;
        empowered = false;
        Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
        Projectile.CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
        Projectile.netUpdate = true;
    }

    public override void AI()
    {
        Player player = Owner;
        if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<Mjolnir>() || player.CCed || player.noItems)
        {
            if (State == HammerState.Flying && IsOwner) player.velocity *= .2f;
            Projectile.Kill();
            return;
        }

        Projectile.timeLeft = 2;
        Projectile.tileCollide = State == HammerState.Thrown;
        player.GetModPlayer<MjolnirPlayer>().Hammer = this;
        if (!initialized)
        {
            initialized = true;
            armAngle = aim.ToRotation();
            if (IsOwner)
            {
                poseStartArm = armAngle;
                poseStartOffset = new Vector2(player.direction * 12, 8);
            }
        }
        previousCenter = Projectile.Center;
        previousHead = Head;
        Timer++;
        pulseCooldown = Math.Max(0, pulseCooldown - 1);
        reflectedCooldown = Math.Max(0, reflectedCooldown - 1);
        bool inputAllowed = !Main.playerInventory && !player.mouseInterface && !Main.blockMouse && !Main.mapFullscreen && player.talkNPC < 0 && player.sign < 0;
        bool left = IsOwner && inputAllowed && Main.mouseLeft;
        bool right = IsOwner && inputAllowed && Main.mouseRight;

        bufferTime = Math.Max(0, bufferTime - 1);
        if (IsOwner && (left && !leftWasDown || right && !rightWasDown))
        {
            bufferedInput = right ? 2 : 1;
            bufferTime = 14;
        }

        if (IsOwner && !inputAllowed && State is HammerState.Spinning or HammerState.Charging or HammerState.RightWindup)
        {
            bufferedInput = bufferTime = 0;
            ChangeState(HammerState.Recovering);
        }

        if (IsOwner)
        {
            if (State is not (HammerState.Flying or HammerState.Sweeping or HammerState.Throwing or HammerState.Slamming or HammerState.GroundImpact))
            {
                Vector2 nextAim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0));
                if (Vector2.DistanceSquared(aim, nextAim) > .002f && Main.GameUpdateCount % 4 == 0)
                    Projectile.netUpdate = true;
                aim = nextAim;
            }
            rightHeldTicks = right ? rightHeldTicks + 1 : 0;
            if (recallHeld && !right)
            {
                recallHeld = false;
                Projectile.netUpdate = true;
            }
        }

        if (InHand)
        {
            if (!IsSweeping) player.ChangeDir(aim.X >= 0 ? 1 : -1);
            if (State == HammerState.Spinning) player.heldProj = Projectile.whoAmI;
            if (State is not (HammerState.Ready or HammerState.Recovering))
            {
                player.itemTime = player.itemAnimation = 2;
            }
        }

        switch (State)
        {
            case HammerState.Ready:
                float readyArm = aim.ToRotation();
                HeldPose(readyArm, HeldRotation(readyArm));
                empoweredAttack = false;
                ReadyInput();
                break;

            case HammerState.Recovering:
                float recoverArm = aim.ToRotation();
                HeldPose(recoverArm, HeldRotation(recoverArm), 12);
                if (Timer >= 8) ReadyInput();
                if (IsOwner && State == HammerState.Recovering && Timer >= 12) ChangeState(HammerState.Ready);
                break;

            case HammerState.Spinning:
                HeldPose(aim.ToRotation(), poseStartRotation - player.direction * .7f, 12);
                Vector2 spinHand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armAngle - MathHelper.PiOver2);
                Projectile.Center = Vector2.Lerp(Projectile.Center, spinHand + aim * 10, SpinAmount);
                if (Timer >= SpinThreshold) Deflect();
                if (Timer % 16 == 0) SoundEngine.PlaySound(SoundID.Item1 with { Volume = .32f, Pitch = .45f + SpinAmount * .25f }, Projectile.Center);
                if (Timer >= SpinThreshold && Timer % 3 == 0)
                {
                    Vector2 rim = new Vector2(MathF.Cos(Timer * .6f) * 12, MathF.Sin(Timer * .6f) * 38).RotatedBy(aim.ToRotation());
                    MjolnirEffects.Spark(Projectile.Center + rim, aim * Main.rand.NextFloat(2, 5), Powered);
                }
                if (IsOwner && !left)
                {
                    if (Timer < SpinThreshold)
                    {
                        BeginAttack();
                        ChangeState(HammerState.Throwing);
                    }
                    else if (player.GetModPlayer<MjolnirPlayer>().FlightCooldown == 0 && !player.mount.Active && player.grapCount == 0)
                    {
                        BeginAttack();
                        ChangeState(HammerState.Flying);
                        player.GetModPlayer<MjolnirPlayer>().FlightCooldown = 150;
                        MjolnirEffects.Burst(Projectile.Center, 16, Powered);
                    }
                    else ChangeState(HammerState.Recovering);
                }
                break;

            case HammerState.RightWindup:
                float windupAngle = FaultbreakerWeaponMotion.HammerAngle(.1f, player.direction);
                HeldPose(windupAngle, windupAngle + MathHelper.PiOver2, RightHoldThreshold);
                if (IsOwner && !right)
                {
                    BeginAttack();
                    ChangeState(HammerState.Slamming);
                }
                else if (IsOwner && Timer >= RightHoldThreshold)
                {
                    if (empowered)
                    {
                        BeginAttack();
                        ChangeState(HammerState.Sweeping);
                    }
                    else ChangeState(HammerState.Charging);
                }
				// #empower #society #burgerking
                break;

            case HammerState.Slamming:
                player.ChangeDir(sweepDirection);
                float slamProgress = Timer / SlamTime;
                float slamAngle = FaultbreakerWeaponMotion.HammerAngle(slamProgress, sweepDirection);
                HeldPose(slamAngle, slamAngle + MathHelper.PiOver2, 7);
                if (Timer == 12) SoundEngine.PlaySound(SoundID.Item1 with { Volume = .8f, Pitch = -.5f }, Head);
                if (slamProgress >= .38f && slamProgress <= .74f)
                {
                    if (Timer % 2 == 0) MjolnirEffects.Spark(Head, (Head - previousHead) * .2f, Powered);
                    if (IsOwner)
                        for (int sample = 1; sample <= 6; sample++)
                        {
                            Vector2 point = Vector2.Lerp(previousHead, Head, sample / 6f);
                            if ((point.X - player.Center.X) * sweepDirection < 6 || !MjolnirGroundPulse.TrySurface(point.X, point.Y, out Vector2 ground) || Math.Abs(ground.Y - point.Y) > 12) continue;
                            Projectile.Center += ground - Head - new Vector2(0, 6);
                            SlamGround(ground);
                            break;
                        }
                }
                if (IsOwner && State == HammerState.Slamming && Timer >= SlamTime) ChangeState(HammerState.Recovering);
                break;

            case HammerState.GroundImpact:
                player.ChangeDir(sweepDirection);
                Projectile.Center = player.MountedCenter + poseStartOffset;
                Projectile.rotation = poseStartRotation;
                armAngle = poseStartArm;
                if (Timer == 1)
                {
                    MjolnirEffects.Burst(Head, empoweredAttack ? 32 : 20, Powered);
                    MjolnirEffects.Shake(Head, empoweredAttack ? 6 : 4);
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = .65f, Pitch = -.5f }, Head);
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = .6f, Pitch = -.4f }, Head);
                }
                if (IsOwner && Timer >= 8) ChangeState(HammerState.Recovering);
                break;

            case HammerState.Throwing:
                HeldPose(aim.ToRotation() + player.direction * .12f, aim.ToRotation() + MathHelper.PiOver2, 6);
                if (IsOwner && Timer >= 6)
                {
                    ChangeState(HammerState.Thrown);
                    Projectile.velocity = aim * 18f - Vector2.UnitY * 2.5f;
                    SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -.25f }, Projectile.Center);
                }
                break;

            case HammerState.Thrown:
                Projectile.velocity.X *= .995f;
				//Projectile.velocity.X *= .395f;
                Projectile.velocity.Y = Math.Min(18, Projectile.velocity.Y + .32f);
                Projectile.rotation += .38f * Math.Sign(Projectile.velocity.X == 0 ? 1 : Projectile.velocity.X);
                AirCommands(left, right);
                break;

            case HammerState.Lodged:
                Projectile.velocity = Vector2.Zero;
                AirCommands(left, right);
                break;

            case HammerState.Recalling:
                Projectile.tileCollide = false;
                armAngle = armAngle.AngleLerp((Projectile.Center - player.MountedCenter).ToRotation(), .24f);
                Vector2 catchHand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armAngle - MathHelper.PiOver2);
                Vector2 catchCenter = catchHand - GripOffset.RotatedBy(-player.direction * .15f);
                Vector2 toPlayer = catchCenter - Projectile.Center;
                float recallSpeed = Math.Min(34, 8 + Timer * 1.25f);
                Vector2 returnVelocity = toPlayer.SafeNormalize(Vector2.UnitX) * Math.Min(toPlayer.Length(), recallSpeed);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, returnVelocity, .26f + .5f * (1 - Math.Min(1, toPlayer.Length() / 110)));
                float settle = 1 - Math.Min(1, toPlayer.Length() / 140);
                Projectile.rotation = (Projectile.rotation + .48f * player.direction).AngleLerp(-player.direction * .15f, settle * .55f);
                if (IsOwner && toPlayer.LengthSquared() < 18 * 18) ChangeState(HammerState.Catching);
                break;

            case HammerState.Catching:
                HeldPose(aim.ToRotation() - player.direction * .24f, -player.direction * .35f, 8);
                if (Timer == 1)
                {
                    MjolnirEffects.Burst(Head, 16, Powered);
                    MjolnirEffects.Shake(Head, 2.4f);
                    SoundEngine.PlaySound(SoundID.Item37 with { Volume = .6f, Pitch = -.3f }, Head);
                }
                if (IsOwner && Timer >= 8)
                {
                    if (recallHeld && rightHeldTicks >= 18)
                    {
                        ChangeState(HammerState.Sweeping);
                    }
                    else if (!recallHeld) ChangeState(HammerState.Recovering);
                }
                break;

            case HammerState.Sweeping:
                if (Timer == 1)
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = empoweredAttack ? 1f : .7f, Pitch = empoweredAttack ? -.65f : -.35f }, Projectile.Center);
                float turn = MjolnirEffects.Ease(SweepProgress) * MathHelper.TwoPi;
                player.ChangeDir(MathF.Cos(turn) >= 0 ? sweepDirection : -sweepDirection);
                Projectile.Center = SweepPosition(Timer);
                Projectile.rotation = poseStartRotation + turn * sweepDirection;
                if (Timer % 3 == 0) MjolnirEffects.Spark(Projectile.Center, -Projectile.rotation.ToRotationVector2() * 3f, Powered);
                if (empoweredAttack && Timer == 28)
                {
                    MjolnirEffects.Burst(Head, 32, true);
                    MjolnirEffects.Shake(Owner.Center, 8);
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = .9f, Pitch = -.55f }, Head);
                    if (IsOwner)
                    {
                        ChainFromHammer(4, 1.2f);
                        if (MjolnirGroundPulse.TrySurface(Owner.Center.X, Owner.Bottom.Y, out Vector2 sweepGround))
                        {
                            Bolt(Head, sweepGround - Vector2.UnitY * 4, .8f, true);
                            ReleaseGroundElectricity(sweepGround);
                        }
                    }
                }
                if (IsOwner && Timer >= SweepTime) ChangeState(HammerState.Recovering);
                break;

            case HammerState.Charging:
                HeldPose(-MathHelper.PiOver2 + player.direction * .15f, -.08f * player.direction, 20);
                if (!Main.dedServ && Timer < ChargeTime && Timer % 3 == 0)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(65, 90);
                    MjolnirEffects.Spark(Head + offset, -offset * .09f, true);
                    if (Timer >= 30)
                        MjolnirEffects.UpdateLightning(skyLeader, Head - new Vector2(24 * MathF.Sin(Timer * .12f), 650), Head);
                }
                if (Timer == 24 || Timer == 44)
                    SoundEngine.PlaySound(SoundID.Item93 with { Volume = .4f, Pitch = Timer / 80f }, Head);
                if (IsOwner && Timer < ChargeTime && !right) ChangeState(HammerState.Recovering);
                else if (IsOwner && Timer == ChargeTime)
                {
                    if (SpendMana(18))
                    {
                        empowered = true;
                        Bolt(Head - new Vector2(player.direction * 100, 1200), Head, 0f, true, true);
                        Projectile.netUpdate = true;
                    }
                    else ChangeState(HammerState.Recovering);
                }
                if (IsOwner && State == HammerState.Charging && Timer >= ChargeTime + 22) ChangeState(HammerState.Recovering);
                break;

            case HammerState.Flying:
                if (IsOwner && inputAllowed && Vector2.DistanceSquared(Main.MouseWorld, player.Center) > 64 * 64)
                {
                    aim = MjolnirEffects.CurveFlight(aim, Main.MouseWorld - player.Center);
                    if (Timer % 3 == 0) Projectile.netUpdate = true;
                }
                HeldPose(aim.ToRotation(), aim.ToRotation() + MathHelper.PiOver2, 6);
                if (IsOwner)
                {
                    Vector2 velocity = aim * (empoweredAttack ? 25f : 21f) * MathHelper.Lerp(.35f, 1, MjolnirEffects.Ease(Timer / 5f));
                    Vector2 allowed = Collision.TileCollision(player.position, velocity, player.width, player.height, false, false, (int)player.gravDir);
                    if (Timer >= 26 || Vector2.DistanceSquared(allowed, velocity) > .01f || player.mount.Active || player.grapCount > 0)
                        StopFlight();
                    else
                    {
                        player.velocity = velocity;
                        player.fallStart = (int)(player.position.Y / 16f);
                    }
                }
                if (Timer % 2 == 0) MjolnirEffects.Spark(player.Center + Main.rand.NextVector2Circular(12, 18), -aim * 4, Powered);
                break;
        }

        spinVisual = MathHelper.Lerp(spinVisual, SpinAmount, .28f);
        if (spinVisual < .001f) spinVisual = 0;
        spinPhase += (.25f + spinVisual * .48f) * (aim.X >= 0 ? 1 : -1);
        PosePlayer();
        if (pulseDelay > 0 && --pulseDelay == 0 && IsOwner)
        {
            Bolt(player.MountedCenter, Head, pulseEmpowered ? 1.65f : 1f, pulseEmpowered);
            ChainFromHammer(pulseEmpowered ? 4 : 2, pulseEmpowered ? 1f : .65f);
            pulseEmpowered = false;
            Projectile.netUpdate = true;
        }
        if (IsOwner && Vector2.DistanceSquared(player.Center, Projectile.Center) > 1100 * 1100 && State is HammerState.Thrown or HammerState.Lodged)
            Recall(false);

        if (!Main.dedServ)
        {
            Lighting.AddLight(Projectile.Center, MjolnirEffects.EnergyColor(Powered).ToVector3() * .8f);
            if ((empowered || empoweredAttack) && Main.GameUpdateCount % 4 == 0)
                MjolnirEffects.Spark(Projectile.Center + Main.rand.NextVector2Circular(18, 18), Main.rand.NextVector2Circular(1.5f, 1.5f), Powered);
            if ((pulseDelay > 0 || State == HammerState.Recalling || State == HammerState.Flying) && Main.GameUpdateCount % 3 == 0)
                MjolnirEffects.UpdateLightning(tether, player.MountedCenter, Head);
        }
        leftWasDown = left;
        rightWasDown = right;
    }

    private bool SpendMana(int amount)
    {
        if (!Owner.CheckMana(Owner.HeldItem, Math.Max(1, (int)(amount * Owner.manaCost)), true)) return false;
        Owner.manaRegenDelay = (int)Owner.maxRegenDelay;
        return true;
    }

    private void AirCommands(bool left, bool right)
    {
        if (!IsOwner) return;
        if (right && !rightWasDown)
        {
            bufferedInput = bufferTime = 0;
            Recall(true);
        }
        else if (left && !leftWasDown && pulseCooldown == 0 && SpendMana(8))
        {
            bufferedInput = bufferTime = 0;
            pulseDelay = 12;
            pulseCooldown = 28;
            pulseEmpowered = empoweredAttack;
            empoweredAttack = false;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = .4f, Pitch = .65f }, Projectile.Center);
        }
    }

    private void Recall(bool held)
    {
        recallHeld = held;
        if (pulseDelay > 0) empoweredAttack |= pulseEmpowered;
        pulseDelay = 0;
        pulseEmpowered = false;
        ChangeState(HammerState.Recalling);
        Bolt(Owner.MountedCenter, Head, empoweredAttack ? 1.1f : .55f, empoweredAttack);
        MjolnirEffects.Burst(Head, 18, Powered);
    }

    private void StopFlight()
    {
        Owner.velocity *= .15f;
        Owner.fallStart = (int)(Owner.position.Y / 16f);
        MjolnirEffects.Burst(Projectile.Center, 22, Powered);
        MjolnirEffects.Shake(Projectile.Center, empoweredAttack ? 7 : 4);
        ChainFromHammer(empoweredAttack ? 4 : 2, empoweredAttack ? 1.05f : .7f);
        ChangeState(HammerState.Recovering);
    }

    private void SlamGround(Vector2 ground)
    {
        ChangeState(HammerState.GroundImpact);
        ReleaseGroundElectricity(ground);
    }

    private void ReleaseGroundElectricity(Vector2 ground)
    {
        int damage = (int)Owner.GetTotalDamage(DamageClass.Magic).ApplyTo(empoweredAttack ? 100 : 64);
        for (int direction = -1; direction <= 1; direction += 2)
        {
            int index = Projectile.NewProjectile(Projectile.GetSource_FromAI(), ground, Vector2.Zero, ModContent.ProjectileType<MjolnirGroundPulse>(), damage, 5, Projectile.owner, direction, empoweredAttack ? 1 : 0);
            if (index < Main.maxProjectiles) Main.projectile[index].CritChance = (int)Owner.GetTotalCritChance(DamageClass.Magic);
        }
    }

    private void Deflect()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || reflectedCooldown > 0) return;
        foreach (Projectile hostile in Main.ActiveProjectiles)
        {
            if (!hostile.hostile || hostile.friendly || hostile.damage <= 0 || hostile.velocity.LengthSquared() < 1 || hostile.width > 48 || hostile.height > 48 || hostile.penetrate != 1 || !hostile.tileCollide || Vector2.DistanceSquared(hostile.Center, Projectile.Center) > 62 * 62) continue;
            Vector2 origin = hostile.Center;
            Vector2 reflectedVelocity = (hostile.Center - Owner.Center).SafeNormalize(aim) * Math.Clamp(hostile.velocity.Length(), 10, 22);
            int reflectedDamage = Math.Min(hostile.damage, Projectile.damage);
            hostile.Kill();
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), origin, reflectedVelocity, ModContent.ProjectileType<MjolnirDeflection>(), reflectedDamage, 3, Projectile.owner);
            reflectedCooldown = 6;
            MjolnirEffects.Burst(origin, 8, Powered);
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = .6f, Pitch = .3f }, origin);
            break;
        }
    }

    private void Bolt(Vector2 start, Vector2 end, float damageScale, bool strong, bool fromHeavens = false)
    {
        if (!IsOwner || Vector2.DistanceSquared(start, end) < 1) return;
        int damage = (int)(Owner.GetTotalDamage(DamageClass.Magic).ApplyTo(88) * damageScale);
        int index = Projectile.NewProjectile(Projectile.GetSource_FromAI(), end, Vector2.Zero,
            ModContent.ProjectileType<MjolnirLightning>(), damage, 3, Projectile.owner, start.X, start.Y, fromHeavens ? 2 : strong ? 1 : 0);
        if (index < Main.maxProjectiles) Main.projectile[index].CritChance = (int)Owner.GetTotalCritChance(DamageClass.Magic);
    }

    private void ChainFromHammer(int count, float damageScale)
    {
        if (!IsOwner) return;
		
        Vector2 origin = Projectile.Center;
        int[] visited = new int[count];
        Array.Fill(visited, -1);
        for (int hop = 0; hop < count; hop++)
        {
            NPC nearest = null;
            float distance = 300 * 300;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float candidateDistance = Vector2.DistanceSquared(origin, npc.Center);
                if (!npc.CanBeChasedBy(Projectile) || Array.IndexOf(visited, npc.whoAmI) >= 0 || candidateDistance >= distance || !Collision.CanHitLine(origin, 1, 1, npc.Center, 1, 1)) continue;
                nearest = npc;
                distance = candidateDistance;
            }
            if (nearest == null) break;
            visited[hop] = nearest.whoAmI;
            Bolt(origin, nearest.Center, damageScale * MathF.Pow(.8f, hop), empoweredAttack || pulseEmpowered);
            origin = nearest.Center;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (IsOwner) ChangeState(HammerState.Lodged);
        Projectile.velocity = Vector2.Zero;
        MjolnirEffects.Burst(Projectile.Center, 16, Powered);
        MjolnirEffects.Shake(Projectile.Center, 2);
        SoundEngine.PlaySound(SoundID.Dig with { Pitch = -.6f }, Projectile.Center);
        return false;
    }

    public override bool? CanDamage() => State is HammerState.Thrown or HammerState.Recalling or HammerState.Sweeping or HammerState.Flying || State == HammerState.Spinning && Timer >= SpinThreshold || State == HammerState.Slamming && FaultbreakerWeaponMotion.HammerCanHit(Timer / SlamTime) ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (State == HammerState.Slamming)
        {
            float distance = 0;
            return Collision.CanHitLine(Owner.Center, 1, 1, targetHitbox.Center.ToVector2(), 1, 1) && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previousHead, Head, 30, ref distance);
        }
        if (State == HammerState.Flying)
        {
            float distance = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Owner.Center, Projectile.Center + aim * 16, 32, ref distance);
        }
        if (State == HammerState.Spinning)
            return Vector2.DistanceSquared(Projectile.Center, Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight())) < 48 * 48;
        if (State == HammerState.Sweeping)
        {
            float distance = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previousCenter, Projectile.Center, 36, ref distance);
        }
        return null;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.SourceDamage *= State == HammerState.Spinning ? .35f : State is HammerState.Flying or HammerState.Slamming ? 1.65f : State == HammerState.Sweeping ? 1.3f : 1f;
        if (empoweredAttack) modifiers.SourceDamage *= 1.5f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.Electrified, empoweredAttack ? 240 : 90);
        MjolnirEffects.Burst(Projectile.Center, 8, Powered);
        if (IsOwner && State == HammerState.Flying) StopFlight();
    }

    public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles, System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles, System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
    {
        if (State == HammerState.Sweeping && Projectile.Center.Y < Owner.MountedCenter.Y) behindNPCs.Add(index);
        else overPlayers.Add(index);
    }

	//i wonder if anyone reads my code
	//im kinda hungry right now i could really go for a pizza...
    public override bool PreDraw(ref Color lightColor)
    {
        float power = empowered || empoweredAttack ? 1f : .15f;
        bool boosted = Powered;
        if (pulseDelay > 0 || State is HammerState.Recalling or HammerState.Flying)
        {
            float opacity = pulseDelay > 0 ? .12f + (12 - pulseDelay) / 22f : .85f;
            MjolnirEffects.ColorLightning(tether, boosted);
            tetherRenderer.Draw(tether, opacity, boosted ? 1.35f : 1);
        }
        if (spinVisual > 0)
            MjolnirEffects.Vortex(Projectile.Center, aim.ToRotation(), spinPhase, spinVisual, power, boosted);
        if (State is HammerState.Thrown or HammerState.Recalling or HammerState.Flying)
        {
            Vector2[] trail = new Vector2[Projectile.oldPos.Length + 1];
            trail[0] = Projectile.Center;
            for (int i = 0; i < Projectile.oldPos.Length; i++) trail[i + 1] = Projectile.oldPos[i] + Projectile.Size * .5f;
            MjolnirEffects.Queue(() =>
            {
                for (int i = 1; i < trail.Length; i++)
                {
                    if (Vector2.DistanceSquared(trail[i - 1], trail[i]) > 180 * 180) break;
                    float fade = 1 - i / (float)trail.Length;
                    MjolnirEffects.Line(trail[i - 1], trail[i], MjolnirEffects.Additive(MjolnirEffects.EnergyColor(boosted)) * fade * .28f, 16 * fade);
                    MjolnirEffects.Line(trail[i - 1], trail[i], MjolnirEffects.Additive(MjolnirEffects.CoreColor(boosted)) * fade * .75f, 3 * fade);
                }
            });
        }
        if (State == HammerState.Sweeping)
        {
            Vector2[] arc = new Vector2[28];
            for (int i = 0; i < arc.Length; i++) arc[i] = SweepPosition(Math.Max(0, Timer - i * .4f));
            MjolnirEffects.SweepArc(arc, Owner.MountedCenter.Y, boosted);
        }
        if (State == HammerState.Slamming && Timer >= SlamTime * .3f)
        {
            Vector2[] arc = new Vector2[18];
            for (int i = 0; i < arc.Length; i++)
            {
                float angle = FaultbreakerWeaponMotion.HammerAngle(Math.Max(.3f, (Timer - i * .35f) / SlamTime), sweepDirection);
                Vector2 hand = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, angle - MathHelper.PiOver2);
                arc[i] = hand + angle.ToRotationVector2() * 30;
            }
            MjolnirEffects.SweepArc(arc, Owner.MountedCenter.Y, boosted);
        }
        if (State == HammerState.GroundImpact)
            MjolnirEffects.Impact(Head, Timer, 1 - Timer / 10f, empoweredAttack ? 1.2f : .8f, boosted);
        if (State == HammerState.Charging)
        {
            Vector2 head = Head;
            float progress = Math.Min(1, Timer / ChargeTime);
            if (Timer < ChargeTime)
            {
                MjolnirEffects.ColorLightning(skyLeader, true);
                skyRenderer.Draw(skyLeader, (progress - .5f) * .5f);
            }
            MjolnirEffects.Queue(() =>
            {
                MjolnirEffects.Glow(head, new Vector2(35 + progress * 50), .18f + progress * .45f, boosted: true);
            });
        }
        if (boosted)
        {
            Vector2 head = Head;
            float pulse = .3f + .08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6);
            MjolnirEffects.Queue(() => MjolnirEffects.Glow(head, new Vector2(64), pulse, boosted: true));
        }
        if (Owner.GetModPlayer<MjolnirPlayer>().FlightCooldown > 0 && IsOwner)
        {
            Vector2 start = Owner.MountedCenter + new Vector2(-18, 35);
            MjolnirEffects.Line(start, start + new Vector2(36, 0), new Color(18, 32, 54), 3);
            MjolnirEffects.Line(start, start + new Vector2(36 * (1 - Owner.GetModPlayer<MjolnirPlayer>().FlightCooldown / 150f), 0), new Color(130, 215, 255), 2);
        }
        float depth = State == HammerState.Sweeping ? 1 + (Projectile.Center.Y - Owner.MountedCenter.Y) / 170 : 1;
        float visibility = 1 - spinVisual * .97f;
        Vector2 drawCenter = Projectile.Center - Main.screenPosition + new Vector2(0, InHand ? Owner.gfxOffY : 0);
        if (!DrawHeldLayer && State != HammerState.Spinning)
            MjolnirEffects.DrawHammer(drawCenter, Projectile.rotation, depth, power, Color.White * visibility, boosted);
        return false;
    }
}

public class MjolnirLightning : ModProjectile
{
    private LightningUtils.LightningData lightning;
    private readonly LightningStrokeRenderer renderer = new();
    private int age;
    private bool FromHeavens => Projectile.ai[2] == 2;
    private bool Boosted => Projectile.ai[2] > 0;
    private Vector2 Start => new(Projectile.ai[0], Projectile.ai[1]);
    private Vector2 End => Projectile.Center;
    public override string Texture => "AerovelenceMod/Assets/Pixel/CrispStarPMA";

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 36;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void AI()
    {
        age++;
        if (age == 1 && !FromHeavens) Projectile.timeLeft = 22;
        if (FromHeavens && Main.player[Projectile.owner].GetModPlayer<MjolnirPlayer>().Hammer is { } hammer &&
            hammer.Projectile.active && hammer.ReceivingCharge)
            Projectile.Center = hammer.Head;
        if (Main.dedServ) return;
        if (lightning == null)
        {
            lightning = MjolnirEffects.Lightning(Math.Clamp((int)(Vector2.Distance(Start, End) / 18), 12, 72), FromHeavens ? 9 : Projectile.ai[2] > 0 ? 5.5f : 3.5f);
            MjolnirEffects.ColorLightning(lightning, Boosted);
            lightning.DisplacementIntensity = FromHeavens ? 2.3f : 1.1f;
            lightning.EndThickness = FromHeavens ? 2 : 1.5f;
            lightning.MaxBranches = FromHeavens ? 8 : 4;
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = FromHeavens ? .95f : .5f, Pitch = FromHeavens ? -.55f : -.15f, MaxInstances = 4 }, End);
            MjolnirEffects.Burst(End, FromHeavens ? 38 : Boosted ? 24 : 16, Boosted);
            Vector2 shakeCenter = Vector2.DistanceSquared(Main.LocalPlayer.Center, Start) < Vector2.DistanceSquared(Main.LocalPlayer.Center, End) ? Start : End;
            MjolnirEffects.Shake(shakeCenter, FromHeavens ? 7 : Boosted ? 6 : 1.6f);
            if (FromHeavens)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = .5f, Pitch = -.7f }, End);
                for (int i = 1; i < 12; i++)
                    MjolnirEffects.Spark(Vector2.Lerp(End, Start, i / 12f), Main.rand.NextVector2Circular(4, 2), Boosted);
            }
        }
        if (age % 3 == 1 || !lightning.Initialized)
            MjolnirEffects.UpdateLightning(lightning, Start, End);
        for (int i = 0; i < 8; i++) Lighting.AddLight(Vector2.Lerp(End, Start, i / 8f), MjolnirEffects.EnergyColor(Boosted).ToVector3() * .85f);
    }

    public override bool? CanDamage() => Projectile.damage > 0 && age <= 4 ? null : false;
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Start, End, Projectile.ai[2] > 0 ? 22 : 12, ref distance);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Electrified, Boosted ? 300 : 180);
    public override void OnKill(int timeLeft) => renderer.Dispose();
	
    public override bool PreDraw(ref Color lightColor)
    {
        if (lightning == null) return false;
        float fade = MathF.Pow(MathHelper.Clamp(Projectile.timeLeft / (FromHeavens ? 36f : 22f), 0, 1), .65f);
        float flicker = age < 10 ? .85f + .15f * MathF.Cos(age * 2.4f) : 1;
        renderer.Draw(lightning, fade * flicker, FromHeavens && age < 5 ? 1.5f : 1);
        MjolnirEffects.Impact(End, age, fade, FromHeavens ? 1.6f : Boosted ? 1.2f : .8f, Boosted);
        return false;
    }
}

public class MjolnirGroundPulse : ModProjectile
{
    private readonly System.Collections.Generic.List<Vector2> path = new();
    private readonly LightningUtils.LightningData lightning = MjolnirEffects.Lightning(2, 3.5f);
    private readonly LightningStrokeRenderer renderer = new();
    private Vector2 previous;
    private int age;
    private bool stopped;
    private bool Charged => Projectile.ai[1] > 0;
    public override string Texture => "AerovelenceMod/Assets/Pixel/CrispStarPMA";

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 90;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => stopped ? false : null;

    public override void AI()
    {
        age++;
        previous = Projectile.Center;
        if (age == 1)
        {
            Projectile.timeLeft = Charged ? 82 : 62;
            path.Add(Projectile.Center - Vector2.UnitY * 3);
            lightning.Branches = new();
            lightning.EndThickness = 1.2f;
            MjolnirEffects.ColorLightning(lightning, Charged);
        }
        if (!stopped)
        {
            float x = Projectile.Center.X + Projectile.ai[0] * 8;
            bool foundSurface = TrySurface(x, Projectile.Center.Y, out Vector2 next);
            bool blockedUphill = foundSurface && next.Y < Projectile.Center.Y - 1f && !Collision.CanHitLine(Projectile.Center - Vector2.UnitY * 24, 1, 1, next - Vector2.UnitY * 24, 1, 1);
            if (age > (Charged ? 60 : 40) || !foundSurface || blockedUphill)
            {
                stopped = true;
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 20);
            }
            else
            {
                Projectile.Center = next;
                path.Add(next - new Vector2(0, 3 + (age % 3 == 0 ? 4 : 0)));
                lightning.SegmentPositions = path.ToArray();
                lightning.MaxSegments = path.Count;
                if (!Main.dedServ)
                {
                    MjolnirEffects.Spark(next - Vector2.UnitY * 4, new Vector2(Projectile.ai[0] * 2, -Main.rand.NextFloat(1, 4)), Charged);
                    Lighting.AddLight(next, MjolnirEffects.EnergyColor(Charged).ToVector3() * .9f);
                }
                if (Charged && age % 14 == 0 && Projectile.owner == Main.myPlayer)
                {
                    NPC target = null;
                    float nearest = 160 * 160;
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        float distance = Vector2.DistanceSquared(next, npc.Center);
                        if (!npc.CanBeChasedBy(Projectile) || distance >= nearest || !Collision.CanHitLine(next - Vector2.UnitY * 6, 1, 1, npc.Center, 1, 1)) continue;
                        nearest = distance;
                        target = npc;
                    }
                    if (target != null)
                    {
                        int index = Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center, Vector2.Zero, ModContent.ProjectileType<MjolnirLightning>(), Projectile.damage / 2, 2, Projectile.owner, next.X, next.Y - 6, 1);
                        if (index < Main.maxProjectiles) Main.projectile[index].CritChance = Projectile.CritChance;
                    }
                }
            }
        }
    }

    internal static bool TrySurface(float x, float referenceY, out Vector2 surface)
    {
        surface = default;
        int tileX = (int)(x / 16);
        int centerY = (int)(referenceY / 16);
        float nearest = float.MaxValue;
        bool found = false;
        for (int tileY = centerY - 2; tileY <= centerY + 3; tileY++)
        {
            if (!WorldGen.InWorld(tileX, tileY, 1)) continue;
            Tile tile = Main.tile[tileX, tileY];
            if (!tile.HasTile || tile.IsActuated || !(Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType] || Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0)) continue;
            float y = tileY * 16 + (tile.IsHalfBlock ? 8 : 0);
            float localX = x - tileX * 16;
            if (tile.Slope == SlopeType.SlopeDownRight) y += 16 - localX;
            else if (tile.Slope == SlopeType.SlopeDownLeft) y += localX;
            float difference = y - referenceY;
            if (difference < -18 || difference > 48 || Math.Abs(difference) >= nearest || Collision.SolidCollision(new Vector2(x - 1, tileY * 16 - 4), 2, 2)) continue;
            surface = new Vector2(x, y);
            nearest = Math.Abs(difference);
            found = true;
        }
        return found;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previous - Vector2.UnitY * 10, Projectile.Center - Vector2.UnitY * 10, 26, ref distance);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Electrified, Charged ? 240 : 120);
    public override void OnKill(int timeLeft) => renderer.Dispose();
    public override bool PreDraw(ref Color lightColor)
    {
        float opacity = Math.Min(1, Projectile.timeLeft / 20f);
        renderer.Draw(lightning, opacity, Charged ? 1.4f : 1);
        if (!stopped)
        {
            Vector2 center = Projectile.Center - Vector2.UnitY * 5;
            MjolnirEffects.Queue(() => MjolnirEffects.Glow(center, new Vector2(42, 20), opacity * .7f, boosted: Charged));
        }
        return false;
    }
}

public class MjolnirDeflection : ModProjectile
{
    public override string Texture => "AerovelenceMod/Assets/Pixel/CrispStarPMA";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.timeLeft = 90;
        Projectile.extraUpdates = 1;
    }
	
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.timeLeft % 3 == 0) MjolnirEffects.Spark(Projectile.Center, -Projectile.velocity * .1f);
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 center = Projectile.Center;
        Vector2 velocity = Projectile.velocity;
        MjolnirEffects.Queue(() =>
        {
            MjolnirEffects.Glow(center, new Vector2(26), .5f);
            MjolnirEffects.Line(center - velocity * 3, center, new Color(75, 175, 255, 0), 5);
            MjolnirEffects.Line(center - velocity * 2, center, new Color(255, 255, 255, 0), 2);
        });
        return false;
    }
	
    public override void OnKill(int timeLeft) => MjolnirEffects.Burst(Projectile.Center, 6);
}

internal static class MjolnirEffects
{
    internal static Color EnergyColor(bool boosted) => boosted ? TumblerVFX.PhaseColor(1f) : new Color(55, 145, 255);
    internal static Color CoreColor(bool boosted) => boosted ? new Color(255, 247, 180) : new Color(225, 250, 255);
    internal static Color Additive(Color color) => new(color.R, color.G, color.B, 0);

    internal static void ColorLightning(LightningUtils.LightningData data, bool boosted)
    {
        data.CoreColorOverride = boosted ? new Color(255, 252, 222) : Color.White;
        data.MidColorOverride = boosted ? new Color(255, 222, 90) : new Color(140, 225, 255);
        data.OuterColorOverride = EnergyColor(boosted);
    }

    internal static float Ease(float progress) => MathHelper.SmoothStep(0, 1, MathHelper.Clamp(progress, 0, 1));

    internal static Vector2 CurveFlight(Vector2 direction, Vector2 target)
    {
        float angle = direction.ToRotation();
        float turn = MathHelper.WrapAngle(target.ToRotation() - angle);
        return (angle + MathHelper.Clamp(turn * .065f, -.032f, .032f)).ToRotationVector2();
    }

    internal static void Queue(Action draw, RenderLayer layer = RenderLayer.OverPlayers) => ModContent.GetInstance<NewPixelationSystem>().QueueRenderAction(layer, draw);

    internal static void Shake(Vector2 center, float strength)
    {
        if (Main.dedServ) return;
        float shake = strength * MathHelper.Clamp(1 - Vector2.Distance(Main.LocalPlayer.Center, center) / 950, 0, 1);
        AeroPlayer camera = Main.LocalPlayer.GetModPlayer<AeroPlayer>();
        camera.ScreenShakePower = Math.Max(camera.ScreenShakePower, shake);
    }

    internal static LightningUtils.LightningData Lightning(int segments, float width) => new(Vector2.Zero, Vector2.UnitY, LightningUtils.LightningStyle.Jagged)
    {
        MaxSegments = segments,
        MaxBranches = 3,
        BranchChance = .8f,
        DisplacementIntensity = 1.1f,
        NoiseFrequency = 1.8f,
        StartThickness = width,
        EndThickness = width * .45f,
        CoreColorOverride = Color.White,
        OuterColorOverride = new Color(55, 145, 255)
    };

    internal static void UpdateLightning(LightningUtils.LightningData data, Vector2 start, Vector2 end)
    {
        if (Vector2.DistanceSquared(start, end) < 1) return;
        if (data.Initialized && Vector2.DistanceSquared(data.SegmentPositions[^1], end) > 48 * 48) data.Branches.Clear();
        LightningUtils.InitializeBetweenPoints(data, start, end, LightningUtils.LightningStyle.Jagged);
        LightningUtils.UpdateSegments(data);
        LightningUtils.UpdateBranches(data);
    }

    internal static void Glow(Vector2 center, Vector2 size, float opacity, float rotation = 0, bool boosted = false)
    {
        Texture2D texture = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/feather_circle128PMA").Value;
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, Additive(EnergyColor(boosted)) * opacity, rotation, texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, Additive(CoreColor(boosted)) * opacity * .65f, rotation, texture.Size() * .5f, size / texture.Size() * .42f, SpriteEffects.None, 0);
    }

    internal static void Impact(Vector2 center, float age, float opacity, float power, bool boosted = false)
    {
        Queue(() =>
        {
            float flash = MathF.Exp(-age / 6f);
            Glow(center, new Vector2(145 * power), opacity * (.4f + flash), boosted: boosted);
            Glow(center, new Vector2(38 * power), flash * 1.4f, boosted: boosted);
        });
    }

    internal static void Vortex(Vector2 center, float aimAngle, float phase, float strength, float power, bool boosted = false)
    {
        if (strength <= 0) return;
        Queue(() =>
        {
            Glow(center, new Vector2(58, 108) * strength, strength * .7f, aimAngle, boosted);
            for (int ribbon = 0; ribbon < 3; ribbon++)
            {
                float radius = 28 + ribbon * 6;
                for (int i = 0; i < 64; i++)
                {
                    float angle = phase + ribbon * 2.1f - i * MathHelper.TwoPi / 64;
                    float nextAngle = angle - MathHelper.TwoPi / 64;
                    float wake = MathF.Pow(1 - i / 64f, 1.5f);
                    float depth = .6f + .4f * MathF.Cos(angle);
                    Vector2 start = center + new Vector2(MathF.Cos(angle) * radius * .3f, MathF.Sin(angle) * radius).RotatedBy(aimAngle) * strength;
                    Vector2 end = center + new Vector2(MathF.Cos(nextAngle) * radius * .3f, MathF.Sin(nextAngle) * radius).RotatedBy(aimAngle) * strength;
                    float alpha = strength * wake * depth;
                    Line(start, end, Additive(EnergyColor(boosted)) * alpha * .32f, 12 + power * 3);
                    Line(start, end, Additive(Color.Lerp(EnergyColor(boosted), CoreColor(boosted), .35f)) * alpha * .8f, 5);
                    Line(start, end, Additive(CoreColor(boosted)) * alpha, 2);
                }
            }
            for (int i = 0; i < 5; i++)
            {
                float streak = (phase * .12f + i * .2f) % 1;
                if (streak < 0) streak += 1;
                Vector2 offset = new Vector2(-24 + streak * 64, MathF.Sin(i * 2.4f + phase * .3f) * 24).RotatedBy(aimAngle);
                Vector2 direction = aimAngle.ToRotationVector2();
                Line(center + offset, center + offset - direction * 14, Additive(CoreColor(boosted)) * strength * MathF.Sin(streak * MathHelper.Pi) * .65f, 2);
            }
        });
    }

    internal static void SweepArc(Vector2[] points, float centerY, bool boosted = false)
    {
        Queue(() => DrawHalf(false), RenderLayer.UnderNPCs);
        Queue(() => DrawHalf(true));
        void DrawHalf(bool front)
        {
            for (int i = 1; i < points.Length; i++)
            {
                if ((points[i].Y >= centerY) != front) continue;
                float fade = 1 - i / (float)points.Length;
                Line(points[i - 1], points[i], Additive(EnergyColor(boosted)) * fade * .28f, (boosted ? 26 : 18) * fade);
                Line(points[i - 1], points[i], Additive(Color.Lerp(EnergyColor(boosted), CoreColor(boosted), .35f)) * fade * .8f, 7 * fade);
                Line(points[i - 1], points[i], Additive(CoreColor(boosted)) * fade, 2);
            }
        }
    }

    internal static void Spark(Vector2 position, Vector2 velocity, bool boosted = false)
    {
        if (Main.dedServ) return;
        Dust spark = Dust.NewDustPerfect(position, ModContent.DustType<ElectricSparkGlow>(), velocity, 0, EnergyColor(boosted), .22f);
        spark.customData = new ElectricSparkBehavior(FadeAlphaPower: .88f, FadeScalePower: .98f, FadeVelPower: .94f, TimeBetweenFrames: 3, KillEarlyTime: 22, Pixelize: true, UnderGlowPower: 1.5f, WhiteLayerPower: .9f);
    }

    internal static void Burst(Vector2 center, int count, bool boosted = false)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < count; i++) Spark(center, Main.rand.NextVector2CircularEdge(1, 1) * Main.rand.NextFloat(2, 7), boosted);
    }

    internal static void Line(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }

    internal static void AddHeldHammer(ref PlayerDrawSet drawInfo, Vector2 center, float rotation, float scale, float power, Color tint, int direction, bool boosted = false)
    {
        Texture2D texture = ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Weapons/CrystalCaverns/Mjolnir/Mjolnir").Value;
        Vector2 origin = direction < 0 ? new Vector2(texture.Width - 21f, 34f) : new Vector2(21f, 34f);
        Vector2 grip = center + new Vector2(0f, 18f).RotatedBy(rotation);
        float drawRotation = rotation - .72f * direction;
        SpriteEffects effects = direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        drawInfo.DrawDataCache.Add(new DrawData(texture, grip, null, tint, drawRotation, origin, scale, effects));
        if (power > 0)
            drawInfo.DrawDataCache.Add(new DrawData(texture, grip, null, Additive(EnergyColor(boosted)) * power * (boosted ? .7f : .32f) * (tint.A / 255f), drawRotation, origin, scale, effects));
    }

    internal static void DrawHammer(Vector2 center, float rotation, float scale, float power, Color tint, bool boosted = false)
    {
        Texture2D texture = ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Weapons/CrystalCaverns/Mjolnir/Mjolnir").Value;
        Vector2 origin = new(17f, 38f);
        Vector2 grip = center + new Vector2(0f, 18f).RotatedBy(rotation);
        float drawRotation = rotation - .72f;
        Main.spriteBatch.Draw(texture, grip, null, tint, drawRotation, origin, scale, SpriteEffects.None, 0);
        if (power > 0)
            Main.spriteBatch.Draw(texture, grip, null, Additive(EnergyColor(boosted)) * power * (boosted ? .7f : .32f), drawRotation, origin, scale, SpriteEffects.None, 0);
    }
}