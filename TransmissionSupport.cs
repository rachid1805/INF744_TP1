using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DataLinkApplication
{
  public class TransmissionSupport : ITransmissionSupport
  {
    #region Attributes

    private bool _pretEmettreSource;
    private bool _pretEmettreDestination;
    private bool _donneeRecueSource;
    private bool _donneeRecueDestination;
    private Frame _envoiSource;
    private Frame _envoiDestination;
    private Frame _receptionSource;
    private Frame _receptionDestination;
    private readonly int _latency;
    private static bool _running;
    private readonly byte _frameToCorrupt;
    private readonly byte _numberOfBitErrors;
    private static double _frameNumber = 1;
    private Frame[] _frameQueueToSend;
    private Frame[] _frameQueueToReceive;
    private Frame[] _ackQueueToSend;
    private Frame[] _ackQueueToReceive;
    private byte _nextFrameToSend;
    private byte _nextFrameToReceive;
    private byte _nextAckToSend;
    private byte _nextAckToReceive;
    private byte _oldestFrameToSend;
    private byte _oldestFrameToReceive;
    private byte _oldestAckToSend;
    private byte _oldestAckToReceive;
    private byte _nbFrameBufferedToSend;
    private byte _nbFrameBufferedToReceive;
    private byte _nbAckBufferedToSend;
    private byte _nbAckBufferedToReceive;
    private static byte _MAX_SEQ;
    private readonly object _lockFrameToSend;
    private readonly object _lockFrameToReceive;
    private readonly object _lockAckToSend;
    private readonly object _lockAckToReceive;

    #endregion

    #region Constructor

    public TransmissionSupport(int latency, byte probabilityError, byte numberOfBitErrors)
    {
      _latency = latency;
      _frameToCorrupt = (probabilityError != 0) ? (byte)(100 / probabilityError) : (byte)0;
      _numberOfBitErrors = numberOfBitErrors;
      _pretEmettreSource = true;
      _donneeRecueDestination = false;
      _pretEmettreDestination = true;
      _donneeRecueSource = false;
      _MAX_SEQ = 7;
      _frameQueueToSend = new Frame[_MAX_SEQ + 1];
      _ackQueueToSend = new Frame[_MAX_SEQ + 1];
      _nextFrameToSend = 0;
      _nextAckToSend = 0;
      _oldestFrameToSend = 0;
      _oldestAckToSend = 0;
      _nbFrameBufferedToSend = 0;
      _nbAckBufferedToSend = 0;
      _frameQueueToReceive = new Frame[_MAX_SEQ + 1];
      _ackQueueToReceive = new Frame[_MAX_SEQ + 1];
      _nextFrameToReceive = 0;
      _nextAckToReceive = 0;
      _oldestFrameToReceive = 0;
      _oldestAckToReceive = 0;
      _nbFrameBufferedToReceive = 0;
      _nbAckBufferedToReceive = 0;
      _lockFrameToSend = new object();
      _lockAckToSend = new object();
      _lockFrameToReceive = new object();
      _lockAckToReceive = new object();
    }

    #endregion

    #region Implementation of ITransmissionSupport

    public void StartPhysicalLayer()
    {
      _running = true;

      while (_running)
      {
        var dataReady = false;
        lock (_lockFrameToSend)
        {
          dataReady = _nbFrameBufferedToSend > 0;
        }
        if (dataReady && !_donneeRecueDestination)
        {
          if ((_frameToCorrupt != 0) && (_frameNumber % _frameToCorrupt) == 0)
          {
            Console.WriteLine(
              string.Format(
                "Transmission support: corruption of {0} bits in frame with buffer 0x{1:X} (Thread Id: {2})",
                _numberOfBitErrors, _frameQueueToSend[_oldestFrameToSend].Info.Data[0], Thread.CurrentThread.ManagedThreadId));
            _frameQueueToReceive[_nextFrameToReceive] = Frame.CorruptFrame(_frameQueueToSend[_oldestFrameToSend], _numberOfBitErrors);
          }
          else
          {
            Console.WriteLine(
              string.Format("Transmission support: transmission of new frame buffer 0x{0:X} (Thread Id: {1})",
                _frameQueueToSend[_oldestFrameToSend].Info.Data[0], Thread.CurrentThread.ManagedThreadId));
            _frameQueueToReceive[_nextFrameToReceive] = Frame.CopyFrom(_frameQueueToSend[_oldestFrameToSend]);
          }
          _frameNumber++;
          _oldestFrameToSend = Inc(_oldestFrameToSend);
          lock (_lockFrameToSend)
          {
            _nbFrameBufferedToSend--;
          }
          _pretEmettreSource = true;

          // Simuler la latence du lien physique
          Thread.Sleep(_latency);

          // New packet for the destination
          _nextFrameToReceive = Inc(_nextFrameToReceive);
          lock (_lockFrameToReceive)
          {
            _nbFrameBufferedToReceive++;
          }
          _donneeRecueDestination = true;
        }

        var ackReady = false;
        lock (_lockAckToSend)
        {
          ackReady = _nbAckBufferedToSend > 0;
        }
        if (ackReady && !_donneeRecueSource)
        {
          if ((_frameToCorrupt != 0) && (_frameNumber % _frameToCorrupt) == 0)
          {
            Console.WriteLine(string.Format(
              "Transmission support: corruption of {0} bits in frame Ack (Thread Id: {1})",
              _numberOfBitErrors, Thread.CurrentThread.ManagedThreadId));
            _ackQueueToReceive[_oldestAckToReceive] = Frame.CorruptFrame(_ackQueueToSend[_oldestAckToSend], _numberOfBitErrors);
          }
          else
          {
            Console.WriteLine(string.Format("Transmission support: transmission of new Ack (Thread Id: {0})",
              Thread.CurrentThread.ManagedThreadId));
            _ackQueueToReceive[_oldestAckToReceive] = Frame.CopyFrom(_ackQueueToSend[_oldestAckToSend]);
          }
          _frameNumber++;
          _oldestAckToSend = Inc(_oldestAckToSend);
          lock (_lockAckToSend)
          {
            _nbAckBufferedToSend--;
          }
          _pretEmettreDestination = true;

          // Simuler la latence du lien physique
          Thread.Sleep(_latency);

          // New Ack for the source
          _nextAckToReceive = Inc(_nextAckToReceive);
          lock (_lockAckToReceive)
          {
            _nbAckBufferedToReceive++;
          }
          _donneeRecueSource = true;
        }
      }
    }

    public void StopPhysicalLayer()
    {
      _running = false;
    }

    public bool ReadyToSendData
    {
      get
      {
        return _pretEmettreSource;
      }
    }

    public void SendData(Frame frame)
    {
      _frameQueueToSend[_nextFrameToSend] = frame;
      _envoiSource = frame;
      _nextFrameToSend = Inc(_nextFrameToSend);
      lock (_lockFrameToSend)
      {
        _nbFrameBufferedToSend++;
        if (_nbFrameBufferedToSend < _MAX_SEQ)
        {
          _pretEmettreSource = true;
        }
        else
        {
          _pretEmettreSource = false;
        }
      }
    }

    public bool ReadyToReceiveData
    {
      get
      {
        return _donneeRecueDestination;
      }
    }

    public Frame ReceiveData()
    {
      var frame = Frame.CopyFrom(_frameQueueToReceive[_oldestFrameToReceive]);
      _oldestFrameToReceive = Inc(_oldestFrameToReceive);
      lock (_lockFrameToReceive)
      {
        _nbFrameBufferedToReceive--;
        if (_nbFrameBufferedToReceive > 0)
        {
          _donneeRecueDestination = true;
        }
        else
        {
          _donneeRecueDestination = false;
        }
      }

      return frame;

    }

    public bool ReadyToSendAck
    {
      get
      {
        return _pretEmettreDestination;
      }
    }

    public void SendAck(Frame frame)
    {
      _ackQueueToSend[_nextAckToSend] = frame;
      _envoiDestination = frame;
      _nextAckToSend = Inc(_nextAckToSend);
      lock (_lockAckToSend)
      {
        _nbAckBufferedToSend++;
        if (_nbAckBufferedToSend < _MAX_SEQ)
        {
          _pretEmettreDestination = true;
        }
        else
        {
          _pretEmettreDestination = false;
        }
      }
    }

    public bool ReadyToReceiveAck
    {
      get
      {
        return _donneeRecueSource;
      }
    }

    public Frame ReceiveAck()
    {
      var frame = Frame.CopyFrom(_ackQueueToReceive[_oldestAckToReceive]);
      _oldestAckToReceive = Inc(_oldestAckToReceive);
      lock (_lockAckToReceive)
      {
        _nbAckBufferedToReceive--;
        if (_nbAckBufferedToReceive > 0)
        {
          _donneeRecueSource = true;
        }
        else
        {
          _donneeRecueSource = false;
        }
      }

      return frame;
    }

    #endregion

    private byte Inc(byte valueToIncrement)
    {
      if (valueToIncrement < _MAX_SEQ)
      {
        return (byte)(valueToIncrement + 1);
      }
      return 0;
    }
  }
}
