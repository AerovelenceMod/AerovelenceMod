using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AerovelenceMod.Common.Systems.Gas;

internal sealed class GasFieldRenderer : IDisposable
{
    private readonly GasRenderData data = new();
    private readonly GraphicsDevice device;
    private readonly Effect effect;
    private readonly RenderTargetBinding[][] targetBindings =
    {
        Array.Empty<RenderTargetBinding>(),
        new RenderTargetBinding[1],
        new RenderTargetBinding[2],
        new RenderTargetBinding[3],
        new RenderTargetBinding[4]
    };
    private readonly RenderTargetBinding[] rendererBinding = new RenderTargetBinding[1];
    private Texture2D optical;
    private Texture2D appearance;
    private Texture2D environment;
    private RenderTarget2D colors;

    public GasFieldRenderer(GraphicsDevice device, Effect effect)
    {
        this.device = device;
        this.effect = effect;
    }

    public void Draw(GasFluid field, bool tileCollision, float blend, bool additive, Vector2 camera, Vector2 minimum, Vector2 maximum,
        Func<int, int, Color> lighting, Func<int, int, GasTileShape> terrain, Matrix transform, float pixelSpacing = 2f)
    {
        data.Build(field, tileCollision, blend, camera, minimum, maximum, lighting, terrain, pixelSpacing);
        if (data.ChunkCount == 0)
            return;
        EnsureTextures();
        Rectangle upload = new(0, 0, data.SampleWidth, data.UsedRows * GasRenderData.SampleSide);
        int count = upload.Width * upload.Height;
        optical.SetData(0, upload, data.Optical, 0, count);
        appearance.SetData(0, upload, data.Appearance, 0, count);
        environment.SetData(0, upload, data.Environment, 0, count);
        effect.Parameters["OpticalTexture"].SetValue(optical);
        effect.Parameters["AppearanceTexture"].SetValue(appearance);
        effect.Parameters["EnvironmentTexture"].SetValue(environment);
        effect.Parameters["SampleTexel"].SetValue(new Vector2(1f / optical.Width, 1f / optical.Height));
        effect.Parameters["VertexTexel"].SetValue(new Vector2(1f / colors.Width, 1f / colors.Height));
        effect.Parameters["Subdivision"].SetValue((float)data.Subdivision);
        effect.Parameters["CellPixels"].SetValue((float)field.CellPixels);
        effect.Parameters["Additive"].SetValue(additive ? 1f : 0f);
        effect.Parameters["TileCollision"].SetValue(tileCollision ? 1f : 0f);
        int targetCount = device.GetRenderTargetsNoAllocEXT(null);
        RenderTargetBinding[] targets = targetBindings[targetCount];
        device.GetRenderTargetsNoAllocEXT(targets);
        Viewport viewport = device.Viewport;
        Rectangle scissor = device.ScissorRectangle;
        try
        {
            device.Textures[3] = null;
            device.SetRenderTargets(rendererBinding);
            device.Clear(Color.Transparent);
            device.BlendState = BlendState.Opaque;
            effect.Parameters["MatrixTransform"].SetValue(Matrix.CreateOrthographicOffCenter(0f, colors.Width, colors.Height, 0f, -1f, 1f));
            effect.CurrentTechnique = effect.Techniques["GasReconstruct"];
            Draw(data.ReconstructionVertices);
        }
        finally
        {
            device.SetRenderTargets(targets);
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
        }
        device.BlendState = BlendState.AlphaBlend;
        effect.Parameters["VertexTexture"].SetValue(colors);
        effect.Parameters["MatrixTransform"].SetValue(transform);
        effect.CurrentTechnique = effect.Techniques["GasComposite"];
        Draw(data.CompositeVertices);
    }

    private void Draw(GasRenderVertex[] vertices)
    {
        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, data.ChunkCount * 4, data.Indices, 0, data.ChunkCount * 2);
        }
    }

    private void EnsureTextures()
    {
        if (optical is not null && !optical.IsDisposed && optical.Height == data.SampleHeight)
            return;
        Dispose();
        optical = new Texture2D(device, data.SampleWidth, data.SampleHeight, false, SurfaceFormat.Vector4);
        appearance = new Texture2D(device, data.SampleWidth, data.SampleHeight, false, SurfaceFormat.Vector4);
        environment = new Texture2D(device, data.SampleWidth, data.SampleHeight, false, SurfaceFormat.Color);
        colors = new RenderTarget2D(device, data.VertexWidth, data.VertexHeight, false, SurfaceFormat.Color, DepthFormat.None);
        rendererBinding[0] = new RenderTargetBinding(colors);
    }

    public void Dispose()
    {
        for (int i = 0; i < 5; i++)
            if (device.Textures[i] is Texture texture && (texture == optical || texture == appearance || texture == environment || texture == colors))
                device.Textures[i] = null;
        optical?.Dispose();
        appearance?.Dispose();
        environment?.Dispose();
        colors?.Dispose();
        optical = null;
        appearance = null;
        environment = null;
        colors = null;
    }
}
