using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using VCore.Standard.Helpers;

namespace CTKS_Chart.Views.Controls
{
  /// <summary>
  /// Interaction logic for Chart.xaml
  /// </summary>
  public partial class Chart : UserControl, INotifyPropertyChanged
  {
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
        typeof(Chart), new PropertyMetadata(null, new PropertyChangedCallback((obj, y) =>
        {

          if (obj is Chart chart && y.NewValue is FrameworkElement frameworkElement)
          {
            chart.MouseMove += chart.Grid_MouseMove;
            chart.MouseLeave += chart.Chart_MouseLeave;
          }
        })));


    #endregion

    #region ChartHeight

    public double ChartHeight
    {
      get { return (double)GetValue(ChartHeightProperty); }
      set { SetValue(ChartHeightProperty, value); }
    }

    public static readonly DependencyProperty ChartHeightProperty =
      DependencyProperty.Register(
        nameof(ChartHeight),
        typeof(double),
        typeof(Chart));

    #endregion

    #region ChartWidth

    public double ChartWidth
    {
      get { return (double)GetValue(ChartWidthProperty); }
      set { SetValue(ChartWidthProperty, value); }
    }

    public static readonly DependencyProperty ChartWidthProperty =
      DependencyProperty.Register(
        nameof(ChartWidth),
        typeof(double),
        typeof(Chart));

    #endregion

    #region MaxYValue

    public decimal MaxYValue
    {
      get { return (decimal)GetValue(MaxYValueProperty); }
      set { SetValue(MaxYValueProperty, value); }
    }

    public static readonly DependencyProperty MaxYValueProperty =
      DependencyProperty.Register(
        nameof(MaxYValue),
        typeof(decimal),
        typeof(Chart));

    #endregion

    #region MinYValue

    public decimal MinYValue
    {
      get { return (decimal)GetValue(MinYValueProperty); }
      set { SetValue(MinYValueProperty, value); }
    }

    public static readonly DependencyProperty MinYValueProperty =
      DependencyProperty.Register(
        nameof(MinYValue),
        typeof(decimal),
        typeof(Chart));

    #endregion

    #region MaxXValue

    public decimal MaxXValue
    {
      get { return (decimal)GetValue(MaxXValueProperty); }
      set { SetValue(MaxXValueProperty, value); }
    }

    public static readonly DependencyProperty MaxXValueProperty =
      DependencyProperty.Register(
        nameof(MaxXValue),
        typeof(decimal),
        typeof(Chart));

    #endregion

    #region MinXValue

    public decimal MinXValue
    {
      get { return (decimal)GetValue(MinXValueProperty); }
      set { SetValue(MinXValueProperty, value); }
    }

