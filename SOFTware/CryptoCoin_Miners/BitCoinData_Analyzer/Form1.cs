using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Numerics;


namespace BitCoinData_Analyzer
{
    public partial class Form1 : Form
    {
        // Global variables/constants

        public const String DIFF_ONE = "0000000000000000000000000000000000000000000000000000FFFF00000000" + "00";

        // w0-w2 offset points
        public const Int32 W0_OFFSET = 128;
        public const Int32 W1_OFFSET = 136;
        public const Int32 W2_OFFSET = 144;
        public const Int32 NONCE_OFFSET = 152;

        Stopwatch stopWatch = new Stopwatch();
        Int64 nS_PerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;

        // BlockHeader as 80B LE Hex String
        List<String[]> BlockHeaderList = new List<String[]>();
        List<String[]> MidstateList = new List<String[]>();
        List<String[]> MinHashList = new List<String[]>();
        List<bool[]> DiffShareState = new List<bool[]>();

        // Attributes Lists
        List<UInt32[]> X0_List = new List<UInt32[]>(); //MidState[0:31]
        List<UInt32[]> X1_List = new List<UInt32[]>(); //MidState[32:63]
        List<UInt32[]> X2_List = new List<UInt32[]>(); //MidState[64:95]
        List<UInt32[]> X3_List = new List<UInt32[]>(); //MidState[96:127]
        List<UInt32[]> X4_List = new List<UInt32[]>(); //MidState[128:159]
        List<UInt32[]> X5_List = new List<UInt32[]>(); //MidState[160:191]
        List<UInt32[]> X6_List = new List<UInt32[]>(); //MidState[192:223]
        List<UInt32[]> X7_List = new List<UInt32[]>(); //MidState[224:255]
        List<UInt32[]> X8_List = new List<UInt32[]>(); //W0 (Last 32b of Merklet Root)
        List<UInt32[]> X9_List = new List<UInt32[]>(); //W1 (Time)
        List<UInt32[]> X10_List = new List<UInt32[]>(); //W2 (Bits)
        List<UInt32[]> X11_List = new List<UInt32[]>(); //W16 (f{w0,w1})
        List<UInt32[]> X12_List = new List<UInt32[]>(); //W17 (f{w1,w2})

        // Class Lists
        List<UInt32[]> Y0_List = new List<UInt32[]>(); // FinalHash[224:255]
        List<UInt32[]> Y1_List = new List<UInt32[]>(); // FinalHash[192:223]

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            panel1.BackColor = Color.Red;

            // Clear global variables
            BlockHeaderList.Clear();
            X0_List.Clear();
            X1_List.Clear();
            X2_List.Clear();
            X3_List.Clear();
            X4_List.Clear();
            X5_List.Clear();
            X6_List.Clear();
            X7_List.Clear();
            X8_List.Clear();
            X9_List.Clear();
            X10_List.Clear();
            X11_List.Clear();
            X12_List.Clear();
            Y0_List.Clear();
            Y1_List.Clear();

            GC.Collect();


            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Int32 FileCnt = openFileDialog1.FileNames.Length;
                    String str, str_tmp = "";
                    Int32 i, j = 0;
                    Int32 start, end = 0;
                    String MidState = "";

                    // Temp list used to parse data before passing to global lists.
                    List<String> intBlockHeaderList = new List<String>();
                    List<UInt32> intX0_List = new List<UInt32>();
                    List<UInt32> intX1_List = new List<UInt32>();
                    List<UInt32> intX2_List = new List<UInt32>();
                    List<UInt32> intX3_List = new List<UInt32>();
                    List<UInt32> intX4_List = new List<UInt32>();
                    List<UInt32> intX5_List = new List<UInt32>();
                    List<UInt32> intX6_List = new List<UInt32>();
                    List<UInt32> intX7_List = new List<UInt32>();
                    List<UInt32> intX8_List = new List<UInt32>();
                    List<UInt32> intX9_List = new List<UInt32>();
                    List<UInt32> intX10_List = new List<UInt32>();
                    List<UInt32> intX11_List = new List<UInt32>();
                    List<UInt32> intX12_List = new List<UInt32>();
                    List<UInt32> intY0_List = new List<UInt32>();
                    List<UInt32> intY1_List = new List<UInt32>();

