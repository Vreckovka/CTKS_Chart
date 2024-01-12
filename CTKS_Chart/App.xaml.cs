using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CTKS_Chart.Binance;
using CTKS_Chart.ViewModels;
using CTKS_Chart.Views;
using Logger;
using Prism.Ioc;
using VCore.WPF;
using VCore.WPF.Logger;
using VCore.WPF.Managers;
using VCore.WPF.Views.SplashScreen;

namespace CTKS_Chart
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>

  public class CtksApplication : VApplication<MainWindow, MainWindowViewModel, SplashScreenView>
  {
   protected override void LoadModules()
    {
      base.LoadModules();

      Kernel.Rebind<ILoggerContainer>().To<CollectionLogger>();

      Kernel.Bind<BinanceBroker>()
        .To<BinanceBroker>()
        .InSingletonScope();
    }
  }

  public partial class App : CtksApplication
  {
    
  }
}
