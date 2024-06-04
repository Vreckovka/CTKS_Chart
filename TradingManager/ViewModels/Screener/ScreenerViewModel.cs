using Binance.Net.Objects.Models.Spot;
using CTKS_Chart.Binance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TradingManager.Views;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF.ItemsCollections;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace TradingManager.ViewModels.Screener
{
  public class SymbolViewModel : SelectableViewModel<BinanceSymbol>
  {
    public SymbolViewModel(BinanceSymbol model, string symbol) : base(model)
    {
      Symbol = symbol;
    }

    public string Symbol { get; set; }
  }



  public class ScreenerViewModel : RegionViewModel<ScreenerView>
  {
    private readonly BinanceBroker binanceBroker;
    private readonly IViewModelsFactory viewModelsFatory;

    #region Constructors

    public ScreenerViewModel(
      IRegionProvider regionProvider, 
      BinanceBroker binanceBroker,
      IViewModelsFactory viewModelsFatory) : base(regionProvider)
    {
      this.binanceBroker = binanceBroker;
      this.viewModelsFatory = viewModelsFatory;

    }

    #endregion Constructors

    public override string RegionName { get; protected set; } = RegionNames.Content;

    public override string Header => "Screener"; 

    #region Symbols

    private ItemsViewModel<SymbolViewModel> symbols = new ItemsViewModel<SymbolViewModel>();

    public ItemsViewModel<SymbolViewModel> Symbols
    {
      get { return symbols; }
      set
      {
        if (value != symbols)
        {
          symbols = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region OnActivation

    public override async void OnActivation(bool firstActivation)
    {
      base.OnActivation(firstActivation);

      if (firstActivation)
      {
        Symbols.AddRange((await binanceBroker.GetSymbols())
          .Symbols
          .Where(x => x.QuoteAsset == "USDT")
          .Select(x => viewModelsFatory.Create<SymbolViewModel>(x,x.Name)));
      }
    }

    #endregion

  }
}
