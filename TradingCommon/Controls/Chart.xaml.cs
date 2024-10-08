﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CTKS_Chart.Strategy;
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

    public ImageSource ChartContent
    {
      get { return (ImageSource)GetValue(ChartContentProperty); }
      set { SetValue(ChartContentProperty, value); }
    }

    public static readonly DependencyProperty ChartContentProperty =
      DependencyProperty.Register(
        nameof(ChartContent),
        typeof(ImageSource),
        typeof(Chart));


    #endregion

    #region Overlay

    public Overlay Overlay
    {
      get { return (Overlay)GetValue(OverlayProperty); }
      set { SetValue(OverlayProperty, value); }
    }

    public static readonly DependencyProperty OverlayProperty =
      DependencyProperty.Register(
        nameof(Overlay),
        typeof(FrameworkElement),
        typeof(Chart), new PropertyMetadata(null, new PropertyChangedCallback((obj, y) =>
        {

          if (obj is Chart chart && y.NewValue is Overlay overlay)
          {
            overlay.MouseMove += chart.Overlay_MouseMove;
            overlay.MouseLeave += chart.Overlay_MouseLeave;
            overlay.MouseWheel += chart.Overlay_MouseWheel;
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

          if (ChartContent == null || DrawingViewModel == null)
            return;

          ActualMousePositionX = DateTimeHelper.UnixTimeStampToUtcDateTime(TradingHelper.GetValueFromCanvasLinear(ChartContent.Width, actualMousePosition.X, DrawingViewModel.MaxUnix, DrawingViewModel.MinUnix));
          ActualMousePositionY = TradingHelper.GetValueFromCanvas(ChartContent.Height, ChartContent.Height - actualMousePosition.Y, DrawingViewModel.MaxValue, DrawingViewModel.MinValue);

          if (AssetPriceRound > 0)
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
        typeof(Chart), new PropertyMetadata(new DrawingViewModel()));

    #endregion

    #region EnableChartMove

    private bool enableChartMove = true;

    public bool EnableChartMove
    {
      get { return enableChartMove; }
      set
      {
        if (value != enableChartMove)
        {
          enableChartMove = value;
          OnPropertyChanged();
        }
      }
    }

    #endregion




    public Chart()
    {
      InitializeComponent();

      SizeChanged += Chart_SizeChanged;
    }

    private void Overlay_MouseLeave(object sender, MouseEventArgs e)
    {
      if (DrawingViewModel != null)
        DrawingViewModel.EnableAutoLock = true;
    }

    private void Chart_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      ChartHeight = ActualHeight;
      ChartWidth = ActualWidth;
    }

    #region Overlay_MouseMove

    double? startY;
    double? startX;

    bool wasPressed = false;
    private void Overlay_MouseMove(object sender, MouseEventArgs e)
    {
      base.OnMouseMove(e);

      ActualMousePosition = e.GetPosition(Image);

      if (!EnableChartMove)
      {
        return;
      }

      if (wasPressed)
      {
        Mouse.OverrideCursor = null;
        wasPressed = false;
      }

      if (e.LeftButton == MouseButtonState.Pressed)
      {
        Mouse.OverrideCursor = Cursors.Hand;
        wasPressed = true;

        if (startY != null)
        {
          var startPrice = TradingHelper.GetValueFromCanvas(ChartHeight, startY.Value, DrawingViewModel.MaxValue, DrawingViewModel.MinValue);
          var nextPrice = TradingHelper.GetValueFromCanvas(ChartHeight, ActualMousePosition.Y, DrawingViewModel.MaxValue, DrawingViewModel.MinValue);

          var deltaY = (nextPrice * 100 / startPrice) / 100;

          if (deltaY != 0)
          {
            DrawingViewModel.SetMaxValue(DrawingViewModel.MaxValue *= deltaY);
            DrawingViewModel.SetMinValue(DrawingViewModel.MinValue *= deltaY);
          }
        }

        if (startX != null)
        {
          var startPrice = (double)TradingHelper.GetValueFromCanvasLinear(ChartWidth, startX.Value, DrawingViewModel.MaxUnix, DrawingViewModel.MinUnix);
          var nextPrice = (double)TradingHelper.GetValueFromCanvasLinear(ChartWidth, ActualMousePosition.X, DrawingViewModel.MaxUnix, DrawingViewModel.MinUnix);

          var deltaY = (startPrice * 100 / nextPrice) / 100.0;

          if (deltaY != 0)
          {
            DrawingViewModel.SetMaxUnix((long)(DrawingViewModel.MaxUnix * deltaY));
            DrawingViewModel.SetMinUnix((long)(DrawingViewModel.MinUnix * deltaY));
          }
        }

        startY = ActualMousePosition.Y;
        startX = ActualMousePosition.X;

        DrawingViewModel.EnableAutoLock = false;
        DrawingViewModel.SetLock(false);
        DrawingViewModel.Render();


        DrawingViewModel.Raise(nameof(DrawingViewModel.LockChart));
      }

      if (e.LeftButton != MouseButtonState.Pressed && (startY != null || startX != null))
      {
        DrawingViewModel.EnableAutoLock = true;
        startY = null;
        startX = null;
      }
    }

    #endregion

    #region Overlay_MouseWheel

    private void Overlay_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var delta = (decimal)0.035;
      var diff = (long)((DrawingViewModel.MaxUnix - DrawingViewModel.MinUnix) * delta);

      if (e.Delta > 0)
      {
        DrawingViewModel.MinUnix += diff;
      }
      else
      {
        DrawingViewModel.MinUnix -= diff;
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
