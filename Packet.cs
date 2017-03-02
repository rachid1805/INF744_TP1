using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLinkApplication
{
  public class Packet
  {
    #region Constants

    public static int MAX_PKT = 1024;

    #endregion

    #region Attributes

    private byte[] _data;

    #endregion

    #region Properties

    public byte[] Data
    {
      get
      {
        return _data;
      }
      set
      {
        if (value.Length > MAX_PKT)
        {
          throw new ApplicationException("Packet size is too large!");
        }
        _data = value;
      }
    }

    #endregion
  }
}
