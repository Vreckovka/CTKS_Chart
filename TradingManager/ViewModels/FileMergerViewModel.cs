using CTKS_Chart.Trading;
using CTKS_Chart.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TradingManager.Providers;
using TradingManager.Views;
using VCore.Standard.Helpers;
using VCore.WPF.Modularity.RegionProviders;
using VCore.WPF.ViewModels;

namespace TradingManager.ViewModels
{
  public class FileMergerViewModel : RegionViewModel<FileMergerView>
  {
    public FileMergerViewModel(IRegionProvider regionProvider) : base(regionProvider)
    {
      MergeFiles();
    }

    public override string RegionName { get; protected set; } = RegionNames.Content;

    public DrawingViewModel DrawingViewModel { get; set; } = new DrawingViewModel();

    public override string Header => "Files merger";

    public void MergeFiles()
    {
      var source = @"D:\Aplikacie\Skusobne\CTKS_Chart\TradingManager\bin\Debug\netcoreapp3.1\Data";

      var files = Directory.GetFiles(source, "*BINANCE_ADAUSDT, 240*");

      List<Candle> allCandles = new List<Candle>();

      foreach(var file in files)
      {
        var candles = TradingViewHelper.ParseTradingView(TimeFrame.H4, file, "ADAUSDT");

        allCandles.AddRange(candles);
      }


      var finalCandles = allCandles.DistinctBy(x => x.OpenTime).OrderBy(x => x.OpenTime).ToList();

      DrawingViewModel.ActualCandles = finalCandles;
    }
  }
}
