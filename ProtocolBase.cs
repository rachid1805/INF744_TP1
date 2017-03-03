using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace DataLinkApplication
{
  public abstract class ProtocolBase : IProtocol, IDisposable
  {
    #region Attributes

    protected static byte _MAX_SEQ;
    protected const byte _STOP_RUNNING = 0;
    protected const byte _NETWORK_LAYER_READY = 1;
    protected const byte _FRAME_ARRIVAL = 2;
    protected const byte _CKSUM_ERROR = 3;
    protected const byte _TIMEOUT = 4;
    protected const byte _ACK_TIMEOUT = 5;
    private AutoResetEvent _networkLayerReadyEventTh0;
    private AutoResetEvent _networkLayerReadyEventTh1;
    protected IDictionary<byte, AutoResetEvent> _networkLayerReadyEvents;
    private AutoResetEvent _frameArrivalEventTh0;
    private AutoResetEvent _frameArrivalEventTh1;
    protected IDictionary<byte, AutoResetEvent> _frameArrivalEvents;
    private AutoResetEvent _frameErrorEventTh0;
    private AutoResetEvent _frameErrorEventTh1;
    protected IDictionary<byte, AutoResetEvent> _frameErrorEvents;
    private AutoResetEvent _frameTimeoutEventTh0;
    private AutoResetEvent _frameTimeoutEventTh1;
    protected IDictionary<byte, AutoResetEvent> _frameTimeoutEvents;
    private AutoResetEvent _ackTimeoutEvent;
    private AutoResetEvent _closingEventTh0;
    private AutoResetEvent _closingEventTh1;
    protected IDictionary<byte, AutoResetEvent> _closingEvents;
    private AutoResetEvent _waitingReadEventTh0;
    private AutoResetEvent _waitingReadEventTh1;
    protected IDictionary<byte, AutoResetEvent> _waitingReadEvents;
    private AutoResetEvent _canWriteEventTh0;
    private AutoResetEvent _canWriteEventTh1;
    protected IDictionary<byte, AutoResetEvent> _canWriteEvents;
    private AutoResetEvent _lastPacketEvent;
    protected WaitHandle[] _waitHandles;
    protected Thread _communicationThread; // (T1) et (T2) : les stations émettrice et réceptrice
    private readonly IDictionary<int, System.Timers.Timer> _timers;
    private readonly int _timeout; // en ms
    private bool _networkLayerEnable;
    private Packet _packetRead;
    private Packet _packetWrite;
    private Frame _frame;
    protected readonly byte _threadId;
    private readonly string _fileName;

    #endregion

    #region Constructor

    protected ProtocolBase(byte windowSize, int timeout, string fileName, bool inFile)
    {
      _MAX_SEQ = (byte) (windowSize - 1);
      _timeout = timeout;
      _fileName = fileName;
      _timers = new Dictionary<int, System.Timers.Timer>(_MAX_SEQ);
      _threadId = (byte) (inFile ? 0 : 1);
    }

    #endregion

    #region Implementation of IProtocol

    public void Protocol()
    {
      // Creates events that trigger the thread changing
      CreateMandatoryEvents();

      // Call specialized function
      DoProtocol();
    }

    public void StartTransfer()
    {
      _networkLayerEnable = true;

      try
      {
        using (FileStream inFile = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
        {
          while (inFile.Position != inFile.Length)
          {
            if (_networkLayerEnable)
            {
              var oneByte = inFile.ReadByte();
              _packetRead = new Packet { Data = new byte[1] { (byte)oneByte } };
              // Send the packet to the data layer
              _networkLayerReadyEvents[_threadId].Set();
              // Wait the packet to be read by the data layer
              _waitingReadEvents[_threadId].WaitOne();
            }
          }
          // No packet to read from the file, trigger the last packet
          _lastPacketEvent.Set();
        }
      }
      catch (FileNotFoundException e)
      {
        Console.WriteLine(e.ToString());
      }
    }

    public void ReceiveTransfer()
    {
      try
      {
        using (FileStream outFile = new FileStream(_fileName, FileMode.Create, FileAccess.Write))
        {
          var waitHandles = new WaitHandle[] { _canWriteEvents[_threadId], _lastPacketEvent };
          var running = true;

          while (running)
          {
            // Wait trigger events to activate the thread action
            var index = WaitHandle.WaitAny(waitHandles);

            // Received one packet
            outFile.WriteByte(_packetWrite.Data[0]);

            if (index == 1)
            {
              // The last packet
              running = false;
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.ToString());
      }
    }

    #endregion

    #region Implementation of IDisposable

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    void IDisposable.Dispose()
    {
      if (_communicationThread != null)
      {
        // Request that the worker thread stop itself
        _closingEvents[_threadId].Set();

        // Use the Join method to block the current thread until the object's thread terminates
        _communicationThread.Join();
        _communicationThread = null;
      }

      // Dispose the events
      foreach (var networkLayerReadyEvent in _networkLayerReadyEvents.Values)
      {
        networkLayerReadyEvent.Dispose();
      }
      foreach (var frameArrivalEvent in _frameArrivalEvents.Values)
      {
        frameArrivalEvent.Dispose();
      }
      foreach (var frameErrorEvent in _frameErrorEvents.Values)
      {
        frameErrorEvent.Dispose();
      }
      foreach (var frameTimeoutEvent in _frameTimeoutEvents.Values)
      {
        frameTimeoutEvent.Dispose();
      }
      if (_ackTimeoutEvent != null)
      {
        _ackTimeoutEvent.Dispose();
      }
      foreach (var waitingReadEvent in _waitingReadEvents.Values)
      {
        waitingReadEvent.Dispose();
      }
      foreach (var canWriteEvent in _canWriteEvents.Values)
      {
        canWriteEvent.Dispose();
      }
      foreach (var closingEvent in _closingEvents.Values)
      {
        closingEvent.Dispose();
      }
      _lastPacketEvent.Dispose();
    }

    #endregion

    #region Protected Functions

    protected abstract void DoProtocol();

    protected Packet FromNetworkLayer()
    {
      // Un packet pour l'émetteur (lu du fichier d'entrée)
      var packet = Packet.CopyFrom(_packetRead);
      // Next packet
      _waitingReadEvents[_threadId].Set();

      return packet;
    }

    protected void ToNetworkLayer(Packet packet)
    {
      // Un packet pour le récepteur (à écrire dans le fichier de sortie)
      _packetWrite = Packet.CopyFrom(packet);
      // We can now write in the file
      _canWriteEvents[_threadId].Set();
    }

    protected Frame FromPhysicalLayer()
    {
      return Frame.CopyFrom(_frame);
    }

    protected void ToPhysicalLayer(Frame frame)
    {
      // Une trame pour le récepteur (à écrire dans le fichier de sortie) ou un ack
      _frame = Frame.CopyFrom(frame);

      // TODO Vérifier si on peut ecrire dans la thread 3

      // Notify the physical layer of the reception thread
      _frameArrivalEvents[(byte) (1 - _threadId)].Set();
    }

    protected void CreateMandatoryEvents()
    {
      _closingEventTh0 = new AutoResetEvent(false);
      _closingEventTh1 = new AutoResetEvent(false);
      _closingEvents = new Dictionary<byte, AutoResetEvent>(2) {{0, _closingEventTh0}, {0, _closingEventTh1}};
      _networkLayerReadyEventTh0 = new AutoResetEvent(false);
      _networkLayerReadyEventTh1 = new AutoResetEvent(false);
      _networkLayerReadyEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _networkLayerReadyEventTh0 }, { 0, _networkLayerReadyEventTh1 } };
      _frameArrivalEventTh0 = new AutoResetEvent(false);
      _frameArrivalEventTh1 = new AutoResetEvent(false);
      _frameArrivalEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _frameArrivalEventTh0 }, { 0, _frameArrivalEventTh1 } };
      _frameErrorEventTh0 = new AutoResetEvent(false);
      _frameErrorEventTh1 = new AutoResetEvent(false);
      _frameErrorEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _frameErrorEventTh0 }, { 0, _frameErrorEventTh1 } };
      _frameTimeoutEventTh0 = new AutoResetEvent(false);
      _frameTimeoutEventTh1 = new AutoResetEvent(false);
      _frameTimeoutEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _frameTimeoutEventTh0 }, { 0, _frameTimeoutEventTh1 } };
      _waitingReadEventTh0 = new AutoResetEvent(false);
      _waitingReadEventTh1 = new AutoResetEvent(false);
      _waitingReadEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _waitingReadEventTh0 }, { 0, _waitingReadEventTh1 } };
      _canWriteEventTh0 = new AutoResetEvent(false);
      _canWriteEventTh1 = new AutoResetEvent(false);
      _canWriteEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _canWriteEventTh0 }, { 0, _canWriteEventTh1 } };
      _lastPacketEvent = new AutoResetEvent(false);
    }

    protected static bool Between(byte a, byte b, byte c)
    {
      return ((a <= b) && (b < c)) || ((c < a) && (a <= b)) || ((b < c) && (c < a));
    }

    protected static byte Inc(byte valueToIncrement)
    {
      if (valueToIncrement < _MAX_SEQ)
      {
        return (byte) (valueToIncrement + 1);
      }
      return 0;
    }

    protected void StartTimer(int frameNb)
    {
      ReleaseTimer(frameNb);

      var timer = new System.Timers.Timer(_timeout);
      _timers.Add(frameNb, timer);
      timer.Elapsed += OnTimedEvent;
      timer.AutoReset = false;
      timer.Enabled = true;
    }

    protected void StopTimer(int frameNb)
    {
      ReleaseTimer(frameNb);
    }

    protected void EnableNetworkLayer()
    {
      _networkLayerEnable = true;
    }

    protected void DisableNetworkLayer()
    {
      _networkLayerEnable = false;
    }


    #endregion

    #region Private Functions

    private void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
      Console.WriteLine("The timeout event was raised at {0:HH:mm:ss.fff}", e.SignalTime);
      _frameTimeoutEvents[_threadId].Set();
    }

    private void ReleaseTimer(int frameNb)
    {
      if (_timers.ContainsKey(frameNb))
      {
        _timers[frameNb].Stop();
        _timers[frameNb].Dispose();
        _timers.Remove(frameNb);
      }
    }

    #endregion
  }
}
