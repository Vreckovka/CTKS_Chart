using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CTKS_Chart.Binance;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VCore.WPF.Other;

#pragma warning disable 618

namespace CTKS_Chart
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

      var console = new WindowPosition();
      RECT rct = new RECT();
      GetWindowRect(GetConsoleWindow(), ref rct);

      console.Left = rct.Left;
      console.Top = rct.Top;
      console.Height = Console.WindowHeight;
      console.Width = Console.WindowWidth;


      File.WriteAllText(positionPath, JsonSerializer.Serialize(new WindowPosition[] {position, console}));
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      if (File.Exists(positionPath))
      {
        var positions = JsonSerializer.Deserialize<WindowPosition[]>(File.ReadAllText(positionPath));

        var pos = positions[0];
        var con = positions[1];

     
        Left = pos.Left;
        Top = pos.Top;
        Width = pos.Width;
        Height = pos.Height;

        await Task.Delay(500);

        SetWindowPos(GetConsoleWindow(), IntPtr.Zero,
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