using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLinkApplication
{
  public class Packet
  {
    #region Constants

    public static int _MAX_PKT = 1024;

    #endregion

    #region Attributes

    private byte[] _data;

    #endregion

    #region Public Functions

    public byte[] Data
    {
      get
      {
        return _data;
      }
      set
      {
        if (value.Length > _MAX_PKT)
        {
          throw new ApplicationException("Packet size is too large!");
        }
        _data = value;
      }
    }

    public static Packet CopyFrom(Packet packet)
    {
      var newByte = new byte[packet.Data.Length];
      for (int i = 0; i < packet.Data.Length; i++)
      {
        newByte[i] = packet.Data[i];
      }
      var newPacket = new Packet { Data = newByte };

      return newPacket;
    }

    #endregion
  }
}
