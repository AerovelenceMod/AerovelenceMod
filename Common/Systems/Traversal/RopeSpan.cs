using System;
using System.Collections.Generic;
using System.Linq;



namespace AerovelenceMod.Common.Systems.Traversal
{
    public sealed class RopeSpan
    {
        public int Id { get; }
        public Point Left { get; }
        public Point Right { get; }
        public bool Zipline { get; }
        public int RopeType { get; internal set; }
        public bool LeftCeiling { get; }
        public bool RightCeiling { get; }
        public float Sag { get; }
        public Vector2[] Nodes { get; }
        public Vector2[] Previous { get; }
        public Vector2[] Rest { get; }
        public Point[] DeckTiles { get; }
        public Point[] RopeTiles { get; }
        private readonly float[] velocity;
        private readonly float[] load;
        private readonly float maximumStep;
        private readonly Vector2 motionAxis;
        public int Segments { get; }
        internal int PathSegments => Nodes.Length - 1;
        public int Sections => Segments - 1;
        public Rectangle Bounds { get; }

        internal bool HookFacesRight(bool right) => !right || Left.X == Right.X;

        internal Vector2 HookPosition(bool right)
        {
            Point post = right ? Right : Left;
            bool ceiling = right ? RightCeiling : LeftCeiling;
            bool facingRight = HookFacesRight(right);
            int offset = Left.X == Right.X ? 9 : Math.Min(9, (Right.X - Left.X) * 8 - 1);
            int x = post.X * 16 + 8 + (facingRight ? offset : -offset) - (facingRight ? 5 : 7);
            return new Vector2(x, post.Y * 16 + (ceiling ? 2 : -48));
        }

        internal Vector2 HookAnchor(bool right) => HookPosition(right) + new Vector2(HookFacesRight(right) ? 5 : 7, 7);

        internal Vector2 BridgeAnchor(bool right, bool rail)
        {
            Point post = right ? Right : Left;
            return new Vector2(post.X * 16 + (right ? 0 : 16), post.Y * 16 + (rail ? -18 : 12));
        }

        public RopeSpan(int id, Point left, Point right, bool zipline, int ropeType, float? sag = null, int? segments = null,
            bool? leftCeiling = null, bool? rightCeiling = null)
        {
            Id = id;
            Left = left;
            Right = right;
            Zipline = zipline;
            RopeType = ropeType;
            LeftCeiling = zipline && (leftCeiling ?? RopeSpanSystem.CeilingPost(left));
            RightCeiling = zipline && (rightCeiling ?? RopeSpanSystem.CeilingPost(right));
            int width = right.X - left.X;
            int height = Math.Abs(right.Y - left.Y);
            int extent = Math.Max(width, height);
            Segments = segments ?? (zipline ? Math.Max(2, extent) : width);
            int count = Segments + 1;
            int nodeCount = count + (zipline ? 2 : 0);
            motionAxis = zipline && width == 0 ? Vector2.UnitX : Vector2.UnitY;
            Nodes = new Vector2[nodeCount];
            Previous = new Vector2[nodeCount];
            Rest = new Vector2[nodeCount];
            velocity = new float[nodeCount];
            load = new float[nodeCount];
            DeckTiles = zipline ? Array.Empty<Point>() : new Point[count - 2];
            List<Point> ropeTiles = new();
            Vector2 a = new(left.X * 16 + 8, (left.Y + 1) * 16 - (zipline ? LeftCeiling ? 6 : 58 : 4));
            Vector2 b = new(right.X * 16 + 8, (right.Y + 1) * 16 - (zipline ? RightCeiling ? 6 : 58 : 4));
            Sag = sag ?? (zipline ? Math.Min(64f, count * 0.65f) * width / Math.Max(1f, extent)
                : Math.Min(12f, count * 0.3f));
            maximumStep = 9;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                int node = i + (zipline ? 1 : 0);
                Rest[node] = Vector2.Lerp(a, b, t) + new Vector2(0, Sag * 4 * t * (1 - t));
                Nodes[node] = Previous[node] = Rest[node];
                if (i > 0) maximumStep = Math.Max(maximumStep, Math.Abs(Vector2.Dot(Rest[node] - Rest[node - 1], motionAxis)));
                if (i == 0 || i == count - 1) continue;
                Point tile = Rest[node].ToTileCoordinates();
                if (!zipline) DeckTiles[i - 1] = tile;
                if (!zipline || !PostTile(tile, left) && !PostTile(tile, right))
                    ropeTiles.Add(new Point(tile.X, tile.Y - (zipline ? 0 : 2)));
            }
            if (zipline)
            {
                Rest[1] = Nodes[1] = Previous[1] = HookAnchor(false);
                Rest[^2] = Nodes[^2] = Previous[^2] = HookAnchor(true);
                Rest[0] = Nodes[0] = Previous[0] = RopeSpanSystem.JunctionAnchor(this, false);
                Rest[^1] = Nodes[^1] = Previous[^1] = RopeSpanSystem.JunctionAnchor(this, true);
            }
            RopeTiles = ropeTiles.Distinct().ToArray();
            Bounds = new Rectangle(left.X * 16 - 32, (int)Math.Min(a.Y, b.Y) - 64,
                (width + 1) * 16 + 64, (int)Math.Abs(b.Y - a.Y) + 160);
        }

