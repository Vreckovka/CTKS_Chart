using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using CTKS_Chart.ViewModels;
using VCore.Standard.Modularity.Interfaces;
using Control = System.Windows.Controls.Control;

#pragma warning disable 618

namespace CTKS_Chart.Views
{
  public class WindowPosition
  {
    public double Height { get; set; }
    public double Width { get; set; }
    public double Left { get; set; }
    public double Top { get; set; }
  }

  public partial class MainWindow : IView
  {
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }

    public static int SWP_NOSIZE = 1;

    [DllImport("kernel32")]
    static extern IntPtr GetConsoleWindow();


    [DllImport("user32")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
      int x, int y, int cx, int cy, int flags);


    private string positionPath = "position.json";
    public MainWindow()
    {
      InitializeComponent();

      Loaded += MainWindow_Loaded;
      Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
      var position = new WindowPosition();

      position.Left = Left;
      position.Top = Top;
      position.Height = ActualHeight;
      position.Width = ActualWidth;

      var handle = GetConsoleWindow();

      if (IntPtr.Zero != handle)
      {
        var console = new WindowPosition();

        RECT rct = new RECT();
        GetWindowRect(handle, ref rct);

        console.Left = rct.Left;
        console.Top = rct.Top;
        console.Height = Console.WindowHeight;
        console.Width = Console.WindowWidth;

        File.WriteAllText(positionPath, JsonSerializer.Serialize(new WindowPosition[] { position, console }));
      }
      else
      {
        File.WriteAllText(positionPath, JsonSerializer.Serialize(new WindowPosition[] { position }));
      }

      if (DataContext is MainWindowViewModel viewModel)
      {
        viewModel.TradingBotViewModel.SaveLayoutSettings();
      }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      if (File.Exists(positionPath))
      {
        var positions = JsonSerializer.Deserialize<WindowPosition[]>(File.ReadAllText(positionPath));

        var pos = positions[0];

        Left = pos.Left;
        Top = pos.Top;
        Width = pos.Width;
        Height = pos.Height;

        if (positions.Length > 1)
        {
          var con = positions[1];

          var handle = GetConsoleWindow();

          if (IntPtr.Zero != handle)
          {
            SetWindowPos(handle, IntPtr.Zero,
              (int)con.Left,
              (int)con.Top,
              (int)con.Width,
              (int)con.Height, SWP_NOSIZE);

            Console.WindowHeight = (int)con.Height;
            Console.WindowWidth = (int)con.Width;
            Console.BufferWidth = (int)con.Width;
          }
        }
      }
    }

    public void SortActualPositions()
    {
      SortDataGrid(ActualPositions,1);
      SortDataGrid(Loggs);
    }

    private void SortDataGrid(DataGrid dataGrid, int columnIndex = 0, ListSortDirection sortDirection = ListSortDirection.Descending)
    {
      var column = dataGrid.Columns[columnIndex];

      // Clear current sort descriptions
      dataGrid.Items.SortDescriptions.Clear();

      // Add the new sort description
      dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));

      // Apply sort
      foreach (var col in dataGrid.Columns)
      {
        col.SortDirection = null;
      }
      column.SortDirection = sortDirection;

      // Refresh items to display sort
      dataGrid.Items.Refresh();
    }

    private void Border_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
      var delta = 0.005;
      if (DataContext is MainWindowViewModel viewModel)
      {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
          if (e.Delta > 0)
          {
            viewModel.TradingBotViewModel.DrawingViewModel.MinValue *= (decimal)(1 - delta);
          }
          else
          {
            viewModel.TradingBotViewModel.DrawingViewModel.MinValue *= (decimal)(1 + delta);
          }
        }
        else if (Keyboard.Modifiers == ModifierKeys.Alt)
        {
          if (e.Delta > 0)
          {
            viewModel.TradingBotViewModel.DrawingViewModel.CandleCount -= 1;
          }
          else
          {
            viewModel.TradingBotViewModel.DrawingViewModel.CandleCount += 1;
          }
        }
        else
        {
          if (e.Delta > 0)
          {
            viewModel.TradingBotViewModel.DrawingViewModel.MaxValue *= (decimal)(1 - delta);
          }
          else
          {
            viewModel.TradingBotViewModel.DrawingViewModel.MaxValue *= (decimal)(1 + delta);
          }
        }

       
      }
    }
  }
}