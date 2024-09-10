using CloudComputing.Domains;
using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.ViewModels;
using LiveCharts.Configurations;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Helpers;
using VCore.WPF.Misc;
using VCore.WPF.ViewModels;
using VNeuralNetwork;
using RunData = CloudComputing.Domains.RunData;

namespace CouldComputingServer
{

  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    private TcpListener _listener;
    private Thread _listenerThread;

    Dictionary<CloudClient, string> messages = new Dictionary<CloudClient, string>();
    List<ClientData> runResults = new List<ClientData>();

    string[] allSymbols = new string[] {
      "ADAUSDT", "BTCUSDT",
      "ETHUSDT", "LTCUSDT",
      "GALAUSDT", "EOSUSDT",
      "AVAXUSDT", "ALGOUSDT",
      "SOLUSDT"
    };

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory, ILogger logger) : base(viewModelsFactory)
    {
      TrainingSession = new TrainingSession(DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss"));
      TrainingSession.CreateSymbolsToTest(allSymbols);

      BuyBotManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Buy);
      SellBotManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Sell);

      CurrentSymbol = TrainingSession.SymbolsToTest[0].Name;
      Logger = logger;
      Title = "Cloud Computing SERVER";
    }

    #region Properties

    public NEATManager<AIBot> BuyBotManager { get; set; }
    public NEATManager<AIBot> SellBotManager { get; set; }

    public TrainingSession TrainingSession { get; set; }

    #region UseRandomizer

    private bool useRandomizer = true;

    public bool UseRandomizer
    {
      get { return useRandomizer; }
      set
      {
        if (value != useRandomizer)
        {
          useRandomizer = value;

          if (useRandomizer)
          {
            TrainingSession.CreateSymbolsToTest(allSymbols);
          }
          else
          {
            TrainingSession.CreateSymbolsToTest(allSymbols[0]);
          }


          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region AgentCount

#if DEBUG
    private int agentCount = 1;
#endif

#if RELEASE
    private int agentCount = 240;
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


    public Func<double, string> PercFormatter { get; set; } = value => value.ToString("N2");
    public Func<double, string> YFormatter { get; set; } = value => value.ToString("N0");

    #region Minutes

#if DEBUG
    private int minutes = 240;
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

    #region Cycle

    private int cycle;

    public int Cycle
    {
      get { return cycle; }
      set
      {
        if (value != cycle)
        {
          cycle = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CycleRunTime

    private TimeSpan cycleRunTime;

    public TimeSpan CycleRunTime
    {
      get { return cycleRunTime; }
      set
      {
        if (value != cycleRunTime)
        {
          cycleRunTime = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion


    #endregion

    #region Commands

    #region LoadGeneration

    protected ActionCommand loadGeneration;

    public ICommand LoadGeneration
    {
      get
      {
        return loadGeneration ??= new ActionCommand(OnLoadGeneration).DisposeWith(this);
      }
    }

    protected virtual void OnLoadGeneration()
    {
      using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
      {
        // Set properties for FolderBrowserDialog
        folderBrowserDialog.Description = "Select a Folder";
        folderBrowserDialog.ShowNewFolderButton = true;
        folderBrowserDialog.SelectedPath = Path.GetFullPath("Trainings");

        // Show the dialog and check if the user selected a folder
        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
        {
          // Get the selected folder path
          string selectedPath = folderBrowserDialog.SelectedPath;

          var directories = Directory.GetDirectories(selectedPath);

          var lastCreatedDirectory = directories
                                        .Select(dir => new DirectoryInfo(dir))
                                        .OrderByDescending(dir => dir.CreationTime)
                                        .Where(x => x.FullName.Contains("Generation"))
                                        .FirstOrDefault();

          if (lastCreatedDirectory == null)
            lastCreatedDirectory = new DirectoryInfo(selectedPath);

          // Call your method or do something with the selected folder path
          if (File.Exists(Path.Combine(selectedPath, "session.txt")))
            TrainingSession.Load(File.ReadAllText(Path.Combine(selectedPath, "session.txt")));

          BuyBotManager.LoadGeneration(Path.Combine(lastCreatedDirectory.FullName, "BUY.txt"));
          SellBotManager.LoadGeneration(Path.Combine(lastCreatedDirectory.FullName, "SELL.txt"));
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
    DateTime cycleLastElapsed;

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

      TrainingSession.SymbolsToTest.Skip(1).ForEach(x => x.IsEnabled = false);
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


      Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(async (x) =>
      {
        lock (batton)
        {
          if (IsGenerationEnded())
          {
            VSynchronizationContext.InvokeOnDispatcher(() =>
            {
              Clients.ForEach(x => x.Done = false);

              UpdateGeneration(runResults);
            });
          }
        }
      });


    }

    #endregion

    #region ResetGeneration

    bool canResetGeneration = true;
    private void ResetGeneration()
    {
      Task.Run(async () =>
      {
        if (canResetGeneration)
        {
          canResetGeneration = false;

          foreach (var client in Clients)
          {
            TCPHelper.SendMessage(client.Client, MessageContract.Done);
          }

          await Task.Delay(5000);

          VSynchronizationContext.InvokeOnDispatcher(() =>
          {
            Logger.Log(MessageType.Warning, "Reseting generation!");
            ToStart = 0;
            InProgress = 0;
            FinishedCount = 0;
            DistributeGeneration(Cycle);
          });

          Observable.Timer(TimeSpan.FromSeconds(10))
         .Subscribe(x => canResetGeneration = true)
         .DisposeWith(this);
        }
      });
    }

    #endregion

    #region DistributeGeneration

    DateTime generationStart;

    private ServerRunData serverRunData;
    private object dbaton = new object();
    SemaphoreSlim semaphoreSlimDistribute = new SemaphoreSlim(1, 1);
    private async void DistributeGeneration(int? generation = null)
    {
      try
      {
        await semaphoreSlimDistribute.WaitAsync();

        generationStart = DateTime.Now;

        if (Clients.Count == 0 || !BuyBotManager.NeatAlgorithm.GenomeList.Any())
          return;

        int agentsToRun = AgentCount;
        //int agentsPerClient = AgentCount / Clients.Count;
        int runAgents = 0;

        ToStart = AgentCount;

        Clients.ForEach(x => x.SentBuyGenomes.Clear());
        Clients.ForEach(x => x.SentSellGenomes.Clear());

        var gen = generation ?? Cycle;

        CurrentSymbol = TrainingSession.SymbolsToTest[gen % TrainingSession.SymbolsToTest.Count].Name;

        Clients.ForEach(x => { x.Done = false; x.ErrorCount = 0; });

        foreach (var client in Clients)
        {
          client.PopulationSize = AgentCount / Clients.Count;
        }

        var ordered = Clients.OrderBy(x => x.LastGenerationTime).ToList();

        var first = ordered.First();
        var last = ordered.Last();

        foreach (var client in Clients.ToList())
        {
          var newData = new ServerRunData()
          {
            AgentCount = client.PopulationSize,
            Generation = Cycle,
            IsRandom = false,
            Minutes = Minutes,
            Split = SplitTake,
            Symbol = CurrentSymbol,
          };

          serverRunData = newData;

          var buyGenomes = BuyBotManager.NeatAlgorithm.GenomeList.Skip(runAgents).Take(client.PopulationSize).ToList();
          var sellGenomes = SellBotManager.NeatAlgorithm.GenomeList.Skip(runAgents).Take(client.PopulationSize).ToList();

          buyGenomes.ForEach(x => client.SentBuyGenomes.Add(x.Id, false));
          sellGenomes.ForEach(x => client.SentSellGenomes.Add(x.Id, false));

          var buyDocument = NeatGenomeXmlIO.SaveComplete(buyGenomes, false);
          var sellDocument = NeatGenomeXmlIO.SaveComplete(sellGenomes, false);

          newData.BuyGenomes = buyDocument.OuterXml;
          newData.SellGenomes = sellDocument.OuterXml;

          agentsToRun -= client.PopulationSize;
          runAgents += client.PopulationSize;

          ToStart -= client.PopulationSize;
          InProgress += client.PopulationSize;

          var message = JsonSerializer.Serialize(newData);

          if (messages.ContainsKey(client))
          {
            messages[client] = message;
          }
          else
          {
            messages.Add(client, message);
          }

          TCPHelper.SendMessage(client.Client, MessageContract.GetDataMessage(message));
        }

        Logger.Log(MessageType.Inform, $"Generation {BuyBotManager.Generation} - {CurrentSymbol} distributed");
      }
      finally
      {
        semaphoreSlimDistribute.Release();
      }
    }

    #endregion

    #region UpdateGeneration

    NeatGenome bestMeanGenome;
    private void UpdateGeneration(IEnumerable<ClientData> runData)
    {
      GenerationRunTime = DateTime.Now - generationStart;

      var isLastSymbol = CurrentSymbol == TrainingSession.SymbolsToTest.Last().Name;

      var bestBuy = BuyBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).First();
      var bestSell = SellBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).First();

      var index = TrainingSession.SymbolsToTest.IndexOf((x) => x.Name == serverRunData.Symbol);

      if (index != null)
      {
        if (bestMeanGenome != null)
        {
          var bestClient = runData.FirstOrDefault(x => x.BuyGenomes.Contains($"Network id=\"{bestMeanGenome.Id}\""));
          var bestRun = bestClient.GenomeData.FirstOrDefault(x => x.BuyGenome.Contains($"Network id=\"{bestMeanGenome.Id}\""));

          Logger.Log(MessageType.Inform, $"{bestMeanGenome.Id} - Genome ID {bestClient.Symbol} - {bestRun.TotalValue} $");

          TrainingSession.AddValue(serverRunData.Symbol, Statistic.AverageFitness, bestClient.Average);
          TrainingSession.AddValue(serverRunData.Symbol, Statistic.BestFitness, bestRun.Fitness);
          TrainingSession.AddValue(serverRunData.Symbol, Statistic.OriginalFitness, bestRun.OriginalFitness);
          TrainingSession.AddValue(serverRunData.Symbol, Statistic.TotalValue, bestRun.TotalValue);
          TrainingSession.AddValue(serverRunData.Symbol, Statistic.Drawdawn, bestRun.Drawdawn);
          TrainingSession.AddValue(serverRunData.Symbol, Statistic.NumberOfTrades, bestRun.NumberOfTrades);

          var fullCycle = Cycle % TrainingSession.SymbolsToTest.Count == 0;

          cycleLastElapsed = DateTime.Now;

          if (fullCycle)
          {
            BestFitness = (float)bestRun.Fitness;
            TotalValue = bestRun.TotalValue;
            Drawdawn = bestRun.Drawdawn;
            NumberOfTrades = bestRun.NumberOfTrades;

            TrainingSession.AddLabel();
          }
        }

        if (isLastSymbol)
        {
          var generation = $"Generation {BuyBotManager.Generation}";
          var folder = Path.Combine("Trainings", TrainingSession.Name);
          Directory.CreateDirectory(folder);

          var training = JsonSerializer.Serialize(TrainingSession);

          File.WriteAllText(Path.Combine(folder, "session.txt"), training);

          CycleRunTime = DateTime.Now - cycleLastElapsed;


          cycleLastElapsed = DateTime.Now;

          BuyBotManager.NeatAlgorithm.GenomeList.ForEach(x => x.UpdateFitnesses());
          SellBotManager.NeatAlgorithm.GenomeList.ForEach(x => x.UpdateFitnesses());

          BuyBotManager.NeatAlgorithm.UpdateGenerationWithoutFitnessReset();
          SellBotManager.NeatAlgorithm.UpdateGenerationWithoutFitnessReset();

          bestMeanGenome = BuyBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).FirstOrDefault();
          var bestMeanSellGenome = SellBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).FirstOrDefault();

          TrainingSession.AddValue(serverRunData.Symbol, Statistic.MedianFitness, (decimal)bestMeanGenome.Fitness);

          SimulationAIPromptViewModel.SaveGeneration(BuyBotManager, TrainingSession.Name, generation, "BUY.txt", "MEDIAN_BUY.txt");
          SimulationAIPromptViewModel.SaveGeneration(SellBotManager, TrainingSession.Name, generation, "SELL.txt", "MEDIAN_SELL.txt");

          BuyBotManager.ResetFitness();
          SellBotManager.ResetFitness();

          StartTest(bestMeanGenome, bestMeanSellGenome);
        }
      }

      Cycle++;
      FinishedCount = 0;
      ToStart = AgentCount;

      runResults.Clear();
      DistributeGeneration();
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

          int receiveBufferSize = MessageContract.BUFFER_SIZE_CLIENT;
          int sendBufferSize = MessageContract.BUFFER_SIZE_CLIENT;

          client.ReceiveBufferSize = receiveBufferSize;
          client.SendBufferSize = sendBufferSize;

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

    #region HandleClient

    object batton = new object();

    private void HandleClient(CloudClient client)
    {
      Task.Run(() =>
        {
          try
          {
            NetworkStream stream = client.Client.GetStream();
            Span<byte> buffer = new byte[MessageContract.BUFFER_SIZE_CLIENT];

            MemoryStream ms = new MemoryStream();
            StringBuilder messageBuilder = new StringBuilder();

            int bytesRead;

            while ((bytesRead = stream.Read(buffer)) > 0)
            {
              try
              {
                lock (batton)
                {
                  // Append received data to the memory stream
                  ms.Write(buffer.Slice(0, bytesRead));

                  // Convert the memory stream to string and append to messageBuilder
                  string currentData = Encoding.Unicode.GetString(ms.ToArray());
                  messageBuilder.Append(currentData);

                  // Reset the memory stream for the next chunk
                  ms.SetLength(0);

                  var message = messageBuilder.ToString();

                  if (message.Contains(MessageContract.Error))
                  {
                    TCPHelper.SendMessage(client.Client, MessageContract.GetDataMessage(messages[client]));
                    Logger.Log(MessageType.Warning, "Error from client reseding data");

                    client.ErrorCount++;
                    ms = new MemoryStream();

                    if (client.ErrorCount % 10 == 0)
                    {
                      ResetGeneration();
                    }

                    continue;
                  }

                  if (!MessageContract.IsDataMessage(message))
                  {
                    continue;
                  }

                  // Full message received, process it
                  string completeMessage = messageBuilder.ToString();

                  // Clear for the next message
                  messageBuilder.Clear();
                  buffer.Clear();
                  stream.Flush();
                  ms = new MemoryStream();

                  var split = MessageContract.GetDataMessageContent(message);
                  var data = JsonSerializer.Deserialize<ClientData>(split);

                  runResults.Add(data);

                  if (!client.Done)
                  {
                    UpdateManager(client, data.BuyGenomes, BuyBotManager, data.SellGenomes, SellBotManager);
                  }

                  client.ErrorCount = 0;

                  stream.Flush();
                  ms.Flush();
                  ms.Dispose();
                  ms = new MemoryStream();
                  buffer.Clear();
                  messageBuilder.Clear();

                  if (client.Done)
                    Logger.Log(MessageType.Inform2, "SUCESSFULL GENOME UPDATE");

                }
              }
              catch (Exception ex)
              {
                ms = new MemoryStream();
                messageBuilder.Clear();

                client.ErrorCount++;

                if (client.ErrorCount % 10 == 0)
                {
                  ResetGeneration();
                }

                Logger.Log(MessageType.Error, $"ERROR TRANSIMITING DATA!", false, false);
              }

            }

            TryRemoveClient(client);
          }
          catch (Exception ex)
          {
            TryRemoveClient(client);

            Logger.Log(ex);
          }
        });
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
          TCPHelper.SendMessage(tcpClient.Client, MessageContract.Done);
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
              existingBuy.AddSequentialFitness(receivedGenome.Item1.Fitness);
              existingSell.AddSequentialFitness(receivedGenome.Item2.Fitness);

              if (existingBuy.fitnesses.Where(x => x > 0).GroupBy(x => x).Any(g => g.Count() > 1))
              {
                Logger.Log(MessageType.Warning, "SAME FITNESSES FOR DIFFERENT SYMBOLS!");

                foreach(var fitness in existingBuy.fitnesses)
                {
                  Logger.Log(MessageType.Warning, fitness.ToString());
                }

                ResetGeneration();

              }

              if (existingBuy.fitnesses.Where(x => x > 0).GroupBy(x => x).Any(g => g.Count() > 1))
              {
                Logger.Log(MessageType.Warning, "SAME FITNESSES FOR DIFFERENT SYMBOLS!");

                foreach (var fitness in existingBuy.fitnesses)
                {
                  Logger.Log(MessageType.Warning, fitness.ToString());
                }

                ResetGeneration();

              }


              FinishedCount += 1;
              InProgress -= 1;

              tcpClient.SentBuyGenomes[existingBuy.Id] = true;
              tcpClient.SentSellGenomes[existingSell.Id] = true;

              tcpClient.Done = tcpClient.SentBuyGenomes.All(x => x.Value == true) && tcpClient.SentSellGenomes.All(x => x.Value == true);

              if (tcpClient.Done)
              {
                tcpClient.LastGenerationTime = DateTime.Now;
                TCPHelper.SendMessage(tcpClient.Client, MessageContract.Done);
              }
            }
          }
          else
          {
            Logger.Log(MessageType.Error, $"NOT FOUND GENOME WITH ID: {receivedGenome.Item1.Id}", true);

            if (tcpClient.ErrorCount < 10)
            {
              tcpClient.ErrorCount = 10;
              ResetGeneration();
              break;
            }

            tcpClient.ErrorCount++;
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

    #region StartTest

    SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    private void StartTest(NeatGenome buy, NeatGenome sell)
    {
      Task.Run(async () =>
      {
        try
        {
          await semaphoreSlim.WaitAsync();

          var symbolsToTest = new string[] {
            "COTIUSDT",
            "LINKUSDT",
            "MATICUSDT",
            "BNBUSDT"};


          var fitness = new List<float>();
          var aIBotRunner = new AIBotRunner(Logger, ViewModelsFactory);

          foreach (var symbol in symbolsToTest)
          {
            await aIBotRunner.RunGeneration(
              1,
              Minutes,
              SplitTake,
              symbol,
              false,
              new List<NeatGenome>() { buy },
              new List<NeatGenome>() { sell }
              );

            var neat = aIBotRunner.Bots[0].TradingBot.Strategy.BuyAIBot.NeuralNetwork;

            fitness.Add(neat.Fitness);

            Logger.Log(MessageType.Inform, $"{aIBotRunner.Bots[0].Asset.Symbol} - {neat.Fitness}");

            neat.ResetFitness();
          }

          var meanFitness = MathHelper.GeometricMean(fitness);

          Logger.Log(MessageType.Inform, $"MEAN - {meanFitness}");

          VSynchronizationContext.InvokeOnDispatcher(() =>
          {
            TrainingSession.AddValue(CurrentSymbol, Statistic.BackTestMean, (decimal)meanFitness);
          });
        }
        finally
        {
          semaphoreSlim.Release();
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

