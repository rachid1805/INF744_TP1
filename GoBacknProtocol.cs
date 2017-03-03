using System;
using System.Collections;
using System.Threading;

namespace DataLinkApplication
{
  public class GoBacknProtocol : ProtocolBase
  {
    #region Constructor

    public GoBacknProtocol(byte windowSize, int timeout, string fileName, bool inFile, ITransmissionSupport transmissionSupport)
      :base(windowSize, timeout, fileName, inFile, transmissionSupport)
    {
      _communicationThread = new Thread(Protocol);
      Console.WriteLine(string.Format("Starting the data link layer thread of the {0}", inFile ? "transmitter" : "receiver"));
      _communicationThread.Start();
    }

    #endregion

    #region Protected Functions

    protected override void DoProtocol()
    {
      Packet[] buffer = new Packet[_MAX_SEQ + 1]; /* buffers for the outbound stream */
      byte ackExpected = 0;     /* oldest frame as yet unacknowledged */
      byte nextFrameToSend = 0; /* MAX SEQ > 1; used for outbound stream */
      byte frameExpected = 0;   /* next frame expected on inbound stream */
      byte nbBuffered = 0;      /* number of output buffers currently in use */

      // Creates events that trigger the thread changing
      var waitHandles = new WaitHandle[]
      {
        _closingEvents[_threadId], _networkLayerReadyEvents[_threadId], _frameArrivalEvents[_threadId],
        _frameErrorEvents[_threadId], _frameTimeoutEvents[_threadId]
      };

      /* allow network layer ready events */
      EnableNetworkLayer();

      var running = true;
      while (running)
      {
        // Wait trigger events to activate the thread action
        var index = WaitHandle.WaitAny(waitHandles);

        // Execute the task related to the event raised
        switch (index)
        {
          case _STOP_RUNNING:
            // Received the closing event (_closingEvent)
            running = false;
            break;
          case _NETWORK_LAYER_READY:
            // The network layer has a packet to send (_networkLayerReadyEvent)
            // In our case, a new packet read from the input file
            // Accept, save, and transmit a new frame.
            // Fetch new packet
            buffer[nextFrameToSend] = FromNetworkLayer();
            nbBuffered++; /* expand the sender’s window */
            Console.WriteLine(string.Format("New packet buffer 0x{0:X} from Network layer of {1} (Thread Id: {2})",
              buffer[nextFrameToSend].Data[0], (_threadId == 0) ? "transmitter" : "receiver", Thread.CurrentThread.ManagedThreadId));
            SendData(nextFrameToSend, frameExpected, buffer); /* transmit the frame */
            nextFrameToSend = Inc(nextFrameToSend); /* advance sender’s upper window edge */
            break;
          case _FRAME_ARRIVAL:
            // A data or control frame has arrived (_frameArrivalEvent)
            // In our case, a new packet to write to the output file or received an ack
            // get incoming frame from physical layer
            var r = FromPhysicalLayer();  /* scratch variable */
            Console.WriteLine(string.Format("New frame buffer 0x{0:X} from physiacl layer of {1} (Thread Id: {2})",
              r.Info.Data[0], (_threadId == 0) ? "transmitter" : "receiver", Thread.CurrentThread.ManagedThreadId));
            if (r.Seq == frameExpected)
            {
              // Frames are accepted only in order.
              ToNetworkLayer(r.Info); /* pass packet to network layer */
              frameExpected = Inc(frameExpected); /* advance lower edge of receiver’s window */
            }
            // Ack n implies n − 1, n − 2, etc.Check for this.
            while (Between(ackExpected, r.Ack, nextFrameToSend))
            {
              // Handle piggybacked ack.
              nbBuffered--; /* one frame fewer buffered */
              StopTimer(ackExpected); /* frame arrived intact; stop timer */
              ackExpected = Inc(ackExpected); /* contract sender’s window */
            }
            break;
          case _CKSUM_ERROR:
            // Just ignore bad frames (_frameErrorEvent)
            break;
          case _TIMEOUT:
            // Trouble; retransmit all outstanding frames (_frameTimeoutEvent)
            nextFrameToSend = ackExpected; /* start retransmitting here */
            for (byte i = 1; i <= nbBuffered; i++)
            {
              SendData(nextFrameToSend, frameExpected, buffer); /* resend frame */
              nextFrameToSend = Inc(nextFrameToSend); /* prepare to send the next one */
            }
            break;
        }

        if (nbBuffered < _MAX_SEQ)
        {
          EnableNetworkLayer();
        }
        else
        {
          DisableNetworkLayer();
        }
      }
    }

    #endregion

    #region Private Functions

    private void SendData(byte frameNb, byte frameExpected, Packet[] buffer)
    {
      // Construct and send a data frame.
      Frame frame = new Frame
      {
        /* insert packet into frame */
        Info = buffer[frameNb],
        /* insert sequence number into frame */
        Seq = frameNb,
        /* piggyback ack */
        Ack = (byte) ((frameExpected + _MAX_SEQ) % (_MAX_SEQ + 1))
      };
      /* transmit the frame */
      ToPhysicalLayer(frame);
      /* start the timer running */
      StartTimer(frameNb);
    }

    #endregion
  }
}
