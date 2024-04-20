using System;
using System.Collections.Generic;
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

namespace CTKS_Chart.Views.Trading
{
  /// <summary>
  /// Interaction logic for BotView.xaml
  /// </summary>
  public partial class BotView : UserControl
  {
    public BotView()
    {
      InitializeComponent();
    }

    double? startY;
    private void Grid_MouseMove(object sender, MouseEventArgs e)
    {
      if (DataContext is TradingBotViewModel vm)
      {
        var viewModel = vm.DrawingViewModel;

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
      if (DataContext is TradingBotViewModel vm)
      {
        var viewModel = vm.DrawingViewModel;

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
