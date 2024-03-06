using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using VCore.Standard.Helpers;
using VCore.WPF.ViewModels.Prompt;

namespace CTKS_Chart.ViewModels
{
  public class StatisticsViewModel : PromptViewModel
  {
    private readonly Strategy.Strategy strategy;

    public StatisticsViewModel(Strategy.Strategy strategy)
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

    #region IntraDayProfits

    private IChartValues intraDayProfits;

    public IChartValues IntraDayProfits
    {
      get { return intraDayProfits; }
      set
      {
        if (value != intraDayProfits)
        {
          intraDayProfits = value;
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


      TotalAutoProfit = new ChartValues<decimal>(sanitizedStates.Where(x => x.TotalAutoProfit != null).Select(x => x.TotalAutoProfit.Value));
      TotalManualProfit = new ChartValues<decimal>(sanitizedStates.Where(x => x.TotalManualProfit != null).Select(x => x.TotalManualProfit.Value));
      TotalProfit = new ChartValues<decimal>(sanitizedStates.Where(x => x.TotalProfit != null).Select(x => x.TotalProfit.Value));

      var states1Date = sanitizedStates.First(x => x.AthPrice > 0 && x.ClosePrice > 0).Date;
      var states2Date = sanitizedStates.First(x => x.TotalManualProfit != null).Date;
      var states3Date = sanitizedStates.First(x => x.ValueToNative != null).Date;

      AthPrice = new ChartValues<decimal>(sanitizedStates.Where(x => x.AthPrice != null && x.ClosePrice != null).Select(x => x.AthPrice.Value));
      ClosePice = new ChartValues<decimal>(sanitizedStates.Where(x => x.ClosePrice != null).Select(x => x.ClosePrice.Value));

      ValueToNative = new ChartValues<decimal>(sanitizedStates.Where(x => x.ValueToNative != null).Select(x => x.ValueToNative.Value));
      ValueToBTC = new ChartValues<decimal>(sanitizedStates.Where(x => x.ValueToBTC != null).Select(x => x.ValueToBTC.Value));

      var nonItraDaytakenProfits = this.strategy
        .ClosedBuyPositions
        .Where(x => x.FilledDate != null && x.CreatedDate != null && x.FilledDate.Value.Date != x.CreatedDate.Value.Date)
        .GroupBy(x => x.FilledDate.Value.Date)
        .Select(x => new Tuple<DateTime, decimal>(x.Key, x.Sum(y => y.TotalProfit))).ToList();

      var intraDayAutoProfits = this.strategy
        .ClosedBuyPositions
        .Where(x => x.IsAutomatic)
        .Where(x => x.FilledDate != null && x.CreatedDate != null && x.FilledDate.Value.Date == x.CreatedDate.Value.Date)
        .GroupBy(x => x.FilledDate.Value.Date)
        .Select(x => new Tuple<DateTime, decimal>(x.Key, x.Sum(y => y.TotalProfit)))
        .ToList();

    

      var intraDayManualProfits = this.strategy
        .ClosedBuyPositions
        .Where(x => !x.IsAutomatic)
        .Where(x => x.FilledDate != null && x.CreatedDate != null && x.FilledDate.Value.Date == x.CreatedDate.Value.Date)
        .GroupBy(x => x.FilledDate.Value.Date)
        .Select(x => new Tuple<DateTime, decimal>(x.Key, x.Sum(y => y.TotalProfit)))
        .ToList();

   

      IntraDayProfits = new ChartValues<decimal>(SanitziedProfits(dates, nonItraDaytakenProfits));
      IntraDayManualProfits = new ChartValues<decimal>(SanitziedProfits(dates, intraDayManualProfits));
      IntraDayAutoProfits = new ChartValues<decimal>(SanitziedProfits(dates, intraDayAutoProfits));

      Labels = new List<string[]>();

      Labels.Add(dates.Select(x => x.ToShortDateString()).ToArray());
      Labels.Add(dates.Where( x => x >= states1Date).Select(x => x.Date.ToShortDateString()).ToArray());
      Labels.Add(dates.Where(x => x >= states2Date).Select(x => x.Date.ToShortDateString()).ToArray());
      Labels.Add(dates.Where(x => x >= states3Date).Select(x => x.Date.ToShortDateString()).ToArray());
      Labels.Add(dates.Select(x => x.ToShortDateString()).ToArray());

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
