using System;
using System.Threading;

namespace DataLinkApplication
{
  public class SelectiveRepeatProtocol : ProtocolBase
  {
    #region Constructor

    public SelectiveRepeatProtocol(byte windowSize, int timeout, string fileName, bool inFile, ITransmissionSupport transmissionSupport)
      :base(windowSize, timeout, fileName, inFile, transmissionSupport)
    {
      _communicationThread = new Thread(Protocol);
      Console.WriteLine(string.Format("Starting the data link layer thread of the {0}", inFile ? "transmitter" : "receiver"));
      _communicationThread.Start();
    }

    #endregion

    #region Protected Functions

    protected override void DoProtocol()
    {
      // Traduire le protocol 6 dans le livre de Tanenbaum en C#
    }

    #endregion
  }
}
