#ATES#
.Net (Mono) Bitcoin CPU/GPU Alternative Mining Program

##Introduction##
This is a .NET (Mono) Bitcoin CPU/GPU mining program which also analyzes the incoming/computed data. That data is then analyzed with the goal to find a "pattern" to streamline the Bitcoin hashing algorithm and get an advantage over the ASICs.

In reality there is no "pattern" since SHA256 is secure and random, but the method in which the Bitcoin protocol "mines" for the block header is such that it looks for a 256bit value of the computed hash and compares below a certain target number. The pattern I was looking for was to find a method in which an algorithm can be used to guess at the probability that a given blockheader would compute to less then a certain number, thus skipping the brute force method of attempting all 2^32 nonce values.

Program in action:



Visual reprsentation of the Bitcoin hashing protocol on block height 267593:
![Alt text](https://github.com/Mr-EE/Ates/Docs/VisualHash_267593.png "Visual Hash")


Data analyzed showing dots for "Zero" and "One"
  - One = A block header that had at least one nonce value resulting in a difficulty 1 or higher
  - Zero = A block header that had no nonce value capable of generating a hash value greter then difficulty 1
![Alt text](https://github.com/Mr-EE/Ates/Docs/Graphs_SmallPoints.png "Graph of Blockheader data")