        internal void SetJunction(Point post, Vector2 anchor)
        {
            int node = post == Left ? 0 : Nodes.Length - 1;
            Rest[node] = Nodes[node] = Previous[node] = anchor;
        }

        private static bool PostTile(Point tile, Point post)
            => tile.X == post.X && tile.Y <= post.Y + 1 && tile.Y >= post.Y - 3;

        public void AddLoad(float parameter, float weight)
        {
            float node = MathHelper.Clamp(parameter, 0, 1) * (Nodes.Length - 1);
            int i = Math.Min((int)node, Nodes.Length - 2);
            load[i] += weight * (1 - (node - i));
            load[i + 1] += weight * (node - i);
        }

        public void Update()
        {
            Array.Copy(Nodes, Previous, Nodes.Length);
            int first = Zipline ? 2 : 1;
            int last = Nodes.Length - first - 1;
            for (int i = first; i <= last; i++)
            {
                float offset = Vector2.Dot(Nodes[i] - Rest[i], motionAxis);
                float neighbors = Vector2.Dot(Nodes[i - 1] - Rest[i - 1] + Nodes[i + 1] - Rest[i + 1], motionAxis) * 0.5f;
                velocity[i] = (velocity[i] + (neighbors - offset) * 0.18f - offset * 0.035f + load[i] * 0.38f) * 0.9f;
            }
            for (int i = first; i <= last; i++)
                Nodes[i] = Rest[i] + motionAxis * MathHelper.Clamp(Vector2.Dot(Nodes[i] - Rest[i], motionAxis) + velocity[i], -8, 18);
            for (int i = first; i <= last; i++)
            {
                float remaining = (last + 1 - i) * maximumStep;
                float lower = Math.Max(Vector2.Dot(Nodes[i - 1], motionAxis) - maximumStep, Vector2.Dot(Nodes[last + 1], motionAxis) - remaining);
                float upper = Math.Min(Vector2.Dot(Nodes[i - 1], motionAxis) + maximumStep, Vector2.Dot(Nodes[last + 1], motionAxis) + remaining);
                Nodes[i] += motionAxis * (MathHelper.Clamp(Vector2.Dot(Nodes[i], motionAxis), lower, upper) - Vector2.Dot(Nodes[i], motionAxis));
            }
            for (int i = last; i >= first; i--)
            {
                float lower = Math.Max(Vector2.Dot(Nodes[i + 1], motionAxis) - maximumStep, Vector2.Dot(Nodes[first - 1], motionAxis) - (i - first + 1) * maximumStep);
                float upper = Math.Min(Vector2.Dot(Nodes[i + 1], motionAxis) + maximumStep, Vector2.Dot(Nodes[first - 1], motionAxis) + (i - first + 1) * maximumStep);
                Nodes[i] += motionAxis * (MathHelper.Clamp(Vector2.Dot(Nodes[i], motionAxis), lower, upper) - Vector2.Dot(Nodes[i], motionAxis));
                if (!Zipline)
                {
                    Vector2 movement = Nodes[i] - Previous[i];
                    Vector2 allowed = Collision.noSlopeCollision(Previous[i] - new Vector2(6, 1), movement, 12, 1);
                    Nodes[i] = Previous[i] + allowed;
                }
                velocity[i] = Vector2.Dot(Nodes[i] - Previous[i], motionAxis);
            }
            Array.Clear(load);
        }

