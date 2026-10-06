
using AerovelenceMod.Common.Systems.Traversal;



using Terraria.GameContent.ObjectInteractions;



namespace AerovelenceMod.Content.Tiles.Traversal
{
    public abstract class RopeSpanTile : ModTile
    {
        public override string Texture => "Terraria/Images/Tiles_" + TileID.Rope;
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            DustType = DustID.WoodFurniture;
            AddMapEntry(new Color(145, 113, 75), this.Localize(Name == nameof(ZiplineRopeTile) ? "Zipline" : "Rope Bridge"));
        }
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;
        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => false;
        public override bool CanDrop(int i, int j) => false;
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            noItem = true;
            if (!fail && !effectOnly) RopeSpanSystem.BreakAt(new Point(i, j));
        }
        public override bool RightClick(int i, int j) => Main.LocalPlayer.GetModPlayer<RopeSpanPlayer>().TryRideAt(Main.SmartCursorIsUsed ? Main.MouseWorld : new Vector2(i * 16 + 8, j * 16 + 8));
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => this is ZiplineRopeTile && Main.SmartCursorIsUsed;
    }

    public sealed class RopeBridgeDeckTile : RopeSpanTile { public override string Texture => "Terraria/Images/Tiles_" + TileID.WoodBlock; }
    public sealed class RopeBridgeRopeTile : RopeSpanTile { }
    public sealed class ZiplineRopeTile : RopeSpanTile { }
}