    public static readonly DependencyProperty MinXValueProperty =
      DependencyProperty.Register(
        nameof(MinXValue),
        typeof(decimal),
        typeof(Chart));

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
        typeof(Chart), new PropertyMetadata(5));


    #endregion

    #region ActualMousePosition

    private Point actualMousePosition;

    public Point ActualMousePosition
    {
      get { return actualMousePosition; }
      set
      {
        if (value != actualMousePosition)
        {
          actualMousePosition = value;
          OnPropertyChanged();

          ActualMousePositionX = DateTimeHelper.UnixTimeStampToUtcDateTime((long)TradingHelper.GetValueFromCanvasLinear(ChartContent.ActualWidth, actualMousePosition.X, (long)MaxXValue, (long)MinXValue));
          ActualMousePositionY = TradingHelper.GetValueFromCanvas(ChartContent.ActualHeight, ChartContent.ActualHeight - actualMousePosition.Y, MaxYValue, MinYValue);

          if (AssetPriceRound > 0)
          {
            ActualMousePositionY = Math.Round(actualMousePositionY, AssetPriceRound);
          }
        }
      }
    }

    #endregion

    #region ActualOverlayMousePosition

    private Point actualOverlayMousePosition;

    public Point ActualOverlayMousePosition
    {
      get { return actualOverlayMousePosition; }
      set
      {
        if (value != actualOverlayMousePosition)
        {
          actualOverlayMousePosition = value;
          OnPropertyChanged();
        }
      }
    }

    #endregion

    #region ActualMousePositionY

    private decimal actualMousePositionY;

    public decimal ActualMousePositionY
    {
      get { return actualMousePositionY; }
      set
      {
        if (value != actualMousePositionY)
        {
          actualMousePositionY = value;
          OnPropertyChanged();
        }
      }
    }

    #endregion

    #region ActualMousePositionX

    private DateTime actualMousePositionX;

    public DateTime ActualMousePositionX
    {
      get { return actualMousePositionX; }
      set
      {
        if (value != actualMousePositionX)
        {
          actualMousePositionX = value;
          OnPropertyChanged();
        }
      }
    }

    #endregion

    public Canvas Overlay { get; } = new Canvas() { Background = Brushes.Transparent };

    public Chart()
    {
      InitializeComponent();

      Overlay.MouseLeave += Overlay_MouseLeave;
      Overlay.MouseEnter += Overlay_MouseEnter;
    }

    private void Overlay_MouseEnter(object sender, MouseEventArgs e)
    {
      renderOverlay = true;
    }

    bool renderOverlay = false;
    private void Overlay_MouseLeave(object sender, MouseEventArgs e)
    {
      ClearOverlay();

      renderOverlay = false;
    }

    private void Chart_MouseLeave(object sender, MouseEventArgs e)
    {
      ClearOverlay();

      renderOverlay = false;
    }

    #region Grid_MouseMove

    double? startY;
    double? startX;

    bool wasPressed = false;
    private void Grid_MouseMove(object sender, MouseEventArgs e)
    {
      base.OnMouseMove(e);

      if (sender is FrameworkElement fr)
      {
        ActualMousePosition = e.GetPosition(ChartContent);
        ActualOverlayMousePosition = e.GetPosition(Overlay);

        if(wasPressed)
        {
          Mouse.OverrideCursor = null;
          wasPressed = false;
        }
      

        if (renderOverlay)
          DrawOveralay();

        if (e.LeftButton == MouseButtonState.Pressed)
        {
          Mouse.OverrideCursor = Cursors.Hand;
          wasPressed = true;

          if (startY != null)
          {
            var startPrice = TradingHelper.GetValueFromCanvas(ChartHeight, startY.Value, MaxYValue, MinYValue);
            var nextPrice = TradingHelper.GetValueFromCanvas(ChartHeight, ActualMousePosition.Y, MaxYValue, MinYValue);

            var deltaY = (nextPrice * 100 / startPrice) / 100;

            MaxYValue *= deltaY;
            MinYValue *= deltaY;
          }

          if (startX != null)
          {
            var startPrice = TradingHelper.GetValueFromCanvas(ChartWidth, startX.Value, MaxXValue, MinXValue);
            var nextPrice = TradingHelper.GetValueFromCanvas(ChartWidth, ActualMousePosition.X, MaxXValue, MinXValue);

            var deltaY = (startPrice * 100 / nextPrice) / 100;

            MaxXValue *= deltaY;
            MinXValue *= deltaY;
          }

          startY = ActualMousePosition.Y;
          startX = ActualMousePosition.X;
        }

        if (e.LeftButton != MouseButtonState.Pressed && startY != null)
        {
          startY = null;
          startX = null;
        }
      }

    }

    #endregion

    private void ClearOverlay()
    {
      Overlay.Children.Clear();

      verticalCrosshair = null;
      horizontalCrosshair = null;
      priceBorder = null; 
      dateBorder = null; 

      priceTextBlock = null;
      dateTextBlock = null;
    }

    #region DrawOverlay

    Line verticalCrosshair;
    Line horizontalCrosshair;
    Border priceBorder;
    Border dateBorder;
    TextBlock priceTextBlock;
    TextBlock dateTextBlock;

    private void DrawOveralay()
    {
      var gray = DrawingHelper.GetBrushFromHex("#45ffffff");

      if(verticalCrosshair != null)
      {
        Canvas.SetLeft(verticalCrosshair, ActualOverlayMousePosition.X);
      }
      else
      {
        verticalCrosshair = new Line()
        {
          X1 = 0,
          X2 = 0,
          Y1 = 0,
          Y2 = Overlay.ActualHeight,
          Stroke = gray,
          StrokeThickness = 1,
          StrokeDashArray = new DoubleCollection() { 5 },
          IsHitTestVisible = false
        };

        Canvas.SetLeft(verticalCrosshair, ActualOverlayMousePosition.X);

        Overlay.Children.Add(verticalCrosshair);
      };

      if (horizontalCrosshair != null)
      {
        Canvas.SetTop(horizontalCrosshair, ActualOverlayMousePosition.Y);
      }
      else
      {
        horizontalCrosshair = new Line()
        {
          X1 = 0,
          X2 = Overlay.ActualWidth,
          Y1 = 0,
          Y2 = 0,
          Stroke = gray,
          StrokeThickness = 1,
          StrokeDashArray = new DoubleCollection() { 5 },
          IsHitTestVisible = false
        };

        Canvas.SetTop(horizontalCrosshair, ActualOverlayMousePosition.Y);
        Overlay.Children.Add(horizontalCrosshair);
      };


      var priceText = Math.Round(ActualMousePositionY, AssetPriceRound).ToString();
      var dateText = ActualMousePositionX.ToString("dd.MM.yyy HH:mm:ss");

      var fontSize = 11;
      var brush = Brushes.White;

      if (priceBorder == null)
      {
        priceBorder = new Border()
        {
          Background = DrawingHelper.GetBrushFromHex("#3b3d40"),
          Padding = new Thickness(2, 2, 2, 2),
          CornerRadius = new CornerRadius(2, 2, 2, 2),
          IsHitTestVisible = false,
        };

        priceTextBlock = new TextBlock() { Text = priceText, FontSize = fontSize, Foreground = brush, FontWeight = FontWeights.Bold };
        var formattedText = DrawingHelper.GetFormattedText(priceText, brush, fontSize);

        priceBorder.Child = priceTextBlock;

        Canvas.SetLeft(priceBorder, Overlay.ActualWidth - (formattedText.Width + 2 + 2 + 4));
        Canvas.SetTop(priceBorder, ActualOverlayMousePosition.Y - ((formattedText.Height / 2) + 2));

        Overlay.Children.Add(priceBorder);
      }
      else
      {
        priceTextBlock.Text = priceText;

        Canvas.SetTop(priceBorder, ActualOverlayMousePosition.Y - ((this.priceTextBlock.ActualHeight / 2) + 2));
      }
    
     
 
     
      if(dateBorder == null)
      {
        var formattedTextDate = DrawingHelper.GetFormattedText(ActualMousePositionX.ToString("dd.MM.yyy HH:mm:ss"), brush, fontSize);
        dateTextBlock = new TextBlock() { Text = dateText, FontSize = fontSize, Foreground = brush, FontWeight = FontWeights.Bold };
        
        dateBorder = new Border()
        {
          Background = priceBorder.Background,
          Padding = priceBorder.Padding,
          CornerRadius = priceBorder.CornerRadius,
          IsHitTestVisible = false,
        };

        dateBorder.Child = dateTextBlock;

        Overlay.Children.Add(dateBorder);
        Canvas.SetLeft(dateBorder, ActualOverlayMousePosition.X - (formattedTextDate.Width / 2));
        Canvas.SetTop(dateBorder, Overlay.ActualHeight - 20);
      }
      else
      {
        dateTextBlock.Text = dateText;
        Canvas.SetLeft(dateBorder, ActualOverlayMousePosition.X - (dateTextBlock.ActualWidth / 2));
      }
    }

    #endregion

    #region Border_MouseWheel

    private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var delta = (decimal)0.025;
      var diff = (MaxXValue - MinXValue) * delta;

      if (e.Delta > 0)
      {
        MinXValue += diff;
      }
      else
      {
        MinXValue -= diff;
      }
    }

    #endregion

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
