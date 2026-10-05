using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Vector4 = System.Numerics.Vector4;

namespace AerovelenceMod.Common.Systems.Gas;

internal struct GasDye
{
    public Vector4 Optical;
    public Vector4 Material;
    public Vector4 Damage;
    public Vector4 Appearance;
    public Vector4 ColorFade;
    public readonly float Density => Optical.W;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GasDye operator +(GasDye a, GasDye b) => new() { Optical = a.Optical + b.Optical, Material = a.Material + b.Material, Damage = a.Damage + b.Damage, Appearance = a.Appearance + b.Appearance, ColorFade = a.ColorFade + b.ColorFade };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GasDye operator -(GasDye a, GasDye b) => new() { Optical = a.Optical - b.Optical, Material = a.Material - b.Material, Damage = a.Damage - b.Damage, Appearance = a.Appearance - b.Appearance, ColorFade = a.ColorFade - b.ColorFade };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GasDye operator *(GasDye a, float b) => new() { Optical = a.Optical * b, Material = a.Material * b, Damage = a.Damage * b, Appearance = a.Appearance * b, ColorFade = a.ColorFade * b };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GasDye Min(GasDye a, GasDye b) => new() { Optical = Vector4.Min(a.Optical, b.Optical), Material = Vector4.Min(a.Material, b.Material), Damage = Vector4.Min(a.Damage, b.Damage), Appearance = Vector4.Min(a.Appearance, b.Appearance), ColorFade = Vector4.Min(a.ColorFade, b.ColorFade) };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GasDye Max(GasDye a, GasDye b) => new() { Optical = Vector4.Max(a.Optical, b.Optical), Material = Vector4.Max(a.Material, b.Material), Damage = Vector4.Max(a.Damage, b.Damage), Appearance = Vector4.Max(a.Appearance, b.Appearance), ColorFade = Vector4.Max(a.ColorFade, b.ColorFade) };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GasDye Clamp(GasDye value, GasDye min, GasDye max) => Max(min, Min(value, max));
}

internal sealed class GasFluid
{
    public const int CellSize = 8;
    public const int ChunkSize = 16;
    public const int ChunkPixels = CellSize * ChunkSize;
    public const int MaxChunks = 128;
    private const int ChunkCells = ChunkSize * ChunkSize;

    internal sealed class Chunk
    {
        public Point Position;
        public int Offset;
        public int EmptyTicks;
        public float Peak;
    }

    private readonly Dictionary<long, Chunk> chunks = new();
    private readonly Stack<int> free = new();
    private readonly List<Point> pending = new();
    private readonly Dictionary<int, GasDye> pendingInjection;
    private readonly int[] active;
    private readonly int[][] parityCells;
    private readonly int[] left;
    private readonly int[] right;
    private readonly int[] up;
    private readonly int[] down;
    private readonly int[] cellX;
    private readonly int[] cellY;
    private readonly bool[] solid;
    private readonly ushort[] solidBlocks;
    private readonly float[] divergence;
    private readonly float[] source;
    private readonly float[] curl;
    private readonly int[] pressureLeft;
    private readonly int[] pressureRight;
    private readonly int[] pressureUp;
    private readonly int[] pressureDown;
    private readonly float[] inverseNeighbors;
    private readonly float[] pressure;
    private Vector2[] velocity;
    private Vector2[] velocityNext;
    private GasDye[] dye;
    private GasDye[] dyeNext;
    private readonly GasDye[] predicted;
    private readonly Vector2[] dyeTrace;
    private readonly Func<Vector2, bool> obstacle;
    private int activeCount;
    private bool topologyDirty;
    private bool pressureTopologyDirty = true;
    private int cachedX = int.MinValue;
    private int cachedY = int.MinValue;
    private int cachedOffset;
    public int Revision { get; private set; }
    public IEnumerable<Chunk> Chunks => chunks.Values;
    public int ChunkCount => chunks.Count;
    public int CellCount => activeCount;
    public int CellPixels { get; }
    public int ChunkWorldSize => CellPixels * ChunkSize;
    private int fastUpdateTicks;
    public bool FastUpdates => fastUpdateTicks > 0;

