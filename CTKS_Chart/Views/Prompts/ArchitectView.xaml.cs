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

    private void Border_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var delta = 0.005;
      if (DataContext is ArchitectViewModel viewModel)
      {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
          if (e.Delta > 0)
          {
            var newMin = viewModel.MinValue;
            newMin *= (decimal)(1 - delta);

            if (newMin > 0)
              viewModel.MinValue = newMin;
          }
          else
          {
            viewModel.MinValue *= (decimal)(1 + delta);
          }
        }
        else if (Keyboard.Modifiers == ModifierKeys.Alt)
        {
          if (e.Delta > 0)
          {
            viewModel.CandleCount -= 1;
          }
          else
          {
            viewModel.CandleCount += 1;
          }
        }
        else
        {
          if (e.Delta > 0)
          {
            viewModel.MaxValue *= (decimal)(1 - delta);
          }
          else
          {
            viewModel.MaxValue *= (decimal)(1 + delta);
          }
        }
      }
    }
  }
}
