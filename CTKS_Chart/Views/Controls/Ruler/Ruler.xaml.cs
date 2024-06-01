using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using VCore.WPF.Controls;
using DecimalMath;
using VCore.ItemsCollections;
using System.Linq;
using System.Windows.Media;

namespace CTKS_Chart.Views.Controls
{

  public class RenderedLabel
  {
    public int? Order { get; set; }
    public TextBlock TextBlock { get; set; }
    public Border Border { get; set; }
    public decimal Price { get; set; }
    public Point Position { get; set; }

    public CtksIntersection Intersection { get; set; }
    public DrawingRenderedLabel Label { get; set; }
  }
  public enum RulerMode
  {
    Vertical,
    Horizontal
  }

  /// <summary>
  /// Interaction logic for Ruler.xaml
  /// </summary>
  public abstract partial class Ruler : UserControl, INotifyPropertyChanged
  {
    public abstract RulerMode Mode { get; }

    protected List<RenderedLabel> Values { get; } = new List<RenderedLabel>();
    protected List<RenderedLabel> Labels { get; } = new List<RenderedLabel>();

    #region DrawingViewModel

    public IDrawingViewModel DrawingViewModel
    {
      get { return (IDrawingViewModel)GetValue(DrawingViewModelProperty); }
      set { SetValue(DrawingViewModelProperty, value); }
    }

    public static readonly DependencyProperty DrawingViewModelProperty =
      DependencyProperty.Register(
        nameof(DrawingViewModel),
        typeof(IDrawingViewModel),
        typeof(Ruler), new PropertyMetadata(new StrategyDrawingViewModel<Position, SimulationStrategy>(null, null)));

    #endregion

    #region ValuesToRender

    public RxObservableCollection<RenderedIntesection> ValuesToRender
    {
      get { return (RxObservableCollection<RenderedIntesection>)GetValue(ValuesToRenderProperty); }
      set { SetValue(ValuesToRenderProperty, value); }
    }

    public static readonly DependencyProperty ValuesToRenderProperty =
      DependencyProperty.Register(
        nameof(ValuesToRender),
        typeof(RxObservableCollection<RenderedIntesection>),
        typeof(Ruler), new PropertyMetadata(new RxObservableCollection<RenderedIntesection>(), (x, y) =>
        {

          if (x is Ruler ruler && y.NewValue is RxObservableCollection<RenderedIntesection> collection)
          {
            ruler.RenderValues();
            ruler.ValuesToRender.CollectionChanged += ruler.ValuesToRender_CollectionChanged;
            collection.ItemUpdated.Subscribe((x) => { ruler.RenderValues(); });
          }
        }));


    #endregion

    #region LabelsToRender

    public RxObservableCollection<DrawingRenderedLabel> LabelsToRender
    {
      get { return (RxObservableCollection<DrawingRenderedLabel>)GetValue(LabelsToRenderProperty); }
      set { SetValue(LabelsToRenderProperty, value); }
    }

    public static readonly DependencyProperty LabelsToRenderProperty =
      DependencyProperty.Register(
        nameof(LabelsToRender),
        typeof(ObservableCollection<DrawingRenderedLabel>),
        typeof(Ruler), new PropertyMetadata(new RxObservableCollection<DrawingRenderedLabel>(), (x, y) =>
        {

          if (x is Ruler ruler && y.NewValue is RxObservableCollection<DrawingRenderedLabel> collection)
          {
            ruler.RenderLabels();
            ruler.LabelsToRender.CollectionChanged += ruler.LablesToRender_CollectionChanged;
            collection.ItemUpdated.Subscribe((x) => { ruler.RenderLabels(); });
          }
        }));

    #endregion

    #region ChartContent

    public ImageSource ChartContent
    {
      get { return (ImageSource)GetValue(ChartContentProperty); }
      set { SetValue(ChartContentProperty, value); }
    }

    public static readonly DependencyProperty ChartContentProperty =
      DependencyProperty.Register(
        nameof(ChartContent),
        typeof(ImageSource),
        typeof(Ruler), new PropertyMetadata(null, (obj, y) =>
        {
          if (obj is Ruler ruler)
          {
            ruler.RenderValues();
          }
        }));



    #endregion

    #region AssetPriceRound

    public int AssetPriceRound
    {
      get { return (int)GetValue(AssetPriceRoundProperty); }
      set { SetValue(AssetPriceRoundProperty, value); }
    }

