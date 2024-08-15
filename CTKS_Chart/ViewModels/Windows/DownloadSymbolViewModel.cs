using CTKS_Chart.Binance.Data;
using System;
using System.Windows.Input;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels.Prompt;

namespace CTKS_Chart.ViewModels
{
  public class DownloadSymbolViewModel : PromptViewModel
  {
    private readonly BinanceDataProvider binanceDataProvider;

    public DownloadSymbolViewModel(BinanceDataProvider binanceDataProvider)
    {
      this.binanceDataProvider = binanceDataProvider ?? throw new System.ArgumentNullException(nameof(binanceDataProvider));

      this.binanceDataProvider.onDownloadedData += BinanceDataProvider_onDownloadedData;

      Title = "Download symbol data";
    }

   
    #region Symbol

    private string symbol = "ADAUSDT";

    public string Symbol
    {
      get { return symbol; }
      set
      {
        if (value != symbol)
        {
          symbol = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Minutes

    private int minutes = 480;

    public int Minutes
    {
      get { return minutes; }
      set
      {
        if (value != minutes)
        {
          minutes = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DownloadedData

    private DownloadedData downloadedData;

    public DownloadedData DownloadedData
    {
      get { return downloadedData; }
      set
      {
        if (value != downloadedData)
        {
          downloadedData = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DownloadSymbol

    protected ActionCommand downloadSymbol;

    public ICommand DownloadSymbol
    {
      get
      {
        return downloadSymbol ??= new ActionCommand(OnDownloadSymbol);
      }
    }


    public void OnDownloadSymbol()
    {
      this.binanceDataProvider.DownloadSymbol(Symbol, TimeSpan.FromMinutes(Minutes));
    }

    #endregion

    private void BinanceDataProvider_onDownloadedData(object sender, DownloadedData e)
    {
      VSynchronizationContext.InvokeOnDispatcher(() => DownloadedData = e);
    }


  }
}
