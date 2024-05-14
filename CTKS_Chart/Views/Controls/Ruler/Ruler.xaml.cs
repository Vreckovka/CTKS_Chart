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

namespace CTKS_Chart.Views.Controls
{
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

    protected List<FrameworkElement> Labels { get; } = new List<FrameworkElement>();

    #region MaxValue

    public decimal MaxValue
    {
      get { return (decimal)GetValue(MaxValueProperty); }
      set { SetValue(MaxValueProperty, value); }
    }

    public static readonly DependencyProperty MaxValueProperty =
      DependencyProperty.Register(
        nameof(MaxValue),
        typeof(decimal),
        typeof(Ruler), new PropertyMetadata(0.0m, (x, y) =>
        {

          if (x is Ruler ruler)
          {
            ruler.RenderValues();
          }
        }));


    #endregion

    #region MinValue

    public decimal MinValue
    {
      get { return (decimal)GetValue(MinValueProperty); }
      set { SetValue(MinValueProperty, value); }
    }

    public static readonly DependencyProperty MinValueProperty =
      DependencyProperty.Register(
        nameof(MinValue),
        typeof(decimal),
        typeof(Ruler), new PropertyMetadata(0.0m, (x, y) =>
        {

          if (x is Ruler ruler)
          {
            ruler.RenderValues();
          }
        }));


    #endregion

    #region Size

    public double Size
    {
      get { return (double)GetValue(SizeProperty); }
      set { SetValue(SizeProperty, value); }
    }

    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(Ruler),
        new PropertyMetadata(0.0));


    #endregion

    #region ValuesToRender

    public ObservableCollection<RenderedIntesection> ValuesToRender
    {
      get { return (ObservableCollection<RenderedIntesection>)GetValue(ValuesToRenderProperty); }
      set { SetValue(ValuesToRenderProperty, value); }
    }

    public static readonly DependencyProperty ValuesToRenderProperty =
      DependencyProperty.Register(
        nameof(ValuesToRender),
        typeof(ObservableCollection<RenderedIntesection>),
        typeof(Ruler), new PropertyMetadata(new ObservableCollection<RenderedIntesection>(), (x, y) =>
        {

          if (x is Ruler ruler)
          {
            ruler.RenderValues();
            ruler.ValuesToRender.CollectionChanged += ruler.ValuesToRender_CollectionChanged;
          }
        }));



    #endregion

    #region ChartContent

    public FrameworkElement ChartContent
    {
      get { return (FrameworkElement)GetValue(ChartContentProperty); }
      set { SetValue(ChartContentProperty, value); }
    }

    public static readonly DependencyProperty ChartContentProperty =
      DependencyProperty.Register(
        nameof(ChartContent),
        typeof(FrameworkElement),
        typeof(Ruler), new PropertyMetadata(null, (obj, y) =>
        {
          if (obj is Ruler ruler && y.NewValue is FrameworkElement frameworkElement)
          {
            frameworkElement.SizeChanged += ruler.FrameworkElement_SizeChanged;
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

    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);

      SizeChanged += Ruler_SizeChanged;
    }

    private void Ruler_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      RenderValues();
    }

    private void ValuesToRender_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      RenderValues();
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
    }

    #region Border_MouseWheel

    private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var delta = 0.05;

      if (e.Delta > 0)
      {
        MaxValue *= (decimal)(1 - delta);
        MinValue *= (decimal)(1 + delta);
      }
      else
      {
        MaxValue *= (decimal)(1 + delta);
        MinValue *= (decimal)(1 - delta);
      }
    }

    double? start;

    #endregion

    #region Grid_MouseMove

    private void Grid_MouseMove(object sender, MouseEventArgs e)
    {
      base.OnMouseMove(e);
      var position = e.GetPosition(this);

      if (e.LeftButton == MouseButtonState.Pressed && start != null)
      {
        var startPrice = TradingHelper.GetValueFromCanvas(Size, start.Value, MaxValue, MinValue);
        var nextPrice = TradingHelper.GetValueFromCanvas(Size, Mode == RulerMode.Horizontal ? position.X : position.Y, MaxValue, MinValue);

        var delta = (nextPrice * 100 / startPrice) / 100;

        if (delta < 1)
        {
          MaxValue *= delta;
          MinValue *= (1 - delta) + 1;
        }
        else
        {
          MaxValue *= delta;
          MinValue *= 1 - (delta - 1);
        }

        if (MinValue < 0)
        {
          MinValue = 0;
        }
      }

      start = Mode == RulerMode.Horizontal ? position.X : position.Y;

      if (e.LeftButton != MouseButtonState.Pressed && start != null)
      {
        start = null;
      }
    }

    #endregion

    protected Border labelBorder;

    #region RenderValues

    protected virtual void RenderValues()
    {
      foreach(var label in Labels)
      {
        Overlay.Children.Remove(label);
      }

      Labels.Clear();
    }

    #endregion

    public abstract void RenderLabel(Point mousePoint, decimal price, DateTime date, int assetPriceRound);
    public abstract void ClearLabel();
   
  

    #region OnPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
