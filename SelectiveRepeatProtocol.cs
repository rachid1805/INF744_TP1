using System.Threading;

namespace DataLinkApplication
{
  public class SelectiveRepeatProtocol : ProtocolBase
  {
    #region Constructor

    public SelectiveRepeatProtocol(byte windowSize, int timeout, string fileName, bool inFile)
      :base(windowSize, timeout, fileName, inFile)
    {
      _communicationThread = new Thread(Protocol);
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
