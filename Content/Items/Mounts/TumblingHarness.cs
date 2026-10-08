namespace AerovelenceMod.Content.Items.Mounts
{
    public class TumblingHarness : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            const string englishTooltip = "Summons a rideable tumblerock\nHold Up while grounded to conjure an electric ramp; keep holding to loop\nNegates fall damage while mounted";
            this.ModifyLocalization("Tumbling Harness", englishTooltip)
                .AddName(Language.Default, "Tumbling Harness").AddTooltip(Language.Default, englishTooltip)
                .AddName(Language.Spanish, "Arnés Rodante").AddTooltip(Language.Spanish, "Invoca una roca rodante que puedes montar\nMantén pulsado Arriba mientras estás en el suelo para conjurar una rampa eléctrica; sigue pulsando para hacer bucles\nAnula el daño por caída mientras montas");
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.width = 32;
            Item.height = 32;
            Item.useTime = Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.UseSound = SoundID.Item79;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.Master;
            Item.master = true;
            Item.mountType = ModContent.MountType<TumblingMount>();
        }
    }

    public class TumblingMountBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.mount.SetMount(ModContent.MountType<TumblingMount>(), player);
            player.buffTime[buffIndex] = 10;
        }
    }
}
