using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Cache;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Text.RegularExpressions;

namespace BitCoin_MinerV0_2
{
    class Miner
    {
        // Constants
        public const Int32 PROTCOL_GETWORK = 0;
        public const Int32 PROTCOL_GBT = 1;
        public const Int32 PROTCOL_GETWORK_STRATUM = 2;
        public const Int32 PROTCOL_FROM_FILE = 3;
        public const Int32 PROTCOL_TEST = 999;

        // Block header offsets
        public const Int32 VER_OFFSET = 1;
        public const Int32 MERKLE_OFFSET = 8;
        public const Int32 PREVH_OFFSET = 72;
        public const Int32 TIME_OFFSET = 136;
        public const Int32 BITS_OFFSET = 144;
        public const Int32 NONCE_OFFSET = 152;


// -- Properties ---------------------------------------------------------
        public String UserName { get; set; }
        public String Password { get; set; }
        public Int32 URLPort { get; set; }
        public String URL { get; set; }

        public String BlockHeader { get; set; }
        public String MidState { get; set; }
        public String Target { get; set; }
        public Int32 CurrentBlock { get; set; }

        public Int32 MiningProtocolType { get; set; }

        public bool ContinueMining { get; set; }

        public List<String> BlockHeaderList { get; set; }
        public List<String> BlockHeightList { get; set; }

        public bool isPool { get; set; }

// -- Methods ------------------------------------------------------------
        public void GetWork()
        {
            if (MiningProtocolType == PROTCOL_TEST)
            {
                BlockHeader = "0100000000000000000000000000000000000000000000000000000000000000000000003BA3EDFD7A7B12B27AC72C3E67768F617FC81BC3888A51323A9FB8AA4B1E5E4A29AB5F49FFFF001D1DAC2B7C";
                Target = Utils.GetTargetFromBlockHeader(BlockHeader);
                MidState = "BC909A336358BFF090CCAC7D1E59CAA8C3C8D8E94F0103C896B187364719F91B";
            }
            else if (MiningProtocolType == PROTCOL_FROM_FILE)
            {
                // Keep getting data from list until empty
                if (BlockHeaderList.Count > 0)
                {
                    BlockHeader = BlockHeaderList[0];
                    Target = Utils.GetTargetFromBlockHeader(BlockHeader);
                    MidState = Utils.GetMidStateFromBlockHeader(BlockHeader);
                    CurrentBlock = Convert.ToInt32(BlockHeightList[0]);

                    // Since we just consumed data fromt he list remove that record.
                    BlockHeaderList.RemoveAt(0);
                    BlockHeightList.RemoveAt(0);
                }
                else
                    ContinueMining = false;
            }
            else if ( (MiningProtocolType == PROTCOL_GETWORK) || (MiningProtocolType == PROTCOL_GETWORK_STRATUM) )
            {
                String reply = "";
                String data = "";

                // Call Getwork and retrieve block header data information
                // Retrieve Block header
                reply = InvokeMethod("getwork");
                
                //If this was from a Stratum proxy then there's extra spaces within message. Remove them before parsing
                //if (MiningProtocolType == PROTCOL_GETWORK_STRATUM)
                    //reply = reply.Replace(" ", "");

                Match match = Regex.Match(reply, "\"data\":\"([A-Fa-f0-9]+)");
                if (match.Success)
                {
                    data = Utils.RemovePadding(match.Groups[1].Value);
                    data = Utils.EndianFlip32BitChunks(data);
                    BlockHeader = data;
                }
                else
                    throw new Exception("Didn't find valid 'data' in Server Response");

                // Retrieve Midstate
                match = Regex.Match(reply, "\"midstate\":\"([A-Fa-f0-9]+)");
                if (match.Success)
                {
                    data = match.Groups[1].Value;
                    data = Utils.EndianFlip32BitChunks(data); //<-- is this needed??
                    MidState = data;
                }
                else
                    throw new Exception("Didn't find valid 'midstate' in Server Response");

                // Retrieve target
                match = Regex.Match(reply, "\"target\":\"([A-Fa-f0-9]+)");
                if (match.Success)
                {
                    data = match.Groups[1].Value;
                    //data = EndianFlip32BitChunks(data); //<-- is this needed??
                    Target = data;
                }
                else
                    throw new Exception("Didn't find valid 'target' in Server Response");


                if (!isPool)
                {
                    // Add a small delay so we are not oversaturating server
                    Thread.Sleep(100);

                    // Find out what block number we are working on
                    reply = InvokeMethod("getblockcount");
                    match = Regex.Match(reply, "\"result\":([0-9]+)");
                    if (match.Success)
                    {
                        data = match.Groups[1].Value;
                        CurrentBlock = Convert.ToInt32(data) + 1; // Increment by 1 since we are working on future block
                    }
                    else
                        throw new Exception("Didn't find valid 'getblockcount' in Server Response");
                }
            }
        }

        public bool SendShare(byte[] share)
        {
            String data = Utils.EndianFlip32BitChunks(Utils.u8Array_To_HexString(share));
            String paddedData = Utils.AddPadding(data);
            String jsonReply = InvokeMethod("getwork", paddedData);
            Match match = Regex.Match(jsonReply, "\"result\":true");
            return match.Success;
        }

        public String InvokeMethod(String method, String paramString = null)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(URL + ":" + URLPort));
            //webRequest.Credentials = new NetworkCredential(UserName, Password);
            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            string jsonParam = (paramString != null) ? "\"" + paramString + "\"" : "";
            string request = "{\"id\":0,\"method\":\"" + method + "\",\"params\":[" + jsonParam + "]}";
            byte[] byteArray = Encoding.UTF8.GetBytes(request);

            HttpRequestCachePolicy noCachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            webRequest.CachePolicy = noCachePolicy;
            //webRequest.Headers.Add("X-Mining-Extensions", "midstate");
            webRequest.UserAgent = "BitCoin_Miner_V0.2";
            webRequest.PreAuthenticate = true;
            var creds = Encoding.UTF8.GetBytes(UserName + ":" + Password);
            webRequest.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(creds));

            webRequest.ContentLength = byteArray.Length;
            using (Stream dataStream = webRequest.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }


            string reply = "";
            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream str = webResponse.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(str))
                    {
                        reply = reader.ReadToEnd();
                    }
                }
            }

            // Remove white spaces if there's any
            reply = reply.Replace(" ", "");

            return reply;
        }

// -- Instance Constructors ----------------------------------------------
        public Miner()
        {
            UserName = "";
            Password = "";
            URLPort = 0;
            URL = "";

            CurrentBlock = 0;

            isPool = false;

            ContinueMining = false;

            MiningProtocolType = PROTCOL_TEST;

            BlockHeaderList = new List<String>();
            BlockHeightList = new List<String>();
        }
    }
}
