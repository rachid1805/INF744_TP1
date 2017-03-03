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

    private bool _pretEmmetreSource;
    private bool _pretEmmetreDestination;
    private bool _donneeRecueSource;
    private bool _donneeRecueDestination;
    private Frame _envoiSource;
    private Frame _envoiDestination;
    private Frame _receptionSource;
    private Frame _receptionDestination;
    private readonly int _latency;

    #endregion

    #region Constructor

    public TransmissionSupport(int latency)
    {
      _latency = latency;
      _pretEmmetreSource = true;
      _donneeRecueDestination = false;
    }

    #endregion

    #region Implementation of ITransmissionSupport

    public void PhysicalLayer()
    {
      if (!_pretEmmetreSource && !_donneeRecueDestination)
      {
        _receptionDestination = Frame.CopyFrom(_envoiSource);
        _pretEmmetreSource = true;
        
        // Simuler la latence du lien physique
        Thread.Sleep(_latency);

        // New packet for the destination
        _donneeRecueDestination = true;
      }
    }

    public bool ReadyToSend
    {
      get
      {
        return _pretEmmetreSource;
      }
    }

    public void SendFrame(Frame frame)
    {
      _envoiSource = Frame.CopyFrom(frame);
      _pretEmmetreSource = false;
    }

    public bool ReadyToReceive
    {
      get
      {
        return _donneeRecueDestination;
      }
    }

    public Frame ReceiveFrame()
    {
      var frame = Frame.CopyFrom(_receptionDestination);
      _donneeRecueDestination = false;

      return frame;
    }

    #endregion
  }
}
