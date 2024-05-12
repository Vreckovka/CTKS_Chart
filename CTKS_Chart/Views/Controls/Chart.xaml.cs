using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using VCore.Standard;
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

    bool renderOverlay = false;
    public Canvas Overlay { get; } = new Canvas() { Background = Brushes.Transparent };

    List<OverlayControl> OverlayControls = new List<OverlayControl>();

    public MeasureTool MeasureTool { get; } = new MeasureTool();

    public Chart()
    {
      InitializeComponent();

      Overlay.MouseLeave += Overlay_MouseLeave;
      Overlay.MouseEnter += Overlay_MouseEnter;
      Overlay.MouseLeftButtonDown += Overlay_MouseLeftButtonDown;

      MeasureTool.Overlay = Overlay;

      OverlayControls.Add(MeasureTool);
    }

    private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if(MeasureTool.IsEnabled)
      {
        if (MeasureTool.IsVisible)
        {
          if (MeasureTool.StartPoint != null && MeasureTool.EndPoint == null)
          {
            MeasureTool.EndPoint = e.GetPosition(Overlay);
          }
          else if (MeasureTool.StartPoint != null && MeasureTool.EndPoint != null)
          {
            MeasureTool.IsEnabled = false;
          }
        }
        else
        {
          MeasureTool.IsVisible = true;
        }
      }
    

    }

    private void Overlay_MouseEnter(object sender, MouseEventArgs e)
    {
      renderOverlay = true;
    }

    private void Overlay_MouseLeave(object sender, MouseEventArgs e)
    {
      renderOverlay = false;
      
      ClearOverlay();
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

        if (wasPressed)
        {
          Mouse.OverrideCursor = null;
          wasPressed = false;
        }


        if (renderOverlay)
        {
          DrawOveralay();

          RenderControls(ActualOverlayMousePosition, ActualMousePositionY, ActualMousePositionX);
        }

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

    #region ClearOverlay

    private void ClearOverlay()
    {
      //Overlay.Children.Clear();

      Overlay.Children.Remove(verticalCrosshair);
      Overlay.Children.Remove(horizontalCrosshair);
      Overlay.Children.Remove(priceBorder);
      Overlay.Children.Remove(dateBorder);


      verticalCrosshair = null;
      horizontalCrosshair = null;
      priceBorder = null;
      dateBorder = null;

      priceTextBlock = null;
      dateTextBlock = null;


    }

    #endregion

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

      if (verticalCrosshair != null)
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

      if (dateBorder == null)
      {
        var formattedTextDate = DrawingHelper.GetFormattedText(dateText, brush, fontSize);
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
      var delta = (decimal)0.035;
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

    private void RenderControls(Point mousePoint, decimal representedPrice, DateTime represnetedDate)
    {
      foreach (var control in OverlayControls.Where(x => x.IsVisible))
      {
        control.Render(mousePoint, representedPrice, represnetedDate);
      }
    }

    #region OnPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }


  public abstract class OverlayControl : ViewModel
  {
    public Canvas Overlay { get; set; }
    public Border UIElement { get; set; }
    public abstract void Render(Point mousePoint, decimal representedPrice, DateTime represnetedDate);

    #region IsVisible

    private bool isVisible;

    public bool IsVisible
    {
      get { return isVisible; }
      set
      {
        if (value != isVisible)
        {
          isVisible = value;

          if (!value)
          {
            Clear();
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsEnabled

    private bool isEnabled;

    public bool IsEnabled
    {
      get { return isEnabled; }
      set
      {
        if (value != isEnabled)
        {
          isEnabled = value;

          if (!value)
            IsVisible = false;

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public virtual void Clear()
    {
      Overlay.Children.Remove(UIElement);
      UIElement = null;
      IsVisible = false;
    }
  }

  public class MeasureTool : OverlayControl
  {
    public Point? StartPoint { get; set; }
    public Point? EndPoint { get; set; }
    public Brush Background { get; set; }
    public Border Tooltip { get; set; }

    public decimal StartPrice { get; set; }
    public DateTime StartDate { get; set; }

    public override void Render(Point mousePoint, decimal representedPrice, DateTime represnetedDate)
    {
      var green = DrawingHelper.GetBrushFromHex("#45aaf542");
      var red = DrawingHelper.GetBrushFromHex("#45f54242");

      if (UIElement == null)
      {
        StartPoint = mousePoint;
        StartPrice = representedPrice;
        StartDate = represnetedDate;

        UIElement = new Border();
        Tooltip = new Border() { Padding = new Thickness(5) };

        StackPanel stackPanel = new StackPanel() { Orientation = Orientation.Vertical };

        var text = new TextBlock() { FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };

        text.Inlines.Add(new Run());
        text.Inlines.Add(new LineBreak());
        text.Inlines.Add(new Run());

        text.Inlines.Add(new LineBreak());
        text.Inlines.Add(new Run());

        stackPanel.Children.Add(text);

        Tooltip.Child = stackPanel;

        Canvas.SetLeft(UIElement, mousePoint.X);
        Canvas.SetTop(UIElement, mousePoint.Y);

        Overlay.Children.Add(UIElement);
        Overlay.Children.Add(Tooltip);
      }

      if (StartPoint != null && EndPoint == null)
      {
        var startPoint = StartPoint.Value;

        var diffX = Math.Abs(startPoint.X - mousePoint.X);
        var diffY = Math.Abs(startPoint.Y - mousePoint.Y);

        if (startPoint.X > mousePoint.X)
        {
          Canvas.SetLeft(UIElement, mousePoint.X);
        }

        if (startPoint.Y > mousePoint.Y)
        {
          Canvas.SetTop(UIElement, mousePoint.Y);
          Canvas.SetTop(Tooltip, mousePoint.Y - Tooltip.ActualHeight - 5);

          Background = green;
        }
        else
        {
          Canvas.SetTop(Tooltip, mousePoint.Y + 5);

          Background = red;
        }

        Canvas.SetLeft(Tooltip, Canvas.GetLeft(UIElement) + ((UIElement.ActualWidth / 2) - (Tooltip.ActualWidth / 2)));


        UIElement.Background = Background;
        Tooltip.Background = Background;

        if (Tooltip.Child is StackPanel stackPanel)
        {
          var text = (TextBlock)stackPanel.Children[0];

          ((Run)text.Inlines.ToList()[0]).Text = $"{StartPrice} - {representedPrice}";
          ((Run)text.Inlines.ToList()[2]).Text = $"{representedPrice - StartPrice} ({((StartPrice - representedPrice) / StartPrice * 100 * -1).ToString("N2")}%)";

          ((Run)text.Inlines.ToList()[4]).Text = $"{(represnetedDate - StartDate).ToString(@"dd\.hh\:mm\:ss")}";
        }

        UIElement.Width = diffX;
        UIElement.Height = diffY;
      }
    }

    public override void Clear()
    {
      base.Clear();
      Overlay.Children.Remove(Tooltip);

      StartPoint = null;
      EndPoint = null;
      Tooltip = null;
    }
  }
}
