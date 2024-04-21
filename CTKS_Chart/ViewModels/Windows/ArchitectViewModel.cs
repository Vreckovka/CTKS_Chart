﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VCore.WPF.ItemsCollections;
using VCore.WPF.Misc;
using VCore.WPF.Prompts;

namespace CTKS_Chart.ViewModels
{
  public interface IDrawingViewModel
  {
    public decimal MaxValue { get; set; }
    public decimal MinValue { get; set; }

    public double CanvasHeight { get; set; }
    public double CanvasWidth { get; set; }
    public int CandleCount { get; set; }
  }

  public class CtksLineViewModel : SelectableViewModel<CtksLine>
  {
    public CtksLineViewModel(CtksLine model) : base(model)
    {
    }

    #region IsVisible

    private bool isVisible = true;
    public bool IsVisible
    {
      get { return isVisible; }
      set
      {
        if (value != isVisible)
        {
          isVisible = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }

  public class ArchitectViewModel : BasePromptViewModel, IDrawingViewModel
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly Asset Asset;
    private SerialDisposable serialDisposable = new SerialDisposable();

    public ArchitectViewModel(
      IList<Layout> layouts,
      ColorSchemeViewModel colorSchemeViewModel,
      IViewModelsFactory viewModelsFactory,
      Asset asset)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.Asset = asset ?? throw new ArgumentNullException(nameof(asset));
      Layouts = layouts ?? throw new ArgumentNullException(nameof(layouts));
      ColorScheme = colorSchemeViewModel ?? throw new ArgumentNullException(nameof(colorSchemeViewModel));

      SelectedLayout = layouts[5];

      serialDisposable.Disposable = Lines.ItemUpdated.Subscribe(x =>
      {
        VSynchronizationContext.PostOnUIThread(RenderOverlay);
      });
    }


    public IEnumerable<Layout> Layouts { get; }
    public override string Title { get; set; } = "Architect";
    public Image ChartImage { get; } = new Image();


    #region SelectedLayout

    private Layout selectedLayout;

    public Layout SelectedLayout
    {
      get { return selectedLayout; }
      set
      {
        if (value != selectedLayout)
        {
          selectedLayout = value;

          maxValue = selectedLayout.MaxValue;
          minValue = selectedLayout.MinValue;
          candleCount = selectedLayout.Ctks.Candles.Count;


          OnLayoutChanged();
          RaisePropertyChanged();

        }
      }
    }

    #endregion

    #region Chart

    private DrawingImage chart;

    public DrawingImage Chart
    {
      get { return chart; }
      set
      {
        if (value != chart)
        {
          chart = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MaxValue

    private decimal maxValue;

    public decimal MaxValue
    {
      get { return maxValue; }
      set
      {
        if (value != maxValue && value > minValue)
        {
          maxValue = Math.Round(value, Asset.PriceRound);

          RenderOverlay();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinValue

    private decimal minValue = (decimal)0.001;

    public decimal MinValue
    {
      get { return minValue; }
      set
      {
        if (value != minValue && value < maxValue)
        {

          if (value > 0)
          {
            minValue = value;

            RenderOverlay();
            RaisePropertyChanged();
          }
        }
      }
    }



    #endregion

    #region MaxUnix

    private long maxUnix;

    public long MaxUnix
    {
      get { return maxUnix; }
      set
      {
        if (value != maxUnix)
        {
          maxUnix = value;
          RenderOverlay();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region MinUnix

    private long minUnix;

    public long MinUnix
    {
      get { return minUnix; }
      set
      {
        if (value != minUnix)
        {

          minUnix = value;

          RenderOverlay();
          RaisePropertyChanged();

        }
      }
    }



    #endregion

    public double CanvasHeight { get; set; } = 1000;
    public double CanvasWidth { get; set; } = 1000;

    #region Lines

    private RxObservableCollection<CtksLineViewModel> lines = new RxObservableCollection<CtksLineViewModel>();

    public RxObservableCollection<CtksLineViewModel> Lines
    {
      get { return lines; }
      set
      {
        if (value != lines)
        {
          lines = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualCandles

    private List<Candle> actualCandles = new List<Candle>();

    public List<Candle> ActualCandles
    {
      get { return actualCandles; }
      set
      {
        if (value != actualCandles)
        {
          actualCandles = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ColorScheme

    private ColorSchemeViewModel colorScheme;

    public ColorSchemeViewModel ColorScheme
    {
      get { return colorScheme; }
      set
      {
        if (value != colorScheme)
        {
          colorScheme = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CandleCount

    private int candleCount = 150;

    public int CandleCount
    {
      get { return candleCount; }
      set
      {
        if (value != candleCount)
        {
          candleCount = value;
          RenderOverlay();

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowCanvas

    protected ActionCommand<Layout> showCanvas;

    public ICommand ShowCanvas
    {
      get
      {
        return showCanvas ??= new ActionCommand<Layout>(OnShowCanvas);
      }
    }

    public void OnShowCanvas(Layout layout)
    {
      SelectedLayout = layout;
    }

    #endregion

    #region Methods

    #region DrawChart

    private DrawnChart DrawChart(
      DrawingContext drawingContext,
      IList<Candle> candles,
      double canvasHeight,
      double canvasWidth,
      int maxCount = 150)
    {

      var skip = candles.Count - maxCount > 0 ? candles.Count - maxCount : 0;
      candles = candles.Skip(skip).ToList();

      double minDrawnPoint = 0;
      double maxDrawnPoint = 0;
      var drawnCandles = new List<ChartCandle>();

      
      var width = TradingHelper.GetCanvasValueLinear(canvasWidth, MinUnix + unixDiff, MaxUnix, MinUnix);
      var margin = width * 0.15;
      width = width - margin;

      if (candles.Any())
      {
        int y = -1;
        for (int i = 0; i < candles.Count; i++)
        {
          y++;
          var point = candles[i];

          var close = TradingHelper.GetCanvasValue(canvasHeight, point.Close.Value, MaxValue, MinValue);
          var open = TradingHelper.GetCanvasValue(canvasHeight, point.Open.Value, MaxValue, MinValue);

          var high = TradingHelper.GetCanvasValue(canvasHeight, point.High.Value, MaxValue, MinValue);
          var low = TradingHelper.GetCanvasValue(canvasHeight, point.Low.Value, MaxValue, MinValue);

          var green = i > 0 ? candles[i - 1].Close < point.Close : point.Open < point.Close;

          var selectedBrush = green ? DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.GREEN].Brush) : DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.RED].Brush);

          Pen pen = new Pen(selectedBrush, 3);
          Pen wickPen = new Pen(selectedBrush, 1);

          var newCandle = new Rect()
          {
            Width = width,
          };

          var lastClose = i > 0 ?
            TradingHelper.GetCanvasValue(canvasHeight, candles[i - 1].Close.Value, MaxValue, MinValue) :
            TradingHelper.GetCanvasValue(canvasHeight, candles[i].Open.Value, MaxValue, MinValue);

          if (lastClose < 0)
          {
            lastClose = 0;
          }


          if (close < 0)
          {
            close = 0;
          }
          else

          if (high > CanvasHeight)
          {
            high = CanvasHeight;
          }

          if (green)
          {
            newCandle.Height = close - lastClose;
          }
          else
          {
            newCandle.Height = lastClose - close;
          }

          var x = TradingHelper.GetCanvasValueLinear(canvasWidth, point.UnixTime, MaxUnix, MinUnix);

          newCandle.X = x - (newCandle.Width / 2);

          if(newCandle.X < 0)
          {
            var newWidth = newCandle.Width + newCandle.X;

            if(newWidth > 0)
            {
              newCandle.Width = newWidth;
            }
            else
            {
              newCandle.Width = 0;
            }
       
            newCandle.X = 0;
          }
          else if(newCandle.X + width > canvasWidth)
          {
            var newWidth = canvasWidth - newCandle.X;

            if (newWidth > 0)
            {
              newCandle.Width = newWidth;
            }
            else
            {
              newCandle.Width = 0;
            }
          }

          if (green)
            newCandle.Y = canvasHeight - close;
          else
            newCandle.Y = canvasHeight - close - newCandle.Height;

          if (newCandle.Y < 0)
          {
            var newHeight = newCandle.Y + newCandle.Height;

            if (newHeight <= 0)
            {
              newHeight = 0;
            }

            newCandle.Height = newHeight;
            newCandle.Y = 0;
          }

          var wickTop = green ? close : open;
          var wickBottom = green ? open : close;



          var topY = canvasHeight - wickTop - (high - wickTop);
          var bottomY = canvasHeight - wickBottom;

          Rect? topWick = null;
          Rect? bottomWick = null;

          if(x > 0 && x < canvasWidth)
          {
            if (high - wickTop > 0 && high > 0)
            {
              if (wickTop < 0)
              {
                wickTop = 0;
              }

              topWick = new Rect()
              {
                Height = high - wickTop,
                X = x,
                Y = topY,
              };
            }

            if (wickBottom - low > 0 && wickBottom > 0)
            {
              if (low < 0)
              {
                low = 0;
              }
              var bottomWickHeight = wickBottom - low;

              if (bottomY < 0)
              {
                bottomWickHeight += bottomY;
                bottomY = 0;
              }

              if (bottomWickHeight > 0)
                bottomWick = new Rect()
                {
                  Height = bottomWickHeight,
                  X = x,
                  Y = bottomY,
                };
            }
          }
       


          if (newCandle.Height > 0 && newCandle.Width > 0)
            drawingContext.DrawRectangle(selectedBrush, pen, newCandle);

          if (topWick != null)
            drawingContext.DrawRectangle(selectedBrush, wickPen, topWick.Value);

          if (bottomWick != null)
            drawingContext.DrawRectangle(selectedBrush, wickPen, bottomWick.Value);

          drawnCandles.Add(new ChartCandle()
          {
            Candle = point,
            Body = newCandle,
            TopWick = topWick,
            BottomWick = bottomWick
          });



          if (bottomWick != null && bottomWick.Value.Y > maxDrawnPoint)
          {
            maxDrawnPoint = bottomWick.Value.Y;
          }

          if (topWick != null && topWick.Value.Y < minDrawnPoint)
          {
            minDrawnPoint = topWick.Value.Y;
          }
        }
      }

      return new DrawnChart()
      {
        MaxDrawnPoint = maxDrawnPoint,
        MinDrawnPoint = minDrawnPoint,
        Candles = drawnCandles
      };
    }

    #endregion

    #region OnLayoutChanged

    long unixDiff = 0;
    private void OnLayoutChanged()
    {
      var ctksLines = SelectedLayout.Ctks.ctksLines.ToList();
      var candles = SelectedLayout.Ctks.Candles.ToList();

      Lines.Clear();
      Lines.AddRange(ctksLines.Select(x => viewModelsFactory.Create<CtksLineViewModel>(x)));

      unixDiff = candles[1].UnixTime - candles[0].UnixTime;

      minUnix = candles.First().UnixTime - (unixDiff * 2);
      maxUnix = candles.Last().UnixTime + (unixDiff * 2);

      RenderOverlay();
    }

    #endregion

    #region RenderOverlay

    public void RenderOverlay()
    {
      Pen shapeOutlinePen = new Pen(Brushes.Transparent, 1);
      shapeOutlinePen.Freeze();

      DrawingGroup dGroup = new DrawingGroup();

      var candles = SelectedLayout.Ctks.Candles;


      using (DrawingContext dc = dGroup.Open())
      {
        dc.DrawLine(shapeOutlinePen, new Point(0, 0), new Point(CanvasHeight, CanvasWidth));

        var drawnChart = DrawChart(dc, candles, CanvasHeight, CanvasWidth, CandleCount);
        var renderedLines = RenderLines(dc, drawnChart.Candles.ToList(), CanvasHeight, CanvasWidth);

        List<CtksIntersection> ctksIntersections = SelectedLayout.Ctks.ctksIntersections;

        ctksIntersections = CreateIntersections(renderedLines, candles.Last());

        RenderIntersections(dc, ctksIntersections,
          drawnChart.Candles.ToList(),
          CanvasHeight,
          CanvasHeight,
          CanvasWidth);

        DrawingImage dImageSource = new DrawingImage(dGroup);

        Chart = dImageSource;
        this.ChartImage.Source = Chart;
      }
    }

    #endregion

    #region CreateIntersections

    private List<CtksIntersection> CreateIntersections(IEnumerable<CtksLine> lines, Candle lastCandle)
    {
      List<CtksIntersection> ctksIntersections = new List<CtksIntersection>();

      foreach (var line in lines)
      {
        var actualLeft = TradingHelper.GetCanvasValueLinear(CanvasWidth, lastCandle.UnixTime, MaxUnix, MinUnix); ;
        var actual = TradingHelper.GetPointOnLine(line.StartPoint.X, line.StartPoint.Y, line.EndPoint.X, line.EndPoint.Y, actualLeft);
        var value = Math.Round(TradingHelper.GetValueFromCanvas(CanvasHeight, CanvasHeight - actual, MaxValue, MinValue), Asset.PriceRound);

        if (value > 0)
        {
          var intersection = new CtksIntersection()
          {
            Value = value,
            TimeFrame = line.TimeFrame,
            Line = line
          };

          ctksIntersections.Add(intersection);
        }
      }

      return ctksIntersections;
    }

    #endregion

    #region RenderIntersections

    public void RenderIntersections(
      DrawingContext drawingContext,
      IEnumerable<CtksIntersection> intersections,
      IList<ChartCandle> candles,
      double desiredHeight,
      double canvasHeight,
      double canvasWidth)
    {
      var validIntersection = intersections
        .Where(x => x.Value > MinValue && x.Value < MaxValue)
        .ToList();

      foreach (var intersection in validIntersection)
      {
        Brush selectedBrush = DrawingHelper.GetBrushFromHex(ColorScheme.ColorSettings[ColorPurpose.NO_POSITION].Brush);

        var actual = TradingHelper.GetCanvasValue(canvasHeight, intersection.Value, MaxValue, MinValue);

        var frame = intersection.TimeFrame;

        var lineY = canvasHeight - actual;

        FormattedText formattedText = DrawingHelper.GetFormattedText(intersection.Value.ToString(), selectedBrush);

        drawingContext.DrawText(formattedText, new Point(CanvasWidth * 0.95, lineY - formattedText.Height / 2));

        Pen pen = new Pen(selectedBrush, 1);
        pen.DashStyle = DashStyles.Dash;
        pen.Thickness = DrawingHelper.GetPositionThickness(frame);

        drawingContext.DrawLine(pen, new Point(0, lineY), new Point(canvasWidth * 0.95, lineY));
      }
    }

    #endregion

    #region RenderLines

    public IEnumerable<CtksLine> RenderLines(
      DrawingContext drawingContext,
      IList<ChartCandle> chartCandles,
      double canvasHeight,
      double canvasWidth)
    {
      var list = new List<CtksLine>();

      foreach (var vm in Lines.Where(x => x.IsVisible))
      {
        var line = vm.Model;

        Brush selectedBrush = Brushes.Yellow;

        if (vm.IsSelected)
        {
          selectedBrush = Brushes.Red;
        }

        var x3 = canvasWidth;

        var firstCandle = chartCandles.SingleOrDefault(x => x.Candle.UnixTime == line.FirstPoint.UnixTime);
        var secondCandle = chartCandles.SingleOrDefault(x => x.Candle.UnixTime == line.SecondPoint.UnixTime);

        if (firstCandle == null || secondCandle == null)
        {
          continue;
        }


        var ctksLine = CreateLine(line.FirstIndex, line.SecondIndex, canvasHeight, canvasWidth, line, firstCandle, secondCandle, line.LineType, line.TimeFrame);

        if (ctksLine == null)
          continue;

        var y3 = TradingHelper.GetPointOnLine(ctksLine.StartPoint.X, ctksLine.StartPoint.Y, ctksLine.EndPoint.X, ctksLine.EndPoint.Y, x3);

        while (y3 < 0 && x3 > 0)
        {
          x3 -= 1;
          y3 = TradingHelper.GetPointOnLine(ctksLine.StartPoint.X, ctksLine.StartPoint.Y, ctksLine.EndPoint.X, ctksLine.EndPoint.Y, x3);
        }

        while (y3 > canvasHeight && x3 > 0)
        {
          x3 -= 1;
          y3 = TradingHelper.GetPointOnLine(ctksLine.StartPoint.X, ctksLine.StartPoint.Y, ctksLine.EndPoint.X, ctksLine.EndPoint.Y, x3);
        }

       
        if (line.FirstPoint.Price > MaxValue || line.SecondPoint.Price > MaxValue)
        {
          continue;
        }

        if (line.FirstPoint.Price < MinValue || line.SecondPoint.Price < MinValue)
        {
          continue;
        }

        if (x3 < 0)
          continue;

        Pen pen = new Pen(selectedBrush, 1);

        var finalPoint = new Point(x3, y3);

        drawingContext.DrawLine(pen, ctksLine.StartPoint, ctksLine.EndPoint);
        drawingContext.DrawLine(pen, ctksLine.EndPoint, finalPoint);

        list.Add(ctksLine);
      }

      return list;
    }

    #endregion

    #region CreateLine

    public CtksLine CreateLine(
      int? firstCandleIndex,
      int? secondCandleIndex,
      double canvasHeight,
      double canvasWidth,
      CtksLine ctksLine,
      ChartCandle first,
      ChartCandle second,
      LineType lineType,
      TimeFrame timeFrame)
    {

      var bottom1 = TradingHelper.GetCanvasValue(canvasHeight, ctksLine.FirstPoint.Price, MaxValue, MinValue);
      var bottom2 = TradingHelper.GetCanvasValue(canvasHeight, ctksLine.SecondPoint.Price, MaxValue, MinValue);

      var left1 = first.Body.Left;
      var left2 = second.Body.Left;

      var width1 = first.Body.Width;
      var width2 = second.Body.Width;

      if(width1 == 0 || width2 == 0)
      {
        return null;
      }

      var startPoint = new Point();
      var endPoint = new Point();

      var xShift = 3;
      if (lineType == LineType.RightBottom)
      {
        startPoint = new Point(left1 + width1, canvasHeight - bottom1);
        endPoint = new Point(left2 + width2, canvasHeight - bottom2);
      }
      else if (lineType == LineType.LeftTop)
      {
        startPoint = new Point(left1, canvasHeight - bottom1);
        endPoint = new Point(left2, canvasHeight - bottom2);
      }
      else if (lineType == LineType.RightTop)
      {
        startPoint = new Point(left1 + width1, canvasHeight - bottom1);
        endPoint = new Point(left2 + width2, canvasHeight - bottom2);
      }
      else if (lineType == LineType.LeftBottom)
      {
        startPoint = new Point(left1, canvasHeight - bottom1);
        endPoint = new Point(left2, canvasHeight - bottom2);
      }

     
      

      

      var x = TradingHelper.GetValueFromCanvasLinear(canvasWidth, startPoint.X, MaxUnix, MinUnix);
      var x2 = TradingHelper.GetValueFromCanvasLinear(canvasWidth, endPoint.X, MaxUnix, MinUnix); 

      if (MinUnix < x && x < MaxUnix && MinUnix < x2 && x2 < MaxUnix) 
      {
        return new CtksLine()
        {
          StartPoint = startPoint,
          EndPoint = endPoint,
          TimeFrame = timeFrame,
          FirstIndex = firstCandleIndex,
          SecondIndex = secondCandleIndex,
          LineType = lineType
        };
      }

      return null;
    }

    #endregion 

    #endregion
  }
}