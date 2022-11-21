using System.Net.WebSockets;
using System.Text;

namespace BstServer;

public class WebSocketHandler
{
    private readonly L4D2AppHost _l4D2AppHost;
    private Guid _lastGuid;

    public WebSocketHandler(L4D2AppHost l4D2AppHost)
    {
        _l4D2AppHost = l4D2AppHost;
        _lastGuid = l4D2AppHost.RunningGuid;
        _l4D2AppHost.DataReceived += L4D2AppHostDataReceived;
    }

    public async Task Response()
    {
        var buffer = new byte[1024 * 4];

        string fullData = FullData.ToString();
        byte[] byteArray = _l4D2AppHost.HostSettings.Encoding.GetBytes(fullData);
        await WebSocket.SendAsync(new ArraySegment<byte>(byteArray, 0, byteArray.Length),
            WebSocketMessageType.Text, true, CancellationToken.None);

        WebSocketReceiveResult result =
            await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        while (!result.CloseStatus.HasValue)
        {
            string text = _l4D2AppHost.HostSettings.Encoding.GetString(buffer).Trim('\0');
            if (result.MessageType == WebSocketMessageType.Text) Console.WriteLine(text);

            await WebSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType,
                result.EndOfMessage, CancellationToken.None);

            result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await WebSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    public WebSocket WebSocket { get; set; }
    public StringBuilder FullData { get; set; } = new StringBuilder();

    private async void L4D2AppHostDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
    {
        if (_lastGuid != _l4D2AppHost.RunningGuid)
        {
            _lastGuid = _l4D2AppHost.RunningGuid;
            FullData.Clear();
            FullData = new StringBuilder();
        }

        var data = e.Data;
        FullData.AppendLine(data);
        if (data == null)
            return;
        byte[] bytes = _l4D2AppHost.HostSettings.Encoding.GetBytes(data);
        if (WebSocket == null)
            return;
        try
        {
            await WebSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length),
                WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception)
        {
            // ignored
        }
    }
}