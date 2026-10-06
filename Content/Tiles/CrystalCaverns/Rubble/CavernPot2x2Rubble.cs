using AerovelenceMod.Content.NPCs.CrystalCaverns;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Furniture;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural;


using ReLogic.Content;
using System;
using System.Collections.Generic;

using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.RGB;

using Terraria.Localization;

using Terraria.ObjectData;
using Terraria.Utilities;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Rubble;

public class CavernPot2x2Rubble : ModTile
{
    private Asset<Texture2D> glowTexture;
    private int drawYOffset;

    public override void SetStaticDefaults()
    {
        Main.tileLighted[Type] = true;
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        Main.tileCut[Type] = true;

        Main.tileOreFinderPriority[Type] = 100;
        Main.tileSpelunker[Type] = true;

        TileID.Sets.DisableSmartCursor[Type] = true;

        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.StyleWrapLimit = 3;
        drawYOffset = 2;
        TileObjectData.newTile.AnchorInvalidTiles = [
            TileID.MagicalIceBlock,
            TileID.Boulder,
            TileID.BouncyBoulder,
            TileID.LifeCrystalBoulder,
            TileID.RollingCactus
        ];
        TileObjectData.addTile(Type);

        AddMapEntry(new Color(70, 70, 85), Terraria.Localization.Language.GetText("MapObject.Pot"));

        glowTexture = (Main.dedServ ? null : ModContent.Request<Texture2D>(Texture + "_Glowmask"));
    }

    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        Tile tile = Main.tile[i, j];

        Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);

        // Draw original texture
        spriteBatch.Draw(
            TextureAssets.Tile[Type].Value,
            new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y + drawYOffset) + zero,
            new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16),
            Lighting.GetColor(i, j), 0f, default, 1f, SpriteEffects.None, 0f);

        // Pulsating color for glowmask
        Color maskColor = Color.White
            * MathHelper.Lerp(0.1f, 1f, ((float)Math.Pow(Math.Sin(NoiseHelper.GetDynamicNoise(new Vector2(i * 0.2f, j * 0.2f), Main.GlobalTimeWrappedHourly * 0.2f)), 4)));

        // Draw glowmask
        spriteBatch.Draw(
            glowTexture.Value,
            new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y + drawYOffset) + zero,
            new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16),
            maskColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        // Return false to stop vanilla draw
        return false;
    }

    public override bool CreateDust(int i, int j, ref int type)
    {
        type = ModContent.DustType<Dusts.CavernPotDust>();
        return base.CreateDust(i, j, ref type);
    }

    public bool PreDropEffects(int i, int j)
    {
        return true;
    }

    public void DropGores(int i, int j, int frameX, int frameY)
    {
        if (!WorldGen.gen && !Main.dedServ)
        {
            for (int k = 0; k < 5; k++)
            {
                Gore.NewGore(new EntitySource_TileBreak(i, j), new Vector2(i * 16, j * 16), default(Vector2), Mod.Find<ModGore>($"CavernPotGore_" + k).Type);
            }
        }
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        bool drop = false;
        int width = 0;
        int height = j;
        for (width += Main.tile[i, j].TileFrameX / 18; width > 1; width -= 2) {
        }
        width *= -1;
        width += i;
        int style = Main.tile[i, j].TileFrameY / 18;
        int posY = 0;
        while (style > 1) {
            style -= 2;
            posY++;
        }
        height -= style;
        for (int k = width; k < width + 2; k++) {
            for (int l = height; l < height + 2; l++) {
                int posX;
                for (posX = Main.tile[k, l].TileFrameX / 18; posX > 1; posX -= 2) {
                }
                if (!Main.tile[k, l].HasTile || Main.tile[k, l].TileType != Type || posX != k - width || Main.tile[k, l].TileFrameY != (l - height) * 18 + posY * 36) {
                    drop = true;
                }
            }
            if (!WorldGen.SolidTile2(k, height + 2)) {
                drop = true;
            }
        }
        if (!drop) {
            return;
        }

        WorldGen.destroyObject = true;
        SoundEngine.PlaySound(SoundID.Shatter, new Vector2(i * 16, j * 16));
        for (int m = width; m < width + 2; m++) {
            for (int n = height; n < height + 2; n++) {
                if (Main.tile[m, n].TileType == Type && Main.tile[m, n].HasTile) {
                    WorldGen.KillTile(m, n);
                }
            }
        }

        if (Main.netMode != NetmodeID.Server)
            DropGores(i, j, frameX, frameY);

        if (Main.netMode != 1)
            SpawnLoot(i, j, width, height, 0);

        WorldGen.destroyObject = false;
    }

    private void SpawnLoot(int i, int j, int x2, int y2, int style)
    {
        UnifiedRandom genRand = WorldGen.genRand;

        float luckRange = 1f;
        luckRange = 1.75f;

        luckRange = (luckRange * 2f + 1f) / 3f;
        int range = (int)(500f / ((luckRange + 1f) / 2f));
        if (WorldGen.gen)
            return;

        if (Player.GetClosestRollLuck(i, j, range) == 0f) {
            if (Main.netMode != 1)
                Projectile.NewProjectile(new EntitySource_TileBreak(i, j), i * 16 + 16, j * 16 + 16, 0f, -12f, ProjectileID.CoinPortal, 0, 0f, Main.myPlayer);

            return;
        }

        if (genRand.Next(35) == 0 && Main.wallDungeon[Main.tile[i, j].WallType] && (double)j > Main.worldSurface) {
            Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, 327);
            return;
        }

        if (Main.getGoodWorld && genRand.Next(6) == 0) {
            Projectile.NewProjectile(new EntitySource_TileBreak(i, j), i * 16 + 16, j * 16 + 8, (float)Main.rand.Next(-100, 101) * 0.002f, 0f, ProjectileID.Bomb, 0, 0f, Main.myPlayer, 16f, 16f);
            return;
        }

        if (Main.remixWorld && Main.netMode != 1 && genRand.Next(5) == 0) {
            Player player = Main.player[Player.FindClosest(new Vector2(i * 16, j * 16), 16, 16)];
            if (Main.rand.Next(2) == 0) {
                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.FallenStar);
            }
            else if ((double)j > Main.rockLayer && j < Main.maxTilesY - 350) {
                int npc = -1;
                npc = NPC.NewNPC(new EntitySource_SpawnNPC(), x2 * 16 + 16, y2 * 16 + 32, ModContent.NPCType<CrystalSlime>());
                if (npc > -1) {
                    Main.npc[npc].netUpdate = true;
                }
            }
            else if ((double)j > Main.worldSurface && (double)j <= Main.rockLayer) {
                int npc = -1;
                npc = NPC.NewNPC(new EntitySource_SpawnNPC(), x2 * 16 + 16, y2 * 16 + 32, ModContent.NPCType<CaveSlime>());
                if (npc > -1) {
                    Main.npc[npc].netUpdate = true;
                }
            }
            else {
                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.FallenStar);
            }

            return;
        }

        if (Main.remixWorld && (double)i > (double)Main.maxTilesX * 0.37 && (double)i < (double)Main.maxTilesX * 0.63 && j > Main.maxTilesY - 220) {
            int stack = Main.rand.Next(20, 41);
            Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.Rope, stack);
            return;
        }

        if (genRand.Next(45) == 0 || (Main.rand.Next(45) == 0 && Main.expertMode)) {
            if ((double)j < Main.worldSurface) {
                int itemRand = genRand.Next(10);
                if (itemRand == 0)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.IronskinPotion);

                if (itemRand == 1)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.ShinePotion);

                if (itemRand == 2)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.NightOwlPotion);

                if (itemRand == 3)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.SwiftnessPotion);

                if (itemRand == 4)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.MiningPotion);

                if (itemRand == 5)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.CalmingPotion);

                if (itemRand == 6)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.BuilderPotion);

                if (itemRand >= 7)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.RecallPotion, genRand.Next(1, 3));
            }
            else if ((double)j < Main.rockLayer) {
                int itemRand = genRand.Next(12);
                if (itemRand == 0)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.RegenerationPotion);

                if (itemRand == 1)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.ShinePotion);

                if (itemRand == 2)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.NightOwlPotion);

                if (itemRand == 3)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.SwiftnessPotion);

                if (itemRand == 4)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.ArcheryPotion);

                if (itemRand == 5)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.GillsPotion);

                if (itemRand == 6)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.HunterPotion);

                if (itemRand == 7)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.MiningPotion);

                if (itemRand == 8)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.TrapsightPotion);

                if (itemRand >= 7)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.RecallPotion, genRand.Next(1, 3));
            }
            else if ((double)j < Main.UnderworldLayer) {
                int itemRand = genRand.Next(19);
                if (itemRand == 0)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.SpelunkerPotion);

                if (itemRand == 1)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.FeatherfallPotion);

                if (itemRand == 2)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.NightOwlPotion);

                if (itemRand == 3)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.WaterWalkingPotion);

                if (itemRand == 4)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.ArcheryPotion);

                if (itemRand == 5)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.GravitationPotion);

                if (itemRand == 6)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.ThornsPotion);

                if (itemRand == 7)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.WaterWalkingPotion);

                if (itemRand == 8)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.InvisibilityPotion);

                if (itemRand == 9)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.HunterPotion);

                if (itemRand == 10)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.MiningPotion);

                if (itemRand == 11)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.HeartreachPotion);

                if (itemRand == 12)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.FlipperPotion);

                if (itemRand == 13)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.TrapsightPotion);

                if (itemRand >= 7)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.RecallPotion, genRand.Next(1, 3));
            }
            else {
                int itemRand = genRand.Next(19);
                if (itemRand == 0)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.SpelunkerPotion);

                if (itemRand == 1)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.FeatherfallPotion);

                if (itemRand == 2)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.ManaRegenerationPotion);

                if (itemRand == 3)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.ObsidianSkinPotion);

                if (itemRand == 4)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.MagicPowerPotion);

                if (itemRand == 5)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.InvisibilityPotion);

                if (itemRand == 6)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.HunterPotion);

                if (itemRand == 7)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.GravitationPotion);

                if (itemRand == 8)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.ThornsPotion);

                if (itemRand == 9)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.WaterWalkingPotion);

                if (itemRand == 10)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.ObsidianSkinPotion);

                if (itemRand == 11)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.BattlePotion);

                if (itemRand == 12)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.HeartreachPotion);

                if (itemRand == 13)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.TitanPotion);

                if (genRand.Next(5) == 0)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.PotionOfReturn);
            }

            return;
        }

        if (Main.netMode == NetmodeID.Server && Main.rand.Next(30) == 0) {
            Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.WormholePotion);
            return;
        }

        int rand = Main.rand.Next(7);
        if (Main.expertMode)
            rand--;

        Player player2 = Main.player[Player.FindClosest(new Vector2(i * 16, j * 16), 16, 16)];
        int min = 0;
        int max = 20;
        for (int k = 0; k < 50; k++) {
            Item item = player2.inventory[k];
            if (!item.IsAir && item.createTile == TileID.Torches) {
                min += item.stack;
                if (min >= max)
                    break;
            }
        }

        bool light = min < max;
        if (rand == 0 && player2.statLife < player2.statLifeMax2) {
            Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.Heart);
            if (Main.rand.Next(2) == 0)
                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.Heart);

            if (Main.expertMode) {
                if (Main.rand.Next(2) == 0)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.Heart);

                if (Main.rand.Next(2) == 0)
                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.Heart);
            }

            return;
        }

        if (rand == 1 || (rand == 0 && light)) {
            int stack = Main.rand.Next(2, 7);
            if (Main.expertMode)
                stack += Main.rand.Next(1, 7);

            if (Main.tile[i, j].LiquidAmount > 0)
                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.Glowstick, stack);
            else
                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ModContent.ItemType<CrystalTorchItem>(), stack);

            return;
        }

        switch (rand) {
            case 2: {
                int stack = Main.rand.Next(10, 21);
                int item = ItemID.WoodenArrow;
                if ((double)j < Main.rockLayer && genRand.Next(2) == 0)
                    item = !Main.hardMode ? ItemID.Shuriken : ItemID.Grenade;

                if (j > Main.UnderworldLayer)
                    item = ItemID.HellfireArrow;
                else if (Main.hardMode)
                    item = (Main.rand.Next(2) != 0) ? ItemID.UnholyArrow : ((WorldGen.SavedOreTiers.Silver != 168) ? ItemID.SilverBullet : ItemID.TungstenBullet);

                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, item, stack);
                return;
            }
            case 3: {
                int item = ItemID.LesserHealingPotion;
                if (j > Main.UnderworldLayer || Main.hardMode)
                    item = ItemID.HealingPotion;

                int stack = 1;
                if (Main.expertMode && Main.rand.Next(3) != 0)
                    stack++;

                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, item, stack);
                return;
            }
            case 4:
                if (j < Main.UnderworldLayer) {

                    int stack = Main.rand.Next(4) + 1;
                    if (Main.expertMode)
                        stack += Main.rand.Next(4);

                    Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.Bomb, stack);
                    return;
                }
                break;
        }

        if ((rand == 4 || rand == 5) && j < Main.UnderworldLayer && !Main.hardMode) {
            int stack = Main.rand.Next(20, 41);
            Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.Rope, stack);
            return;
        }

        float valueMult = 200 + genRand.Next(-100, 101);
        if ((double)j < Main.worldSurface)
            valueMult *= 0.5f;
        else if ((double)j < Main.rockLayer)
            valueMult *= 0.75f;
        else if ((double)j > Main.UnderworldLayer)
            valueMult *= 1.25f;

        valueMult *= 1f + (float)Main.rand.Next(-20, 21) * 0.01f;
        if (Main.rand.Next(4) == 0)
            valueMult *= 1f + (float)Main.rand.Next(5, 11) * 0.01f;

        if (Main.rand.Next(8) == 0)
            valueMult *= 1f + (float)Main.rand.Next(10, 21) * 0.01f;

        if (Main.rand.Next(12) == 0)
            valueMult *= 1f + (float)Main.rand.Next(20, 41) * 0.01f;

        if (Main.rand.Next(16) == 0)
            valueMult *= 1f + (float)Main.rand.Next(40, 81) * 0.01f;

        if (Main.rand.Next(20) == 0)
            valueMult *= 1f + (float)Main.rand.Next(50, 101) * 0.01f;

        if (Main.expertMode)
            valueMult *= 2.5f;

        if (Main.expertMode && Main.rand.Next(2) == 0)
            valueMult *= 1.25f;

        if (Main.expertMode && Main.rand.Next(3) == 0)
            valueMult *= 1.5f;

        if (Main.expertMode && Main.rand.Next(4) == 0)
            valueMult *= 1.75f;

        valueMult *= luckRange;
        if (NPC.downedBoss1)
            valueMult *= 1.1f;

        if (NPC.downedBoss2)
            valueMult *= 1.1f;

        if (NPC.downedBoss3)
            valueMult *= 1.1f;

        if (NPC.downedMechBoss1)
            valueMult *= 1.1f;

        if (NPC.downedMechBoss2)
            valueMult *= 1.1f;

        if (NPC.downedMechBoss3)
            valueMult *= 1.1f;

        if (NPC.downedPlantBoss)
            valueMult *= 1.1f;

        if (NPC.downedQueenBee)
            valueMult *= 1.1f;

        if (NPC.downedGolemBoss)
            valueMult *= 1.1f;

        if (NPC.downedPirates)
            valueMult *= 1.1f;

        if (NPC.downedGoblins)
            valueMult *= 1.1f;

        if (NPC.downedFrost)
            valueMult *= 1.1f;

        while ((int)valueMult > 0) {
            if (valueMult > 1000000f) {
                int value = (int)(valueMult / 1000000f);
                if (value > 50 && Main.rand.Next(2) == 0)
                    value /= Main.rand.Next(3) + 1;

                if (Main.rand.Next(2) == 0)
                    value /= Main.rand.Next(3) + 1;

                valueMult -= (float)(1000000 * value);
                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.PlatinumCoin, value);
                continue;
            }

            if (valueMult > 10000f) {
                int stack2 = (int)(valueMult / 10000f);
                if (stack2 > 50 && Main.rand.Next(2) == 0)
                    stack2 /= Main.rand.Next(3) + 1;

                if (Main.rand.Next(2) == 0)
                    stack2 /= Main.rand.Next(3) + 1;

                valueMult -= (float)(10000 * stack2);
                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.GoldCoin, stack2);
                continue;
            }

            if (valueMult > 100f) {
                int stack2 = (int)(valueMult / 100f);
                if (stack2 > 50 && Main.rand.Next(2) == 0)
                    stack2 /= Main.rand.Next(3) + 1;

                if (Main.rand.Next(2) == 0)
                    stack2 /= Main.rand.Next(3) + 1;

                valueMult -= (float)(100 * stack2);
                Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.SilverCoin, stack2);
                continue;
            }

            int stack = (int)valueMult;
            if (stack > 50 && Main.rand.Next(2) == 0)
                stack /= Main.rand.Next(3) + 1;

            if (Main.rand.Next(2) == 0)
                stack /= Main.rand.Next(4) + 1;

            if (stack < 1)
                stack = 1;

            valueMult -= (float)stack;
            Item.NewItem(WorldGen.GetItemSource_FromTileBreak(i, j), i * 16, j * 16, 16, 16, ItemID.CopperCoin, stack);
        }
    }
}