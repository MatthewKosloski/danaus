namespace Danaus.Network;

public class Chunk(byte[] data, Chunk? prev, Chunk? next)
{
    public byte[] Data { get; } = data;
    public Chunk? Previous { get; set; } = prev;
    public Chunk? Next { get; set; } = next;
}