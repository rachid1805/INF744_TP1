using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DataLinkApplication
{
  public class ProtocolBase : IDisposable
  {
    #region Attributes

    protected AutoResetEvent _networkLayerReadyEvent;
    protected AutoResetEvent _frameArrivalEvent;
    protected AutoResetEvent _frameErrorEvent;
    protected AutoResetEvent _frameTimeoutEvent;
    protected AutoResetEvent _ackTimeoutEvent;
    protected AutoResetEvent _closeEvent;
    protected WaitHandle[] _waitHandles;

    #endregion

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    void IDisposable.Dispose()
    {
    }
  }
}
