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
        typeof(Chart), new PropertyMetadata(null, new PropertyChangedCallback((obj,y) => { 
        
        if(obj is Chart chart)
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

          ActualMousePositionX = DateTimeHelper.UnixTimeStampToLocalDateTime((long)TradingHelper.GetValueFromCanvasLinear(ChartContent.ActualWidth, actualMousePosition.X, (long)MaxXValue, (long)MinXValue));
          ActualMousePositionY = TradingHelper.GetValueFromCanvas(ChartContent.ActualHeight, ChartContent.ActualHeight - actualMousePosition.Y, MaxYValue, MinYValue);

          if(AssetPriceRound > 0)
          {
            ActualMousePositionY = Math.Round(actualMousePositionY, AssetPriceRound);
          }
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

    #region AssetPriceRound

    private int assetPriceRound;

    public int AssetPriceRound
    {
      get { return assetPriceRound; }
      set
      {
        if (value != assetPriceRound)
        {
          assetPriceRound = value;
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


    public Chart()
    {
      InitializeComponent();
    }


    double? startY;
    double? startX;

    private void Grid_MouseMove(object sender, MouseEventArgs e)
    {
      base.OnMouseMove(e);

      if (sender is FrameworkElement fr)
      {
        ActualMousePosition = e.GetPosition(ChartContent);

        if (e.LeftButton == MouseButtonState.Pressed)
        {
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

    private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var delta = (decimal)0.005;

      if (e.Delta > 0)
      {
        MinXValue *= (1 + delta);
      }
      else
      {
        MinXValue *= (1 - delta);
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
