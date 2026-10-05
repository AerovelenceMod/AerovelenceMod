using AerovelenceMod.Common.Globals.Worlds;
using AerovelenceMod.Content.Items.BossSummons;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.Localization;

namespace AerovelenceMod.Common.Systems
{
    public class ModIntegrationsSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            DoBossChecklistIntegration();
        }

        private void DoBossChecklistIntegration()
        {
            if (ModLoader.TryGetMod("BossChecklist", out Mod bossChecklistMod))
            {
                string tumblerInternalName = "CrystalTumbler";
                float tumblerWeight = 2.8f;
                Func<bool> tumblerDowned = () => DownedWorld.DownedCrystalTumbler;
                int tumblerBossType = ModContent.NPCType<Content.NPCs.Bosses.CrystalTumbler.CrystalTumbler>();
                int tumblerSpawnItem = ModContent.ItemType<Content.Items.BossSummons.CrystalKey>();
                List<int> tumblerCollectibles = new List<int>()
                {
                    ModContent.ItemType<Content.Items.Accessories.Boss.PrismaticSoul>()
                };
                LocalizedText tumblerDisplayName = Terraria.Localization.Language.GetText("Mods.AerovelenceMod.NPCs.CrystalTumbler.DisplayName");
                LocalizedText tumblerSpawnInfo = Terraria.Localization.Language.GetText("Mods.AerovelenceMod.NPCs.CrystalTumbler.SpawnInfo").WithFormatArgs("[i:" + ModContent.ItemType<CrystalKey>() + "]");
                Action<SpriteBatch, Rectangle, Color> tumblerPortrait = (SpriteBatch spriteBatch, Rectangle rect, Color color) =>
                {
                    Texture2D texture = ModContent.Request<Texture2D>("AerovelenceMod/Content/NPCs/Bosses/CrystalTumbler/CrystalTumbler").Value;
                    Rectangle frame = texture.Frame(1, 2, 0, 0);
                    Vector2 centered = rect.Center.ToVector2();
                    spriteBatch.Draw(texture, centered, frame, color, 0f, frame.Size() / 2f, 1f, SpriteEffects.None, 0f);
                    Texture2D eyeTexture = ModContent.Request<Texture2D>("AerovelenceMod/Content/NPCs/Bosses/CrystalTumbler/CrystalTumbler_Eye", AssetRequestMode.ImmediateLoad).Value;
                    Rectangle eyeFrame = eyeTexture.Frame(1, 2, 0, 0);
                    spriteBatch.Draw(eyeTexture, centered, eyeFrame, Color.White, 0f, eyeFrame.Size() / 2f, 1f, SpriteEffects.None, 0f);

                };
                bossChecklistMod.Call(
                    "LogBoss",
                    Mod,
                    tumblerInternalName,
                    tumblerWeight,
                    tumblerDowned,
                    tumblerBossType,
                    new Dictionary<string, object>()
                    {
                        ["spawnItems"] = tumblerSpawnItem,
                        ["collectibles"] = tumblerCollectibles,
                        ["spawnInfo"] = tumblerSpawnInfo,
                        ["displayName"] = tumblerDisplayName,
                        ["customPortrait"] = tumblerPortrait
                    }
                );

                string cyvercryInternalName = "Cyvercry";
                float cyvercryWeight = 12.3f;
                Func<bool> cyvercryDowned = () => DownedWorld.DownedCyvercry;
                int cyvercryBossType = ModContent.NPCType<Content.NPCs.Bosses.Cyvercry.Cyvercry>();
                int cyvercrySpawnItem = ModContent.ItemType<Content.Items.BossSummons.ObsidianEye>();
                List<int> cyvercryCollectibles = new List<int>()
                {
                    ModContent.ItemType<Content.Items.Accessories.Boss.EnergyShield>()
                };
                LocalizedText cyvercrySpawnInfo = Terraria.Localization.Language.GetText("Mods.AerovelenceMod.NPCs.Cyvercry.SpawnInfo").WithFormatArgs("[i:" + ModContent.ItemType<ObsidianEye>() + "]");
                bossChecklistMod.Call(
                    "LogBoss",
                    Mod,
                    cyvercryInternalName,
                    cyvercryWeight,
                    cyvercryDowned,
                    cyvercryBossType,
                    new Dictionary<string, object>()
                    {
                        ["spawnItems"] = cyvercrySpawnItem,
                        ["collectibles"] = cyvercryCollectibles,
                        ["spawnInfo"] = cyvercrySpawnInfo

                    }
                );
            }
        }
    }
}
