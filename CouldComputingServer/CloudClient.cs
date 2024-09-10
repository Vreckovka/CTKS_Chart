using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace CouldComputingServer
{
  public class CloudClient
  {
    public int ErrorCount { get; set; }
    public TcpClient Client { get; set; }
    public Dictionary<uint, bool> SentBuyGenomes { get; set; } = new Dictionary<uint, bool>();
    public Dictionary<uint, bool> SentSellGenomes { get; set; } = new Dictionary<uint, bool>();

    public bool Done { get; set; }
    public bool ReceivedData { get; set; }

    public int PopulationSize { get; set; }
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
