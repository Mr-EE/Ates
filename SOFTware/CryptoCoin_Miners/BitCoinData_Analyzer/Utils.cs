using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Security.Cryptography;

namespace BitCoinData_Analyzer
{
    class Utils
    {
        public const String DIFF_ONE = "0000000000000000000000000000000000000000000000000000FFFF00000000";

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

        public static Int64 GetDiffculty(Byte[] FinalHash)
        {
            BigInteger TargetValue = new BigInteger(FinalHash);
            BigInteger DiffOneValue = new BigInteger(HexString_To_u8Array(DIFF_ONE));

            BigInteger tmp = BigInteger.Divide(DiffOneValue, TargetValue);

            return (Int64)tmp;
        }
    }


}
