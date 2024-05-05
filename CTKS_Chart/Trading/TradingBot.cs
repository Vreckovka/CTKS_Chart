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
      Strategy = strategy;
      Asset = asset;

      if (strategy != null)
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


    public Asset Asset { get; }
    public Dictionary<string, TimeFrame> TimeFrames { get; private set; } = new Dictionary<string, TimeFrame>();
    public Dictionary<string, TimeFrame> IndicatorTimeFrames { get; private set; } = new Dictionary<string, TimeFrame>();

    public decimal StartingMinPrice { get; }
    public decimal StartingMaxPrice { get; }


    #region LoadTimeFrames

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

    #endregion

    #region LoadIndicators

    public void LoadIndicators()
    {
      var indicator_dictionary = new Dictionary<string, TimeFrame>();
      var pattern = $"*{Asset.IndicatorDataPath}*.csv";

      var dir = Path.Combine(Settings.DataPath, "Indicator");

      if(Directory.Exists(dir))
      {
        var files = Directory.GetFiles(dir, pattern);

        foreach (var file in files)
        {
          TimeFrame timeFrame = TimeFrame.D1;

          if (Path.GetFileName(file).Contains("240"))
          {
            timeFrame = TimeFrame.H4;
          }
          else if (Path.GetFileName(file).Contains("1D"))
          {
            timeFrame = TimeFrame.D1;
          }

          indicator_dictionary.Add(file, timeFrame);
        }

        IndicatorTimeFrames = indicator_dictionary;
      }
     
    }

    #endregion

  }
}
