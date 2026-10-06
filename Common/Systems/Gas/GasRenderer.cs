using System;
using AerovelenceMod.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Systems.Gas;

public sealed class GasRenderer : ModSystem
{
    private GasFieldRenderer fieldRenderer;
    private Asset<Effect> effect;
    private readonly Texture[] textures = new Texture[5];
    private readonly SamplerState[] samplers = new SamplerState[5];
    private readonly Func<int, int, Color> readLighting = ReadLighting;
    private readonly Func<int, int, GasTileShape> readTerrain = ReadTerrain;
    private Action persistentDraw;
    private bool alphaRegistered;

    public override void Load()
    {
        if (!Main.dedServ)
            effect = ModContent.Request<Effect>("AerovelenceMod/Assets/Shaders/Gas", AssetRequestMode.AsyncLoad);
    }

    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;
        persistentDraw ??= DrawGas;
        if (!alphaRegistered)
            alphaRegistered = ModContent.GetInstance<NewPixelationSystem>().RegisterPersistentRenderAction(RenderLayer.BeforeSolidTiles, HasGas, persistentDraw);
        if (!Main.gameMenu)
        {
            GasSystem system = ModContent.GetInstance<GasSystem>();
            if (system.HasAnyGas)
            {
                int budget = 768;
                AddLights(system.Colliding, ref budget);
                AddLights(system.Free, ref budget);
                AddLights(system.AdditiveColliding, ref budget);
                AddLights(system.AdditiveFree, ref budget);
                foreach (GasFluid visual in system.VisualFields)
                    AddLights(visual, ref budget);
            }
        }
    }

    private static bool HasGas()
    {
        if (Main.gameMenu)
            return false;
        GasSystem system = ModContent.GetInstance<GasSystem>();
        return system.HasAnyGas;
    }

    private void DrawGas()
    {
        GasSystem system = ModContent.GetInstance<GasSystem>();
        if (!system.HasAnyGas)
            return;
        GraphicsDevice device = Main.instance.GraphicsDevice;
        fieldRenderer ??= new GasFieldRenderer(device, effect.Value);
        SpriteBatch spriteBatch = Main.spriteBatch;
        spriteBatch.End();
        BlendState blend = device.BlendState;
        DepthStencilState depth = device.DepthStencilState;
        RasterizerState rasterizer = device.RasterizerState;
        for (int i = 0; i < textures.Length; i++)
        {
            textures[i] = device.Textures[i];
            samplers[i] = device.SamplerStates[i];
        }
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            DrawField(system.Colliding, true, system.RenderBlend, false);
            DrawField(system.Free, false, system.RenderBlend, false);
            DrawField(system.VisualFields[0], true, system.RenderBlend, false);
            DrawField(system.VisualFields[1], false, system.RenderBlend, false);
            DrawField(system.AdditiveColliding, true, system.RenderBlend, true);
            DrawField(system.AdditiveFree, false, system.RenderBlend, true);
            DrawField(system.VisualFields[2], true, system.RenderBlend, true);
            DrawField(system.VisualFields[3], false, system.RenderBlend, true);
        }
        finally
        {
            device.BlendState = blend;
            device.DepthStencilState = depth;
            device.RasterizerState = rasterizer;
            for (int i = 0; i < textures.Length; i++)
            {
                device.Textures[i] = textures[i];
                device.SamplerStates[i] = samplers[i];
                textures[i] = null;
                samplers[i] = null;
            }
            spriteBatch.Begin(default, default, Main.DefaultSamplerState, default, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);
        }
    }

    private void DrawField(GasFluid field, bool tileCollision, float blend, bool additive)
    {
        if (field is null || field.ChunkCount == 0)
            return;
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
        Vector2 viewSize = screenSize / Math.Max(0.25f, Math.Min(zoom.X, zoom.Y));
        Vector2 minimum = (screenSize - viewSize) * 0.5f - new Vector2(16f);
        Vector2 maximum = (screenSize + viewSize) * 0.5f + new Vector2(16f);
        Matrix transform = Main.GameViewMatrix.EffectMatrix * Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -1f, 1f);
        fieldRenderer.Draw(field, tileCollision, blend, additive, Main.screenPosition, minimum, maximum, readLighting, readTerrain,
            transform, 2f / Math.Max(1f, Math.Max(zoom.X, zoom.Y)));
    }

    private static Color ReadLighting(int x, int y)
        => Lighting.GetColor(Math.Clamp(x, 0, Main.maxTilesX - 1), Math.Clamp(y, 0, Main.maxTilesY - 1));

    private static GasTileShape ReadTerrain(int x, int y)
    {
        if (!WorldGen.InWorld(x, y, 1))
            return GasTileShape.Solid;
        Tile tile = Main.tile[x, y];
        if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
            return GasTileShape.Empty;
        if (tile.IsHalfBlock)
            return GasTileShape.HalfBlock;
        return tile.Slope switch
        {
            SlopeType.SlopeDownRight => GasTileShape.DownRight,
            SlopeType.SlopeDownLeft => GasTileShape.DownLeft,
            SlopeType.SlopeUpRight => GasTileShape.UpRight,
            SlopeType.SlopeUpLeft => GasTileShape.UpLeft,
            _ => GasTileShape.Solid
        };
    }

    private static void AddLights(GasFluid field, ref int budget)
    {
        if (field is null || !field.HasLight || budget <= 0)
            return;
        float zoom = Math.Max(0.25f, Main.GameViewMatrix.Zoom.X);
        Vector2 view = new Vector2(Main.screenWidth, Main.screenHeight) / zoom;
        Vector2 minimum = Main.screenPosition + (new Vector2(Main.screenWidth, Main.screenHeight) - view) * 0.5f - new Vector2(128f);
        Vector2 maximum = minimum + view + new Vector2(256f);
        foreach (GasFluid.Chunk chunk in field.Chunks)
        {
            Vector2 origin = new Vector2(chunk.Position.X, chunk.Position.Y) * field.ChunkWorldSize;
            if (origin.X + field.ChunkWorldSize < minimum.X || origin.Y + field.ChunkWorldSize < minimum.Y || origin.X > maximum.X || origin.Y > maximum.Y)
                continue;
            int tiles = field.ChunkWorldSize / 16;
            for (int y = 0; y < tiles; y++)
                for (int x = 0; x < tiles; x++)
                {
                    int tileX = chunk.Position.X * tiles + x;
                    int tileY = chunk.Position.Y * tiles + y;
                    Vector3 light = field.TileLight(tileX, tileY);
                    if (light.LengthSquared() < 0.00001f)
                        continue;
                    Lighting.AddLight(new Vector2(tileX * 16 + 8, tileY * 16 + 8), light);
                    if (--budget <= 0)
                        return;
                }
        }
    }

    public override void Unload()
    {
        if (Main.dedServ)
            return;
        if (persistentDraw is not null)
            ModContent.GetInstance<NewPixelationSystem>().UnregisterPersistentRenderAction(RenderLayer.BeforeSolidTiles, persistentDraw);
        alphaRegistered = false;
        persistentDraw = null;
        GasFieldRenderer old = fieldRenderer;
        fieldRenderer = null;
        effect = null;
        Main.QueueMainThreadAction(() => old?.Dispose());
    }
}
