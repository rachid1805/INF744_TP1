using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace DataLinkApplication
{
  public interface IProtocol
  {
    void Protocol();
    void FromNetworkLayer(Packet packet);
    void FromPhysicalLayer(Frame frame);
  }
}