        public float Parameter(float x) => Nodes[^1].X == Nodes[0].X ? 0
            : MathHelper.Clamp((x - Nodes[0].X) / (Nodes[^1].X - Nodes[0].X), 0, 1);

        public float Advance(float parameter, float distance, out float remaining)
        {
            float index = MathHelper.Clamp(parameter, 0, 1) * PathSegments;
            int direction = Math.Sign(distance);
            float travel = Math.Abs(distance);
            while (direction != 0 && (direction > 0 ? index < PathSegments : index > 0))
            {
                int segment = direction > 0 ? (int)MathF.Floor(index) : (int)MathF.Ceiling(index) - 1;
                int end = direction > 0 ? segment + 1 : segment;
                float length = Vector2.Distance(Nodes[segment], Nodes[segment + 1]);
                float available = Math.Abs(end - index) * length;
                if (travel < available)
                {
                    remaining = 0;
                    return (index + direction * travel / length) / PathSegments;
                }
                travel -= available;
                index = end;
            }
            remaining = travel * direction;
            return index / PathSegments;
        }

        public Vector2 At(float parameter, bool previous = false)
        {
            Vector2[] points = previous ? Previous : Nodes;
            float index = MathHelper.Clamp(parameter, 0, 1) * (points.Length - 1);
            int i = Math.Min((int)index, points.Length - 2);
            return Vector2.Lerp(points[i], points[i + 1], index - i);
        }

        public float SurfaceY(float left, float right, bool previous = false)
        {
            float surface = Math.Min(At(Parameter(left), previous).Y, At(Parameter(right), previous).Y);
            Vector2[] points = previous ? Previous : Nodes;
            int start = Math.Max(1, (int)MathF.Ceiling(Parameter(left) * (points.Length - 1)));
            int end = Math.Min(points.Length - 2, (int)(Parameter(right) * (points.Length - 1)));
            for (int i = start; i <= end; i++) surface = Math.Min(surface, points[i].Y);
            return surface;
        }

        public Vector2 Tangent(float parameter)
        {
            int i = Math.Min((int)(MathHelper.Clamp(parameter, 0, 1) * (Nodes.Length - 1)), Nodes.Length - 2);
            if (Zipline) i = Math.Clamp(i, 1, Nodes.Length - 3);
            return Vector2.Normalize(Nodes[i + 1] - Nodes[i]);
        }

        internal bool ClosestReachable(Vector2 position, Vector2 origin, float reach, out float parameter)
        {
            parameter = 0;
            float distance = float.MaxValue;
            for (int i = 0; i < Nodes.Length - 1; i++)
            {
                Vector2 edge = Nodes[i + 1] - Nodes[i];
                float lengthSquared = edge.LengthSquared();
                if (lengthSquared < 0.0001f) continue;
                Vector2 relative = Nodes[i] - origin;
                float projection = Vector2.Dot(relative, edge) / lengthSquared;
                float remaining = reach * reach - (relative.LengthSquared() - projection * projection * lengthSquared);
                if (remaining < 0) continue;
                float extent = MathF.Sqrt(remaining / lengthSquared);
                float start = Math.Max(0, -projection - extent), end = Math.Min(1, -projection + extent);
                if (start > end) continue;
                float t = MathHelper.Clamp(Vector2.Dot(position - Nodes[i], edge) / lengthSquared, start, end);
                float d = Vector2.DistanceSquared(position, Nodes[i] + edge * t);
                if (d >= distance) continue;
                distance = d;
                parameter = (i + t) / (Nodes.Length - 1);
            }
            return distance < float.MaxValue;
        }

        public float Closest(Vector2 position)
        {
            float result = 0, distance = float.MaxValue;
            for (int i = 0; i < Nodes.Length - 1; i++)
            {
                Vector2 edge = Nodes[i + 1] - Nodes[i];
                float lengthSquared = edge.LengthSquared();
                if (lengthSquared < 0.0001f) continue;
                float t = MathHelper.Clamp(Vector2.Dot(position - Nodes[i], edge) / lengthSquared, 0, 1);
                float d = Vector2.DistanceSquared(position, Nodes[i] + edge * t);
                if (d >= distance) continue;
                distance = d;
                result = (i + t) / (Nodes.Length - 1);
            }
            return result;
        }

        public IEnumerable<Point> Tiles()
        {
            foreach (Point point in DeckTiles) yield return point;
            foreach (Point point in RopeTiles) yield return point;
        }
    }
}
