using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FieldValue = System.Numerics.Vector4;

namespace AerovelenceMod.Common.Systems.Gas;

internal enum GasTileShape : byte
{
    Empty,
    Solid,
    HalfBlock,
    DownRight,
    DownLeft,
    UpRight,
    UpLeft
}

internal struct GasRenderVertex : IVertexType
{
    private static readonly VertexDeclaration declaration = new(
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2));

    public Vector2 Position;
    public Vector2 Local;
    public Vector2 SampleOrigin;
    public Vector2 VertexOrigin;
    VertexDeclaration IVertexType.VertexDeclaration => declaration;
}

internal sealed class GasRenderData
{
    public const int Columns = 8;
    public const int SampleSide = GasFluid.ChunkSize + 6;
    public const int VertexSide = GasFluid.ChunkSize * 4 + 1;
    private readonly FieldValue[] opticalScratch = new FieldValue[SampleSide * SampleSide];
    private readonly FieldValue[] appearanceScratch = new FieldValue[SampleSide * SampleSide];
    private readonly Color[] lightingScratch = new Color[12 * 12];
    public readonly GasRenderVertex[] ReconstructionVertices = new GasRenderVertex[GasFluid.MaxChunks * 4];
    public readonly GasRenderVertex[] CompositeVertices = new GasRenderVertex[GasFluid.MaxChunks * 4];
    public readonly int[] Indices = new int[GasFluid.MaxChunks * 6];
    public FieldValue[] Optical { get; private set; } = Array.Empty<FieldValue>();
    public FieldValue[] Appearance { get; private set; } = Array.Empty<FieldValue>();
    public Color[] Environment { get; private set; } = Array.Empty<Color>();
    public int Rows { get; private set; }
    public int ChunkCount { get; private set; }
    public int UsedRows => (ChunkCount + Columns - 1) / Columns;
    public int SampleWidth => Columns * SampleSide;
    public int SampleHeight => Rows * SampleSide;
    public int VertexWidth => Columns * VertexSide;
    public int VertexHeight => Rows * VertexSide;
    public int Subdivision { get; private set; }

    public GasRenderData()
    {
        for (int i = 0; i < GasFluid.MaxChunks; i++)
        {
            int vertex = i * 4, index = i * 6;
            Indices[index] = vertex;
            Indices[index + 1] = vertex + 1;
            Indices[index + 2] = vertex + 2;
            Indices[index + 3] = vertex + 1;
            Indices[index + 4] = vertex + 3;
            Indices[index + 5] = vertex + 2;
        }
    }

