using CloudComputing.Domains;
using CTKS_Chart.Strategy;
using CTKS_Chart.Strategy.AIStrategy;
using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using Logger;
using Ninject;
using SharpNeat.Genomes.Neat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VNeuralNetwork;
using RunData = CloudComputing.Domains.RunData;

namespace CloudComputingClient
{
  class Program
  {
    #region Fields

    static IKernel Kernel { get; set; }
    static IViewModelsFactory ViewModelsFactory { get; set; }


    static List<SimulationTradingBot<AIPosition, AIStrategy>> Bots { get; set; } = new List<SimulationTradingBot<AIPosition, AIStrategy>>();

    static int ToStart { get; set; }
    static int InProgress { get; set; }
    static int FinishedCount { get; set; }
    static TimeSpan RunTime { get; set; }
    static TimeSpan GenerationRunTime { get; set; }

    static DateTime lastElapsed;
    static DateTime generationStart;
    static Random random = new Random();

    public static bool Connected { get; set; }
    static RunData LastData { get; set; }

    static string serverIp;
    static int port;

    static ServerRunData ServerRunData { get; set; }
    static ServerRunData LastServerRunData { get; set; }

    static NEATManager<AIBot> buyManager;
    static NEATManager<AIBot> sellManager;

    static ILogger Logger { get; set; }

    #endregion

    #region Methods

    #region Main

    static void Main(string[] args)
    {
      try
      {
        Kernel = new StandardKernel();

        Kernel.Bind<IViewModelsFactory>().To<BaseViewModelsFactory>();
        Kernel.Bind<ILoggerContainer>().To<Logger.ConsoleLogger>();
        Kernel.Bind<ILogger>().To<Logger.Logger>();
        Kernel.Bind<IWindowManager>().To<WindowManager>();

        ViewModelsFactory = Kernel.Get<IViewModelsFactory>();
        Logger = Kernel.Get<ILogger>();

        TradingViewHelper.DebugFlag = true;

        buyManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Buy);
        sellManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Sell);

        buyManager.InitializeManager(1);
        sellManager.InitializeManager(1);

        lastElapsed = DateTime.Now;

        var add = JsonSerializer.Deserialize<ServerAdress>(File.ReadAllText("server.txt"));

        serverIp = add.IP.Trim();
#if DEBUG
        serverIp = "127.0.0.1";
#endif
        port = add.Port;

        UpdateUI();
        ListenToServer(ConnectToServer());

        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        Observable.Interval(TimeSpan.FromSeconds(5)).Subscribe((x) =>
        {
          if (!IsClientConnected(tcpClient))
          {
            ListenToServer(ConnectToServer());
          }

          UpdateUI();
        });

        string input;

