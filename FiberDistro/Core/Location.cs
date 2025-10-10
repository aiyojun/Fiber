using System.Net;

namespace FiberDistro.Core;

public class Location(IPAddress address, int port, int fid)
{
    public static Location Empty = new(IPAddress.Parse("0.0.0.0"), 0, 0);
    
    public readonly IPAddress Address = address;
    
    public readonly int Port = port;
    
    public readonly int Fid = fid;

    public Location(IPEndPoint endpoint, int fid): this(endpoint.Address, endpoint.Port, fid)
    {
        
    }

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(Address.GetAddressBytes());
        writer.Write(Port);
        writer.Write(Fid);
        return stream.ToArray();
    }

    public static Location Deserialize(byte[] data)
    {
        if (data.Length != 12) throw new ArgumentException("invalid location data");
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);
        return new Location(new IPAddress(reader.ReadBytes(4)), reader.ReadInt32(), reader.ReadInt32());
    }

    public static Location Parse(string location)
    {
        var items = location.Split(':');
        return new Location(IPAddress.Parse(items[0]), int.Parse(items[1]), int.Parse(items[2]));
    }

    public bool BelongsTo(IPAddress address)
    {
        return Address.Equals(address);
    }
    
    public bool BelongsTo(IPEndPoint endPoint)
    {
        return Address.Equals(endPoint.Address) && endPoint.Port == Port;
    }
    
    public override string ToString()
    {
        return $"{Address}:{Port}:{Fid}";
    }
    
    public override bool Equals(object? obj)
    {
        return obj is Location location && location.Address.Equals(Address) && location.Port == Port && location.Fid == Fid;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Address, Port, Fid);
    }
}