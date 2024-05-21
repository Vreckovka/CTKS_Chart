using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using VCore.ItemsCollections;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.ItemsCollections;
using VCore.WPF.Misc;
using VCore.WPF.Prompts;
using VCore.WPF.ViewModels.Prompt;

namespace CTKS_Chart.ViewModels
{
  public class ArchitectViewModel : BaseArchitectViewModel<Position, SimulationStrategy>
  {
    public ArchitectViewModel(IList<CtksLayout> layouts,
      ColorSchemeViewModel colorSchemeViewModel,
      IViewModelsFactory viewModelsFactory,
      BaseTradingBot<Position, SimulationStrategy> tradingBot, Layout layout) : base(layouts, colorSchemeViewModel, viewModelsFactory, tradingBot, layout)
    {
    }
  }

  public class ArchitectPromptViewModel : BasePromptViewModel<ArchitectViewModel>
  {
    public ArchitectPromptViewModel(ArchitectViewModel model) : base(model)
    {
      Title = "Architect";
    }
  }

  public class BaseArchitectViewModel<TPosition, TStrategy> : DrawingViewModel<TPosition, TStrategy>
    where TPosition : Position, new()
    where TStrategy : BaseSimulationStrategy<TPosition>, new()

  {
    private readonly IViewModelsFactory viewModelsFactory;

    private SerialDisposable serialDisposable = new SerialDisposable();

    public BaseArchitectViewModel(IList<CtksLayout> layouts,
      ColorSchemeViewModel colorSchemeViewModel,
      IViewModelsFactory viewModelsFactory, BaseTradingBot<TPosition, TStrategy> tradingBot, Layout layout) : base(tradingBot, layout)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
      Layouts = layouts ?? throw new ArgumentNullException(nameof(layouts));
      ColorScheme = colorSchemeViewModel ?? throw new ArgumentNullException(nameof(colorSchemeViewModel));

      ColorScheme = colorSchemeViewModel;

      SelectedLayout = layouts[5];


      serialDisposable.Disposable = Lines.ItemUpdated.Subscribe(x =>
      {
        VSynchronizationContext.PostOnUIThread(() => RenderOverlay());
      });

      Observable.Timer(TimeSpan.FromSeconds(0.25)).ObserveOnDispatcher().Subscribe(x => OnLayoutChanged());
    }

    #region Properties

    #region Layouts

    private IEnumerable<CtksLayout> layouts;

