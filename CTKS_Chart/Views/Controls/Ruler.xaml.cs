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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using VCore.WPF.Controls;

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
  public partial class Ruler : UserControl, INotifyPropertyChanged
  {
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

    #region Mode

    private RulerMode mode;

    public RulerMode Mode
    {
      get { return mode; }
      set
      {
        if (value != mode)
        {
          mode = value;
          OnPropertyChanged();
        }
      }
    }

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

    private void ValuesToRender_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      RenderValues();
    }

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

    private void Overlay_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      RenderValues();
    }

    public Canvas Overlay { get; } = new Canvas();

    public Ruler()
    {
      InitializeComponent();
    }

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

    private void RenderValues()
    {
      Overlay.Children.Clear();

      foreach (var intersection in ValuesToRender)
      {
        var pricePositionY = TradingHelper.GetCanvasValue(Overlay.ActualHeight, intersection.Model.Value, MaxValue, MinValue);

        var text = Math.Round(intersection.Model.Value, AssetPriceRound).ToString();
        var fontSize = 11;

        var formattedText = DrawingHelper.GetFormattedText(text, intersection.SelectedBrush, fontSize);

        var price = new TextBlock() { Text = text, FontSize = fontSize, Foreground = intersection.SelectedBrush };

        var padding = 5;
        Width = formattedText.Width + (padding * 2);
        pricePositionY = pricePositionY + (formattedText.Height / 2);
        Overlay.Children.Add(price);

        Canvas.SetLeft(price, padding);
        Canvas.SetTop(price, Overlay.ActualHeight - pricePositionY);
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
      if (EqualityComparer<T>.Default.Equals(field, value)) return false;
      field = value;
      OnPropertyChanged(propertyName);
      return true;
    }
  }
}
