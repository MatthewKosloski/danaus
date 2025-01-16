using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Danaus.Network;

public class RequestService
{
    public static async Task<HttpResponse?> GetResponse(HttpRequest request)
    {
        Chunk? chunk = await GetChunkedResponse(request);

        // Encode the chunked byte data into a UTF8 string.
        var builder = new StringBuilder();
        while (chunk?.Next != null)
        {
            builder.Append(Encoding.UTF8.GetString(chunk.Data));
            chunk = chunk.Next;
        }

        StringReader reader = new(builder.ToString());

        // Parse the status line.
        string? line = await reader.ReadLineAsync();
        var statusPieces = line?.Split(" ");

        // Parse the response headers.
        Dictionary<HttpResponseHeader, string> headers = new();
        do
        {
            line = await reader.ReadLineAsync();
            if (line != null)
            {
                var parts = line.Split(":");
                var header = HttpResponseHeader.GetByName(parts[0]);
                if (header != null)
                {
                    headers.Add(header, parts[1].Trim());
                }
            }
        } while (line != string.Empty);

        // The rest of string contains the HTML document.
        var content = await reader.ReadToEndAsync();

        return new HttpResponse(
            url: request.Url,
            method: request.Method,
            headers: headers,
            httpVersion: statusPieces?[0] ?? string.Empty,
            httpStatusText: statusPieces?[2] ?? string.Empty,
            httpStatusCode: ushort.Parse(statusPieces?[1] ?? string.Empty),
            content: content
        );
    }

    public static async Task<Chunk?> GetChunkedResponse(HttpRequest request)
    {
        Socket? socket = await GetSocket(request);

        if (socket is null)
        {
            return null;
        }

        var requestBytes = request.GetBytes();

        int bytesSent = 0;
        while (bytesSent < requestBytes.Length)
        {
            bytesSent += await socket.SendAsync(requestBytes.AsMemory(bytesSent), SocketFlags.None);
        }

        // Buffer the response into a linked list.
        Chunk? chunk = null;
        int hasBytes = 1;
        int chunkSize = 256;
        while (hasBytes != 0)
        {
            var chunkData = new byte[chunkSize];
            hasBytes = await socket.ReceiveAsync(chunkData, SocketFlags.None);
            chunk = new Chunk(chunkData, chunk, null);
        }

        // Reverse the linked list such that chunk points to the first node.
        while (chunk?.Previous != null)
        {
            Chunk temp = chunk;
            chunk = chunk.Previous;
            chunk.Next = temp;
        }

        return chunk;
    }

    private static async Task<Socket?> GetSocket(HttpRequest request)
    {
        if (request.Url.Host is null)
        {
            throw new ArgumentException("Missing host on request Url.");
        }

        if (request.Url?.SpecialScheme?.Port is null)
        {
            throw new ArgumentException("Request Url must have a special scheme with a port number.");
        }

        IPHostEntry hostInfo = Dns.GetHostEntry(request.Url.Host);
        IPAddress[] addresses = hostInfo.AddressList;

        Socket? socket = null;
        foreach (IPAddress addr in addresses)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endpoint = new IPEndPoint(addr, request.Url.PortOrSpecialSchemePortOr80);

            await socket.ConnectAsync(endpoint);

            if (socket.Connected)
            {
                break;
            }

            socket = null;
        }

        return socket;
    }
}