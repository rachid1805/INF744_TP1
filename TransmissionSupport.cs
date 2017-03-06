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
    private double _frameNumber;

    #endregion

    #region Constructor

    public TransmissionSupport(int latency, byte probabilityError, byte numberOfBitErrors)
    {
      _latency = latency;
      _frameToCorrupt = (byte)(100 / probabilityError);
      _numberOfBitErrors = numberOfBitErrors;
      _pretEmettreSource = true;
      _donneeRecueDestination = false;
      _pretEmettreDestination = true;
      _donneeRecueSource = false;
      _frameNumber = 1;
    }

    #endregion

    #region Implementation of ITransmissionSupport

    public void StartPhysicalLayer()
    {
      _running = true;

      while (_running)
      {
        if (!_pretEmettreSource && !_donneeRecueDestination)
        {
          if ((_frameToCorrupt != 0) && (_frameNumber % _frameToCorrupt) == 0)
          {
            // Corrupt frame
            //Console.WriteLine(string.Format("Transmission support: corruption of frame with buffer 0x{0:X} (Thread Id: {1})",
            //  _envoiSource.Info.Data[0], Thread.CurrentThread.ManagedThreadId));
            _receptionDestination = Frame.CorruptFrame(_envoiSource, _numberOfBitErrors);
            Console.WriteLine(string.Format("Transmission support: corruption of frame buffer 0x{0:X} to frame buffer 0x{1:X} (Thread Id: {2})",
              _envoiSource.Info.Data[0], _receptionDestination.Info.Data[0], Thread.CurrentThread.ManagedThreadId));
          }
          else
          {
            Console.WriteLine(string.Format("Transmission support: transmission of new frame buffer 0x{0:X} (Thread Id: {1})",
              _envoiSource.Info.Data[0], Thread.CurrentThread.ManagedThreadId));
            _receptionDestination = Frame.CopyFrom(_envoiSource);
          }
          _frameNumber++;
          _pretEmettreSource = true;

          // Simuler la latence du lien physique
          Thread.Sleep(_latency);

          // New packet for the destination
          _donneeRecueDestination = true;
        }
        if (!_pretEmettreDestination && !_donneeRecueSource)
        {
          if ((_frameToCorrupt != 0) && (_frameNumber % _frameToCorrupt) == 0)
          {
            Console.WriteLine(string.Format("Transmission support: corruption of Ack (Thread Id: {0})",
              Thread.CurrentThread.ManagedThreadId));
            _receptionSource = Frame.CorruptFrame(_envoiDestination, _numberOfBitErrors);
          }
          else
          {
            Console.WriteLine(string.Format("Transmission support: transmission of new Ack (Thread Id: {0})",
              Thread.CurrentThread.ManagedThreadId));
            _receptionSource = Frame.CopyFrom(_envoiDestination);
          }
          _frameNumber++;
          _pretEmettreDestination = true;

          // Simuler la latence du lien physique
          Thread.Sleep(_latency);

          // New Ack for the source
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
      _envoiSource = Frame.CopyFrom(frame);
      _pretEmettreSource = false;
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
      var frame = Frame.CopyFrom(_receptionDestination);
      _donneeRecueDestination = false;

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
      _envoiDestination = Frame.CopyFrom(frame);
      _pretEmettreDestination = false;
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
      var frame = Frame.CopyFrom(_receptionSource);
      _donneeRecueSource = false;

      return frame;
    }

    #endregion
  }
}
