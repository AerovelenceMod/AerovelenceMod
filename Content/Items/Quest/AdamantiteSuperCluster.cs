using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Quest
{
    public class AdamantiteSuperCluster : RareOreCluster
    {
        public override int RewardTier => 10;
        protected override int SpriteWidth => 34;
        protected override int SpriteHeight => 36;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Adamantite Super Cluster", EnglishTooltip)
                .AddName(Language.Spanish, "Supercúmulo de adamantita")
                .AddTooltip(Language.Spanish, SpanishTooltip);
            base.SetStaticDefaults();
        }
    }
}
