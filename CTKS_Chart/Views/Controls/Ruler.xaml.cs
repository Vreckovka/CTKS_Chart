﻿using System;
using System.Collections.Generic;
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
        typeof(Ruler));


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
        typeof(Ruler));


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
