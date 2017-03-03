using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLinkApplication
{
  public interface ITransmissionSupport
  {
    void PhysicalLayer();
    bool ReadyToSend { get; }
    void SendFrame(Frame frame);
    bool ReadyToReceive { get; }
    Frame ReceiveFrame();
  }
}
