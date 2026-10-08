using System;
using System.Collections.Generic;
using Terraria.DataStructures;
using ReLogic.Content;

namespace AerovelenceMod.Content.Items.Tools
{
    public class SpeedstersPickaxe : TranslatableModItem
    {
        private const int BaseUseTime = 10;
        private const int MinUseTime = 4;
        internal const int MaxStacks = 40;
        private static Texture2D glowTexture;

        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("SpeedstersPickaxe", "Mining ramps up the speed of your pickaxe")
            .AddName(Language.Default, "Speedster's Pickaxe").AddTooltip(Language.Default, "Mining ramps up the speed of your pickaxe")
            .AddName(Language.Spanish, "Pico del Velocista").AddTooltip(Language.Spanish, "Minar aumenta la velocidad de tu pico")
            .AddName(Language.French, "Pioche du Sprinteur").AddTooltip(Language.French, "L'extraction augmente la vitesse de votre pioche")
            .AddName(Language.German, "Rasante Spitzhacke").AddTooltip(Language.German, "Bergbau steigert die Geschwindigkeit deiner Spitzhacke")
            .AddName(Language.Italian, "Piccone del Velocista").AddTooltip(Language.Italian, "L'estrazione aumenta la velocità del tuo piccone")
            //.AddName(Language.Polish, "Kilof Sprintera").AddTooltip(Language.Polish, "Kopanie zwiększa prędkość twojego kilofa")
            //.AddName(Language.PortugueseBrazil, "Picareta do Velocista").AddTooltip(Language.PortugueseBrazil, "Minerar acelera a velocidade da sua picareta")
            .AddName(Language.Russian, "Кирка Скоростного").AddTooltip(Language.Russian, "Добыча руды ускоряет вашу кирку");
            //.AddName(Language.ChineseTraditional, "疾速鎬").AddTooltip(Language.ChineseTraditional, "挖礦會逐步提升你的鎬速度")
            //.AddName(Language.ChineseSimplified, "疾速镐").AddTooltip(Language.ChineseSimplified, "挖矿会逐步提升你的镐速度");
        }

        public override void SetDefaults()
        {
            Item.crit = 4;
            Item.damage = 5;
            Item.DamageType = DamageClass.Melee;
            Item.width = 34;
            Item.height = 34;
            Item.useTime = BaseUseTime;
            Item.useAnimation = BaseUseTime * 3;

            Item.pick = 64;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarities.EarlyPHM;
            Item.autoReuse = true;
            Item.useTurn = true;
        }

        public override void Load()
        {
            if (Main.dedServ) return;
            Main.QueueMainThreadAction(() =>
            {
                Texture2D source = ModContent.Request<Texture2D>(Texture + "_Glow", AssetRequestMode.ImmediateLoad).Value;
                Color[] pixels = new Color[source.Width * source.Height];
                source.GetData(pixels);
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte value = Math.Max(pixels[i].R, Math.Max(pixels[i].G, pixels[i].B));
                    pixels[i] = new Color(value, value, value, pixels[i].A);
                }
                glowTexture = new Texture2D(Main.instance.GraphicsDevice, source.Width, source.Height);
                glowTexture.SetData(pixels);
            });
        }

        public override void Unload()
        {
            Texture2D texture = glowTexture;
            glowTexture = null;
            if (texture != null) Main.QueueMainThreadAction(texture.Dispose);
        }

        public override float UseSpeedMultiplier(Player player)
        {
            float charge = player.GetModPlayer<SpeedsterPlayer>().MiningStacks / (float)MaxStacks;
            return BaseUseTime / MathHelper.Lerp(BaseUseTime, MinUseTime, charge);
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            float charge = Main.LocalPlayer.GetModPlayer<SpeedsterPlayer>().MiningStacks / (float)MaxStacks;
            if (charge <= 0f || glowTexture == null) return;
            float pulse = 0.35f + MathF.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.08f;
            spriteBatch.Draw(glowTexture, position, frame, new Color(255, 135, 40, 0) * (charge * pulse), 0f, origin, scale, SpriteEffects.None, 0f);
        }

        public override bool ModifyItemDraw(ref PlayerDrawSet drawInfo, ref DrawData drawData, ref DrawData? coloredDrawData, ref DrawData? glowMaskDrawData)
        {
            SpeedsterPlayer player = drawInfo.drawPlayer.GetModPlayer<SpeedsterPlayer>();
            float charge = player.MiningStacks / (float)MaxStacks;
            if (charge <= 0f || glowTexture == null || drawInfo.shadow != 0f) return true;
            if (player.LastTrailUpdate != Main.GameUpdateCount)
            {
                DrawData sample = drawData;
                sample.texture = glowTexture;
                sample.position += Main.screenPosition;
                sample.shader = 0;
                player.Trail.Insert(0, sample);
                if (player.Trail.Count > 5) player.Trail.RemoveAt(5);
                player.LastTrailUpdate = Main.GameUpdateCount;
            }
            for (int i = player.Trail.Count - 1; i > 0; i--)
            {
                DrawData afterimage = player.Trail[i];
                afterimage.position -= Main.screenPosition;
                afterimage.color = new Color(255, 105, 25, 0) * (charge * (1f - i / 5f) * 0.35f);
                drawInfo.DrawDataCache.Add(afterimage);
            }
            DrawData glow = drawData;
            glow.texture = glowTexture;
            glow.color = new Color(255, 150, 55, 0) * (charge * 0.55f);
            glow.shader = 0;
            glowMaskDrawData = glow;
            return true;
        }
    }

    public class SpeedsterMining : GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (Main.dedServ || WorldGen.gen || fail || effectOnly || !Main.tileSolid[type]) return;
            Player player = Main.LocalPlayer;
            if (player.HeldItem.type != ModContent.ItemType<SpeedstersPickaxe>() || !player.controlUseItem || player.itemAnimation <= 0) return;
            if (i != Player.tileTargetX || j != Player.tileTargetY) return;
            SpeedsterPlayer modPlayer = player.GetModPlayer<SpeedsterPlayer>();
            modPlayer.MiningStacks = Math.Min(modPlayer.MiningStacks + 1, SpeedstersPickaxe.MaxStacks);
            modPlayer.MiningDecayTimer = 0;
        }
    }

    public class SpeedsterPlayer : ModPlayer
    {
        public int MiningStacks;
        public int MiningDecayTimer;
        internal readonly List<DrawData> Trail = new(5);
        internal ulong LastTrailUpdate = ulong.MaxValue;

        public override void PostUpdate()
        {
            bool swinging = Player.HeldItem.type == ModContent.ItemType<SpeedstersPickaxe>() && Player.itemAnimation > 0;
            if (!swinging || MiningStacks == 0) Trail.Clear();
            if (Player.dead)
            {
                MiningStacks = MiningDecayTimer = 0;
                Trail.Clear();
                return;
            }
            if (swinging && Player.controlUseItem)
            {
                MiningDecayTimer = 0;
                return;
            }
            if (MiningStacks > 0 && ++MiningDecayTimer >= 5)
            {
                MiningDecayTimer = 0;
                MiningStacks--;
            }
        }
    }
}
