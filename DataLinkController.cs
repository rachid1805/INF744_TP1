﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DataLinkApplication
{
  public class DataLinkController : IDisposable
  {
    #region Attributes

    public static DataLinkController _dataLinkController;
    private string _configFile;
    private IProtocol _transmissionProtocol;
    private IProtocol _receptionProtocol;
    private Thread _transmissionSupportThread;      // (T3) le support de transmission
    private Thread _transmissionNetworkLayerThread; // (T4) la couche réseau de transmission (Envoi de fichier)
    private Thread _receptionNetworkLayerThread;    // (T5) la couche réseau de réception (Réception de fichier)

    #endregion

    #region Public Functions

    public int Process(string[] args)
    {
      try
      {
        if (!ParseArgs(args))
        {
          Console.WriteLine("{0} [-f <fichier des commandes>]", args[0]);
          Console.WriteLine("-f: Nom et path du fichier des commandes (format: xml)");
          return 1;
        }
      }
      catch
      {
        return 1;
      }

      try
      {
        // Read the config file content
        var xmlFileReader = new XmlDataLinkConfigFileReader(_configFile);
        Console.WriteLine("********************************************************");
        Console.WriteLine("Reading input file :             {0}", _configFile);
        Console.WriteLine("********************************************************");
        Console.WriteLine("Name and path of input file:     {0}",xmlFileReader.InFile);
        Console.WriteLine("Name and path of output file:    {0}", xmlFileReader.OutFile);
        Console.WriteLine("Windows size:                    {0}", xmlFileReader.WindowSize);
        Console.WriteLine("RejectType:                      {0}", xmlFileReader.RejectType);
        Console.WriteLine("Frame Timeout:                   {0} ms", xmlFileReader.Timeout);
        Console.WriteLine("Acknowledgement Timeout:         {0} ms", xmlFileReader.AckTimeout);
        Console.WriteLine("Transmission latency:            {0} ms", xmlFileReader.Latency);
        Console.WriteLine("Probability of injected errors:  {0}", xmlFileReader.ProbabilityError);
        Console.WriteLine("Number of errors per frame:      {0}", xmlFileReader.NumberOfBitErrors);
        Console.WriteLine("********************************************************");
        // Create the transmission support thread
        var transmissionSupport = new TransmissionSupport(xmlFileReader.Latency, xmlFileReader.ProbabilityError, xmlFileReader.NumberOfBitErrors);
        _transmissionSupportThread = new Thread(transmissionSupport.StartPhysicalLayer);
        _transmissionSupportThread.Start();
        Console.WriteLine(string.Format("Started the physical layer thread (Thread Id: {0})",
          _transmissionSupportThread.ManagedThreadId));

        if (xmlFileReader.RejectType == RejectType.Global)
        {
          _transmissionProtocol = new GoBacknProtocol(xmlFileReader.WindowSize, xmlFileReader.Timeout,
            xmlFileReader.AckTimeout, xmlFileReader.InFile, ActorType.Transmitter, transmissionSupport);
          _receptionProtocol = new GoBacknProtocol(xmlFileReader.WindowSize, xmlFileReader.Timeout,
            xmlFileReader.AckTimeout, xmlFileReader.OutFile, ActorType.Receiver, transmissionSupport);
          // Create the transmission/reception network layer threads
          _transmissionNetworkLayerThread = new Thread(_transmissionProtocol.StartTransfer);
          _receptionNetworkLayerThread = new Thread(_receptionProtocol.ReceiveTransfer);
          _transmissionNetworkLayerThread.Start();
          _receptionNetworkLayerThread.Start();
          Console.WriteLine(string.Format("Started the network layer thread of the transmitter (Thread Id: {0})",
            _transmissionNetworkLayerThread.ManagedThreadId));
          Console.WriteLine(string.Format("Started the network layer thread of the receiver (Thread Id: {0})",
            _receptionNetworkLayerThread.ManagedThreadId));
        }
        else
        {
          _transmissionProtocol = new SelectiveRepeatProtocol(xmlFileReader.WindowSize, xmlFileReader.Timeout,
            xmlFileReader.AckTimeout, xmlFileReader.InFile, ActorType.Transmitter, transmissionSupport);
          _receptionProtocol = new SelectiveRepeatProtocol(xmlFileReader.WindowSize, xmlFileReader.Timeout,
            xmlFileReader.AckTimeout, xmlFileReader.OutFile, ActorType.Receiver, transmissionSupport);
          // Create the transmission/reception network layer threads
          _transmissionNetworkLayerThread = new Thread(_transmissionProtocol.StartTransfer);
          _receptionNetworkLayerThread = new Thread(_receptionProtocol.ReceiveTransfer);
          _transmissionNetworkLayerThread.Start();
          _receptionNetworkLayerThread.Start();
          Console.WriteLine(string.Format("Started the network layer thread of the transmitter (Thread Id: {0})",
            _transmissionNetworkLayerThread.ManagedThreadId));
          Console.WriteLine(string.Format("Started the network layer thread of the receiver (Thread Id: {0})",
            _receptionNetworkLayerThread.ManagedThreadId));
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.ToString());
        return 1;
      }

      Console.WriteLine("");
      Console.WriteLine("*****************************************");
      Console.WriteLine("*********** Press ESC to stop ***********");
      Console.WriteLine("*****************************************");
      Console.WriteLine("");
      Console.ReadKey();

      return 0;
    }

    #endregion

    #region Implementation of IDisposable

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <filterpriority>2</filterpriority>
    void IDisposable.Dispose()
    {
      if (_transmissionProtocol != null)
      {
        ((IDisposable)_transmissionProtocol).Dispose();
      }
      if (_receptionProtocol != null)
      {
        ((IDisposable)_receptionProtocol).Dispose();
      }
      if (_transmissionSupportThread != null)
      {
        _transmissionSupportThread.Join();
        _transmissionSupportThread = null;
      }
    }

    #endregion

    #region Private Functions

    private bool ParseArgs(string[] args)
    {
      var count = args.Length;
      for (int ix = 0; ix < count; ix++)
      {
        if (args[ix].Length < 2 || (args[ix][0] != '-' && args[ix][0] != '/'))
        {
          return false;
        }

        char opt = args[ix][1];
        switch (opt)
        {
          case 'f':
            if (args[ix].Length > 2)
            {
              _configFile = args[ix].Substring(2);
            }
            else
            {
              if (++ix == count)
              {
                return false;
              }
              _configFile = args[ix];
            }
            break;
          default:
            return false;
        }
      }
      return true;
    }

    #endregion
  }
}
