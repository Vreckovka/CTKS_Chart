using ChromeDriverScrapper;
using CTKS_Chart.Binance;
using CTKS_Chart.Trading;
using Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using TradingManager.Providers;
using TradingManager.Views;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace TradingManager.ViewModels
{
  public class FilesManagerViewModel : RegionViewModel<FilesView>
  {
    private readonly IChromeDriverProvider chromeDriverProvider;
    private readonly IWindowManager windowsManager;
    private readonly ILogger logger;
    private readonly TradingViewDataProvider tradingViewDataProvider;

    public FilesManagerViewModel(
      IChromeDriverProvider chromeDriverProvider,
      IRegionProvider regionProvider,
      IWindowManager windowsManager,
      ILogger logger,
      BinanceBroker binanceBroker) : base(regionProvider)
    {
      this.chromeDriverProvider = chromeDriverProvider ?? throw new ArgumentNullException(nameof(chromeDriverProvider));
      this.windowsManager = windowsManager ?? throw new ArgumentNullException(nameof(windowsManager));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      tradingViewDataProvider = new TradingViewDataProvider(chromeDriverProvider);

      binanceBroker.GetSymbols();
    }

    public ObservableCollection<TradingViewFolderDataViewModel> Folders { get; set; } = new ObservableCollection<TradingViewFolderDataViewModel>();

    public override string RegionName { get; protected set; } = RegionNames.Content;

    public override string Header => "Files";

    #region LastChecked

    private DateTime lastChecked;

    public DateTime LastChecked
    {
      get { return lastChecked; }
      set
      {
        if (value != lastChecked)
        {
          lastChecked = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region LastUpdated

    private DateTime lastUpdated;

    public DateTime LastUpdated
    {
      get { return lastUpdated; }
      set
      {
        if (value != lastUpdated)
        {
          lastUpdated = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region UpdateTimeFramesCommand

    protected ActionCommand updateTimeFramesCommand;

    public ICommand UpdateTimeFramesCommand
    {
      get
      {
        return updateTimeFramesCommand ??= new ActionCommand(UpdateTimeframes).DisposeWith(this);
      }
    }


    #endregion

    #region CheckFilesCommand

    protected ActionCommand checkFilesCommand;

    public ICommand CheckFilesCommand
    {
      get
      {
        return checkFilesCommand ??= new ActionCommand(CheckFiles).DisposeWith(this);
      }
    }

    #endregion

    #region Methods

    #region Initialize

    public override void Initialize()
    {
      base.Initialize();
      CheckData();

      Observable.Interval(TimeSpan.FromMinutes(0.1)).Subscribe((x) =>
      {
        DateTime lastUtc = TimeZoneInfo.ConvertTimeToUtc(LastChecked, TimeZoneInfo.Local);

        if (DateTime.UtcNow.Date > lastUtc.Date && DateTime.UtcNow.TimeOfDay > TimeSpan.FromMinutes(1))
        {
          CheckData();
        }

#if DEBUG
        CheckData();
#endif
      });
    }

    #endregion

    #region CheckData

    private void CheckData()
    {
      VSynchronizationContext.InvokeOnDispatcher(CheckFiles);

      if (Folders.SelectMany(x => x.Files).Any(x => x.IsOutDated))
      {
        UpdateTimeframes();
      }
    }

    #endregion

    #region CheckFiles

    public void CheckFiles()
    {
      try
      {
        LastChecked = DateTime.Now;
        Folders.Clear();
        var folders = File.ReadAllLines("folders_to_check.txt");

        var allTimeFrames = EnumHelper.GetAllValuesAndDescriptions(typeof(TimeFrame));

        foreach (var path in folders)
        {
          if (Directory.Exists(path))
          {
            var files = Directory.GetFiles(path);
            var folderVm = new TradingViewFolderDataViewModel()
            {
              Path = path,
              Name = Directory.GetParent(Directory.GetParent(path).FullName).Name.ToUpper()
            };


            foreach (var file in files)
            {
              var fileName = Path.GetFileName(file);
              var nameSplit = fileName.Replace(".csv", null).Split(",");

              var symbolSplit = nameSplit[0].Split(" ");
              var timeframeText = nameSplit[1].Trim();

              var value = allTimeFrames.FirstOrDefault(x => x.Description == timeframeText);
              Enum.TryParse(value.Value.ToString(), out TimeFrame timeFrame);

              var vm = new TradingViewDataViewModel(windowsManager)
              {
                Path = file,
                Name = fileName,
                TimeFrame = timeFrame,
                TradingViewSymbol = new TradingViewSymbol()
                {
                  Provider = symbolSplit[0],
                  Symbol = symbolSplit[1]
                },
                Candles = TradingViewHelper.ParseTradingView(timeFrame, file)
              };

              vm.IsOutDated = TradingViewHelper.IsOutDated(vm.TimeFrame, vm.Candles);
              folderVm.Files.Add(vm);
            }

            Folders.Add(folderVm);
          }

        }
      }
      catch (Exception ex)
      {
        logger.Log(ex);
      }
    }
    #endregion

    #region UpdateTimeframes

    public async void UpdateTimeframes()
    {
      var outdatedFiles = Folders.SelectMany(x => x.Files).Where(x => x.IsOutDated).ToList();

      foreach (var outdated in outdatedFiles)
      {
        var timeFrame = EnumHelper.Description(outdated.TimeFrame);
        var newFilePath = await tradingViewDataProvider.DownloadTimeframe(outdated.TradingViewSymbol, timeFrame);
        chromeDriverProvider.Dispose();

        var newCandles = TradingViewHelper.ParseTradingView(outdated.TimeFrame, newFilePath);
        var oldCandles = TradingViewHelper.ParseTradingView(outdated.TimeFrame, outdated.Path);

        var addedCandles = newCandles
          .Where(x => !oldCandles.Any(y => y.OpenTime == x.OpenTime))
          .OrderBy(x => x.OpenTime)
          .ToList();

        string[] lines = File.ReadAllLines(newFilePath);

        foreach (var newCandle in addedCandles)
        {
          using (StreamWriter w = File.AppendText(outdated.Path))
          {
            w.WriteLine(lines[newCandle.FileLineIndex]);
          }
        }
      }

      VSynchronizationContext.InvokeOnDispatcher(CheckFiles);
    }

    #endregion

    #endregion
  }
}
