using ChromeDriverScrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingManager.Providers
{
  public class TradingViewSymbol
  {
    public string Symbol { get; set; }
    public string Provider { get; set; }

    public override string ToString()
    {
      return $"{Provider}:{Symbol}";
    }
  }

  public class TradingViewDataProvider
  {
    private readonly IChromeDriverProvider chromeDriverProvider;

    public TradingViewDataProvider(IChromeDriverProvider chromeDriverProvider)
    {
      this.chromeDriverProvider = chromeDriverProvider ?? throw new ArgumentNullException(nameof(chromeDriverProvider));
    }


    public Task<string> DownloadTimeframe(TradingViewSymbol symbol, string timeFrame)
    {
      return Task.Run(async () =>
      {
        bool sucess = false;

        var fileName = symbol.ToString().Replace(":", "_");

        var chromePath = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        chromePath += @"\Local\Google\Chrome\User Data";

        var downloadDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Chart data");
        var actualFileName = Path.Combine(downloadDir, $"{fileName}, {timeFrame}.csv");
        var newFileName = Path.Combine(downloadDir, $"{fileName}, {timeFrame} (1).csv");

        this.chromeDriverProvider.Initialize(options: new List<string>()
        {
          $"--user-data-dir={chromePath}",
           "--disable-gpu",
           "--no-sandbox",
           "--disable-infobars",
           "--disable-extensions",
           "--log-level=3",
           "--disable-cookie-encryption=false",
           "--block-new-web-contents",
           "--enable-precise-memory-info",
           "--ignore-certificate-errors",
        }, downloadDirectory: downloadDir);


        this.chromeDriverProvider.ChromeDriver?.Manage()?.Window?.Minimize();
        this.chromeDriverProvider.SafeNavigate($"https://www.tradingview.com/chart/p9TLSOTV/?symbol={symbol.Provider}%3A{symbol.Symbol}&interval={timeFrame}", out var red);


        while (!sucess)
        {
          try
          {
            DownloadFile();

            await Task.Delay(1000);

            if (File.Exists(newFileName) || File.Exists(actualFileName))
            {
              sucess = true;
            }

            if (File.Exists(newFileName))
            {
              File.Delete(actualFileName);
              File.Move(newFileName, actualFileName);

            }

            if (sucess)
            {
              return actualFileName;
            }
            else
              await Task.Delay(5000);
          }
          catch (Exception ex)
          {
            if (!sucess)
              await Task.Delay(5000);
          }
        }

        return null;
      });
    }

    private async Task<bool> SelectSymbol(TradingViewSymbol symbol)
    {
      var enterEvent = "new KeyboardEvent('keydown', {code: 'Enter',key: 'Enter',charCode: 13,keyCode: 13,view: window,bubbles: true})";

      this.chromeDriverProvider.ExecuteScriptVoid("document.getElementById(\"header-toolbar-symbol-search\").click()", 1);
      await Task.Delay(1000);
      this.chromeDriverProvider.ExecuteScriptVoid($"document.querySelectorAll('[data-role=\"search\"]')[0].value = \"{symbol}\" ", 1);
      await Task.Delay(1000);
      this.chromeDriverProvider.ExecuteScriptVoid($"document.querySelectorAll('[data-role=\"search\"]')[0].dispatchEvent({enterEvent})", 1);
      await Task.Delay(1000);

      var selectedSymbol = this.chromeDriverProvider.ExecuteScript("return document.getElementById(\"header-toolbar-symbol-search\").innerText", 1).ToString();

      return symbol.Symbol == selectedSymbol;
    }

    private async void SelectTimeframe(string timeframe)
    {
      this.chromeDriverProvider.ExecuteScriptVoid("document.querySelectorAll('[data-tooltip=\"Time Interval\"]')[0].click()", 1);

      await Task.Delay(1000);

      this.chromeDriverProvider.ExecuteScriptVoid($"document.querySelectorAll('[data-value=\"{timeframe}\"]')[0].click()", 1);

      await Task.Delay(1000);

      this.chromeDriverProvider.ExecuteScriptVoid("document.querySelectorAll('[data-tooltip=\"Time Interval\"]')[0].click()", 1);


    }

    private void DownloadFile()
    {
      this.chromeDriverProvider.ExecuteScriptVoid("document.querySelectorAll('[data-name=\"save-load-menu\"]')[0].click();", 1);
      this.chromeDriverProvider.ExecuteScriptVoid("document.querySelectorAll('[data-role=\"menuitem\"]')[5].click();", 1);
      this.chromeDriverProvider.ExecuteScriptVoid("document.querySelectorAll('[data-name=\"submit-button\"]')[0].click();", 1);
    }
  }
}
