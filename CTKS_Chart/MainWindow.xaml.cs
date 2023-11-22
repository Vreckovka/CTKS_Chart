using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using VCore.Standard.Helpers;
using VCore.WPF.Misc;

namespace CTKS_Chart
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public ObservableCollection<KeyValuePair<DateTime, double>> ForexData { get; set; } = new ObservableCollection<KeyValuePair<DateTime, double>>();
    public ObservableCollection<Ctks> CtksTimeFrames { get; set; } = new ObservableCollection<Ctks>();


    double maxValue = 500;
    double minValue = 45;

    private double canvasHeight = 500;
    private double canvasWidth = 500;

    public MainWindow()
    {
      InitializeComponent();
      DataContext = this;

      ForexChart_Loaded();
    }

    #region ShowLines

    protected ActionCommand<Ctks> showLines;

    public ICommand ShowLines
    {
      get
      {
        return showLines ??= new ActionCommand<Ctks>(OnShowLines);
      }
    }

    protected virtual void OnShowLines(Ctks ctks)
    {
      if (ctks.LinesVisible)
      {
        ctks.ClearRenderedLines();
      }
      else
      {
        ctks.RenderLines();
      }

      ctks.LinesVisible = !ctks.LinesVisible;
    }

    #endregion

    #region ShowIntersections

    protected ActionCommand<Ctks> showIntersections;

    public ICommand ShowIntersections
    {
      get
      {
        return showIntersections ??= new ActionCommand<Ctks>(OnShowIntersections);
      }
    }

    protected virtual void OnShowIntersections(Ctks ctks)
    {
      if (ctks.IntersectionsVisible)
      {
        ctks.ClearRenderedIntersections();
      }
      else
      {
        ctks.RenderIntersections();
      }

      ctks.IntersectionsVisible = !ctks.IntersectionsVisible;
    }

    #endregion

    #region Methods

    private async void ForexChart_Loaded()
    {
      //var tradingView_12m = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart\\BTC-USD.csv";

      var localtion = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart";

      LoadBitcoin(localtion);

      var tradingView__ada_2W = $"{localtion}\\BINANCE ADAUSD, 2W.csv";
      var tradingView__ada_1D = $"{localtion}\\BINANCE ADAUSD, 1D.csv";
      var tradingView__ada_240 = $"{localtion}\\BINANCE ADAUSD, 240.csv";
      var tradingView__ada_120 = $"{localtion}\\BINANCE ADAUSDT, 120.csv";

      var tradingView__eth_1W = $"{localtion}\\BITSTAMP ETHUSD, 1W.csv";

      var tradingView_spy_12 = $"{localtion}\\BATS SPY, 12M.csv";
      var tradingView_spy_6 = $"{localtion}\\BATS SPY, 6M.csv";

      var tradingView_ltc_240 = $"{localtion}\\BINANCE LTCUSD.P, 240.csv";


      var spy6 = ParseTradingView(tradingView_spy_6);
      var spy12 = ParseTradingView(tradingView_spy_12);

      int skip = 0;
      //ChartPoints = new ObservableCollection<Candle>(all.Take(skip));


      var canvas1 = new Canvas();
      var canvas2 = new Canvas();
      var canvas3 = new Canvas();


      canvas_grid.Children.Add(canvas1);
      canvas_grid.Children.Add(canvas2);
      canvas_grid.Children.Add(canvas3);

      //for (int i = skip; i < all.Count; i++)
      //{
      //  ClearCanvas();
      //  ChartPoints.Add(all[i]);
      //  CreateChart();


      //  await Task.Delay(1000);
      //}

      canvas1.Loaded += (x, y) =>
      {
        var canvas = x as Canvas;

        var candles = spy12;
        Canvas_Loaded(canvas, candles); 

        var ctks = new Ctks(canvas, GetValueFromCanvas, TimeFrame.M12);
        ctks.CreateLines(candles);
        ctks.AddIntersections();

        ctks.RenderIntersections();
        CtksTimeFrames.Add(ctks);
      };
      canvas2.Loaded += (x, y) => {
        var canvas = x as Canvas;
       

        var candles = spy6;
        Canvas_Loaded(canvas, candles);

        var ctks = new Ctks(canvas, GetValueFromCanvas, TimeFrame.M6);
        ctks.CreateLines(candles);
        ctks.AddIntersections();

        ctks.RenderIntersections();
        CtksTimeFrames.Add(ctks);
      };

      canvas3.Loaded += (x, y) =>
      {
        var canvas = x as Canvas;

        Canvas_Loaded(canvas, spy6);

        var ctks = new Ctks(canvas, GetValueFromCanvas, TimeFrame.M6);

        ctks.RenderIntersections(intersections: CtksTimeFrames[0].ctksIntersections, timeFrame: TimeFrame.M12);
        ctks.RenderIntersections(intersections: CtksTimeFrames[1].ctksIntersections, timeFrame: TimeFrame.M6);
      };
    }

    private void Canvas_Loaded(Canvas sender, IList<Candle> candles)
    {
      CreateChart(sender, candles);
    }

    private List<Candle> ParseTradingView(string path)
    {
      var list = new List<Candle>();

      var file = File.ReadAllText(path);

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


        list.Add(new Candle()
        {
          Close = closeParsed,
          Open = openParsed,
          High = highParsed,
          Low = lowParsed
        });
      }

      return list;
    }

    #region LoadBitcoin

    private void LoadBitcoin(string location)
    {
      var tradingView_btc_12m = $"{location}\\INDEX BTCUSD, 12M.csv";
      var tradingView_btc_6m = $"{location}\\INDEX BTCUSD, 6M.csv";
      var tradingView_btc_3m = $"{location}\\INDEX BTCUSD, 3M.csv";
      var tradingView_btc_1m = $"{location}\\INDEX BTCUSD, 1M.csv";

      var tradingView_btc_2W = $"{location}\\INDEX BTCUSD, 2W.csv";
      var tradingView_btc_1W = $"{location}\\INDEX BTCUSD, 1W.csv";

      var tradingView_btc_720m = $"{location}\\INDEX BTCUSD, 720.csv";
    }

    #endregion

    #region GetCanvasValue

    private double GetCanvasValue(Canvas canvas, double value)
    {
      var canvasHeight = canvas.ActualHeight * 0.75;

      var logValue = Math.Log10(value);
      var logMaxValue = Math.Log10(maxValue);
      var logMinValue = Math.Log10(minValue);

      var logRange = logMaxValue - logMinValue;
      double diffrence = logValue - logMinValue;

      return diffrence * canvasHeight / logRange;
    }

    #endregion

    #region CreateChart

    private void CreateChart(Canvas canvas, IList<Candle> candles)
    {
      var canvasWidth = canvas.ActualWidth * 0.85;

      if (candles.Any())
      {
        for (int i = 0; i < candles.Count; i++)
        {
          var point = candles[i];
          var valueForCanvas = GetCanvasValue(canvas,point.Close);
          var width = canvasWidth / candles.Count;

          var green = i > 0 ? candles[i - 1].Close < point.Close : point.Open < point.Close;

          var lastCandle = new Rectangle()
          {
            Width = width,
            Height = 25,
            Fill = green ? Brushes.Green : Brushes.Red,
          };

          Panel.SetZIndex(lastCandle, 99);


          var open = i > 0 ? GetCanvasValue(canvas, candles[i - 1].Close) : GetCanvasValue(canvas, candles[i].Open);

          if (green)
          {
            lastCandle.Height = valueForCanvas - open;
          }
          else
          {
            lastCandle.Height = open - valueForCanvas;
          }

          canvas.Children.Add(lastCandle);

          if (green)
            Canvas.SetBottom(lastCandle, open);
          else
            Canvas.SetBottom(lastCandle, open - lastCandle.Height);

          Canvas.SetLeft(lastCandle, ((i + 1) * width) + 2);

        }
      }
    }

    #endregion

    private double GetValueFromCanvas(Canvas canvas, double value)
    {
      var canvasHeight = canvas.ActualHeight * 0.75;

      var logMaxValue = Math.Log10(maxValue);
      var logMinValue = Math.Log10(minValue);

      var logRange = logMaxValue - logMinValue;

      return Math.Pow(10, (value * logRange / canvasHeight) + logMinValue);
    }

    private void main_canvas_Loaded(object sender, RoutedEventArgs e)
    {
     
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
      //var mosue = Mouse.GetPosition(main_canvas);

      //Canvas.SetLeft(coordinates, mosue.X + 20);
      //Canvas.SetTop(coordinates, mosue.Y - 10);

      //var canvasValue = mosue.Y;

      //coordinates.Text = $"{GetValueFromCanvas(main_canvas.ActualHeight - canvasValue).ToString("N2")}";
    }

    private void main_canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      //if (e.WidthChanged)
      //{
      //  ClearCanvas();
      //  CreateChart();
      //}
    }

    private void ClearCanvas()
    {
      //main_canvas.Children.Clear();
      //main_canvas.Children.Add(coordinates);
      //ctks.ctksLines.Clear();
    }
  } 
  #endregion
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

