using System;
using System.Collections.Generic;

namespace AerovelenceMod.Common.Utilities;

public sealed partial class SlimeRenderer
{
    private void PaintCrystalFacet(HashSet<int> mask, Vector2 root, Vector2 axis, Vector2 side, float length, float halfWidth, int variant, SlimeFacetStyle facet)
    {
        Vector3 light = Vector3.Normalize(new Vector3(LightDirection * .78f, .72f));
        Vector3 fill = Vector3.Normalize(new Vector3(-LightDirection * .4f, .9f));
        float litSide = (variant & 2) == 0 ? 1f : -1f;
        foreach (int index in mask)
        {
            Vector2 point = new(index % width + .5f, index / width + .5f);
            Vector2 offset = point - root;
            float along = Vector2.Dot(offset, axis) / length;
            float taper = Math.Max(.2f, (1f - along) / .8f);
            float across = Vector2.Dot(offset, side) / (halfWidth * Math.Min(1f, taper));
            bool litFace = across * litSide >= -.15f;
            float slope = litFace ? .55f : -1.1f;
            Vector3 normal = Vector3.Normalize(new Vector3(side * (slope * litSide) + axis * (.25f + along * .6f), .95f));
            float diffuse = .18f + Math.Max(0f, Vector3.Dot(normal, light)) * .65f
                + Math.Max(0f, Vector3.Dot(normal, fill)) * .2f;
            diffuse += MathHelper.SmoothStep(0f, .12f, MathHelper.Clamp((along - .55f) / .4f, 0f, 1f))
                * Math.Max(0f, -Vector2.Dot(axis, LightDirection));
            float depth = -CrystalFacetDistance(point);
            float ridgeDistance = Math.Abs(Vector2.Dot(offset, side) + .15f * litSide * halfWidth * Math.Min(1f, taper));
            float refractionDistance = Math.Abs(Vector2.Dot(offset, side) - .25f * litSide * halfWidth * Math.Min(1f, taper));
            Color color = litFace ? facet.Mid : facet.Dark;
            if (!litFace && diffuse < .45f) color = palette.BackBright;
            if (!litFace && depth < .55f) color = diffuse < .45f ? palette.BackBright : palette.BackSecondary;
            if (!litFace && ridgeDistance < .4f && depth >= .55f) color = facet.Mid;
            if (litFace && diffuse > .72f && refractionDistance < .65f && along > .16f) color = facet.Bright;
            if (facet.InteriorLine && litFace && refractionDistance < .35f && diffuse > .81f)
            {
                if (along > .87f && diffuse > .84f) color = facet.Highlight;
                else if (along > .42f && along < .64f) color = diffuse > .835f ? facet.AccentA : facet.AccentB;
            }
            pixels[index] = Lit(color, color == facet.Bright ? .1f : 0f);
        }
    }

    private float CrystalFacetDistance(Vector2 point)
    {
        float nearest = float.MaxValue;
        for (int index = 0; index < facetPolygon.Length; index++)
        {
            Vector2 start = facetPolygon[index], edge = facetPolygon[(index + 1) % facetPolygon.Length] - start;
            float t = MathHelper.Clamp(Vector2.Dot(point - start, edge) / Math.Max(.001f, edge.LengthSquared()), 0f, 1f);
            nearest = Math.Min(nearest, Vector2.Distance(point, start + edge * t));
        }
        return PointInPolygon(point, facetPolygon) ? -nearest : nearest;
    }

    private void DrawCrystalOutline(HashSet<int> mask, SlimeFacetStyle facet)
    {
        facetOutlineMask.Clear();
        foreach (int index in mask)
        {
            int x = index % width, y = index / width;
            if (x > 1) facetOutlineMask.Add(index - 1);
            if (x < width - 2) facetOutlineMask.Add(index + 1);
            if (y > 1) facetOutlineMask.Add(index - width);
            if (y < height - 2) facetOutlineMask.Add(index + width);
        }
        Vector2 center = Vector2.Zero;
        foreach (Vector2 vertex in facetPolygon) center += vertex / facetPolygon.Length;
        foreach (int index in facetOutlineMask)
        {
            if (mask.Contains(index) || pixels[index].A > 0) continue;
            Vector2 point = new(index % width + .5f, index / width + .5f);
            Vector2 normal = Vector2.UnitY;
            float nearest = float.MaxValue;
            for (int edgeIndex = 0; edgeIndex < facetPolygon.Length; edgeIndex++)
            {
                Vector2 start = facetPolygon[edgeIndex], edge = facetPolygon[(edgeIndex + 1) % facetPolygon.Length] - start;
                float t = MathHelper.Clamp(Vector2.Dot(point - start, edge) / Math.Max(.001f, edge.LengthSquared()), 0f, 1f);
                float distance = Vector2.DistanceSquared(point, start + edge * t);
                if (distance >= nearest) continue;
                nearest = distance;
                normal = SafeNormalize(new Vector2(-edge.Y, edge.X), Vector2.UnitY);
                if (Vector2.Dot(normal, start + edge * .5f - center) < 0f) normal = -normal;
            }
            float facing = Vector2.Dot(normal, LightDirection);
            Color color = facing > .2f ? facet.OutlineLight ?? facet.Outline.Value : facet.Outline.Value;
            if (facing > .72f && facet.OutlineHighlight.HasValue) color = facet.OutlineHighlight.Value;
            else if (facing > -.35f && facing <= .2f && facet.OutlineMid.HasValue) color = facet.OutlineMid.Value;
            pixels[index] = Lit(color);
        }
    }

}
