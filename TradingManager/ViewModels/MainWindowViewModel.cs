using ChromeDriverScrapper;
using CTKS_Chart.Trading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingManager.Providers;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.ViewModels;

namespace TradingManager.ViewModels
{
  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    private readonly IChromeDriverProvider chromeDriverProvider;

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory, IChromeDriverProvider chromeDriverProvider) : base(viewModelsFactory)
    {
      this.chromeDriverProvider = chromeDriverProvider ?? throw new ArgumentNullException(nameof(chromeDriverProvider));
    }

    public override void Initialize()
    {
      base.Initialize();

      CheckData();
    }

    public void CheckData()
    {
      var list = new List<string>()
      {
        "\\\\desktop-6s0dghi\\bots\\ada\\Data\\Chart data"
      };

      foreach (var path in list)
      {
        var files = Directory.GetFiles(path);

        foreach (var file in files)
        {
          var candles = TradingViewHelper.ParseTradingView(file);

          if(IsOutDated(candles))
          {
            Debug.WriteLine(file);
          }
        }
      }

      var tradingView = new TradingViewDataProvider(chromeDriverProvider);

      tradingView.DownloadTimeframe(new TradingViewSymbol() { Provider = "BINANCE", Symbol = "BTCUSDT" }, "1W");
    }

  
  }
}
