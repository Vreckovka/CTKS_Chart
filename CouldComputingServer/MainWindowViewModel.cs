using CloudComputing.Domains;
using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.ViewModels;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Logger;
using SharpNeat.Genomes.Neat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels;
using VNeuralNetwork;

namespace CouldComputingServer
{
  public class CloudClient
  {
    public TcpClient Client { get; set; }
    public Dictionary<uint, bool> SentBuyGenomes { get; set; } = new Dictionary<uint, bool>();
    public Dictionary<uint, bool> SentSellGenomes { get; set; } = new Dictionary<uint, bool>();

    public bool Done { get; set; }
  }

  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    private TcpListener _listener;
    private Thread _listenerThread;
    string session;

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory, ILogger logger) : base(viewModelsFactory)
    {
      session = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");

      BuyBotManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Buy);
      SellBotManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Sell);

      CurrentSymbol = symbolsToTest[0];
      Logger = logger;
      Title = "Cloud Computing SERVER";
    }

    #region Properties

    public NEATManager<AIBot> BuyBotManager { get; set; }
    public NEATManager<AIBot> SellBotManager { get; set; }

    #region AgentCount

#if DEBUG
    private int agentCount = 10;
#endif

#if RELEASE
    private int agentCount = 120;
#endif

    public int AgentCount
    {
      get { return agentCount; }
      set
      {
        if (value != agentCount)
        {
          agentCount = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public ObservableCollection<CloudClient> Clients { get; set; } = new ObservableCollection<CloudClient>();

    #region ToStart

    private int toStart;

    public int ToStart
    {
      get { return toStart; }
      set
      {
        if (value != toStart)
        {
          toStart = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region InProgress

    private double inProgress;

    public double InProgress
    {
      get { return inProgress; }
      set
      {
        if (value != inProgress)
        {
          inProgress = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region FinishedCount

    private double finishedCount;

    public double FinishedCount
    {
      get { return finishedCount; }
      set
      {
        if (value != finishedCount)
        {
          finishedCount = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    public SeriesCollection AverageData { get; set; } = new SeriesCollection();
    public SeriesCollection FullData { get; set; } = new SeriesCollection();
    public SeriesCollection BestData { get; set; } = new SeriesCollection();
    public SeriesCollection DrawdawnData { get; set; } = new SeriesCollection();
    public SeriesCollection FitnessData { get; set; } = new SeriesCollection();
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

    public Func<double, string> PercFormatter { get; set; } = value => value.ToString("N2");
    public Func<double, string> YFormatter { get; set; } = value => value.ToString("N0");

    #region Minutes

#if DEBUG
    private int minutes = 720;
#endif

#if RELEASE
    private int minutes = 240;
#endif

    public int Minutes
    {
      get { return minutes; }
      set
      {
        if (value != minutes)
        {
          minutes = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region SplitTake

    private double splitTake = 4.5;

    public double SplitTake
    {
      get { return splitTake; }
      set
      {
        if (value != splitTake)
        {
          splitTake = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region RunTime

    private TimeSpan runTime;

    public TimeSpan RunTime
    {
      get { return runTime; }
      set
      {
        if (value != runTime)
        {
          runTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Distribute

    protected ActionCommand distribute;

    public ICommand Distribute
    {
      get
      {
        return distribute ??= new ActionCommand(OnDistribute);
      }
    }

    SerialDisposable serialDisposable = new SerialDisposable();
    DateTime lastElapsed;

    protected virtual void OnDistribute()
    {
      lastElapsed = DateTime.Now;

      BuyBotManager.InitializeManager(AgentCount);
      SellBotManager.InitializeManager(AgentCount);

      serialDisposable.Disposable = Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOnDispatcher().Subscribe((x) =>
      {
        TimeSpan diff = DateTime.Now - lastElapsed;

        RunTime = RunTime.Add(diff);

        lastElapsed = DateTime.Now;
      });

      DistributeGeneration();
    }

    #endregion

    #region BestFitness

    private float bestFitness;

    public float BestFitness
    {
      get { return bestFitness; }
      set
      {
        if (value != bestFitness)
        {
          bestFitness = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region GenerationRunTime

    private TimeSpan generationRunTime;

    public TimeSpan GenerationRunTime
    {
      get { return generationRunTime; }
      set
      {
        if (value != generationRunTime)
        {
          generationRunTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TotalValue

    private decimal totalValue;

    public decimal TotalValue
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

    #region Drawdawn

    private decimal drawdawn;

    public decimal Drawdawn
    {
      get { return drawdawn; }
      set
      {
        if (value != drawdawn)
        {
          drawdawn = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region NumberOfTrades

    private decimal numberOfTrades;

    public decimal NumberOfTrades
    {
      get { return numberOfTrades; }
      set
      {
        if (value != numberOfTrades)
        {
          numberOfTrades = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CurrentSymbol

    private string currentSymbol;

    public string CurrentSymbol
    {
      get { return currentSymbol; }
      set
      {
        if (value != currentSymbol)
        {
          currentSymbol = value;
          RaisePropertyChanged();
        }
      }
    }

    public ILogger Logger { get; }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();

      _listenerThread = new Thread(StartListening);
      _listenerThread.IsBackground = true;
      _listenerThread.Start();


      CreateCharts(AverageData);
      CreateCharts(FullData);
      CreateCharts(BestData);
      CreateCharts(DrawdawnData);
      CreateCharts(FitnessData);
      CreateCharts(NumberOfTradesData);

      Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOnDispatcher().Subscribe(async (x) =>
      {
        if (IsGenerationEnded())
        {
          Clients.ForEach(x => x.Done = false);

          UpdateGeneration(runResults);
        }
      });
    }

    #endregion

    private void CreateCharts(SeriesCollection series)
    {
      for (int i = 0; i < symbolsToTest.Length; i++)
      {
        var newSeries = new LineSeries()
        {
          Values = new ChartValues<decimal>(),
          PointGeometrySize = 0
        };


        newSeries.Fill = Brushes.Transparent;
        newSeries.Title = symbolsToTest[i];
        newSeries.PointForeground = Brushes.Transparent;

        series.Add(newSeries);
      }
    }

    #region DistributeGeneration

    DateTime generationStart;
    Random random = new Random();
    bool wasRandomPrevious = false;
    private ServerRunData serverRunData;
    private void DistributeGeneration()
    {
      generationStart = DateTime.Now;

      if (Clients.Count == 0)
        return;

      int agentsToRun = AgentCount;
      int agentsPerClient = AgentCount / Clients.Count;
      int runAgents = 0;

      ToStart = AgentCount;

      Clients.ForEach(x => x.SentBuyGenomes.Clear());
      Clients.ForEach(x => x.SentSellGenomes.Clear());

      CurrentSymbol = symbolsToTest[wholeRunsCount % symbolsToTest.Length];
      bool isRandom = false;

      if (!wasRandomPrevious)
      {
        isRandom = IsRandom();

        if (isRandom)
        {
          wasRandomPrevious = true;
          CurrentSymbol = symbolsToTest[random.Next(0, symbolsToTest.Length)];
        }
      }
      else
      {
        wasRandomPrevious = false;
      }



      foreach (var client in Clients.ToList())
      {
        if (agentsToRun < agentsPerClient)
        {
          agentsPerClient = agentsToRun;
        }

        var newData = new ServerRunData()
        {
          AgentCount = agentsPerClient,
          Generation = BuyBotManager.Generation,
          IsRandom = isRandom,
          Minutes = Minutes,
          Split = SplitTake,
          Symbol = CurrentSymbol,
        };

        serverRunData = newData;

        var buyGenomes = BuyBotManager.NeatAlgorithm.GenomeList.Skip(runAgents).Take(agentsPerClient).ToList();
        var sellGenomes = SellBotManager.NeatAlgorithm.GenomeList.Skip(runAgents).Take(agentsPerClient).ToList();

        buyGenomes.ForEach(x => client.SentBuyGenomes.Add(x.Id, false));
        sellGenomes.ForEach(x => client.SentSellGenomes.Add(x.Id, false));

        var buyDocument = NeatGenomeXmlIO.SaveComplete(buyGenomes, false);
        var sellDocument = NeatGenomeXmlIO.SaveComplete(sellGenomes, false);

        foreach (var buyGene in buyGenomes)
        {
          Console.WriteLine($"SENDING IDs: {buyGene.Id}");
        }

        newData.BuyGenomes = buyDocument.OuterXml.Replace("\"", "'");
        newData.SellGenomes = sellDocument.OuterXml.Replace("\"", "'");

        agentsToRun -= agentsPerClient;
        runAgents += agentsPerClient;

        ToStart -= agentsPerClient;
        InProgress += agentsPerClient;

        SendMessage(client, JsonSerializer.Serialize(newData).Replace("\n", "") + MessageContract.EndOfMessage);
      }

      if (!isRandom)
      {
        wholeRunsCount++;
      }
    }

    #endregion

    #region UpdateGeneration

    private void UpdateGeneration(IEnumerable<RunData> runData)
    {
      GenerationRunTime = DateTime.Now - generationStart;

      var addStats = !IsRandom() && CurrentSymbol == symbolsToTest[0];

      var bestBuy = BuyBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).First();
      var bestSell = SellBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).First();

      var index = symbolsToTest.IndexOf((x) => x == serverRunData.Symbol);

      if (index != null && !wasRandomPrevious)
      {
        var bestRun = runData.OrderByDescending(x => x.Fitness).First();

        FullData[index.Value].Values.Add(bestRun.TotalValue);
        DrawdawnData[index.Value].Values.Add(bestRun.Drawdawn);
        NumberOfTradesData[index.Value].Values.Add(bestRun.NumberOfTrades);
        FitnessData[index.Value].Values.Add(bestRun.OriginalFitness);
        BestData[index.Value].Values.Add(bestRun.Fitness);
        AverageData[index.Value].Values.Add(bestRun.Average);

        if (addStats)
        {
          BestFitness = (float)bestRun.OriginalFitness;
          TotalValue = bestRun.TotalValue;
          Drawdawn = bestRun.Drawdawn;
          NumberOfTrades = bestRun.NumberOfTrades;
        }

        if (addStats)
        {
          Labels.Add(BuyBotManager.Generation.ToString());
          RaisePropertyChanged(nameof(Labels));
        }
      }

      FinishedCount = 0;
      ToStart = AgentCount;

      SimulationAIPromptViewModel.SaveProgress(BuyBotManager, session, "BUY");
      SimulationAIPromptViewModel.SaveProgress(SellBotManager, session, "SELL");


      BuyBotManager.UpdateNEATGeneration();
      SellBotManager.UpdateNEATGeneration();

      runResults.Clear();
      DistributeGeneration();
    }

    #endregion


    string[] symbolsToTest = new string[] { "ADAUSDT", "BTCUSDT", "ETHUSDT", "LTCUSDT", "BNBUSDT" };

    List<RunData> runResults = new List<RunData>();

    #region IsRandom

    private int wholeRunsCount;
    private bool IsRandom()
    {
      return wholeRunsCount % symbolsToTest.Length == 0 && BuyBotManager.Generation != 0;
    }

    #endregion

    #region StartListening

    private void StartListening()
    {
      try
      {
        var add = JsonSerializer.Deserialize<ServerAdress>(File.ReadAllText("server.txt"));
        _listener = new TcpListener(IPAddress.Parse(add.IP), add.Port);
        _listener.Start();

        Console.WriteLine($"Server started {add.IP} {add.Port}");

        while (true)
        {
          TcpClient client = _listener.AcceptTcpClient();

          VSynchronizationContext.InvokeOnDispatcher(() =>
          {
            var newClient = new CloudClient() { Client = client };
            Clients.Add(newClient);

            Thread clientThread = new Thread(() => HandleClient(newClient));
            clientThread.IsBackground = true;
            clientThread.Start();
          });
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error: {ex.Message}\n");
      }
    }

    #endregion

    #region SendMessage

    private void SendMessage(CloudClient client, string serverData)
    {
      try
      {
        var _stream = client.Client.GetStream();

        if (_stream != null)
        {

          var data = Encoding.Unicode.GetBytes(serverData);

          _stream.Write(data, 0, data.Length);
          _stream.Flush();
        }
      }
      catch (Exception ex)
      {
        if (ex.ToString().Contains("An existing connection was forcibly closed by the remote host.."))
        {
          TryRemoveClient(client);
        }

        Console.WriteLine(ex);
      }
    }

    #endregion

    #region HandleClient

    object batton = new object();
    int ind = 0;
    private void HandleClient(CloudClient client)
    {
      try
      {
        NetworkStream stream = client.Client.GetStream();
        Span<byte> buffer = new byte[10485760];

        MemoryStream ms = new MemoryStream();


        int bytesRead;
        string currentData = "";

        // Read the stream until all data is received.
        while ((bytesRead = stream.Read(buffer)) > 0)
        {
          try
          {
            ms.Write(buffer);

            currentData = Encoding.Unicode.GetString(ms.ToArray()).Trim();

            if (currentData.Contains(MessageContract.EndOfMessage))
            {
              var indexOf = currentData.IndexOf(MessageContract.EndOfMessage);

              currentData = currentData.Substring(0, indexOf);

              var data = JsonSerializer.Deserialize<RunData>(currentData);

              lock (batton)
              {
                Console.WriteLine($"Received message {++ind}");

                runResults.Add(data);

                if (!client.Done)
                {
                  UpdateManager(client, data.BuyGenomes, BuyBotManager, data.SellGenomes, SellBotManager);
                }
              }

              stream.Flush();
              ms.Flush();
              ms.Dispose();
              ms = new MemoryStream();
              buffer.Clear();
            }
          }
          catch (Exception ex)
          {
            stream.Flush();
            ms.Flush();
            ms.Dispose();
            ms = new MemoryStream();
            buffer.Clear();

            Logger.Log(ex);
          }
        }

        TryRemoveClient(client);

      }
      catch (Exception ex)
      {
        TryRemoveClient(client);

        Logger.Log(ex);
      }
    }

    #endregion

    #region IsGenerationEnded

    public bool IsGenerationEnded()
    {
      return Clients.All(x => x.Done) && Clients.Count > 0;
    }

    #endregion

    #region UpdateManager

    object managerBatton = new object();
    bool broken = false;

    private void UpdateManager(
      CloudClient tcpClient,
      string buyXml,
      NEATManager<AIBot> buyManger,
      string sellXml, NEATManager<AIBot> sellManager)
    {
      lock (managerBatton)
      {
        if (buyManger.NeatAlgorithm == null)
        {
          SendMessage(tcpClient, MessageContract.Done);
          return;
        }

        List<NeatGenome> buyGenomeList = new List<NeatGenome>();
        List<NeatGenome> sellGenomeList = new List<NeatGenome>();

        using (StringReader tx = new StringReader(buyXml))
        using (XmlReader xr = XmlReader.Create(tx))
        {
          buyGenomeList = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, buyManger.GetGenomeFactory());
        }

        using (StringReader tx = new StringReader(sellXml))
        using (XmlReader xr = XmlReader.Create(tx))
        {
          sellGenomeList = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, sellManager.GetGenomeFactory());
        }

        foreach (var buyGene in buyGenomeList)
        {
          Console.WriteLine($"RECEIVED ID: {buyGene.Id}");
        }

        List<Tuple<NeatGenome, NeatGenome>> list = new List<Tuple<NeatGenome, NeatGenome>>();

        for (int i = 0; i < buyGenomeList.Count; i++)
        {
          list.Add(new Tuple<NeatGenome, NeatGenome>(buyGenomeList[i], sellGenomeList[i]));
        }


        foreach (var receivedGenome in list)
        {
          var existingBuy = buyManger.NeatAlgorithm.GenomeList.SingleOrDefault(x => x.Id == receivedGenome.Item1.Id);
          var existingSell = sellManager.NeatAlgorithm.GenomeList.SingleOrDefault(x => x.Id == receivedGenome.Item2.Id);

          if (existingBuy != null && existingSell != null)
          {
            if (!tcpClient.SentBuyGenomes[existingBuy.Id] && !tcpClient.SentSellGenomes[existingSell.Id])
            {
              buyManger.NeatAlgorithm.GenomeList[buyManger.NeatAlgorithm.GenomeList.IndexOf(existingBuy)].AddFitness(receivedGenome.Item1.Fitness);
              sellManager.NeatAlgorithm.GenomeList[sellManager.NeatAlgorithm.GenomeList.IndexOf(existingSell)].AddFitness(receivedGenome.Item2.Fitness);

              FinishedCount += 1;
              InProgress -= 1;

              tcpClient.SentBuyGenomes[existingBuy.Id] = true;
              tcpClient.SentSellGenomes[existingSell.Id] = true;

              tcpClient.Done = tcpClient.SentBuyGenomes.All(x => x.Value == true) && tcpClient.SentSellGenomes.All(x => x.Value == true);

              if (tcpClient.Done)
              {
                SendMessage(tcpClient, MessageContract.Done);
              }
            }
          }
          else
          {
            broken = true;
            Console.WriteLine($"------------------NOT FOUND GENOME WITH ID: {receivedGenome.Item1.Id}------------------");
            Console.WriteLine($"------------------NOT FOUND GENOME WITH ID: {receivedGenome.Item2.Id}------------------");
          }
        }
      }
    }

    #endregion

    #region TryRemoveClient

    private void TryRemoveClient(CloudClient client)
    {
      VSynchronizationContext.InvokeOnDispatcher(() =>
      {
        try
        {
          client?.Client.Close();
          Clients.Remove(client);
        }
        catch (Exception ex)
        {
          Logger.Log(ex);
        }
      });
    }

    #endregion

    #region IsClientConnected

    private bool IsClientConnected(TcpClient client)
    {
      try
      {
        if (client == null || !client.Connected)
        {
          return false;
        }

        // Poll to check if the client is still connected
        if (client.Client.Poll(0, SelectMode.SelectRead))
        {
          // Check if the buffer is empty, meaning the client has disconnected
          return client.Client.Available > 0;
        }

        return true;
      }
      catch (SocketException)
      {
        return false; // Exception means the client is not connected
      }
    }

    #endregion

    #region OnClose

    protected override void OnClose(Window window)
    {
      Clients.ForEach(x => x.Client.Close());

      base.OnClose(window);
    }

    #endregion

    #endregion
  }

}
