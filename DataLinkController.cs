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
    private Thread _transmissionThread;        // (T1) la station émettrice
    private Thread _receptionThread;           // (T2) la station réceptrice
    private Thread _transmissionSupportThread; // (T3) le support de transmission

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
        if (xmlFileReader.RejectType == RejectType.Global)
        {
          _transmissionProtocol = new GoBacknProtocol();
          _receptionProtocol = new GoBacknProtocol();
        }
        else
        {
          _transmissionProtocol = new SelectiveRepeatProtocol();
          _receptionProtocol = new SelectiveRepeatProtocol();
        }

        // Create the transmission support thread
        var transmissionSupport = new TransmissionSupportController();
        _transmissionSupportThread = new Thread(transmissionSupport.TransmissionSupport);
        _transmissionSupportThread.Start();

        // Create the reception thread
        _receptionThread = new Thread(_receptionProtocol.Protocol);
        _receptionThread.Start();

        // Create the transmission thread
        _transmissionThread = new Thread(_transmissionProtocol.Protocol);
        _transmissionThread.Start();
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
      if (_transmissionThread != null)
      {
        _transmissionThread.Join();
        _transmissionThread = null;
      }
      if (_receptionThread != null)
      {
        _receptionThread.Join();
        _receptionThread = null;
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
