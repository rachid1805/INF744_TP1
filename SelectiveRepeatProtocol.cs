﻿using System;
using System.Collections;
using System.Threading;

namespace DataLinkApplication
{
  public class SelectiveRepeatProtocol : ProtocolBase
  {
    private static byte _NR_BUFS;
    private bool _no_nak;
    private byte _oldest_frame;

    #region Constructor

    public SelectiveRepeatProtocol(byte windowSize, int timeout, int ackTimeout, string fileName, ActorType actorType, ITransmissionSupport transmissionSupport)
      : base(windowSize, timeout, ackTimeout, fileName, actorType, transmissionSupport)
    {
      _NR_BUFS = (byte)((_MAX_SEQ + 1) / 2);
      _no_nak = true;
      _oldest_frame = (byte)(_MAX_SEQ + 1);
      _communicationThread = new Thread(Protocol);
      _communicationThread.Start();
      Console.WriteLine(string.Format("Started the data link layer thread of the {0} (Thread Id: {1})", actorType,
        _communicationThread.ManagedThreadId));
    }

    #endregion

    #region Protected Functions

    protected override void DoProtocol()
    {
      byte ackExpected = 0;     /* lower edge of sender's window */
      byte nextFrameToSend = 0; /* upper edge of sender's window */
      byte frameExpected = 0;   /* lower edge of receiver's window */
      byte tooFar = _NR_BUFS;   /* upper edge of receiver's windows*/
      Packet[] outBuffer = new Packet[_NR_BUFS]; /* buffers for the outbound stream */
      Packet[] inBuffer = new Packet[_NR_BUFS]; /* buffers for the inboound stream */
      bool[] arrived = new bool[_NR_BUFS];
      for (int i = 0; i < _NR_BUFS; i++) arrived[i] = false;
      byte nbBuffered = 0;      /* number of output buffers currently in use */
            
      // Creates events that trigger the thread changing
      var waitHandles = new WaitHandle[]
      {
        _closingEvents[_actorType], _networkLayerReadyEvents[_actorType], _frameArrivalEvents[_actorType],
        _frameErrorEvents[_actorType], _frameTimeoutEvents[_actorType],_ackTimeoutEvent
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
            Console.WriteLine("EVENT STOP RUNNING");
            break;
          case _NETWORK_LAYER_READY:
            // The network layer has a packet to send (_networkLayerReadyEvent)
            // In our case, a new packet read from the input file
            // Accept, save, and transmit a new frame.
            // Fetch new packet
            nbBuffered++; /* expand the window */
            if (nbBuffered < _NR_BUFS)
              EnableNetworkLayer();
            else
              DisableNetworkLayer();
            outBuffer[nextFrameToSend % _NR_BUFS] = FromNetworkLayer();
            SendData(FrameKind.Data, nextFrameToSend, frameExpected, outBuffer); /* transmit the frame */
            nextFrameToSend = Inc(nextFrameToSend); /* advance  upper window edge */

            break;
          case _FRAME_ARRIVAL:
            // A data or control frame has arrived (_frameArrivalEvent)
            // In our case, a new packet to write to the output file or received an ack
            // get incoming frame from physical layer
            var a = FromPhysicalLayer();  /* scratch variable */
            //Decode frame and correct errors using Hamming protocol
            var r = Hamming.decodeHamming(a);

            if (r == null)
            {
              // goto case _CKSUM_ERROR;
              break; //reject frame
            }
            if (r.Kind == FrameKind.Data)
            {
              if ((r.Seq != frameExpected) && _no_nak)
              {
                SendData(FrameKind.Nak, 0, frameExpected, outBuffer);

              }
              else
              {
                StartAckTimer();           
              }

              if (Between(frameExpected, r.Seq, tooFar) && (arrived[r.Seq % _NR_BUFS] == false))
              {
                arrived[r.Seq % _NR_BUFS] = true;           //mark buffer as full
                inBuffer[r.Seq % _NR_BUFS] = r.Info;        //insert data
     
                while (arrived[frameExpected % _NR_BUFS])
                {
                  ToNetworkLayer(inBuffer[frameExpected % _NR_BUFS]);
                  _no_nak = true;
                  arrived[frameExpected % _NR_BUFS] = false;
                  frameExpected = Inc(frameExpected);
                  tooFar = Inc(tooFar);
                  StartAckTimer();
                }
              }
            }

            if ((r.Kind == FrameKind.Nak) && Between(ackExpected, (byte)((r.Ack + 1) % (_MAX_SEQ + 1)), nextFrameToSend))
            {
              SendData(FrameKind.Data, (byte)((r.Ack + 1) % (_MAX_SEQ + 1)), frameExpected, outBuffer);
            }
            while (Between(ackExpected, r.Ack, nextFrameToSend))
            {
              nbBuffered--;
              StopTimer(ackExpected % _NR_BUFS); /* frame arrived intact; stop timer */
              ackExpected = Inc(ackExpected); /* contract sender’s window */
              _oldest_frame = ackExpected;
            }

            break;
          case _CKSUM_ERROR:
            if (_no_nak)
            {
              SendData(FrameKind.Nak, 0, frameExpected, outBuffer);  //damaged frame
            }
            break;
          case _TIMEOUT:
            SendData(FrameKind.Data, _oldest_frame, frameExpected, outBuffer);

            break;
          case _ACK_TIMEOUT:
            SendData(FrameKind.Ack, 0, frameExpected, outBuffer);
            break;
        }

        if (nbBuffered < _NR_BUFS)
        {
          EnableNetworkLayer();
        }
        else
        {
          DisableNetworkLayer();
        }
      }
      Console.WriteLine(string.Format("**** The Data Link Layer of the {0} thread terminated ****", _actorType));
    }

    private void SendData(FrameKind fk, byte frameNb, byte frameExpected, Packet[] buffer)
    {
      Packet data = null;
      if (fk == FrameKind.Data)
        data = buffer[frameNb % _NR_BUFS];
      else
        data = new Packet { Data = new byte[1] { (byte)0 } }; //envoie packet de 0. Pas utilise par application
      if (fk == FrameKind.Nak)
        _no_nak = false;

      // Construct and send a data frame.
      Frame frame = new Frame
      {
        /* insert packet into frame */
        Kind = fk,
        Info = data,
        /* insert sequence number into frame */
        Seq = frameNb,
        /* piggyback ack */
        Ack = (byte)((frameExpected + _MAX_SEQ) % (_MAX_SEQ + 1))
      };

      //Encode frame with Hamming protocol
      Frame frameEncoded = Hamming.encodeHamming(frame);

      /* transmit the frame */
      ToPhysicalLayer(frameEncoded);
      /* start the timer running */
      if (fk == FrameKind.Data)
      {
        StartTimer(frameNb % _NR_BUFS);
      }

      StopAckTimer();
    }

    #endregion
  }
}
