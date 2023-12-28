using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Binance.Net.Enums;
using VCore.Standard;

namespace CTKS_Chart
{
  public class TradingBot : ViewModel
  {
    public TradingBot(Asset asset, Dictionary<string, TimeFrame> timeFrames, Strategy strategy)
    {
      Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
      Asset = asset;
      TimeFrames = timeFrames;
      StartingMinPrice = asset.StartLowPrice;
      StartingMaxPrice = asset.StartMaxPrice;

    }



    #region Strategy

    private Strategy strategy;

    public Strategy Strategy
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

    public Asset Asset { get;  }
    public Dictionary<string, TimeFrame> TimeFrames { get; }

    public decimal StartingMinPrice { get; }
    public decimal StartingMaxPrice { get; }

  }
}
