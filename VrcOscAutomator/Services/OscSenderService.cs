using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class OscSenderService : IOscSender
{
    private readonly UdpClient _udp = new();
    private List<IPEndPoint> _endpoints = [];

    public void SetTargets(IEnumerable<OscTarget> targets)
    {
        // 不正なIP・ポートなエントリはスキップする
        _endpoints = targets
            .Where(t => t.IsEnabled && t.Port is >= IPEndPoint.MinPort and <= IPEndPoint.MaxPort)
            .Select(t => IPAddress.TryParse(t.IpAddress?.Trim(), out IPAddress? ip)
                ? new IPEndPoint(ip, t.Port)
                : null)
            .Where(ep => ep is not null)
            .Select(ep => ep!)
            .ToList();
    }

    public void SendFloat(string address, float value)
    {
        if (_endpoints.Count == 0) return;
        byte[] packet = BuildPacket(address, [0x2C, 0x66, 0x00, 0x00], w => BinaryPrimitives.WriteSingleBigEndian(w, value));
        foreach (IPEndPoint ep in _endpoints)
            _udp.Send(packet, packet.Length, ep);
    }

    public void SendInt(string address, int value)
    {
        if (_endpoints.Count == 0) return;
        byte[] packet = BuildPacket(address, [0x2C, 0x69, 0x00, 0x00], w => BinaryPrimitives.WriteInt32BigEndian(w, value));
        foreach (IPEndPoint ep in _endpoints)
            _udp.Send(packet, packet.Length, ep);
    }

    public void SendBool(string address, bool value)
    {
        if (_endpoints.Count == 0) return;
        // Bool: type tag は T(0x54) or F(0x46)、値バイトなし
        byte[] addrBlock = BuildAddrBlock(address);
        byte[] typeBlock = [0x2C, value ? (byte)0x54 : (byte)0x46, 0x00, 0x00];
        byte[] packet = new byte[addrBlock.Length + typeBlock.Length];
        addrBlock.CopyTo(packet, 0);
        typeBlock.CopyTo(packet, addrBlock.Length);
        foreach (IPEndPoint ep in _endpoints)
            _udp.Send(packet, packet.Length, ep);
    }

    public void SendString(string address, string value)
    {
        if (_endpoints.Count == 0) return;
        byte[] addrBlock = BuildAddrBlock(address);
        byte[] typeBlock = [0x2C, 0x73, 0x00, 0x00]; // ,s
        byte[] strBytes = Encoding.UTF8.GetBytes(value);
        int strPaddedLen = PadTo4(strBytes.Length + 1);
        byte[] strBlock = new byte[strPaddedLen];
        strBytes.CopyTo(strBlock, 0);
        byte[] packet = new byte[addrBlock.Length + typeBlock.Length + strBlock.Length];
        int pos = 0;
        addrBlock.CopyTo(packet, pos); pos += addrBlock.Length;
        typeBlock.CopyTo(packet, pos); pos += typeBlock.Length;
        strBlock.CopyTo(packet, pos);
        foreach (IPEndPoint ep in _endpoints)
            _udp.Send(packet, packet.Length, ep);
    }

    private static byte[] BuildAddrBlock(string address)
    {
        byte[] addrBytes = Encoding.UTF8.GetBytes(address);
        byte[] block = new byte[PadTo4(addrBytes.Length + 1)];
        addrBytes.CopyTo(block, 0);
        return block;
    }

    private static byte[] BuildPacket(string address, byte[] typeBlock, Action<Span<byte>> writeValue)
    {
        byte[] addrBlock = BuildAddrBlock(address);

        byte[] valueBlock = new byte[4];
        writeValue(valueBlock);

        byte[] packet = new byte[addrBlock.Length + typeBlock.Length + valueBlock.Length];
        int pos = 0;
        addrBlock.CopyTo(packet, pos); pos += addrBlock.Length;
        typeBlock.CopyTo(packet, pos); pos += typeBlock.Length;
        valueBlock.CopyTo(packet, pos);

        return packet;
    }

    private static int PadTo4(int length) => (length + 3) & ~3;

    public void Dispose()
    {
        _udp.Dispose();
    }
}