                    for (i = 0; i < FileCnt; i++)
                    {
                        StreamReader sr_data = new StreamReader(openFileDialog1.FileNames[i]);

                        str = "";
                        str_tmp = "";

                        // Reset index pointers. Read in data until end of file or max array size
                        j = 0;
                        while (((str = sr_data.ReadLine()) != null) & (j <= Int32.MaxValue))
                        {
                            // There might be some extra line feeds at the end of the dataset. If so then the end of file has been reached for usefull data
                            if (str == "")
                                continue;

                            // Grab Blockheader and add to list
                            start = str.IndexOf(",", 0); // First "," = BlockHeader
                            intBlockHeaderList.Add(str.Substring(0, start));

                            // Grab Midstate (32Bytes LE) and parse into X0-X7 as 32b LE UInt32
                            start = start + 1;
                            end = str.IndexOf(",", start); // Second "," = MidState
                            MidState = str.Substring(start, (end - start));
                            intX0_List.Add(Convert.ToUInt32(MidState.Substring(0, 8), 16));
                            intX1_List.Add(Convert.ToUInt32(MidState.Substring(8, 8), 16));
                            intX2_List.Add(Convert.ToUInt32(MidState.Substring(16, 8), 16));
                            intX3_List.Add(Convert.ToUInt32(MidState.Substring(24, 8), 16));
                            intX4_List.Add(Convert.ToUInt32(MidState.Substring(32, 8), 16));
                            intX5_List.Add(Convert.ToUInt32(MidState.Substring(40, 8), 16));
                            intX6_List.Add(Convert.ToUInt32(MidState.Substring(48, 8), 16));
                            intX7_List.Add(Convert.ToUInt32(MidState.Substring(56, 8), 16));

                            // Grab W0 and add to list as 32b LE
                            start = end + 1;
                            end = str.IndexOf(",", start); // Third "," = W0
                            str_tmp = str.Substring(start, (end - start));
                            intX8_List.Add(Convert.ToUInt32(str_tmp));

                            // Grab W1 and add to list as 32b LE
                            start = end + 1;
                            end = str.IndexOf(",", start); // Forth "," = W1
                            str_tmp = str.Substring(start, (end - start));
                            intX9_List.Add(Convert.ToUInt32(str_tmp));

                            // Grab W2 and add to list as 32b LE
                            start = end + 1;
                            end = str.IndexOf(",", start); // Fifth "," = W2
                            str_tmp = str.Substring(start, (end - start));
                            intX10_List.Add(Convert.ToUInt32(str_tmp));

                            // Grab W16 and add to list as 32b LE
                            start = end + 1;
                            end = str.IndexOf(",", start); // Sixth "," = W16
                            str_tmp = str.Substring(start, (end - start));
                            intX11_List.Add(Convert.ToUInt32(str_tmp));

                            // Grab W17 and add to list as 32b LE
                            start = end + 1;
                            end = str.IndexOf(",", start); // Seventh "," = W17
                            str_tmp = str.Substring(start, (end - start));
                            intX12_List.Add(Convert.ToUInt32(str_tmp));

                            // Grab boolen indicating if blockhead has a >= diff share
                            start = end + 1;
                            end = str.Length - start;
                            str_tmp = str.Substring(start, end);

                            if (str_tmp == "TRUE")
                            {
                                SHA256Managed Hash256Engine = new SHA256Managed();
                                Byte[] tmp = new Byte[32];
                                UInt32[] hashResult = new UInt32[8];

                                tmp = Hash256Engine.ComputeHash(Utils.HexString_To_u8Array(intBlockHeaderList[j]));
                                tmp = Hash256Engine.ComputeHash(tmp);

                                hashResult = Utils.HexString_To_u32Array(Utils.u8Array_To_HexString(tmp));

                                intY0_List.Add(Convert.ToUInt32(hashResult[7]));
                                intY1_List.Add(Convert.ToUInt32(hashResult[6]));
                            }
                            else
                            {
                                intY0_List.Add(Convert.ToUInt32(UInt32.MaxValue));
                                intY1_List.Add(Convert.ToUInt32(UInt32.MaxValue));
                            }

                            // Increment line counter
                            j++;
                        }

                        sr_data.Close();

                        // Transfer data from this file to global variables for later processing
                        BlockHeaderList.Add(intBlockHeaderList.ToArray());

                        X0_List.Add(intX0_List.ToArray());
                        X1_List.Add(intX1_List.ToArray());
                        X2_List.Add(intX2_List.ToArray());
                        X3_List.Add(intX3_List.ToArray());
                        X4_List.Add(intX4_List.ToArray());
                        X5_List.Add(intX5_List.ToArray());
                        X6_List.Add(intX6_List.ToArray());
                        X7_List.Add(intX7_List.ToArray());
                        X8_List.Add(intX8_List.ToArray());
                        X9_List.Add(intX9_List.ToArray());
                        X10_List.Add(intX10_List.ToArray());
                        X11_List.Add(intX11_List.ToArray());
                        X12_List.Add(intX12_List.ToArray());

                        Y0_List.Add(intY0_List.ToArray());
                        Y1_List.Add(intY1_List.ToArray());
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                    button1.Enabled = true;
                    return;
                }
            }
            else
            {
                button1.Enabled = true;
                return;
            }

