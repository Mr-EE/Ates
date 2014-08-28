using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

namespace BitCoin_MinerV0_2
{
    class Stats
    {

// -- Properties ---------------------------------------------------------
        public Double SessionHashRate { get; set; }
        public Double SessionAvgHashRate { get; set; }
        public Double HashRateThread { get; set; }
        public Double Sessiontime { get; set; }
        public Double ThreadTime { get; set; }
        public Double BatchTime { get; set; }

        public Int32 CurrentBlock { get; set; }
        public Int64 CurrentDiff { get; set; }
        public Int64 NetworkDiff { get; set; }
        public Int32 TotalBlocks { get; set; }
        public Int32 AcceptedBlocks { get; set; }
        public Int32 RejectedBlocks { get; set; }
        public Int32 FailedHWBlocks { get; set; }
        public Int32 DiffOneBlocks { get; set; }

        public Int64 LastShareDiff { get; set; }
        public Int64 MaxDiff { get; set; }
        public Int64 AverageDiff { get; set; }
        public Int64 TotalDiff { get; set; }

        public Int64 NoncesProcessed { get; set; }

        public Double TimeSinceLastShare { get; set; }

        public Int32 BlockProgress { get; set; }


// -- Methods ------------------------------------------------------------
        public void ClearStats()
        {
            SessionHashRate = 0;
            SessionAvgHashRate = 0;
            HashRateThread = 0;
            Sessiontime = 0;
            ThreadTime = 0;
            BatchTime = 0;

            CurrentBlock = 0;
            CurrentDiff = 0;
            NetworkDiff = 0;
            TotalBlocks = 0;
            AcceptedBlocks = 0;
            RejectedBlocks = 0;
            FailedHWBlocks = 0;
            DiffOneBlocks = 0;

            LastShareDiff = 0;
            MaxDiff = 0;
            AverageDiff = 0;
            TotalDiff = 0;

            NoncesProcessed = 0;

            TimeSinceLastShare = 0;

            BlockProgress = 0;
        }

        public void UpdateMaxDiffculty()
        {
            if (LastShareDiff > MaxDiff)
                MaxDiff = LastShareDiff;
        }

        public void UpdateHashRateThread()
        {
            HashRateThread = ((Double)NoncesProcessed / ThreadTime) / 1000000D; // in MH/s
        }

        public void UpdateBatchTime(Int64 BatchSize)
        {
            Int32 numBatches = 0;

            if ((NoncesProcessed % BatchSize) == 0)
                numBatches = (Int32)(NoncesProcessed / BatchSize);
            else
                numBatches = (Int32)(NoncesProcessed / BatchSize) + 1;

            BatchTime = (ThreadTime / (Double)numBatches);
        }

        public void UpdateSessionHashRate()
        {
            SessionHashRate = (((Double)LastShareDiff * (Double)((Int64)(UInt32.MaxValue) + 1)) / (Double)TimeSinceLastShare) / 1000000D; //Mh/s
        }

        public void UpdateSessionAvgHashRate()
        {
            SessionAvgHashRate = (((Double)TotalDiff * (Double)((Int64)(UInt32.MaxValue) + 1)) / (Double)Sessiontime) / 1000000D; //Mh/s
        }

        // Some util functions used only by this class (private)
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


// -- Instance Constructors ----------------------------------------------
        public Stats()
        {
            SessionHashRate = 0;
            SessionAvgHashRate = 0;
            HashRateThread = 0;
            Sessiontime = 0;
            ThreadTime = 0;
            BatchTime = 0;

            CurrentBlock = 0;
            CurrentDiff = 0;
            NetworkDiff = 0;
            TotalBlocks = 0;
            AcceptedBlocks = 0;
            RejectedBlocks = 0;
            FailedHWBlocks = 0;
            DiffOneBlocks = 0;

            LastShareDiff = 0;
            MaxDiff = 0;
            AverageDiff = 0;
            TotalDiff = 0;

            NoncesProcessed = 0;

            TimeSinceLastShare = 0;

            BlockProgress = 0;
        }
    }
}
