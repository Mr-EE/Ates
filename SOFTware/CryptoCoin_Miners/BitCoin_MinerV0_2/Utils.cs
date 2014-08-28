using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Security.Cryptography;

namespace BitCoin_MinerV0_2
{
    class Utils
    {
        // Define the K contants
        public static readonly UInt32[] K_Array = {
               0x428a2f98,0x71374491,0xb5c0fbcf,0xe9b5dba5,0x3956c25b,0x59f111f1,0x923f82a4,0xab1c5ed5,
               0xd807aa98,0x12835b01,0x243185be,0x550c7dc3,0x72be5d74,0x80deb1fe,0x9bdc06a7,0xc19bf174,
               0xe49b69c1,0xefbe4786,0x0fc19dc6,0x240ca1cc,0x2de92c6f,0x4a7484aa,0x5cb0a9dc,0x76f988da,
               0x983e5152,0xa831c66d,0xb00327c8,0xbf597fc7,0xc6e00bf3,0xd5a79147,0x06ca6351,0x14292967,
               0x27b70a85,0x2e1b2138,0x4d2c6dfc,0x53380d13,0x650a7354,0x766a0abb,0x81c2c92e,0x92722c85,
               0xa2bfe8a1,0xa81a664b,0xc24b8b70,0xc76c51a3,0xd192e819,0xd6990624,0xf40e3585,0x106aa070,
               0x19a4c116,0x1e376c08,0x2748774c,0x34b0bcb5,0x391c0cb3,0x4ed8aa4a,0x5b9cca4f,0x682e6ff3,
               0x748f82ee,0x78a5636f,0x84c87814,0x8cc70208,0x90befffa,0xa4506ceb,0xbef9a3f7,0xc67178f2};

        // Define the initial STATE constants
        public static readonly UInt32[] State_Array = {
               0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19};


        public const String DIFF_ONE = "0000000000000000000000000000000000000000000000000000FFFF00000000" + "00";

        // Block header offsets
        public const Int32 VER_OFFSET = 1;
        public const Int32 MERKLE_OFFSET = 8;
        public const Int32 PREVH_OFFSET = 72;
        public const Int32 TIME_OFFSET = 136;
        public const Int32 BITS_OFFSET = 144;
        public const Int32 NONCE_OFFSET = 152;

        // Properties


        // Methods
        public static Byte[] HexString_To_u8Array(String Hex)
        {
            int i, j = 0;
            Byte[] result = new Byte[Hex.Length / 2];

            for (i = 0, j = 0; i < Hex.Length; i += 2, j++)
            {
                result[j] = Convert.ToByte(Hex.Substring(i, 2), 16);
            }

            return result;
        }

        public static UInt16[] HexString_To_u16Array(String Hex)
        {
            int i, j = 0;
            ushort[] result = new ushort[Hex.Length / 4];

            for (i = 0, j = 0; i < Hex.Length; i += 4, j++)
            {
                result[j] = Convert.ToUInt16(Hex.Substring(i, 4), 16);
            }

            return result;
        }

        public static UInt32[] HexString_To_u32Array(String Hex)
        {
            int i, j = 0;
            UInt32[] result = new UInt32[Hex.Length / 8];

            for (i = 0, j = 0; i < Hex.Length; i += 8, j++)
            {
                result[j] = Convert.ToUInt32(Hex.Substring(i, 8), 16);
            }

            return result;
        }

        public static UInt64[] HexString_To_u64Array(String Hex)
        {
            int i, j = 0;
            UInt64[] result = new UInt64[Hex.Length / 16];

            for (i = 0, j = 0; i < Hex.Length; i += 16, j++)
            {
                result[j] = Convert.ToUInt64(Hex.Substring(i, 16), 16);
            }

            return result;
        }

        public static String u8Array_To_HexString(Byte[] x)
        {
            String Result = "";
            int i = 0;

            for (i = 0; i < x.Length; i++)
            {
                Result = Result + x[i].ToString("X2");
            }

            return Result;
        }

        public static String u32Array_To_HexString(UInt32[] x)
        {
            String Result = "";
            int i = 0;

            for (i = 0; i < x.Length; i++)
            {
                Result = Result + x[i].ToString("X8");
            }

            return Result;
        }

        public static Byte[] u32Array_To_u8Array(UInt32[] x)
        {
            Byte[] result = new Byte[x.Length * 4];
            Byte[] temp0 = new Byte[4];
            int i, j = 0;

            for (i = 0, j = 0; i < x.Length; i++, j += 4)
            {
                temp0 = BitConverter.GetBytes(x[i]);
                result[j] = temp0[3];
                result[j + 1] = temp0[2];
                result[j + 2] = temp0[1];
                result[j + 3] = temp0[0];

                //Array.Copy(BitConverter.GetBytes(x[i]), 0, result, j, 4);
            }

            return result;
        }

