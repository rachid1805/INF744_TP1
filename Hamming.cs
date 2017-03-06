using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataLinkApplication
{
    class Hamming
    {
        //pas d'instance de classe
        private Hamming() { }

        public static Frame encodeHamming(Frame frame)
        {

            //convert to binary representation
            string binaryKind = Convert.ToString((int)frame.Kind, 2).PadLeft(8, '0');
            string binarySeq = Convert.ToString((int)frame.Seq, 2).PadLeft(8, '0');
            string binaryAck = Convert.ToString((int)frame.Ack, 2).PadLeft(8, '0');
            byte[] data = frame.Info.Data;
            string binaryData = Convert.ToString((int)data[0], 2).PadLeft(8, '0');

            //concatenate 
            string name = binaryKind + binarySeq + binaryAck + binaryData;

            //calculate hamming code
            byte HammingByte = calculateParity(name);

            //return frame with hamming code
            Frame f = new Frame
            {
                Kind = frame.Kind,
                Seq = frame.Seq,
                Ack = frame.Ack,
                Info = frame.Info,
                Hamming = HammingByte,
            };
            return f;
        }

        public static Frame decodeHamming(Frame frame)
        {
            //convert to binary representation
            string binaryKind = Convert.ToString((int)frame.Kind, 2).PadLeft(8, '0');
            string binarySeq = Convert.ToString((int)frame.Seq, 2).PadLeft(8, '0');
            string binaryAck = Convert.ToString((int)frame.Ack, 2).PadLeft(8, '0');
            byte[] data = frame.Info.Data;
            string binaryData = Convert.ToString((int)data[0], 2).PadLeft(8, '0');
            byte binaryHammingInput = frame.Hamming;

            //concatenate 
            string name = binaryKind + binarySeq + binaryAck + binaryData;

            //calculate hamming code
            byte HammingByteCalculated = calculateParity(name);

            byte compareHamming = (byte)(binaryHammingInput ^ HammingByteCalculated);

            if (compareHamming == 0)
            {
                //return frame without hamming code
                Frame f = new Frame
                {
                    Kind = frame.Kind,
                    Seq = frame.Seq,
                    Ack = frame.Ack,
                    Info = frame.Info,
                    //Hamming = HammingByte,
                };
                return f;
            }
            else
            {
                string CompareHammingBits = Convert.ToString(compareHamming, 2).PadLeft(6, '0');
                int positionError = 0;

                if (CompareHammingBits[0].Equals('1')) { positionError = positionError + 1; }
                if (CompareHammingBits[1].Equals('1')) { positionError = positionError + 2; }
                if (CompareHammingBits[2].Equals('1')) { positionError = positionError + 4; }
                if (CompareHammingBits[3].Equals('1')) { positionError = positionError + 8; }
                if (CompareHammingBits[4].Equals('1')) { positionError = positionError + 16; }
                if (CompareHammingBits[5].Equals('1')) { positionError = positionError + 32; }

                System.Text.StringBuilder strBuilderName = new System.Text.StringBuilder(name);
                strBuilderName.Insert(0, CompareHammingBits[0]);
                strBuilderName.Insert(1, CompareHammingBits[1]);
                strBuilderName.Insert(3, CompareHammingBits[2]);
                strBuilderName.Insert(7, CompareHammingBits[3]);
                strBuilderName.Insert(15, CompareHammingBits[4]);
                strBuilderName.Insert(31, CompareHammingBits[5]);

                //correct error
                if (strBuilderName[positionError - 1].Equals('1'))
                {
                    strBuilderName[positionError - 1] = '0';
                }
                else
                {
                    strBuilderName[positionError - 1] = '1';
                }

                //remove correcting bits
                strBuilderName.Remove(0, 1);
                strBuilderName.Remove(0, 1);
                strBuilderName.Remove(1, 1);
                strBuilderName.Remove(4, 1);
                strBuilderName.Remove(11, 1);
                strBuilderName.Remove(26, 1);

                Packet packetData = new Packet { Data = new byte[1] { (byte)Convert.ToInt32(strBuilderName.ToString(24, 8), 2) } };
                Frame f2 = new Frame
                {
                    Kind = (FrameKind)((byte)Convert.ToInt32(strBuilderName.ToString(0, 8), 2)),
                    Seq = (byte)Convert.ToInt32(strBuilderName.ToString(8, 8), 2),
                    Ack = (byte)Convert.ToInt32(strBuilderName.ToString(16, 8), 2),
                    Info = packetData,
                    //Hamming = HammingByte,
                };
                return f2;
            }
        }
        private static byte calculateParity(String name)
        {
            //Calculate parity bits
            int P0 = ((name[0] - '0') + (name[1] - '0') + (name[3] - '0') + (name[4] - '0') + (name[6] - '0') + (name[8] - '0') + (name[10] - '0') + (name[11] - '0') + (name[13] - '0') + (name[15] - '0') + (name[17] - '0') + (name[19] - '0') + (name[21] - '0') + (name[23] - '0') + (name[25] - '0') + (name[26] - '0') + (name[28] - '0') + (name[30] - '0')) % 2;
            int P1 = ((name[0] - '0') + (name[2] - '0') + (name[3] - '0') + (name[5] - '0') + (name[6] - '0') + (name[9] - '0') + (name[10] - '0') + (name[12] - '0') + (name[13] - '0') + (name[16] - '0') + (name[17] - '0') + (name[20] - '0') + (name[21] - '0') + (name[24] - '0') + (name[25] - '0') + (name[27] - '0') + (name[28] - '0') + (name[31] - '0')) % 2;
            int P2 = ((name[1] - '0') + (name[2] - '0') + (name[3] - '0') + (name[7] - '0') + (name[8] - '0') + (name[9] - '0') + (name[10] - '0') + (name[14] - '0') + (name[15] - '0') + (name[16] - '0') + (name[17] - '0') + (name[22] - '0') + (name[23] - '0') + (name[24] - '0') + (name[25] - '0') + (name[29] - '0') + (name[30] - '0') + (name[31] - '0')) % 2;
            int P3 = ((name[4] - '0') + (name[5] - '0') + (name[6] - '0') + (name[7] - '0') + (name[8] - '0') + (name[9] - '0') + (name[10] - '0') + (name[18] - '0') + (name[19] - '0') + (name[20] - '0') + (name[21] - '0') + (name[22] - '0') + (name[23] - '0') + (name[24] - '0') + (name[25] - '0')) % 2;
            int P4 = ((name[11] - '0') + (name[12] - '0') + (name[13] - '0') + (name[14] - '0') + (name[15] - '0') + (name[16] - '0') + (name[17] - '0') + (name[18] - '0') + (name[19] - '0') + (name[20] - '0') + (name[21] - '0') + (name[22] - '0') + (name[23] - '0') + (name[24] - '0') + (name[25] - '0')) % 2;
            int P5 = ((name[26] - '0') + (name[27] - '0') + (name[28] - '0') + (name[29] - '0') + (name[30] - '0') + (name[30] - '0')) % 2;

            int HammingB = (P0 << 5) | (P1 << 4) | (P2 << 3) | (P3 << 2) | (P4 << 1) | P5;
            byte HammingByte = (byte)HammingB;
            return HammingByte;
        }
    }
}
