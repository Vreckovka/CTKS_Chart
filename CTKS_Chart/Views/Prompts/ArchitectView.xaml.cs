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
      if (DataContext is IDrawingViewModel viewModel)
      {
        base.OnMouseMove(e);
        var delta = 0.01;
        var position = e.GetPosition(this);

        if (e.LeftButton == MouseButtonState.Pressed && startY != null)
        {
          var startPrice = TradingHelper.GetValueFromCanvas(viewModel.CanvasHeight, startY.Value, viewModel.MaxValue, viewModel.MinValue);
          var nextPrice = TradingHelper.GetValueFromCanvas(viewModel.CanvasHeight, position.Y, viewModel.MaxValue, viewModel.MinValue);

          var deltaY = (nextPrice * 100 / startPrice) / 100;


          viewModel.MaxValue *= deltaY;
          viewModel.MinValue *= deltaY;
        }

        startY = position.Y;

        if (e.LeftButton != MouseButtonState.Pressed && startY != null)
        {
          startY = null;
        }
      }
    }

    private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (DataContext is IDrawingViewModel viewModel)
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
