using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CTKS_Chart.Binance;
using CTKS_Chart.Binance.Data;
using CTKS_Chart.Strategy;
using CTKS_Chart.Trading;
using CTKS_Chart.Views;
using CTKS_Chart.Views.Simulation;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using Logger;
using Microsoft.Expression.Interactivity.Core;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;
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

  public class SimulationTradingBot<TPosition, TStrategy> : TradingBotViewModel<TPosition, TStrategy>, ISimulationTradingBot
    where TPosition : Position, new()
    where TStrategy : BaseSimulationStrategy<TPosition>, new()
  {
    string results;
    public event EventHandler Finished;
    public bool SaveResults { get; set; }

    public SimulationTradingBot(
      TradingBot<TPosition, TStrategy> tradingBot,
      ILogger logger,
      IWindowManager windowManager,
      BinanceBroker binanceBroker,
      IViewModelsFactory viewModelsFactory) :
      base(tradingBot, logger, windowManager, binanceBroker, viewModelsFactory)
    {
    
      IsSimulation = true;

      TradingBot.Strategy.EnableRangeFilterStrategy = true;
      DrawChart = false;

      results = $"{TradingBot.Asset.Symbol}_simulation_results.txt";

      //DownloadCandles(TradingBot.Asset.Symbol, TimeSpan.FromMinutes(15));

      if (string.IsNullOrEmpty(DisplayName))
      {
        DisplayName = tradingBot.Asset.Symbol;
      }
    }

    public ObservableCollection<SimulationResult> SimulationResults { get; } = new ObservableCollection<SimulationResult>();

    private string displayName;
    public string DisplayName
    {
      get
      {
        return displayName;
      }
      set
      {
        if (value != displayName)
        {
          displayName = value;
          RaisePropertyChanged();
        }
      }
    }

    #region RunningTime

    private TimeSpan runningTime;

    public TimeSpan RunningTime
    {
      get { return runningTime; }
      set
      {
        if (value != runningTime)
        {
          runningTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region DataPath

    private string dataPath;

    public string DataPath
    {
      get { return dataPath; }
      set
      {
        if (value != dataPath)
        {
          dataPath = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Delay

    private int delay = 1;

    public int Delay
    {
      get { return delay; }
      set
      {
        if (value != delay)
        {
          delay = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public DateTime FromDate { get; set; } = new DateTime(2018, 9, 21);
    public TimeFrame DataTimeFrame { get; set; } = TimeFrame.Null;
    public double SplitTake { get; set; }

    #region LoadLayouts

    private List<Candle> cutCandles = new List<Candle>();
    protected override async Task LoadLayouts(CtksLayout mainLayout)
    {
      var mainCtks = new Ctks(mainLayout, mainLayout.TimeFrame, TradingBot.Asset);

      var dailyCandles = TradingViewHelper.ParseTradingView(TimeFrame.D1, $"Data\\Indicators\\{Asset.IndicatorDataPath}, 1D.csv", Asset.Symbol, saveData: true);
      var mainCandles = TradingViewHelper.ParseTradingView(DataTimeFrame, DataPath, Asset.Symbol, saveData: true);

      //fromDate = new DateTime(2021,8, 30);

      cutCandles = mainCandles.Where(x => x.CloseTime > FromDate).ToList();
      var candles = mainCandles.Where(x => x.CloseTime < FromDate).ToList();

      DrawingViewModel.ActualCandles = candles;

      var unixDiff = candles[1].UnixTime - candles[0].UnixTime;

      MainLayout.MaxValue = candles.Max(x => x.High.Value);
      MainLayout.MinValue = candles.Where(x => x.Low.Value > 0).Min(x => x.Low.Value);

      if (DrawingViewModel.MaxValue == 0)
      {
        DrawingViewModel.MaxValue = MainLayout.MaxValue;
        DrawingViewModel.MinValue = MainLayout.MinValue;
      }

      DrawingViewModel.MaxUnix = candles.Max(x => x.UnixTime) + (unixDiff * 20);
      DrawingViewModel.MinUnix = DrawingViewModel.MaxUnix - (unixDiff * 100);

      DrawingViewModel.LockChart = true;


      var rangeAdaFilterData = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart\\bin\\Debug\\netcoreapp3.1\\BINANCE ADAUSDT, 1D.csv";
      var rangeBtcFilterData = "D:\\Aplikacie\\Skusobne\\CTKS_Chart\\CTKS_Chart\\bin\\Debug\\netcoreapp3.1\\INDEX BTCUSD, 1D.csv";

      TradingBot.Strategy.InnerStrategies.Add(new RangeFilterStrategy<TPosition>(rangeAdaFilterData, Asset.Symbol, rangeBtcFilterData, TradingBot.Strategy));

      LoadSecondaryLayouts(FromDate);
      PreLoadCTks(FromDate);

      mainLayout.Ctks = mainCtks;
      //Layouts.Add(mainLayout);
      SelectedLayout = mainLayout;

      if (SplitTake != 0)
      {
        var take = (int)(mainCandles.Count / SplitTake);

        Simulate(cutCandles.Take(take).ToList(), InnerLayouts);
      }
      else
      {
        Simulate(cutCandles.ToList(), InnerLayouts);
      }
    }

    #endregion

    #region SimulateCandle

    private void SimulateCandle(List<CtksLayout> secondaryLayouts, Candle candle)
    {
      DrawingViewModel.ActualCandles.Add(candle);

      if (DrawChart)
      {
        VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          RenderLayout(secondaryLayouts, candle);
        });
      }
      else
      {
        RenderLayout(secondaryLayouts, candle);
      }

    }

    #endregion

    #region LoadSimulationResults

    public void LoadSimulationResults()
    {
      SimulationResults.Clear();

      if (File.Exists(results))
      {
        var content = File.ReadAllLines(results);

        foreach (var line in content)
        {
          var result = JsonSerializer.Deserialize<SimulationResult>(line);

          result.RunTime = TimeSpan.FromTicks(result.RunTimeTicks);
          SimulationResults.Add(result);
        }

        SimulationResults.LinqSortDescending(x => x.Date);
      }
    }

    #endregion

    #region Simulate

    CancellationTokenSource cts;
    IDisposable disposable;
    DateTime lastElapsed;

    private async void Simulate(List<Candle> cutCandles, List<CtksLayout> secondaryLayouts)
    {
      try
      {
        RunningTime = new TimeSpan();
        cts?.Cancel();
        cts = new CancellationTokenSource();

        disposable?.Dispose();

        lastElapsed = DateTime.Now;

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
          disposable = Observable.Interval(TimeSpan.FromSeconds(0.25))
         .ObserveOnDispatcher()
         .Subscribe((x) =>
         {

           TimeSpan diff = DateTime.Now - lastElapsed;

           RunningTime = RunningTime.Add(diff);

           lastElapsed = DateTime.Now;
         });

        if (DrawChart)
        {
          if (DrawingViewModel.MaxValue == 0)
          {
            DrawingViewModel.OnRestChart();
          }

          DrawingViewModel.OnRestChartX();
        }

        var result = new SimulationResult()
        {
          BotName = DisplayName
        };


        await Task.Run(async () =>
        {
          DateTime lastDay = DateTime.MinValue;
          for (int i = 0; i < cutCandles.Count; i++)
          {
            var actual = cutCandles[i];

            if (cts.IsCancellationRequested)
              return;

            SimulateCandle(secondaryLayouts, actual);

            await Task.Delay(!IsSimulation || DrawChart ? Delay : 0);

            if (lastDay < actual.OpenTime.Date)
            {
              lastDay = actual.OpenTime.Date;
              result.DataPoints.Add(new SimulationResultDataPoint()
              {
                Date = lastDay,
                TotalNative = TradingBot.Strategy.TotalNativeAsset,
                TotalValue = TradingBot.Strategy.TotalValue,
                TotalNativeValue = TradingBot.Strategy.TotalNativeAssetValue,
                Close = actual.Close.Value
              });
            }

            if (TradingBot.Strategy.TotalValue > result.MaxValue.Value)
            {
            //Ignore high after last market low
            if (actual.OpenTime < new DateTime(2023, 5, 1))
              {
                result.MaxValue.Value = TradingBot.Strategy.TotalValue;
                result.MaxValue.Candle = actual;
                result.LowAfterMaxValue = new SimulationResultValue(decimal.MaxValue);
              }
            }
            else if (TradingBot.Strategy.TotalValue < result.LowAfterMaxValue.Value)
            {
              result.LowAfterMaxValue.Value = TradingBot.Strategy.TotalValue;
              result.MaxValue.Candle = actual;
            }
          }

          disposable?.Dispose();

        }, cts.Token);

        if (!cts.IsCancellationRequested && SaveResults)
        {
          DrawingViewModel.OnRestChart();
          DrawingViewModel.Render();

          result.TotalValue = TradingBot.Strategy.TotalValue;
          result.TotalProfit = TradingBot.Strategy.TotalProfit;
          result.TotalNativeValue = TradingBot.Strategy.TotalNativeAssetValue;
          result.TotalNative = TradingBot.Strategy.TotalNativeAsset;
          result.RunTime = RunningTime;
          result.RunTimeTicks = RunningTime.Ticks;
          result.Date = DateTime.Now;

          var json = JsonSerializer.Serialize(result);

          if (File.Exists(results))
          {
            using (StreamWriter w = File.AppendText(results))
            {
              w.WriteLine(JsonSerializer.Serialize(result));
            }
          }
          else
          {
            File.WriteAllText(results, json);
          }

          SimulationResults.Add(result);
          SimulationResults.LinqSortDescending(x => x.Date);
        }

        Finished?.Invoke(this, null);
      }
      catch (Exception)
      {
      }
    }

    #endregion

    #region Stop

    public override void Stop()
    {
      cts?.Cancel();
      disposable?.Dispose();
      var simulationStrategy = new TStrategy();

      simulationStrategy.EnableRangeFilterStrategy = true;
      simulationStrategy.Asset = TradingBot.Asset;
      Layouts.Clear();
      InnerLayouts.Clear();

      TradingBot.Strategy = simulationStrategy;
    }

    #endregion

    #region ShowStatistics

    private ActionCommand<SimulationResult> showStatistics;

    public ICommand ShowStatistics
    {
      get
      {
        return showStatistics ??= new ActionCommand<SimulationResult>(OnShowStatistics);
      }
    }

    protected virtual void OnShowStatistics(SimulationResult simulationResult)
    {
      windowManager.ShowPrompt<SimulationStatisticsView>(new SimulationStatisticsViewModel(TradingBot.Asset)
      {
        DataPoints = simulationResult.DataPoints
      }, 1000, 1000);
    }

    #endregion

  }
}
