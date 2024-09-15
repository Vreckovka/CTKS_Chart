using CloudComputing.Domains;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using VCore.Standard;

namespace CouldComputingServer
{
  public class CloudClient : ViewModel
  {

    #region ErrorCount

    private int errorCount;

    public int ErrorCount
    {
      get { return errorCount; }
      set
      {
        if (value != errorCount)
        {
          errorCount = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public TcpClient Client { get; set; }
    public Dictionary<uint, bool> SentBuyGenomes { get; set; } = new Dictionary<uint, bool>();
    public Dictionary<uint, bool> SentSellGenomes { get; set; } = new Dictionary<uint, bool>();

    #region Done

    private bool done;

    public bool Done
    {
      get { return done; }
      set
      {
        if (value != done)
        {
          done = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ReceivedData

    private bool receivedData;

    public bool ReceivedData
    {
      get { return receivedData; }
      set
      {
        if (value != receivedData)
        {
          receivedData = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Data

    private ClientData data;

    public ClientData Data
    {
      get { return data; }
      set
      {
        if (value != data)
        {
          data = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion



    #region PopulationSize

    private int populationSize;

    public int PopulationSize
    {
      get { return populationSize; }
      set
      {
        if (value != populationSize)
        {
          populationSize = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public DateTime LastGenerationTime { get; set; } 

    public string IP
    {
      get
      {
        IPEndPoint remoteIpEndPoint = (IPEndPoint)Client.Client.RemoteEndPoint;

        return remoteIpEndPoint.ToString();
      }
    }
  }

}
