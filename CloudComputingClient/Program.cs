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

    public static bool Connected { get; set; }
    static RunData LastData { get; set; }

    static string serverIp;
    static int port;

    static ServerRunData ServerRunData { get; set; }
    static ServerRunData LastServerRunData { get; set; }

    static AIBotRunner aIBotRunner { get; set; }

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

        SetupExceptionHandling();

        ViewModelsFactory = Kernel.Get<IViewModelsFactory>();
        Logger = Kernel.Get<ILogger>();

        TradingViewHelper.DebugFlag = true;

        aIBotRunner = new AIBotRunner(Logger, ViewModelsFactory);

        aIBotRunner.OnGenerationCompleted += (x, y) => GenerationCompleted();

        var add = JsonSerializer.Deserialize<ServerAdress>(File.ReadAllText("server.txt"));

        serverIp = add.IP.Trim();
#if DEBUG
        // serverIp = "127.0.0.1";
#endif
        port = add.Port;

        UpdateUI();
        ConnectToServer();
        ListenToServer();

        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentCulture = new CultureInfo("en-US");

        Observable.Interval(TimeSpan.FromSeconds(5)).Subscribe((x) =>
        {
          if (!IsClientConnected(tcpClient))
          {
            ConnectToServer();
            ListenToServer();
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
        Logger.Log(ex);
      }
    }

    #endregion

    #region SetupExceptionHandling

    private static void SetupExceptionHandling()
    {
      AppDomain.CurrentDomain.UnhandledException += (s, e) => Logger.Log((Exception)e.ExceptionObject);

      TaskScheduler.UnobservedTaskException += (s, e) =>
      {
        Logger.Log(e.Exception);
        e.SetObserved();
      };
    }

    #endregion

    #region GenerationCompleted

    static SerialDisposable sendResultSerialDisposable = new SerialDisposable();

    private static void GenerationCompleted()
    {
      try
      {
        var aIStrategy = aIBotRunner.Bots.OrderByDescending(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork.Fitness).First().TradingBot.Strategy;

        SendResult();

        sendResultSerialDisposable.Disposable = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe((x) =>
        {
          SendResult();

          Logger.Log(MessageType.Inform2, "RESULTS SENDED");
        });


        UpdateUI();
      }
      catch (Exception ex)
      {
        Logger.Log(ex);
      }
    }

    #endregion

    #region SendResult

    static object resultLock = new object();
    private static void SendResult()
    {
      lock (resultLock)
      {
        try
        {
          var clientData = new ClientData();

          var bots = aIBotRunner.Bots.ToList();

          foreach (var bot in bots)
          {
            var data = new RunData();
            var strat = bot.TradingBot.Strategy;

            var buyGenome = (NeatGenome)strat.BuyAIBot.NeuralNetwork;
            var sellGenome = (NeatGenome)strat.SellAIBot.NeuralNetwork;

            data.BuyGenomeId = buyGenome.Id;
            data.SellGenomeId = sellGenome.Id;

            data.Drawdawn = strat.MaxDrawdawnFromMaxTotalValue;
            data.TotalValue = strat.TotalValue;
            data.NumberOfTrades = strat.ClosedSellPositions.Count;
            data.OriginalFitness = (decimal)strat.OriginalFitness;
            data.Fitness = (decimal)strat.BuyAIBot.NeuralNetwork.Fitness;

            clientData.GenomeData.Add(data);
          }

          var buyGenomes = bots.Select(x => x.TradingBot.Strategy.BuyAIBot.NeuralNetwork).OfType<NeatGenome>().ToList();

          clientData.Average = (decimal)buyGenomes.Average(x => x.Fitness);

          clientData.Symbol = bots[0].Asset.Symbol;

          SendMessage(tcpClient, clientData);

          LastData = clientData.GenomeData.OrderByDescending(x => x.Fitness).FirstOrDefault();
        }
        catch (SocketException ex)
        {
          Logger.Log(ex);
          CloseClient();
        }
        catch (Exception ex)
        {
          Logger.Log(ex);
        }
      }
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

          tcpClient.ReceiveBufferSize = MessageContract.BUFFER_SIZE_CLIENT;
          tcpClient.SendBufferSize = MessageContract.BUFFER_SIZE_CLIENT;

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
    static Thread clientThread;

    private static void ListenToServer()
    {
      try
      {
        clientThread?.Abort();

        clientThread = new Thread(() => HandleIncomingMessage());
        clientThread.IsBackground = true;
        clientThread.Start();
      }
      catch (Exception ex)
      {
        Logger.Log(ex);
      }
    }

    #endregion

    #region HandleIncomingMessage

    static object batton = new object();

    private static void HandleIncomingMessage()
    {
      try
      {
        NetworkStream stream = tcpClient?.GetStream();

        if (stream == null)
          return;

        Span<byte> buffer = new byte[MessageContract.BUFFER_SIZE_CLIENT_CACHE];
        MemoryStream ms = new MemoryStream();
        int bytesRead;
        StringBuilder messageBuilder = new StringBuilder();

        // Read the stream until all data is received.
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

              if (currentData.Contains(MessageContract.Finished))
              {
                aIBotRunner.Bots.ForEach(x => x.Stop());
                sendResultSerialDisposable.Disposable?.Dispose();

                ClearMessage(ref ms, buffer, stream, messageBuilder);

                continue;
              }

              if (currentData.Contains(MessageContract.Done))
              {
                ClearMessage(ref ms, buffer, stream, messageBuilder);

                aIBotRunner.Bots.ForEach(x => x.Stop());

                continue;
              }


              // Check if the message contains the 'Done' marker to indicate it's complete
              if (MessageContract.IsDataMessage(messageBuilder.ToString()))
              {
                // Full message received, process it
                string completeMessage = messageBuilder.ToString();

                ClearMessage(ref ms, buffer, stream, messageBuilder);

                if (!completeMessage.Contains(MessageContract.Handshake))
                  // Process the full message
                  ProcessMessage(completeMessage);

                continue;
              }
            }
          }
          catch (SocketException ex)
          {
            Logger.Log(ex);
            CloseClient();
          }
          catch (Exception ex)
          {
            ClearMessage(ref ms, buffer, stream, messageBuilder);

            sendResultSerialDisposable.Disposable?.Dispose();
            try
            {
              TCPHelper.SendMessage(tcpClient, MessageContract.Error);
            }
            catch (Exception)
            {
            }

            Logger.Log(ex);
          }
        }
      }
      catch (Exception ex)
      {
        sendResultSerialDisposable.Disposable?.Dispose();
        Connected = false;
        CloseClient();
        Logger.Log(ex);
        Console.WriteLine("DISCONNECTED!");
      }
    }

    #endregion

    #region ClearMessage

    private static void ClearMessage(
      ref MemoryStream ms,
      Span<byte> buffer,
      NetworkStream stream,
      StringBuilder messageBuilder)
    {
      ms = new MemoryStream();
      messageBuilder.Clear();
      buffer.Clear();
      stream.Flush();
    }

    #endregion

    #region ProcessMessage

    private static async void ProcessMessage(string message)
    {
      sendResultSerialDisposable.Disposable?.Dispose();
      var split = MessageContract.GetDataMessageContent(message);
      var serverRunData = JsonSerializer.Deserialize<ServerRunData>(split);

      if (serverRunData != null)
      {
        LastServerRunData = ServerRunData;
        ServerRunData = serverRunData;

        var buyManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Buy);
        var sellManager = SimulationAIPromptViewModel.GetNeatManager(ViewModelsFactory, PositionSide.Sell);

        var buyGenomes = buyManager.GetGeneration(serverRunData.BuyGenomes);
        var sellGenomes = sellManager.GetGeneration(serverRunData.SellGenomes);

        UpdateUI();

        _ = aIBotRunner.RunGeneration(
                          ServerRunData.AgentCount,
                          ServerRunData.Minutes,
                          ServerRunData.Symbol,
                          ServerRunData.MaxTake,
                          ServerRunData.StartIndex,
                          buyGenomes,
                          sellGenomes);

        UpdateUI();

        for (int i = 0; i < 2; i++)
        {
          try
          {
            TCPHelper.SendMessage(tcpClient, MessageContract.GetDataMessage(MessageContract.Handshake + serverRunData.Symbol));

            await Task.Delay(500);
          }
          catch (SocketException ex)
          {
            Logger.Log(ex, false, false);
            CloseClient();
          }
        }
      }
    }

    #endregion

    #region SendMessage

    static object batton0 = new object();
    private static void SendMessage(TcpClient tcpClient, ClientData runData)
    {
      try
      {
        lock (batton0)
        {
          var _stream = tcpClient?.GetStream();

          if (_stream != null)
            TCPHelper.SendMessage(tcpClient, MessageContract.GetDataMessage(JsonSerializer.Serialize(runData)));
        }
      }
      catch (SocketException ex)
      {
        CloseClient();
        Logger.Log(ex);
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
      sendResultSerialDisposable.Disposable?.Dispose();
      clientThread?.Abort();
    }

    #endregion

    #region UpdateUI

    private static void UpdateUI()
    {
      Console.Clear();

      Console.WriteLine($"Generation: {ServerRunData?.Generation ?? -1}");
      Console.WriteLine($"Run Time: {aIBotRunner.RunTime.ToString(@"hh\:mm\:ss")}");

      Console.WriteLine();
      Console.WriteLine($"Symbol: {ServerRunData?.Symbol}");
      Console.WriteLine($"Minutes: {ServerRunData?.Minutes}");
      Console.WriteLine($"Max Take: {ServerRunData?.MaxTake}");
      Console.WriteLine($"Start index: {ServerRunData?.StartIndex}");
      Console.WriteLine();
      Console.WriteLine($"To Start: {aIBotRunner.ToStart} In Progress: {aIBotRunner.InProgress} Finished: {aIBotRunner.FinishedCount}");


      Console.WriteLine();
      Console.WriteLine($"----LAST GENERATION----");
      Console.WriteLine($"GEN.Run Time: {aIBotRunner.GenerationRunTime.ToString(@"hh\:mm\:ss")}");
      Console.WriteLine($"Symbol: {LastServerRunData?.Symbol}");
      Console.WriteLine($"Minutes: {LastServerRunData?.Minutes}");
      Console.WriteLine($"Max Take: {LastServerRunData?.MaxTake}");
      Console.WriteLine($"Start index: {LastServerRunData?.StartIndex}");

      Console.WriteLine();
      Console.WriteLine($"BEST Fitness: {LastData?.Fitness.ToString("N2")}");
      //Console.WriteLine($"AVG.Fitness: {LastData?.Average.ToString("N2")}");
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
