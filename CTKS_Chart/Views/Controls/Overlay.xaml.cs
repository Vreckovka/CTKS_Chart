using CTKS_Chart.Trading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CTKS_Chart.Views.Controls
{
  /// <summary>
  /// Interaction logic for Overlay.xaml
  /// </summary>
  public partial class Overlay : UserControl, INotifyPropertyChanged
  {
    bool renderOverlay = false;

    #region Chart

    public Chart Chart
    {
      get { return (Chart)GetValue(ChartProperty); }
      set { SetValue(ChartProperty, value); }
    }

    public static readonly DependencyProperty ChartProperty =
      DependencyProperty.Register(
        nameof(Chart),
        typeof(Chart),
        typeof(Overlay));

    #endregion

    #region VerticalRuler

    public VerticalRuler VerticalRuler
    {
      get { return (VerticalRuler)GetValue(VerticalRulerProperty); }
      set { SetValue(VerticalRulerProperty, value); }
    }

    public static readonly DependencyProperty VerticalRulerProperty =
      DependencyProperty.Register(
        nameof(VerticalRuler),
        typeof(VerticalRuler),
        typeof(Overlay));

    #endregion

    #region HorizontalRuler

    public HorizontalRuler HorizontalRuler
    {
      get { return (HorizontalRuler)GetValue(HorizontalRulerProperty); }
      set { SetValue(HorizontalRulerProperty, value); }
    }

    public static readonly DependencyProperty HorizontalRulerProperty =
      DependencyProperty.Register(
        nameof(HorizontalRuler),
        typeof(HorizontalRuler),
        typeof(Overlay));

    #endregion

    public Overlay()
    {
      InitializeComponent();

      OverlayCanvas.MouseMove += OverlayCanvas_MouseMove;
      OverlayCanvas.MouseLeave += Overlay_MouseLeave;
      OverlayCanvas.MouseEnter += Overlay_MouseEnter;
      OverlayCanvas.MouseLeftButtonDown += Overlay_MouseLeftButtonDown;


      MeasureTool.Overlay = OverlayCanvas;
      VerticalCrosshair.Overlay = OverlayCanvas;
      HorizontalCrosshair.Overlay = OverlayCanvas;

      OverlayControls.Add(MeasureTool);
      OverlayControls.Add(VerticalCrosshair);
      OverlayControls.Add(HorizontalCrosshair);
    }

    #region Properties

    List<OverlayControl> OverlayControls = new List<OverlayControl>();
    public MeasureTool MeasureTool { get; } = new MeasureTool();
    public VerticalCrosshair VerticalCrosshair { get; } = new VerticalCrosshair();
    public HorizontalCrosshair HorizontalCrosshair { get; } = new HorizontalCrosshair();

    #region ActualOverlayMousePosition

    private Point actualOverlayMousePosition;

    public Point ActualOverlayMousePosition
    {
      get { return actualOverlayMousePosition; }
      set
      {
        if (value != actualOverlayMousePosition)
        {
          actualOverlayMousePosition = value;
          OnPropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Methods

    #region OverlayCanvas_MouseMove

    private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
    {
      ActualOverlayMousePosition = e.GetPosition(OverlayCanvas);

      if (renderOverlay)
      {
        DrawOveralay();
      }


      VerticalRuler?.RenderLabel(e.GetPosition(VerticalRuler), Chart.ActualMousePositionY, Chart.ActualMousePositionX, Chart.AssetPriceRound);
      HorizontalRuler?.RenderLabel(e.GetPosition(HorizontalRuler), Chart.ActualMousePositionY, Chart.ActualMousePositionX, Chart.AssetPriceRound);
    }

    #endregion

    #region Overlay_MouseLeftButtonDown

    private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (MeasureTool.IsEnabled)
      {
        if (MeasureTool.IsVisible)
        {
          if (MeasureTool.StartPoint != null && MeasureTool.EndPoint == null)
          {
            MeasureTool.EndPoint = e.GetPosition(OverlayCanvas);
          }
          else if (MeasureTool.StartPoint != null && MeasureTool.EndPoint != null)
          {
            MeasureTool.IsEnabled = false;
          }
        }
        else
        {
          MeasureTool.IsVisible = true;
        }
      }
    }

    #endregion

    #region Overlay_MouseEnter

    private void Overlay_MouseEnter(object sender, MouseEventArgs e)
    {
      renderOverlay = true;
    }

    #endregion

    #region Overlay_MouseLeave

    private void Overlay_MouseLeave(object sender, MouseEventArgs e)
    {
      renderOverlay = false;

      ClearOverlay();
    }

    #endregion

    #region RenderControls

    private void RenderControls(Point mousePoint, decimal representedPrice, DateTime represnetedDate, int assetPriceRound)
    {
      foreach (var control in OverlayControls.Where(x => x.IsVisible))
      {
        control.Render(mousePoint, representedPrice, represnetedDate,assetPriceRound );
      }
    }

    #endregion

    #region DrawOverlay

    private void DrawOveralay()
    {
      VerticalCrosshair.IsVisible = true;
      HorizontalCrosshair.IsVisible = true;

      RenderControls(ActualOverlayMousePosition, Chart.ActualMousePositionY, Chart.ActualMousePositionX, Chart.AssetPriceRound);

    }

    #endregion

    #region ClearOverlay

    private void ClearOverlay()
    {
      VerticalCrosshair.IsVisible = false;
      HorizontalCrosshair.IsVisible = false;

      VerticalRuler?.ClearLabel();
      HorizontalRuler?.ClearLabel();
    }

    #endregion

    #region OnPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #endregion
  }
}
