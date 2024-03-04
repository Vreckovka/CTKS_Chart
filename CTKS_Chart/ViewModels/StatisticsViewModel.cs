using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using LiveCharts;
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

    private string[] labels;

    public string[] Labels
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

    #region Labels2

    private string[] labels2;

    public string[] Labels2
    {
      get { return labels2; }
      set
      {
        if (value != labels2)
        {
          labels2 = value;
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
      var dates = new List<string>();

      foreach (var line in lines)
      {
        var stat = JsonSerializer.Deserialize<State>(line);

        states.Add(stat);
        dates.Add(stat.Date.ToShortDateString());
      }

      

      TotalValue = new ChartValues<decimal>(states.Select(x => x.TotalValue));
      ActualValue = new ChartValues<decimal>(states.Where(x => x.ActualValue != null).Select(x => x.ActualValue.Value));
      ActualAutoValue = new ChartValues<decimal>(states.Where(x => x.ActualAutoValue != null).Select(x => x.ActualAutoValue.Value));


      TotalAutoProfit = new ChartValues<decimal>(states.Where(x => x.TotalAutoProfit != null).Select(x => x.TotalAutoProfit.Value));
      TotalManualProfit = new ChartValues<decimal>(states.Where(x => x.TotalManualProfit != null).Select(x => x.TotalManualProfit.Value));
      TotalProfit = new ChartValues<decimal>(states.Select(x => x.TotalProfit));

      var stats = states.Where(x => x.AthPrice > 0 && x.ClosePrice > 0).ToList();

      AthPrice = new ChartValues<decimal>(stats.Where(x => x.AthPrice > 0).Select(x => x.AthPrice));
      ClosePice = new ChartValues<decimal>(stats.Where(x => x.ClosePrice > 0).Select(x => x.ClosePrice.Value));

      ValueToNative = new ChartValues<decimal>(stats.Where(x => x.ValueToNative > 0).Select(x => x.ValueToNative));
      ValueToBTC = new ChartValues<decimal>(stats.Where(x => x.ValueToBTC > 0).Select(x => x.ValueToBTC));

      Labels = dates.ToArray();
      Labels2 = stats.Select(x => x.Date.ToShortDateString()).ToArray();

      ValueFormatter = value => value.ToString("N2");
      PriceFormatter = value => value.ToString($"N{strategy.Asset.PriceRound}");
      NativeFormatter = value => value.ToString($"N{strategy.Asset.NativeRound}");
      BTCFormatter = value => value.ToString($"N5");
    }
  }
}
