using CloudComputing.Domains;
using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.ViewModels;
using LiveCharts;
using SharpNeat.Genomes.Neat;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Windows.Input;
using System.Xml;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels;
using VNeuralNetwork;

namespace CouldComputingServer
{
  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    private TcpListener _listener;
    private Thread _listenerThread;
    string session;

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory) : base(viewModelsFactory)
    {
      session = DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");

      BuyBotManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Buy);
      SellBotManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Sell);
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

    public ObservableCollection<TcpClient> Clients { get; set; } = new ObservableCollection<TcpClient>();

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

    private int inProgress;

    public int InProgress
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


    public ChartValues<float> ChartData { get; set; } = new ChartValues<float>();
    public ChartValues<decimal> FullData { get; set; } = new ChartValues<decimal>();
    public ChartValues<float> BestData { get; set; } = new ChartValues<float>();
    public ChartValues<decimal> DrawdawnData { get; set; } = new ChartValues<decimal>();
    public ChartValues<float> FitnessData { get; set; } = new ChartValues<float>();
    public ChartValues<double> NumberOfTradesData { get; set; } = new ChartValues<double>();

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

    #endregion

    #region Methods

    #region Initialize


    public override void Initialize()
    {
      base.Initialize();

      _listenerThread = new Thread(StartListening);
      _listenerThread.IsBackground = true;
      _listenerThread.Start();


    }

    #endregion

    #region DistributeGeneration

    DateTime generationStart;
    private void DistributeGeneration()
    {
      generationStart = DateTime.Now;

      if (Clients.Count == 0)
        return;

      int agentsToRun = AgentCount;
      int agentsPerClient = AgentCount / Clients.Count;
      int runAgents = 0;

      ToStart = AgentCount;

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
          IsRandom = false,
          Minutes = Minutes,
          Split = SplitTake,
          Symbol = "ADAUSDT",
        };

        var buyGenomes = BuyBotManager.NeatAlgorithm.GenomeList.Skip(runAgents).Take(agentsPerClient).ToList();
        var sellGenomes = SellBotManager.NeatAlgorithm.GenomeList.Skip(runAgents).Take(agentsPerClient).ToList();

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

        SendMessage(client, newData);
      }
    }

    #endregion

    #region UpdateGeneration

    private void UpdateGeneration()
    {
      GenerationRunTime = DateTime.Now - generationStart;

      VSynchronizationContext.InvokeOnDispatcher(async () =>
      {
        var addStats = !IsRandom() && BuyBotManager.Generation + 1 % 10 > 1;

        var bestBuy = BuyBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).First();
        var bestSell = SellBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).First();

        if (addStats)
        {
          ChartData.Add(BuyBotManager.NeatAlgorithm.GenomeList.Average(x => x.Fitness));
          BestData.Add(bestSell.Fitness);


          Labels.Add(BuyBotManager.Generation.ToString());
          RaisePropertyChanged(nameof(Labels));
        }

        FinishedCount = 0;
        ToStart = AgentCount;

        SimulationAIPromptViewModel.SaveProgress(BuyBotManager, session, "BUY");
        SimulationAIPromptViewModel.SaveProgress(SellBotManager, session, "SELL");

        if (taskCompletionSource != null && !taskCompletionSource.Task.IsCompleted)
        {
          await taskCompletionSource.Task;
        }

        _ = RunTest(bestBuy, bestSell, addStats);

        BuyBotManager.UpdateNEATGeneration();
        SellBotManager.UpdateNEATGeneration();

        DistributeGeneration();
      });
    }

    #endregion

    #region RunTest

    TaskCompletionSource<bool> taskCompletionSource;

    private Task RunTest(NeatGenome buy, NeatGenome sell, bool addStats)
    {
      taskCompletionSource = new TaskCompletionSource<bool>();

      InProgress++;

      var buyGene = new NeatGenome(buy, 0, 0);
      var sellGene = new NeatGenome(sell, 0, 0);

      var testBot = SimulationAIPromptViewModel.GetBot("ADAUSDT",
        new AIBot(buyGene),
        new AIBot(sellGene),
        Minutes,
        SplitTake,
        new Random(),
        ViewModelsFactory);

      testBot.FromDate = new DateTime(2019, 1, 1);

      testBot.TradingBot.Strategy.BuyAIBot.NeuralNetwork.InputCount = BuyBotManager.inputCount;
      testBot.TradingBot.Strategy.SellAIBot.NeuralNetwork.InputCount = SellBotManager.inputCount;

      BuyBotManager.NeatAlgorithm.UpdateNetworks(new List<NeatGenome>() { buyGene });
      SellBotManager.NeatAlgorithm.UpdateNetworks(new List<NeatGenome>() { sellGene });

      Task.Run(async () =>
      {
        //Pri zmene symbolu sa nacitava nove CTKS a neni dobre
        if (BuyBotManager.Generation == 0)
          await Task.Delay(500);


        testBot.Finished += (x, y) => TestBot_Finished(x, y, addStats);
        testBot.Start();
      });


      ToStart--;

      return taskCompletionSource.Task;
    }

    #endregion

    #region TestBot_Finished

    private void TestBot_Finished(object sender, EventArgs e, bool addStats)
    {
      if (sender is SimulationTradingBot<AIPosition, AIStrategy> sim)
      {
        VSynchronizationContext.InvokeOnDispatcher(() =>
        {
          taskCompletionSource.SetResult(true);

          var simStrategy = sim.TradingBot.Strategy;

          SimulationAIPromptViewModel.AddFitness(simStrategy);

          if (addStats)
          {
            FullData.Add(simStrategy.TotalValue);
            DrawdawnData.Add(simStrategy.MaxDrawdawnFromMaxTotalValue);
            NumberOfTradesData.Add(simStrategy.ClosedSellPositions.Count);

            BestFitness = simStrategy.BuyAIBot.NeuralNetwork.Fitness;
            FitnessData.Add(BestFitness);

          }

          InProgress--;
        });
      }

    }

    #endregion

    #region IsRandom

    private bool IsRandom()
    {
      return BuyBotManager.Generation % 5 == 0 && BuyBotManager.Generation != 0;
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
            Clients.Add(client);
          });

          Thread clientThread = new Thread(() => HandleClient(client));
          clientThread.IsBackground = true;
          clientThread.Start();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error: {ex.Message}\n");
      }
    }

    #endregion

    #region SendMessage

    private void SendMessage(TcpClient client, ServerRunData serverData)
    {
      try
      {
        var _stream = client.GetStream();

        if (_stream != null)
        {
          var json = JsonSerializer.Serialize(serverData).Replace("\n", "") + "END_OF_MESSAGE";
          var data = Encoding.Unicode.GetBytes(json);

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
    private void HandleClient(TcpClient client)
    {
      try
      {
        NetworkStream stream = client.GetStream();
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

            if (currentData.Contains("_END_"))
            {
              var indexOf = currentData.IndexOf("_END_");

              currentData = currentData.Substring(0, indexOf);

              var data = JsonSerializer.Deserialize<RunData>(currentData);

              lock (batton)
              {
                UpdateManager(data.BuyGenomes, BuyBotManager);
                UpdateManager(data.SellGenomes, SellBotManager);


                if (FinishedCount == AgentCount)
                {
                  UpdateGeneration();
                }
              }

              stream.Flush();
              ms.Flush();
              ms.Dispose();
              ms = new MemoryStream();
            }

            buffer.Clear();
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex);
          }
        }

        TryRemoveClient(client);

      }
      catch (Exception ex)
      {
        if (ex.ToString().Contains("An existing connection was forcibly closed by the remote host.."))
        {
          TryRemoveClient(client);
          return;
        }

        if (ex.Message.Contains("Unable to read data from the transport connection: A blocking operation was interrupted by a call to WSACancelBlockingCall"))
        {
          TryRemoveClient(client);
          return;
        }

        Console.WriteLine(ex);
      }
    }

    #endregion

    #region UpdateManager

    private void UpdateManager(string xml, NEATManager<AIBot> manager)
    {
      using (StringReader tx = new StringReader(xml))
      using (XmlReader xr = XmlReader.Create(tx))
      {
        // Replace NeatGenomeXmlIO.ReadCompleteGenomeList with your actual method to read genomes.
        var genomeList = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, manager.GetGenomeFactory());

        if (manager == BuyBotManager)
        {
          foreach (var buyGene in genomeList)
          {
            Console.WriteLine($"RECEIVED IDs: {buyGene.Id}");
          }
        }


        foreach (var receivedGenome in genomeList)
        {
          var existing = manager.NeatAlgorithm.GenomeList.SingleOrDefault(x => x.Id == receivedGenome.Id);

          if (existing != null)
          {
            manager.NeatAlgorithm.GenomeList[manager.NeatAlgorithm.GenomeList.IndexOf(existing)].AddFitness(receivedGenome.Fitness);

            FinishedCount += 0.5;

            if (FinishedCount % 2 == 0)
            {
              InProgress -= 1;
            }
          }
          else
          {
            Console.WriteLine($"NOT FOUND GENOME WITH ID: {receivedGenome.Id}");
          }
        }
      }
    }

    #endregion

    #region TryRemoveClient

    private void TryRemoveClient(TcpClient client)
    {
      client?.Close();
      VSynchronizationContext.InvokeOnDispatcher(() => { Clients.Remove(client); });
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

    #endregion
  }

}