    public IEnumerable<CtksLayout> Layouts
    {
      get { return layouts; }
      set
      {
        if (value != layouts)
        {
          layouts = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

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

          OnLayoutChanged();
          RaisePropertyChanged();
          RaisePropertyChanged(nameof(Epsilon));
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

    #region SetAllLinesVisibility

    protected ActionCommand<bool> setAllLinesVisibility;

    public ICommand SetAllLinesVisibility
    {
      get
      {
        return setAllLinesVisibility ??= new ActionCommand<bool>(OnSetAllLinesVisibility);
      }
    }


    public void OnSetAllLinesVisibility(bool value)
    {
      Lines.ForEach(x => x.IsVisible = value);
    }

    #endregion

    #region Methods

    public override void Initialize()
    {
      base.Initialize();


    }

    #region OnLayoutChanged

    private void OnLayoutChanged()
    {
      var ctksLines = SelectedLayout.Ctks.ctksLines.ToList();
      var candles = SelectedLayout.Ctks.Candles.ToList();

      Lines.Clear();
      Lines.AddRange(ctksLines.Select(x => viewModelsFactory.Create<CtksLineViewModel>(x)));

      unixDiff = candles[1].UnixTime - candles[0].UnixTime;

      SetMinUnix(candles.First().UnixTime - (unixDiff * 2));
      SetMaxUnix(candles.Last().UnixTime + (unixDiff * 2));
      SetMaxValue(candles.Max(x => x.High.Value));
      SetMinValue(candles.Min(x => x.Low.Value));

      RaisePropertyChanged(nameof(MaxUnix));
      RaisePropertyChanged(nameof(MinUnix));
      RaisePropertyChanged(nameof(MaxValue));
      RaisePropertyChanged(nameof(MinValue));

      RenderOverlay();
    }

    #endregion

    #region RenderOverlay

    public override void RenderOverlay(decimal? athPrice = null, Candle actual = null)
    {
      Pen shapeOutlinePen = new Pen(Brushes.Transparent, 1);
      shapeOutlinePen.Freeze();

      DrawingGroup dGroup = new DrawingGroup();

      var candles = SelectedLayout.Ctks.Candles.ToList();

      WriteableBitmap writeableBmp = BitmapFactory.New((int)CanvasWidth, (int)CanvasHeight);

      using (writeableBmp.GetBitmapContext())
      {
        using (DrawingContext dc = dGroup.Open())
        {
          dc.DrawLine(shapeOutlinePen, new Point(0, 0), new Point(CanvasWidth, CanvasHeight));
          candles = candles.Where(x => x.UnixTime + unixDiff >= MinUnix && x.UnixTime - unixDiff <= MaxUnix).ToList();

          RenderIntersections(dc, SelectedLayout.Ctks.Intersections, SelectedLayout.TimeFrame);

          var drawnChart = DrawChart(writeableBmp, candles, CanvasHeight, CanvasWidth);
          var renderedLines = RenderLines(writeableBmp, CanvasHeight, CanvasWidth);

          DrawnChart = drawnChart;
          Overlay = new DrawingImage(dGroup);
          Chart = writeableBmp;
        }
      }
    }

    #endregion

    #region RenderLines

    public IEnumerable<CtksLine> RenderLines(
      WriteableBitmap drawingContext,
      double canvasHeight,
      double canvasWidth)
    {
      var list = new List<CtksLine>();
      var lastCandle = SelectedLayout.Ctks.Candles.LastOrDefault();
      double actualLeft = 0.0;

      if (lastCandle != null)
        actualLeft = TradingHelper.GetCanvasValueLinear(canvasWidth, lastCandle.UnixTime, MaxUnix, MinUnix);

      foreach (var vm in Lines.Where(x => x.IsVisible))
      {
        var line = vm.Model;

        Color selectedBrush = Colors.Yellow;

        if (vm.IsSelected)
        {
          selectedBrush = Colors.Red;
        }
        //Pen pen = new Pen(selectedBrush, 1);

        var x3 = canvasWidth;
        var ctksLine = CreateLine(canvasHeight, canvasWidth, line);

        if (ctksLine.StartPoint.X > canvasWidth && ctksLine.EndPoint.X > canvasWidth)
        {
          continue;
        }

        var y3 = TradingHelper.GetPointOnLine(ctksLine.StartPoint.X, ctksLine.StartPoint.Y, ctksLine.EndPoint.X, ctksLine.EndPoint.Y, x3);
        Point startPoint = ctksLine.StartPoint;

        if (startPoint.X < 0)
        {
          var newY = TradingHelper.GetPointOnLine(startPoint.X, startPoint.Y, ctksLine.EndPoint.X, ctksLine.EndPoint.Y, 0);
          startPoint = new Point(0, newY);
        }

        if (startPoint.Y > canvasHeight)
        {
          var newX = TradingHelper.GetPointOnLine(startPoint.Y, startPoint.X, ctksLine.EndPoint.Y, ctksLine.EndPoint.X, canvasHeight);
          startPoint = new Point(newX, canvasHeight);
        }
        else if (startPoint.Y < 0)
        {
          var newX = TradingHelper.GetPointOnLine(startPoint.Y, startPoint.X, ctksLine.EndPoint.Y, ctksLine.EndPoint.X, 0);
          startPoint = new Point(newX, 0);
        }

        if (y3 < 0)
        {
          x3 = TradingHelper.GetPointOnLine(startPoint.Y, startPoint.X, ctksLine.EndPoint.Y, ctksLine.EndPoint.X, 0);
          y3 = 0;
        }
        else if (y3 > canvasHeight)
        {
          x3 = TradingHelper.GetPointOnLine(startPoint.Y, startPoint.X, ctksLine.EndPoint.Y, ctksLine.EndPoint.X, canvasHeight);
          y3 = canvasHeight;
        }

        if (x3 < 0 || x3 > canvasWidth)
        {
          continue;
        }

        if (startPoint.Y < 0)
        {
          continue;
        }

        var finalPoint = new Point(x3, y3);

        drawingContext.DrawLine((int)startPoint.X, (int)startPoint.Y, (int)finalPoint.X, (int)finalPoint.Y, selectedBrush);

        var firstPoint = new Point(
            TradingHelper.GetCanvasValueLinear(canvasWidth, line.FirstPoint.UnixTime, MaxUnix, MinUnix),
            TradingHelper.GetCanvasValue(canvasHeight, line.FirstPoint.Price, MaxValue, MinValue));

        var secondPoint = new Point(
          TradingHelper.GetCanvasValueLinear(canvasWidth, line.SecondPoint.UnixTime, MaxUnix, MinUnix),
          TradingHelper.GetCanvasValue(canvasHeight, line.SecondPoint.Price, MaxValue, MinValue));

        var intersectionPoint = TradingHelper.GetPointOnLine(firstPoint.X, firstPoint.Y, secondPoint.X, secondPoint.Y, actualLeft);

        if (intersectionPoint < CanvasHeight && intersectionPoint > 0 && actualLeft < canvasWidth && actualLeft > 0)
        {
          int size = 3;
          drawingContext.DrawEllipse((int)actualLeft - size,
            (int)canvasHeight - (int)intersectionPoint - size,
            (int)actualLeft + size, (int)canvasHeight - (int)intersectionPoint + size,
            Colors.Orange);

          drawingContext.FillEllipse((int)actualLeft - size,
            (int)canvasHeight - (int)intersectionPoint - size,
            (int)actualLeft + size, (int)canvasHeight - (int)intersectionPoint + size,
            Colors.Orange);
        }

        list.Add(ctksLine);
      }

      var actualPen = new Pen(Brushes.White, 1);
      actualPen.DashStyle = new DashStyle(new List<double>() { 15 }, 5);

      if (actualLeft < canvasWidth && actualLeft > 0)
        drawingContext.DrawLine((int)actualLeft, 0, (int)actualLeft, (int)canvasHeight, Colors.White);

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

      var x1 = TradingHelper.GetCanvasValueLinear(canvasWidth, ctksLine.FirstPoint.UnixTime, MaxUnix, MinUnix);
      var x2 = TradingHelper.GetCanvasValueLinear(canvasWidth, ctksLine.SecondPoint.UnixTime, MaxUnix, MinUnix);

      var startPoint = new Point(x1, y1);
      var endPoint = new Point(x2, y2);

      return new CtksLine()
      {
        StartPoint = startPoint,
        EndPoint = endPoint,
        TimeFrame = ctksLine.TimeFrame,
        LineType = ctksLine.LineType
      };
    }

    #endregion 

    #endregion
  }
}