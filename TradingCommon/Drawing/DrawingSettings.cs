using System;
using System.Text.Json.Serialization;
using VCore.Standard;

namespace CTKS_Chart.ViewModels
{
  public class DrawingSettings : ViewModel
  {
    #region ShowClusters

    private bool showClusters = true;

    public bool ShowClusters
    {
      get { return showClusters; }
      set
      {
        if (value != showClusters)
        {
          showClusters = value;

          RenderLayout?.Invoke();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowIntersections

    private bool showIntersections = true;

    public bool ShowIntersections
    {
      get { return showIntersections; }
      set
      {
        if (value != showIntersections)
        {
          showIntersections = value;

          RenderLayout?.Invoke();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowATH

    private bool showATH = true;

    public bool ShowATH
    {
      get { return showATH; }
      set
      {
        if (value != showATH)
        {
          showATH = value;

          RenderLayout?.Invoke();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowAutoPositions

    private bool showAutoPositions = true;

    public bool ShowAutoPositions
    {
      get { return showAutoPositions; }
      set
      {
        if (value != showAutoPositions)
        {
          showAutoPositions = value;

          RenderLayout?.Invoke();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowManualPositions

    private bool showManualPositions = true;

    public bool ShowManualPositions
    {
      get { return showManualPositions; }
      set
      {
        if (value != showManualPositions)
        {
          showManualPositions = value;

          RenderLayout?.Invoke();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ShowAveragePrice

    private bool showAveragePrice = true;

    public bool ShowAveragePrice
    {
      get { return showAveragePrice; }
      set
      {
        if (value != showAveragePrice)
        {
          showAveragePrice = value;

          RenderLayout?.Invoke();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    [JsonIgnore]
    public Action RenderLayout { get; set; }
  }
}