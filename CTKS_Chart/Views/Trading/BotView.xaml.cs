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

    private void Border_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
      var delta = 0.005;
      if (DataContext is TradingBotViewModel tradingBot)
      {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
          if (e.Delta > 0)
          {
            tradingBot.DrawingViewModel.MinValue *= (decimal)(1 - delta);
          }
          else
          {
            tradingBot.DrawingViewModel.MinValue *= (decimal)(1 + delta);
          }
        }
        else if (Keyboard.Modifiers == ModifierKeys.Alt)
        {
          if (e.Delta > 0)
          {
            tradingBot.DrawingViewModel.CandleCount -= 1;
          }
          else
          {
            tradingBot.DrawingViewModel.CandleCount += 1;
          }
        }
        else
        {
          if (e.Delta > 0)
          {
            tradingBot.DrawingViewModel.MaxValue *= (decimal)(1 - delta);
          }
          else
          {
            tradingBot.DrawingViewModel.MaxValue *= (decimal)(1 + delta);
          }
        }
      }
    }
  }
}