        public static String StrHex_Flip(String x)
        {
            String Result = "";
            int i = 0;

            for (i = 0; i < x.Length; i += 2)
            {
                Result = Result + x.Substring((x.Length - 2) - i, 2);
            }

            return Result;
        }

        public static Byte[] u8Array_Flip(Byte[] x)
        {
            Byte[] Result = new Byte[x.Length];
            int i = 0;

            for (i = 0; i < x.Length; i++)
            {
                Result[i] = x[x.Length - 1 - i];
            }

            return Result;
        }

        public static String EndianFlip32BitChunks(String input)
        {
            //32 bits = 4*4 bytes = 4*4*2 chars
            String result = "";
            for (Int32 i = 0; i < input.Length; i += 8)
                for (Int32 j = 0; j < 8; j += 2)
                {
                    //append byte (2 chars)
                    result += input[i - j + 6];
                    result += input[i - j + 7];
                }
            return result;
        }

        public static String RemovePadding(String input)
        {
            //payload length: final 64 bits in big-endian - 0x0000000000000280 = 640 bits = 80 bytes = 160 chars
            return input.Substring(0, 160);
        }

        public static String AddPadding(String input)
        {
            //add the padding to the payload. It never changes.
            return input + "000000800000000000000000000000000000000000000000000000000000000000000000000000000000000080020000";
        }

        public static Int64 GetDiffculty(Byte[] FinalHash)
        {
            // Add an extra zero byte to avoid signed convertion.
            Byte[] tmpFinalHash = new Byte[33];
            Array.Copy(FinalHash, tmpFinalHash, FinalHash.Length);
            tmpFinalHash[32] = 0;

            BigInteger TargetValue = new BigInteger(tmpFinalHash);
            BigInteger DiffOneValue = new BigInteger(HexString_To_u8Array(DIFF_ONE));

            BigInteger tmp = BigInteger.Divide(DiffOneValue, TargetValue);

            return (Int64)tmp;
        }

        public static String GetTargetFromBlockHeader(String BlockHeader)
        {
            String Target = "";

            // Parse bits info from block header
            String Bits = StrHex_Flip(BlockHeader.Substring(BITS_OFFSET, sizeof(UInt32) * 2));

            // Convert Bits to Target {Target = Bits[2:0]*2^(8*(Bits[3]-3))}
            UInt32 a = Convert.ToUInt32(Bits.Substring(0, 2), 16);
            UInt32 b = (Convert.ToUInt32(Bits.Substring(2, 6), 16));
            Int32 c = 8 * ((Int32)a - 3);
            BigInteger d = BigInteger.Pow(2, c);
            BigInteger tmp = BigInteger.Multiply((BigInteger)b, d);
            String tmpTarget = tmp.ToString("X32");
            while (tmpTarget.Length < 64)
                tmpTarget = "0" + tmpTarget;
            Target = StrHex_Flip(tmpTarget);

            return Target;
        }

        public static String GetMidStateFromBlockHeader(String intBlockHeader)
        {
            // Perform Padding
            Byte[][] M = SHA256_MessagePadding(Utils.HexString_To_u8Array(intBlockHeader));
            UInt32 nBlocks = (UInt32)M.Length;

            // Perform the SHA256 Hash on Mn blocks.
            UInt32[] W = new UInt32[64];
            UInt32[] H = new UInt32[8];
            Byte[] Hash = new Byte[32];

            // Set initial State registers
            H = State_Array;

            // Perform SHA256 on first Block
            W = SHA256_MessageSchedule(M[0]);
            H = SHA256_CompressionFunction(H, K_Array, W);

            return Utils.u32Array_To_HexString(H);
        }

        public static UInt32[] SHA256_CompressionFunction(UInt32[] State, UInt32[] K, UInt32[] W)
        {
            // Define variables to be used
            UInt32 a, b, c, d, _e, f, g, h = 0; // need to use '_e' since 'e' is used internally??
            UInt32 T1, T2 = 0;
            int i = 0;
            UInt32[] H = new UInt32[8];

            // Init registers with state values
            Array.Copy(State, H, 8);

            a = H[0];
            b = H[1];
            c = H[2];
            d = H[3];
            _e = H[4];
            f = H[5];
            g = H[6];
            h = H[7];

            // Run compression fuction
            for (i = 0; i < 64; i++)
            {
                T1 = h + EP1(_e) + CH(_e, f, g) + K[i] + W[i];
                T2 = EP0(a) + MAJ(a, b, c);
                h = g;
                g = f;
                f = _e;
                _e = d + T1;
                d = c;
                c = b;
                b = a;
                a = T1 + T2;
            }

            // Compute intermediate hash value
            H[0] += a;
            H[1] += b;
            H[2] += c;
            H[3] += d;
            H[4] += _e;
            H[5] += f;
            H[6] += g;
            H[7] += h;

            return H;
        }

