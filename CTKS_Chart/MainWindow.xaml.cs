using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace CTKS_Chart
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public ObservableCollection<Candle> ChartPoints { get; set; } = new ObservableCollection<Candle>();

    public ObservableCollection<KeyValuePair<DateTime, double>> ForexData { get; set; } = new ObservableCollection<KeyValuePair<DateTime, double>>();

    public double CanvasHeight
    {
      get
      {
        return main_canvas.ActualHeight * 0.75;
      }
    }

    private Ctks ctks;
    public MainWindow()
    {
      InitializeComponent();
      DataContext = this;

      ctks = new Ctks(main_canvas);

    }

    double maxValue = 600;
    double minValue = 25;

    private async void ForexChart_Loaded()
    {
      //var tradingView_12m = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart\\BTC-USD.csv";

      var localtion = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart";
      var tradingView_12m = $"{localtion}\\INDEX BTCUSD, 12M.csv";
      var tradingView_spy_12 = $"{localtion}\\BATS SPY, 12M.csv";

      var file = File.ReadAllText(tradingView_spy_12);

      var lines = file.Split("\n");
      CultureInfo.CurrentCulture = new CultureInfo("en-US");

      foreach (var line in lines.Skip(1))
      {
        var data = line.Split(",");

        double.TryParse(data[0], out var timeParsed);
        double.TryParse(data[1], out var openParsed);
        double.TryParse(data[2], out var highParsed);
        double.TryParse(data[3], out var lowParsed);
        double.TryParse(data[4], out var closeParsed);


        ChartPoints.Add(new Candle()
        {
          Close = closeParsed,
          Open = openParsed,
          High = highParsed,
          Low = lowParsed
        });
      }

      ChartPoints = new ObservableCollection<Candle>(ChartPoints.Take(ChartPoints.Count - 0));

      CreateChart();
    }

    private double GetCanvasValue(double value)
    {
      var logValue = Math.Log10(value);
      var logMaxValue = Math.Log10(maxValue);
      var logMinValue = Math.Log10(minValue);

      var logRange = logMaxValue - logMinValue;
      double diffrence = logValue - logMinValue;

      return diffrence * CanvasHeight / logRange;
    }

    private void CreateChart()
    {
      var canvasWidth = main_canvas.ActualWidth * 0.75;

      for (int i = 0; i < ChartPoints.Count; i++)
      {
        var point = ChartPoints[i];
        var valueForCanvas = GetCanvasValue(point.Close);
        var width = canvasWidth / ChartPoints.Count;


        var green = i > 0 ? ChartPoints[i - 1].Close < point.Close : point.Open < point.Close;

        Rectangle rec = new Rectangle()
        {
          Width = width,
          Height = 25,
          Fill = green ? Brushes.Green : Brushes.Red,
        };

        Panel.SetZIndex(rec, 99);


        var open = i > 0 ? GetCanvasValue(ChartPoints[i - 1].Close) : GetCanvasValue(ChartPoints[i].Open);

        if (green)
        {
          rec.Height = valueForCanvas - open;
        }
        else
        {
          rec.Height = open - valueForCanvas;
        }

        if (rec.Height < 0.5)
        {
          rec.Height = 0.5;
        }

        main_canvas.Children.Add(rec);

        if (green)
          Canvas.SetBottom(rec, open);
        else
          Canvas.SetBottom(rec, open - rec.Height);

        Canvas.SetLeft(rec, ((i + 1) * width) + 2);

      }

      var candles = main_canvas.Children.OfType<Rectangle>().ToList();

      for (int i = 0; i < ChartPoints.Count - 2; i++)
      {
        var currentCandle = ChartPoints[i];
        var nextCandle = ChartPoints[i + 1];

        //await Task.Delay(1000);

        if (currentCandle.IsGreen)
        {
          if (nextCandle.IsGreen)
            ctks.CreateLine(i, i + 1, LineType.LeftTop, GetValueFromCanvas);

          if (currentCandle.Close < nextCandle.Close || (currentCandle.Open < nextCandle.Close))
            ctks.CreateLine(i, i + 1, LineType.RightBttom, GetValueFromCanvas);
        }
      }
    }

    private double GetValueFromCanvas(double value)
    {
      var logMaxValue = Math.Log10(maxValue);
      var logMinValue = Math.Log10(minValue);

      var logRange = logMaxValue - logMinValue;

      return Math.Pow(10, (value * logRange / CanvasHeight) + logMinValue);
    }

    private void main_canvas_Loaded(object sender, RoutedEventArgs e)
    {
      ForexChart_Loaded();
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
      var mosue = Mouse.GetPosition(main_canvas);

      Canvas.SetLeft(coordinates, mosue.X + 20);
      Canvas.SetTop(coordinates, mosue.Y - 10);

      var canvasValue = mosue.Y;

      coordinates.Text = $"{GetValueFromCanvas(main_canvas.ActualHeight - canvasValue).ToString("N2")}";}

    private void main_canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {

      if (e.WidthChanged)
      {
        main_canvas.Children.Clear();
        main_canvas.Children.Add(coordinates);

        CreateChart();
      }
     
    }
  }
  }

  public class Candle
  {
    public double Close { get; set; }
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }

    public bool IsGreen
    {
      get { return Close > Open; }
    }
  }

