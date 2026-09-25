using System;
using System.Collections.Generic;

namespace AerovelenceMod.Common.Systems.Generation.CrystalCaverns;

public enum CitadelStructureArchetype
{
    Watchtower,
    TerraceHouse,
    Gatehouse,
    Loomery,
    Shrine,
    Archive,
    Barracks,
    Nursery,
    Cistern,
    BridgeHouse,
    CourtyardKeep,
    GrandSanctum,
    RuinedBlock
}

public static class CitadelStructureGenerator
{
    public static CitadelHouseStamp Create(
        CitadelHouseSite source,
        int worldSeed,
        int siteX,
        int siteY,
        CitadelSiteProfile profile,
        bool ruined)
    {
        Random random = new(Seed(worldSeed, siteX, siteY, source.Id, ruined));
        int band = siteY < 155 ? 0 : siteY > 210 ? 2 : 1;
        CitadelStructureArchetype archetype = ChooseArchetype(random, band, profile, ruined);
        CitadelStructureBuilder builder = new();

        int centerX = builder.Width / 2;
        int centerY = builder.Height / 2;
        int facing = profile.OpenDirection != 0 ? profile.OpenDirection : random.Next(2) == 0 ? -1 : 1;

        switch (archetype)
        {
            case CitadelStructureArchetype.Watchtower:
                BuildWatchtower(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.TerraceHouse:
                BuildTerrace(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.Gatehouse:
                BuildGatehouse(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.Loomery:
                BuildLoomery(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.Shrine:
                BuildShrine(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.Archive:
                BuildArchive(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.Barracks:
                BuildBarracks(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.Nursery:
                BuildNursery(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.Cistern:
                BuildCistern(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.BridgeHouse:
                BuildBridgeHouse(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.CourtyardKeep:
                BuildCourtyardKeep(builder, random, centerX, centerY, facing, profile);
                break;
            case CitadelStructureArchetype.GrandSanctum:
                BuildGrandSanctum(builder, random, centerX, centerY, facing, profile);
                break;
            default:
                BuildRuinedBlock(builder, random, centerX, centerY, facing, profile);
                break;
        }

        AddSiteAdaptation(builder, random, centerX, centerY, facing, profile, band);
        AddAgeAndDamage(builder, random, ruined, archetype, band);
        AddSilkInfestation(builder, random, ruined, archetype, band);
        AddFoundations(builder, random, centerX, centerY, archetype, profile, band);
        AddSmallDetails(builder, random, archetype, facing);
        builder.ReplaceExposedBrickWithStone(random, ruined ? 0.32 : 0.12 + band * 0.03);
        builder.AddRubbleBelowDamage(random, ruined ? 40 : 16);

        string id = source.Id + "_" + archetype;
        return builder.ToStamp(id, source.PreviewX, source.PreviewY, 2);
    }

    private static CitadelStructureArchetype ChooseArchetype(Random random, int band, CitadelSiteProfile profile, bool ruined)
    {
        if (ruined)
        {
            return Weighted(random,
                (CitadelStructureArchetype.RuinedBlock, 32),
                (CitadelStructureArchetype.Watchtower, 12),
                (CitadelStructureArchetype.Shrine, 12),
                (CitadelStructureArchetype.BridgeHouse, 10),
                (CitadelStructureArchetype.CourtyardKeep, 10),
                (CitadelStructureArchetype.Gatehouse, 9),
                (CitadelStructureArchetype.Archive, 8),
                (CitadelStructureArchetype.Loomery, 7));
        }

        if (profile.Tall && profile.OpenHeight >= 34)
        {
            return Weighted(random,
                (CitadelStructureArchetype.Watchtower, 36),
                (CitadelStructureArchetype.Nursery, 20),
                (CitadelStructureArchetype.Shrine, 16),
                (CitadelStructureArchetype.Archive, 10),
                (CitadelStructureArchetype.BridgeHouse, 10),
                (CitadelStructureArchetype.TerraceHouse, 8));
        }

        if (profile.Wide && profile.OpenWidth >= 44)
        {
            return Weighted(random,
                (CitadelStructureArchetype.CourtyardKeep, 24),
                (CitadelStructureArchetype.Gatehouse, 19),
                (CitadelStructureArchetype.Loomery, 16),
                (CitadelStructureArchetype.BridgeHouse, 14),
                (CitadelStructureArchetype.Barracks, 12),
                (CitadelStructureArchetype.GrandSanctum, 7),
                (CitadelStructureArchetype.TerraceHouse, 8));
        }

        if (profile.Hanging)
        {
            return Weighted(random,
                (CitadelStructureArchetype.Nursery, 30),
                (CitadelStructureArchetype.BridgeHouse, 24),
                (CitadelStructureArchetype.Watchtower, 18),
                (CitadelStructureArchetype.Shrine, 14),
                (CitadelStructureArchetype.TerraceHouse, 14));
        }

        if (band == 0)
        {
            return Weighted(random,
                (CitadelStructureArchetype.Watchtower, 26),
                (CitadelStructureArchetype.Gatehouse, 17),
                (CitadelStructureArchetype.Shrine, 17),
                (CitadelStructureArchetype.BridgeHouse, 14),
                (CitadelStructureArchetype.TerraceHouse, 14),
                (CitadelStructureArchetype.Archive, 8),
                (CitadelStructureArchetype.GrandSanctum, 4));
        }

        if (band == 2)
        {
            return Weighted(random,
                (CitadelStructureArchetype.Cistern, 24),
                (CitadelStructureArchetype.Nursery, 18),
                (CitadelStructureArchetype.Barracks, 16),
                (CitadelStructureArchetype.Archive, 13),
                (CitadelStructureArchetype.Loomery, 11),
                (CitadelStructureArchetype.CourtyardKeep, 9),
                (CitadelStructureArchetype.TerraceHouse, 9));
        }

        return Weighted(random,
            (CitadelStructureArchetype.Loomery, 20),
            (CitadelStructureArchetype.Archive, 16),
            (CitadelStructureArchetype.TerraceHouse, 16),
            (CitadelStructureArchetype.Barracks, 14),
            (CitadelStructureArchetype.CourtyardKeep, 12),
            (CitadelStructureArchetype.Shrine, 8),
            (CitadelStructureArchetype.BridgeHouse, 8),
            (CitadelStructureArchetype.Cistern, 6));
    }

    private static void BuildWatchtower(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int towerWidth = random.Next(10, 14);
        int towerHeight = random.Next(19, 27);
        int towerX = cx - towerWidth / 2 - (facing < 0 ? 2 : -2);
        int towerY = cy - towerHeight / 2 - 4;
        var tower = new CitadelStructureBuilder.Box(towerX, towerY, towerWidth, towerHeight);
        b.Tower(tower, facing < 0, facing > 0, random.Next(3, 5), random.NextDouble() < 0.30);
        b.AddOuterButtresses(tower, random.Next(3, 6));

        int hallWidth = random.Next(12, 18);
        int hallHeight = random.Next(8, 12);
        int hallY = tower.Bottom - hallHeight + 1;
        int hallX = facing > 0 ? tower.Right + random.Next(2, 5) : tower.Left - hallWidth - random.Next(1, 4);
        var hall = new CitadelStructureBuilder.Box(hallX, hallY, hallWidth, hallHeight);
        b.Room(hall, facing < 0, facing > 0, true);
        FurnishResidential(b, hall);
        ConnectRooms(b, tower, hall, facing);

        if (random.NextDouble() < 0.75)
            b.Balcony(facing > 0 ? hall.Right : hall.Left, hall.Top + 4, facing, random.Next(5, 9), random.NextDouble() < 0.35);

        if (random.NextDouble() < 0.55)
        {
            int basementWidth = Math.Max(10, towerWidth + random.Next(0, 5));
            var basement = new CitadelStructureBuilder.Box(cx - basementWidth / 2, tower.Bottom + 1, basementWidth, random.Next(6, 9));
            b.Basement(basement, tower.CenterX);
        }
    }

    private static void BuildTerrace(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int rooms = random.Next(2, 4);
        int currentX = cx - facing * random.Next(12, 18);
        int baseY = cy + 4;
        CitadelStructureBuilder.Box previous = default;

        for (int i = 0; i < rooms; i++)
        {
            int w = random.Next(9, 14);
            int h = random.Next(7, 11);
            int x = facing > 0 ? currentX : currentX - w + 1;
            int y = baseY - i * random.Next(3, 6) - h;
            var room = new CitadelStructureBuilder.Box(x, y, w, h);
            b.Room(room, i == 0 && facing < 0, i == 0 && facing > 0, i % 2 == 0);
            b.VerticalWindows(room, Math.Max(1, w / 7));
            FurnishResidential(b, room);

            if (i > 0)
                ConnectRooms(b, previous, room, facing);

            previous = room;
            currentX = facing > 0 ? room.Right + random.Next(2, 5) : room.Left - random.Next(2, 5);
        }

        if (random.NextDouble() < 0.7)
            b.Balcony(previous.CenterX, previous.Bottom - 2, facing, random.Next(6, 10), false);
    }

    private static void BuildGatehouse(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int gap = random.Next(8, 13);
        int towerWidth = random.Next(8, 11);
        int towerHeight = random.Next(15, 21);
        int floorY = cy + 8;
        var left = new CitadelStructureBuilder.Box(cx - gap / 2 - towerWidth, floorY - towerHeight + 1, towerWidth, towerHeight);
        var right = new CitadelStructureBuilder.Box(cx + gap / 2, floorY - towerHeight + 1 + random.Next(-2, 3), towerWidth, towerHeight + random.Next(-2, 3));
        int gateTop = Math.Max(left.Top, right.Top) + random.Next(5, 8);
        b.Gatehouse(left, right, gateTop, floorY);
        b.AddOuterButtresses(left, 4);
        b.AddOuterButtresses(right, 4);

        if (random.NextDouble() < 0.55)
        {
            var upper = new CitadelStructureBuilder.Box(cx - random.Next(6, 9), gateTop - random.Next(7, 10), random.Next(12, 17), random.Next(6, 9));
            b.Room(upper, false, false, true);
            FurnishResidential(b, upper);
            b.RoseWindow(upper.CenterX, upper.CenterY, 1);
        }
    }

    private static void BuildLoomery(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int width = random.Next(24, 34);
        int height = random.Next(11, 16);
        var hall = new CitadelStructureBuilder.Box(cx - width / 2, cy - height / 2, width, height);
        b.LoomHall(hall);
        AddEntranceDoor(b, hall, facing);
        b.AddOuterButtresses(hall, random.Next(3, 6));

        int wingWidth = random.Next(8, 12);
        int wingHeight = random.Next(7, 10);
        var wing = new CitadelStructureBuilder.Box(
            facing > 0 ? hall.Right + random.Next(2, 5) : hall.Left - wingWidth - random.Next(1, 4),
            hall.Bottom - wingHeight + 1,
            wingWidth,
            wingHeight);
        b.Room(wing, facing < 0, facing > 0, false);
        FurnishResidential(b, wing);
        ConnectRooms(b, hall, wing, facing);

        if (random.NextDouble() < 0.65)
        {
            int upperWidth = random.Next(10, 15);
            int upperHeight = random.Next(6, 9);
            int upperBottom = hall.Top - random.Next(2, 5);
            var upper = new CitadelStructureBuilder.Box(hall.CenterX - upperWidth / 2 + random.Next(-3, 4), upperBottom - upperHeight + 1, upperWidth, upperHeight);
            b.Room(upper, false, false, true);
            FurnishResidential(b, upper);
            ConnectStacked(b, upper, hall, facing);
        }
    }

    private static void BuildShrine(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int width = random.Next(16, 23);
        int height = random.Next(14, 20);
        var chapel = new CitadelStructureBuilder.Box(cx - width / 2, cy - height / 2, width, height);
        b.Chapel(chapel, facing);
        AddEntranceDoor(b, chapel, facing);
        int shrineGalleryY = chapel.Bottom - 5;
        b.PlatformLine(chapel.Left + 3, chapel.Right - 3, shrineGalleryY);
        b.StairRun(chapel.Left + 4, shrineGalleryY, 1, Math.Max(2, chapel.Bottom - shrineGalleryY));
        b.StairRun(chapel.Right - 4, shrineGalleryY, -1, Math.Max(2, chapel.Bottom - shrineGalleryY));
        b.AddOuterButtresses(chapel, random.Next(4, 7));

        int sideWidth = random.Next(7, 10);
        int sideHeight = random.Next(7, 10);
        var left = new CitadelStructureBuilder.Box(chapel.Left - sideWidth - 1, chapel.Bottom - sideHeight + 1, sideWidth, sideHeight);
        var right = new CitadelStructureBuilder.Box(chapel.Right + 2, chapel.Bottom - sideHeight + 1, sideWidth, sideHeight);
        b.Room(left, true, false, false);
        b.Room(right, false, true, false);
        FurnishResidential(b, left);
        FurnishResidential(b, right);
        ConnectRooms(b, left, chapel, 1);
        ConnectRooms(b, chapel, right, 1);

        if (random.NextDouble() < 0.6)
        {
            int spireHeight = random.Next(7, 12);
            var tower = new CitadelStructureBuilder.Box(chapel.CenterX - 3, chapel.Top - spireHeight + 2, 7, spireHeight);
            b.Tower(tower, false, false, 1, random.NextDouble() < 0.25);
        }
    }

    private static void BuildArchive(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int width = random.Next(20, 29);
        int height = random.Next(15, 23);
        var archive = new CitadelStructureBuilder.Box(cx - width / 2, cy - height / 2, width, height);
        b.Archive(archive);
        AddEntranceDoor(b, archive, facing);
        b.AddOuterButtresses(archive, random.Next(3, 5));

        if (random.NextDouble() < 0.7)
        {
            int annexWidth = random.Next(8, 12);
            var annex = new CitadelStructureBuilder.Box(
                facing > 0 ? archive.Right + random.Next(2, 5) : archive.Left - annexWidth - random.Next(1, 4),
                archive.Bottom - 8,
                annexWidth,
                9);
            b.Room(annex, facing < 0, facing > 0, true);
            FurnishResidential(b, annex);
            ConnectRooms(b, archive, annex, facing);
        }

        if (random.NextDouble() < 0.5)
        {
            var cellar = new CitadelStructureBuilder.Box(archive.CenterX - 6, archive.Bottom + 1, 13, random.Next(6, 9));
            b.Basement(cellar, archive.CenterX);
        }
    }

    private static void BuildBarracks(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int lowerWidth = random.Next(20, 29);
        int lowerHeight = random.Next(8, 11);
        var lower = new CitadelStructureBuilder.Box(cx - lowerWidth / 2, cy, lowerWidth, lowerHeight);
        b.Room(lower, facing < 0, facing > 0, true);
        AddEntranceDoor(b, lower, facing);
        FurnishResidential(b, lower);

        int upperWidth = random.Next(14, lowerWidth - 2);
        int upperHeight = random.Next(7, 10);
        int offset = random.Next(-4, 5);
        var upper = new CitadelStructureBuilder.Box(cx - upperWidth / 2 + offset, lower.Top - upperHeight + 1, upperWidth, upperHeight);
        b.Room(upper, false, false, true);
        FurnishResidential(b, upper);

        for (int x = lower.Left + 3; x < lower.Right - 2; x += 5)
            b.Beam(x, lower.Top + 1, lower.Bottom - 1);
        ConnectStacked(b, upper, lower, facing);

        if (random.NextDouble() < 0.65)
            b.Balcony(facing > 0 ? upper.Right : upper.Left, upper.Bottom - 2, facing, random.Next(5, 9), true);
    }

    private static void BuildNursery(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int width = random.Next(17, 25);
        int height = random.Next(18, 27);
        var nursery = new CitadelStructureBuilder.Box(cx - width / 2, cy - height / 2, width, height);
        b.Nursery(nursery);
        AddEntranceDoor(b, nursery, facing);
        b.AddOuterButtresses(nursery, random.Next(3, 6));

        if (profile.Hanging || random.NextDouble() < 0.5)
        {
            for (int x = nursery.Left + 3; x < nursery.Right - 2; x += 5)
            {
                int length = random.Next(3, 8);
                for (int y = nursery.Top - length; y < nursery.Top; y++)
                    b.Set(x, y, y == nursery.Top - length ? 'B' : 'L');
            }
        }

        if (random.NextDouble() < 0.65)
        {
            int wingWidth = random.Next(8, 11);
            var wing = new CitadelStructureBuilder.Box(facing > 0 ? nursery.Right + random.Next(2, 5) : nursery.Left - wingWidth - random.Next(1, 4), nursery.Bottom - 8, wingWidth, 9);
            b.Room(wing, facing < 0, facing > 0, false);
            FurnishResidential(b, wing);
            ConnectRooms(b, nursery, wing, facing);
        }
    }

    private static void BuildCistern(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int width = random.Next(22, 31);
        int height = random.Next(13, 18);
        var cistern = new CitadelStructureBuilder.Box(cx - width / 2, cy - height / 2, width, height);
        b.Cistern(cistern);
        AddEntranceDoor(b, cistern, facing);
        b.AddOuterButtresses(cistern, random.Next(5, 8));

        int towerWidth = random.Next(7, 10);
        int towerHeight = random.Next(10, 15);
        var tower = new CitadelStructureBuilder.Box(
            facing > 0 ? cistern.Right - towerWidth + 1 : cistern.Left,
            cistern.Top - towerHeight + 2,
            towerWidth,
            towerHeight);
        b.Tower(tower, false, false, 2, random.NextDouble() < 0.45);
    }

    private static void BuildBridgeHouse(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int gap = random.Next(9, 16);
        int widthA = random.Next(9, 14);
        int widthB = random.Next(9, 14);
        int heightA = random.Next(10, 15);
        int heightB = random.Next(9, 14);
        int floorY = cy + random.Next(3, 8);

        var left = new CitadelStructureBuilder.Box(cx - gap / 2 - widthA, floorY - heightA + 1, widthA, heightA);
        var right = new CitadelStructureBuilder.Box(cx + gap / 2, floorY - heightB + 1 + random.Next(-3, 4), widthB, heightB);
        b.Room(left, facing < 0, false, true);
        b.Room(right, false, facing > 0, true);
        FurnishResidential(b, left);
        FurnishResidential(b, right);
        b.VerticalWindows(left, 2);
        b.VerticalWindows(right, 2);

        int bridgeY = Math.Min(left.Bottom - 2, right.Bottom - 2);
        if (random.NextDouble() < 0.55)
            b.Bridge(left.Right, right.Left, bridgeY, random.NextDouble() < 0.45);
        else
            b.StoneBridge(left.Right, right.Left, bridgeY, random.Next(2, 5));
        b.PlatformLine(left.Right - 3, left.Right, bridgeY);
        b.PlatformLine(right.Left, right.Left + 3, bridgeY);
        b.Door(left.Right, bridgeY);
        b.Door(right.Left, bridgeY);

        if (random.NextDouble() < 0.5)
            b.Balcony(facing > 0 ? right.Right : left.Left, bridgeY, facing, random.Next(5, 9), false);
    }

    private static void BuildCourtyardKeep(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int width = random.Next(27, 37);
        int height = random.Next(15, 21);
        var outer = new CitadelStructureBuilder.Box(cx - width / 2, cy - height / 2, width, height);
        b.Courtyard(outer, 2, facing);

        int roomWidth = random.Next(8, 12);
        int roomHeight = random.Next(7, 10);
        var left = new CitadelStructureBuilder.Box(outer.Left, outer.Bottom - roomHeight + 1, roomWidth, roomHeight);
        var right = new CitadelStructureBuilder.Box(outer.Right - roomWidth + 1, outer.Bottom - roomHeight + 1, roomWidth, roomHeight);
        b.Room(left, facing < 0, false, true);
        b.Room(right, false, facing > 0, true);
        FurnishResidential(b, left);
        FurnishResidential(b, right);

        int towerWidth = random.Next(7, 10);
        int towerHeight = random.Next(12, 18);
        var tower = new CitadelStructureBuilder.Box(
            facing > 0 ? outer.Left : outer.Right - towerWidth + 1,
            outer.Top - towerHeight / 2,
            towerWidth,
            towerHeight);
        b.Tower(tower, false, false, 2, random.NextDouble() < 0.25);
        b.AddOuterButtresses(outer, random.Next(3, 5));
    }

    private static void BuildGrandSanctum(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int width = random.Next(38, 47);
        int height = random.Next(20, 27);
        var hall = new CitadelStructureBuilder.Box(cx - width / 2, cy - height / 2, width, height);
        b.GrandHall(hall, random.Next(4, 7), true);
        AddEntranceDoor(b, hall, facing);
        int sanctumGalleryY = hall.Top + hall.Height / 2 + 1;
        b.PlatformLine(hall.Left + 4, hall.Right - 4, sanctumGalleryY);
        b.StairRun(hall.Left + 5, sanctumGalleryY, 1, Math.Max(2, hall.Bottom - sanctumGalleryY));
        b.StairRun(hall.Right - 5, sanctumGalleryY, -1, Math.Max(2, hall.Bottom - sanctumGalleryY));
        b.AddOuterButtresses(hall, random.Next(5, 8));

        int towerWidth = random.Next(8, 11);
        int towerHeight = random.Next(17, 23);
        var leftTower = new CitadelStructureBuilder.Box(hall.Left - towerWidth - 2, hall.Top - random.Next(2, 6), towerWidth, towerHeight);
        var rightTower = new CitadelStructureBuilder.Box(hall.Right + 3, hall.Top - random.Next(2, 6), towerWidth, towerHeight);
        b.Tower(leftTower, false, false, 3, random.NextDouble() < 0.25);
        b.Tower(rightTower, false, false, 3, random.NextDouble() < 0.25);
        ConnectRooms(b, leftTower, hall, 1);
        ConnectRooms(b, hall, rightTower, 1);

        int upperWidth = random.Next(13, 18);
        int upperHeight = random.Next(7, 10);
        int upperBottom = hall.Top - random.Next(2, 5);
        var upper = new CitadelStructureBuilder.Box(cx - upperWidth / 2, upperBottom - upperHeight + 1, upperWidth, upperHeight);
        b.Chapel(upper, facing);
        ConnectStacked(b, upper, hall, facing);
    }

    private static void BuildRuinedBlock(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile)
    {
        int pieces = random.Next(2, 5);
        int currentX = cx - random.Next(18, 24);
        int baseY = cy + random.Next(4, 10);

        for (int i = 0; i < pieces; i++)
        {
            int w = random.Next(8, 12);
            int h = random.Next(7, 13);
            int y = baseY - h - random.Next(-2, 5);
            var room = new CitadelStructureBuilder.Box(currentX, y, w, h);
            if (i == 0 && random.NextDouble() < 0.45)
                b.Tower(room, false, false, Math.Max(1, h / 6), true);
            else
                b.Room(room, false, false, random.NextDouble() < 0.5);

            if (random.NextDouble() < 0.8)
                b.JaggedCollapse(room.Left + random.Next(1, room.Width), room.Top + random.Next(0, Math.Max(1, room.Height / 2)), random.Next(2, 5), random.Next(2, 4), random);

            currentX += w + random.Next(2, 5);
        }
    }

    private static void AddSiteAdaptation(CitadelStructureBuilder b, Random random, int cx, int cy, int facing, CitadelSiteProfile profile, int band)
    {
        if (profile.WalledLeft)
        {
            int x = cx - Math.Min(18, profile.LeftDistance + 4);
            int y = cy + random.Next(5, 11);
            for (int i = 0; i < random.Next(2, 5); i++)
                b.RockRoot(x + i * 2, y + i, -1, random.Next(4, 8), random.Next(1, 3));
        }

        if (profile.WalledRight)
        {
            int x = cx + Math.Min(18, profile.RightDistance + 4);
            int y = cy + random.Next(5, 11);
            for (int i = 0; i < random.Next(2, 5); i++)
                b.RockRoot(x - i * 2, y + i, 1, random.Next(4, 8), random.Next(1, 3));
        }

        if (profile.Hanging)
        {
            int count = random.Next(2, 5);
            for (int i = 0; i < count; i++)
            {
                int x = cx + random.Next(-12, 13);
                int length = random.Next(4, 10);
                for (int y = cy - 15; y < cy - 15 + length; y++)
                    b.Set(x, y, y == cy - 15 ? 'B' : 'L', false);
            }
        }

        if (profile.Grounded && band == 2)
        {
            for (int i = 0; i < random.Next(2, 5); i++)
                b.RockRoot(cx + random.Next(-10, 11), cy + random.Next(7, 12), random.Next(2) == 0 ? -1 : 1, random.Next(6, 11), random.Next(1, 3));
        }
    }

    private static void AddSilkInfestation(CitadelStructureBuilder b, Random random, bool ruined, CitadelStructureArchetype archetype, int band)
    {
        int patches = ruined ? random.Next(5, 9) : random.Next(2 + band, 5 + band);
        if (archetype == CitadelStructureArchetype.Nursery) patches += 4;
        if (archetype == CitadelStructureArchetype.Loomery) patches += 2;

        for (int i = 0; i < patches; i++)
        {
            int x = random.Next(8, b.Width - 8);
            int y = random.Next(7, b.Height - 7);
            b.SilkPatch(x, y, random.Next(2, 6), random.Next(2, 5), random);
        }

        int hanging = random.Next(2, 5 + band);
        for (int i = 0; i < hanging; i++)
        {
            int x = random.Next(8, b.Width - 8);
            int top = FindTopSolid(b, x);
            if (top < 0) continue;
            int length = random.Next(2, 7);
            for (int y = top + 1; y <= Math.Min(b.Height - 2, top + length); y++)
            {
                if (b.Get(x, y) != '.' && b.Get(x, y) != 'w') break;
                b.Set(x, y, random.NextDouble() < 0.28 ? 'L' : 'v');
            }
        }
    }

    private static void AddAgeAndDamage(CitadelStructureBuilder b, Random random, bool ruined, CitadelStructureArchetype archetype, int band)
    {
        int hits = ruined ? random.Next(4, 8) : random.NextDouble() < 0.55 ? random.Next(1, 3) : 0;
        if (archetype == CitadelStructureArchetype.RuinedBlock) hits += random.Next(2, 5);

        for (int i = 0; i < hits; i++)
            if (!b.DamageOuterWall(random, random.Next(2, ruined ? 6 : 4), random.Next(2, ruined ? 5 : 4))) break;

        if (band == 2 && random.NextDouble() < 0.65)
        {
            int y = random.Next(b.Height / 2, b.Height - 6);
            int direction = random.Next(2) == 0 ? -1 : 1;
            b.RockRoot(random.Next(20, b.Width - 20), y, direction, random.Next(5, 10), random.Next(2, 4));
        }
    }

    private static void AddFoundations(CitadelStructureBuilder b, Random random, int cx, int cy, CitadelStructureArchetype archetype, CitadelSiteProfile profile, int band)
    {
        int roots = archetype is CitadelStructureArchetype.Gatehouse or CitadelStructureArchetype.CourtyardKeep or CitadelStructureArchetype.GrandSanctum ? random.Next(4, 7) : random.Next(2, 5);
        int minDepth = 3 + band;
        int maxDepth = 7 + band * 2;
        b.RootExistingFoundation(random, roots, minDepth, maxDepth);
    }

    private static void AddSmallDetails(CitadelStructureBuilder b, Random random, CitadelStructureArchetype archetype, int facing)
    {
        int chains = random.Next(2, 6);
        for (int i = 0; i < chains; i++)
        {
            int x = random.Next(7, b.Width - 7);
            int top = FindTopSolid(b, x);
            if (top < 0) continue;
            int length = random.Next(2, 6);
            for (int y = top + 1; y <= Math.Min(top + length, b.Height - 2); y++)
            {
                char current = b.Get(x, y);
                if (current != '.' && current != 'w' && current != 'a' && current != 'b') break;
                b.Set(x, y, 'L');
            }
        }

        if (archetype is CitadelStructureArchetype.Watchtower or CitadelStructureArchetype.Gatehouse or CitadelStructureArchetype.GrandSanctum)
        {
            int x = facing > 0 ? b.Width - 13 : 12;
            int y = b.Height / 2;
            for (int i = 0; i < 4; i++)
                b.Set(x + facing * i, y, 'P', false);
        }
    }

    private static void AddEntranceDoor(CitadelStructureBuilder b, CitadelStructureBuilder.Box box, int facing)
    {
        if (facing >= 0) b.Door(box.Right, box.Bottom);
        else b.Door(box.Left, box.Bottom);
    }

    private static void FurnishResidential(CitadelStructureBuilder b, CitadelStructureBuilder.Box box)
    {
        if (box.Width >= 10)
        {
            b.Workbench(box.Left + 3, box.Bottom);
            b.Chair(box.Left + 6, box.Bottom);
            if (box.Width >= 13) b.Bookcase(box.Right - 5, box.Bottom);
            b.Lantern(box.CenterX, box.Top + 2);
        }
    }

    private static void ConnectRooms(CitadelStructureBuilder b, CitadelStructureBuilder.Box first, CitadelStructureBuilder.Box second, int direction)
    {
        CitadelStructureBuilder.Box left = first.CenterX <= second.CenterX ? first : second;
        CitadelStructureBuilder.Box right = first.CenterX <= second.CenterX ? second : first;
        int overlapTop = Math.Max(left.Top + 3, right.Top + 3);
        int overlapBottom = Math.Min(left.Bottom - 1, right.Bottom - 1);
        if (overlapBottom - overlapTop < 2) return;
        int floorY = overlapBottom + 1;
        int x1 = left.Right;
        int x2 = right.Left;
        for (int y = floorY - 3; y < floorY; y++)
        {
            b.Set(x1, y, 'w');
            b.Set(x2, y, 'w');
        }
        b.PlatformLine(x1, x2, floorY);
        if (x2 - x1 >= 8 && (x1 + x2 + floorY) % 3 == 0)
            b.Bridge(x1, x2, floorY, true);
    }

    private static void ConnectStacked(CitadelStructureBuilder b, CitadelStructureBuilder.Box upper, CitadelStructureBuilder.Box lower, int facing)
    {
        int overlapLeft = Math.Max(upper.Left + 2, lower.Left + 3);
        int overlapRight = Math.Min(upper.Right - 2, lower.Right - 3);
        if (overlapRight - overlapLeft < 4) return;
        int direction = facing >= 0 ? 1 : -1;
        int topX = direction > 0 ? overlapLeft + 1 : overlapRight - 1;
        int topY = upper.Bottom;
        int drop = Math.Clamp(lower.Top - upper.Bottom + Math.Min(5, lower.Height - 3), 3, 8);
        b.Clear(topX - 1, topY, 3, Math.Max(1, lower.Top - topY + 1), 'w');
        b.StairRun(topX, topY, direction, drop);
    }

    private static int FindTopSolid(CitadelStructureBuilder b, int x)
    {
        for (int y = 1; y < b.Height - 1; y++)
        {
            char cell = b.Get(x, y);
            if (cell is 'B' or 's' or 'o' or 'I')
            {
                char below = b.Get(x, y + 1);
                if (below is '.' or 'w' or 'a' or 'b' or 'u')
                    return y;
            }
        }
        return -1;
    }

    private static CitadelStructureArchetype Weighted(Random random, params (CitadelStructureArchetype Type, int Weight)[] entries)
    {
        int total = 0;
        foreach (var entry in entries) total += entry.Weight;
        int roll = random.Next(total);
        foreach (var entry in entries)
        {
            if (roll < entry.Weight) return entry.Type;
            roll -= entry.Weight;
        }
        return entries[^1].Type;
    }

    private static int Seed(int worldSeed, int siteX, int siteY, string id, bool ruined)
    {
        unchecked
        {
            uint hash = (uint)worldSeed;
            hash ^= (uint)siteX * 0x9E3779B9u;
            hash = RotateLeft(hash, 13);
            hash ^= (uint)siteY * 0x85EBCA6Bu;
            hash = RotateLeft(hash, 17);
            for (int i = 0; i < id.Length; i++)
            {
                hash ^= id[i];
                hash *= 0xC2B2AE35u;
                hash = RotateLeft(hash, 11);
            }
            hash ^= ruined ? 0xA511E9B3u : 0x63D83595u;
            hash ^= hash >> 16;
            return (int)(hash & 0x7FFFFFFF);
        }
    }

    private static uint RotateLeft(uint value, int count)
    {
        return value << count | value >> (32 - count);
    }
}