    public GasFluid(Func<Vector2, bool> obstacle = null, int maxChunks = MaxChunks, int cellSize = CellSize)
    {
        this.obstacle = obstacle;
        CellPixels = cellSize <= 4 ? 4 : CellSize;
        if (CellPixels < CellSize)
            pendingInjection = new Dictionary<int, GasDye>();
        maxChunks = Math.Clamp(maxChunks, 1, MaxChunks);
        int capacity = maxChunks * ChunkCells + 1;
        active = new int[capacity];
        parityCells = new[] { new int[capacity / 2 + 1], new int[capacity / 2 + 1] };
        left = new int[capacity];
        right = new int[capacity];
        up = new int[capacity];
        down = new int[capacity];
        cellX = new int[capacity];
        cellY = new int[capacity];
        solid = new bool[capacity];
        solidBlocks = new ushort[maxChunks];
        divergence = new float[capacity];
        source = new float[capacity];
        curl = new float[capacity];
        pressureLeft = new int[capacity];
        pressureRight = new int[capacity];
        pressureUp = new int[capacity];
        pressureDown = new int[capacity];
        inverseNeighbors = new float[capacity];
        pressure = new float[capacity];
        velocity = new Vector2[capacity];
        velocityNext = new Vector2[capacity];
        dye = new GasDye[capacity];
        dyeNext = new GasDye[capacity];
        predicted = new GasDye[capacity];
        dyeTrace = new Vector2[capacity];
        for (int i = maxChunks - 1; i >= 0; i--)
            free.Push(i);
    }

    private static long Key(int x, int y) => ((long)x << 32) | (uint)y;

    private int Index(int x, int y)
    {
        int cx = x >> 4;
        int cy = y >> 4;
        if (cx != cachedX || cy != cachedY)
        {
            cachedX = cx;
            cachedY = cy;
            cachedOffset = chunks.TryGetValue(Key(cx, cy), out Chunk chunk) ? chunk.Offset : 0;
        }
        return cachedOffset == 0 ? 0 : cachedOffset + ((y & 15) << 4) + (x & 15);
    }

    private void AddChunk(Point point)
    {
        long key = Key(point.X, point.Y);
        if (chunks.ContainsKey(key) || free.Count == 0)
            return;
        Chunk chunk = new() { Position = point, Offset = free.Pop() * ChunkCells + 1 };
        chunks.Add(key, chunk);
        cachedX = int.MinValue;
        int slot = (chunk.Offset - 1) / ChunkCells;
        solidBlocks[slot] = 0;
        for (int y = 0; y < ChunkSize; y++)
            for (int x = 0; x < ChunkSize; x++)
            {
                int i = chunk.Offset + y * ChunkSize + x;
                cellX[i] = point.X * ChunkSize + x;
                cellY[i] = point.Y * ChunkSize + y;
                solid[i] = obstacle?.Invoke(Center(i)) ?? false;
                if (solid[i])
                    solidBlocks[slot] |= (ushort)(1 << ((y >> 2) * 4 + (x >> 2)));
            }
        topologyDirty = true;
    }

    private Vector2 Center(int i) => new((cellX[i] + 0.5f) * CellPixels, (cellY[i] + 0.5f) * CellPixels);

    private void Rebuild()
    {
        if (!topologyDirty)
            return;
        activeCount = 0;
        int red = 0;
        int black = 0;
        foreach (Chunk chunk in chunks.Values)
            for (int i = chunk.Offset; i < chunk.Offset + ChunkCells; i++)
            {
                active[activeCount++] = i;
                int parity = (cellX[i] + cellY[i]) & 1;
                parityCells[parity][parity == 0 ? red++ : black++] = i;
                left[i] = Index(cellX[i] - 1, cellY[i]);
                right[i] = Index(cellX[i] + 1, cellY[i]);
                up[i] = Index(cellX[i], cellY[i] - 1);
                down[i] = Index(cellX[i], cellY[i] + 1);
            }
        topologyDirty = false;
        pressureTopologyDirty = true;
    }

    private void Prepare(Vector2 position, float radius)
    {
        int minX = (int)MathF.Floor((position.X - radius) / CellPixels);
        int minY = (int)MathF.Floor((position.Y - radius) / CellPixels);
        int maxX = (int)MathF.Floor((position.X + radius) / CellPixels);
        int maxY = (int)MathF.Floor((position.Y + radius) / CellPixels);
        int minChunkX = minX >> 4;
        int minChunkY = minY >> 4;
        int maxChunkX = maxX >> 4;
        int maxChunkY = maxY >> 4;
        for (int y = minChunkY; y <= maxChunkY; y++)
            for (int x = minChunkX; x <= maxChunkX; x++)
                AddChunk(new Point(x, y));
        Rebuild();
    }

    private void InjectCell(int i, Vector2 impulse, Vector3 color, float amount, float weight, float lift, float turbulence, float viscosity, float decay, float expansion, int damage, bool hostile, Vector4? appearance, Vector4? colorFade)
    {
        if (i == 0 || solid[i] || weight <= 0f)
            return;
        float mass = amount * weight;
        GasDye injected = new()
        {
            Optical = new Vector4(color.X * mass, color.Y * mass, color.Z * mass, mass),
            Material = new Vector4(lift, turbulence, viscosity, decay) * mass,
            Damage = damage <= 0 ? Vector4.Zero : hostile ? new Vector4(0f, mass, 0f, damage * mass) : new Vector4(mass, 0f, damage * mass, 0f),
            Appearance = (appearance ?? new Vector4(1f, 0f, 0f, 1f)) * mass,
            ColorFade = (colorFade ?? Vector4.Zero) * mass
        };
        dye[i] += injected;
        float densityLimit = Math.Min(1f, 6f / Math.Max(0.0001f, dye[i].Density));
        dye[i] *= densityLimit;
        if (pendingInjection is not null)
        {
            pendingInjection.TryGetValue(i, out GasDye queued);
            pendingInjection[i] = (queued + injected) * densityLimit;
        }
        velocity[i] = Vector2.Lerp(velocity[i], impulse / CellPixels, Math.Clamp(weight * 0.65f, 0f, 1f));
        source[i] += weight * expansion;
    }

