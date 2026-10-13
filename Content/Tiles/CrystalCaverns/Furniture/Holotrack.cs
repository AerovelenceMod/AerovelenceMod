using AerovelenceMod.Common.Systems;
using Terraria.GameContent.ObjectInteractions;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural;
namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Furniture;
public sealed class HolotrackTile : ModTile
{
    public override string Texture => $"Terraria/Images/Tiles_{TileID.MinecartTrack}";
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileLighted[Type] = true;
        Main.tileObsidianKill[Type] = true;
        TileID.Sets.GeneralPlacementTiles[Type] = false;
        DustType = DustID.BlueCrystalShard;
        HitSound = SoundID.Item52;
        AddMapEntry(new Color(100, 205, 244), this.MapNameFromItem(ModContent.ItemType<HolotrackItem>()));
        RegisterItemDrop(ModContent.ItemType<HolotrackItem>());
        MinecartTrackUtils.Register(Type);
    }
    public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
    {
        Minecart.FrameTrack(i, j, false);
        return false;
    }
    public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
    public override void MouseOver(int i, int j)
    {
        Main.LocalPlayer.noThrow = 2;
        Main.LocalPlayer.cursorItemIconEnabled = true;
        Main.LocalPlayer.cursorItemIconID = ModContent.ItemType<HolotrackItem>();
    }
    public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (!fail && !effectOnly)
            HolotrackSystem.Remove(i, j);
    }
    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        if (!Main.tile[i, j].IsTileInvisible || Main.ShouldShowInvisibleWalls())
            HolotrackSystem.DrawTile(i, j, spriteBatch);
        return false;
    }
    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
    {
        float charge = HolotrackSystem.ChargeAt(i, j);
        r = .015f + charge * .1f;
        g = .05f + charge * .25f;
        b = .08f + charge * .35f;
    }
}
public sealed class HolotrackItem : TranslatableModItem
{
    public override string Texture => $"Terraria/Images/Item_{ItemID.MinecartTrack}";
    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Holotrack", "A rail that lights up as you ride it");
        Item.ResearchUnlockCount = 100;
    }
    public override void SetDefaults()
    {
        CommonItemHelper.SetupPlaceableItem(this, 28, 14, Item.buyPrice(copper: 5), ModContent.TileType<HolotrackTile>());
        Item.color = new Color(100, 205, 244);
    }
    public override void AddRecipes()
    {
        CreateRecipe(25).AddIngredient(ItemID.MinecartTrack, 25).AddIngredient<ChargedStoneItem>()
            .AddTile(TileID.WorkBenches).Register();
    }
}
