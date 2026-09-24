using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria.GameContent;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Accessories.SmallAccessories
{
    public class PlatinumHook : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("PlatinumHook", "Extends all gem hooks and upgrades them to glow")
            .AddName(Language.Default, "Platinum Hook").AddTooltip(Language.Default, "Extends all gem hooks and upgrades them to glow")
            .AddName(Language.Spanish, "Gancho de Platino").AddTooltip(Language.Spanish, "Extiende todos los ganchos de gema y los hace brillar")
            .AddName(Language.French, "Crochet en Platine").AddTooltip(Language.French, "Étend tous les crochets en gemmes et les fait briller")
            .AddName(Language.German, "Platin-Haken").AddTooltip(Language.German, "Erweitert alle Edelsteinhaken und lässt sie leuchten")
            .AddName(Language.Italian, "Gancio di Platino").AddTooltip(Language.Italian, "Estende tutti i ganci di gemme e li fa brillare")
            //.AddName(Language.Polish, "Platynowy Hak").AddTooltip(Language.Polish, "Wydłuża wszystkie haki z klejnotami i sprawia, że świecą")
            //.AddName(Language.PortugueseBrazil, "Gancho de Platina").AddTooltip(Language.PortugueseBrazil, "Estende todos os ganchos de gema e os faz brilhar")
            .AddName(Language.Russian, "Платиновый Крюк").AddTooltip(Language.Russian, "Удлиняет все крюки с драгоценными камнями и заставляет их светиться");
            //.AddName(Language.ChineseTraditional, "白金鉤爪").AddTooltip(Language.ChineseTraditional, "延長所有寶石鉤並讓它們發光")
            //.AddName(Language.ChineseSimplified, "白金钩爪").AddTooltip(Language.ChineseSimplified, "延长所有宝石钩并让它们发光");
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
            => player.GetModPlayer<PlatinumHookPlayer>().Equipped = true;

        public override void Load() => IL_Projectile.AI_007_GrapplingHooks += ExtendGemRange;
        public override void Unload() => IL_Projectile.AI_007_GrapplingHooks -= ExtendGemRange;

        private static void ExtendGemRange(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdcI4(230), i => i.MatchSub(),
                i => i.MatchLdcI4(30), i => i.MatchMul(), i => i.MatchAdd()))
                throw new InvalidOperationException("Platinum Hook could not find the gem hook range calculation.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<int, Projectile, int>>(ApplyRange);
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdcI4(420)))
                throw new InvalidOperationException("Platinum Hook could not find the amber hook range.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<int, Projectile, int>>(ApplyRange);
        }

        private static int ApplyRange(int range, Projectile projectile)
            => PlatinumHookProjectile.IsEnabled(projectile) ? range + 200 : range;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarities.EarlyPHM;
            Item.accessory = true;
        }
    }
    public class PlatinumHookPlayer : ModPlayer
    {
        internal bool Equipped;
        public override void ResetEffects() => Equipped = false;
        public override void UpdateDead() => Equipped = false;
    }

    public class PlatinumHookProjectile : GlobalProjectile
    {
        internal static bool IsGem(int type) => type >= ProjectileID.GemHookAmethyst && type <= ProjectileID.GemHookDiamond
            || type == ProjectileID.AmberHook;
        internal static bool IsEnabled(Projectile projectile) => IsGem(projectile.type) && projectile.owner >= 0
            && projectile.owner < Main.maxPlayers && Main.player[projectile.owner].active
            && Main.player[projectile.owner].GetModPlayer<PlatinumHookPlayer>().Equipped;

        private static Color GemColor(int type) => type switch
        {
            ProjectileID.GemHookAmethyst => new Color(195, 100, 255),
            ProjectileID.GemHookTopaz => new Color(255, 205, 75),
            ProjectileID.GemHookSapphire => new Color(90, 155, 255),
            ProjectileID.GemHookEmerald => new Color(90, 255, 155),
            ProjectileID.GemHookRuby => new Color(255, 95, 115),
            ProjectileID.AmberHook => new Color(255, 155, 65),
            _ => new Color(195, 235, 255)
        };

        public override void PostAI(Projectile projectile)
        {
            if (Main.dedServ || !IsEnabled(projectile)) return;
            Vector2 end = Main.player[projectile.owner].MountedCenter;
            Vector3 light = GemColor(projectile.type).ToVector3() * 0.6f;
            int steps = Math.Max(1, (int)(Vector2.Distance(projectile.Center, end) / 24f));
            for (int i = 0; i <= steps; i++)
                Lighting.AddLight(Vector2.Lerp(projectile.Center, end, i / (float)steps), light);
        }

        public override void PostDraw(Projectile projectile, Color lightColor)
        {
            if (!IsEnabled(projectile)) return;
            Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
            Color color = GemColor(projectile.type) with { A = 0 };
            Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, null, color,
                projectile.rotation, texture.Size() * 0.5f, projectile.scale, SpriteEffects.None);
        }
    }
}