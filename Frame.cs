using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLinkApplication
{
  public class Frame
  {
    #region Public Functions

    public FrameKind Kind { get; set; }
    public byte Seq { get; set; }
    public byte Ack { get; set; }
    public Packet Info { get; set; }
    public byte Hamming;

        public static Frame CopyFrom(Frame frame)
    {
      var newFrame = new Frame
      {
        Kind = frame.Kind,
        Seq = frame.Seq,
        Ack = frame.Ack,
        Info = Packet.CopyFrom(frame.Info)
      };

      return newFrame;
    }

    #endregion
  }
}
