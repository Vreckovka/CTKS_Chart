using System;
using System.Collections.Generic;
using System.IO;
using CTKS_Chart.ViewModels;
using VCore.Standard;
using VCore.Standard.Helpers;

namespace CTKS_Chart.Trading
{
  public class TradingBot : ViewModel
  {
    public TradingBot(Asset asset, Strategy.Strategy strategy)
    {
      Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
      Asset = asset;

      strategy.Asset = asset;

      StartingMinPrice = asset.StartLowPrice;
      StartingMaxPrice = asset.StartMaxPrice;
    }

    #region Strategy

    private Strategy.Strategy strategy;

    public Strategy.Strategy Strategy
    {
      get { return strategy; }
      set
      {
        if (value != strategy)
        {
          strategy = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public void LoadTimeFrames()
    {
      if (Asset?.TimeFrames != null)
      {
        var dictionary = new Dictionary<string, TimeFrame>();

        foreach (var timeframe in Asset.TimeFrames)
        {
          var tradingView_data = $"{Asset.DataPath}\\{Asset.DataSymbol}, {EnumHelper.Description(timeframe)}.csv";

          dictionary.Add(Path.Combine(Settings.DataPath, tradingView_data), timeframe);
        }

        TimeFrames = dictionary;
      }
    }


    public Asset Asset { get;  }
    public Dictionary<string, TimeFrame> TimeFrames { get; private set; }

    public decimal StartingMinPrice { get; }
    public decimal StartingMaxPrice { get; }

  }
}