    public static readonly DependencyProperty AssetPriceRoundProperty =
      DependencyProperty.Register(
        nameof(AssetPriceRound),
        typeof(int),
        typeof(Ruler), new PropertyMetadata(5));


    #endregion

    #region ResetChart

    public ICommand ResetChart
    {
      get { return (ICommand)GetValue(ResetChartProperty); }
      set { SetValue(ResetChartProperty, value); }
    }

    public static readonly DependencyProperty ResetChartProperty =
      DependencyProperty.Register(
        nameof(ResetChart),
        typeof(ICommand),
        typeof(Ruler), null);


    #endregion

    public decimal MaxValue
    {
      get
      {
        if (DrawingViewModel != null)
          return Mode == RulerMode.Horizontal ? DrawingViewModel.MaxUnix : DrawingViewModel.MaxValue;
        return 1;
      }
      set
      {
        if (Mode == RulerMode.Horizontal)
        {
          DrawingViewModel.MaxUnix = (long)value;
        }
        else
          DrawingViewModel.MaxValue = value;
      }
    }

    public decimal MinValue
    {
      get
      {
        if (DrawingViewModel != null)
          return Mode == RulerMode.Horizontal ? DrawingViewModel.MinUnix : DrawingViewModel.MinValue;
        return 1;
      }
      set
      {
        if (Mode == RulerMode.Horizontal)
        {
          DrawingViewModel.MinUnix = (long)value;
        }
        else
          DrawingViewModel.MinValue = value;
      }
    }

    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);

      SizeChanged += Ruler_SizeChanged;
      MouseDoubleClick += Ruler_MouseDoubleClick;
    }

    private void Ruler_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      ResetChart?.Execute(null);
    }

    private void Ruler_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (MaxValue > 0 && MinValue > 0)
        RenderValues();
    }

    private void ValuesToRender_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      RenderValues();
    }

    private void LablesToRender_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      RenderLabels();
    }

    protected virtual void FrameworkElement_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      RenderValues();
    }


    private void Overlay_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      RenderValues();
    }

    public Ruler()
    {
      InitializeComponent();

      MouseLeftButtonDown += Ruler_MouseLeftButtonDown;
      MouseLeftButtonUp += Ruler_MouseLeftButtonUp;
      MouseMove += Grid_MouseMove;
      MouseWheel += Border_MouseWheel;
    }

    private void Ruler_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      ReleaseMouseCapture();
      start = null;
      Mouse.OverrideCursor = null;
    }

    private void Ruler_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      Mouse.Capture(this);
      var position = e.GetPosition(this);
      start = Mode == RulerMode.Horizontal ? position.X : position.Y;
      Mouse.OverrideCursor = Mode == RulerMode.Horizontal ? Cursors.SizeWE : Cursors.SizeNS;
    }

    #region Border_MouseWheel

    private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var delta = (decimal)0.055;
      var diff = (MaxValue - MinValue) * delta;

      if (e.Delta > 0)
      {
        MinValue += diff;
      }
      else
      {
        MinValue -= diff;
      }
    }

    double? start;

    #endregion

    #region Grid_MouseMove

    private void Grid_MouseMove(object sender, MouseEventArgs e)
    {
      base.OnMouseMove(e);
      var position = e.GetPosition(this);

      if (start != null)
      {
        var size = Mode == RulerMode.Horizontal ? ActualWidth : ActualHeight;
        var startPrice = TradingHelper.GetValueFromCanvas(size, start.Value, MaxValue, MinValue);
        var nextPrice = TradingHelper.GetValueFromCanvas(size, Mode == RulerMode.Horizontal ? position.X : position.Y, MaxValue, MinValue);

        var delta = (nextPrice * 100 / startPrice) / 100;

        if (delta < 1)
        {
          if (Mode == RulerMode.Vertical)
            MaxValue *= delta;

          MinValue *= (1 - delta) + 1;
        }
        else
        {
          if (Mode == RulerMode.Vertical)
            MaxValue *= delta;
          MinValue *= 1 - (delta - 1);
        }

        if (MinValue < 0)
        {
          MinValue = 0;
        }

        start = Mode == RulerMode.Horizontal ? position.X : position.Y;
      }
    }

    #endregion

    protected Border labelBorder;

    #region RenderValues

    protected virtual void RenderValues()
    {
      RenderLabels();
    }

    #endregion

    public abstract void RenderLabel(Point mousePoint, decimal price, DateTime date, int assetPriceRound);
    public abstract void ClearLabel();
    protected abstract void RenderLabels();

    #region OnPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
