using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using Cloo;

namespace BitCoin_MinerV0_2
{
    class Worker
    {
// -- Constants ----------------------------------------------------------
        //public readonly String FILE_OPENCL_CUSTOM = "sha256_kernal.cl";
        //public readonly String FILE_OPENCL_CUSTOM_MINMAX = "sha256_kernal_MinMax.cl";
        public const Int32 OPENCL_CUSTOM = 0;
        public const Int32 OPENCL_CUSTOM_MIN = 1;

        public readonly Int64 OFFSET_DEFAULT = 0; // Starting point //490000000;
        public readonly Int64 GLOBALSIZE_DEFAULT = 1048576; // Default batch size
        public readonly Int64 LOCALSIZE_DEFAULT = 64; // Local Group size

        public readonly UInt32 KERNELSIZE_DEFAULT = 1; // Kernerl Group size variable. How many hashes to perform per kernerl execution

        public readonly Int64 GLOBALSIZE_ANALYSIS = 4096; // Batch size for data analysis
        public readonly UInt32 KERNELSIZE_ANALYSIS = 65536; // Kernerl Group size variable for data analysis
        public readonly Int32 BATCHLOOPS_ANALYSIS = 16; // Number of batch loops to perform

        public readonly Int32 NONCE_ERROR = 0;
        public readonly Int32 NONCE_FOUND = 1;
        public readonly Int32 MAXNONCE_REACHED = 2;
        public readonly Int32 NONCE_NOTFOUND = 3;

        // w0-w2 offset points
        public const Int32 W0_OFFSET = 128;
        public const Int32 W1_OFFSET = 136;
        public const Int32 W2_OFFSET = 144;

// -- Properties ---------------------------------------------------------
        public String BlockHeader { get; set; } // The block header format in Little Endian used for hashing
        public String MidState { get; set; }
        public String Target { get; set; }
        //public UInt32 startNonce { get; set; }
        //public UInt32 stopNonce { get; set; }
        //public UInt32 ThreadBatch { get; set; }
        public Int32 ThreadCoolDown { get; set; }

        public String ValidBlockHeader { get; set; }

        /*
        // Block header sections
        public String Version { get; set; }
        public String PreviousHash { get; set; }
        public String MerkletRoot { get; set; }
        public String Time { get; set; }
        public String Bits { get; set; }
        public String Nonce { get; set; }
        */

        // Open CL properties
        public String cl_Source { get; set; }
        public ComputePlatform cl_Platform { get; set; }
        public IList<ComputeDevice> cl_Devices { get; set; }
        public ComputeContextPropertyList cl_Properties { get; set; }
        public ComputeContext cl_Context { get; set; }
        public ComputeProgram cl_Program { get; set; } 
        public ComputeKernel cl_Kernel { get; set; } 
        public ComputeCommandQueue cl_Commands { get; set; }

        // NDRrange variables
        public Int64[] workerOffset { get; set; }
        public Int64[] workerGlobalSize { get; set; }
        public Int64[] workerLocalSize { get; set; }

        public UInt32[] workerKernelSize { get; set; }

        public Int32 CompletedWorkFlag { get; set; }

        public Int32 KernelSelection { get; set; }

        public List<UInt32[]> MinArray_List { get; set; }

        //private Byte[] intBlockHeader { get; set; }
        //private Byte[] intTarget { get; set; }
        public Byte[] FinalHash { get; set; }
        public UInt32 NonceToCheck { get; set; }
        public UInt32 ValidNonce { get; set; }

        // Kernal Arguments
        private UInt32[] hMidState { get; set; }
        private UInt32[] wIn { get; set; }
        //public UInt32[] GroupSizeIn { get; set; }
        public UInt32[] NonceOut { get; set; }
        
