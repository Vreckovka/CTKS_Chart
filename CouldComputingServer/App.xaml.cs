using CTKS_Chart.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VCore.WPF;
using VCore.WPF.Views.SplashScreen;

namespace CouldComputingServer
{

  public class CtksApplication : VApplication<MainWindow, MainWindowViewModel, SplashScreenView>
  {
    protected override void LoadModules()
    {
      base.LoadModules();
    }
  }

  public partial class App : CtksApplication
  {

  }
}
