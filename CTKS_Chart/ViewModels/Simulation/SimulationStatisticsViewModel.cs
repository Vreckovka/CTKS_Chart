using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using CTKS_Chart.Trading;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using VCore.WPF.Prompts;

namespace CTKS_Chart.ViewModels
{
  public class SimulationStatisticsViewModel : BasePromptViewModel
  {
    private readonly Asset asset;

    public SimulationStatisticsViewModel(Asset asset, IList<SimulationResultDataPoint> dataPoints)
    {
      this.asset = asset ?? throw new ArgumentNullException(nameof(asset));

      DataPoints = dataPoints;
      Title = "Simulation result statistics";

    }

    #region Properties

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

    public SeriesCollection TotalValueSeries { get; set; }
    public Func<double, string> Formatter { get; set; }
    public double Base { get; set; }

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

    #endregion

    #region Methods

    public override void Initialize()
    {
      base.Initialize();

      LoadChart();
    }

    public IList<SimulationResultDataPoint> DataPoints { get; set; } = new List<SimulationResultDataPoint>();
    double pointD = 0;
    private void LoadChart()
    {
      Base = 10;

      var mapper = Mappers.Xy<decimal>()
         .Y(point => Math.Log((double)point, Base));

      TotalValueSeries = new SeriesCollection(mapper);

      TotalValueSeries.Add(new LineSeries() { Values = new ChartValues<decimal>(DataPoints.Where(x => x.TotalValue > 0).Select(x => x.TotalValue)), PointGeometrySize = 0 });

      Formatter = value => Math.Pow(Base, value).ToString("N");

      TotalNative = new ChartValues<decimal>(DataPoints.Select(x => x.TotalNative));
      TotalNativeValue = new ChartValues<decimal>(DataPoints.Select(x => x.TotalNativeValue));
      Price = new ChartValues<decimal>(DataPoints.Select(x => x.Close));

      Labels = new List<string[]>();

      Labels.Add(DataPoints.Select(x => x.Date.ToShortDateString()).ToArray());

      ValueFormatter = value => value.ToString("N2");
      NativeFormatter = value => value.ToString($"N{asset.NativeRound}");

      CreateHODL();
      RaisePropertyChanged(nameof(TotalValueSeries));
      RaisePropertyChanged(nameof(Formatter));
      RaisePropertyChanged(nameof(Base));
    }

    private void CreateHODL()
    {
      var firstPoint = DataPoints.Where(x => x.TotalValue > 0).First();
      var startingNative = firstPoint.TotalValue / firstPoint.Close;
      List<SimulationResult> results = new List<SimulationResult>();

      foreach (var candle in DataPoints)
      {
        var actualValue = startingNative * candle.Close;

        var newsd = new SimulationResult()
        {
          TotalValue = actualValue
        };

        results.Add(newsd);
      }

      TotalValueSeries.Add(new LineSeries() { Values = new ChartValues<decimal>(results.Where(x => x.TotalValue > 0).Select(x => x.TotalValue)), PointGeometrySize = 0 });
    }

    #endregion
  }
}
