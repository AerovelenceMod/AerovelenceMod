



using Terraria.Audio;

using Terraria.DataStructures;

using Terraria.GameContent;
using System;


namespace AerovelenceMod.Content.Items.Weapons.Misc.Ranged
{
    public class PouchOfRocks : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("PouchOfMagnets", "Throws 2 magnetic stones which can collide and explode")
            .AddName(Language.Default, "Pouch of Magnets").AddTooltip(Language.Default, "Throws 2 magnetic stones which can collide and explode")
            .AddSkillStrike(Language.Default, "The explosions Skill Strike")

            .AddName(Language.Spanish, "Bolsa de Imanes").AddTooltip(Language.Spanish, "Lanza 2 piedras magnéticas que pueden chocar y explotar").AddSkillStrike(Language.Spanish, "Las explosiones realizan Golpes de Habilidad")
            .AddName(Language.French, "Pochette d'Aimants").AddTooltip(Language.French, "Lance 2 pierres magnétiques qui peuvent entrer en collision et exploser").AddSkillStrike(Language.French, "Les explosions déclenchent des Coups de Compétence")
            .AddName(Language.German, "Beutel mit Magneten").AddTooltip(Language.German, "Wirft 2 magnetische Steine, die kollidieren und explodieren können").AddSkillStrike(Language.German, "Die Explosionen führen Fähigkeitsschläge aus")
            .AddName(Language.Italian, "Sacca di Magneti").AddTooltip(Language.Italian, "Lancia 2 pietre magnetiche che possono collidere ed esplodere").AddSkillStrike(Language.Italian, "Le Esplosioni eseguono Colpi dell'Abilità")
            //.AddName(Language.Polish, "Worek z Magnesami").AddTooltip(Language.Polish, "Rzuca 2 magnetyczne kamienie, które mogą się zderzyć i eksplodować").AddSkillStrike(Language.Polish, "Eksplozje wykonują Ciosy Umiejętności")
            //.AddName(Language.PortugueseBrazil, "Bolsa de Ímãs").AddTooltip(Language.PortugueseBrazil, "Lança 2 pedras magnéticas que podem colidir e explodir").AddSkillStrike(Language.PortugueseBrazil, "As explosões realizam Golpes de Habilidade")
            .AddName(Language.Russian, "Мешочек с Магнитами").AddTooltip(Language.Russian, "Бросает 2 магнитных камня, которые могут столкнуться и взорваться").AddSkillStrike(Language.Russian, "Взрывы активируют Навык Удара");
            //.AddName(Language.ChineseTraditional, "磁石袋").AddTooltip(Language.ChineseTraditional, "投擲 2 顆磁石，可相撞並爆炸").AddSkillStrike(Language.ChineseTraditional, "爆炸觸發技能打擊")
            //.AddName(Language.ChineseSimplified, "磁石袋").AddTooltip(Language.ChineseSimplified, "投掷 2 颗磁石，可相撞并爆炸").AddSkillStrike(Language.ChineseSimplified, "爆炸触发技能打击");
        }

        public override void SetDefaults()
        {
            Item.damage = 8;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 28;
            Item.height = 30;
            Item.useTime = 50;
            Item.useAnimation = 50;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 2;
            Item.value = Item.buyPrice(gold: 5);
            Item.rare = ItemRarities.EarlyPHM;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<MagneticRock>();
            Item.shootSpeed = 7f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float spread = MathHelper.ToRadians(8);
            Vector2 vel1 = velocity.RotatedBy(-spread);
            Vector2 vel2 = velocity.RotatedBy(spread);
            int pairID = Main.rand.Next(1, int.MaxValue);
            Projectile.NewProjectile(source, position, vel1, type, damage, knockback, player.whoAmI, 0f, pairID);
            Projectile.NewProjectile(source, position, vel2, type, damage, knockback, player.whoAmI, 0f, pairID);
            return false;
        }
    }

    public class MagneticRock : ModProjectile
    {
        private const float CollisionDistance = 10f;
        private const float MagneticStrength = 0.5f;

        private bool magnetized
        {
            get => Projectile.ai[2] == 1f;
            set => Projectile.ai[2] = value ? 1f : 0f;
        }
        internal Projectile Arc { get; set; }

        private bool frameAssigned = false;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 2;
            ProjectileID.Sets.NeedsUUID[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 26;
            Projectile.aiStyle = 0;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 15;
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            if (!frameAssigned)
            {
                Projectile.frame = Projectile.identity % 2;
                frameAssigned = true;
            }


            Projectile.tileCollide = !magnetized;
            Projectile.rotation += 0.5f;
            Projectile.ai[0] += 1f;

            if (Projectile.ai[0] >= 15f)
            {
                Projectile.velocity.Y += 0.1f;
            }

            if (Projectile.ai[0] == 1f && Projectile.owner == Main.myPlayer)
            {
                Projectile.velocity *= Main.rand.NextFloat(0.9f, 1.1f);
                Projectile.netUpdate = true;
            }

            if (Projectile.ai[0] >= 40f)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile other = Main.projectile[i];
                    if (other.active && other.type == Projectile.type && other.whoAmI != Projectile.whoAmI && other.owner == Projectile.owner)
                    {
                        if (other.ai[1] != Projectile.ai[1])
                            continue;
                        if (other.ai[0] < 40f)
                            continue;

                        float distance = Vector2.Distance(Projectile.Center, other.Center);
                        Vector2 toOther = other.Center - Projectile.Center;
                        if (toOther != Vector2.Zero)
                        {
                            toOther.Normalize();
                            Projectile.velocity += toOther * MagneticStrength;
                            if (!magnetized)
                            {
                                Projectile.tileCollide = false;
                                magnetized = true;
                                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
                                if (Projectile.owner == Main.myPlayer && Projectile.identity < other.identity)
                                {
                                    int zapId = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                                        ModContent.ProjectileType<MagneticZap>(), Projectile.damage, Projectile.knockBack,
                                        Projectile.owner, Projectile.identity, other.identity);
                                    if (zapId >= 0 && zapId < Main.maxProjectiles)
                                    {
                                        Arc = Main.projectile[zapId];
                                        if (other.ModProjectile is MagneticRock partner)
                                            partner.Arc = Arc;
                                    }
                                }
                            }
                        }

                        if (distance < CollisionDistance)
                        {
                            TriggerCollisionEffect();
                            break;
                        }
                    }
                }
            }
        }



        private void TriggerCollisionEffect()
        {
            if (Projectile.owner != Main.myPlayer) return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MagneticRockExplosion>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            Projectile.Kill();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.type == Projectile.type && other.owner == Projectile.owner && other.whoAmI != Projectile.whoAmI && other.ai[1] == Projectile.ai[1])
                    other.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Arc is { active: true } && Arc.owner == Projectile.owner
                && Arc.type == ModContent.ProjectileType<MagneticZap>()
                && (Arc.ai[0] == Projectile.identity || Arc.ai[1] == Projectile.identity))
                Arc.Kill();
            SoundEngine.PlaySound(SoundID.DD2_SkeletonHurt, Projectile.Center);
            for (float m = 0f; m < 5f; m += 0.5f)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity, DustID.Granite, new Vector2((float)Math.Sin(m) * 1.3f, (float)Math.Cos(m)) * 2.4f, 0, Color.Gray);
                dust.velocity *= Main.rand.NextFloat(0.4f, 1.3f);
                dust.noGravity = true;
                dust.scale = 1f;
            }
        }
    }

    public class MagneticZap : ModProjectile
    {
        private LightningUtils.LightningData lightning;
        private Projectile firstRock;
        private Projectile secondRock;
        private int age;

        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.light = 0.8f;
        }

        private bool MatchesRock(Projectile rock, float identity)
            => rock is { active: true } && rock.owner == Projectile.owner && rock.identity == identity
                && rock.type == ModContent.ProjectileType<MagneticRock>();

        private Projectile ResolveRock(float identity)
        {
            int index = Projectile.GetByUUID(Projectile.owner, identity);
            return (uint)index < Main.maxProjectiles && MatchesRock(Main.projectile[index], identity) ? Main.projectile[index] : null;
        }

        private bool Connected => MatchesRock(firstRock, Projectile.ai[0]) && MatchesRock(secondRock, Projectile.ai[1])
            && firstRock.ai[1] == secondRock.ai[1];

        public override void AI()
        {
            if (!Connected)
            {
                firstRock = ResolveRock(Projectile.ai[0]);
                secondRock = ResolveRock(Projectile.ai[1]);
            }
            if (!Connected)
            {
                if (Projectile.owner == Main.myPlayer || ++age >= 60)
                    Projectile.Kill();
                else
                    Projectile.timeLeft = 2;
                return;
            }
            if (lightning == null)
            {
                if (firstRock.ModProjectile is MagneticRock first) first.Arc = Projectile;
                if (secondRock.ModProjectile is MagneticRock second) second.Arc = Projectile;
                lightning = new LightningUtils.LightningData(Projectile)
                {
                    MaxSegments = 17,
                    CoreColorOverride = Color.White,
                    OuterColorOverride = new Color(100, 180, 255),
                    StartThickness = 2f,
                    EndThickness = 2f,
                    GlowIntensity = 0.2f,
                    GlowScale = 0.1f
                };
                LightningUtils.InitializeBetweenPoints(lightning, firstRock.Center, secondRock.Center);
                SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.5f, Pitch = Main.rand.NextFloat(0.3f, 1f) });
            }
            Projectile.Center = firstRock.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 2;
            if (age++ % 3 == 0)
                DisplaceOffsets(lightning.SegmentOffsets, 0, lightning.MaxSegments - 1, 1f);
            UpdateLightning();
            if (age % 6 == 0)
                LightningUtils.SpawnDust(lightning);
        }

        private void UpdateLightning()
        {
            LightningUtils.InitializeBetweenPoints(lightning, firstRock.Center, secondRock.Center);
            Vector2 direction = (secondRock.Center - firstRock.Center).SafeNormalize(Vector2.UnitX);
            Vector2 normal = new(-direction.Y, direction.X);
            float amplitude = Math.Min(7f, lightning.DistanceToTarget * 0.05f);
            for (int i = 1; i < lightning.MaxSegments - 1; i++)
                lightning.SegmentPositions[i] += normal * (lightning.SegmentOffsets[i] * amplitude);
        }

        private static void DisplaceOffsets(float[] offsets, int first, int last, float amplitude)
        {
            if (last - first < 2) return;
            int middle = (first + last) / 2;
            offsets[middle] = (offsets[first] + offsets[last]) * 0.5f + Main.rand.NextFloat(-amplitude, amplitude);
            DisplaceOffsets(offsets, first, middle, amplitude * 0.7f);
            DisplaceOffsets(offsets, middle, last, amplitude * 0.7f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (lightning != null && Connected)
            {
                UpdateLightning();
                LightningUtils.DrawTaperedLightning(lightning, Main.spriteBatch);
            }
            return false;
        }

        public override void OnKill(int timeLeft) => lightning?.StrokeRenderer?.Dispose();
    }

    public class MagneticRockExplosion : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public int timer
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public override void SetStaticDefaults() => Main.projFrames[Projectile.type] = 7;
        public float alphaPercent = 0;
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.timeLeft = 200;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => timer < 4;

        public override void AI()
        {
            if (timer == 0)
                Projectile.rotation = Main.rand.NextFloat(6.28f);

            alphaPercent = Math.Clamp(MathHelper.Lerp(alphaPercent, -0.2f, 0.08f), 0, 1);

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 3)
            {
                if (Projectile.frame == 6)
                    Projectile.active = false;

                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }

            Lighting.AddLight(Projectile.Center, Color.SkyBlue.ToVector3() * alphaPercent * 0.4f);

            SkillStrikeUtil.setSkillStrike(Projectile, 1.5f);
            timer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Mod.Assets.Request<Texture2D>("Assets/Anim/BlueFlareDarkGlowPMA").Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Vector2 scale = new Vector2(1f, 1f);
            Color glowColor = Color.Aquamarine;
            glowColor.A = 0;
            Color whiteColor = Color.White;
            whiteColor.A = 0;
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.Black * 0.4f, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, whiteColor, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
