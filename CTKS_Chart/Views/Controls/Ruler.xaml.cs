using System;
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
using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;

namespace CTKS_Chart.Views.Controls
{
  /// <summary>
  /// Interaction logic for Ruler.xaml
  /// </summary>
  public partial class Ruler : UserControl
  {
    public Ruler()
    {
      InitializeComponent();
    }

    private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var delta = 0.05;
      if (DataContext is IDrawingViewModel viewModel)
      {
        if (e.Delta > 0)
        {
          viewModel.MaxValue *= (decimal)(1 - delta);
          viewModel.MinValue *= (decimal)(1 + delta);
        }
        else
        {
          viewModel.MaxValue *= (decimal)(1 + delta);
          viewModel.MinValue *= (decimal)(1 - delta);
        }
      }
    }

    double? startY;
    private void Grid_MouseMove(object sender, MouseEventArgs e)
    {
      if (DataContext is IDrawingViewModel viewModel)
      {
        base.OnMouseMove(e);
        var position = e.GetPosition(this);

        if (e.LeftButton == MouseButtonState.Pressed && startY != null)
        {       
          var startPrice = TradingHelper.GetValueFromCanvas(viewModel.CanvasHeight, startY.Value, viewModel.MaxValue, viewModel.MinValue);
          var nextPrice = TradingHelper.GetValueFromCanvas(viewModel.CanvasHeight, position.Y, viewModel.MaxValue, viewModel.MinValue);

          var deltaY = (nextPrice * 100 / startPrice) / 100;

          if (deltaY < 1)
          {
            viewModel.MaxValue *= deltaY;
            viewModel.MinValue *= (1 - deltaY) + 1;
          }
          else
          {
            viewModel.MaxValue *= deltaY;
            viewModel.MinValue *= 1 - (deltaY - 1);
          }

          if(viewModel.MinValue < 0 )
          {
            viewModel.MinValue = 0;
          }
        }

        startY = position.Y;

        if (e.LeftButton != MouseButtonState.Pressed && startY != null)
        {
          startY = null;
        }
      }
    }
  }
}
