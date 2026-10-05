using System;
using System.Diagnostics;
using AerovelenceMod.Common.Systems;





namespace AerovelenceMod.Common.Systems.Gas;

public sealed class GasRenderer : ModSystem
{
    private readonly GasRenderMesh mesh = new();
    private BasicEffect effect;
    private Action persistentDraw;
    private bool alphaRegistered;

    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;
        persistentDraw ??= DrawGas;
        if (!alphaRegistered)
            alphaRegistered = ModContent.GetInstance<PixelationSystem>().RegisterPersistentRenderAction(RenderLayer.BeforeSolidTiles, HasGas, persistentDraw);
        if (!Main.gameMenu && GasPerfControl.LightingEnabled)
        {
            long start = Stopwatch.GetTimestamp();
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
            GasPerfControl.UpdateLighting(start);
        }
    }

    private static bool HasGas()
    {
        if (Main.gameMenu || !GasPerfControl.RenderingEnabled)
            return false;
        GasSystem system = ModContent.GetInstance<GasSystem>();
        return system.HasAnyGas;
    }

    private void DrawGas()
    {
        long start = Stopwatch.GetTimestamp();
        DrawGas(false);
        DrawGas(true);
        GasPerfControl.UpdateRender(start);
    }

    private void DrawGas(bool additive)
    {
        GasSystem system = ModContent.GetInstance<GasSystem>();
        GasFluid colliding = additive ? system.AdditiveColliding : system.Colliding;
        GasFluid free = additive ? system.AdditiveFree : system.Free;
        if (colliding is null)
            return;
        SpriteBatch spriteBatch = Main.spriteBatch;
        spriteBatch.End();
        GraphicsDevice device = Main.instance.GraphicsDevice;
        effect ??= new BasicEffect(device) { VertexColorEnabled = true, TextureEnabled = false };
        BlendState blend = device.BlendState;
        DepthStencilState depth = device.DepthStencilState;
        RasterizerState rasterizer = device.RasterizerState;
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.World = Matrix.Identity;
            effect.View = Main.GameViewMatrix.EffectMatrix;
            effect.Projection = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, -1f, 1f);
            DrawField(colliding, true, system.RenderBlend, additive);
            DrawField(free, false, system.RenderBlend, additive);
            DrawField(system.VisualFields[additive ? 2 : 0], true, system.RenderBlend, additive);
            DrawField(system.VisualFields[additive ? 3 : 1], false, system.RenderBlend, additive);
        }
        finally
        {
            device.BlendState = blend;
            device.DepthStencilState = depth;
            device.RasterizerState = rasterizer;
            spriteBatch.Begin(default, default, Main.DefaultSamplerState, default, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);
        }
    }

    private void DrawField(GasFluid field, bool tileCollision, float blend, bool additive)
    {
        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
        Vector2 viewSize = screenSize / Math.Max(0.25f, Math.Min(zoom.X, zoom.Y));
        Vector2 minimum = (screenSize - viewSize) * 0.5f - new Vector2(16f);
        Vector2 maximum = (screenSize + viewSize) * 0.5f + new Vector2(16f);
        mesh.Build(field, tileCollision, blend, additive, Main.screenPosition, minimum, maximum, ReadLighting, 2f / Math.Max(1f, Math.Max(zoom.X, zoom.Y)));
        if (mesh.IndexCount == 0)
            return;
        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, mesh.Vertices, 0, mesh.VertexCount, mesh.Indices, 0, mesh.IndexCount / 3);
        }
    }

    private static Vector3 ReadLighting(int x, int y)
        => Lighting.GetColor(Math.Clamp(x, 0, Main.maxTilesX - 1), Math.Clamp(y, 0, Main.maxTilesY - 1)).ToVector3();

    private static void AddLights(GasFluid field, ref int budget)
    {
        if (field is null || budget <= 0)
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
            ModContent.GetInstance<PixelationSystem>().UnregisterPersistentRenderAction(RenderLayer.BeforeSolidTiles, persistentDraw);
        alphaRegistered = false;
        persistentDraw = null;
        BasicEffect old = effect;
        effect = null;
        Main.QueueMainThreadAction(() => old?.Dispose());
    }
}