    public void Build(GasFluid field, bool tileCollision, float blend, Vector2 camera, Vector2 minimum, Vector2 maximum, Func<int, int, Color> lighting, Func<int, int, GasTileShape> terrain, float pixelSpacing = 2f)
    {
        ChunkCount = 0;
        if (field is null || field.ChunkCount == 0)
            return;
        Subdivision = field.CellPixels == 4 && pixelSpacing >= 2f ? 2 : 4;
        int side = GasFluid.ChunkSize * Subdivision + 1;
        float spacing = field.CellPixels / (float)Subdivision;
        if (field.FastUpdates)
            blend = 1f;
        foreach (GasFluid.Chunk chunk in field.Chunks)
        {
            Vector2 world = new Vector2(chunk.Position.X, chunk.Position.Y) * field.ChunkWorldSize;
            Vector2 position = world - camera;
            if (position.X + field.ChunkWorldSize < minimum.X || position.X > maximum.X
                || position.Y + field.ChunkWorldSize < minimum.Y || position.Y > maximum.Y)
                continue;
            float peak = 0f;
            int minSampleX = SampleSide, minSampleY = SampleSide, maxSampleX = -1, maxSampleY = -1;
            int cellX = chunk.Position.X * GasFluid.ChunkSize;
            int cellY = chunk.Position.Y * GasFluid.ChunkSize;
            for (int y = 0; y < SampleSide; y++)
                for (int x = 0; x < SampleSide; x++)
                {
                    int index = y * SampleSide + x;
                    field.RenderAppearance(cellX + x - 2, cellY + y - 2, blend, out opticalScratch[index], out appearanceScratch[index]);
                    float density = opticalScratch[index].W;
                    peak = Math.Max(peak, density);
                    if (density <= 0f)
                        continue;
                    minSampleX = Math.Min(minSampleX, x);
                    minSampleY = Math.Min(minSampleY, y);
                    maxSampleX = Math.Max(maxSampleX, x);
                    maxSampleY = Math.Max(maxSampleY, y);
                }
            if (peak <= 0.003f)
                continue;
            Reserve(ChunkCount + 1);
            int column = ChunkCount % Columns, row = ChunkCount / Columns;
            Vector2 sampleOrigin = new(column * SampleSide, row * SampleSide);
            Vector2 vertexOrigin = new(column * VertexSide, row * VertexSide);
            int minX = Math.Clamp((minSampleX - 3) * Subdivision - Subdivision / 2, 0, side - 1);
            int minY = Math.Clamp((minSampleY - 3) * Subdivision - Subdivision / 2, 0, side - 1);
            int maxX = Math.Clamp((maxSampleX + 1) * Subdivision - Subdivision / 2, 0, side - 1);
            int maxY = Math.Clamp((maxSampleY + 1) * Subdivision - Subdivision / 2, 0, side - 1);
            int firstX = Math.Max(0, minX - 1), firstY = Math.Max(0, minY - 1);
            int lastX = Math.Min(side - 2, maxX) + 1, lastY = Math.Min(side - 2, maxY) + 1;
            WriteQuad(ReconstructionVertices, ChunkCount, vertexOrigin + new Vector2(minX, minY),
                vertexOrigin + new Vector2(maxX + 1, maxY + 1), new Vector2(minX, minY), new Vector2(maxX + 1, maxY + 1), sampleOrigin, vertexOrigin);
            WriteQuad(CompositeVertices, ChunkCount, position + new Vector2(firstX, firstY) * spacing,
                position + new Vector2(lastX, lastY) * spacing, new Vector2(firstX, firstY), new Vector2(lastX, lastY), sampleOrigin, vertexOrigin);
            int minCellX = (minX + Subdivision / 2) / Subdivision;
            int minCellY = (minY + Subdivision / 2) / Subdivision;
            int maxCellX = (maxX + Subdivision / 2) / Subdivision;
            int maxCellY = (maxY + Subdivision / 2) / Subdivision;
            int firstTileX = (cellX + minCellX - 1) * field.CellPixels / 16;
            int firstTileY = (cellY + minCellY - 1) * field.CellPixels / 16;
            int lastTileX = (cellX + maxCellX) * field.CellPixels / 16;
            int lastTileY = (cellY + maxCellY) * field.CellPixels / 16;
            int lightWidth = lastTileX - firstTileX + 1;
            for (int y = firstTileY; y <= lastTileY; y++)
                for (int x = firstTileX; x <= lastTileX; x++)
                    lightingScratch[(y - firstTileY) * lightWidth + x - firstTileX] = lighting(x, y);
            for (int y = 0; y < SampleSide; y++)
            {
                int target = (row * SampleSide + y) * SampleWidth + column * SampleSide;
                int source = y * SampleSide;
                Array.Copy(opticalScratch, source, Optical, target, SampleSide);
                Array.Copy(appearanceScratch, source, Appearance, target, SampleSide);
                Array.Clear(Environment, target, SampleSide);
                if (y < minCellY + 1 || y > maxCellY + 2)
                    continue;
                int tileY = (cellY + y - 2) * field.CellPixels / 16;
                for (int x = minCellX + 1; x <= maxCellX + 2; x++)
                {
                    int tileX = (cellX + x - 2) * field.CellPixels / 16;
                    Color color = lightingScratch[(tileY - firstTileY) * lightWidth + tileX - firstTileX];
                    color.A = 0;
                    Environment[target + x] = color;
                }
            }
            if (tileCollision)
            {
                int tiles = field.ChunkWorldSize / 16;
                for (int y = 0; y < tiles + 2; y++)
                    for (int x = 0; x < tiles + 2; x++)
                    {
                        int index = (row * SampleSide + y) * SampleWidth + column * SampleSide + x;
                        Environment[index].A = (byte)terrain(chunk.Position.X * tiles + x - 1, chunk.Position.Y * tiles + y - 1);
                    }
            }
            ChunkCount++;
        }
    }

    private void Reserve(int count)
    {
        int rows = (count + Columns - 1) / Columns;
        if (rows <= Rows)
            return;
        Rows = Math.Max(rows, Math.Max(1, Rows * 2));
        int length = SampleWidth * SampleHeight;
        FieldValue[] optical = Optical, appearance = Appearance;
        Color[] environment = Environment;
        Array.Resize(ref optical, length);
        Array.Resize(ref appearance, length);
        Array.Resize(ref environment, length);
        Optical = optical;
        Appearance = appearance;
        Environment = environment;
    }

    private static void WriteQuad(GasRenderVertex[] vertices, int chunk, Vector2 minimum, Vector2 maximum,
        Vector2 localMinimum, Vector2 localMaximum, Vector2 sampleOrigin, Vector2 vertexOrigin)
    {
        int start = chunk * 4;
        vertices[start] = new() { Position = minimum, Local = localMinimum, SampleOrigin = sampleOrigin, VertexOrigin = vertexOrigin };
        vertices[start + 1] = new() { Position = new Vector2(maximum.X, minimum.Y), Local = new Vector2(localMaximum.X, localMinimum.Y), SampleOrigin = sampleOrigin, VertexOrigin = vertexOrigin };
        vertices[start + 2] = new() { Position = new Vector2(minimum.X, maximum.Y), Local = new Vector2(localMinimum.X, localMaximum.Y), SampleOrigin = sampleOrigin, VertexOrigin = vertexOrigin };
        vertices[start + 3] = new() { Position = maximum, Local = localMaximum, SampleOrigin = sampleOrigin, VertexOrigin = vertexOrigin };
    }
}