using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CTKS_Chart.Strategy;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using VCore.Standard.Helpers;
using VCore.WPF.ViewModels.Prompt;

namespace CTKS_Chart.ViewModels
{
  public class StatisticsViewModel<TPosition> : PromptViewModel where TPosition : Position, new() 
  {
    private readonly BaseStrategy<TPosition> strategy;

    public StatisticsViewModel(BaseStrategy<TPosition> strategy)
    {
      this.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

      LoadStats();
    }

    public override string Title
    {
      get;
      set;
    } = "Statistics";

    #region TotalValue

    private IChartValues totalValue;

    public IChartValues TotalValue
    {
      get { return totalValue; }
      set
      {
        if (value != totalValue)
        {
          totalValue = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalValue

    private IChartValues athPrice;

    public IChartValues AthPrice
    {
      get { return athPrice; }
      set
      {
        if (value != athPrice)
        {
          athPrice = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ClosePice

    private IChartValues closePice;

    public IChartValues ClosePice
    {
      get { return closePice; }
      set
      {
        if (value != closePice)
        {
          closePice = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalProfit

    private IChartValues totalProfit;

    public IChartValues TotalProfit
    {
      get { return totalProfit; }
      set
      {
        if (value != totalProfit)
        {
          totalProfit = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualAutoValue

    private IChartValues actualAutoValue;

    public IChartValues ActualAutoValue
    {
      get { return actualAutoValue; }
      set
      {
        if (value != actualAutoValue)
        {
          actualAutoValue = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualValue

    private IChartValues actualValue;

    public IChartValues ActualValue
    {
      get { return actualValue; }
      set
      {
        if (value != actualValue)
        {
          actualValue = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalManualProfit

    private IChartValues totalManualProfit;

    public IChartValues TotalManualProfit
    {
      get { return totalManualProfit; }
      set
      {
        if (value != totalManualProfit)
        {
          totalManualProfit = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DailyProfits

    private IChartValues dailyProfits;

    public IChartValues DailyProfits
    {
      get { return dailyProfits; }
      set
      {
        if (value != dailyProfits)
        {
          dailyProfits = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IntraDayAutoProfits

    private IChartValues intraDayAutoProfits;

    public IChartValues IntraDayAutoProfits
    {
      get { return intraDayAutoProfits; }
      set
      {
        if (value != intraDayAutoProfits)
        {
          intraDayAutoProfits = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IntraDayManualProfits

    private IChartValues intraDayManualProfits;

    public IChartValues IntraDayManualProfits
    {
      get { return intraDayManualProfits; }
      set
      {
        if (value != intraDayManualProfits)
        {
          intraDayManualProfits = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalAutoProfit

    private IChartValues totalAutoProfit;

    public IChartValues TotalAutoProfit
    {
      get { return totalAutoProfit; }
      set
      {
        if (value != totalAutoProfit)
        {
          totalAutoProfit = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DailyPnl

    private IChartValues dailyPnl;

    public IChartValues DailyPnl
    {
      get { return dailyPnl; }
      set
      {
        if (value != dailyPnl)
        {
          dailyPnl = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ValueToNative

    private IChartValues valueToNative;

    public IChartValues ValueToNative
    {
      get { return valueToNative; }
      set
      {
        if (value != valueToNative)
        {
          valueToNative = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ValueToBTC

    private IChartValues valueToBTC;

    public IChartValues ValueToBTC
    {
      get { return valueToBTC; }
      set
      {
        if (value != valueToBTC)
        {
          valueToBTC = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Labels

    private IList<string[]> labels;

    public IList<string[]> Labels
    {
      get { return labels; }
      set
      {
        if (value != labels)
        {
          labels = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ValueFormatter

    private Func<double, string> valueFormatter;

    public Func<double, string> ValueFormatter
    {
      get { return valueFormatter; }
      set
      {
        if (value != valueFormatter)
        {
          valueFormatter = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region NativeFormatter

    private Func<double, string> nativeFormatter;

    public Func<double, string> NativeFormatter
    {
      get { return nativeFormatter; }
      set
      {
        if (value != nativeFormatter)
        {
          nativeFormatter = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region BTCFormatter

    private Func<double, string> bTCFormatter;

    public Func<double, string> BTCFormatter
    {
      get { return bTCFormatter; }
      set
      {
        if (value != bTCFormatter)
        {
          bTCFormatter = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region PriceFormatter

    private Func<double, string> priceFormatter;

    public Func<double, string> PriceFormatter
    {
      get { return priceFormatter; }
      set
      {
        if (value != priceFormatter)
        {
          priceFormatter = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    private void LoadStats()
    {
      var lines = File.ReadLines(TradingBotViewModel.stateDataPath);
      var states = new List<State>();
      var dates = new List<DateTime>();

      foreach (var line in lines)
      {
        var stat = JsonSerializer.Deserialize<State>(line);

        states.Add(stat);
        dates.Add(stat.Date);
      }

      var sanitizedStates = SanitzedStates(states);

      TotalValue = new ChartValues<decimal>(sanitizedStates.Where(x => x.TotalValue != null).Select(x => x.TotalValue.Value));
      ActualValue = new ChartValues<decimal>(sanitizedStates.Where(x => x.ActualValue != null).Select(x => x.ActualValue.Value));
      ActualAutoValue = new ChartValues<decimal>(sanitizedStates.Where(x => x.ActualAutoValue != null).Select(x => x.ActualAutoValue.Value));

      var profits = sanitizedStates.Where(x => x.TotalAutoProfit != null && x.TotalManualProfit != null).ToList();

      TotalAutoProfit = new ChartValues<decimal>(profits.Select(x => x.TotalAutoProfit.Value));
      TotalManualProfit = new ChartValues<decimal>(profits.Select(x => x.TotalManualProfit.Value));
      
      
      TotalProfit = new ChartValues<decimal>(sanitizedStates.Where(x => x.TotalProfit != null).Select(x => x.TotalProfit.Value));


      var athPrices = sanitizedStates.Where(x => x.AthPrice != null && x.ClosePrice != null).ToList();
      var btcValues = sanitizedStates.Where(x => x.ValueToNative != null && x.ValueToBTC != null).ToList();


      AthPrice = new ChartValues<decimal>(athPrices.Select(x => x.AthPrice.Value));
      ClosePice = new ChartValues<decimal>(athPrices.Select(x => x.ClosePrice.Value));

      ValueToNative = new ChartValues<decimal>(btcValues.Select(x => x.ValueToNative.Value));
      ValueToBTC = new ChartValues<decimal>(btcValues.Select(x => x.ValueToBTC.Value));


      var intraDayAutoProfits = this.strategy
        .ClosedBuyPositions
        .Where(x => x.IsAutomatic)
        .Where(x => x.CompletedDate != null && x.FilledDate != null && x.CompletedDate.Value.Date == x.FilledDate.Value.Date)
        .GroupBy(x => x.FilledDate.Value.Date)
        .Select(x => new Tuple<DateTime, decimal>(x.Key, x.Sum(y => y.TotalProfit)))
        .ToList();

      var intraDayManualProfits = this.strategy
        .ClosedBuyPositions
        .Where(x => !x.IsAutomatic)
        .Where(x => x.CompletedDate != null && x.FilledDate != null && x.CompletedDate.Value.Date == x.FilledDate.Value.Date)
        .GroupBy(x => x.FilledDate.Value.Date)
        .Select(x => new Tuple<DateTime, decimal>(x.Key, x.Sum(y => y.TotalProfit)))
        .ToList();

      var dailyProfits = this.strategy
        .ClosedSellPositions
        .Where(x => x.FilledDate != null && x.OpositPositions.Count > 0)
        .Where(x => x.OpositPositions[0].CompletedDate != null && x.OpositPositions[0].FilledDate != null)
        .Where(x => x.OpositPositions[0].CompletedDate.Value.Date != x.OpositPositions[0].FilledDate.Value.Date)
        .GroupBy(x => x.FilledDate.Value.Date)
        .Select(x => new Tuple<DateTime, decimal>(x.Key, x.Sum(y => y.Profit)))
        .ToList();



      DailyProfits = new ChartValues<decimal>(SanitziedProfits(dates, dailyProfits));
      IntraDayManualProfits = new ChartValues<decimal>(SanitziedProfits(dates, intraDayManualProfits));
      IntraDayAutoProfits = new ChartValues<decimal>(SanitziedProfits(dates, intraDayAutoProfits));

      Labels = new List<string[]>();

      Labels.Add(dates.Select(x => x.ToShortDateString()).ToArray());
      Labels.Add(athPrices.Select(x => x.Date.ToShortDateString()).ToArray());
      Labels.Add(profits.Select(x => x.Date.ToShortDateString()).ToArray());
      Labels.Add(btcValues.Select(x => x.Date.ToShortDateString()).ToArray());

      ValueFormatter = value => value.ToString("N2");
      PriceFormatter = value => value.ToString($"N{strategy.Asset.PriceRound}");
      NativeFormatter = value => value.ToString($"N{strategy.Asset.NativeRound}");
      BTCFormatter = value => value.ToString($"N5");
    }

    private IEnumerable<decimal> SanitziedProfits(IEnumerable<DateTime> dates, List<Tuple<DateTime,decimal>> keyValuePair)
    {
      var result = new List<decimal>();

      foreach(var date in dates)
      {
        var item = keyValuePair.SingleOrDefault(x => x.Item1 == date);

        result.Add(item?.Item2 ?? 0);
      }

      return result;
    }

    private List<State> SanitzedStates(List<State> states)
    {
      var newStates = new List<State>();

      for (int i = 0; i < states.Count; i++)
      {
        var state = states[i];
        var newState = state.DeepClone();

        if(state.AthPrice == 0)
        {
          var existing = states.LastOrDefault(x => x.Date < state.Date && x.AthPrice != 0);

          if(existing != null)
          {
            newState.AthPrice = existing.AthPrice;
          }
        }

        newStates.Add(newState);
      }

      return newStates;
    }
  }
}
