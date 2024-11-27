using System;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SimpleGraph;
    public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ViewModel();
    }
    
    private CancellationTokenSource? _cancellationTokenSource;
    private async void Connect(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button button) return;
    
            var ticketName = TicketName.Text;
            var ticketCount = TicketCount.Text;
            if (string.IsNullOrWhiteSpace(ticketName) || string.IsNullOrWhiteSpace(ticketCount)) return;
            Console.WriteLine("Try getting tickets...");
            Console.WriteLine($"Ticket: {ticketName}; Count: {ticketCount}");
        
            ((ViewModel)DataContext!).TitleText = ticketName;
        
            button.Content = "Connecting...";
        
            var uri = new Uri("wss://testnet.binance.vision/ws-api/v3");
    
            using var client = new ClientWebSocket();
            try
            {
                await client.ConnectAsync(uri, CancellationToken.None);
                button.Content = "Disconnect";
                Console.WriteLine($"Connected.");
            
                button.Click -= Connect;
                button.Click += Disconnect;
                
                button.Classes.Add("isConnected");
                
                _cancellationTokenSource = new CancellationTokenSource();
                await GetData(_cancellationTokenSource.Token, client, ticketName, Convert.ToInt32(ticketCount));
            
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        catch (Exception exception)
        {
            throw; // TODO handle exception
        }
    }

    private async Task GetData(CancellationToken cancellationToken, ClientWebSocket client, string ticketName, int ticketCount)
    {
        Chart.IsVisible = true;
        var buffer = new byte[1024];
        while (client.State == WebSocketState.Open)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var depth = JsonSerializer.Serialize(new
            {
                id = Guid.NewGuid().ToString(),
                method = "depth",
                @params = new {
                    symbol = ticketName,
                    limit = 5,
                }
            });
            var bytesDepth = Encoding.UTF8.GetBytes(depth);
        
            await client.SendAsync(new ArraySegment<byte>(bytesDepth), WebSocketMessageType.Text, true, CancellationToken.None);
        
            var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var time = DateTime.Now;

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Console.WriteLine("Connection closed.");
            }
            else
            {
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var response = JsonSerializer.Deserialize<Response>(receivedMessage);
                Console.WriteLine();
                Console.WriteLine($"ReceivedTime: {time}; Bid: {response?.result.bids[0][0]}; Ask: {response?.result.asks[0][0]}");

                double.TryParse(response?.result.bids[0][0], NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var yMax);
                ((ViewModel)DataContext!).YMax = yMax * 1.2;
                double.TryParse(response?.result.asks[0][0], NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var yMin);
                ((ViewModel)DataContext!).YMax = yMin * 0.8;
            
                ((ViewModel)DataContext!).IsReading = true;
                    double.TryParse(response?.result.bids[0][0], NumberStyles.Float, CultureInfo.InvariantCulture, out var bid);
                    Console.WriteLine($"bid: {bid}; origBid: {response?.result.bids[0][0]}");
                    ((ViewModel)DataContext!).AddBid(time, bid);
                    if (((ViewModel)DataContext!).Bids.Count > Convert.ToInt32(ticketCount)) ((ViewModel)DataContext!).Bids.RemoveAt(0);
                
                    double.TryParse(response?.result.asks[0][0], NumberStyles.Float, CultureInfo.InvariantCulture, out var ask);
                    Console.WriteLine($"ask: {ask}; origAsk: {response?.result.asks[0][0]}");
                    ((ViewModel)DataContext!).AddAsk(time, ask);
                    if (((ViewModel)DataContext!).Asks.Count > Convert.ToInt32(ticketCount)) ((ViewModel)DataContext!).Asks.RemoveAt(0);
                
                    ((ViewModel)DataContext!).AxisX.CustomSeparators = ((ViewModel)DataContext!).GetSeparators(Convert.ToInt32(ticketCount));
            
                Thread.Sleep(((ViewModel)DataContext!).Delay);
            }
        }
    }

    private void Disconnect(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        try
        {
            _cancellationTokenSource?.Cancel();

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            button.Content = "Connect";
            button.Click -= Disconnect;
            button.Click += Connect;

            button.Classes.Clear();

            Console.WriteLine("Disconnect");

            ((ViewModel)DataContext!).Bids.Clear();
            ((ViewModel)DataContext!).Asks.Clear();
            ((ViewModel)DataContext!).AxisX.CustomSeparators = ((ViewModel)DataContext!).GetSeparators(0);
            Chart.IsVisible = false;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}