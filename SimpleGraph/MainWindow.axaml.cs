using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;

namespace SimpleGraph;

public class ViewModel
{

    public ISeries[] Series { get; set; } =
    {
        new LineSeries<double>
        {
            Name = "Bid",
            Values = new double[] { 3, 4, 6, 5 },
            Fill = null,
            Stroke = new SolidColorPaint(SKColors.Blue),
        },
        new LineSeries<double>
        {
            Name = "Ask",
            Values = new double[] { 4, 3, 8, 6 },
            Fill = null,
            Stroke = new SolidColorPaint(SKColors.Red),
        }
    };
    
    public LabelVisual Title { get; set; } = new LabelVisual
    {
        Text = "My chart title",
        TextSize = 25,
        Padding = new LiveChartsCore.Drawing.Padding(15),
        Paint = new SolidColorPaint(SKColors.DarkSlateGray)
    };  
    
    

}
    public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private async void Connect(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
    
        var ticketName = TicketName.Text;
        var ticketCount = TicketCount.Text;
        if (string.IsNullOrWhiteSpace(ticketName) || string.IsNullOrWhiteSpace(ticketCount)) return;
        Console.WriteLine("Try getting tickets...");
        Console.WriteLine($"Ticket: {ticketName}; Count: {ticketCount}");
        
        button.IsEnabled = false;
        button.Content = "Connecting...";
    
        var uri = new Uri("wss://testnet.binance.vision/ws-api/v3");
    
        using var client = new ClientWebSocket();
        try
        {
            await client.ConnectAsync(uri, CancellationToken.None);
            button.Content = "Disconnect";
            Console.WriteLine($"Connected.");
            
            var buffer = new byte[1024];
            while (client.State == WebSocketState.Open)
            {
                var ping = JsonSerializer.Serialize(new
                {
                    id = Guid.NewGuid().ToString(),
                    method = "depth",
                    @params = new {
                        symbol = ticketName,
                        limit = 5,
                    }
                });
                var bytesPing = Encoding.UTF8.GetBytes(ping);
                
                var time = DateTime.Now;
                await client.SendAsync(new ArraySegment<byte>(bytesPing), WebSocketMessageType.Text, true, CancellationToken.None);
                
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Console.WriteLine("Connection closed.");
                    continue;
                }
                else
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var response = JsonSerializer.Deserialize<Response>(receivedMessage);
                    Console.WriteLine();
                    Console.WriteLine($"ReceivedTime: {time}; Bid: {response?.result.bids[0][0]}; Ask: {response?.result.asks[0][0]}");
    
                    System.Threading.Thread.Sleep(10);
                }
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}