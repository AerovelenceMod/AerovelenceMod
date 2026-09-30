using System;
using System.Collections.Generic;
using System.IO;
using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Content.Dusts;
using AerovelenceMod.Content.Items.Crafting;
using AerovelenceMod.Content.Items.Misc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AerovelenceMod.Content.NPCs.TownNPC.BabyCondurtleTownPet
{
    [AutoloadHead]
    public class BabyCondurtle : TranslatableModNPC
    {
        internal const byte FeedRequestPacket = 233;
        internal const byte FeedResultPacket = 234;
        private int feedCooldown;
        private int hatchTime;
        private bool hatchEffectsPlayed;
        internal void StartHatching() { hatchTime = 30; NPC.netUpdate = true; }
        private static ITownNPCProfile profile;
        private bool frightened;
        private int fearTime;
        private int dangerClock;
        private int shellProgress;
        private float walkClock;

        public override void Load()
        {
            LocalizationManager.RegisterTranslation("AerovelenceMod.BabyCondurtle.Feed", "Feed", "default");
            LocalizationManager.RegisterTranslation("AerovelenceMod.BabyCondurtle.Feed", "Alimentar", "es-ES");
            LocalizationManager.RegisterTranslation("AerovelenceMod.BabyCondurtle.Fed", "*Munch munch munch* ...prrr! This baby condurtle dropped you {0} silver coins!", "default");
            LocalizationManager.RegisterTranslation("AerovelenceMod.BabyCondurtle.Fed", "*Ñam ñam ñam*... ¡prrr! ¡Este bebé condurtle te dio {0} monedas de plata!", "default");
            if (!Main.dedServ) On_Main.DrawNPCHeadFriendly += DrawMapHead;
        }
        public override void Unload()
        {
            if (!Main.dedServ) On_Main.DrawNPCHeadFriendly -= DrawMapHead;
            profile = null;
        }
        private static void DrawMapHead(On_Main.orig_DrawNPCHeadFriendly orig, Entity entity, byte alpha, float scale, SpriteEffects effects, int head, float x, float y)
        {
            if (entity is NPC { ModNPC: BabyCondurtle }) effects ^= SpriteEffects.FlipHorizontally;
            orig(entity, alpha, scale, effects, head, x, y);
        }

        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Baby Condurtle", "A tiny, humming bundle of crystal and shell. Gentle petting keeps its little sparks happy.")
                .AddName(Language.Spanish, "Condurtle Bebé")
                .AddFlavor(Language.Spanish, "Un pequeño montoncito de cristal y caparazón que tararea. Las caricias suaves alegran sus pequeñas chispas.");
            Main.npcFrameCount[Type] = 11;
            NPCID.Sets.ExtraFramesCount[Type] = 4;
            NPCID.Sets.AttackFrameCount[Type] = 0;
            NPCID.Sets.DangerDetectRange[Type] = 240;
            NPCID.Sets.AttackType[Type] = -1;
            NPCID.Sets.AttackTime[Type] = -1;
            NPCID.Sets.IsTownPet[Type] = true;
            NPCID.Sets.CannotSitOnFurniture[Type] = true;
            NPCID.Sets.NPCFramingGroup[Type] = 8;
            NPCID.Sets.HatOffsetY[Type] = 1;
            NPCID.Sets.ShimmerTownTransform[Type] = false;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Shimmer] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.PlayerDistanceWhilePetting[Type] = 28;
            NPCID.Sets.IsPetSmallForPetting[Type] = true;
            NPCID.Sets.TownNPCBestiaryPriority.Add(Type);
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Velocity = 0.5f, Direction = 1 });
            if (!Main.dedServ)
                profile = new BabyCondurtleProfile(Texture, HeadTexture);
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 30;
            NPC.height = 22;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 0;
            NPC.defense = 12;
            NPC.lifeMax = 250;
            NPC.knockBackResist = 0.35f;
            NPC.HitSound = SoundID.NPCHit1 with { Volume = 0.45f, Pitch = 0.5f };
            NPC.DeathSound = SoundID.NPCDeath6 with { Volume = 0.4f };
            NPC.housingCategory = 1;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.Add(BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface);
            base.SetBestiary(database, bestiaryEntry);
        }

        public override bool CanTownNPCSpawn(int numTownNPCs) => BabyCondurtleWorld.Unlocked;
        public override ITownNPCProfile TownNPCProfile() => profile;
        public override List<string> SetNPCNameList() => new() { "Pebble", "Pip", "Bubbles", "Nibbles", "Mica", "Pudding", "Lil' Turty", "Semi", "Shellshock", "King Turt", "Shelly", "Shellsea", "Sparky", "Scamper", "Tessie", "Ohm", "Snap", "Gurt", "Watt", "Connie Turty" };
        public override string GetChat()
        {
            Main.npcChatCornerItem = FindCorn(Main.LocalPlayer) >= 0 ? ModContent.ItemType<CrystalCornItem>() : 0;
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.15f, Pitch = -0.45f }, NPC.Center);
            return Main.rand.Next(5) switch { 0 => "Prrr...", 1 => "Mrrp!", 2 => "Chirp, chirp!", 3 => "Brrup?", _ => "Hmmm... prrr!" };
        }
        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Terraria.Localization.Language.GetTextValue("UI.PetTheAnimal");
            bool hasCorn = FindCorn(Main.LocalPlayer) >= 0;
            button2 = hasCorn ? LocalizationManager.GetTranslation("AerovelenceMod.BabyCondurtle.Feed") : "";
            Main.npcChatCornerItem = hasCorn ? ModContent.ItemType<CrystalCornItem>() : 0;
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shop)
        {
            if (firstButton) return;
            int slot = FindCorn(Main.LocalPlayer);
            if (slot < 0) return;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(FeedRequestPacket);
                packet.Write((short)NPC.whoAmI);
                packet.Write((byte)slot);
                packet.Send();
            }
            else Feed(Main.myPlayer, NPC.whoAmI, slot);
        }

        private static int FindCorn(Player player)
        {
            int type = ModContent.ItemType<CrystalCornItem>();
            for (int slot = 0; slot < 50; slot++)
                if (player.inventory[slot].type == type && player.inventory[slot].stack > 0) return slot;
            return -1;
        }

        internal static void Feed(int sender, int npcIndex, int slot)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || sender < 0 || sender >= Main.maxPlayers || npcIndex < 0 || npcIndex >= Main.maxNPCs || slot < 0 || slot >= 50) return;
            Player player = Main.player[sender];
            NPC pet = Main.npc[npcIndex];
            if (!player.active || player.dead || !pet.active || pet.ModNPC is not BabyCondurtle baby || baby.feedCooldown > 0 || Vector2.DistanceSquared(player.Center, pet.Center) > 240 * 240) return;
            Item corn = player.inventory[slot];
            if (corn.type != ModContent.ItemType<CrystalCornItem>() || corn.stack <= 0) return;
            int silver = Main.rand.Next(50, 91);
            int itemIndex = Item.NewItem(pet.GetSource_GiftOrReward(), pet.Hitbox, ItemID.SilverCoin, silver, noBroadcast: true);
            if (itemIndex < 0 || itemIndex >= Main.maxItems) return;
            baby.feedCooldown = 30;
            if (--corn.stack == 0) corn.TurnToAir();
            Main.item[itemIndex].playerIndexTheItemIsReservedFor = sender;
            Main.item[itemIndex].noGrabDelay = 0;
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemIndex);
                NetMessage.SendData(MessageID.ItemOwner, -1, -1, null, itemIndex);
                NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, sender, slot);
                ModPacket packet = baby.Mod.GetPacket();
                packet.Write(FeedResultPacket);
                packet.Write((short)npcIndex);
                packet.Write((byte)sender);
                packet.Write((byte)silver);
                packet.Send();
            }
            else ShowFeed(npcIndex, sender, silver);
        }

        internal static void ShowFeed(int npcIndex, int playerIndex, int silver)
        {
            if (Main.dedServ || npcIndex < 0 || npcIndex >= Main.maxNPCs || Main.npc[npcIndex].ModNPC is not BabyCondurtle) return;
            NPC pet = Main.npc[npcIndex];
            if (!pet.active) return;
            SoundEngine.PlaySound(SoundID.Item2 with { Volume = .6f, Pitch = .5f }, pet.Center);
            for (int i = 0; i < 6; i++)
                Dust.NewDustPerfect(pet.Top + Main.rand.NextVector2Circular(14, 6), ModContent.DustType<GenericSparkle>(), new Vector2(Main.rand.NextFloat(-.6f, .6f), -1.2f));
            if (playerIndex == Main.myPlayer && Main.LocalPlayer.talkNPC == npcIndex)
                Main.npcChatText = string.Format(LocalizationManager.GetTranslation("AerovelenceMod.BabyCondurtle.Fed"), silver);
        }

        public override bool PreAI()
        {
            if (hatchTime > 0)
            {
                if (!hatchEffectsPlayed)
                {
                    BabyCondurtleEgg.HatchEffects(NPC);
                    hatchEffectsPlayed = true;
                }
                hatchTime--;
                NPC.velocity.X *= .7f;
                if (hatchTime == 0 && Main.netMode != NetmodeID.MultiplayerClient) NPC.netUpdate = true;
                return false;
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                feedCooldown = Math.Max(0, feedCooldown - 1);
                if (fearTime > 0)
                    fearTime--;
                if (++dangerClock >= 30)
                {
                    dangerClock = 0;
                    foreach (NPC other in Main.ActiveNPCs)
                    {
                        if (other.whoAmI == NPC.whoAmI || other.friendly || other.damage <= 0 || other.dontTakeDamage)
                            continue;
                        if (Vector2.DistanceSquared(NPC.Center, other.Center) < 240f * 240f && Collision.CanHitLine(NPC.position, NPC.width, NPC.height, other.position, other.width, other.height))
                        {
                            fearTime = Math.Max(fearTime, 90);
                            break;
                        }
                    }
                }
                bool next = fearTime > 0;
                if (next != frightened)
                {
                    frightened = next;
                    NPC.netUpdate = true;
                }
            }
            shellProgress = Math.Clamp(shellProgress + (frightened ? 1 : -1), 0, 18);
            if (shellProgress > 0)
            {
                NPC.velocity.X *= 0.7f;
                if (Math.Abs(NPC.velocity.X) < 0.1f)
                    NPC.velocity.X = 0f;
                return false;
            }
            return true;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                fearTime = 150;
                frightened = true;
                NPC.netUpdate = true;
            }
        }
        public override void SendExtraAI(BinaryWriter writer) { writer.Write(frightened); writer.Write((byte)shellProgress); writer.Write((byte)hatchTime); }
        public override void ReceiveExtraAI(BinaryReader reader) { frightened = reader.ReadBoolean(); shellProgress = Math.Clamp((int)reader.ReadByte(), 0, 18); hatchTime = Math.Clamp((int)reader.ReadByte(), 0, 30); }

        internal static int ShellFrame(int progress, bool emerging) => emerging ? 10 : 7 + Math.Clamp((progress - 1) / 6, 0, 2);
        private float ShellDrop => NPC.IsABestiaryIconDummy ? 0f : 2f * MathHelper.Clamp((shellProgress - 6f) / 6f, 0f, 1f);
        public override void FindFrame(int frameHeight)
        {
            NPC.spriteDirection = NPC.direction;
            int frame = 0;
            if (shellProgress > 0 && !NPC.IsABestiaryIconDummy)
            {
                frame = ShellFrame(shellProgress, !frightened);
                walkClock = 0f;
            }
            else if (Math.Abs(NPC.velocity.X) > 0.1f)
            {
                walkClock += Math.Clamp(Math.Abs(NPC.velocity.X), 0.35f, 1.5f);
                frame = 1 + (int)(walkClock / 6f) % 6;
            }
            else
                walkClock = 0f;
            NPC.frame.Y = frame * frameHeight;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glowmask").Value;
            Rectangle frame = new(0, Math.Clamp(NPC.frame.Y / 30, 0, 10) * 30, 42, 30);
            Vector2 position = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY + 6f + ShellDrop);
            Vector2 origin = new(21f, 30f);
            float emergence = 1 - hatchTime / 30f;
            Vector2 hatchScale = new(1 + MathF.Sin(emergence * MathHelper.Pi) * .12f, MathHelper.SmoothStep(.25f, 1, emergence));
            Vector2 scale = hatchScale * NPC.scale;
            float opacity = MathHelper.Clamp(emergence * 4, 0, 1);
            SpriteEffects flip = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(texture, position, frame, drawColor * opacity, NPC.rotation, origin, scale, flip, 0f);
            float pulse = 0.6f + MathF.Sin(Main.GlobalTimeWrappedHourly * 2f + NPC.whoAmI) * 0.15f;
            spriteBatch.Draw(glow, position, frame, Color.White * .8f * opacity, NPC.rotation, origin, scale, flip, 0f);
            spriteBatch.Draw(glow, position, frame, new Color(90, 180, 255, 0) * pulse * .4f * opacity, NPC.rotation, origin, scale, flip, 0f);
            return false;
        }

        public override void PartyHatPosition(ref Vector2 position, ref SpriteEffects spriteEffects)
        {
            position.X += shellProgress > 0 ? 0 : 11f * NPC.spriteDirection;
            position.Y += 5f + ShellDrop;
        }
    }

    internal sealed class BabyCondurtleProfile : ITownNPCProfile
    {
        private readonly Asset<Texture2D> texture;
        private readonly int head;
        internal BabyCondurtleProfile(string path, string headPath)
        {
            texture = ModContent.Request<Texture2D>(path);
            head = ModContent.GetModHeadSlot(headPath);
        }
        public int RollVariation() => 0;
        public string GetNameForVariant(NPC npc) => npc.getNewNPCName();
        public Asset<Texture2D> GetTextureNPCShouldUse(NPC npc) => texture;
        public int GetHeadTextureIndex(NPC npc) => head;
    }

    public class BabyCondurtleWorld : ModSystem
    {
        public static bool Unlocked;
        public override void ClearWorld() => Unlocked = false;
        public override void SaveWorldData(TagCompound tag) => tag["BabyCondurtleUnlocked"] = Unlocked;
        public override void LoadWorldData(TagCompound tag) => Unlocked = tag.GetBool("BabyCondurtleUnlocked");
        public override void NetSend(BinaryWriter writer) => writer.Write(Unlocked);
        public override void NetReceive(BinaryReader reader) => Unlocked = reader.ReadBoolean();
        public override void OnWorldUnload() => Unlocked = false;
    }
}