        // Kernel buffers
        private ComputeBuffer<UInt32> var_MidStateIn;
        private ComputeBuffer<UInt32> var_wIn;
        private ComputeBuffer<UInt32> var_GroupSizeIn;
        private ComputeBuffer<UInt32> var_NonceOut;


// -- Methods ------------------------------------------------------------
        public void BuildOpenCL()
        {
            //String x = Environment.CurrentDirectory;
            //cl_Source = File.ReadAllText(x + "\\" + OPENCL_CUSTOM);
            //cl_Source = File.ReadAllText("..\\..\\..\\OpenCL\\" + OPENCL_CUSTOM);

            if (KernelSelection == OPENCL_CUSTOM)
                cl_Source = Encoding.UTF8.GetString(Properties.Resources.sha256_kernal);
            else if (KernelSelection == OPENCL_CUSTOM_MIN)
                cl_Source = Encoding.UTF8.GetString(Properties.Resources.sha256_kernal_wMin);
            else
                cl_Source = Encoding.UTF8.GetString(Properties.Resources.sha256_kernal);


            cl_Properties = new ComputeContextPropertyList(cl_Platform);

            cl_Context = new ComputeContext(cl_Devices, cl_Properties, null, IntPtr.Zero);

            // Create the command queue. This is used to control kernel execution and manage read/write/copy operations.
            cl_Commands = new ComputeCommandQueue(cl_Context, cl_Context.Devices[0], ComputeCommandQueueFlags.None);

            // Create program object
            cl_Program = new ComputeProgram(cl_Context, cl_Source);

            //Compiles the source codes.
            cl_Program.Build(null, null, null, IntPtr.Zero);

            // Create the kernel function
            cl_Kernel = cl_Program.CreateKernel("search");
        }

        public void SetUpOpenCL()
        {
            // Grab data for Kernel arguments
            ExtractKernelArguments();

            // Create the input buffers and fill them with data from the arrays.
            // Access modifiers should match those in a kernel.
            // CopyHostPointer means the buffer should be filled with the data provided in the last argument.
            var_MidStateIn = new ComputeBuffer<UInt32>(cl_Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, hMidState);
            var_wIn = new ComputeBuffer<UInt32>(cl_Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, wIn);
            var_GroupSizeIn = new ComputeBuffer<UInt32>(cl_Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, workerKernelSize);

            // Write to the output to force a re-init of data in case Kernel doesn't write to it during normal operation (i.e. conditional branches)
            var_NonceOut = new ComputeBuffer<UInt32>(cl_Context, ComputeMemoryFlags.WriteOnly | ComputeMemoryFlags.CopyHostPointer, NonceOut);               

            // Set Kernel arguments
            cl_Kernel.SetMemoryArgument(0, var_MidStateIn);
            cl_Kernel.SetMemoryArgument(1, var_wIn);
            cl_Kernel.SetMemoryArgument(2, var_GroupSizeIn);
            cl_Kernel.SetMemoryArgument(3, var_NonceOut);
        }

        public bool ExecuteOpenCL0()
        {
            UInt32[] intNonceOut = new UInt32[NonceOut.Length];

            // Execute the kernel "count" times.
            cl_Commands.Execute(cl_Kernel, workerOffset, workerGlobalSize, workerLocalSize, null);
            //cl_Commands.Flush();
            //cl_Commands.Finish();

            // Read back the results.
            cl_Commands.ReadFromBuffer(var_NonceOut, ref intNonceOut, true, null);

            //cl_Commands.Finish();

            if (intNonceOut[0] == 1)
            {
                NonceOut = intNonceOut;
                return true;
            }
            else
                return false;
        }

        public UInt32[] ExecuteOpenCL1()
        {
            UInt32[] intNonceOut = new UInt32[NonceOut.Length];

            // Execute the kernel "count" times.
            cl_Commands.Execute(cl_Kernel, workerOffset, workerGlobalSize, workerLocalSize, null);
            //cl_Commands.Flush();
            //cl_Commands.Finish();

            // Read back the results.
            cl_Commands.ReadFromBuffer(var_NonceOut, ref intNonceOut, true, null);

            //cl_Commands.Finish();

            return intNonceOut;
        }

        private void ExtractKernelArguments()
        {
            hMidState = HexString_To_u32Array(MidState);
            wIn = HexString_To_u32Array(BlockHeader.Substring(W0_OFFSET, 24)); // Grab last three 32bit values from blockheader (wo, w1, w2)

            if (KernelSelection == OPENCL_CUSTOM)
                NonceOut = new UInt32[2];
            else if (KernelSelection == OPENCL_CUSTOM_MIN)
                NonceOut = new UInt32[(UInt32)workerGlobalSize[0]];
            else
                NonceOut = new UInt32[2];
        }

