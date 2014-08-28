using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BitCoin_Miner
{
    class Constants
    {        
        // Block header offsets
        public const Int32 VER_OFFSET = 1;
        public const Int32 MERKLE_OFFSET = 8;
        public const Int32 PREVH_OFFSET = 72;
        public const Int32 TIME_OFFSET = 136;
        public const Int32 BITS_OFFSET = 144;
        public const Int32 NONCE_OFFSET = 152;

        // Mining method types
        public const Int32 MINER_OPENCL_CUSTOM = 0;
        public const Int32 MINER_NET_STOCK = 1;
        public const Int32 MINER_NET_CUSTOM = 2;
        public const Int32 MINER_OPENCL_PHAT2K = 3;

        // Mining protocol types
        public const Int32 PROTCOL_GETWORK = 0;
        public const Int32 PROTCOL_GBT = 2;
        public const Int32 PROTCOL_STRATUM = 3;
        public const Int32 PROTCOL_INTTEST = 4;

        public const Int32 ERROR = 99;
        public const Int32 NO_ERROR = 0;

        public const Int32 NUMBER_PROCESSES = 8;

    }
}
