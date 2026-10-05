using System;


using FieldValue = System.Numerics.Vector4;

namespace AerovelenceMod.Common.Systems.Gas;

internal sealed class GasRenderMesh
{
    private const int Subdivision = 4;
    private const float TilePadding = 4f;
    private const int Side = GasFluid.ChunkSize * Subdivision + 1;
    private const int SampleSide = GasFluid.ChunkSize + 6;
    private readonly VertexPositionColor[] vertices = new VertexPositionColor[GasFluid.MaxChunks * Side * Side];
    private readonly int[] indices = new int[GasFluid.MaxChunks * (Side - 1) * (Side - 1) * 6];
    private readonly FieldValue[] samples = new FieldValue[SampleSide * SampleSide];
    private readonly FieldValue[] horizontal = new FieldValue[SampleSide * Side];
    private readonly FieldValue[] appearanceSamples = new FieldValue[SampleSide * SampleSide];
    private readonly FieldValue[] appearanceHorizontal = new FieldValue[SampleSide * Side];
    private readonly Vector3[] illumination = new Vector3[SampleSide * SampleSide];
    private readonly VertexPositionColor[] chunkVertices = new VertexPositionColor[Side * Side];
    private readonly int[] vertexMap = new int[Side * Side];
    private readonly bool[] support = new bool[(GasFluid.ChunkSize + 1) * (GasFluid.ChunkSize + 1)];
    private Vector2 chunkOrigin;
    private Vector2 camera;
    private float spacing;
    private int meshSide;
    private int vertexCount;
    private int indexCount;
    public VertexPositionColor[] Vertices => vertices;
    public int[] Indices => indices;
    public int VertexCount => vertexCount;
    public int IndexCount => indexCount;