        public void DiposeMemObjects()
        {
            var_MidStateIn.Dispose();
            var_wIn.Dispose();
            var_GroupSizeIn.Dispose();
            var_NonceOut.Dispose();
        }

        public bool VerifyWork(UInt32 tmpNonce)
        {
            SHA256Managed Hash256Engine = new SHA256Managed();
            String intMessage = BlockHeader.Substring(0, BlockHeader.Length - 8);
            Byte[] intMessageByteArray = new Byte[80];
            Byte[] hashResult = new Byte[32];
            Byte[] NonceArray = new Byte[4];

            // Pass nonce value to byte array but convert to LE
            NonceArray[3] = (Byte)tmpNonce;
            NonceArray[2] = (Byte)(tmpNonce >> 8);
            NonceArray[1] = (Byte)(tmpNonce >> 16);
            NonceArray[0] = (Byte)(tmpNonce >> 24);

            intMessageByteArray = HexString_To_u8Array(intMessage + u8Array_To_HexString(NonceArray));

            // Perform SHA256 twice
            hashResult = Hash256Engine.ComputeHash(intMessageByteArray, 0, intMessageByteArray.Length);
            hashResult = Hash256Engine.ComputeHash(hashResult, 0, hashResult.Length);


            // Check to see if this was valid work reported by OpenCL program. 
            Int32 i = 0;
            Int32 j = 31;
            while ( (hashResult[j] == 0)  && (j != 0) )
            {
                j--;
                i++;
            }

            if (i >= 4)
            {
                ValidBlockHeader = u8Array_To_HexString(intMessageByteArray);
                ValidNonce = tmpNonce;
                FinalHash = hashResult;
                return true;
            }
            else
            {
                ValidBlockHeader = "";
                ValidNonce = 0;
                Array.Clear(FinalHash, 0, FinalHash.Length);
                return false;
            }
        }

        public bool CompareTarget()
        {
            // Add an extra zero byte to avoid signed convertion.
            Byte[] tmpFinalHash = new Byte[33];
            Array.Copy(FinalHash, tmpFinalHash, FinalHash.Length);
            tmpFinalHash[32] = 0;

            BigInteger FinalHashValue = new BigInteger(tmpFinalHash);
            BigInteger TargetValue = new BigInteger(HexString_To_u8Array(Target + "00"));

            if (FinalHashValue <= TargetValue)
                return true;
            else
                return false;
        }

        public UInt32 FindMinHashIndex()
        {
            SHA256Managed Hash256Engine = new SHA256Managed();
            String intMessage = BlockHeader.Substring(0, BlockHeader.Length - 8);
            Byte[] intMessageByteArray = new Byte[80];
            Byte[] hashResult = new Byte[32];
            Byte[] NonceArray = new Byte[4];
            UInt32 NonceOut = 0;
            UInt32 NonceIndex = 0;

            UInt32[] intMinArray;
            Byte[] tmpFinalHashValue = new Byte[33];

            BigInteger FinalHashValue;
            BigInteger HashValueMin = BigInteger.Pow(2, 256) - (BigInteger)1;

            for (Int32 i = 0; i < MinArray_List.Count; i++)
            {
                intMinArray = MinArray_List[i];

                for (UInt32 j = 0; j < intMinArray.Length; j++)
                {
                    NonceOut = intMinArray[j];

                    // Pass nonce value to byte array but convert to LE
                    NonceArray[3] = (Byte)NonceOut;
                    NonceArray[2] = (Byte)(NonceOut >> 8);
                    NonceArray[1] = (Byte)(NonceOut >> 16);
                    NonceArray[0] = (Byte)(NonceOut >> 24);

                    intMessageByteArray = Utils.HexString_To_u8Array(intMessage + Utils.u8Array_To_HexString(NonceArray));

                    // Perform SHA256 twice
                    hashResult = Hash256Engine.ComputeHash(intMessageByteArray, 0, intMessageByteArray.Length);
                    hashResult = Hash256Engine.ComputeHash(hashResult, 0, hashResult.Length);

                    // Find  min. hash from this batch
                    Array.Copy(hashResult, tmpFinalHashValue, 32);
                    tmpFinalHashValue[32] = 0;

                    FinalHashValue = new BigInteger(tmpFinalHashValue);

                    if (FinalHashValue <= HashValueMin)
                    {
                        HashValueMin = FinalHashValue;
                        NonceIndex = NonceOut;
                    }
                }
            }

            //Byte[] tmpByteArray = HashValueMin.ToByteArray();
            //Array.Clear(FinalHash, 0, FinalHash.Length);
            //Array.Copy(tmpByteArray, FinalHash, tmpByteArray.Length);

            return NonceIndex;
            //ValidNonce = NonceIndex;
        }

