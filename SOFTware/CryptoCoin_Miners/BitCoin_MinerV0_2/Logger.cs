using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BitCoin_MinerV0_2
{
    class Logger
    {
        // -- Constants ---------------------------------------------------------
        // w0-w2 offset points
        public const Int32 W0_OFFSET = 128;
        public const Int32 W1_OFFSET = 136;
        public const Int32 W2_OFFSET = 144;

        public const UInt32 W9_VALUE = 0;
        public const UInt32 W10_VALUE = 0;
        public const UInt32 W14_VALUE = 0;
        public const UInt32 W15_VALUE = 640;

        // -- Properties ---------------------------------------------------------
        public String BlockHeader { get; set; }
        public String Midstate { get; set; }
        public UInt32 w_1_0 { get; set; }
        public UInt32 w_1_1 { get; set; }
        public UInt32 w_1_2 { get; set; }
        public UInt32 w_1_16 { get; set; }
        public UInt32 w_1_17 { get; set; }

        public Int32 LogInterval { get; set; }


// -- Methods ------------------------------------------------------------
        public void ResetLogger()
        {
            BlockHeader = "";
            Midstate = "";
            w_1_0 = 0;
            w_1_1 = 0;
            w_1_2 = 0;
            w_1_16 = 0;
            w_1_17 = 0;

            LogInterval = 0; // Default 0 - disabled.
        }

        public void UpdateLogger(String intBlockHeader, Int64 intNonce, String intMidState)
        {
            String intMessage = intBlockHeader.Substring(0, intBlockHeader.Length - 8);
            Byte[] NonceArray = new Byte[4];

            // Pass nonce value to byte array but convert to LE
            NonceArray[3] = (Byte)intNonce;
            NonceArray[2] = (Byte)(intNonce >> 8);
            NonceArray[1] = (Byte)(intNonce >> 16);
            NonceArray[0] = (Byte)(intNonce >> 24);

            BlockHeader = intMessage + u8Array_To_HexString(NonceArray);
            Midstate = intMidState;
            //w_1_0 = Convert.ToUInt32(intBlockHeader.Substring(W0_OFFSET, 8), 16);
            //w_1_1 = Convert.ToUInt32(intBlockHeader.Substring(W1_OFFSET, 8), 16);
            //w_1_2 = Convert.ToUInt32(intBlockHeader.Substring(W2_OFFSET, 8), 16);
            //w_1_16 = w_1_0 + SIG0(w_1_1) + W9_VALUE + SIG1(W14_VALUE);
            //w_1_17 = w_1_1 + SIG0(w_1_2) + W10_VALUE + SIG1(W15_VALUE);
        }

        public String CreateOutputString()
        {
            //return BlockHeader + "," + Midstate + "," + w_1_0 + "," + w_1_1 + "," + w_1_2 + "," + w_1_16 + "," + w_1_17;
            return BlockHeader + "," + Midstate;
        }

        // Some utils used within class
        private String u8Array_To_HexString(Byte[] x)
        {
            String Result = "";
            int i = 0;

            for (i = 0; i < x.Length; i++)
            {
                Result = Result + x[i].ToString("X2");
            }

            return Result;
        }

        private UInt32 ROTLEFT(UInt32 x, Byte n)
        {
            return (UInt32)((x << n) | (x >> (32 - n)));
        }

        private UInt32 ROTRIGHT(UInt32 x, Byte n)
        {
            return (UInt32)((x >> n) | (x << (32 - n)));
        }

        private UInt32 SIG0(UInt32 x)
        {
            return (ROTRIGHT(x, 7) ^ ROTRIGHT(x, 18) ^ (x >> 3));
        }

        private UInt32 SIG1(UInt32 x)
        {
            return (ROTRIGHT(x, 17) ^ ROTRIGHT(x, 19) ^ (x >> 10));
        }

// -- Instance Constructors ----------------------------------------------
        public Logger()
        {
            BlockHeader = "";
            Midstate = "";
            w_1_0 = 0;
            w_1_1 = 0;
            w_1_2 = 0;
            w_1_16 = 0;
            w_1_17 = 0;

            LogInterval = 0; // Default 0 - disabled.
        }
    }
}