    public void Inject(Vector2 position, Vector2 impulse, Vector3 color, float amount, float radius, float lift, float turbulence, float viscosity, float decay, float expansion, int damage, bool hostile, Vector4? appearance = null, Vector4? colorFade = null)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(impulse.X) || !float.IsFinite(impulse.Y) || !float.IsFinite(amount) || amount <= 0f)
            return;
        radius = Math.Clamp(radius, CellPixels, 48f);
        Prepare(position, radius);
        if (CellPixels < CellSize && impulse.LengthSquared() > 64f)
            fastUpdateTicks = Math.Max(fastUpdateTicks, 8);
        int minX = (int)MathF.Floor((position.X - radius) / CellPixels);
        int minY = (int)MathF.Floor((position.Y - radius) / CellPixels);
        int maxX = (int)MathF.Floor((position.X + radius) / CellPixels);
        int maxY = (int)MathF.Floor((position.Y + radius) / CellPixels);
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                int i = Index(x, y);
                if (i == 0 || solid[i])
                    continue;
                Vector2 offset = Center(i) - position;
                float radial = Math.Max(0f, 1f - offset.LengthSquared() / (radius * radius));
                InjectCell(i, impulse, color, amount, radial * radial, lift, turbulence, viscosity, decay, expansion, damage, hostile, appearance, colorFade);
            }
    }

    public void InjectJet(Vector2 position, Vector2 impulse, Vector3 color, float amount, float lift, float turbulence, float viscosity, float decay, float expansion, int damage, bool hostile, Vector4? appearance = null, Vector4? colorFade = null)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(impulse.X) || !float.IsFinite(impulse.Y) || !float.IsFinite(amount) || amount <= 0f)
            return;
        Prepare(position, CellPixels * 2f);
        fastUpdateTicks = Math.Max(fastUpdateTicks, 8);
        amount *= (CellSize * CellSize) / (float)(CellPixels * CellPixels);
        Vector2 point = position / CellPixels - new Vector2(0.5f);
        int x = (int)MathF.Floor(point.X);
        int y = (int)MathF.Floor(point.Y);
        float fx = point.X - x;
        float fy = point.Y - y;
        for (int oy = -1; oy <= 2; oy++)
            for (int ox = -1; ox <= 2; ox++)
            {
                float weight = JetWeight(ox - fx) * JetWeight(oy - fy);
                InjectCell(Index(x + ox, y + oy), impulse, color, amount, weight, lift, turbulence, viscosity, decay, expansion, damage, hostile, appearance, colorFade);
            }
    }

    private static float JetWeight(float distance)
    {
        distance = Math.Abs(distance);
        if (distance < 1f)
            return 2f / 3f - distance * distance + distance * distance * distance * 0.5f;
        float edge = Math.Max(0f, 2f - distance);
        return edge * edge * edge / 6f;
    }

    private float Face(float value, int a, int b) => solid[a] || solid[b] ? 0f : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CornerIndices(int x, int y, out int a, out int b, out int c, out int d)
    {
        a = Index(x, y);
        if ((x & 15) != 15 && (y & 15) != 15)
        {
            b = a == 0 ? 0 : a + 1;
            c = a == 0 ? 0 : a + ChunkSize;
            d = a == 0 ? 0 : a + ChunkSize + 1;
            return;
        }
        b = Index(x + 1, y);
        c = Index(x, y + 1);
        d = Index(x + 1, y + 1);
    }

    private float SampleComponent(float x, float y, bool horizontal)
    {
        int ix = (int)MathF.Floor(x);
        int iy = (int)MathF.Floor(y);
        float fx = x - ix;
        float fy = y - iy;
        CornerIndices(ix, iy, out int a, out int b, out int c, out int d);
        float va = horizontal ? Face(velocity[a].X, a, right[a]) : Face(velocity[a].Y, a, down[a]);
        float vb = horizontal ? Face(velocity[b].X, b, right[b]) : Face(velocity[b].Y, b, down[b]);
        float vc = horizontal ? Face(velocity[c].X, c, right[c]) : Face(velocity[c].Y, c, down[c]);
        float vd = horizontal ? Face(velocity[d].X, d, right[d]) : Face(velocity[d].Y, d, down[d]);
        return MathHelper.Lerp(MathHelper.Lerp(va, vb, fx), MathHelper.Lerp(vc, vd, fx), fy);
    }

    private Vector2 Trace(Vector2 origin, Vector2 displacement)
    {
        float distance = displacement.Length();
        if (distance < 0.00001f)
            return origin;
        displacement *= Math.Min(1f, (CellSize * 5f / CellPixels) / distance);
        if (obstacle is null)
            return origin + displacement;
        Vector2 destination = origin + displacement;
        int originX = (int)MathF.Floor(origin.X);
        int originY = (int)MathF.Floor(origin.Y);
        if ((originX >> 4) == ((int)MathF.Floor(destination.X) >> 4)
            && (originY >> 4) == ((int)MathF.Floor(destination.Y) >> 4))
        {
            int originIndex = Index(originX, originY);
            int blocks = originIndex == 0 ? 0 : solidBlocks[(originIndex - 1) / ChunkCells];
            if (blocks == 0)
                return destination;
            int x1 = (originX & 15) >> 2, y1 = (originY & 15) >> 2;
            int x2 = ((int)MathF.Floor(destination.X) & 15) >> 2, y2 = ((int)MathF.Floor(destination.Y) & 15) >> 2;
            int row = ((1 << (Math.Abs(x2 - x1) + 1)) - 1) << Math.Min(x1, x2);
            int mask = 0;
            for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
                mask |= row << (y * 4);
            if ((blocks & mask) == 0)
                return destination;
        }
        int steps = Math.Max(1, (int)MathF.Ceiling(displacement.Length() * 3f));
        Vector2 last = origin;
        for (int s = 1; s <= steps; s++)
        {
            Vector2 point = origin + displacement * (s / (float)steps);
            int i = Index((int)MathF.Floor(point.X), (int)MathF.Floor(point.Y));
            if (solid[i])
                return last;
            last = point;
        }
        return last;
    }

    private void Corners(GasDye[] field, Vector2 point, out GasDye a, out GasDye b, out GasDye c, out GasDye d, out float fx, out float fy)
    {
        float x = point.X - 0.5f;
        float y = point.Y - 0.5f;
        int ix = (int)MathF.Floor(x);
        int iy = (int)MathF.Floor(y);
        fx = x - ix;
        fy = y - iy;
        CornerIndices(ix, iy, out int ia, out int ib, out int ic, out int id);
        a = field[ia];
        b = field[ib];
        c = field[ic];
        d = field[id];
    }

    private GasDye SampleDye(GasDye[] field, Vector2 point)
    {
        Corners(field, point, out GasDye a, out GasDye b, out GasDye c, out GasDye d, out float fx, out float fy);
        if (a.Density == 0f && b.Density == 0f && c.Density == 0f && d.Density == 0f)
            return default;
        return (a * (1f - fx) + b * fx) * (1f - fy) + (c * (1f - fx) + d * fx) * fy;
    }

    public GasDye Sample(Vector2 position) => SampleDye(dye, position / CellPixels);

    public GasDye SampleDamage(Rectangle hitbox, bool hostile)
    {
        GasDye best = default;
        if (chunks.Count == 0)
            return best;
        float strength = 0f;
        int minX = (int)MathF.Floor(hitbox.Left / (float)CellPixels);
        int minY = (int)MathF.Floor(hitbox.Top / (float)CellPixels);
        int maxX = (int)MathF.Floor(hitbox.Right / (float)CellPixels);
        int maxY = (int)MathF.Floor(hitbox.Bottom / (float)CellPixels);
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 position = new(Math.Clamp((x + 0.5f) * CellPixels, hitbox.Left, hitbox.Right), Math.Clamp((y + 0.5f) * CellPixels, hitbox.Top, hitbox.Bottom));
                GasDye candidate = Sample(position);
                float density = hostile ? candidate.Damage.Y : candidate.Damage.X;
                if (density <= strength)
                    continue;
                strength = density;
                best = candidate;
            }
        return best;
    }
    public float DensityAt(int x, int y) => dye[Index(x, y)].Density;
    public GasDye CellAt(int x, int y) => dye[Index(x, y)];
    public GasDye RenderCell(int x, int y, float blend)
    {
        int i = Index(x, y);
        return dyeNext[i] * (1f - blend) + dye[i] * blend;
    }

    public bool RenderBlocked(Vector2 position, float padding)
    {
        if (obstacle is null || !obstacle(position))
            return false;
        if (padding <= 0f)
            return true;
        float diagonal = padding * 0.70710678f;
        return obstacle(position + new Vector2(padding, 0f))
            && obstacle(position - new Vector2(padding, 0f))
            && obstacle(position + new Vector2(0f, padding))
            && obstacle(position - new Vector2(0f, padding))
            && obstacle(position + new Vector2(diagonal, diagonal))
            && obstacle(position - new Vector2(diagonal, diagonal))
            && obstacle(position + new Vector2(diagonal, -diagonal))
            && obstacle(position + new Vector2(-diagonal, diagonal));
    }

    public Vector3 TileLight(int tileX, int tileY)
    {
        int cells = 16 / CellPixels;
        Vector3 brightest = Vector3.Zero;
        float brightness = 0f;
        for (int y = 0; y < cells; y++)
            for (int x = 0; x < cells; x++)
            {
                GasDye cell = CellAt(tileX * cells + x, tileY * cells + y);
                Vector3 light = GasAppearance.Light(cell.Optical, cell.Appearance);
                float strength = light.LengthSquared();
                if (strength <= brightness)
                    continue;
                brightness = strength;
                brightest = light;
            }
        return brightest;
    }

    public GasDye RenderBoundaryCell(int x, int y, float blend)
    {
        int center = Index(x, y);
        if (obstacle is null || !(center != 0 ? solid[center] : obstacle(new Vector2(x + 0.5f, y + 0.5f) * CellPixels)))
            return RenderCell(x, y, blend);
        GasDye nearest = default;
        int nearestDistance = int.MaxValue;
        int count = 0;
        for (int oy = -2; oy <= 2; oy++)
            for (int ox = -2; ox <= 2; ox++)
            {
                int distance = ox * ox + oy * oy;
                if (distance == 0 || distance > nearestDistance)
                    continue;
                int i = Index(x + ox, y + oy);
                if (i == 0 || solid[i])
                    continue;
                if (distance < nearestDistance)
                {
                    nearest = default;
                    count = 0;
                    nearestDistance = distance;
                }
                nearest += dyeNext[i] * (1f - blend) + dye[i] * blend;
                count++;
            }
        return count > 0 ? nearest * (1f / count) : default;
    }

    public void RenderAppearance(int x, int y, float blend, out Vector4 optical, out Vector4 appearance)
    {
        int center = Index(x, y);
        optical = Vector4.Zero;
        appearance = Vector4.Zero;
        if (obstacle is null || !(center != 0 ? solid[center] : obstacle(new Vector2(x + 0.5f, y + 0.5f) * CellPixels)))
        {
            optical = dyeNext[center].Optical * (1f - blend) + dye[center].Optical * blend;
            appearance = dyeNext[center].Appearance * (1f - blend) + dye[center].Appearance * blend;
            return;
        }
        int nearestDistance = int.MaxValue;
        int count = 0;
        for (int oy = -2; oy <= 2; oy++)
            for (int ox = -2; ox <= 2; ox++)
            {
                int distance = ox * ox + oy * oy;
                if (distance == 0 || distance > nearestDistance)
                    continue;
                int i = Index(x + ox, y + oy);
                if (i == 0 || solid[i])
                    continue;
                if (distance < nearestDistance)
                {
                    optical = appearance = Vector4.Zero;
                    count = 0;
                    nearestDistance = distance;
                }
                optical += dyeNext[i].Optical * (1f - blend) + dye[i].Optical * blend;
                appearance += dyeNext[i].Appearance * (1f - blend) + dye[i].Appearance * blend;
                count++;
            }
        if (count > 0)
        {
            optical *= 1f / count;
            appearance *= 1f / count;
        }
    }
    public Vector4 RenderOptical(int x, int y, float blend)
    {
        int i = Index(x, y);
        return Vector4.Lerp(dyeNext[i].Optical, dye[i].Optical, blend);
    }
    public bool SolidAt(int x, int y) => solid[Index(x, y)];

    public Vector2 FlowAt(Vector2 position)
    {
        Vector2 point = position / CellPixels;
        return new Vector2(SampleComponent(point.X - 1f, point.Y - 0.5f, true), SampleComponent(point.X - 0.5f, point.Y - 1f, false)) * CellPixels;
    }

    public void ApplyAirflow(Vector2 position, Vector2 direction, float speed, float range, float radius, float spread, Func<Vector2, Vector2, bool> visible = null)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(direction.X) || !float.IsFinite(direction.Y)
            || !float.IsFinite(speed) || !float.IsFinite(range) || !float.IsFinite(radius) || !float.IsFinite(spread)
            || speed <= 0f || range <= 0f || direction.LengthSquared() < 0.0001f)
            return;
        direction.Normalize();
        speed = Math.Min(speed, 40f);
        range = Math.Min(range, 480f);
        radius = Math.Clamp(radius, 1f, 64f);
        spread = Math.Clamp(spread, 0f, 0.6f);
        Rebuild();
        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            if (solid[i])
                continue;
            Vector2 center = Center(i);
            float wx = Weight(center + new Vector2(CellPixels * 0.5f, 0f));
            float wy = Weight(center + new Vector2(0f, CellPixels * 0.5f));
            if (wx + wy <= 0f || (visible is not null && !visible(position, center)))
                continue;
            if (speed >= 12f)
                fastUpdateTicks = Math.Max(fastUpdateTicks, 8);
            if (!solid[right[i]])
                velocity[i].X = MathHelper.Lerp(velocity[i].X, direction.X * speed / CellPixels, wx * 0.22f);
            if (!solid[down[i]])
                velocity[i].Y = MathHelper.Lerp(velocity[i].Y, direction.Y * speed / CellPixels, wy * 0.22f);
        }

        float Weight(Vector2 point)
        {
            Vector2 offset = point - position;
            float along = Vector2.Dot(offset, direction);
            if (along < 0f || along >= range)
                return 0f;
            float width = Math.Max(CellSize, radius + along * spread);
            float across = Math.Abs(offset.X * direction.Y - offset.Y * direction.X) / width;
            if (across >= 1f)
                return 0f;
            float edge = 1f - across * across;
            float end = 1f - along / range;
            return edge * edge * end * end;
        }
    }

    public void ApplyVacuum(Vector2 position, Vector2 direction, bool radial, Func<Vector2, Vector2, bool> visible = null)
    {
        float range = radial ? 160f : 260f;
        Rebuild();
        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            if (solid[i])
                continue;
            Vector2 center = Center(i);
            Vector2 offset = center - position;
            float distance = offset.Length();
            if (distance >= range)
                continue;
            Vector2 outward = distance > 0.01f ? offset / distance : direction;
            float cone = radial || distance < 28f ? 1f : Math.Clamp((Vector2.Dot(outward, direction) - 0.3f) * 2.5f, 0f, 1f);
            if (cone <= 0f || (visible is not null && !visible(position, center)))
                continue;
            float falloff = (1f - distance / range) * cone;
            Vector2 tangent = new(-outward.Y, outward.X);
            velocity[i] += (-outward * 0.24f + tangent * 0.035f) * (falloff * CellSize / CellPixels);
            if (distance < 24f)
            {
                dye[i] *= 0.15f;
                if (pendingInjection is not null && pendingInjection.TryGetValue(i, out GasDye queued))
                    pendingInjection[i] = queued * 0.15f;
                source[i] -= 1.5f * (1f - distance / 24f);
            }
        }
    }

    public void Step(float timeStep = 1f)
    {
        if (chunks.Count == 0)
        {
            fastUpdateTicks = 0;
            return;
        }
        timeStep = Math.Clamp(timeStep, 0.25f, 2f);
        GrowAndPrune();
        Rebuild();
        if (activeCount == 0)
        {
            fastUpdateTicks = 0;
            return;
        }
        if (Revision % 12 == 0 && obstacle is not null)
        {
            pressureTopologyDirty = true;
            Array.Clear(solidBlocks);
            for (int k = 0; k < activeCount; k++)
            {
                int i = active[k];
                solid[i] = obstacle(Center(i));
                if (solid[i])
                    solidBlocks[(i - 1) / ChunkCells] |= (ushort)(1 << (((cellY[i] & 15) >> 2) * 4 + ((cellX[i] & 15) >> 2)));
                if (solid[i])
                {
                    dye[i] = default;
                    pendingInjection?.Remove(i);
                    velocity[i] = Vector2.Zero;
                }
            }
        }

        int steps = CellPixels < CellSize && FastUpdates ? Math.Max(1, (int)MathF.Ceiling(timeStep / 0.5f)) : 1;
        if (steps > 1)
        {
            foreach (var injection in pendingInjection)
                dye[injection.Key] -= injection.Value * (1f - 1f / steps);
        }
        for (int step = 0; step < steps; step++)
        {
            if (step > 0)
            {
                foreach (var injection in pendingInjection)
                {
                    int i = injection.Key;
                    dye[i] += injection.Value * (1f / steps);
                    if (dye[i].Density > 6f)
                        dye[i] *= 6f / dye[i].Density;
                }
            }
            StepCore(timeStep / steps);
        }
        pendingInjection?.Clear();
        if (fastUpdateTicks > 0)
            fastUpdateTicks--;
    }

    private void StepCore(float timeStep)
    {
        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            if (solid[i])
                continue;
            Vector2 faceX = new(cellX[i] + 1f, cellY[i] + 0.5f);
            Vector2 faceY = new(cellX[i] + 0.5f, cellY[i] + 1f);
            Vector2 flowX = new(velocity[i].X, (velocity[i].Y + velocity[right[i]].Y + velocity[up[i]].Y + velocity[right[up[i]]].Y) * 0.25f);
            Vector2 flowY = new((velocity[i].X + velocity[down[i]].X + velocity[left[i]].X + velocity[down[left[i]]].X) * 0.25f, velocity[i].Y);
            if (flowX == Vector2.Zero && flowY == Vector2.Zero)
            {
                velocityNext[i] = Vector2.Zero;
                continue;
            }
            Vector2 traceX = Trace(faceX, -flowX * timeStep);
            Vector2 traceY = Trace(faceY, -flowY * timeStep);
            velocityNext[i] = new Vector2(SampleComponent(traceX.X - 1f, traceX.Y - 0.5f, true), SampleComponent(traceY.X - 0.5f, traceY.Y - 1f, false)) * (1f - 0.002f * timeStep);
        }
        (velocity, velocityNext) = (velocityNext, velocity);
        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            curl[i] = (velocity[right[i]].Y - velocity[left[i]].Y - velocity[down[i]].X + velocity[up[i]].X) * 0.5f;
        }
        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            if (solid[i])
            {
                velocityNext[i] = Vector2.Zero;
                continue;
            }
            GasDye cell = dye[i];
            float inverseDensity = 1f / Math.Max(0.001f, cell.Density);
            Vector2 gradient = new(Math.Abs(curl[right[i]]) - Math.Abs(curl[left[i]]), Math.Abs(curl[down[i]]) - Math.Abs(curl[up[i]]));
            gradient /= Math.Max(0.00001f, gradient.Length());
            float confinement = Math.Clamp(cell.Material.Y * inverseDensity, 0f, 1f) * 0.18f;
            Vector2 force = new(gradient.Y * curl[i], -gradient.X * curl[i]);
            float viscosity = Math.Clamp(cell.Material.Z * inverseDensity, 0f, 0.15f);
            Vector2 laplacian = velocity[left[i]] + velocity[right[i]] + velocity[up[i]] + velocity[down[i]] - velocity[i] * 4f;
            Vector2 value = velocity[i] + (laplacian * viscosity + force * confinement) * timeStep;
            value.Y -= cell.Material.X * 0.45f * timeStep * CellSize / CellPixels;
            float speed = value.Length();
            float limit = 2.5f * CellSize / CellPixels;
            velocityNext[i] = speed > limit ? value * (limit / speed) : value;
        }
        (velocity, velocityNext) = (velocityNext, velocity);
        Project(CellPixels < CellSize ? 6 : 12);

        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            Vector2 position = new(cellX[i] + 0.5f, cellY[i] + 0.5f);
            Vector2 flow = new((velocity[i].X + velocity[left[i]].X) * 0.5f, (velocity[i].Y + velocity[up[i]].Y) * 0.5f);
            dyeTrace[i] = solid[i] ? position : Trace(position, -flow * timeStep);
            predicted[i] = solid[i] ? default : SampleDye(dye, dyeTrace[i]);
        }
        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            if (solid[i] || (predicted[i].Density < 0.0003f && dye[i].Density < 0.0003f))
            {
                dyeNext[i] = default;
                source[i] = 0f;
                continue;
            }
            Vector2 position = new(cellX[i] + 0.5f, cellY[i] + 0.5f);
            Vector2 flow = new((velocity[i].X + velocity[left[i]].X) * 0.5f, (velocity[i].Y + velocity[up[i]].Y) * 0.5f);
            GasDye reverse = SampleDye(predicted, Trace(position, flow * timeStep));
            Corners(dye, dyeTrace[i], out GasDye a, out GasDye b, out GasDye c, out GasDye d, out _, out _);
            GasDye min = GasDye.Min(GasDye.Min(a, b), GasDye.Min(c, d));
            GasDye max = GasDye.Max(GasDye.Max(a, b), GasDye.Max(c, d));
            GasDye corrected = GasDye.Clamp(predicted[i] + (dye[i] - reverse) * 0.5f, min, max);
            float decay = corrected.Material.W / Math.Max(corrected.Density, 0.001f);
            corrected *= 1f / (1f + Math.Clamp(decay, 0.002f, 0.1f) * timeStep);
            if (corrected.ColorFade.W > 0f && corrected.Density >= 0.0003f)
            {
                float rate = corrected.ColorFade.W / corrected.Density;
                Vector4 target = corrected.ColorFade * (corrected.Density / corrected.ColorFade.W);
                target.W = corrected.Density;
                corrected.Optical = Vector4.Lerp(corrected.Optical, target, 1f - MathF.Exp(-rate * timeStep));
            }
            dyeNext[i] = corrected.Density < 0.0003f ? default : corrected;
            source[i] = 0f;
        }
        (dye, dyeNext) = (dyeNext, dye);
        Revision++;
    }

    internal void Project(int iterations)
    {
        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            velocity[i].X = Face(velocity[i].X, i, right[i]);
            velocity[i].Y = Face(velocity[i].Y, i, down[i]);
        }
        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            divergence[i] = solid[i] ? 0f : velocity[i].X - velocity[left[i]].X + velocity[i].Y - velocity[up[i]].Y - source[i];
            pressure[i] = 0f;
        }
        if (pressureTopologyDirty)
        {
            for (int k = 0; k < activeCount; k++)
            {
                int i = active[k];
                pressureLeft[i] = solid[left[i]] ? 0 : left[i];
                pressureRight[i] = solid[right[i]] ? 0 : right[i];
                pressureUp[i] = solid[up[i]] ? 0 : up[i];
                pressureDown[i] = solid[down[i]] ? 0 : down[i];
                int count = (solid[left[i]] ? 0 : 1) + (solid[right[i]] ? 0 : 1) + (solid[up[i]] ? 0 : 1) + (solid[down[i]] ? 0 : 1);
                inverseNeighbors[i] = count > 0 ? 1f / count : 0f;
            }
            pressureTopologyDirty = false;
        }

        for (int iteration = 0; iteration < iterations; iteration++)
            for (int parity = 0; parity < 2; parity++)
            {
                int[] cells = parityCells[parity];
                for (int k = 0; k < activeCount / 2; k++)
                {
                    int i = cells[k];
                    if (solid[i])
                        continue;
                    float sum = pressure[pressureLeft[i]] + pressure[pressureRight[i]] + pressure[pressureUp[i]] + pressure[pressureDown[i]];
                    pressure[i] = MathHelper.Lerp(pressure[i], (sum - divergence[i]) * inverseNeighbors[i], 1.5f);
                }
            }
        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            velocity[i].X = Face(velocity[i].X - pressure[right[i]] + pressure[i], i, right[i]);
            velocity[i].Y = Face(velocity[i].Y - pressure[down[i]] + pressure[i], i, down[i]);
        }
    }

    private void GrowAndPrune()
    {
        pending.Clear();
        foreach (Chunk chunk in chunks.Values)
        {
            float peak = 0f;
            int requested = 0;
            for (int y = 0; y < ChunkSize; y++)
                for (int x = 0; x < ChunkSize; x++)
                {
                    int i = chunk.Offset + y * ChunkSize + x;
                    float density = dye[i].Density;
                    peak = Math.Max(peak, density);
                    if (density < 0.025f)
                        continue;
                    if (x < 6 && (requested & 1) == 0)
                    {
                        requested |= 1;
                        pending.Add(new Point(chunk.Position.X - 1, chunk.Position.Y));
                    }
                    if (x >= ChunkSize - 6 && (requested & 2) == 0)
                    {
                        requested |= 2;
                        pending.Add(new Point(chunk.Position.X + 1, chunk.Position.Y));
                    }
                    if (y < 6 && (requested & 4) == 0)
                    {
                        requested |= 4;
                        pending.Add(new Point(chunk.Position.X, chunk.Position.Y - 1));
                    }
                    if (y >= ChunkSize - 6 && (requested & 8) == 0)
                    {
                        requested |= 8;
                        pending.Add(new Point(chunk.Position.X, chunk.Position.Y + 1));
                    }
                }
            chunk.Peak = peak;
            chunk.EmptyTicks = peak < 0.001f ? chunk.EmptyTicks + 1 : 0;
        }
        foreach (Point point in pending)
        {
            AddChunk(point);
            if (chunks.TryGetValue(Key(point.X, point.Y), out Chunk supported))
                supported.EmptyTicks = 0;
        }
        pending.Clear();
        foreach (Chunk chunk in chunks.Values)
            if (chunk.EmptyTicks > 60)
                pending.Add(chunk.Position);
        foreach (Point point in pending)
        {
            long key = Key(point.X, point.Y);
            Chunk chunk = chunks[key];
            for (int i = chunk.Offset; i < chunk.Offset + ChunkCells; i++)
            {
                dye[i] = dyeNext[i] = predicted[i] = default;
                pendingInjection?.Remove(i);
                velocity[i] = velocityNext[i] = Vector2.Zero;
                pressure[i] = source[i] = curl[i] = 0f;
                solid[i] = false;
            }
            chunks.Remove(key);
            cachedX = int.MinValue;
            free.Push((chunk.Offset - 1) / ChunkCells);
            topologyDirty = true;
        }
    }

    public float TotalDensity()
    {
        float total = 0f;
        for (int k = 0; k < activeCount; k++) total += dye[active[k]].Density;
        return total;
    }

    public float DivergenceNorm()
    {
        float sum = 0f;
        for (int k = 0; k < activeCount; k++)
        {
            int i = active[k];
            if (solid[i]) continue;
            float value = velocity[i].X - velocity[left[i]].X + velocity[i].Y - velocity[up[i]].Y;
            sum += value * value;
        }
        return MathF.Sqrt(sum / Math.Max(1, activeCount));
    }
}