            panel1.BackColor = Color.Green;
            button1.Enabled = true;
            button2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Data has been parsed in at this point so now we do the analysis
            // Open up prompt for user to select directory and store the path
            String StoreDataDirectory = "";
            folderBrowserDialog1.Description = "Select The Directory To Store Data File(s)";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StoreDataDirectory = folderBrowserDialog1.SelectedPath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Prompting User To Select Folder Director. Error Code:: " + ex.Message);
                    button1.Enabled = true;
                    return;
                }
            }

            // Processing Data Here
            Int32 i, j, k, l = 0;
            String[] strArrayBlockHeader;
            String[] strArrayMidState;
            String[] strArrayMinHash;
            bool[] strArrayDiffShareState;

            // Write results to output
            StreamWriter FileOutputStreamWriter2;
            String  FileNamePath2 = "";

            try
            {

                FileNamePath2 = StoreDataDirectory + "\\" + "ProcessedData_SumDiffWNonce" + ".csv";
                FileOutputStreamWriter2 = new StreamWriter(FileNamePath2);

                // Write header data
                //FileOutputStreamWriter2.WriteLine("M0,M1,M2,M3,M4,M5,M6,M7,W0,W1,W2,NONCE,CombinedX,CombinedY");
                FileOutputStreamWriter2.WriteLine("CombinedX,CombinedY");

                for (i = 0; i < BlockHeaderList.Count; i++) // Process each file
                {
                    strArrayBlockHeader = BlockHeaderList[i];
                    strArrayMidState = MidstateList[i];
                    strArrayMinHash = MinHashList[i];
                    strArrayDiffShareState = DiffShareState[i];

                    for (j = 0; j < strArrayBlockHeader.Length; j++) // process each unit per file
                    {
                        String strCombinedX = "";
                        UInt32[] CombinedX;
                        String strCombinedY = "";
                        //UInt32[] CombinedY;


                        // Process X array
                        strCombinedX = strArrayMidState[j] +
                                    strArrayBlockHeader[j].Substring(W0_OFFSET, 24) +
                                    strArrayBlockHeader[j].Substring(NONCE_OFFSET, 8);
                        CombinedX = Utils.HexString_To_u32Array(strCombinedX);

                        for (k = (CombinedX.Length-1); k > 0 ; k--)
                        {
                            for (l = 0; l < k; l++)
                                CombinedX[l] = CombinedX[l + 1] - CombinedX[l];
                        }

                        // Process Y array
                        strCombinedY = strArrayMinHash[j];
                        BigInteger DiffOneValue = new BigInteger(Utils.HexString_To_u8Array(DIFF_ONE));
                        BigInteger MinShare = new BigInteger(Utils.HexString_To_u8Array(strCombinedY + "00"));// Add extra leading "Zero" for proper BigInteger casting

                        Double tmpDiffOneValue = (Double)DiffOneValue;
                        Double tmpMinShare = (Double)MinShare;
                        Double tmpY_Out =(tmpDiffOneValue / tmpMinShare);
                        Single Y_Out = (Single)tmpY_Out;

                        /*
                        strCombinedY = strArrayMinHash[j];
                        CombinedY = Utils.HexString_To_u32Array(strCombinedY);
                        for (k = (CombinedY.Length - 1); k > 0; k--)
                        {
                            for (l = 0; l < k; l++)
                                CombinedY[l] = CombinedY[l + 1] - CombinedY[l];
                        }
                        */

                        /*
                        FileOutputStreamWriter2.WriteLine(  tmpMidState[0].ToString() + "," +
                                                            tmpMidState[1].ToString() + "," +
                                                            tmpMidState[2].ToString() + "," +
                                                            tmpMidState[3].ToString() + "," +
                                                            tmpMidState[4].ToString() + "," +
                                                            tmpMidState[5].ToString() + "," +
                                                            tmpMidState[6].ToString() + "," +
                                                            tmpMidState[7].ToString() + "," +

                                                            tmpWArray[0].ToString() + "," +
                                                            tmpWArray[1].ToString() + "," +
                                                            tmpWArray[2].ToString() + "," +

                                                            tmpNonce.ToString() + "," +

                                                            CombinedX[0].ToString() + "," +

                                                            Y_Out.ToString());
                        */

                        FileOutputStreamWriter2.WriteLine(CombinedX[0].ToString() + "," + Y_Out.ToString());
                    }
                }
                FileOutputStreamWriter2.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                button1.Enabled = true;
                button3.Enabled = true;
                return;
            }

            button1.Enabled = true;
            button3.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button3.Enabled = false;
            button2.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            button7.Enabled = false;
            button8.Enabled = false;
            button9.Enabled = false;
            panel1.BackColor = Color.Red;

            // Clear global variables
            BlockHeaderList.Clear();
            MidstateList.Clear();
            MinHashList.Clear();
            DiffShareState.Clear();

            GC.Collect();

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Int32 FileCnt = openFileDialog1.FileNames.Length;
                    String str = "";
                    Int32 i, j = 0;
                    Int32 start, end = 0;

                    // Temp list used to parse data before passing to global lists.
                    List<String> intBlockHeaderList = new List<String>();
                    List<String> intMidstateList = new List<String>();
                    List<String> intMinHashList = new List<String>();
                    List<bool> intDiffShareState = new List<bool>();

                    for (i = 0; i < FileCnt; i++)
                    {
                        StreamReader sr_data = new StreamReader(openFileDialog1.FileNames[i]);

                        str = "";

                        intBlockHeaderList.Clear();
                        intMidstateList.Clear();
                        intMinHashList.Clear();
                        intDiffShareState.Clear();

                        // Reset index pointers. Read in data until end of file or max array size
                        j = 0;
                        while (((str = sr_data.ReadLine()) != null) & (j <= Int32.MaxValue))
                        {
                            // There might be some extra line feeds at the end of the dataset. If so then the end of file has been reached for usefull data
                            if (str == "")
                                continue;

                            // Grab Blockheader and add to list
                            start = 0;
                            end = str.IndexOf(",", 0); // First "," = BlockHeader
                            intBlockHeaderList.Add(str.Substring(start, (end - start)));

                            // Grab Midstate (32Bytes LE) and parse into X0-X7 as 32b LE UInt32
                            start = end + 1;
                            end = str.IndexOf(",", start); // Second "," = MidState
                            intMidstateList.Add(str.Substring(start, (end - start)));

                            // Grab Min Hash (32Bytes LE)
                            start = end + 1;
                            end = str.IndexOf(",", start); // Third "," = Min Hash
                            intMinHashList.Add(str.Substring(start, (end - start)));

                            // Grab boolen indicating if blockhead has a >= diff share
                            start = end + 1;
                            end = str.Length - start;
                            if (str.Substring(start, end) == "TRUE")
                                intDiffShareState.Add(true);
                            else
                                intDiffShareState.Add(false);

                            // Increment line counter
                            j++;
                        }

                        sr_data.Close();

                        // Transfer data from this file to global variables for later processing
                        BlockHeaderList.Add(intBlockHeaderList.ToArray());
                        MidstateList.Add(intMidstateList.ToArray());
                        MinHashList.Add(intMinHashList.ToArray());
                        DiffShareState.Add(intDiffShareState.ToArray());
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                    button3.Enabled = true;
                    return;
                }
            }
            else
            {
                button3.Enabled = true;
                return;
            }

            panel1.BackColor = Color.Green;
            button3.Enabled = true;
            button2.Enabled = true;
            button5.Enabled = true;
            button6.Enabled = true;
            button7.Enabled = true;
            button8.Enabled = true;
            button9.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Int32 i, j = 0;
            List<String[]> rawBlockHeaderList = new List<String[]>();
            List<String[]> rawBlockHeaderHeight = new List<String[]>();
            List<String> intBlockHeaderList = new List<String>();
            List<String> intBlockHeaderHeight = new List<String>();

            button4.Enabled = false;
            panel1.BackColor = Color.Red;

            openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Int32 FileCnt = openFileDialog1.FileNames.Length;
                    String str = "";
                    String tmpStr1 = "";
                    String tmpStr2 = "";
                    Int32 start, end = 0;

                    for (i = 0; i < FileCnt; i++)
                    {
                        StreamReader sr_data = new StreamReader(openFileDialog1.FileNames[i]);

                        intBlockHeaderList.Clear();
                        intBlockHeaderHeight.Clear();

                        str = "";
                        tmpStr1 = "";
                        tmpStr2 = "";

                        // read first line and discard since it's the headers
                        str = sr_data.ReadLine();
                        str = "";

                        // Reset index pointers. Read in data until end of file or max array size
                        j = 0;
                        while (((str = sr_data.ReadLine()) != null) & (j <= Int32.MaxValue))
                        {
                            // There might be some extra line feeds at the end of the dataset. If so then the end of file has been reached for usefull data
                            if (str == "")
                                continue;

                            tmpStr1 = "";
                            tmpStr2 = "";

                            start = 0;
                            end = str.IndexOf(",", start); // second "," = Version
                            tmpStr1 = str.Substring(start, (end - start));

                            start = str.IndexOf(",", 0) + 1; // First "," = Block height
                            end = str.IndexOf(",", start); // second "," = Version
                            tmpStr2 = str.Substring(start, (end - start));

                            start = end + 1;
                            end = str.IndexOf(",", start); // Third "," = Prev Hash
                            tmpStr2 += str.Substring(start, (end - start));

                            start = end + 1;
                            end = str.IndexOf(",", start); // Forth "," = Merk rooth
                            tmpStr2 += str.Substring(start, (end - start));

                            start = end + 1;
                            end = str.IndexOf(",", start); // Fifth "," = Time
                            tmpStr2 += str.Substring(start, (end - start));

                            start = end + 1;
                            end = str.IndexOf(",", start); // Sixth "," = Bits
                            tmpStr2 += str.Substring(start, (end - start));

                            // add default nonce value
                            tmpStr2+= "00000000";

                            intBlockHeaderHeight.Add(tmpStr1);
                            intBlockHeaderList.Add(tmpStr2);

                            // Increment line counter
                            j++;
                        }

                        sr_data.Close();

                        // Transfer data from this file to global variables for later processing
                        rawBlockHeaderList.Add(intBlockHeaderList.ToArray());
                        rawBlockHeaderHeight.Add(intBlockHeaderHeight.ToArray());
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                    button4.Enabled = true;
                    return;
                }
            }
            else
            {
                button4.Enabled = true;
                return;
            }


            // Now save results to a file
            // Open up prompt for user to select directory and store the path
            String StoreDataDirectory = "";
            folderBrowserDialog1.Description = "Select The Directory To Store Data File(s)";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StoreDataDirectory = folderBrowserDialog1.SelectedPath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Prompting User To Select Folder Director. Error Code:: " + ex.Message);
                    button4.Enabled = true;
                    return;
                }
            }

            // Processing Data Here
            String[] strArrayBlockHeader;
            String[] strArrayBlockHeight;

            // Write results to output
            StreamWriter FileOutputStreamWriter;
            String FileNamePath = "";

            try
            {
                for (i = 0; i < rawBlockHeaderList.Count; i++) // Process each file
                {
                    FileNamePath = StoreDataDirectory + "\\" + "BlockChainBlockHeaders" + i.ToString() + ".csv";
                    FileOutputStreamWriter = new StreamWriter(FileNamePath);

                    strArrayBlockHeader = rawBlockHeaderList[i];
                    strArrayBlockHeight = rawBlockHeaderHeight[i];

                    for (j = 0; j < strArrayBlockHeader.Length; j++) // process each unit per file
                    {
                        FileOutputStreamWriter.WriteLine(strArrayBlockHeight[j].ToString() + "," + strArrayBlockHeader[j].ToString());
                    }

                    FileOutputStreamWriter.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                button4.Enabled = true;
                button3.Enabled = true;
                return;
            }

            panel1.BackColor = Color.Green;
            button4.Enabled = true;
            button3.Enabled = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Data has been parsed in at this point so now we do the analysis
            // Open up prompt for user to select directory and store the path
            String StoreDataDirectory = "";
            folderBrowserDialog1.Description = "Select The Directory To Store Data File(s)";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StoreDataDirectory = folderBrowserDialog1.SelectedPath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Prompting User To Select Folder Director. Error Code:: " + ex.Message);
                    button1.Enabled = true;
                    return;
                }
            }

            // Processing Data Here
            Int32 i, j, k, l = 0;
            String[] strArrayBlockHeader;
            String[] strArrayMidState;
            String[] strArrayMinHash;
            bool[] strArrayDiffShareState;

            // Write results to output
            StreamWriter FileOutputStreamWriter2;
            String FileNamePath2 = "";

            try
            {
                FileNamePath2 = StoreDataDirectory + "\\" + "ProcessedData_SumDiffWONonce" + ".csv";
                FileOutputStreamWriter2 = new StreamWriter(FileNamePath2);

                // Write header data
                FileOutputStreamWriter2.WriteLine("CombinedX,CombinedY");

                for (i = 0; i < BlockHeaderList.Count; i++) // Process each file
                {
                    strArrayBlockHeader = BlockHeaderList[i];
                    strArrayMidState = MidstateList[i];
                    strArrayMinHash = MinHashList[i];
                    strArrayDiffShareState = DiffShareState[i];

                    for (j = 0; j < strArrayBlockHeader.Length; j++) // process each unit per file
                    {
                        String strCombinedX = "";
                        UInt32[] CombinedX;
                        String strCombinedY = "";

                        // Process X array
                        strCombinedX = strArrayMidState[j] +
                                    strArrayBlockHeader[j].Substring(W0_OFFSET, 24);
                        CombinedX = Utils.HexString_To_u32Array(strCombinedX);

                        for (k = (CombinedX.Length-1); k > 0 ; k--)
                        {
                            for (l = 0; l < k; l++)
                                CombinedX[l] = CombinedX[l + 1] - CombinedX[l];
                        }


                        // Process Y array
                        strCombinedY = strArrayMinHash[j];
                        BigInteger DiffOneValue = new BigInteger(Utils.HexString_To_u8Array(DIFF_ONE));
                        BigInteger MinShare = new BigInteger(Utils.HexString_To_u8Array(strCombinedY + "00"));// Add extra leading "Zero" for proper BigInteger casting

                        Double tmpDiffOneValue = (Double)DiffOneValue;
                        Double tmpMinShare = (Double)MinShare;
                        Double tmpY_Out =(tmpDiffOneValue / tmpMinShare);
                        Single Y_Out = (Single)tmpY_Out;

                        // Output to file
                        FileOutputStreamWriter2.WriteLine(CombinedX[0].ToString() + "," + Y_Out.ToString());
                    }
                }
                FileOutputStreamWriter2.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                button1.Enabled = true;
                button3.Enabled = true;
                return;
            }

            button1.Enabled = true;
            button3.Enabled = true;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // Data has been parsed in at this point so now we do the analysis
            // Open up prompt for user to select directory and store the path
            String StoreDataDirectory = "";
            folderBrowserDialog1.Description = "Select The Directory To Store Data File(s)";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StoreDataDirectory = folderBrowserDialog1.SelectedPath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Prompting User To Select Folder Director. Error Code:: " + ex.Message);
                    button1.Enabled = true;
                    return;
                }
            }

            // Processing Data Here
            Int32 i, j, k, l = 0;
            String[] strArrayBlockHeader;
            String[] strArrayMidState;
            String[] strArrayMinHash;
            bool[] strArrayDiffShareState;

            // Write results to output
            StreamWriter FileOutputStreamWriter2;
            String FileNamePath2 = "";

            try
            {
                FileNamePath2 = StoreDataDirectory + "\\" + "ProcessedData_XORWONonce" + ".csv";
                FileOutputStreamWriter2 = new StreamWriter(FileNamePath2);

                // Write header data
                FileOutputStreamWriter2.WriteLine("CombinedX,CombinedY");

                for (i = 0; i < BlockHeaderList.Count; i++) // Process each file
                {
                    strArrayBlockHeader = BlockHeaderList[i];
                    strArrayMidState = MidstateList[i];
                    strArrayMinHash = MinHashList[i];
                    strArrayDiffShareState = DiffShareState[i];

                    for (j = 0; j < strArrayBlockHeader.Length; j++) // process each unit per file
                    {
                        String strCombinedX = "";
                        UInt32[] CombinedX;
                        String strCombinedY = "";

                        // Process X array
                        strCombinedX = strArrayMidState[j] +
                                    strArrayBlockHeader[j].Substring(W0_OFFSET, 24);
                        CombinedX = Utils.HexString_To_u32Array(strCombinedX);

                        for (k = (CombinedX.Length-1); k > 0 ; k--)
                        {
                            for (l = 0; l < k; l++)
                                CombinedX[l] = CombinedX[l + 1] ^ CombinedX[l];
                        }

                        // Process Y array
                        strCombinedY = strArrayMinHash[j];
                        BigInteger DiffOneValue = new BigInteger(Utils.HexString_To_u8Array(DIFF_ONE));
                        BigInteger MinShare = new BigInteger(Utils.HexString_To_u8Array(strCombinedY + "00"));// Add extra leading "Zero" for proper BigInteger casting

                        Double tmpDiffOneValue = (Double)DiffOneValue;
                        Double tmpMinShare = (Double)MinShare;
                        Double tmpY_Out =(tmpDiffOneValue / tmpMinShare);
                        Single Y_Out = (Single)tmpY_Out;

                        //Output to file
                        FileOutputStreamWriter2.WriteLine(CombinedX[0].ToString() + "," + Y_Out.ToString());
                    }
                }
                FileOutputStreamWriter2.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                button1.Enabled = true;
                button3.Enabled = true;
                return;
            }

            button1.Enabled = true;
            button3.Enabled = true;
        }

        private void button7_Click(object sender, EventArgs e)
        {
                        // Data has been parsed in at this point so now we do the analysis
            // Open up prompt for user to select directory and store the path
            String StoreDataDirectory = "";
            folderBrowserDialog1.Description = "Select The Directory To Store Data File(s)";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StoreDataDirectory = folderBrowserDialog1.SelectedPath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Prompting User To Select Folder Director. Error Code:: " + ex.Message);
                    button1.Enabled = true;
                    return;
                }
            }

            // Processing Data Here
            Int32 i, j, k, l = 0;
            String[] strArrayBlockHeader;
            String[] strArrayMidState;
            String[] strArrayMinHash;
            bool[] strArrayDiffShareState;

            // Write results to output
            StreamWriter FileOutputStreamWriter1;
            String FileNamePath1 = "";

            try
            {
                FileNamePath1 = StoreDataDirectory + "\\" + "RawData" + ".csv";
                FileOutputStreamWriter1 = new StreamWriter(FileNamePath1);

                // Write header data
                FileOutputStreamWriter1.WriteLine("M0,M1,M2,M3,M4,M5,M6,M7,W0,W1,W2,NONCE,H0,H1,H2,H3,H4,H5,H6,H7,gDiff1");

                for (i = 0; i < BlockHeaderList.Count; i++) // Process each file
                {
                    strArrayBlockHeader = BlockHeaderList[i];
                    strArrayMidState = MidstateList[i];
                    strArrayMinHash = MinHashList[i];
                    strArrayDiffShareState = DiffShareState[i];

                    for (j = 0; j < strArrayBlockHeader.Length; j++) // process each unit per file
                    {
                        UInt32[] tmpMidState;
                        UInt32[] tmpWArray;
                        UInt32 tmpNonce;
                        UInt32[] tmpMinHash;
                        UInt32 tmpDiffShareState;

                        // Grab MidState and convert to u32 array
                        tmpMidState = Utils.HexString_To_u32Array(strArrayMidState[j]);

                        // Grab last three 32bit values from blockheader (w0, w1, w2)
                        tmpWArray = Utils.HexString_To_u32Array(strArrayBlockHeader[j].Substring(W0_OFFSET, 24));

                        // Grab last 32 bits which is the nonce
                        tmpNonce = Convert.ToUInt32(strArrayBlockHeader[j].Substring(NONCE_OFFSET, 8),16);

                        // Grab min hash and convert to u32 array
                        tmpMinHash = Utils.HexString_To_u32Array(strArrayMinHash[j]);


                        // Grab Diff>=1 state
                        if (strArrayDiffShareState[j] == true)
                            tmpDiffShareState = 1;
                        else
                            tmpDiffShareState = 0;

                        FileOutputStreamWriter1.WriteLine(  tmpMidState[0].ToString() + "," +
                                                            tmpMidState[1].ToString() + "," +
                                                            tmpMidState[2].ToString() + "," +
                                                            tmpMidState[3].ToString() + "," +
                                                            tmpMidState[4].ToString() + "," +
                                                            tmpMidState[5].ToString() + "," +
                                                            tmpMidState[6].ToString() + "," +
                                                            tmpMidState[7].ToString() + "," +

                                                            tmpWArray[0].ToString() + "," +
                                                            tmpWArray[1].ToString() + "," +
                                                            tmpWArray[2].ToString() + "," +

                                                            tmpNonce.ToString() + "," +

                                                            tmpMinHash[0].ToString() + "," +
                                                            tmpMinHash[1].ToString() + "," +
                                                            tmpMinHash[2].ToString() + "," +
                                                            tmpMinHash[3].ToString() + "," +
                                                            tmpMinHash[4].ToString() + "," +
                                                            tmpMinHash[5].ToString() + "," +
                                                            tmpMinHash[6].ToString() + "," +
                                                            tmpMinHash[7].ToString() + "," +

                                                            tmpDiffShareState.ToString());
                    }
                }
                FileOutputStreamWriter1.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                button1.Enabled = true;
                button3.Enabled = true;
                return;
            }

            button1.Enabled = true;
            button3.Enabled = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            // Data has been parsed in at this point so now we do the analysis
            // Open up prompt for user to select directory and store the path
            String StoreDataDirectory = "";
            folderBrowserDialog1.Description = "Select The Directory To Store Data File(s)";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StoreDataDirectory = folderBrowserDialog1.SelectedPath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Prompting User To Select Folder Director. Error Code:: " + ex.Message);
                    button1.Enabled = true;
                    return;
                }
            }

            // Processing Data Here
            Int32 i, j, k, l = 0;
            String[] strArrayBlockHeader;
            String[] strArrayMidState;
            String[] strArrayMinHash;
            bool[] strArrayDiffShareState;

            // Write results to output
            StreamWriter FileOutputStreamWriter2;
            String FileNamePath2 = "";

            try
            {
                FileNamePath2 = StoreDataDirectory + "\\" + "ProcessedData_XORWNonce" + ".csv";
                FileOutputStreamWriter2 = new StreamWriter(FileNamePath2);

                // Write header data
                FileOutputStreamWriter2.WriteLine("CombinedX,CombinedY");

                for (i = 0; i < BlockHeaderList.Count; i++) // Process each file
                {
                    strArrayBlockHeader = BlockHeaderList[i];
                    strArrayMidState = MidstateList[i];
                    strArrayMinHash = MinHashList[i];
                    strArrayDiffShareState = DiffShareState[i];

                    for (j = 0; j < strArrayBlockHeader.Length; j++) // process each unit per file
                    {
                        String strCombinedX = "";
                        UInt32[] CombinedX;
                        String strCombinedY = "";

                        // Process X array
                        strCombinedX = strArrayMidState[j] +
                                    strArrayBlockHeader[j].Substring(W0_OFFSET, 24) +
                                    strArrayBlockHeader[j].Substring(NONCE_OFFSET, 8);
                        CombinedX = Utils.HexString_To_u32Array(strCombinedX);

                        for (k = (CombinedX.Length - 1); k > 0; k--)
                        {
                            for (l = 0; l < k; l++)
                                CombinedX[l] = CombinedX[l + 1] ^ CombinedX[l];
                        }

                        // Process Y array
                        strCombinedY = strArrayMinHash[j];
                        BigInteger DiffOneValue = new BigInteger(Utils.HexString_To_u8Array(DIFF_ONE));
                        BigInteger MinShare = new BigInteger(Utils.HexString_To_u8Array(strCombinedY + "00"));// Add extra leading "Zero" for proper BigInteger casting

                        Double tmpDiffOneValue = (Double)DiffOneValue;
                        Double tmpMinShare = (Double)MinShare;
                        Double tmpY_Out = (tmpDiffOneValue / tmpMinShare);
                        Single Y_Out = (Single)tmpY_Out;

                        //Output to file
                        FileOutputStreamWriter2.WriteLine(CombinedX[0].ToString() + "," + Y_Out.ToString());
                    }
                }
                FileOutputStreamWriter2.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                button1.Enabled = true;
                button3.Enabled = true;
                return;
            }

            button1.Enabled = true;
            button3.Enabled = true;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            button9.Enabled = false;

            // -- Pre-Process Data --
            Int32 i, j, k, l = 0;
            String[] strArrayBlockHeader;
            String[] strArrayMidState;
            String[] strArrayMinHash;

            List<UInt16> X0_Train = new List<UInt16>();
            List<UInt16> X1_Train = new List<UInt16>();
            List<Single> Y_Train = new List<Single>();

            List<UInt16> X0_Test = new List<UInt16>();
            List<UInt16> X1_Test = new List<UInt16>();
            List<Single> Y_Test = new List<Single>();

            //Grab filter value for pre-processig data. Filter out data above this value, 
            // or if = 0 then apply no filtering  

            Double FilterValueLo = Convert.ToDouble(textBox1.Text);
            Double FilterValueHi = Convert.ToDouble(textBox3.Text);
            bool TestSet = false;

            for (i = 0; i < BlockHeaderList.Count; i++) // Process each file
            {
                // Last file is the test set
                if (i == (BlockHeaderList.Count - 1))
                    TestSet = true;
                else
                    TestSet = false;

                strArrayBlockHeader = BlockHeaderList[i];
                strArrayMidState = MidstateList[i];
                strArrayMinHash = MinHashList[i];

                for (j = 0; j < strArrayBlockHeader.Length; j++) // process each unit per file
                {
                    String strCombinedX = "";
                    String strCombinedY = "";
                    UInt32[] CombinedX;

                    // Process X array
                    strCombinedX = strArrayMidState[j] +
                                strArrayBlockHeader[j].Substring(W0_OFFSET, 24);
                    CombinedX = Utils.HexString_To_u32Array(strCombinedX);

                    for (k = (CombinedX.Length - 1); k > 0; k--)
                    {
                        for (l = 0; l < k; l++)
                            CombinedX[l] = CombinedX[l + 1] - CombinedX[l];
                    }

                     // Process Y array
                    strCombinedY = strArrayMinHash[j];
                    BigInteger DiffOneValue = new BigInteger(Utils.HexString_To_u8Array(DIFF_ONE));
                    BigInteger MinShare = new BigInteger(Utils.HexString_To_u8Array(strCombinedY + "00"));// Add extra leading "Zero" for proper BigInteger casting

                    Double tmpDiffOneValue = (Double)DiffOneValue;
                    Double tmpMinShare = (Double)MinShare;
                    Double tmpY_Out = (tmpDiffOneValue / tmpMinShare);

                    // Parse data into training and test data
                    if (TestSet)
                    {
                        X0_Test.Add((UInt16)(CombinedX[0] / (UInt32)Math.Pow(2, 16)));
                        X1_Test.Add((UInt16)(CombinedX[0] % (UInt32)Math.Pow(2, 16)));
                        Y_Test.Add((Single)tmpY_Out);
                    }
                    else
                    {
                        //Int32[] Classes = new Int32[4] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                        // 1 = <1
                        // 2 = >=1 && <2
                        // 3 = >=2 && <4
                        // 4 = >=4 && <8
                        // 5 = >=8 && <16
                        // 6 = >=16 && <32
                        // 7 = >=32 && <64
                        // 8 = >=64 && <128
                        // 9 = >=128 && <256
                        // 10 = >=256 && <512
                        // 11 = >=512 && <1024
                        // 12 = >=1024

                        // Divide Y results into catagories
                        if ((tmpY_Out >= FilterValueLo) && (tmpY_Out < FilterValueHi))
                        {

                        }

                        /*
                        // Check to see if data is within filter constraints
                        if (FilterValueHi == 0) // No filtering
                        {
                            X0_Train.Add((UInt16)(CombinedX[0] / (UInt32)Math.Pow(2, 16)));
                            X1_Train.Add((UInt16)(CombinedX[0] % (UInt32)Math.Pow(2, 16)));
                            Y_Train.Add((Single)tmpY_Out);
                        }
                        else if ((tmpY_Out >= FilterValueLo) && (tmpY_Out < FilterValueHi)) // Based on filter range
                        {
                            X0_Train.Add((UInt16)(CombinedX[0] / (UInt32)Math.Pow(2, 16)));
                            X1_Train.Add((UInt16)(CombinedX[0] % (UInt32)Math.Pow(2, 16)));
                            Y_Train.Add((Single)tmpY_Out);
                        }
                        */ 
                    }
                }
            }

            // -- Set up testing data --
            // Grab the last 1/4th of data to use as a test set. The remaining 3/4 will be used as model.
            //Int32 TestSetCount = X0.Count / 4;
            //Int32 TrainingSetCount = X0.Count - TestSetCount;

            UInt16[] X0_Train_Array = X0_Train.ToArray();
            UInt16[] X1_Train_Array = X1_Train.ToArray();
            Single[] Y_Train_Array = Y_Train.ToArray();

            UInt16[] X0_Test_Array = X0_Test.ToArray();
            UInt16[] X1_Test_Array = X1_Test.ToArray();
            Single[] Y_Test_Array = Y_Test.ToArray();
            Single[] Y_Predicted = new Single[Y_Test.Count];

            //Array.Copy(X0_Train_Array, TrainingSetCount, X0_Test_Array, 0, TestSetCount);
            //Array.Copy(X1_Train_Array, TrainingSetCount, X1_Test_Array, 0, TestSetCount);
            //Array.Copy(Y_Train_Array, TrainingSetCount, Y_Test_Array, 0, TestSetCount);


            // -- Perform Baz-Algo (Modified K NN Algorithm) --
            Int64 MinDistance = Int64.MaxValue;
            Int32 MinDistanceIndex = 0;
            
            UInt16 a, b = 0; // New data
            UInt16 p, q = 0; // Training data
            Int64 d = 0; // Distance

            for (i = 0; i < X0_Test_Array.Length; i++)
            {
                a = X0_Test_Array[i];
                b = X1_Test_Array[i];

                MinDistance = Int64.MaxValue;
                MinDistanceIndex = 0;

                for (j = 0; j < X0_Train_Array.Length; j++)
                {
                    p = X0_Train_Array[j];
                    q = X1_Train_Array[j];

                    d = (Int64)Math.Pow(a - p, 2) + (Int64)Math.Pow(b - q, 2);

                    if (d <= MinDistance)
                    {
                        MinDistance = d;
                        MinDistanceIndex = j;
                    }
                }

                Y_Predicted[i] = Y_Train_Array[MinDistanceIndex];
            }


            // -- Store Results --
            // Open up prompt for user to select directory and store the path
            String StoreDataDirectory = "";
            folderBrowserDialog1.Description = "Select The Directory To Store Data File(s)";
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StoreDataDirectory = folderBrowserDialog1.SelectedPath;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Prompting User To Select Folder Director. Error Code:: " + ex.Message);
                    button1.Enabled = true;
                    button9.Enabled = true;
                    return;
                }
            }

            // Write results to output
            StreamWriter FileOutputStreamWriter2;
            String FileNamePath2 = "";

            try
            {
                FileNamePath2 = StoreDataDirectory + "\\" + "ProcessedData_Knn" + ".csv";
                FileOutputStreamWriter2 = new StreamWriter(FileNamePath2);

                // Write header data
                FileOutputStreamWriter2.WriteLine("X0,X1,Y,Y_Predicted");

                for (i = 0; i < X0_Test_Array.Length; i++)
                {
                    //Output to file
                    FileOutputStreamWriter2.WriteLine(  X0_Test_Array[i].ToString() + "," +
                                                        X1_Test_Array[i].ToString() + "," +
                                                        Y_Test_Array[i].ToString() + "," +
                                                        Y_Predicted[i].ToString());
                }


                FileOutputStreamWriter2.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                button1.Enabled = true;
                button9.Enabled = true;
                return;
            }


            button1.Enabled = true;
            button9.Enabled = true;
        }
    }
}
