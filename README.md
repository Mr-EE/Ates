#ATES#
.Net (Mono) Bitcoin CPU/GPU Alternative Mining Program

##Introduction##
This is a .NET (Mono) Bitcoin CPU/GPU mining program which also analyzes the incoming/computed data. That data is then analyzed with the goal to find a "pattern" to streamline the Bitcoin hashing algorithm and get an advantage over the ASICs.

In reality there is no "pattern" since SHA256 is secure and random, but the method in which the Bitcoin protocol "mines" for the block header is such that it looks for a 256bit value of the computed hash and compares below a certain target number. The pattern I was looking for was to find a method in which an algorithm can be used to guess at the probability that a given blockheader would compute to less then a certain number, thus skipping the brute force method of attempting all 2^32 nonce values.

Program in action:
![Alt text](https://github.com/Mr-EE/Ates/blob/master/Docs/ScreenCap_MainWindow.png "Main Window")
![Alt text](https://github.com/Mr-EE/Ates/blob/master/Docs/ScreenCap_DebugWindow.png "Debug Window")


There's not a lot of documentation because I started this project off as a side project to try some stuff out. Once the program grew to what it is now I went back and started documenting but never finished. Here's what I have of the mining program portion:
![Alt text](https://github.com/Mr-EE/Ates/blob/master/Docs/CryptoMiner_BlockDiagramV0_2_MainLoop.png "Main Loop Block Diagram")
![Alt text](https://github.com/Mr-EE/Ates/blob/master/Docs/CryptoMiner_BlockDiagramV0_2_Classes.png "Classes Block Diagram")

Unfortunately the data analysis portion was completely mad scientist and almost no documentation. Here's some outputs of analyzed data in a visual representation:

Visual representation of the Bitcoin hashing protocol on block height 267593:
![Alt text](https://github.com/Mr-EE/Ates/blob/master/Docs/VisualHash_267593.png "Visual Hash")


Data analyzed showing dots for "Zero" and "One"
  - One = A block header that had at least one nonce value resulting in a difficulty 1 or higher
  - Zero = A block header that had no nonce value capable of generating a hash value greter then difficulty 1
![Alt text](https://github.com/Mr-EE/Ates/blob/master/Docs/Graphs_SmallPoints.png "Graph of Blockheader data")



##Builds##
Refer to SOFTware folder. I sued Visual Studios 2010, but I also opened up the solution in SharpeDevelop and it worked. I built the Mono equivalent with the MCS compiler and it also worked, but some boxes looked a little funny.

I included some windows executable in the SOFTware folder that can be used on a Windows machine with >.NET 4.0. I included 64 and 32bit versions of the mining program and the data analyzer


##Progress##
Dead project (2014)

Other priorities came up and I had to abandon this project.

##License##
GPL v3