    public void Build(GasFluid field, bool tileCollision, float blend, bool additive, Vector2 screenPosition, Vector2 minimum, Vector2 maximum, Func<int, int, Vector3> lighting, float pixelSpacing = 1f)
    {
        vertexCount = 0;
        indexCount = 0;
        if (field is null)
            return;
        int subdivision = field.CellPixels == 4 && pixelSpacing >= 2f ? 2 : Subdivision;
        int side = GasFluid.ChunkSize * subdivision + 1;
        meshSide = side;
        if (field.FastUpdates)
            blend = 1f;
        foreach (GasFluid.Chunk chunk in field.Chunks)
        {
            Vector2 position = new Vector2(chunk.Position.X, chunk.Position.Y) * field.ChunkWorldSize - screenPosition;
            if (position.X + field.ChunkWorldSize < minimum.X || position.X > maximum.X || position.Y + field.ChunkWorldSize < minimum.Y || position.Y > maximum.Y)
                continue;
            float samplePeak = 0f;
            int minSampleX = SampleSide, minSampleY = SampleSide, maxSampleX = -1, maxSampleY = -1;
            for (int y = 0; y < SampleSide; y++)
                for (int x = 0; x < SampleSide; x++)
                {
                    int gx = chunk.Position.X * GasFluid.ChunkSize + x - 2;
                    int gy = chunk.Position.Y * GasFluid.ChunkSize + y - 2;
                    int index = y * SampleSide + x;
                    field.RenderAppearance(gx, gy, blend, out samples[index], out appearanceSamples[index]);
                    float density = samples[index].W;
                    samplePeak = Math.Max(samplePeak, density);
                    if (density > 0f)
                    {
                        minSampleX = Math.Min(minSampleX, x);
                        minSampleY = Math.Min(minSampleY, y);
                        maxSampleX = Math.Max(maxSampleX, x);
                        maxSampleY = Math.Max(maxSampleY, y);
                    }
                }
            if (samplePeak <= 0.003f)
                continue;
            int minX = Math.Clamp((minSampleX - 3) * subdivision - subdivision / 2, 0, side - 1);
            int minY = Math.Clamp((minSampleY - 3) * subdivision - subdivision / 2, 0, side - 1);
            int maxX = Math.Clamp((maxSampleX + 1) * subdivision - subdivision / 2, 0, side - 1);
            int maxY = Math.Clamp((maxSampleY + 1) * subdivision - subdivision / 2, 0, side - 1);
            chunkOrigin = new Vector2(chunk.Position.X, chunk.Position.Y) * field.ChunkWorldSize;
            camera = screenPosition;
            spacing = field.CellPixels / (float)subdivision;
            Array.Clear(chunkVertices);
            for (int y = 0; y < SampleSide; y++)
                for (int x = 0; x < SampleSide; x++)
                {
                    int gx = chunk.Position.X * GasFluid.ChunkSize + x - 2;
                    int gy = chunk.Position.Y * GasFluid.ChunkSize + y - 2;
                    illumination[y * SampleSide + x] = lighting(gx * field.CellPixels / 16, gy * field.CellPixels / 16);
                }
            for (int y = 0; y < SampleSide; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    int shiftedX = x + subdivision / 2;
                    int i = y * SampleSide + shiftedX / subdivision;
                    float fraction = (shiftedX % subdivision) / (float)subdivision;
                    horizontal[y * side + x] = GasAppearance.Cubic(samples[i], samples[i + 1], samples[i + 2], samples[i + 3], fraction);
                    appearanceHorizontal[y * side + x] = GasAppearance.Cubic(appearanceSamples[i], appearanceSamples[i + 1], appearanceSamples[i + 2], appearanceSamples[i + 3], fraction);
                }
            for (int cy = 0; cy <= GasFluid.ChunkSize; cy++)
                for (int cx = 0; cx <= GasFluid.ChunkSize; cx++)
                {
                    bool occupied = false;
                    for (int oy = 0; oy < 4 && !occupied; oy++)
                        for (int ox = 0; ox < 4; ox++)
                            occupied |= samples[(cy + oy) * SampleSide + cx + ox].W > 0f;
                    support[cy * (GasFluid.ChunkSize + 1) + cx] = occupied;
                }
            Array.Fill(vertexMap, -1);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    int cx = (x + subdivision / 2) / subdivision;
                    int cy = (y + subdivision / 2) / subdivision;
                    float fx = ((x + subdivision / 2) % subdivision) / (float)subdivision;
                    float fy = ((y + subdivision / 2) % subdivision) / (float)subdivision;
                    int local = y * side + x;
                    Color color = Color.Transparent;
                    if (support[cy * (GasFluid.ChunkSize + 1) + cx])
                    {
                        int h = cy * side + x;
                        FieldValue value = FieldValue.Max(GasAppearance.Cubic(horizontal[h], horizontal[h + side], horizontal[h + side * 2], horizontal[h + side * 3], fy), FieldValue.Zero);
                        if (value.W > 0.003f)
                        {
                            FieldValue appearance = FieldValue.Max(GasAppearance.Cubic(appearanceHorizontal[h], appearanceHorizontal[h + side], appearanceHorizontal[h + side * 2], appearanceHorizontal[h + side * 3], fy), FieldValue.Zero);
                            int s = (cy + 1) * SampleSide + cx + 1;
                            Vector3 ambient = Vector3.Lerp(Vector3.Lerp(illumination[s], illumination[s + 1], fx), Vector3.Lerp(illumination[s + SampleSide], illumination[s + SampleSide + 1], fx), fy);
                            color = additive ? GasAppearance.ShadeAdditive(value, ambient, appearance) : GasAppearance.Shade(value, ambient, appearance);
                        }
                    }
                    chunkVertices[local].Color = color;
                }
            for (int y = Math.Max(0, minY - 1); y <= Math.Min(side - 2, maxY); y++)
                for (int x = Math.Max(0, minX - 1); x <= Math.Min(side - 2, maxX); x++)
                {
                    int local = y * side + x;
                    if ((chunkVertices[local].Color.PackedValue | chunkVertices[local + 1].Color.PackedValue | chunkVertices[local + side].Color.PackedValue | chunkVertices[local + side + 1].Color.PackedValue) == 0)
                        continue;
                    Vector2 world = new Vector2(chunk.Position.X, chunk.Position.Y) * field.ChunkWorldSize + new Vector2(x + 0.5f, y + 0.5f) * (field.CellPixels / (float)subdivision);
                    if (tileCollision && field.RenderBlocked(world, TilePadding))
                        continue;
                    int a = IncludeVertex(local);
                    int b = IncludeVertex(local + 1);
                    int c = IncludeVertex(local + side);
                    int d = IncludeVertex(local + side + 1);
                    if (((x + y) & 1) == 0)
                    {
                        indices[indexCount++] = a;
                        indices[indexCount++] = b;
                        indices[indexCount++] = c;
                        indices[indexCount++] = b;
                        indices[indexCount++] = d;
                        indices[indexCount++] = c;
                    }
                    else
                    {
                        indices[indexCount++] = a;
                        indices[indexCount++] = b;
                        indices[indexCount++] = d;
                        indices[indexCount++] = a;
                        indices[indexCount++] = d;
                        indices[indexCount++] = c;
                    }
                }
        }
    }
    private int IncludeVertex(int local)
    {
        int index = vertexMap[local];
        if (index >= 0)
            return index;
        index = vertexCount++;
        vertexMap[local] = index;
        Vector2 world = chunkOrigin + new Vector2(local % meshSide, local / meshSide) * spacing;
        vertices[index] = new VertexPositionColor(new Vector3(world - camera, 0f), chunkVertices[local].Color);
        return index;
    }

}
