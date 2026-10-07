using System;
using System.Collections.Generic;
using Terraria.Graphics.Effects;
using Terraria.Utilities;

namespace AerovelenceMod.Common.Systems
{
    /// <summary>Turns captured bits of the scene into pixelated shards that fall away</summary>
    public sealed class CrackShardSystem : ModSystem
    {
        /// <summary>Caps active shards and queued breaks so the effect does not get out of hand</summary>
        private const int MaxShards = 96;
        private readonly record struct BreakRequest(Vector2 Origin, CrackFragment Fragment, int Seed);
        private static readonly Queue<BreakRequest> pending = new();
        private static readonly List<Shard> shards = new();
        private static RenderTarget2D sceneCopy, shardTarget;
        private static BasicEffect effect;
        private const float CameraDistance = 480f;
        private static readonly Shard[] drawOrder = new Shard[MaxShards];
        private static readonly IComparer<Shard> depthOrder = Comparer<Shard>.Create((a, b) => (a.Age * b.Lifetime).CompareTo(b.Age * a.Lifetime));
        private static RasterizerState scissors;
        private static VertexDeclaration meshDeclaration;
        private static bool capturing;
        private PixelationSystem pixelation;
        private bool registered;
        /// <summary>One captured chunk, its mesh, and the movement that carries it away</summary>
        private sealed class Shard : IDisposable
        {
            public readonly Texture2D Texture;
            public readonly VertexBuffer Vertices;
            public readonly IndexBuffer Indices;
            public readonly CrackFragment Fragment;
            public readonly Rectangle Bounds;
            public readonly float Spin, Tumble, Phase;
            public Vector2 Position, Velocity;
            public float Rotation;
            public int Age;
            public readonly int Lifetime;
            public Shard(Texture2D texture, BreakRequest request, Rectangle bounds)
            {
                Texture = texture;
                Fragment = request.Fragment;
                Bounds = bounds;
                UnifiedRandom random = new(request.Seed);
                Position = request.Origin + bounds.Center.ToVector2();
                Vector2 outward = request.Fragment.Center.SafeNormalize(Vector2.UnitY);
                Velocity = outward * random.NextFloat(.6f, 1.8f) + new Vector2(random.NextFloat(-.5f, .5f), random.NextFloat(-2f, -.5f));
                Spin = random.NextFloat(.005f, .014f) * (random.NextBool() ? 1f : -1f);
                Tumble = random.NextFloat(.012f, .025f);
                Phase = random.NextFloat(MathHelper.TwoPi);
                Lifetime = random.Next(150, 210);
                (Vertices, Indices) = BuildMesh(texture.GraphicsDevice, request.Fragment, bounds);
            }
            public void Update()
            {
                Age++;
                Velocity.X *= .996f;
                Velocity.Y += .055f;
                Position += Velocity;
                Rotation += Spin;
            }
            public void Dispose()
            {
                Texture.Dispose();
                Vertices.Dispose();
                Indices.Dispose();
            }
        }
        private readonly struct ShardVertex : IVertexType
        {
            public readonly Vector3 Position, Normal;
            public readonly Vector2 TextureCoordinate;
            public readonly Color Color;
            public static VertexDeclaration Declaration => meshDeclaration ??= new(
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(32, VertexElementFormat.Color, VertexElementUsage.Color, 0));
            VertexDeclaration IVertexType.VertexDeclaration => Declaration;
            public ShardVertex(Vector3 position, Vector3 normal, Vector2 uv)
            {
                Position = position;
                Normal = normal;
                TextureCoordinate = uv;
                Color = normal.Z == 0f ? new Color(140, 148, 173) : Color.White;
            }
        }
        /// <summary>Gives each pixel chunk front and back faces plus a little thickness around exposed edges</summary>
        private static (VertexBuffer, IndexBuffer) BuildMesh(GraphicsDevice device, CrackFragment fragment, Rectangle bounds)
        {
            List<ShardVertex> vertices = new();
            List<ushort> indices = new();
            Vector2 size = bounds.Size(), center = size * .5f;
            int width = bounds.Width / CrackUtil.PixelSize, height = bounds.Height / CrackUtil.PixelSize;
            bool[,] cells = new bool[width, height];
            foreach (Rectangle span in fragment.Spans)
            {
                Vector2 start = new(span.X - bounds.X, span.Y - bounds.Y);
                Vector2 end = start + span.Size();
                AddQuad(new(start.X, start.Y, 2f), new(end.X, start.Y, 2f), new(end.X, end.Y, 2f), new(start.X, end.Y, 2f), Vector3.UnitZ);
                AddQuad(new(start.X, start.Y, -2f), new(start.X, end.Y, -2f), new(end.X, end.Y, -2f), new(end.X, start.Y, -2f), -Vector3.UnitZ);
                for (int y = (int)start.Y / CrackUtil.PixelSize; y < (int)end.Y / CrackUtil.PixelSize; y++)
                    for (int x = (int)start.X / CrackUtil.PixelSize; x < (int)end.X / CrackUtil.PixelSize; x++) cells[x, y] = true;
            }
            for (int side = 0; side < 4; side++)
            {
                bool horizontal = side < 2;
                int lines = horizontal ? height : width, length = horizontal ? width : height;
                for (int line = 0; line < lines; line++)
                {
                    int run = -1;
                    for (int offset = 0; offset <= length; offset++)
                    {
                        int x = horizontal ? offset : line, y = horizontal ? line : offset;
                        bool edge = offset < length && Filled(x, y) && !Filled(x + (side == 2 ? -1 : side == 3 ? 1 : 0), y + (side == 0 ? -1 : side == 1 ? 1 : 0));
                        if (edge && run < 0) run = offset;
                        if (edge || run < 0) continue;
                        float a = run * CrackUtil.PixelSize, b = offset * CrackUtil.PixelSize;
                        float c = (line + (side == 1 || side == 3 ? 1 : 0)) * CrackUtil.PixelSize;
                        Vector2 start = horizontal ? new(a, c) : new(c, a);
                        Vector2 end = horizontal ? new(b, c) : new(c, b);
                        if (side == 1 || side == 2) (start, end) = (end, start);
                        Vector3 normal = new Vector3(end.Y - start.Y, start.X - end.X, 0f);
                        normal.Normalize();
                        AddQuad(new(start, -2f), new(end, -2f), new(end, 2f), new(start, 2f), normal);
                        run = -1;
                    }
                }
            }
            VertexBuffer vertexBuffer = new(device, ShardVertex.Declaration, vertices.Count, BufferUsage.WriteOnly);
            IndexBuffer indexBuffer = new(device, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices.ToArray());
            indexBuffer.SetData(indices.ToArray());
            return (vertexBuffer, indexBuffer);
            bool Filled(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && cells[x, y];
            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                ushort first = (ushort)vertices.Count;
                AddVertex(a); AddVertex(b); AddVertex(c); AddVertex(d);
                indices.Add(first); indices.Add((ushort)(first + 1)); indices.Add((ushort)(first + 2));
                indices.Add(first); indices.Add((ushort)(first + 2)); indices.Add((ushort)(first + 3));
                void AddVertex(Vector3 point)
                {
                    Vector2 uv = new Vector2(point.X, point.Y) - new Vector2(normal.X, normal.Y) * .5f;
                    if (normal.Z == 0f) uv = Vector2.Clamp(uv, new Vector2(.5f), size - new Vector2(.5f));
                    uv /= size;
                    vertices.Add(new ShardVertex(point - new Vector3(center, 0f), normal, uv));
                }
            }
        }
        /// <summary>Queues a nearby fragment for scene capture, the actual shard is created during drawing</summary>
        /// <param name="origin">World-space offset applied to the fragment coordinates</param>
        /// <param name="fragment">The opening chunk to break away</param>
        /// <param name="seed">Random seed for its motion, spin, and lifetime</param>
        public static void Break(Vector2 origin, CrackFragment fragment, int seed)
        {
            if (Main.dedServ || fragment == null || fragment.Spans.Count == 0 || pending.Count >= MaxShards || Main.screenWidth <= 0) return;
            Vector2 screen = Vector2.Transform(origin + fragment.Center - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix);
            if (screen.X < -96f || screen.Y < -96f || screen.X > Main.screenWidth + 96f || screen.Y > Main.screenHeight + 96f) return;
            pending.Enqueue(new BreakRequest(origin, fragment, seed));
            if (capturing) return;
            capturing = true;
            Filters.Scene.OnPostDraw += FinishCapture;
        }
        public override void Load()
        {
            if (!Main.dedServ) On_Main.DrawProjectiles += CaptureScene;
        }
        public override void Unload()
        {
            On_Main.DrawProjectiles -= CaptureScene;
            pixelation?.UnregisterPersistentRenderAction(RenderLayer.OverPlayers, Draw);
            Clear();
            CrackUtil.ClearTextures();
            RasterizerState previous = scissors;
            BasicEffect previousEffect = effect;
            VertexDeclaration previousDeclaration = meshDeclaration;
            scissors = null;
            effect = null;
            meshDeclaration = null;
            Main.QueueMainThreadAction(() => { previous?.Dispose(); previousEffect?.Dispose(); previousDeclaration?.Dispose(); });
        }
        public override void OnWorldUnload()
        {
            Clear();
            CrackUtil.ClearTextures();
        }
        public override void PostUpdateEverything()
        {
            if (Main.dedServ) return;
            if (Main.gameMenu) { Clear(); return; }
            if (!registered)
            {
                pixelation = ModContent.GetInstance<PixelationSystem>();
                registered = pixelation.RegisterPersistentRenderAction(RenderLayer.OverPlayers, () => shards.Count > 0, Draw);
            }
            for (int i = shards.Count - 1; i >= 0; i--)
            {
                Shard shard = shards[i];
                shard.Update();
                if (shard.Age < shard.Lifetime) continue;
                shards.RemoveAt(i);
                Main.QueueMainThreadAction(shard.Dispose);
            }
            if (shards.Count == 0 && pending.Count == 0 && (sceneCopy != null || shardTarget != null))
            {
                RenderTarget2D previous = sceneCopy, previousTarget = shardTarget;
                sceneCopy = null;
                shardTarget = null;
                Main.QueueMainThreadAction(() => { previous?.Dispose(); previousTarget?.Dispose(); });
            }
        }
        /// <summary>Unhooks the capture callback once every queued break has been handled</summary>
        private static void FinishCapture()
        {
            if (pending.Count > 0) return;
            Filters.Scene.OnPostDraw -= FinishCapture;
            capturing = false;
        }
        /// <summary>Drops pending breaks and queuees shard resources for cleanup on the main thread</summary>
        private static void Clear()
        {
            if (pending.Count == 0 && shards.Count == 0 && sceneCopy == null && shardTarget == null && !capturing) return;
            pending.Clear();
            if (capturing) Filters.Scene.OnPostDraw -= FinishCapture;
            capturing = false;
            Shard[] previousShards = shards.ToArray();
            RenderTarget2D previousScene = sceneCopy, previousTarget = shardTarget;
            shards.Clear();
            sceneCopy = null;
            shardTarget = null;
            Main.QueueMainThreadAction(() =>
            {
                foreach (Shard shard in previousShards) shard.Dispose();
                previousScene?.Dispose();
                previousTarget?.Dispose();
            });
        }
        /// <summary>Copies the scene before projectiles are drawn, then cuts out the queued fragments</summary>
        private static void CaptureScene(On_Main.orig_DrawProjectiles orig, Main self)
        {
            if (pending.Count > 0 && !Main.gameMenu && !Main.mapFullscreen)
            {
                GraphicsDevice device = Main.instance.GraphicsDevice;
                RenderTargetBinding[] bindings = device.GetRenderTargets();
                Viewport viewport = device.Viewport;
                Rectangle scissor = device.ScissorRectangle;
                Matrix view = Main.GameViewMatrix.TransformationMatrix;
                Texture2D source;
                Texture2D backbuffer = null;
                if (bindings.Length > 0)
                    source = (Texture2D)bindings[0].RenderTarget;
                else
                {
                    Color[] pixels = new Color[viewport.Width * viewport.Height];
                    device.GetBackBufferData(pixels);
                    backbuffer = new Texture2D(device, viewport.Width, viewport.Height);
                    backbuffer.SetData(pixels);
                    source = backbuffer;
                }
                try
                {
                    if (sceneCopy == null || sceneCopy.Width != source.Width || sceneCopy.Height != source.Height)
                    {
                        sceneCopy?.Dispose();
                        sceneCopy = new RenderTarget2D(device, source.Width, source.Height, false, SurfaceFormat.Color,
                            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                    }
                    device.SetRenderTarget(sceneCopy);
                    device.Clear(Color.Transparent);
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                    Main.spriteBatch.Draw(source, Vector2.Zero, Color.White);
                    Main.spriteBatch.End();
                    while (pending.TryDequeue(out BreakRequest request))
                    {
                        Rectangle bounds = request.Fragment.Spans[0];
                        foreach (Rectangle span in request.Fragment.Spans) bounds = Rectangle.Union(bounds, span);
                        Texture2D texture = CaptureFragment(device, Main.spriteBatch, sceneCopy, view,
                            request.Origin - Main.screenPosition, request.Fragment, bounds);
                        if (shards.Count >= MaxShards)
                        {
                            shards[0].Dispose();
                            shards.RemoveAt(0);
                        }
                        shards.Add(new Shard(texture, request, bounds));
                    }
                }
                finally
                {
                    device.SetRenderTargets(bindings);
                    device.Viewport = viewport;
                    device.ScissorRectangle = scissor;
                    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                    Main.spriteBatch.Draw(sceneCopy, Vector2.Zero, Color.White);
                    Main.spriteBatch.End();
                    backbuffer?.Dispose();
                    FinishCapture();
                }
            }
            orig(self);
        }
        /// <summary>Clips the scene to the fragment spans so each shard carries its own piece of the view</summary>
        private static Texture2D CaptureFragment(GraphicsDevice device, SpriteBatch spriteBatch, Texture2D scene,
            Matrix view, Vector2 origin, CrackFragment fragment, Rectangle bounds)
        {
            scissors ??= new RasterizerState { CullMode = CullMode.None, ScissorTestEnable = true };
            RenderTarget2D texture = new(device, bounds.Width, bounds.Height, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            device.SetRenderTarget(texture);
            device.Clear(Color.Transparent);
            Matrix transform = Matrix.Invert(view) * Matrix.CreateTranslation(-origin.X - bounds.X, -origin.Y - bounds.Y, 0f);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
                DepthStencilState.None, scissors, null, transform);
            foreach (Rectangle span in fragment.Spans)
            {
                device.ScissorRectangle = new Rectangle(span.X - bounds.X, span.Y - bounds.Y, span.Width, span.Height);
                spriteBatch.Draw(scene, Vector2.Zero, Color.White);
            }
            spriteBatch.End();
            return texture;
        }
        /// <summary>Renders shards at half resolution, then scales them up for the pixelated look</summary>
        private static void Draw()
        {
            if (shards.Count == 0) return;
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Matrix view = Main.GameViewMatrix.EffectMatrix;
            RenderTargetBinding[] bindings = device.GetRenderTargets();
            Viewport viewport = device.Viewport;
            Rectangle scissor = device.ScissorRectangle;
            Main.spriteBatch.End();
            try
            {
                int width = (viewport.Width + 1) / 2, height = (viewport.Height + 1) / 2;
                if (shardTarget == null || shardTarget.Width != width || shardTarget.Height != height)
                {
                    shardTarget?.Dispose();
                    shardTarget = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color,
                        DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
                }
                if (effect == null)
                {
                    effect = new BasicEffect(device)
                    {
                        TextureEnabled = true,
                        VertexColorEnabled = true,
                        LightingEnabled = true,
                        AmbientLightColor = new Vector3(.6f, .6f, .68f),
                        SpecularColor = new Vector3(.7f, .85f, 1f),
                        SpecularPower = 48f,
                        View = Matrix.CreateTranslation(0f, 0f, -CameraDistance)
                    };
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(.35f, .55f, -.75f));
                    effect.DirectionalLight0.DiffuseColor = new Vector3(.4f);
                    effect.DirectionalLight0.SpecularColor = Vector3.One;
                }
                device.SetRenderTarget(shardTarget);
                device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Transparent, 1f, 0);
                device.BlendState = BlendState.AlphaBlend;
                device.DepthStencilState = DepthStencilState.Default;
                device.RasterizerState = view.Determinant() < 0f ? RasterizerState.CullClockwise : RasterizerState.CullCounterClockwise;
                device.SamplerStates[0] = SamplerState.PointClamp;
                Matrix screen = Matrix.CreateScale(1f / width, -1f / height, 1f) * Matrix.CreateTranslation(-1f, 1f, 0f);
                float extent = 1f / CameraDistance;
                Matrix perspective = Matrix.CreatePerspectiveOffCenter(-extent, extent, -extent, extent, 1f, 1000f);
                shards.CopyTo(drawOrder);
                Array.Sort(drawOrder, 0, shards.Count, depthOrder);
                for (int i = 0; i < shards.Count; i++)
                {
                    Shard shard = drawOrder[i];
                    float progress = shard.Age / (float)shard.Lifetime;
                    effect.Alpha = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((progress - .75f) / .25f, 0f, 1f));
                    effect.Texture = shard.Texture;
                    effect.World = Matrix.CreateRotationX(shard.Age * shard.Tumble * (.45f + .35f * MathF.Sin(shard.Phase)))
                        * Matrix.CreateRotationY(shard.Age * shard.Tumble) * Matrix.CreateRotationZ(shard.Rotation)
                        * Matrix.CreateTranslation(0f, 0f, CameraDistance * .62f * MathF.Pow(progress, .85f));
                    Vector2 position = shard.Position - Main.screenPosition;
                    effect.Projection = perspective * Matrix.CreateTranslation(position.X, position.Y, 0f) * view * screen;
                    device.SetVertexBuffer(shard.Vertices);
                    device.Indices = shard.Indices;
                    effect.CurrentTechnique.Passes[0].Apply();
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, shard.Vertices.VertexCount, 0, shard.Indices.IndexCount / 3);
                }
            }
            finally
            {
                Array.Clear(drawOrder);
                device.SetVertexBuffer(null);
                device.Indices = null;
                device.SetRenderTargets(bindings);
                device.Viewport = viewport;
                device.ScissorRectangle = scissor;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone);
                if (shardTarget != null) Main.spriteBatch.Draw(shardTarget, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null, view);
            }
        }
    }
}
