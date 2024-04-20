﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using CTKS_Chart.ViewModels;
using VCore.Standard.Modularity.Interfaces;

namespace CTKS_Chart.Views.Prompts
{
  /// <summary>
  /// Interaction logic for ArchitectView.xaml
  /// </summary>
  public partial class ArchitectView : UserControl, IView
  {
    public ArchitectView()
    {
      InitializeComponent();
    }



    double? startY;
    Point centerPoint = new Point(500, 500);
    private void Grid_MouseMove(object sender, MouseEventArgs e)
    {
      if (DataContext is ArchitectViewModel viewModel)
      {
        base.OnMouseMove(e);
        var delta = 0.01;

        if (e.LeftButton == MouseButtonState.Pressed)
        {
          var position = e.GetPosition(this);

          var deltaY = startY - position.Y;

          if (deltaY > 0)
          {
            viewModel.MaxValue *= (decimal)(1 - delta);
            viewModel.MinValue *= (decimal)(1 - delta);
          }
          else
          {
            viewModel.MaxValue *= (decimal)(1 + delta);
            viewModel.MinValue *= (decimal)(1 + delta);
          }

          startY = position.Y;
        }

        if (e.LeftButton != MouseButtonState.Pressed && startY != null)
        {
          startY = null;
        }
      }
    }

    private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (DataContext is ArchitectViewModel viewModel)
      {

        if (e.Delta > 0)
        {
          viewModel.CandleCount += 1;
        }
        else
        {
          viewModel.CandleCount -= 1;
        }
      }
    }
  }
}
