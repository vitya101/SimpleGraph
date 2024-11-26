using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;

namespace SimpleGraph;

public class ViewModel : INotifyPropertyChanged
{
    public int Delay { get; set; } = 100;
    public object Sync { get; } = new();
    public bool IsReading { get; set; }
    public Axis[] XAxes { get; set; }
    public readonly DateTimeAxis AxisX;
    public Axis[] YAxes { get; set; }
    public readonly Axis AxisY;

    
    private double _yMax = 0;
    public double YMax
    {
        get => _yMax;
        set
        {
            if (value == _yMax) return;
            _yMax = value;
            OnPropertyChanged(nameof(YMax));
        }
    }

    private double _yMin = 0;
    public double YMin
    {
        get => _yMin;
        set
        {
            if (value == _yMin) return;
            _yMin = value;
            OnPropertyChanged(nameof(YMin));
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    public LabelVisual Title { get; set; }
    private string _titleText = "";

    public string TitleText 
    { 
        get => _titleText; 
        set 
        {
            _titleText = value; 
            OnPropertyChanged(nameof(TitleText));
            Title.Text = TitleText;

        }
    }

    public List<DateTimePoint> Bids { get; } = [];
    public void AddBid(DateTime time, double value)
    {
        Bids.Add(new DateTimePoint(time, value));
        OnPropertyChanged(nameof(Bids));
    }
    
    public List<DateTimePoint> Asks { get; } = [];
    public void AddAsk(DateTime time, double value)
    {
        Asks.Add(new DateTimePoint(time, value));
        OnPropertyChanged(nameof(Asks));
    }
    
    public ObservableCollection<ISeries> Series { get; set; }
    public ViewModel()
    {
        Title = new LabelVisual {
            Text = TitleText,
        };
        
        Series = [
            new LineSeries<DateTimePoint>
            {
                Name = "Bids",
                Values = Bids,
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(SKColors.Green)
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Asks",
                Values = Asks,
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(SKColors.Red)
            }
        ];
        
        AxisX = new DateTimeAxis(TimeSpan.FromSeconds(1), Formatter)
        {
            CustomSeparators = GetSeparators(0),
            AnimationsSpeed = TimeSpan.FromMilliseconds(0),
            SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(70)),
        };
        XAxes = [AxisX];

        AxisY = new Axis()
        {
            MinLimit = YMin,
            MaxLimit = YMax,
        };
        YAxes = [AxisY];
        
        _ = ReadData();
    }

    private async Task ReadData()
    {
        while (IsReading)
        {
            await Task.Delay(Delay);
        }
    }

    public double[] GetSeparators(int ticketCount)
    {
        
        if (ticketCount == 0) return [];
        
        var now = DateTime.Now;
        var n = ticketCount / 10;
        
        return
        [
            now.AddSeconds(-(n*3)).Ticks,
            now.AddSeconds(-(n*2)).Ticks,
            now.AddSeconds(-(n)).Ticks,
            now.Ticks
        ];
    }

    private static string Formatter(DateTime date)
    {
        var secsAgo = (DateTime.Now - date).TotalSeconds;

        return secsAgo < 1
            ? "now"
            : $"{secsAgo:N0}s ago";
    }
}