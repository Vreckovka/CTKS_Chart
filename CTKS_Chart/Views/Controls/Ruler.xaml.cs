using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
using VCore.Standard.Helpers;
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

    private void FrameworkElement_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      if(Mode == RulerMode.Horizontal)
      {
        Overlay.Width = e.NewSize.Width;
      }
      else
      {
        Overlay.Height = e.NewSize.Height;
      }

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

    #region RenderValues

    private void RenderValues()
    {
      Overlay.Children.Clear();
      var fontSize = 10;
      var padding = 8;


      if(Mode == RulerMode.Horizontal)
      {
        var diff = (long)(MaxValue - MinValue);
        var count = 9;

        var step = diff / count;
        long actualStep = (long)MinValue + (step / 2);

        for (int i = 0; i < count ; i++)
        {
          var utcDate = DateTimeHelper.UnixTimeStampToUtcDateTime(actualStep);
          var x = TradingHelper.GetCanvasValueLinear(Overlay.ActualWidth, actualStep, (long)MaxValue, (long)MinValue);

          var label = utcDate.ToString("dd.MM HH:mm:ss");

          //Month
          if (step >= 2628000)
          {
            label = utcDate.ToString("MM");
          }
          else if (step > 350000)
          {
            label = utcDate.ToString("dd.MM");
          }
          else if (step > 50000)
          {
            label = utcDate.ToString("dd.MM HH");
          }
          else if (step > 35000)
          {
            label = utcDate.ToString("dd HH:mm");
          }
          else
          {
            label = utcDate.ToString("HH:mm:ss");
          }

          var brush = DrawingHelper.GetBrushFromHex("#45ffffff");
          var formattedText = DrawingHelper.GetFormattedText(label, brush, fontSize);

          var dateText = new TextBlock()
          {
            Text = label,
            FontSize = fontSize,
            Foreground = brush,
          };

          Overlay.Children.Add(dateText);

          Canvas.SetLeft(dateText, x - (formattedText.Width / 2));
          Canvas.SetTop(dateText, Overlay.ActualHeight - ((Overlay.ActualHeight / 2) + 5) );

          actualStep += step;
        }
      }
      else if(Mode == RulerMode.Vertical)
      {
        if(ValuesToRender.Count > 0 && ValuesToRender.Count < 30)
        {
          var max = ValuesToRender
                   .Select(x => DrawingHelper.GetFormattedText($"*{Math.Round(x.Model.Value, AssetPriceRound)}*".ToString(),
                   x.SelectedBrush, fontSize))
                   .Max(x => x.Width);

          Width = max + (padding * 2);

          foreach (var intersection in ValuesToRender)
          {
            padding = 8;

            var pricePositionY = TradingHelper.GetCanvasValue(Overlay.ActualHeight, intersection.Model.Value, MaxValue, MinValue);

            var text = Math.Round(intersection.Model.Value, AssetPriceRound).ToString();

            if (intersection.Model.IsCluster)
            {
              text = $"*{text}*";
            }

            var formattedText = DrawingHelper.GetFormattedText(text, intersection.SelectedBrush, fontSize);

            var price = new TextBlock()
            {
              Text = text,
              FontSize = fontSize,
              Foreground = intersection.SelectedBrush,
              FontWeight = intersection.Model.IsCluster ? FontWeights.Bold : FontWeights.Normal
            };

            pricePositionY = pricePositionY + (formattedText.Height / 2);

            if (intersection.Model.IsCluster)
            {
              padding -= 5;
            }

            Overlay.Children.Add(price);

            Canvas.SetLeft(price, padding);
            Canvas.SetTop(price, Overlay.ActualHeight - pricePositionY);
          }
        }
        
        if((ValuesToRender.Count < 5 || ValuesToRender.Count >= 30) && Overlay.ActualHeight > 0)
        {
          var count = 12;

          var step = Overlay.ActualHeight / count;
          var actualStep = step;

          for (int i = 0; i < count - 1; i++)
          {
            var yPrice = TradingHelper.GetValueFromCanvas(Overlay.ActualHeight, actualStep, MaxValue, MinValue);
            var y = Overlay.ActualHeight - TradingHelper.GetCanvasValue(Overlay.ActualHeight, yPrice, MaxValue, MinValue);

            var brush = DrawingHelper.GetBrushFromHex("#45ffffff");
            var label = Math.Round(yPrice, AssetPriceRound).ToString();

            var formattedText = DrawingHelper.GetFormattedText(label, brush, fontSize);

            var dateText = new TextBlock()
            {
              Text = label,
              FontSize = fontSize,
              Foreground = brush,
            };

            Overlay.Children.Add(dateText);

            Canvas.SetLeft(dateText, padding);
            Canvas.SetTop(dateText, y - (formattedText.Height / 2));

            actualStep += step;
          }
        }
      }
    }

    #endregion

    #region OnPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
