using CloudComputing.Domains;
using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.ViewModels;
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
using VCore.WPF.Misc;
using VCore.WPF.ViewModels;
using VNeuralNetwork;

namespace CouldComputingServer
{

  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    private TcpListener _listener;
    private Thread _listenerThread;

    Dictionary<CloudClient, string> messages = new Dictionary<CloudClient, string>();
    List<ClientData> runResults = new List<ClientData>();

    string[] allSymbols = new string[] {
          "BTCUSDT","COTIUSDT",
         "ETHUSDT",  "LTCUSDT",
         "GALAUSDT", "EOSUSDT",
         "AVAXUSDT", "SOLUSDT",
         "LINKUSDT",
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
      cycleLastElapsed = DateTime.Now;
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

    #region ResetGeneration

    bool canResetGeneration = true;
    private void ResetGeneration()
    {
      Task.Run(async () =>
      {
        try
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

              var mod = Cycle % TrainingSession.SymbolsToTest.Count;

              Cycle = Cycle - mod;

              BuyBotManager.NeatAlgorithm.GenomeList.OfType<NeatGenome>().ForEach(x => x.fitnesses.Clear());
              SellBotManager.NeatAlgorithm.GenomeList.OfType<NeatGenome>().ForEach(x => x.fitnesses.Clear());

              DistributeGeneration(Cycle);
            });

            Observable.Timer(TimeSpan.FromSeconds(10))
           .Subscribe(x => canResetGeneration = true)
           .DisposeWith(this);
          }
        }
        catch (Exception ex)
        {
          Logger.Log(ex);
        }
      });
    }

    #endregion

    #region DistributeGeneration

    DateTime generationStart;

    private ServerRunData serverRunData;
    private object dbaton = new object();
    SemaphoreSlim semaphoreSlimDistribute = new SemaphoreSlim(1, 1);

    private SerialDisposable serialDisposable1 = new SerialDisposable();

    Random random = new Random();

    private async void DistributeGeneration(int? generation = null)
    {
      try
      {
        await semaphoreSlimDistribute.WaitAsync();
        serialDisposable1.Disposable?.Dispose();

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

        Clients.ForEach(x => { x.Done = false; x.ErrorCount = 0; x.ReceivedData = false; });


        if (Clients.Any(x => x.PopulationSize == 0))
        {
          foreach (var client in Clients)
          {
            client.PopulationSize = AgentCount / Clients.Count;
          }
        }


        var ordered = Clients.OrderBy(x => x.LastGenerationTime).ToList();

        var first = ordered.First();
        var last = ordered.Last();

        TimeSpan difference = last.LastGenerationTime - first.LastGenerationTime;
        var sec = (int)(difference.TotalMilliseconds / 100);

        sec = Math.Min(sec, last.PopulationSize / 10);

        if (sec >= 1)
        {
          last.PopulationSize -= sec;
          first.PopulationSize += sec;
        }

        int maxTake = 720;
        int randomStartIndex = 0;

        if (maxTake > 0)
        {
          var dailyCandles = SimulationTradingBot
            .GetIndicatorData(
            new TimeFrameData() { Name = "1D", TimeFrame = CTKS_Chart.Trading.TimeFrame.D1 },
            SimulationPromptViewModel.GetAsset(CurrentSymbol, Minutes.ToString()))
            .Where(x => x.IndicatorData.RangeFilter.HighTarget > 0)
            .ToList();

          var selectedCandles = dailyCandles;

          int maxStartIndex = dailyCandles.Count - maxTake;
          randomStartIndex = random.Next(0, maxStartIndex + 1);
        }

        var tasks = new List<Task>();

        foreach (var client in Clients.ToList())
        {
          var newData = new ServerRunData()
          {
            AgentCount = client.PopulationSize,
            Generation = Cycle,
            IsRandom = false,
            Minutes = Minutes,
            MaxTake = maxTake,
            StartIndex = randomStartIndex,
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


          tasks.Add(Task.Run(() => TCPHelper.SendMessage(client.Client, MessageContract.GetDataMessage(message))));
        }


        await Task.WhenAll(tasks);

        serialDisposable1.Disposable = Observable.Interval(TimeSpan.FromSeconds(5)).Subscribe(async (x) =>
        {
          try
          {
            foreach (var client in Clients.Where(x => !x.ReceivedData))
            {
              if (!messages.Any())
              {
                TCPHelper.SendMessage(client.Client, MessageContract.Done);
              }
              else if (messages.TryGetValue(client, out var message))
              {
                TCPHelper.SendMessage(client.Client, MessageContract.GetDataMessage(message));
                Logger.Log(MessageType.Warning, $"Resending data NO HANDSHAKE");
              }
              else
              {
                Logger.Log(MessageType.Warning, $"NO CACHED DATA!");
                ResetGeneration();
              }
            }
          }
          catch (Exception ex)
          {
            Logger.Log(ex);
          }
        });


        Logger.Log(MessageType.Inform, $"Generation {BuyBotManager.Generation} - {CurrentSymbol} distributed");
      }
      finally
      {
        semaphoreSlimDistribute.Release();
      }
    }

    #endregion

    #region UpdateGeneration

    NeatGenome bestBuyGenome;
    NeatGenome bestSellGenome;

    private async void UpdateGeneration(IEnumerable<ClientData> runData)
    {
      try
      {

        await semaphoreSlimDistribute.WaitAsync();

        lock (this)
        {
          GenerationRunTime = DateTime.Now - generationStart;

          var isLastSymbol = CurrentSymbol == TrainingSession.SymbolsToTest.Last().Name;

          var bestBuy = BuyBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).First();
          var bestSell = SellBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).First();

          var index = TrainingSession.SymbolsToTest.IndexOf((x) => x.Name == serverRunData.Symbol);

          if (index != null)
          {
            if (bestBuyGenome != null)
            {
              var bestClient = runData.FirstOrDefault(x => x.GenomeData.Any(x => x.BuyGenomeId == bestBuyGenome.Id));
              var bestRun = bestClient.GenomeData.FirstOrDefault(x => x.BuyGenomeId == bestBuyGenome.Id);

              Logger.Log(MessageType.Inform, $"{bestBuyGenome.Id} - Genome ID {bestClient.Symbol} - {bestRun.TotalValue.ToString("N2")} $");

              TrainingSession.AddValue(serverRunData.Symbol, Statistic.AverageFitness, bestClient.Average);
              TrainingSession.AddValue(serverRunData.Symbol, Statistic.BestFitness, bestRun.Fitness);
              TrainingSession.AddValue(serverRunData.Symbol, Statistic.OriginalFitness, bestRun.OriginalFitness);
              TrainingSession.AddValue(serverRunData.Symbol, Statistic.TotalValue, bestRun.TotalValue);
              TrainingSession.AddValue(serverRunData.Symbol, Statistic.Drawdawn, bestRun.Drawdawn);
              TrainingSession.AddValue(serverRunData.Symbol, Statistic.NumberOfTrades, bestRun.NumberOfTrades);

              var fullCycle = Cycle % TrainingSession.SymbolsToTest.Count == 0;

              var aIBotRunner = new AIBotRunner(Logger, ViewModelsFactory);

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

              bestBuyGenome = BuyBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).FirstOrDefault();
              bestSellGenome = SellBotManager.NeatAlgorithm.GenomeList.OrderByDescending(x => x.Fitness).FirstOrDefault();

              TrainingSession.AddValue(serverRunData.Symbol, Statistic.MedianFitness, (decimal)bestBuyGenome.Fitness);

              Logger.Log(MessageType.Inform, $"Mean fitness: {bestBuyGenome.Fitness}");

              SimulationAIPromptViewModel.SaveGeneration(BuyBotManager, TrainingSession.Name, generation, "BUY.txt", "MEDIAN_BUY.txt");
              SimulationAIPromptViewModel.SaveGeneration(SellBotManager, TrainingSession.Name, generation, "SELL.txt", "MEDIAN_SELL.txt");

              BuyBotManager.ResetFitness();
              SellBotManager.ResetFitness();

              StartTest(generation);
            }
          }

          Cycle++;
          FinishedCount = 0;
          ToStart = AgentCount;

          runResults.Clear();
        }
      }
      finally
      {
        semaphoreSlimDistribute.Release();
      }

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
          Span<byte> buffer = new byte[MessageContract.BUFFER_SIZE_CLIENT_CACHE];

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
                string currentData = Encoding.UTF8.GetString(ms.ToArray());
                messageBuilder.Append(currentData);

                // Reset the memory stream for the next chunk
                ms.SetLength(0);

                var message = messageBuilder.ToString();

                if (message.Trim() == MessageContract.Error)
                {
                  Logger.Log(MessageType.Warning, "Error from client");

                  client.ReceivedData = false;
                  TCPHelper.SendMessage(client.Client, MessageContract.GetDataMessage(messages[client]));
                  messageBuilder.Clear();

                  continue;
                }

                // Here is where you handle message boundary detection
                if (MessageContract.IsDataMessage(message))
                {
                  // Process complete message
                  ProcessMessage(client, message);

                  // Clear the builder after processing a full message
                  messageBuilder.Clear();
                }
                else
                {
                  // If message is not complete, keep buffering
                  continue;
                }
              }
            }
            catch (Exception ex)
            {
              ms = new MemoryStream();
              messageBuilder.Clear();
              stream.Flush();

              client.ErrorCount++;

              if (client.ErrorCount > errorThreshold)
              {
                client.ReceivedData = false;

                Logger.Log(MessageType.Warning, "Resetting HANDSHAKE");
                client.ErrorCount = 0;
              }

              Logger.Log(MessageType.Error, $"ERROR TRANSMITTING DATA!", false, false);
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

    int errorThreshold = 10;

    private void ProcessMessage(CloudClient client, string message)
    {
      message = MessageContract.GetDataMessageContent(message);
      var storedData = JsonSerializer.Deserialize<ServerRunData>(messages[client]);

      if (message.Contains(MessageContract.Handshake))
      {
        message = message.Replace(MessageContract.Handshake, "");

        if (!client.ReceivedData)
        {
          client.ReceivedData = message == storedData.Symbol;

          if (!client.ReceivedData)
          {
            client.ErrorCount++;

            if (client.ErrorCount > errorThreshold)
            {
              client.ErrorCount = 0;
              ResetGeneration();
            }
          }
          else
          {
            client.ErrorCount = 0;
          }
        }

        return;
      }

      var data = JsonSerializer.Deserialize<ClientData>(message);
      if (!client.Done && data.Symbol == storedData.Symbol && client.ReceivedData)
      {
        UpdateManager(client, data.GenomeData, BuyBotManager, SellBotManager);

        client.ErrorCount = 0;

        if (client.Done)
        {
          runResults.Add(data);
          Logger.Log(MessageType.Inform2, "SUCESSFULL GENOME UPDATE");
          TCPHelper.SendMessage(client.Client, MessageContract.Finished);
        }

        if (IsGenerationEnded())
        {
          VSynchronizationContext.InvokeOnDispatcher(() =>
          {
            UpdateGeneration(runResults);
          });
        }
      }
      else
      {
        if (!client.Done)
        {
          client.ErrorCount++;

          if (client.ErrorCount > errorThreshold)
          {
            client.ReceivedData = false;

            client.ErrorCount = 0;
            Logger.Log(MessageType.Warning, "Reseting HANDSHAKE");
          }
        }
      }
    }

    #endregion

    #region IsGenerationEnded

    public bool IsGenerationEnded()
    {
      return Clients.All(x => x.Done && x.ReceivedData) && Clients.Count > 0;
    }

    #endregion

    #region UpdateManager

    object managerBatton = new object();

    private void UpdateManager(
      CloudClient tcpClient,
      IList<CloudComputing.Domains.RunData> runDatas,
      NEATManager<AIBot> buyManger,
      NEATManager<AIBot> sellManager)
    {
      lock (managerBatton)
      {
        try
        {
          if (buyManger.NeatAlgorithm == null)
          {
            TCPHelper.SendMessage(tcpClient.Client, MessageContract.Done);
            return;
          }

          foreach (var receivedGenome in runDatas)
          {
            var existingBuy = buyManger.NeatAlgorithm.GenomeList.SingleOrDefault(x => x.Id == receivedGenome.BuyGenomeId);
            var existingSell = sellManager.NeatAlgorithm.GenomeList.SingleOrDefault(x => x.Id == receivedGenome.SellGenomeId);

            if (existingBuy != null && existingSell != null)
            {
              if (!tcpClient.SentBuyGenomes[existingBuy.Id] && !tcpClient.SentSellGenomes[existingSell.Id])
              {
                existingBuy.AddSequentialFitness((float)receivedGenome.Fitness);
                existingSell.AddSequentialFitness((float)receivedGenome.Fitness);

                var fitnesses = existingBuy.fitnesses.Where(x => x > 0).ToList();

                if (fitnesses.GroupBy(x => x).Any(g => g.Count() > 1))
                {
                  Logger.Log(MessageType.Warning, "SAME FITNESSES FOR DIFFERENT SYMBOLS!");

                  foreach (var fitness in fitnesses)
                  {
                    Logger.Log(MessageType.Warning, fitness.ToString());
                  }
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
              Logger.Log(MessageType.Error, $"NOT FOUND GENOME WITH ID: {receivedGenome.BuyGenomeId}", true);

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
        catch (Exception ex)
        {
          Logger.Log(ex);
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

    SemaphoreSlim testSemaphore = new SemaphoreSlim(1, 1);

    private void StartTest(string generation)
    {
      VSynchronizationContext.InvokeOnDispatcher(async () =>
      {
        try
        {
          await testSemaphore.WaitAsync();

          var symbolsToTest = new string[] {
            "ADAUSDT",
            "MATICUSDT",
            "BNBUSDT",
            "ALGOUSDT"};

          var fitness = new List<float>();

          var buyBotManager_1 = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Buy);
          var bellBotManager_1 = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Sell);

          var aiPath = @$"Trainings\{TrainingSession.Name}\{generation}\MEDIAN_BUY.txt";
          var buy = aiPath.Replace("SELL", "BUY");
          var sell = aiPath.Replace("BUY", "SELL");

          buyBotManager_1.LoadBestGenome(buy);
          bellBotManager_1.LoadBestGenome(sell);

          buyBotManager_1.InitializeManager(1);
          bellBotManager_1.InitializeManager(1);

          var buyG = buyBotManager_1.NeatAlgorithm.GenomeList[0];
          var sellG = bellBotManager_1.NeatAlgorithm.GenomeList[0];

          foreach (var symbol in symbolsToTest)
          {
            var aIBotRunner = new AIBotRunner(Logger, ViewModelsFactory);

            await aIBotRunner.RunGeneration(
              1,
              Minutes,
              symbol,
              false,
              0,
              0,
              new List<NeatGenome>() { new NeatGenome(buyG, buyG.Id, 0) },
              new List<NeatGenome>() { new NeatGenome(sellG, sellG.Id, 0) }
              );

            var neat = aIBotRunner.Bots[0].TradingBot.Strategy.BuyAIBot.NeuralNetwork;

            fitness.Add(neat.Fitness);

            var strategy = aIBotRunner.Bots[0].TradingBot.Strategy;

            Logger.Log(MessageType.Inform,
              $"{aIBotRunner.Bots[0].Asset.Symbol} - " +
              $"{neat.Fitness} - " +
              $"({strategy.MaxDrawdawnFromMaxTotalValue.ToString("N2")} %, " +
              $"{strategy.TotalValue.ToString("N2")} $)");

            neat.ResetFitness();
          }

          var meanFitness = MathHelper.GeometricMean(fitness);

          TrainingSession.AddValue(CurrentSymbol, Statistic.BackTestMean, (decimal)meanFitness);
          Logger.Log(MessageType.Inform, $"MEAN - {meanFitness}");

        }
        finally
        {
          testSemaphore.Release();
        }
      });
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