        public static Byte[][] SHA256_MessagePadding(Byte[] Message)
        {
            UInt32 MessageLengthBytes = (UInt32)Message.Length;
            int i = 0;
            UInt32 k = 0;
            Byte[] temp0 = new Byte[8];

            // Pad data to align message size into 64 bytes boundaries:
            // - Append 0x80 to message
            // - Append 'k' zeroes
            // - Append length of orginal message in bits in a BE 64bit (8 byte) format
            // *Set 'k' such that L + 1 + k = 56 MOD 64 } Where L = orginal message length, k = padded "zeroes" bytes, 1 = 1 byte

            // Determine 'k'
            if ((MessageLengthBytes % 64) < 56)
            {
                k = 64 - (MessageLengthBytes % 64) - 8 - 1;
            }
            else
            {
                k = 128 - (MessageLengthBytes % 64) - 8 - 1;
            }

            Byte[] MessagePadding = new Byte[k + 1 + 8];
            Byte[] MessagePadded = new Byte[MessageLengthBytes + MessagePadding.Length];

            // Append 0x80
            MessagePadding[0] = 0x80;

            for (i = 1; i < k; i++)
            {
                MessagePadding[i] = 0;
            }

            // Append orginal message length in bits. Perform a byte swap as BitConverter uses LE format
            temp0 = BitConverter.GetBytes((UInt64)(MessageLengthBytes * 8));
            for (i = 0; i < 8; i++)
            {
                MessagePadding[k + 1 + i] = temp0[7 - i];
            }

            // Copy padding and orginal message into a new byte array aligned to 64 bytes. Append padding to message
            Array.Copy(Message, 0, MessagePadded, 0, MessageLengthBytes);
            Array.Copy(MessagePadding, 0, MessagePadded, MessageLengthBytes, MessagePadding.Length);

            // Parse the data into 64 Byte chunks
            UInt32 nBlocks = (UInt32)(MessagePadded.Length / 64);
            Byte[][] M = new Byte[nBlocks][];
            for (i = 0; i < nBlocks; i++)
            {
                M[i] = new Byte[64];
                Array.Copy(MessagePadded, i * 64, M[i], 0, 64);
            }

            return M;
        }

        public static UInt32[] SHA256_MessageSchedule(Byte[] Message)
        {
            UInt32[] W = new UInt32[64];
            byte[] temp = new byte[4];
            int i, j = 0;

            // First copy message into the first 16 spots of the uint32 array. Copy as big endian
            for (i = 0, j = 0; i < 16; i++, j += 4)
            {
                temp[3] = Message[j];
                temp[2] = Message[j + 1];
                temp[1] = Message[j + 2];
                temp[0] = Message[j + 3];

                W[i] = BitConverter.ToUInt32(temp, 0); // Need to flip bytes before this function since it passes uint32 as little endian
            }

            // Now expand message into next 48 bytes using SHA256 algorithms
            for (i = 16; i < 64; i++)
            {
                W[i] = W[i - 16] + SIG0(W[i - 15]) + W[i - 7] + SIG1(W[i - 2]);
            }

            return W;
        }

        public static UInt32 CH(UInt32 x, UInt32 y, UInt32 z)
        {
            return ((x & y) ^ (~x & z));
        }

        public static UInt32 MAJ(UInt32 x, UInt32 y, UInt32 z)
        {
            return ((x & y) ^ (x & z) ^ (y & z));
        }

        public static UInt32 EP0(UInt32 x)
        {
            return (ROTRIGHT(x, 2) ^ ROTRIGHT(x, 13) ^ ROTRIGHT(x, 22));
        }

        public static UInt32 EP1(UInt32 x)
        {
            return (ROTRIGHT(x, 6) ^ ROTRIGHT(x, 11) ^ ROTRIGHT(x, 25));
        }

        public static UInt32 ROTLEFT(UInt32 x, Byte n)
        {
            return (UInt32)((x << n) | (x >> (32 - n)));
        }

        public static UInt32 ROTRIGHT(UInt32 x, Byte n)
        {
            return (UInt32)((x >> n) | (x << (32 - n)));
        }

        public static UInt32 SIG0(UInt32 x)
        {
            return (ROTRIGHT(x, 7) ^ ROTRIGHT(x, 18) ^ (x >> 3));
        }

        public static UInt32 SIG1(UInt32 x)
        {
            return (ROTRIGHT(x, 17) ^ ROTRIGHT(x, 19) ^ (x >> 10));
        }

        /*
        private String FlipEachu32StrHex(String x)
        {
            String Result = "";
            String tmp1 = "";
            String tmp2 = "";
            int i, j = 0;

            for (i = 1; i <= x.Length; i += 8)
            {
                tmp1 = x.Substring(i, 8);
                tmp2 = "";

                for (j = 8; j > 0; j -= 2)
                    tmp2 = tmp2 + tmp1.Substring(i - 1, 2);

                Result += tmp2;
            }

            return Result;
        }
        */


    }


}
