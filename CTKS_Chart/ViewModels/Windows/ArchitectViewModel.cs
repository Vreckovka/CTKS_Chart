using System;
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
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VCore.WPF.ItemsCollections;
using VCore.WPF.Misc;
using VCore.WPF.Prompts;

namespace CTKS_Chart.ViewModels
{
  public class ArchitectViewModel : BasePromptViewModel, IDrawingViewModel
  {
    private readonly IViewModelsFactory viewModelsFactory;
    private readonly Asset Asset;
    private SerialDisposable serialDisposable = new SerialDisposable();
    long unixDiff = 0;


    public ArchitectViewModel(
      IList<CtksLayout> layouts,
      ColorSchemeViewModel colorSchemeViewModel,
      IViewModelsFactory viewModelsFactory,
      Asset asset)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      this.Asset = asset ?? throw new ArgumentNullException(nameof(asset));
      Layouts = layouts ?? throw new ArgumentNullException(nameof(layouts));
      ColorScheme = colorSchemeViewModel ?? throw new ArgumentNullException(nameof(colorSchemeViewModel));

      DrawingViewModel = new DrawingViewModel(
        new TradingBot(
        new Asset()
        {
          NativeRound = asset.NativeRound,
          PriceRound = asset.PriceRound
        }, null), new CtksLayout());

      DrawingViewModel.Initialize();
      DrawingViewModel.ColorScheme = colorSchemeViewModel;

      SelectedLayout = layouts[5];

      DrawingViewModel.chartDiff = 0;

      serialDisposable.Disposable = Lines.ItemUpdated.Subscribe(x =>
      {
        VSynchronizationContext.PostOnUIThread(RenderOverlay);
      });

      DrawingViewModel.DrawingSettings.RenderLayout = () => RenderOverlay();
    }


    public IEnumerable<CtksLayout> Layouts { get; }
    public override string Title { get; set; } = "Architect";
    public Image ChartImage { get; } = new Image();

    public DrawingViewModel DrawingViewModel { get; }

    #region SelectedLayout

    private CtksLayout selectedLayout;

    public CtksLayout SelectedLayout
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

    #region Epsilon

