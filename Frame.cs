using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLinkApplication
{
  public class Frame
  {
    private static readonly Random s_random = new Random();

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
        Info = Packet.CopyFrom(frame.Info),
        Hamming = frame.Hamming
      };

      return newFrame;
    }

    public static Frame CorruptFrame(Frame frameToCorrupt, byte numberOfBitsToCorrupt)
    {
      string binaryKind = Convert.ToString((int)frameToCorrupt.Kind, 2).PadLeft(8, '0');
      string binarySeq = Convert.ToString((int)frameToCorrupt.Seq, 2).PadLeft(8, '0');
      string binaryAck = Convert.ToString((int)frameToCorrupt.Ack, 2).PadLeft(8, '0');
      string binaryData = Convert.ToString((int)frameToCorrupt.Info.Data[0], 2).PadLeft(8, '0');
      string binaryHamming = Convert.ToString((int)frameToCorrupt.Hamming, 2).PadLeft(8, '0');
      string name = binaryKind + binarySeq + binaryAck + binaryData + binaryHamming;

      StringBuilder strBuilder = new StringBuilder(name);

      while (numberOfBitsToCorrupt > 0)
      {
        var bitToCorrupt = s_random.Next(39);

        if (name[bitToCorrupt].Equals('1'))
        {
          strBuilder[bitToCorrupt] = '0';
        }
        else
        {
          strBuilder[bitToCorrupt] = '1';
        }
        numberOfBitsToCorrupt--;
      }

      return new Frame
      {
        Kind = (FrameKind)((byte)Convert.ToInt32(strBuilder.ToString(0, 8), 2)),
        Seq = (byte)Convert.ToInt32(strBuilder.ToString(8, 8), 2),
        Ack = (byte)Convert.ToInt32(strBuilder.ToString(16, 8), 2),
        Info = new Packet { Data = new byte[1] { (byte)Convert.ToInt32(strBuilder.ToString(24, 8), 2) } },
        Hamming = (byte)Convert.ToInt32(strBuilder.ToString(32, 8), 2)
      };
    }

    #endregion
  }
}
