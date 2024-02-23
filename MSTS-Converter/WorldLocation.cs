using OpenTK.Mathematics;

[Serializable]
public struct WorldLocation
{
    public static WorldLocation None = new WorldLocation();

    public int TileX;
    public int TileZ;
    public Vector3 Location;

    public WorldLocation(int tileX, int tileZ, float x, float y, float z)
    {
        TileX = tileX;
        TileZ = tileZ;
        Location = new Vector3(x, y, z);
    }

    public WorldLocation(int tileX, int tileZ, Vector3 location)
    {
        TileX = tileX;
        TileZ = tileZ;
        Location = location;
    }

    public void Normalize()
    {
        while (Location.X >= 1024) { Location.X -= 2048; TileX++; }
        while (Location.X < -1024) { Location.X += 2048; TileX--; }
        while (Location.Z >= 1024) { Location.Z -= 2048; TileZ++; }
        while (Location.Z < -1024) { Location.Z += 2048; TileZ--; }
    }

    public void NormalizeTo(int tileX, int tileZ)
    {
        while (TileX < tileX) { Location.X -= 2048; TileX++; }
        while (TileX > tileX) { Location.X += 2048; TileX--; }
        while (TileZ < tileZ) { Location.Z -= 2048; TileZ++; }
        while (TileZ > tileZ) { Location.Z += 2048; TileZ--; }
    }

    public static bool Within(WorldLocation location1, WorldLocation location2, float distance)
    {
        return GetDistanceSquared(location1, location2) < distance * distance;
    }

    public static float GetDistanceSquared(WorldLocation location1, WorldLocation location2)
    {
        var dx = location1.Location.X - location2.Location.X;
        var dy = location1.Location.Y - location2.Location.Y;
        var dz = location1.Location.Z - location2.Location.Z;
        dx += 2048 * (location1.TileX - location2.TileX);
        dz += 2048 * (location1.TileZ - location2.TileZ);
        return dx * dx + dy * dy + dz * dz;
    }

    public static Vector3 GetDistance(WorldLocation locationFrom, WorldLocation locationTo)
    {
        return new Vector3(locationTo.Location.X - locationFrom.Location.X + (locationTo.TileX - locationFrom.TileX) * 2048, locationTo.Location.Y - locationFrom.Location.Y, locationTo.Location.Z - locationFrom.Location.Z + (locationTo.TileZ - locationFrom.TileZ) * 2048);
    }

    public static Vector2 GetDistance2D(WorldLocation locationFrom, WorldLocation locationTo)
    {
        return new Vector2(locationTo.Location.X - locationFrom.Location.X + (locationTo.TileX - locationFrom.TileX) * 2048, locationTo.Location.Z - locationFrom.Location.Z + (locationTo.TileZ - locationFrom.TileZ) * 2048);
    }

    public override string ToString()
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{{TileX:{0} TileZ:{1} X:{2} Y:{3} Z:{4}}}", TileX, TileZ, Location.X, Location.Y, Location.Z);
    }

    public static bool operator ==(WorldLocation a, WorldLocation b)
    {
        return a.TileX == b.TileX && a.TileZ == b.TileZ && a.Location == b.Location;
    }

    public static bool operator !=(WorldLocation a, WorldLocation b)
    {
        return a.TileX != b.TileX || a.TileZ != b.TileZ || a.Location != b.Location;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;
        var other = (WorldLocation)obj;
        return this == other;
    }

    public override int GetHashCode()
    {
        return TileX.GetHashCode() ^ TileZ.GetHashCode() ^ Location.GetHashCode();
    }
}