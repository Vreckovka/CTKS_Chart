using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CTKS_Chart.ViewModels;
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

  public partial class App : Application
  {
    private Logger.Logger logger;
    public App()
    {
      logger = new Logger.Logger(new ConsoleLogger(), new FileLoggerContainer());
    }

    protected override void OnActivated(EventArgs e)
    {
      base.OnActivated(e);


    }



    #region SetupExceptionHandling

    private void SetupExceptionHandling()
    {

      //#if !DEBUG
      AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        LogUnhandledException((Exception) e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

      DispatcherUnhandledException += (s, e) =>
      {
        LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
        e.Handled = true;
      };
      //#endif
      TaskScheduler.UnobservedTaskException += (s, e) =>
      {
        LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
      };
    }

    #endregion


    private async void LogUnhandledException(Exception exception, string source)
    {
      string message = $"Unhandled exception ({source})";

      try
      {
        AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();

        message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
      }
      catch (Exception ex)
      {
        logger.Log(ex);
      }
      finally
      {
        logger.Log(exception);

      }
    }
  }
}
