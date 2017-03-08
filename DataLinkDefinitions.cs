using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLinkApplication
{
  public enum RejectType
  {
    Global,
    Selective
  }

  public enum FrameKind
  {
    Data,
    Ack,
    Nak
  }

  public enum ActorType
  {
    Transmitter = 0,
    Receiver = 1
  }
}