    public decimal Epsilon
    {
      get { return SelectedLayout.Ctks.Epsilon; }
      set
      {
        if (value != SelectedLayout.Ctks.Epsilon)
        {
          SelectedLayout.Ctks.Epsilon = value;
         SelectedLayout.Ctks.AddIntersections();

          RenderOverlay();
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

    protected ActionCommand<CtksLayout> showCanvas;

    public ICommand ShowCanvas
    {
      get
      {
        return showCanvas ??= new ActionCommand<CtksLayout>(OnShowCanvas);
      }
    }

    public void OnShowCanvas(CtksLayout layout)
    {
      SelectedLayout = layout;
    }

    #endregion

    #region Methods

    #region OnLayoutChanged


    private void OnLayoutChanged()
    {
      var ctksLines = SelectedLayout.Ctks.ctksLines.ToList();
      var candles = SelectedLayout.Ctks.Candles.ToList();

      Lines.Clear();
      Lines.AddRange(ctksLines.Select(x => viewModelsFactory.Create<CtksLineViewModel>(x)));

      unixDiff = candles[1].UnixTime - candles[0].UnixTime;

      minUnix = candles.First().UnixTime - (unixDiff * 2);
      maxUnix = candles.Last().UnixTime + (unixDiff * 2);

      RaisePropertyChanged(nameof(MaxUnix));
      RaisePropertyChanged(nameof(MinUnix));

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

      DrawingViewModel.MaxValue = MaxValue;
      DrawingViewModel.MinValue = MinValue;
      DrawingViewModel.MaxUnix = MaxUnix;
      DrawingViewModel.MinUnix = MinUnix;
      DrawingViewModel.unixDiff = unixDiff;

      using (DrawingContext dc = dGroup.Open())
      {
        dc.DrawLine(shapeOutlinePen, new Point(0, 0), new Point(CanvasHeight, CanvasWidth));
        candles = candles.Where(x => x.UnixTime + unixDiff >= MinUnix && x.UnixTime - unixDiff <= MaxUnix).ToList();

        var drawnChart = DrawingViewModel.DrawChart(dc, candles, CanvasHeight, CanvasWidth);
        var renderedLines = RenderLines(dc, CanvasHeight, CanvasWidth);

        if (DrawingViewModel.DrawingSettings.ShowIntersections)
        {
          DrawingViewModel.DrawIntersections(
                dc,
                SelectedLayout.Ctks.Intersections,
                CanvasHeight,
                CanvasWidth
           );
        }

        if (DrawingViewModel.DrawingSettings.ShowClusters)
        {
          DrawingViewModel.DrawClusters(
               dc,
               SelectedLayout.Ctks.Intersections,
               CanvasHeight,
               CanvasWidth
          );
        }

        DrawingImage dImageSource = new DrawingImage(dGroup);

        Chart = dImageSource;
        this.ChartImage.Source = Chart;
      }
    }

    #endregion

    #region RenderLines

    public IEnumerable<CtksLine> RenderLines(
      DrawingContext drawingContext,
      double canvasHeight,
      double canvasWidth)
    {
      var list = new List<CtksLine>();
      var lastCandle = SelectedLayout.Ctks.Candles.LastOrDefault();
      double actualLeft = 0.0;

      if (lastCandle != null)
        actualLeft = TradingHelper.GetCanvasValueLinear(canvasWidth, lastCandle.UnixTime, maxUnix, minUnix);

      foreach (var vm in Lines.Where(x => x.IsVisible))
      {
        var line = vm.Model;

        Brush selectedBrush = Brushes.Yellow;

        if (vm.IsSelected)
        {
          selectedBrush = Brushes.Red;
        }

        var x3 = canvasWidth;

        var ctksLine = CreateLine(canvasHeight, canvasWidth, line);

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


        if (x3 < 0)
          continue;

        Pen pen = new Pen(selectedBrush, 1);

        var finalPoint = new Point(x3, y3);

        drawingContext.DrawLine(pen, ctksLine.StartPoint, ctksLine.EndPoint);
        drawingContext.DrawLine(pen, ctksLine.EndPoint, finalPoint);



        var firstPoint = new Point(
            TradingHelper.GetCanvasValueLinear(canvasWidth, line.FirstPoint.UnixTime, maxUnix, minUnix),
            TradingHelper.GetCanvasValue(canvasHeight, line.FirstPoint.Price, maxValue, minValue));

        var secondPoint = new Point(
          TradingHelper.GetCanvasValueLinear(canvasWidth, line.SecondPoint.UnixTime, maxUnix, minUnix),
          TradingHelper.GetCanvasValue(canvasHeight, line.SecondPoint.Price, maxValue, minValue));

        var intersectionPoint = TradingHelper.GetPointOnLine(firstPoint.X, firstPoint.Y, secondPoint.X, secondPoint.Y, actualLeft);

        if (intersectionPoint < CanvasHeight && intersectionPoint > 0 && actualLeft < canvasWidth && actualLeft > 0)
        {
          drawingContext.DrawEllipse(Brushes.Red, pen, new Point(actualLeft, canvasHeight - intersectionPoint), 2, 2);
        }

        list.Add(ctksLine);
      }

      var actualPen = new Pen(Brushes.White, 1);
      actualPen.DashStyle = new DashStyle(new List<double>() { 15 }, 5);

      if (actualLeft < canvasWidth && actualLeft > 0)
        drawingContext.DrawLine(actualPen, new Point(actualLeft, 0), new Point(actualLeft, canvasHeight));

      return list;
    }

    #endregion

    #region CreateLine

    public CtksLine CreateLine(
      double canvasHeight,
      double canvasWidth,
      CtksLine ctksLine)
    {
      var y1 = canvasHeight - TradingHelper.GetCanvasValue(canvasHeight, ctksLine.FirstPoint.Price, MaxValue, MinValue);
      var y2 = canvasHeight - TradingHelper.GetCanvasValue(canvasHeight, ctksLine.SecondPoint.Price, MaxValue, MinValue);

      var x1 = TradingHelper.GetCanvasValueLinear(canvasWidth, ctksLine.FirstPoint.UnixTime, maxUnix, minUnix);
      var x2 = TradingHelper.GetCanvasValueLinear(canvasWidth, ctksLine.SecondPoint.UnixTime, maxUnix, minUnix);

      var startPoint = new Point(x1, y1);
      var endPoint = new Point(x2, y2);

      if (MinUnix < ctksLine.FirstPoint.UnixTime &&
          ctksLine.FirstPoint.UnixTime < MaxUnix &&
          MinUnix < ctksLine.SecondPoint.UnixTime &&
          ctksLine.SecondPoint.UnixTime < MaxUnix)
      {
        return new CtksLine()
        {
          StartPoint = startPoint,
          EndPoint = endPoint,
          TimeFrame = ctksLine.TimeFrame,
          LineType = ctksLine.LineType
        };
      }

      return null;
    }

    #endregion 

    #endregion
  }
}