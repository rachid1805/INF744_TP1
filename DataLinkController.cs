using System;
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

        // Create the transmission support thread
        var transmissionSupport = new TransmissionSupport(xmlFileReader.Latency);
        _transmissionSupportThread = new Thread(transmissionSupport.PhysicalLayer);
        _transmissionSupportThread.Start();
        Console.WriteLine(string.Format("Started the physical layer thread (Thread Id: {0})",
          _transmissionSupportThread.ManagedThreadId));

        if (xmlFileReader.RejectType == RejectType.Global)
        {
          _transmissionProtocol = new GoBacknProtocol(xmlFileReader.WindowSize, xmlFileReader.Timeout,
            xmlFileReader.InFile, true, transmissionSupport);
          _receptionProtocol = new GoBacknProtocol(xmlFileReader.WindowSize, xmlFileReader.Timeout,
            xmlFileReader.OutFile, false, transmissionSupport);
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
            xmlFileReader.InFile, true, transmissionSupport);
          _receptionProtocol = new SelectiveRepeatProtocol(xmlFileReader.WindowSize, xmlFileReader.Timeout,
            xmlFileReader.OutFile, false, transmissionSupport);
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
