using System;
using System.Net.WebSockets;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SimpleGraph;

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

            var ping = new
            {
                id = "",
                method = "",
            }.ToString();
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}