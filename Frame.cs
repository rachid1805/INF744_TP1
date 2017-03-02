using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLinkApplication
{
  public class Frame
  {
    #region Properties

    public FrameKind Kind { get; set; }
    public int Seq { get; set; }
    public int Ack { get; set; }
    public Packet Info { get; set; }

    #endregion
  }
}
