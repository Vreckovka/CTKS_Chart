using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CTKS_Chart.ViewModels;
using CTKS_Chart.Views;
using Logger;
using VCore.WPF;
using VCore.WPF.Managers;
using VCore.WPF.Views.SplashScreen;

namespace CTKS_Chart
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>

  public class CtksApplication : VApplication<MainWindow, MainWindowViewModel, SplashScreenView>
  {
  }

  public partial class App : CtksApplication
  {
    protected override void OnStartup(StartupEventArgs e)
    {
      IsConsoleVisible = true;

      base.OnStartup(e);
    }
  }
}
