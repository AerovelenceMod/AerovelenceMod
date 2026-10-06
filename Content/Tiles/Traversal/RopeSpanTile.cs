
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
        public override bool CreateDust(int i, int j, ref int type)
        {
            if (this is RopeBridgeDeckTile) return true;
            RopeSpan span = RopeSpanSystem.AtTile(new Point(i, j));
            if (span == null) return false;
            type = RopeSpanSystem.RopeDustType(RopeSpanSystem.RopeTileType(span.RopeType));
            return type >= 0;
        }
        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;
        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak) => false;
        public override bool CanDrop(int i, int j) => false;
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            noItem = true;
            if (!fail && !effectOnly) RopeSpanSystem.BreakAt(new Point(i, j));
        }
        public override bool RightClick(int i, int j)
        {
            if (!Main.LocalPlayer.releaseUseTile) return false;
            if (RopeSpanSystem.IsRope(Main.LocalPlayer.HeldItem))
                return RopeSpanSystem.AtTile(new Point(i, j)) is RopeSpan span && RopeSpanSystem.ClickSpan(span.Id);
            return this is ZiplineRopeTile && Main.LocalPlayer.GetModPlayer<RopeSpanPlayer>().TryRideAt(Main.SmartCursorIsUsed ? Main.MouseWorld : new Vector2(i * 16 + 8, j * 16 + 8));
        }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
            => RopeSpanSystem.IsRope(Main.LocalPlayer.HeldItem) || this is ZiplineRopeTile && Main.SmartCursorIsUsed;
    }

    public sealed class RopeBridgeDeckTile : RopeSpanTile { public override string Texture => "Terraria/Images/Tiles_" + TileID.WoodBlock; }
    public sealed class RopeBridgeRopeTile : RopeSpanTile { }
    public sealed class ZiplineRopeTile : RopeSpanTile { }
}
