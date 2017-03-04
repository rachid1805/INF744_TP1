using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLinkApplication
{
  public interface ITransmissionSupport
  {
    void StartPhysicalLayer();
    void StopPhysicalLayer();

    // Data
    bool ReadyToSendData { get; }
    void SendData(Frame frame);
    bool ReadyToReceiveData { get; }
    Frame ReceiveData();

    // Ack
    bool ReadyToSendAck { get; }
    void SendAck(Frame frame);
    bool ReadyToReceiveAck { get; }
    Frame ReceiveAck();
  }
}
