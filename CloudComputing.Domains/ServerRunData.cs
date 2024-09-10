using System;
using System.Net.Sockets;
using System.Text;

namespace CloudComputing.Domains
{
  public class ServerRunData
  {
    public int Generation { get; set; }
    public string BuyGenomes { get; set; }
    public string SellGenomes { get; set; }

    public int AgentCount { get; set; }
    public int Minutes { get; set; }
    public double Split { get; set; }
    public bool IsRandom { get; set; }

    public string Symbol { get; set; }
  }

  public static class TCPHelper
  {
    public static void SendMessage(TcpClient client, string message)
    {
      var _stream = client.GetStream();

      if (_stream != null)
      {

        var data = Encoding.Unicode.GetBytes(message);

        int totalBytesSent = 0;

        // Send data in chunks
        while (totalBytesSent < data.Length)
        {
          // Calculate the number of bytes to send in this chunk
          int bytesToSend = Math.Min(MessageContract.BUFFER_SIZE_CLIENT, data.Length - totalBytesSent);

          // Write the current chunk to the stream
          _stream.Write(data, totalBytesSent, bytesToSend);
          _stream.Flush(); // Ensure the data is sent immediately

          // Update the total number of bytes sent
          totalBytesSent += bytesToSend;
        }
      }
    }
  }
}
