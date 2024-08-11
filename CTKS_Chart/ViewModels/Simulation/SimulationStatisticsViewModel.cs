using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using CTKS_Chart.Trading;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using VCore.WPF.Prompts;

namespace CTKS_Chart.ViewModels
{
  public class SimulationStatisticsViewModel : BasePromptViewModel
  {
    private readonly Asset asset;
   

    public SimulationStatisticsViewModel(Asset asset)
    {
      this.asset = asset ?? throw new ArgumentNullException(nameof(asset));
      Title = "Simulation result statistics";
    }

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

    #region TotalNative

    private IChartValues totalNative;

    public IChartValues TotalNative
    {
      get { return totalNative; }
      set
      {
        if (value != totalNative)
        {
          totalNative = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalNativeValue

    private IChartValues totalNativeValue;

    public IChartValues TotalNativeValue
    {
      get { return totalNativeValue; }
      set
      {
        if (value != totalNativeValue)
        {
          totalNativeValue = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Price

    private IChartValues price;

    public IChartValues Price
    {
      get { return price; }
      set
      {
        if (value != price)
        {
          price = value;
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

    public override void Initialize()
    {
      base.Initialize();


      LoadChart();
    }

    public IList<SimulationResultDataPoint> DataPoints { get; set; } = new List<SimulationResultDataPoint>();

    private void LoadChart()
    {
      var mapper = Mappers.Xy<ObservablePoint>()
    .X(point => Math.Log(point.X, 10)) //a 10 base log scale in the X axis
    .Y(point => point.Y);

      DataPoints = DataPoints.Where((x, i) => i % 3 == 0).ToList();

      TotalValue = new ChartValues<decimal>(DataPoints.Select(x => x.TotalValue));
      TotalNative = new ChartValues<decimal>(DataPoints.Select(x => x.TotalNative));
      TotalNativeValue = new ChartValues<decimal>(DataPoints.Select(x => x.TotalNativeValue));
      Price = new ChartValues<decimal>(DataPoints.Select(x => x.Close));

      Labels = new List<string[]>();

      Labels.Add(DataPoints.Select(x => x.Date.ToShortDateString()).ToArray());

      ValueFormatter = value => value.ToString("N2");
      NativeFormatter = value => value.ToString($"N{asset.NativeRound}");
    }
  }
}
