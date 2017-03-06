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
    private const byte _LAST_PACKET = 1;
    protected const byte _STOP_RUNNING = 0;
    protected const byte _NETWORK_LAYER_READY = 1;
    protected const byte _FRAME_ARRIVAL = 2;
    protected const byte _CKSUM_ERROR = 3;
    protected const byte _TIMEOUT = 4;
    protected const byte _ACK_TIMEOUT = 5;
    private static AutoResetEvent _networkLayerReadyEventTh0;
    private static AutoResetEvent _networkLayerReadyEventTh1;
    protected static IDictionary<byte, AutoResetEvent> _networkLayerReadyEvents;
    private static AutoResetEvent _frameArrivalEventTh0;
    private static AutoResetEvent _frameArrivalEventTh1;
    protected static IDictionary<byte, AutoResetEvent> _frameArrivalEvents;
    private static AutoResetEvent _frameErrorEventTh0;
    private static AutoResetEvent _frameErrorEventTh1;
    protected static IDictionary<byte, AutoResetEvent> _frameErrorEvents;
    private static AutoResetEvent _frameTimeoutEventTh0;
    private static AutoResetEvent _frameTimeoutEventTh1;
    protected static IDictionary<byte, AutoResetEvent> _frameTimeoutEvents;
    private static AutoResetEvent _ackTimeoutEvent;
    private static AutoResetEvent _closingEventTh0;
    private static AutoResetEvent _closingEventTh1;
    protected static IDictionary<byte, AutoResetEvent> _closingEvents;
    private static AutoResetEvent _waitingReadEventTh0;
    private static AutoResetEvent _waitingReadEventTh1;
    protected static IDictionary<byte, AutoResetEvent> _waitingReadEvents;
    private static AutoResetEvent _canWriteEventTh0;
    private static AutoResetEvent _canWriteEventTh1;
    protected static IDictionary<byte, AutoResetEvent> _canWriteEvents;
    private static AutoResetEvent _lastPacketEvent;
    protected Thread _communicationThread; // (T1) et (T2) : les stations émettrice et réceptrice
    private readonly IDictionary<int, System.Timers.Timer> _timers;
    private readonly int _timeout; // en ms
    private bool _networkLayerEnable;
    private Packet _packetRead;
    private Packet _packetWrite;
    protected readonly byte _threadId;
    private readonly string _fileName;
    private readonly ITransmissionSupport _transmissionSupport;
    private static byte _remainingPacket = 0;
    private static object _lock;

    #endregion

    #region Constructor

    protected ProtocolBase(byte windowSize, int timeout, string fileName, bool inFile, ITransmissionSupport transmissionSupport)
    {
      _transmissionSupport = transmissionSupport;
      _MAX_SEQ = (byte) (windowSize - 1);
      _timeout = timeout;
      _fileName = fileName;
      _networkLayerEnable = false;
      _timers = new Dictionary<int, System.Timers.Timer>(_MAX_SEQ);
      _threadId = (byte) (inFile ? 0 : 1);
      _lock = new object();

      // Creates events that trigger the thread changing
      CreateMandatoryEvents();
    }

    #endregion

    #region Implementation of IProtocol

    public void Protocol()
    {
      // Call specialized function
      DoProtocol();
    }

    public void StartTransfer()
    {
      if (_threadId == 0)
      {
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
                lock (_lock)
                {
                  _remainingPacket++;
                }
                Console.WriteLine(string.Format("Read byte 0x{0:X} from input file {1}", oneByte, _fileName));
                // Send the packet to the data layer
                _networkLayerReadyEvents[_threadId].Set();
                // Wait the packet to be read by the data layer
                _waitingReadEvents[_threadId].WaitOne();
                Thread.Sleep(1);
              }
            }
            // No packet to read from the file, trigger the last packet
            Console.WriteLine(string.Format("--- The last byte from the input file ---"));

            while (_remainingPacket > 0)
            {
              Thread.Sleep(1);
            }
            // The last packet
            _lastPacketEvent.Set();
          }
        }
        catch (FileNotFoundException e)
        {
          Console.WriteLine(e.ToString());
        }
        Console.WriteLine("**** The Network Layer of the transmission thread terminated ****");
      }
    }

    public void ReceiveTransfer()
    {
      if (_threadId == 1)
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

              if (index != _LAST_PACKET)
              {
                // Received one packet
                Console.WriteLine(string.Format("Write byte 0x{0:X} to output file {1}", _packetWrite.Data[0], _fileName));
                outFile.WriteByte(_packetWrite.Data[0]);
                lock (_lock)
                {
                  _remainingPacket--;
                }

                // Send the (faked) Ack
                _packetRead = new Packet { Data = new byte[1] { 0 } };
                // Send the packet to the data layer
                _networkLayerReadyEvents[_threadId].Set();
                // Wait the packet to be read by the data layer
                _waitingReadEvents[_threadId].WaitOne();
              }
              else
              {
                // The last packet
                running = false;
              }
            }
            Console.WriteLine(string.Format("--- Transmission completed and the output file {0} closed ---", _fileName));
            // Terminate the reception thread
            _closingEvents[_threadId].Set();
            // Terminate the transmission thread
            _closingEvents[(byte) (1 - _threadId)].Set();
            // Terminate the physical layer
            _transmissionSupport.StopPhysicalLayer();
          }
        }
        catch (Exception e)
        {
          Console.WriteLine(e.ToString());
        }
        Console.WriteLine("**** The Network Layer of the reception thread terminated ****");
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
      // Vérifier si une trame est disponible
      if (_threadId == 1)
      {
        while (!_transmissionSupport.ReadyToReceiveData)
        {
          Thread.Sleep(1);
        }
        // Une trame de données
        return _transmissionSupport.ReceiveData();
      }
      else
      {
        while (!_transmissionSupport.ReadyToReceiveAck)
        {
          Thread.Sleep(1);
        }
        // Une trame Ack
        return _transmissionSupport.ReceiveAck();
      }

      throw new ApplicationException(string.Format("No frame available for collecting. Thread Id {0}", _threadId));
    }

    protected void ToPhysicalLayer(Frame frame)
    {
      if (_threadId == 0)
      {
        // Sending a data to the receiver
        //while (!_transmissionSupport.ReadyToSendData)
        //{
        //  Thread.Sleep(1);
        //}
        if (_transmissionSupport.ReadyToSendData)
        {
          _transmissionSupport.SendData(frame);
          _frameArrivalEvents[(byte)(1 - _threadId)].Set();
        }
      }
      else
      {
        // Sending an ack to the transmitter
        //while (!_transmissionSupport.ReadyToSendAck)
        //{
        //  Thread.Sleep(1);
        //}
        if (_transmissionSupport.ReadyToSendAck)
        {
          _transmissionSupport.SendAck(frame);
          _frameArrivalEvents[(byte)(1 - _threadId)].Set();
        }
      }
    }

    protected void CreateMandatoryEvents()
    {
      _closingEventTh0 = new AutoResetEvent(false);
      _closingEventTh1 = new AutoResetEvent(false);
      _closingEvents = new Dictionary<byte, AutoResetEvent>(2) {{0, _closingEventTh0}, {1, _closingEventTh1}};
      _networkLayerReadyEventTh0 = new AutoResetEvent(false);
      _networkLayerReadyEventTh1 = new AutoResetEvent(false);
      _networkLayerReadyEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _networkLayerReadyEventTh0 }, { 1, _networkLayerReadyEventTh1 } };
      _frameArrivalEventTh0 = new AutoResetEvent(false);
      _frameArrivalEventTh1 = new AutoResetEvent(false);
      _frameArrivalEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _frameArrivalEventTh0 }, { 1, _frameArrivalEventTh1 } };
      _frameErrorEventTh0 = new AutoResetEvent(false);
      _frameErrorEventTh1 = new AutoResetEvent(false);
      _frameErrorEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _frameErrorEventTh0 }, { 1, _frameErrorEventTh1 } };
      _frameTimeoutEventTh0 = new AutoResetEvent(false);
      _frameTimeoutEventTh1 = new AutoResetEvent(false);
      _frameTimeoutEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _frameTimeoutEventTh0 }, { 1, _frameTimeoutEventTh1 } };
      _waitingReadEventTh0 = new AutoResetEvent(false);
      _waitingReadEventTh1 = new AutoResetEvent(false);
      _waitingReadEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _waitingReadEventTh0 }, { 1, _waitingReadEventTh1 } };
      _canWriteEventTh0 = new AutoResetEvent(false);
      _canWriteEventTh1 = new AutoResetEvent(false);
      _canWriteEvents = new Dictionary<byte, AutoResetEvent>(2) { { 0, _canWriteEventTh0 }, { 1, _canWriteEventTh1 } };
      _lastPacketEvent = new AutoResetEvent(false);
    }

    protected static bool Between(byte a, byte b, byte c)
    {
      return ((a <= b) && (b < c)) || ((c < a) && (a <= b)) || ((b < c) && (c < a));
    }

    protected byte Inc(byte valueToIncrement)
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
