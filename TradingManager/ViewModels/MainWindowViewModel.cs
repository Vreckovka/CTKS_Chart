using ChromeDriverScrapper;
using CTKS_Chart.Binance;
using CTKS_Chart.Trading;
using Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TradingManager.Providers;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels;
using VCore.WPF.ViewModels.Navigation;

namespace TradingManager.ViewModels
{
  public static class RegionNames
  {
    public const string Content = "Content";
  }

  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    private readonly IViewModelsFactory viewModelsFactory;

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory) : base(viewModelsFactory)
    {
      Title = "Trading Manager";

      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    public override void Initialize()
    {
      base.Initialize();

      var filesManager = viewModelsFactory.Create<FilesManagerViewModel>();
      NavigationViewModel.Items.Add(new NavigationItem(filesManager));

      filesManager.IsActive = true;
    }
  }
}
