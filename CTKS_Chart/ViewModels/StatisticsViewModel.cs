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

    #region YFormatter

    private Func<double, string> yFormatter;

    public Func<double, string> YFormatter
    {
      get { return yFormatter; }
      set
      {
        if (value != yFormatter)
        {
          yFormatter = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    private void LoadStats()
    {
      var lines = File.ReadLines(@"state_data.txt");
      var states = new List<State>();
      var dates = new List<string>();

      foreach (var line in lines)
      {
        var stat = JsonSerializer.Deserialize<State>(line);

        states.Add(stat);
        dates.Add(stat.Date.ToShortDateString());
      }

      TotalValue = new ChartValues<decimal>(states.Select(x => x.TotalValue));
      TotalProfit = new ChartValues<decimal>(states.Select(x => x.TotalProfit));
      Labels = dates.ToArray();
      YFormatter = value => value.ToString("N2");
    }
  }
}
