using Terraria.Audio;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using AerovelenceMod.Common.Systems;
using System;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Dusts.GlowDusts;
using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Accessories.SmallAccessories
{
    public class OpalOfCaVea : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Opal Of Ca Vea", "Glowing crystals grow on your back every 5 seconds, up to 3\nEnemies take damage if all 3 crystals have spawned")
            .AddName(Language.Spanish, "Ópalo de Ca Vea").AddTooltip(Language.Spanish, "Cristales brillantes crecen en tu espalda cada 5 segundos, hasta 3\nLos enemigos reciben daño si se han generado los 3 cristales")
            .AddName(Language.French, "Opale de Ca Vea").AddTooltip(Language.French, "Des cristaux lumineux poussent sur votre dos toutes les 5 secondes, jusqu'à 3\nLes ennemis subissent des dégâts si les 3 cristaux sont apparus")
            .AddName(Language.German, "Ca Veas Opal").AddTooltip(Language.German, "Leuchtende Kristalle wachsen alle 5 Sekunden auf deinem Rücken, bis zu 3\nFeinde erleiden Schaden, wenn alle 3 Kristalle erschienen sind")
            .AddName(Language.Italian, "Opale di Ca Vea").AddTooltip(Language.Italian, "Cristalli luminosi crescono sulla tua schiena ogni 5 secondi, fino a 3\nI nemici subiscono danni se tutti e 3 i cristalli sono apparsi")
            //.AddName(Language.Polish, "Opal Ca Vea").AddTooltip(Language.Polish, "Świecące kryształy rosną na twoich plecach co 5 sekund, do 3 sztuk\nWrogowie otrzymują obrażenia, jeśli pojawią się wszystkie 3 kryształy")
            //.AddName(Language.PortugueseBrazil, "Opala de Ca Vea").AddTooltip(Language.PortugueseBrazil, "Cristais brilhantes crescem nas suas costas a cada 5 segundos, até 3\nInimigos sofrem dano se todos os 3 cristais surgirem")
            .AddName(Language.Russian, "Опал Ка Веа").AddTooltip(Language.Russian, "Светящиеся кристаллы появляются на вашей спине каждые 5 секунд, до 3 штук\nВраги получают урон, если появились все 3 кристалла");
            //.AddName(Language.ChineseTraditional, "卡維亞的蛋白石").AddTooltip(Language.ChineseTraditional, "每 5 秒你的背上會長出發光的水晶，最多 3 個\n如果所有 3 個水晶都已生成，敵人會受到傷害")
            //.AddName(Language.ChineseSimplified, "卡维亚的蛋白石").AddTooltip(Language.ChineseSimplified, "每 5 秒你的背上会长出发光的水晶，最多 3 个\n如果所有 3 个水晶都已生成，敌人会受到伤害");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.sellPrice(gold: 1);
            Item.rare = ItemRarities.EarlyPHM;
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<OpalOfCaVeaPlayer>().hasOpal = true;

            if (!hideVisual)
            {
                player.GetModPlayer<OpalOfCaVeaPlayer>().hasOpalVisibility = true;
            }
        }
    }

    public class OpalOfCaVeaPlayer : ModPlayer
    {
        public bool hasOpal;
        public bool hasOpalVisibility;
        public int crystalCount;
        private int crystalTimer;
        internal readonly int[] CrystalAge = new int[3];

        public override void ResetEffects() => hasOpal = hasOpalVisibility = false;

        public override void UpdateDead()
        {
            crystalCount = crystalTimer = 0;
            Array.Clear(CrystalAge);
        }

        public override void PostUpdate()
        {
            if (!hasOpal || Player.dead)
            {
                crystalCount = crystalTimer = 0;
                Array.Clear(CrystalAge);
                return;
            }
            for (int i = 0; i < crystalCount; i++)
                CrystalAge[i] = Math.Min(24, CrystalAge[i] + 1);
            if (hasOpal)
            {
                crystalTimer++;
                if (crystalCount < 3 && crystalTimer >= 300)
                {
                    CrystalAge[crystalCount++] = 0;
                    crystalTimer = 0;
                    if (hasOpalVisibility && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.3f, Pitch = 0.3f + crystalCount * 0.1f }, Player.Center);
                        for (int i = 0; i < 8; i++)
                        {
                            Dust spark = Dust.NewDustPerfect(Player.Center + new Vector2(-14f * Player.direction, -8f), ModContent.DustType<GlowPixelCross>(),
                                Main.rand.NextVector2Circular(2f, 2f), newColor: Color.SkyBlue, Scale: 0.2f);
                            spark.customData = DustBehaviorUtil.AssignBehavior_GPCBase(rotPower: 0.1f, timeBeforeSlow: 4,
                                preSlowPower: 0.9f, postSlowPower: 0.85f, velToBeginShrink: 1f, fadePower: 0.85f, shouldFadeColor: false);
                        }
                    }
                }
                float intensity = crystalCount / 3f;
                Lighting.AddLight(Player.Center, new Vector3(0.0f, 0.3f, 0.7f) * intensity);
            }
        }

        public override void OnRespawn()
        {
            crystalCount = crystalTimer = 0;
        }


        public override void OnHitByNPC(NPC npc, Player.HurtInfo info)
        {
            if (hasOpal && crystalCount >= 3)
            {
                int reflectDamage = 20;
                npc.StrikeNPC(new NPC.HitInfo
                {
                    Damage = reflectDamage,
                    Knockback = 0f,
                    HitDirection = 0,
                    Crit = false
                }, fromNet: false, noPlayerInteraction: false);

                if (Main.myPlayer == Player.whoAmI)
                {
                    Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero,
                        ModContent.ProjectileType<ElectricPopEffect>(), 0, 0f, Player.whoAmI);
                    crystalCount = 0;
                    crystalTimer = 0;
                    for (int t = 0; t < 8; t++)
                    {
                        Vector2 dustVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 3.25f);

                        Dust gd = Dust.NewDustPerfect(npc.Center, ModContent.DustType<GlowPixelCross>(), dustVel, newColor: Color.SkyBlue, Scale: Main.rand.NextFloat(0.2f, 0.4f));
                        gd.customData = DustBehaviorUtil.AssignBehavior_GPCBase(rotPower: 0.2f, timeBeforeSlow: 5,
                            preSlowPower: 0.95f, postSlowPower: 0.89f, velToBeginShrink: 1f, fadePower: 0.9f, shouldFadeColor: false);
                    }
                }
            }
        }
    }

    public class OpalOfCaVeaCrystalLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.BackAcc);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (player.dead || player.invis || drawInfo.shadow != 0f) return;

            var modPlayer = player.GetModPlayer<OpalOfCaVeaPlayer>();
            if (!modPlayer.hasOpal) return;

            if (modPlayer.hasOpalVisibility)
            {
                Texture2D crystalTexture = ModContent.Request<Texture2D>("AerovelenceMod/Content/Tiles/CrystalCaverns/Natural/CavernCrystalItem").Value;
                Vector2 basePosition = drawInfo.Position + player.Size * 0.5f + new Vector2(0f, player.gfxOffY);
                Vector2 screenPos = Main.screenPosition;
                float heightFactor = player.mount.Active ? 0.8f : 1f;
                Vector2 offset1 = new(-14 * player.direction, -player.height * 0.5f * heightFactor);
                Vector2 offset2 = new(-13 * player.direction, -player.height * 0.2f * heightFactor);
                Vector2 offset3 = new(-8 * player.direction, player.height * 0.1f * heightFactor);
                Vector2[] offsets = [offset1, offset2, offset3];

                int crystalsToDraw = modPlayer.crystalCount;
                for (int i = 0; i < crystalsToDraw; i++)
                {
                    Vector2 offset = offsets[i];
                    offset.X -= i * 4 * player.direction;
                    offset.Y += 10f;
                    Vector2 drawPos = basePosition + offset - screenPos;
                    float rotation = (player.direction == 1) ? MathHelper.ToRadians(45) : MathHelper.ToRadians(-45);
                    float dynamicSway = player.velocity.X * 0.05f;
                    rotation -= dynamicSway;
                    SpriteEffects effects = (player.direction == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    float progress = modPlayer.CrystalAge[i] / 24f;
                    float growth = progress >= 1f ? 1f : 1f + 2.7f * MathF.Pow(progress - 1f, 3f) + 1.7f * MathF.Pow(progress - 1f, 2f);
                    drawPos += new Vector2(player.direction * 8f, 5f * player.gravDir) * (1f - progress);
                    DrawData data = new(crystalTexture, drawPos, null, Color.White * Math.Min(1f, progress * 4f), rotation,
                        crystalTexture.Size() * 0.5f, Math.Max(0.01f, growth), effects, 0);
                    drawInfo.DrawDataCache.Add(data);
                    if (progress < 1f)
                        drawInfo.DrawDataCache.Add(new DrawData(crystalTexture, drawPos, null, new Color(170, 225, 255, 0) * (1f - progress), rotation,
                            crystalTexture.Size() * 0.5f, Math.Max(0.01f, growth), effects));
                }
            }
        }
    }


    public class OpalGlowLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.BackAcc);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.GetModPlayer<OpalOfCaVeaPlayer>().crystalCount >= 3;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
        }
    }


    public class ElectricPopEffect : ModProjectile
    {
        public override string Texture => "AerovelenceMod/Assets/Orbs/ElectricPopD";
        public float drawScale = 0.005f;
        private float colorLerpProgress = 0f;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.scale = 0.1f;
            Projectile.alpha = 0;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.Center = player.Center;
            Projectile.alpha = Math.Min(Projectile.alpha + 8, 255);
            colorLerpProgress = (120f - Projectile.timeLeft) / 60f;
            drawScale += 0.0055f;
            Projectile.rotation += 0.3f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            PixellationSystem.QueuePixelationAction(() =>
            {
                SpriteBatch spriteBatch = Main.spriteBatch;
                Texture2D texture = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/ElectricPopE").Value;
                Texture2D texture2 = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/ElectricPopC").Value;
                Rectangle frame = texture.Frame();
                Vector2 origin = frame.Size() / 2f;
                Color color1Start = new(0, 255, 255);
                Color color1End = new(0, 0, 255);
                Color color2Start = new(255, 255, 255);
                Color color2End = new(255, 255, 0);
                Color drawColor = Color.Lerp(color1Start, color1End, colorLerpProgress) * ((255 - Projectile.alpha) / 255f);
                Color drawColor2 = Color.Lerp(color2Start, color2End, colorLerpProgress) * ((255 - Projectile.alpha) / 255f);
                Player player = Main.player[Projectile.owner];
                Vector2 drawPos = (player.Center - Main.screenPosition) / 2f;
                float finalDrawScale = drawScale / 2;
                spriteBatch.Draw(texture, drawPos, frame, drawColor, Projectile.rotation, origin, finalDrawScale, SpriteEffects.None, 0f);
                spriteBatch.Draw(texture2, drawPos, frame, drawColor2, -Projectile.rotation, origin, finalDrawScale, SpriteEffects.None, 0f);
            }, PixellationSystem.RenderType.Additive);

            return false;
        }
    }
}