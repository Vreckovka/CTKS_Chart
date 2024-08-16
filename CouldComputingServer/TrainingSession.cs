using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Helpers;

namespace CouldComputingServer
{
  public class TrainingSession : ViewModel
  {
    public TrainingSession()
    {

    }

    public TrainingSession(string name)
    {
      Name = name;

      SymbolsToTest.ItemUpdated.Where(x => x.EventArgs.PropertyName == nameof(SymbolToTest.IsEnabled))
        .ObserveOnDispatcher()
        .Subscribe(x =>
        {
          var symbol = (SymbolToTest)x.Sender;
          var index = SymbolsToTest.IndexOf(symbol);

          var visibility = symbol.IsEnabled ? Visibility.Visible : Visibility.Collapsed;

          ((LineSeries)AverageData[index]).Visibility = visibility;
          ((LineSeries)TotalValueData[index]).Visibility = visibility;
          ((LineSeries)BestData[index]).Visibility = visibility;
          ((LineSeries)DrawdawnData[index]).Visibility = visibility;
          ((LineSeries)FitnessData[index]).Visibility = visibility;
          ((LineSeries)NumberOfTradesData[index]).Visibility = visibility;
        });
    }

    [JsonIgnore]
    public string Name { get; set; }

    public RxObservableCollection<SymbolToTest> SymbolsToTest { get; set; } = new RxObservableCollection<SymbolToTest>();

    public Dictionary<string, List<decimal>> BestFitness { get; set; } = new Dictionary<string, List<decimal>>();
    public Dictionary<string, List<decimal>> OriginalFitness { get; set; } = new Dictionary<string, List<decimal>>();
    public Dictionary<string, List<decimal>> AverageFitness { get; set; } = new Dictionary<string, List<decimal>>();
    public Dictionary<string, List<decimal>> TotalValue { get; set; } = new Dictionary<string, List<decimal>>();
    public Dictionary<string, List<decimal>> Drawdawn { get; set; } = new Dictionary<string, List<decimal>>();
    public Dictionary<string, List<decimal>> NumberOfTrades { get; set; } = new Dictionary<string, List<decimal>>();

    [JsonIgnore]
    public SeriesCollection AverageData { get; set; } = new SeriesCollection();
    [JsonIgnore]
    public SeriesCollection TotalValueData { get; set; } = new SeriesCollection();
    [JsonIgnore]
    public SeriesCollection BestData { get; set; } = new SeriesCollection();
    [JsonIgnore]
    public SeriesCollection DrawdawnData { get; set; } = new SeriesCollection();
    [JsonIgnore]
    public SeriesCollection FitnessData { get; set; } = new SeriesCollection();
    [JsonIgnore]
    public SeriesCollection NumberOfTradesData { get; set; } = new SeriesCollection();

    #region Labels

    private List<string> labels = new List<string>();

    public List<string> Labels
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

    public void AddValue(string symbol, Statistic statistics, decimal value)
    {
      var symbolIndex = SymbolsToTest.IndexOf(x => x.Name == symbol);

      if (symbolIndex != null)
      {
        switch (statistics)
        {
          case Statistic.BestFitness:
            BestData[symbolIndex.Value].Values.Add(AddValue(BestFitness, symbol, value));
            break;
          case Statistic.OriginalFitness:
            FitnessData[symbolIndex.Value].Values.Add(AddValue(OriginalFitness, symbol, value));
            break;
          case Statistic.AverageFitness:
            AverageData[symbolIndex.Value].Values.Add(AddValue(AverageFitness, symbol, value));
            break;
          case Statistic.TotalValue:
            TotalValueData[symbolIndex.Value].Values.Add(AddValue(TotalValue, symbol, value));
            break;
          case Statistic.Drawdawn:
            DrawdawnData[symbolIndex.Value].Values.Add(AddValue(Drawdawn, symbol, value));
            break;
          case Statistic.NumberOfTrades:
            NumberOfTradesData[symbolIndex.Value].Values.Add(AddValue(NumberOfTrades, symbol, value));
            break;
        }
      }
    }

    private decimal AddValue(Dictionary<string, List<decimal>> values, string key, decimal value)
    {
      if (values.ContainsKey(key))
      {
        values[key].Add(value);
      }
      else
      {
        values.Add(key, new List<decimal>() { value });
      }

      return Math.Round(values[key].TakeLast(20).Average(), 2);
    }

    public void AddLabel(string label)
    {
      Labels.Add(label);
      RaisePropertyChanged(nameof(Labels));
    }


    #region CreateCharts

    private void CreateCharts(SeriesCollection series)
    {
      for (int i = 0; i < SymbolsToTest.Count; i++)
      {
        var newSeries = new LineSeries()
        {
          Values = new ChartValues<decimal>(),
          PointGeometrySize = 0
        };


        newSeries.Fill = Brushes.Transparent;
        newSeries.Title = SymbolsToTest[i].Name;
        newSeries.PointForeground = Brushes.Transparent;

        series.Add(newSeries);
      }
    }

    #endregion


    public void CreateSymbolsToTest(params string[] symbols)
    {
      SymbolsToTest.Clear();

      foreach (var symbol in symbols)
      {
        SymbolsToTest.Add(new SymbolToTest() { Name = symbol });
      }


      CreateCharts(AverageData);
      CreateCharts(TotalValueData);
      CreateCharts(BestData);
      CreateCharts(DrawdawnData);
      CreateCharts(FitnessData);
      CreateCharts(NumberOfTradesData);

    }

    public void Load(string path)
    {
      var session = JsonSerializer.Deserialize<TrainingSession>(path);

      for (int i = 0; i < SymbolsToTest.Count; i++)
      {
        var symbol = SymbolsToTest[i];

        session.AverageFitness[symbol.Name].ForEach(x => AddValue(symbol.Name, Statistic.AverageFitness, x));
        session.BestFitness[symbol.Name].ForEach(x => AddValue(symbol.Name, Statistic.BestFitness, x));
        session.Drawdawn[symbol.Name].ForEach(x => AddValue(symbol.Name, Statistic.Drawdawn, x));
        session.OriginalFitness[symbol.Name].ForEach(x => AddValue(symbol.Name, Statistic.OriginalFitness, x));
        session.NumberOfTrades[symbol.Name].ForEach(x => AddValue(symbol.Name, Statistic.NumberOfTrades, x));
        session.TotalValue[symbol.Name].ForEach(x => AddValue(symbol.Name, Statistic.TotalValue, x));
      }
    }
  }

}
