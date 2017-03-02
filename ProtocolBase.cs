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
    protected WaitHandle[] m_waitHandles;

    #endregion

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    void IDisposable.Dispose()
    {
      if (_networkLayerReadyEvent != null)
      {
        //// Request that the worker thread stop itself
        //m_closingEvent.Set();

        //// Use the Join method to block the current thread until the object's thread terminates
        //m_detectionThread.Join();
        //m_detectionThread = null;

        //// Dispose the events
        //m_deviceChangedEvent.Dispose();
        //m_closingEvent.Dispose();

        //// Clear all equipments information from the cache
        //m_identifiedEquipments.Clear();
      }
    }
  }
}
