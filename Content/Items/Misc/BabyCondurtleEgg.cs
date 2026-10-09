using System;
using System.Collections.Generic;
using AerovelenceMod.Content.NPCs.CrystalCaverns;
using AerovelenceMod.Content.NPCs.TownNPC.BabyCondurtleTownPet;
using Terraria.Audio;

namespace AerovelenceMod.Content.Items.Misc
{
    public class BabyCondurtleEgg : TranslatableModItem
    {
        internal const byte HatchPacket = 230;
        private const string Description = "Hatches a Baby Condurtle town pet\nPermanently welcomes it to this world; it can share a home with a townsperson\nOccasionally laid by living Condurtles";
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Baby Condurtle Egg", Description)
                .AddName(Language.Spanish, "Huevo de Condurtle Bebé")
                .AddTooltip(Language.Spanish, "Hace nacer a un Condurtle Bebé como mascota del pueblo\nLo acoge permanentemente en este mundo; puede compartir casa con un habitante\nLos Condurtles vivos ponen estos huevos de vez en cuando");
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 16;
            Item.height = 18;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarities.EarlyPHM;
            Item.value = Item.sellPrice(silver: 20);
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = Item.useAnimation = 45;
            Item.consumable = true;
            Item.UseSound = SoundID.Dig with { Volume = .4f, Pitch = .45f };
        }
        public override bool CanUseItem(Player player) => !BabyCondurtleWorld.Unlocked;
        public override bool ConsumeItem(Player player) => Main.netMode == NetmodeID.SinglePlayer;
        public override bool? UseItem(Player player)
        {
            if (!player.ItemAnimationJustStarted || player.whoAmI != Main.myPlayer || BabyCondurtleWorld.Unlocked)
                return false;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(HatchPacket);
                packet.Send();
            }
            else
                Hatch(player);
            return true;
        }
        internal static void ReceiveHatch(int sender)
        {
            if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers || BabyCondurtleWorld.Unlocked)
                return;
            Player player = Main.player[sender];
            if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<BabyCondurtleEgg>() || player.HeldItem.stack <= 0)
                return;
            if (!Hatch(player))
                return;
            player.HeldItem.stack--;
            if (player.HeldItem.stack <= 0)
                player.HeldItem.TurnToAir();
            NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, sender, player.selectedItem);
        }
        private static bool Hatch(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || BabyCondurtleWorld.Unlocked)
                return false;
            int type = ModContent.NPCType<BabyCondurtle>();
            if (!NPC.AnyNPCs(type))
            {
                int index = NPC.NewNPC(player.GetSource_ItemUse(player.HeldItem), (int)player.Center.X, (int)player.Bottom.Y, type);
                if (index >= Main.maxNPCs) return false;
                if (index < Main.maxNPCs)
                {
                    NPC pet = Main.npc[index];
                    ((BabyCondurtle)pet.ModNPC).StartHatching();
                    pet.homeless = true;
                    pet.GivenName = pet.getNewNPCName();
                    pet.netUpdate = true;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index);
                }
            }
            BabyCondurtleWorld.Unlocked = true;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.WorldData);
            return true;
        }

        internal static void HatchEffects(NPC pet)
        {
            if (Main.dedServ) return;
            SoundEngine.PlaySound(SoundID.Grass with { Volume = .65f, Pitch = .25f }, pet.Center);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = .55f, Pitch = .6f }, pet.Center);
            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = new(Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-3.5f, -.8f));
                Dust dust = Dust.NewDustPerfect(pet.Bottom + new Vector2(Main.rand.NextFloat(-10, 10), -6), DustID.Bone, velocity, 30, new Color(210, 225, 235), Main.rand.NextFloat(.65f, 1.1f));
                dust.noLight = true;
            }
            for (int i = 0; i < 6; i++)
                Dust.NewDustPerfect(pet.Bottom - Vector2.UnitY * 4, DustID.Smoke, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -.5f), 150, Color.LightGray, .65f);
        }
    }

    public class CondurtleEggLaying : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        private int cooldown = 3600;
        private int rollClock;
        public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.ModNPC is Condurtle;
        public override void PostAI(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || npc.life <= 0 || npc.IsABestiaryIconDummy)
                return;
            if (cooldown > 0)
            {
                cooldown--;
                return;
            }
            if (++rollClock < 60)
                return;
            rollClock = 0;
            if (!Collision.SolidCollision(npc.BottomLeft + new Vector2(2f, 1f), npc.width - 4, 3) || npc.lavaWet || !Main.rand.NextBool(180))
                return;
            int eggType = ModContent.ItemType<BabyCondurtleEgg>();
            foreach (Item item in Main.ActiveItems)
                if (item.type == eggType && Vector2.DistanceSquared(item.Center, npc.Center) < 320f * 320f)
                    return;
            int index = Item.NewItem(npc.GetSource_FromAI(), new Rectangle((int)npc.Center.X - npc.direction * 16 - 8, (int)npc.Bottom.Y - 18, 16, 18), eggType);
            if (index >= 0 && index < Main.maxItems)
            {
                Main.item[index].velocity = new Vector2(-npc.direction * 0.8f, -1.5f);
                Main.item[index].noGrabDelay = 45;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index);
                cooldown = 18000;
            }
        }
    }
}
