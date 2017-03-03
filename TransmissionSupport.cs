﻿using System;
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

    #endregion

    #region Constructor

    public TransmissionSupport(int latency)
    {
      _latency = latency;
      _pretEmettreSource = true;
      _donneeRecueDestination = false;
      _pretEmettreDestination = true;
      _donneeRecueSource = false;
    }

    #endregion

    #region Implementation of ITransmissionSupport

    public void PhysicalLayer()
    {
      var running = true;

      while (running)
      {
        if (!_pretEmettreSource && !_donneeRecueDestination)
        {
          Console.WriteLine(string.Format("Transmission support: transmission of new frame buffer 0x{0:X} (Thread Id: {1})",
            _envoiSource.Info.Data[0], Thread.CurrentThread.ManagedThreadId));
          _receptionDestination = Frame.CopyFrom(_envoiSource);
          _pretEmettreSource = true;

          // Simuler la latence du lien physique
          Thread.Sleep(_latency);

          // New packet for the destination
          _donneeRecueDestination = true;
        }
        if (!_pretEmettreDestination && !_donneeRecueSource)
        {
          Console.WriteLine(string.Format("Transmission support: transmission of new Ack (Thread Id: {0})",
            Thread.CurrentThread.ManagedThreadId));
          _receptionSource = Frame.CopyFrom(_envoiDestination);
          _pretEmettreDestination = true;

          // Simuler la latence du lien physique
          Thread.Sleep(_latency);

          // New Ack for the source
          _donneeRecueSource = true;
        }
      }
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
