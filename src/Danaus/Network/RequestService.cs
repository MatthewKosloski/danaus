using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Danaus.Network;

public class RequestService
{
    public static async Task<HttpResponse> GetResponse(HttpRequest request)
    {
        // The caller is responsible for disposing this resource.
        MemoryStream memoryStream = await GetResponseAsMemoryStream(request);
        StreamReader streamReader = new(memoryStream, Encoding.UTF8);

        // Parse the status line.
        string? line = await streamReader.ReadLineAsync();
        var statusPieces = line?.Split(" ");

        // Parse the response headers.
        Dictionary<HttpResponseHeader, string> headers = new();
        do
        {
            line = await streamReader.ReadLineAsync();
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

        return new HttpResponse(
            url: request.Url,
            method: request.Method,
            headers: headers,
            httpVersion: statusPieces?[0] ?? string.Empty,
            httpStatusText: statusPieces?[2] ?? string.Empty,
            httpStatusCode: ushort.Parse(statusPieces?[1] ?? string.Empty),
            content: streamReader
        );
    }

    private static async Task<MemoryStream> GetResponseAsMemoryStream(HttpRequest request)
    {
        MemoryStream memoryStream = new();
    
        Socket? socket = await GetSocket(request);

        if (socket is null)
        {
            return memoryStream;
        }

        var requestBytes = request.GetBytes();

        int bytesSent = 0;
        while (bytesSent < requestBytes.Length)
        {
            bytesSent += await socket.SendAsync(requestBytes.AsMemory(bytesSent), SocketFlags.None);
        }

        int hasBytes = 1;
        int chunkSize = 256;
        while (hasBytes != 0)
        {
            var chunkData = new byte[chunkSize];
            hasBytes = await socket.ReceiveAsync(chunkData, SocketFlags.None);
            await memoryStream.WriteAsync(chunkData);
        }

        memoryStream.Position = 0;

        return memoryStream;
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