        do
        {
          Logger.Log(MessageType.Inform, "Type 'exit' to quit the program:");
          input = Console.ReadLine();
        } while (input != "exit");
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
      }
    }

    #endregion

    #region RunGeneration

    private static void RunGeneration(
      int agentCount,
      int minutes,
      double split,
      string symbol,
      bool isRandom,
      List<NeatGenome> buyGenomes,
      List<NeatGenome> sellGenomes)
    {
      serialDisposable.Disposable?.Dispose();

      CreateStrategies(agentCount, minutes, split, symbol, isRandom, buyGenomes, sellGenomes);

    }

    #endregion

    #region CreateStrategies

    private static void CreateStrategies(
      int agentCount,
      int minutes,
      double splitTake,
      string symbol,
      bool isRandom,
      List<NeatGenome> buyGenomes,
      List<NeatGenome> sellGenomes)
    {
      Bots.Clear();

      ToStart = 0;


      for (int i = 0; i < agentCount; i++)
      {
        buyGenomes[i].InputCount = buyGenomes[i].NodeList.Count(x => x.NodeType == SharpNeat.Network.NodeType.Input);
        sellGenomes[i].InputCount = sellGenomes[i].NodeList.Count(x => x.NodeType == SharpNeat.Network.NodeType.Input);


        var bot = SimulationAIPromptViewModel.GetBot(
          symbol,
          new AIBot(buyGenomes[i]),
          new AIBot(sellGenomes[i]),
          minutes,
          splitTake,
          random,
          ViewModelsFactory,
          Logger,
          isRandom);

        ToStart++;

        Bots.Add(bot);
      }


      RunBots(random, symbol, isRandom, splitTake, minutes);
    }

    #endregion

    #region GenerationCompleted
    static SerialDisposable serialDisposable = new SerialDisposable();

    private static void GenerationCompleted()
    {
      GenerationRunTime = DateTime.Now - generationStart;

      Bots.ForEach(x =>
      {
        SimulationAIPromptViewModel.AddFitness(x.TradingBot.Strategy);
      });

      var aIStrategy = Bots.OrderByDescending(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness).First().TradingBot.Strategy;

      SendResult();

      serialDisposable.Disposable = Observable.Interval(TimeSpan.FromSeconds(5)).Subscribe((x) =>
      {
        SendResult();
      });

      FinishedCount = 0;

      UpdateUI();
    }

    #endregion

    #region SendResult

    static object resultLock = new object();
    private static void SendResult()
    {
      lock (resultLock)
      {
        var data = new RunData();

        var best = Bots.OrderByDescending(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness).First().TradingBot.Strategy;

        var buyGenomes = Bots.Select(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork).OfType<NeatGenome>().ToList();
        var sellGenomes = Bots.Select(x => x.TradingBot.Strategy.SellAIBot.NeuralNetwork).OfType<NeatGenome>().ToList();

        data.BuyGenomes = NeatGenomeXmlIO.SaveComplete(buyGenomes, false).OuterXml;
        data.SellGenomes = NeatGenomeXmlIO.SaveComplete(sellGenomes, false).OuterXml;

        data.Average = (decimal)buyGenomes.Average(x => x.Fitness);
        data.Drawdawn = best.MaxDrawdawnFromMaxTotalValue;
        data.TotalValue = best.TotalValue;
        data.NumberOfTrades = best.ClosedSellPositions.Count;
        data.OriginalFitness = (decimal)best.OriginalFitness;
        data.Fitness = (decimal)best.BuyAIBot.NeuralNetwork.Fitness;

        SendMessage(tcpClient, data);

        LastData = data;
      }
    }

    #endregion

    #region RunBots

    static object batton = new object();
    public static void RunBots(
              Random random,
              string symbol,
              bool useRandomDate,
              double pSpliTake,
              int minutes)
    {
      generationStart = DateTime.Now;
      Task.Run(() =>
      {
        DateTime fromDate = DateTime.Now;
        double splitTake = 0;

        if (useRandomDate)
        {
          var year = random.Next(2019, 2023);
          fromDate = new DateTime(year, random.Next(1, 13), random.Next(1, 25));
          splitTake = pSpliTake;
        }
        else
        {
          var asset = Bots.First().Asset;
          var dailyCandles = TradingViewHelper.ParseTradingView(TimeFrame.D1, $"Data\\Indicators\\{asset.IndicatorDataPath}, 1D.csv", asset.Symbol, saveData: true);

          //ignore filter starting values of indicators
          fromDate = dailyCandles.First(x => x.IndicatorData.RangeFilterData.HighTarget > 0).CloseTime.AddDays(30);
        }

        var allCandles = SimulationTradingBot.GetSimulationCandles(
           minutes,
           SimulationPromptViewModel.GetSimulationDataPath(symbol, minutes.ToString()), symbol, fromDate);

        var simulateCandles = allCandles.cutCandles;
        var candles = allCandles.candles;
        var mainCandles = allCandles.allCandles;

        foreach (var bot in Bots)
        {
          if (splitTake != 0)
          {
            var take = (int)(mainCandles.Count / splitTake);

            simulateCandles = simulateCandles.Take(take).ToList();
          }

          bot.InitializeBot(simulateCandles);
          bot.HeatBot(simulateCandles, bot.TradingBot.Strategy);
        }

        ToStart = Bots.Count;

        var min = Math.Min(Bots.Count, 10);

        var splitTakeC = Bots.SplitList(Bots.Count / min);
        var tasks = new List<Task>();

        foreach (var take in splitTakeC)
        {
          tasks.Add(Task.Run(() =>
          {
            lock (batton)
            {
              ToStart -= take.Count;
              InProgress += take.Count;
            }

            foreach (var candle in simulateCandles)
            {
              if (take.Any(x => x.stopRequested))
                return;

              foreach (var bot in take)
              {
                bot.SimulateCandle(candle);
              }
            }

            lock (batton)
            {
              FinishedCount += take.Count;
              InProgress -= take.Count;
            }
          }));
        }

        Task.WaitAll(tasks.ToArray());

        GenerationCompleted();
      });
    }

    #endregion

    #region ConnectToServer

    private static TcpClient ConnectToServer()
    {
      try
      {
        if (!IsClientConnected(tcpClient))
        {
          tcpClient = new TcpClient(serverIp, port);

          Logger.Log(MessageType.Inform2, $"Connectted to: {serverIp}\n");
        }

        return tcpClient;
      }
      catch (Exception ex)
      {
        Logger.Log(ex, false, false);
        return null;
      }
    }

    #endregion

    #region IsClientConnected

    private static bool IsClientConnected(TcpClient client)
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

    #region ListenToServer

    static TcpClient tcpClient;

    private static void ListenToServer(TcpClient client)
    {
      var clientThread = new Thread(() => HandleIncomingMessage(client));
      clientThread.IsBackground = true;
      clientThread.Start();
    }

    #endregion

    #region HandleIncomingMessage

    private static void HandleIncomingMessage(TcpClient tcpClient)
    {
      try
      {
        NetworkStream stream = tcpClient?.GetStream();

        if (stream == null)
          return;

        Span<byte> buffer = new byte[MessageContract.BUFFER_SIZE];
        MemoryStream ms = new MemoryStream();


        int bytesRead;
        // Read the stream until all data is received.
        while ((bytesRead = stream.Read(buffer)) > 0)
        {
          try
          {
            serialDisposable.Disposable?.Dispose();
            ms.Write(buffer);

            string currentData = Encoding.Unicode.GetString(ms.ToArray());

            if (currentData.Contains(MessageContract.Done))
            {
              ms = new MemoryStream();
              continue;
            }

            if (!currentData.Contains(MessageContract.EndOfMessage))
            {
              continue;
            }


            var split = currentData.Split(MessageContract.EndOfMessage);

            var serverRunData = JsonSerializer.Deserialize<ServerRunData>(split[0]);

            if (serverRunData != null)
            {
              LastServerRunData = ServerRunData;
              ServerRunData = serverRunData;
            }

            if (ServerRunData != null)
            {
              Task.Run(() =>
              {
                List<NeatGenome> buyGenomes = new List<NeatGenome>();
                List<NeatGenome> sellGenomes = new List<NeatGenome>();

                using (StringReader tx = new StringReader(ServerRunData.BuyGenomes.ToString().Replace("'", "\"")))
                using (XmlReader xr = XmlReader.Create(tx))
                {
                  // Replace NeatGenomeXmlIO.ReadCompleteGenomeList with your actual method to read genomes.
                  buyGenomes = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, buyManager.GetGenomeFactory());
                }

                using (StringReader tx = new StringReader(ServerRunData.SellGenomes.ToString().Replace("'", "\"")))
                using (XmlReader xr = XmlReader.Create(tx))
                {
                  // Replace NeatGenomeXmlIO.ReadCompleteGenomeList with your actual method to read genomes.
                  sellGenomes = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, sellManager.GetGenomeFactory());
                }

                foreach (var buyGene in buyGenomes)
                {
                  Debug.WriteLine($"RECEIVED IDs: {buyGene.Id}");
                }

                buyManager.NeatAlgorithm.UpdateNetworks(buyGenomes);
                sellManager.NeatAlgorithm.UpdateNetworks(sellGenomes);

                UpdateUI();

                RunGeneration(
                           ServerRunData.AgentCount,
                           ServerRunData.Minutes,
                           ServerRunData.Split,
                           ServerRunData.Symbol,
                           ServerRunData.IsRandom,
                           buyGenomes,
                           sellGenomes);

                UpdateUI();
              });
            }
          }
          catch (Exception ex)
          {
            TCPHelper.SendMessage(tcpClient, MessageContract.Error);
            ms = new MemoryStream();
            Logger.Log(ex);
            buffer.Clear();
          }
        }
      }
      catch (Exception ex)
      {
        Connected = false;
        CloseClient();
        Logger.Log(ex);
        Console.WriteLine("DISCONNECTED!");
      }
    }

    #endregion

    #region SendMessage

    static object batton0 = new object();
    static int asd = 0;
    private static void SendMessage(TcpClient tcpClient, RunData runData)
    {
      try
      {

        lock (batton0)
        {
          var _stream = tcpClient?.GetStream();
          Console.WriteLine($"SENDING {++asd}");

          if (_stream != null)
          {
            var json = JsonSerializer.Serialize(runData) + MessageContract.EndOfMessage;

            TCPHelper.SendMessage(tcpClient, json); 
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Log(ex);
      }
    }


    #endregion

    #region CloseClient

    private static void CloseClient()
    {
      tcpClient?.Close();
      tcpClient = null;
    }

    #endregion

    #region UpdateUI

    private static void UpdateUI()
    {
      Console.Clear();

      TimeSpan diff = DateTime.Now - lastElapsed;

      RunTime = RunTime.Add(diff);

      lastElapsed = DateTime.Now;

      Console.WriteLine($"Generation: {ServerRunData?.Generation ?? -1}");
      Console.WriteLine($"Run Time: {RunTime.ToString(@"hh\:mm\:ss")}"); 
      Console.WriteLine();

      Console.WriteLine($"Symbol: {ServerRunData?.Symbol}");
      Console.WriteLine($"Symbol: {ServerRunData?.Minutes}");
      Console.WriteLine();
      Console.WriteLine($"To Start: {ToStart} In Progress: {InProgress} Finished: {FinishedCount}");
      Console.WriteLine();


      Console.WriteLine($"----LAST GENERATION----");
      Console.WriteLine($"GEN.Run Time: {GenerationRunTime.ToString(@"hh\:mm\:ss")}");
      Console.WriteLine($"Symbol: {LastServerRunData?.Symbol}");
      Console.WriteLine($"Minutes: {LastServerRunData?.Minutes}");
      Console.WriteLine($"BEST Fitness: {LastData?.Fitness.ToString("N2")}");
      Console.WriteLine($"AVG.Fitness: {LastData?.Average.ToString("N2")}");
      Console.WriteLine($"ORG.Fitness: {LastData?.OriginalFitness.ToString("N2")}");
      Console.WriteLine();
      Console.WriteLine($"Total Value: {LastData?.TotalValue.ToString("N2")} $");
      Console.WriteLine($"Drawdawn: {LastData?.Drawdawn.ToString("N2")} %");
      Console.WriteLine($"Number of Trades: {LastData?.NumberOfTrades}");

      var connected = IsClientConnected(tcpClient);
      Console.WriteLine(connected ? $"Connected to: {serverIp}" : "Not connected...");
    }

    #endregion

    #endregion
  }
}
