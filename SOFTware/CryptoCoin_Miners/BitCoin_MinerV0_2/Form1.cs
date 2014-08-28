using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Numerics;
using System.Globalization;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Cloo;

namespace BitCoin_MinerV0_2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

// Global variables/constants
        Worker _worker;
        Miner _miner;
        Stats _stats;
        Logger _logger;
        BackgroundWorker bgWorker1;

        Stopwatch stopWatch = new Stopwatch();
        Int64 nS_PerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;

        //bool isLogOutput = false;
        bool isAnalyzeData = false;

        //String StoreDataDirectory = "";
        //StreamWriter FileOutputStreamWriter;

        // Callable functions -------------------------------------------------------------------
        private void InitOpenCL()
        {
            Int32 PlatformID = CLPlatformsCmbBox.SelectedIndex;
            Int32 DeviceID = CLDevicesCmbBox.SelectedIndex;

            // Create an instance of the worker class on form load.
            _worker = new Worker();

            // Add selected platform to worker class
            _worker.cl_Platform = ComputePlatform.Platforms[PlatformID];

            // Add selected device to worker class
            _worker.cl_Devices.Clear();
            _worker.cl_Devices.Add(_worker.cl_Platform.Devices[DeviceID]);
        }

        private void GetUserControlData()
        {
            // Grab the data from the user control fields on the form and pass to variables
            _miner.UserName = UserNameTxtBox.Text;
            _miner.Password = PasswordTxtBox.Text;
            _miner.URLPort = Convert.ToInt32(PortTxtBox.Text);
            _miner.URL = URLTxtBox.Text;

            _worker.workerLocalSize[0] = (Int64)WorkSizeNumBox.Value;

            if (isAnalyzeDataChkBox.Checked == true)
            {
                isAnalyzeData = true;
                _worker.workerKernelSize[0] = _worker.KERNELSIZE_ANALYSIS;
                _worker.workerGlobalSize[0] = _worker.GLOBALSIZE_ANALYSIS;
            }
            else
            {
                isAnalyzeData = false;
                _worker.workerKernelSize[0] = _worker.KERNELSIZE_DEFAULT;
                _worker.workerGlobalSize[0] = (Int64)ThreadBatchNumBox.Value;
            }

            _worker.ThreadCoolDown = (Int32)ThreadCoolDownNumBox.Value;

            _logger.LogInterval = (Int32)LogIntervalNumBox.Value;

            switch (MiningProtocolCmbBox.SelectedIndex)
            {
                case 0:
                    _miner.MiningProtocolType = Miner.PROTCOL_GETWORK;
                    break;

                case 1:
                    _miner.MiningProtocolType = Miner.PROTCOL_TEST;
                    break;

                case 2:
                    _miner.MiningProtocolType = Miner.PROTCOL_GETWORK_STRATUM;
                    break;

                case 3:
                    _miner.MiningProtocolType = Miner.PROTCOL_FROM_FILE;
                    break;

                default:
                    _miner.MiningProtocolType = Miner.PROTCOL_TEST;
                    break;
            }

            if (isPoolChkBox.Checked)
                _miner.isPool = true;
            else
                _miner.isPool = false;

            /*
            if (isLogFileChkBox.Checked)
                isLogOutput = true;
            else
                isLogOutput = false;
            */
            // Not Implemented yet!!
            /*
            switch (MiningProtocolCmbBox.SelectedIndex)
            {
                case 0:
                    break;

                case 1:
                    break;

                default:
                    break;
            }
            */
        }

        private void UserControlDataState(bool state)
        {
            // If true then enable user control fields, else disable fields
            if (state == false)
            {
                MiningProtocolCmbBox.Enabled = false;
                UserNameTxtBox.Enabled = false;
                PasswordTxtBox.Enabled = false;
                PortTxtBox.Enabled = false;
                URLTxtBox.Enabled = false;
                Start_Mining.Enabled = false;
                WorkSizeNumBox.Enabled = false;
                ThreadBatchNumBox.Enabled = false;
                ThreadCoolDownNumBox.Enabled = false;
                LogIntervalNumBox.Enabled = false;
                CLDevicesCmbBox.Enabled = false;
                CLPlatformsCmbBox.Enabled = false;
                Cancel_Mining.Enabled = true;

                isPoolChkBox.Enabled = false;
                isAnalyzeDataChkBox.Enabled = false;
                isLogFileChkBox.Enabled = false;

                MiningStatusLbl.Text = "Mining...";
                MiningStatusPnl.BackColor = Color.Green;
            }
            else
            {
                MiningProtocolCmbBox.Enabled = true;
                UserNameTxtBox.Enabled = true;
                PasswordTxtBox.Enabled = true;
                PortTxtBox.Enabled = true;
                URLTxtBox.Enabled = true;
                Start_Mining.Enabled = true;
                WorkSizeNumBox.Enabled = true;
                ThreadBatchNumBox.Enabled = true;
                ThreadCoolDownNumBox.Enabled = true;
                LogIntervalNumBox.Enabled = true;
                CLDevicesCmbBox.Enabled = true;
                CLPlatformsCmbBox.Enabled = true;
                Cancel_Mining.Enabled = false;

                isPoolChkBox.Enabled = true;
                isAnalyzeDataChkBox.Enabled = true;
                isLogFileChkBox.Enabled = true;

                MiningStatusLbl.Text = "Idle...";
                MiningStatusPnl.BackColor = Color.Red;
            }

        }

        private void ClearStats()
        {
            // Reset stats to intial state
            SessionHashRateTxtBox.Clear();
            HashRateThreadTxtBox.Clear();
            SessionTimeTxtBox.Clear();
            ThreadTimeTxtBox.Clear();
            TotalSharesTxtBox.Clear();
            AcceptedSharesTxtBox.Clear();
            FailedHWTxtBox.Clear();
            RejectedSharesTxtBox.Clear();
            CurrentDiffTxtBox.Clear();
            DiffOneSharesTxtBox.Clear();
            LastShareDiffTxtBox.Clear();
            MaxDiffTxtBox.Clear();
            NetworkDiffTxtBox.Clear();
            AverageDiffTxtBox.Clear();
            BlockProgressBar.Value = 0;
            ProgressCntLbl.Text = "xx% Complete";
            OutputTxtBox.Clear();
            DebugTxtBox.Clear();
            BatchTimeTxtBox.Clear();
            //_stats.ClearStats();
        }

        private void UpdateStats()
        {
            // Grab stats data and pass to respective textbox, progess bar, etc...
            SessionHashRateTxtBox.Text = _stats.SessionHashRate.ToString("N2");
            SessionAvgHashRateTxtBox.Text = _stats.SessionAvgHashRate.ToString("N2");

            HashRateThreadTxtBox.Text = _stats.HashRateThread.ToString("N0");
            SessionTimeTxtBox.Text = ((Int64)_stats.Sessiontime).ToString();
            ThreadTimeTxtBox.Text = _stats.ThreadTime.ToString();
            BatchTimeTxtBox.Text = _stats.BatchTime.ToString("N3");

            CurrentBlockTxtBox.Text = _stats.CurrentBlock.ToString();
            CurrentDiffTxtBox.Text = _stats.CurrentDiff.ToString();
            NetworkDiffTxtBox.Text = _stats.NetworkDiff.ToString();
            TotalSharesTxtBox.Text = _stats.TotalBlocks.ToString();
            AcceptedSharesTxtBox.Text = _stats.AcceptedBlocks.ToString();
            FailedHWTxtBox.Text = _stats.FailedHWBlocks.ToString();
            RejectedSharesTxtBox.Text = _stats.RejectedBlocks.ToString();
            BlockProgressBar.Value = _stats.BlockProgress;

            DiffOneSharesTxtBox.Text = _stats.DiffOneBlocks.ToString();
            LastShareDiffTxtBox.Text = _stats.LastShareDiff.ToString();
            MaxDiffTxtBox.Text = _stats.MaxDiff.ToString();
            //AverageDiffTxtBox.Clear();

            ProgressCntLbl.Text = _stats.BlockProgress.ToString() + "% Complete";
        }

        public bool PrepareWork()
        {
            // Clean up crap left behind before running another batch
            //GC.Collect();
            _stats.BlockProgress = 0;
            UpdateStats();

            if (_miner.ContinueMining == true)
            {

                // Initiate and execute noncefinder thread
                try
                {
                    // Call getwork and set up data to mine
                    _miner.GetWork();

                    // Pass received data from Getwork to worker class
                    _worker.BlockHeader = _miner.BlockHeader;
                    _worker.MidState = _miner.MidState;
                    _worker.Target = _miner.Target;
                    _stats.CurrentBlock = _miner.CurrentBlock;
                    _stats.CurrentDiff = Utils.GetDiffculty(Utils.HexString_To_u8Array(_miner.Target));
                    _stats.NetworkDiff = Utils.GetDiffculty(Utils.HexString_To_u8Array(Utils.GetTargetFromBlockHeader(_miner.BlockHeader)));


                    // Set starting nonce value and reset global size to user input
                    _worker.workerOffset[0] = _worker.OFFSET_DEFAULT;

                    if (isAnalyzeData == true)
                    {
                        _worker.workerKernelSize[0] = _worker.KERNELSIZE_ANALYSIS;
                        _worker.workerGlobalSize[0] = _worker.GLOBALSIZE_ANALYSIS;
                    }
                    else
                    {
                        _worker.workerKernelSize[0] = _worker.KERNELSIZE_DEFAULT;
                        _worker.workerGlobalSize[0] = (Int64)ThreadBatchNumBox.Value;
                    }

                    // Setup Open CL object
                    _worker.SetUpOpenCL();

                    // Set up background worker object & hook up handlers
                    bgWorker1 = new BackgroundWorker();
                    bgWorker1.WorkerSupportsCancellation = true;

                    if (isAnalyzeData == true)
                        bgWorker1.DoWork += new DoWorkEventHandler(bgWorker_DoWorkOpenCL1);
                    else
                        bgWorker1.DoWork += new DoWorkEventHandler(bgWorker_DoWorkOpenCL0);

                    bgWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

                    //bgWorker1.WorkerReportsProgress = true;
                    //bgWorker1.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);

                    // Launch background thread to do the work.  This will
                    // trigger bgWorker_DoWorkOpenCL(arg).
                    stopWatch.Reset();
                    stopWatch.Start(); // Start timer

                    bgWorker1.RunWorkerAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                    return false;
                }
            }
            else
                return false;

            return true;
        }

        private void bgWorker_DoWorkOpenCL0(object sender, DoWorkEventArgs e) // **This method is on a different thread**
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            Int64 ThisEnd, NextOffset, NextEnd = 0;
            bool LoopEndFlag = false;
            Int64 NonceCounter = 0;

            // Before starting the thread reset nonce flag to error. Only a valid execution will set this value valid
            _worker.CompletedWorkFlag = _worker.NONCE_ERROR;

            try
            {
                while (!bw.CancellationPending && !LoopEndFlag)
                {
                    // Wait a user defined time before next openCL thread execution. If this is a GPU device then this gives it
                    //  some time to perform graphics related tasks and not freeze/crash card
                    Thread.Sleep(_worker.ThreadCoolDown);

                    if (_worker.ExecuteOpenCL0())
                    {
                        NonceCounter += _worker.workerGlobalSize[0];
                        LoopEndFlag = true;
                        _worker.CompletedWorkFlag = _worker.NONCE_FOUND;
                        _worker.NonceToCheck = _worker.NonceOut[1];
                    }
                    else
                    {
                        NonceCounter += _worker.workerGlobalSize[0];

                        // See if we have exhausted all the nonce values
                        ThisEnd = _worker.workerOffset[0] + (_worker.workerGlobalSize[0] - 1);
                        NextOffset = _worker.workerOffset[0] + _worker.workerGlobalSize[0];
                        NextEnd = NextOffset + (_worker.workerGlobalSize[0] - 1);

                        if (ThisEnd >= UInt32.MaxValue)
                        {
                            _worker.CompletedWorkFlag = _worker.MAXNONCE_REACHED;
                            _worker.NonceToCheck = 0;
                            LoopEndFlag = true;
                        }
                        else if (NextEnd > UInt32.MaxValue)
                        {
                            _worker.workerOffset[0] = NextOffset;
                            _worker.workerGlobalSize[0] = _worker.workerGlobalSize[0] - (NextEnd - UInt32.MaxValue);
                        }
                        else
                            _worker.workerOffset[0] = NextOffset;
                    }

                    // Update progress
                    _stats.BlockProgress = (Int32)(((Single)NonceCounter / (Single)UInt32.MaxValue) * (Single)100);

                    // Clean up crap left behind before running another batch
                    //GC.Collect();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            // Pass data back to orginating thread
            e.Result = NonceCounter;

            // If operation was cancelled (triggered by CancellationPending), 
            // we bailed out early.  But still need to set
            // Cancel flag, because RunWorkerCompleted event will still fire.
            if (bw.CancellationPending)
                e.Cancel = true;
        }

        private void bgWorker_DoWorkOpenCL1(object sender, DoWorkEventArgs e) // **This method is on a different thread**
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            Int64 NonceCounter = 0;
            Int32 i,j = 0;

            // Before starting the thread reset nonce flag to error. Only a valid execution will set this value valid
            _worker.CompletedWorkFlag = _worker.NONCE_ERROR;

            // Clear list before storing new data
            _worker.MinArray_List.Clear();

            try
            {
                for (i = 0; i < _worker.BATCHLOOPS_ANALYSIS; i++)
                {
                    // Perform kernerl execution and store result to a list for later processing
                    _worker.MinArray_List.Add(_worker.ExecuteOpenCL1());

                    NonceCounter += (_worker.workerGlobalSize[0] * _worker.workerKernelSize[0]);

                    // Wait a user defined time before next openCL thread execution. If this is a GPU device then this gives it
                    //  some time to perform graphics related tasks and not freeze/crash card
                    Thread.Sleep(_worker.ThreadCoolDown);

                    _worker.workerOffset[0] += _worker.workerGlobalSize[0];

                    // Update progress
                        _stats.BlockProgress = (Int32)(((Single)NonceCounter / (Single)UInt32.MaxValue) * (Single)100);

                    if (bw.CancellationPending)
                        break;
                }

                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Pass data back to orginating thread
            e.Result = NonceCounter;

            // Find lowest hash and see if it's greater than Diff 1
            Byte[] CheckMinHash = new Byte[32];
            _worker.NonceToCheck = _worker.FindMinHashIndex();
            CheckMinHash = _worker.GetHash(_worker.NonceToCheck);

            // Check to see if >= Diff 1
            i = 0;
            j = 31;
            while ( (CheckMinHash[j] == 0) && (j != 0) )
            {
                j--;
                i++;
            }

            if (i >= 4)
                _worker.CompletedWorkFlag = _worker.NONCE_FOUND;
            else
                _worker.CompletedWorkFlag = _worker.NONCE_NOTFOUND;

            // If operation was cancelled (triggered by CancellationPending), 
            // we bailed out early.  But still need to set
            // Cancel flag, because RunWorkerCompleted event will still fire.
            if (bw.CancellationPending)
                e.Cancel = true;
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Once we arrive here we are either finished our thread or it was cancelled or there was an error.
            stopWatch.Stop();

            // Some clean-up of buffers that was used from the last batch.
            _worker.DiposeMemObjects();
            GC.Collect();

            if (e.Error != null)
            {
                OutputTxtBox.AppendText(DateTime.Now.ToString() + ": " + e.Error.Message + "\n");
                OutputTxtBox.AppendText("\n");
                UserControlDataState(true);
                _miner.ContinueMining = false;
                // Stop stats/log timer based on user input
                LogTimer.Stop();
                LogTimer.Enabled = false;

                return;
            }
            else if (e.Cancelled)
            {
                OutputTxtBox.AppendText(DateTime.Now.ToString() + ": " + "** Cancelled **" + "\n");
                OutputTxtBox.AppendText("\n");
                UserControlDataState(true);
                _miner.ContinueMining = false;
                // Stop stats/log timer based on user input
                LogTimer.Stop();
                LogTimer.Enabled = false;

                return;
            }
            else
            {
                String ResultMessage = "";

                //Update some stats
                _stats.ThreadTime = ((Double)stopWatch.ElapsedMilliseconds / 1000.0);
                _stats.Sessiontime += _stats.ThreadTime;
                _stats.TimeSinceLastShare += _stats.ThreadTime;
                _stats.NoncesProcessed = (Int64)e.Result;
                _stats.UpdateHashRateThread();
                _stats.UpdateBatchTime((Int64)ThreadBatchNumBox.Value);
                _stats.NoncesProcessed = 0;

                _stats.TotalBlocks++;

                // Only keep one event in the output windows as it can get huge if you log eveything!
                OutputTxtBox.Clear();

                // Did we find a Nonce? or did we run out of nonces to try?
                if (_worker.CompletedWorkFlag == _worker.NONCE_FOUND)
                {
                    // Check work to see if it meets difficulty 1
                    if (_worker.VerifyWork(_worker.NonceToCheck))
                    {
                        // [DEBUG] Output to debug screen 
                        _logger.UpdateLogger(_worker.BlockHeader, _worker.ValidNonce, _worker.MidState);
                        DebugTxtBox.AppendText(_logger.CreateOutputString().ToUpper() + "," + Utils.u8Array_To_HexString(_worker.FinalHash).ToUpper() + "," + "TRUE" + "\n");

                        // Update some stats
                        _stats.DiffOneBlocks++;
                        _stats.LastShareDiff = Utils.GetDiffculty(_worker.FinalHash);
                        _stats.TotalDiff += _stats.LastShareDiff;
                        _stats.UpdateMaxDiffculty();
                        _stats.UpdateSessionHashRate();
                        _stats.UpdateSessionAvgHashRate();
                        _stats.TimeSinceLastShare = 0;

                        // Check to see if it meets target
                        if (_worker.CompareTarget())
                        {
                            // If this is a internal test then don't actually send a share!
                            if ((_miner.MiningProtocolType != Miner.PROTCOL_TEST) && (_miner.MiningProtocolType != Miner.PROTCOL_FROM_FILE))
                            {
                                // Send share and check it was successfull
                                if (_miner.SendShare(Utils.HexString_To_u8Array(_worker.ValidBlockHeader)))
                                {
                                    _stats.AcceptedBlocks++;
                                    ResultMessage = "Share Found >= Target and was ACCEPTED!";
                                }
                                else
                                {
                                    _stats.RejectedBlocks++;
                                    ResultMessage = "Share Found >= Target and was REJECTED!";
                                }
                            }
                            else
                            {
                                _stats.AcceptedBlocks++;
                                ResultMessage = "Share Found >= Target and was ACCEPTED!";
                            }
                        }
                        else
                            ResultMessage = "Share Found >= Diff 1 (but not above Target)";
                    }
                    else
                    {
                        ResultMessage = "Share Found but failed verification";
                        _stats.FailedHWBlocks++;
                    }


                    OutputTxtBox.AppendText("**  " + DateTime.Now.ToString() + " **\n");

                    if (isAnalyzeData == true)
                        OutputTxtBox.AppendText("!! Analysis block !! \n");
                    else
                        OutputTxtBox.AppendText("!! Mining block !! \n");

                    OutputTxtBox.AppendText(ResultMessage + "\n");
                    OutputTxtBox.AppendText("Message: " + _worker.BlockHeader.ToUpper() + "\n");
                    OutputTxtBox.AppendText("Valid Nonce Value: " + _worker.ValidNonce.ToString("X") + "\n");
                    OutputTxtBox.AppendText("Hash Result: " + Utils.u8Array_To_HexString(_worker.FinalHash).ToUpper() + "\n");
                    OutputTxtBox.AppendText("Target:      " + _worker.Target.ToUpper() + "\n");
                    OutputTxtBox.AppendText("Share Diffculty: " + _stats.LastShareDiff.ToString());
                    OutputTxtBox.AppendText("\n");
                }
                else if (_worker.CompletedWorkFlag == _worker.MAXNONCE_REACHED)
                {
                    OutputTxtBox.AppendText("**  " + DateTime.Now.ToString() + " **\n");
                    OutputTxtBox.AppendText("!! Mining block !! \n");
                    OutputTxtBox.AppendText("!! No Nonce Found. Reached Maximum Nonce Attempts \n");
                    OutputTxtBox.AppendText("\n");

                    // [DEBUG] Output to debug screen
                    _logger.UpdateLogger(_worker.BlockHeader, 0, _worker.MidState);
                    DebugTxtBox.AppendText(_logger.CreateOutputString().ToUpper() + "," + "--" + "," + "FALSE" + "\n");
                }
                else if (_worker.CompletedWorkFlag == _worker.NONCE_NOTFOUND)
                {
                    OutputTxtBox.AppendText("**  " + DateTime.Now.ToString() + " **\n");
                    OutputTxtBox.AppendText("!! Analysis block !! \n");
                    OutputTxtBox.AppendText("!! No >= 1 Diff Shares Found \n");
                    OutputTxtBox.AppendText("\n");

                    // [DEBUG] Output to debug screen
                    _logger.UpdateLogger(_worker.BlockHeader, _worker.NonceToCheck, _worker.MidState);
                    DebugTxtBox.AppendText(_logger.CreateOutputString().ToUpper() + "," + Utils.u8Array_To_HexString(_worker.GetHash(_worker.NonceToCheck)).ToUpper() + "," + "FALSE" + "\n");
                }
                else
                {
                    OutputTxtBox.AppendText(DateTime.Now.ToString() + ": " + "** Unknown Error?!?! **" + "\n");
                    OutputTxtBox.AppendText("\n");
                    UserControlDataState(true);
                    // Stop stats/log timer based on user input
                    LogTimer.Stop();
                    LogTimer.Enabled = false;

                    return;
                }

                // Call prepare work method with repeat mining thread attempt. If failed attempt end mining.
                if (!PrepareWork())
                {
                    _miner.ContinueMining = false;

                    // Stop stats/log timer based on user input
                    LogTimer.Stop();
                    LogTimer.Enabled = false;
                    UserControlDataState(true);
                }
            }
        }

        /*
        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Just update progress bar with % complete
            _stats.BlockProgress = e.ProgressPercentage;
        }
        */

        //Methods -------------------------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            Int32 i = 0;

            // Check to see if there's at least one platform, then display them in the drop down box
            CLPlatformsCmbBox.Items.Clear();
            if (ComputePlatform.Platforms.Count > 0)
            {
                for (i = 0; i < ComputePlatform.Platforms.Count; i++)
                    CLPlatformsCmbBox.Items.Add(ComputePlatform.Platforms[i].Name);

                // Pick the first platform and select it
                CLPlatformsCmbBox.SelectedIndex = 0;

                // Since this happens on form load, check to see if any OpenCL devices are available and display them
                CLDevicesCmbBox.Items.Clear();
                if (ComputePlatform.Platforms[0].Devices.Count > 0)
                {
                    for (i = 0; i < ComputePlatform.Platforms[0].Devices.Count; i++)
                        CLDevicesCmbBox.Items.Add(ComputePlatform.Platforms[0].Devices[i].Name);            
                }
            }

            // Populate each drop down menu with the first item
            CLDevicesCmbBox.SelectedIndex = 0;
            MininingMethodCmbBox.SelectedIndex = 0;
            MiningProtocolCmbBox.SelectedIndex = 0;  
        }

        private void CLPlatformsCmbBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            Int32 PlatformID = CLPlatformsCmbBox.SelectedIndex;
            Int32 i = 0;

            // Check to see if any OpenCL devices are avaialbe and display them
            if (ComputePlatform.Platforms[PlatformID].Devices.Count > 0)
            {
                CLDevicesCmbBox.Items.Clear();
                if (ComputePlatform.Platforms[PlatformID].Devices.Count > 0)
                {
                    for (i = 0; i < ComputePlatform.Platforms[PlatformID].Devices.Count; i++)
                        CLDevicesCmbBox.Items.Add(ComputePlatform.Platforms[PlatformID].Devices[i].Name);
                }
            }

            CLDevicesCmbBox.SelectedIndex = 0;
        }

        private void Clear_Output_Click(object sender, EventArgs e)
        {
            OutputTxtBox.Clear();
            DebugTxtBox.Clear();
        }

        private void Start_Mining_Click(object sender, EventArgs e)
        {
            // Clear up any unused crap before starting a mining session
            GC.Collect();

            InitOpenCL();

            // Create instances of classes for next mining session
            _miner = new Miner();
            _stats = new Stats();
            _logger = new Logger();
            _worker.ResetClass();

            // Get user data and place info into user data class for easy access
            GetUserControlData();

            // Disable User data field since we already got the data and disable the start button and enable the stop button
            UserControlDataState(false);

            /*
            // Check to see if we are going to log output.
            if (isLogOutput)
            {
                StoreDataDirectory = "";

                // Prompt user to select a directory where the data files will be stored
                // Set up initial parameters
                folderBrowserDialog1.Description = "Select The Directory To Store log Files";
                folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Desktop;

                // Open up prompt for user to select directory and store the path
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        StoreDataDirectory = folderBrowserDialog1.SelectedPath;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error Prompting User To Select Folder Director. Error Code:: " + ex.Message);
                    }

                }

                String FileNamePath = StoreDataDirectory + "\\" + "OutputLogFile_" + DateTime.Now.ToString() + ".csv";
                FileOutputStreamWriter = new StreamWriter(FileNamePath);
            }
            */

            // Clear all stats and output window
            ClearStats();
            OutputTxtBox.Clear();

            // Build OpenCL program
            if (isAnalyzeData == true)
                _worker.KernelSelection = Worker.OPENCL_CUSTOM_MIN;
            else
                _worker.KernelSelection = Worker.OPENCL_CUSTOM;

            _worker.BuildOpenCL();

            _miner.ContinueMining = true;

            //Mine from a file using a csv file of block header data. Format = "BlochHeight","BlockHeaderData"
            if (_miner.MiningProtocolType == Miner.PROTCOL_FROM_FILE) 
            {
                Int32 i, j = 0;
                List<String> intBlockHeaderList = new List<String>();
                List<String> intBlockHeaderHeight = new List<String>();

                _miner.BlockHeaderList.Clear();
                _miner.BlockHeightList.Clear();

                openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                openFileDialog1.Multiselect = true;

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        Int32 FileCnt = openFileDialog1.FileNames.Length;
                        String str = "";
                        Int32 start, end = 0;

                        for (i = 0; i < FileCnt; i++)
                        {
                            StreamReader sr_data = new StreamReader(openFileDialog1.FileNames[i]);

                            str = "";

                            // Reset index pointers. Read in data until end of file or max array size
                            j = 0;
                            while (((str = sr_data.ReadLine()) != null) & (j <= Int32.MaxValue))
                            {
                                // There might be some extra line feeds at the end of the dataset. If so then the end of file has been reached for usefull data
                                if (str == "")
                                    continue;

                                start = 0;
                                end = str.IndexOf(",", start); // First "," = Block Height
                                _miner.BlockHeightList.Add(str.Substring(start, (end - start)));

                                start = end + 1;
                                end = str.Length;
                                _miner.BlockHeaderList.Add(str.Substring(start, (end - start)));

                                // Increment line counter
                                j++;
                            }
                            sr_data.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                        return;
                    }
                }
                else
                    return;
            }

            // Call prepare work method to initiate the first thread minining attempt
            if (PrepareWork())
            {
                // Set up stats/log timer based on user input
                LogTimer.Interval = _logger.LogInterval * 1000; // Convert to seconds
                LogTimer.Enabled = true;
                LogTimer.Start();
            }
            else
                UserControlDataState(true);
        }

        private void Cancel_Mining_Click(object sender, EventArgs e)
        {
            bgWorker1.CancelAsync();
            //UserControlDataState(true);

            _miner.ContinueMining = false;

            // Stop stats/log timer based on user input
            LogTimer.Stop();
            LogTimer.Enabled = false;
        }

        private void LogTimer_Tick(object sender, EventArgs e)
        {
            UpdateStats();
        }

        private void About_Btn_Click(object sender, EventArgs e)
        {
            AboutBox1 Box = new AboutBox1();
            Box.ShowDialog();
        }

        private void isPoolChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (isPoolChkBox.Checked)
            {
                UserNameTxtBox.Text = "";
                PasswordTxtBox.Text = "";
                PortTxtBox.Text = "8332";
                URLTxtBox.Text = "http://mint.bitminter.com";
            }
            else
            {
                UserNameTxtBox.Text = "test1";
                PasswordTxtBox.Text = "1234";
                PortTxtBox.Text = "8332";
                URLTxtBox.Text = "http://127.0.0.1";
            }
        }

        // If data is to be analyzed from a file then no point of setting thread count
        private void isAnalyzeDataChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (isAnalyzeDataChkBox.Checked)
                ThreadBatchNumBox.Enabled = false;
            else
                ThreadBatchNumBox.Enabled = true;
        }
    }
}