        public Byte[] GetHash(UInt32 tmpNonce)
        {
            SHA256Managed Hash256Engine = new SHA256Managed();
            String intMessage = BlockHeader.Substring(0, BlockHeader.Length - 8);
            Byte[] intMessageByteArray = new Byte[80];
            Byte[] hashResult = new Byte[32];
            Byte[] NonceArray = new Byte[4];

            // Pass nonce value to byte array but convert to LE
            NonceArray[3] = (Byte)tmpNonce;
            NonceArray[2] = (Byte)(tmpNonce >> 8);
            NonceArray[1] = (Byte)(tmpNonce >> 16);
            NonceArray[0] = (Byte)(tmpNonce >> 24);

            intMessageByteArray = Utils.HexString_To_u8Array(intMessage + Utils.u8Array_To_HexString(NonceArray));

            // Perform SHA256 twice
            hashResult = Hash256Engine.ComputeHash(intMessageByteArray, 0, intMessageByteArray.Length);
            hashResult = Hash256Engine.ComputeHash(hashResult, 0, hashResult.Length);

            return hashResult;
        }

        public void ResetClass()
        {
            cl_Source = "";
            //cl_Devices.Clear();
            ValidBlockHeader = "";

            workerOffset[0] =  OFFSET_DEFAULT;
            workerGlobalSize[0] = GLOBALSIZE_DEFAULT;
            workerLocalSize[0] = LOCALSIZE_DEFAULT;

            workerKernelSize[0] = KERNELSIZE_DEFAULT;

            ValidNonce = 0;
            NonceToCheck = 0;

            Array.Clear(hMidState, 0, hMidState.Length);
            Array.Clear(wIn, 0, wIn.Length);
            //Array.Clear(NonceOut, 0, NonceOut.Length);

            Array.Clear(FinalHash, 0, FinalHash.Length);

            KernelSelection = OPENCL_CUSTOM;

            MinArray_List.Clear();

            CompletedWorkFlag = NONCE_ERROR;
        }


        // Some utils used within class
        private Byte[] HexString_To_u8Array(String Hex)
        {
            int i, j = 0;
            Byte[] result = new Byte[Hex.Length / 2];

            for (i = 0, j = 0; i < Hex.Length; i += 2, j++)
            {
                result[j] = Convert.ToByte(Hex.Substring(i, 2), 16);
            }

            return result;
        }

        private UInt32[] HexString_To_u32Array(String Hex)
        {
            int i, j = 0;
            UInt32[] result = new UInt32[Hex.Length / 8];

            for (i = 0, j = 0; i < Hex.Length; i += 8, j++)
            {
                result[j] = Convert.ToUInt32(Hex.Substring(i, 8), 16);
            }

            return result;
        }

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

        private Byte[] Flipu8Array(Byte[] Input)
        {
            Int32 i = 0;
            Byte[] Result = new Byte[Input.Length];

            for (i = 0; i < Input.Length; i++)
                Result[i] = Input[(Input.Length-1) - i];

            return Result;
        }


        // -- Instance Constructors ----------------------------------------------
        public Worker()
        {
            cl_Source = "";
            cl_Devices = new List<ComputeDevice>();
            ValidBlockHeader = "";

            workerOffset = new Int64[1] { OFFSET_DEFAULT };
            workerGlobalSize = new Int64[1] { GLOBALSIZE_DEFAULT };
            workerLocalSize = new Int64[1] { LOCALSIZE_DEFAULT };

            workerKernelSize = new UInt32[1] { KERNELSIZE_DEFAULT };

            ValidNonce = 0;
            NonceToCheck = 0;

            hMidState = new UInt32[8];
            wIn = new UInt32[3];
            //NonceOut = new UInt32[2];

            FinalHash = new Byte[32];

            KernelSelection = OPENCL_CUSTOM;

            MinArray_List = new List<UInt32[]>();

            CompletedWorkFlag = NONCE_ERROR;
        }
    }
}
