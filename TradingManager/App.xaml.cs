using ChromeDriverScrapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TradingManager.ViewModels;
using VCore.WPF;
using VCore.WPF.Views.SplashScreen;

namespace TradingManager
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  /// 

  public class TradingManagerApp : VApplication<MainWindow, MainWindowViewModel, SplashScreenView>
  {
    protected override void LoadModules()
    {
      base.LoadModules();


      Kernel.Bind<IChromeDriverProvider>()
        .To<ChromeDriverProvider>()
        .InSingletonScope();
    }
  }

  public partial class App : TradingManagerApp
  {
  }
}
