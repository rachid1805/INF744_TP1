﻿using System;
using System.Collections;
using System.Threading;

namespace DataLinkApplication
{
  public class SelectiveRepeatProtocol : ProtocolBase
  {
        private static byte _NR_BUFS;
        private bool no_nak;
        private byte oldest_frame;
    #region Constructor

        public SelectiveRepeatProtocol(byte windowSize, int timeout, string fileName, bool inFile, ITransmissionSupport transmissionSupport)
      :base(windowSize, timeout, fileName, inFile, transmissionSupport)
    {
      _NR_BUFS = (byte)((_MAX_SEQ + 1) / 2);
      no_nak = true;
      oldest_frame = (byte)(_MAX_SEQ + 1);
      _communicationThread = new Thread(Protocol);
      _communicationThread.Start();
      Console.WriteLine(string.Format("Starting the data link layer thread of the {0}", inFile ? "transmitter" : "receiver"));
      //_communicationThread.Start();
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
        _closingEvents[_threadId], _networkLayerReadyEvents[_threadId], _frameArrivalEvents[_threadId],
        _frameErrorEvents[_threadId], _frameTimeoutEvents[_threadId],_ackTimeoutEvent
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
                        //var r = FromPhysicalLayer();  /* scratch variable */
                        var a = FromPhysicalLayer();  /* scratch variable */
                        //Decode frame and correct errors using Hamming protocol
                        var r = Hamming.decodeHamming(a);
                        if (r == null)
                        {
                            // _frameErrorE_CKSUM_ERRORvents[(byte)(1 - _threadId)].Set();
                             // goto case _CKSUM_ERROR;
                            break; //reject frame
                        }
                        if (r.Kind == FrameKind.Data)
                        {
                            if ((r.Seq != frameExpected) && no_nak)
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
                                    no_nak = true;
                                    arrived[frameExpected % _NR_BUFS] = false;
                                    Inc(frameExpected);
                                    Inc(tooFar);
                                    StartAckTimer();
                                }
                            }
                        }

                        if ((r.Kind == FrameKind.Nak) && Between(ackExpected, (byte)((r.Ack + 1) % (_MAX_SEQ + 1)), nextFrameToSend))
                        {
                            SendData(FrameKind.Data, (byte)((r.Ack + 1) % (_MAX_SEQ + 1)), (byte)(frameExpected ), outBuffer);      
                        }
                        while (Between(ackExpected, r.Ack, nextFrameToSend))
                        {
                            nbBuffered--;
                            StopTimer(ackExpected % _NR_BUFS); /* frame arrived intact; stop timer */
                            ackExpected = Inc(ackExpected); /* contract sender’s window */
                        }

                        break;
                    case _CKSUM_ERROR:
                        if (no_nak)
                        {
                            SendData(FrameKind.Nak, 0, frameExpected, outBuffer);  //damaged frame
                        }
                        break;
                    case _TIMEOUT:
                            SendData(FrameKind.Data, oldest_frame, frameExpected, outBuffer);           
                        break;
                    case _ACK_TIMEOUT:
                        SendData(FrameKind.Ack, 0, frameExpected, outBuffer);
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
            Console.WriteLine(string.Format("**** The Data Link Layer of the {0} thread terminated ****", _threadId == 0 ? "transmission" : "reception"));
        }

    private void SendData(FrameKind fk,byte frameNb, byte frameExpected, Packet[] buffer)
    {
            Packet data=null;
            if (fk == FrameKind.Data)
                data = buffer[frameNb % _NR_BUFS];
            else
                data = new Packet { Data = new byte[1] { (byte)0 } }; //envoie packet de 0. Pas utilise par application
            if (fk == FrameKind.Nak)
                no_nak = false;
            
            // Construct and send a data frame.
            Frame frame = new Frame
        {
            /* insert packet into frame */
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
            StartTimer(frameNb);
        }

            StopAckTimer();
    }
        #endregion
    }
}
