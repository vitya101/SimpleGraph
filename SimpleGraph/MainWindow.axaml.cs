using System;
using System.Collections.Generic;
using System.ComponentModel;    
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;

namespace SimpleGraph;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ViewModel();
        
    }

    public class ViewModel
    {
        public ISeries[] Series { get; set; }
            = new ISeries[]
            {
                new LineSeries<int>
                {
                    Values = new int[] { 4, 6, 5, 3, -3, -1, 2 }
                },
                new ColumnSeries<double>
                {
                    Values = new double[] { 2, 5, 4, -2, 4, -3, 5 }
                }
            };
    }
    
    private async void Connect(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        var ticketName = TicketName.Text;
        var ticketCount = TicketCount.Text;
        if (string.IsNullOrWhiteSpace(ticketName) || string.IsNullOrWhiteSpace(ticketCount)) return;
        Log.Text += $"Try getting tickets...\nTicket: {ticketName}; Count: {ticketCount}\n";
        
        button.IsEnabled = false;
        button.Content = "Connecting...";

        var uri = new Uri("wss://testnet.binance.vision/ws-api/v3");

        using var client = new ClientWebSocket();
        try
        {
            await client.ConnectAsync(uri, CancellationToken.None);
            button.Content = "Disconnect";
            Log.Text += $"Connected.\n\n";
            
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
                Log.Text += "Sending ping.\n";
                
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    Log.Text += "Connection closed.";
                    continue;
                }
                else
                {
                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var response = JsonSerializer.Deserialize<Response>(receivedMessage);
                    Console.WriteLine(receivedMessage);
                    Log.Text += $"\nReceived\nTime: {time}; Bid: {response?.result.bids[0][0]}; Ask: {response?.result.asks[0][0]}\n";

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