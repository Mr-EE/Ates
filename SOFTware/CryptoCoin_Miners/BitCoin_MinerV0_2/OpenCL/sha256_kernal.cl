//#define VECTORS4
//#define VECTORS2
#define BITALIGN

#define RShift(x, n) ((x) >> n)

#ifdef VECTORS4
	typedef uint4 u;
	#define W_SIZE (32)
#elif defined VECTORS2
	typedef uint2 u;
	#define W_SIZE (64)
#else
	typedef uint u;
	#define W_SIZE (128)
#endif

#ifdef BITALIGN
	#pragma OPENCL EXTENSION cl_amd_media_ops : enable
	#define Ch(x, y, z) bitselect((u)z, (u)y, (u)x)
	#define Ma(x, y, z) bitselect((u)x, (u)y, (u)z ^ (u)x)
	#define RRotate(x, y) amd_bitalign((u)x, (u)x, (u)y)
#else
	#define Ch(x, y, z) (z ^ (x & (y ^ z)))
	#define Ma(x, y, z) ((x & z) | (y & (x | z)))
	#define RRotate(x, y) rotate((u)x, (u)(32-y))
#endif


#define s0(i) (RRotate(w[i - 15], 7) ^ RRotate(w[i - 15], 18) ^ RShift(w[i - 15], 3))
#define s1(i) (RRotate(w[i - 2], 17) ^ RRotate(w[i - 2], 19) ^ RShift(w[i - 2], 10))

#define S0(i) (RRotate(i, 2) ^ RRotate(i, 13) ^ RRotate(i, 22))
#define S1(i) (RRotate(i, 6) ^ RRotate(i, 11) ^ RRotate(i, 25))

#define T1a(e,f,g,i) (h + S1(e) + Ch(e,f,g) + k[i] + w[i])
#define T1b(e,f,g,i) (h + S1(e) + Ch(e,f,g) + k[i] + w[i+64]) 
#define T2(a,b,c)    (S0(a) + Ma(a,b,c))

#define SHA256_Roundn_a(i) {t1 = T1a(e,f,g,i); t2 = T2(a,b,c); h = g; g = f; f = e; e = d + t1; d = c; c = b; b = a; a = t1 + t2;}
#define SHA256_Roundn_b(i) {t1 = T1b(e,f,g,i); t2 = T2(a,b,c); h = g; g = f; f = e; e = d + t1; d = c; c = b; b = a; a = t1 + t2;}


#define W(i)  (w[i] = w[i - 16] + s0(i) + w[i - 7] + s1(i))


__constant uint hi[8] =
{
    0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19
};

__constant uint k[64] =
{
	0x428a2f98,0x71374491,0xb5c0fbcf,0xe9b5dba5,0x3956c25b,0x59f111f1,0x923f82a4,0xab1c5ed5,
	0xd807aa98,0x12835b01,0x243185be,0x550c7dc3,0x72be5d74,0x80deb1fe,0x9bdc06a7,0xc19bf174,
	0xe49b69c1,0xefbe4786,0x0fc19dc6,0x240ca1cc,0x2de92c6f,0x4a7484aa,0x5cb0a9dc,0x76f988da,
	0x983e5152,0xa831c66d,0xb00327c8,0xbf597fc7,0xc6e00bf3,0xd5a79147,0x06ca6351,0x14292967,
	0x27b70a85,0x2e1b2138,0x4d2c6dfc,0x53380d13,0x650a7354,0x766a0abb,0x81c2c92e,0x92722c85,
	0xa2bfe8a1,0xa81a664b,0xc24b8b70,0xc76c51a3,0xd192e819,0xd6990624,0xf40e3585,0x106aa070,
	0x19a4c116,0x1e376c08,0x2748774c,0x34b0bcb5,0x391c0cb3,0x4ed8aa4a,0x5b9cca4f,0x682e6ff3,
	0x748f82ee,0x78a5636f,0x84c87814,0x8cc70208,0x90befffa,0xa4506ceb,0xbef9a3f7,0xc67178f2
};


__kernel void search(__global const uint *MidStateIn, __global const uint *wIn, __global const uint *GroupSizeIn, __global uint *NonceOut)
{

    u w[W_SIZE];
	//uint intHash_Out[8];
    u t1, t2 = 0;
	u a, b, c, d, e, f, g, h = 0;

	uint intHash7;

	w[0] = wIn[0];
	w[1] = wIn[1];
	w[2] = wIn[2];
	w[3] = (uint)get_global_id(0);
    w[4] = 0x80000000U;
    w[5] = w[6] = w[7] = w[8] = w[9] = w[10] = w[11] = w[12] = w[13] = w[14] = 0x00000000U;
    w[15] = 0x00000280U;
	W(16);
	W(17);
	W(18);
	W(19);
	W(20);
	W(21);
	W(22);
	W(23);
	W(24);
	W(25);
	W(26);
	W(27);
	W(28);
	W(29);
	W(30);
	W(31);
	W(32);
	W(33);
	W(34);
	W(35);
	W(36);
	W(37);
	W(38);
	W(39);
	W(35);
	W(36);
	W(37);
	W(38);
	W(39);
	W(40);
	W(41);
	W(42);
	W(43);
	W(44);
	W(45);
	W(46);
	W(47);
	W(48);
	W(49);
	W(50);
	W(51);
	W(52);
	W(53);
	W(54);
	W(55);
	W(56);
	W(57);
	W(58);
	W(59);
	W(60);
	W(61);
	W(62);
	W(63);

    a = MidStateIn[0]; b = MidStateIn[1]; c = MidStateIn[2]; d = MidStateIn[3]; e = MidStateIn[4]; f = MidStateIn[5]; g = MidStateIn[6]; h = MidStateIn[7];
	
	SHA256_Roundn_a(0);
	SHA256_Roundn_a(1);
    SHA256_Roundn_a(2);
	SHA256_Roundn_a(3);
	SHA256_Roundn_a(4);
    SHA256_Roundn_a(5);
	SHA256_Roundn_a(6);
	SHA256_Roundn_a(7);
    SHA256_Roundn_a(8);
	SHA256_Roundn_a(9);
	SHA256_Roundn_a(10);
    SHA256_Roundn_a(11);
	SHA256_Roundn_a(12);
	SHA256_Roundn_a(13);
    SHA256_Roundn_a(14);
	SHA256_Roundn_a(15);
	SHA256_Roundn_a(16);
    SHA256_Roundn_a(17);
	SHA256_Roundn_a(18);
	SHA256_Roundn_a(19);
    SHA256_Roundn_a(20);
	SHA256_Roundn_a(21);
	SHA256_Roundn_a(22);
    SHA256_Roundn_a(23);
	SHA256_Roundn_a(24);
	SHA256_Roundn_a(25);
    SHA256_Roundn_a(26);
	SHA256_Roundn_a(27);
	SHA256_Roundn_a(28);
    SHA256_Roundn_a(29);
	SHA256_Roundn_a(30);
	SHA256_Roundn_a(31);
    SHA256_Roundn_a(32);
	SHA256_Roundn_a(33);
	SHA256_Roundn_a(34);
    SHA256_Roundn_a(35);
	SHA256_Roundn_a(36);
	SHA256_Roundn_a(37);
    SHA256_Roundn_a(38);
	SHA256_Roundn_a(39);
	SHA256_Roundn_a(40);
    SHA256_Roundn_a(41);
	SHA256_Roundn_a(42);
	SHA256_Roundn_a(43);
    SHA256_Roundn_a(44);
	SHA256_Roundn_a(45);
	SHA256_Roundn_a(46);
    SHA256_Roundn_a(47);
	SHA256_Roundn_a(48);
	SHA256_Roundn_a(49);
    SHA256_Roundn_a(50);
	SHA256_Roundn_a(51);
	SHA256_Roundn_a(52);
    SHA256_Roundn_a(53);
	SHA256_Roundn_a(54);
	SHA256_Roundn_a(55);
    SHA256_Roundn_a(56);
	SHA256_Roundn_a(57);
	SHA256_Roundn_a(58);
    SHA256_Roundn_a(59);
    SHA256_Roundn_a(60);
	SHA256_Roundn_a(61);
	SHA256_Roundn_a(62);
    SHA256_Roundn_a(63);


    /* SHA pass 2 */
    w[64] = MidStateIn[0] + a;
    w[65] = MidStateIn[1] + b;
    w[66] = MidStateIn[2] + c;
    w[67] = MidStateIn[3] + d;
    w[68] = MidStateIn[4] + e;
    w[69] = MidStateIn[5] + f;
    w[70] = MidStateIn[6] + g;
    w[71] = MidStateIn[7] + h;
    w[72] = 0x80000000U;
    w[73] = w[74] = w[75] = w[76] = w[77] = w[78] = 0x00000000U;
    w[79] = 0x00000100U;
	W(80);
	W(81);
	W(82);
	W(83);
	W(84);
	W(85);
	W(86);
	W(87);
	W(88);
	W(89);
	W(90);
	W(91);
	W(92);
	W(93);
	W(94);
	W(95);
	W(96);
	W(97);
	W(98);
	W(99);
	W(100);
	W(101);
	W(102);
	W(103);
	W(104);
	W(105);
	W(106);
	W(107);
	W(108);
	W(109);
	W(110);
	W(111);
	W(112);
	W(113);
	W(114);
	W(115);
	W(116);
	W(117);
	W(118);
	W(119);
	W(120);
	W(121);
	W(122);
	W(123);
	W(124);
	W(125);
	W(126);
	W(127);

    a = hi[0], b = hi[1], c = hi[2], d = hi[3], e = hi[4], f = hi[5], g = hi[6], h = hi[7];

	SHA256_Roundn_b(0);
	SHA256_Roundn_b(1);
    SHA256_Roundn_b(2);
	SHA256_Roundn_b(3);
	SHA256_Roundn_b(4);
    SHA256_Roundn_b(5);
	SHA256_Roundn_b(6);
	SHA256_Roundn_b(7);
    SHA256_Roundn_b(8);
	SHA256_Roundn_b(9);
	SHA256_Roundn_b(10);
    SHA256_Roundn_b(11);
	SHA256_Roundn_b(12);
	SHA256_Roundn_b(13);
    SHA256_Roundn_b(14);
	SHA256_Roundn_b(15);
	SHA256_Roundn_b(16);
    SHA256_Roundn_b(17);
	SHA256_Roundn_b(18);
	SHA256_Roundn_b(19);
    SHA256_Roundn_b(20);
	SHA256_Roundn_b(21);
	SHA256_Roundn_b(22);
    SHA256_Roundn_b(23);
	SHA256_Roundn_b(24);
	SHA256_Roundn_b(25);
    SHA256_Roundn_b(26);
	SHA256_Roundn_b(27);
	SHA256_Roundn_b(28);
    SHA256_Roundn_b(29);
	SHA256_Roundn_b(30);
	SHA256_Roundn_b(31);
    SHA256_Roundn_b(32);
	SHA256_Roundn_b(33);
	SHA256_Roundn_b(34);
    SHA256_Roundn_b(35);
	SHA256_Roundn_b(36);
	SHA256_Roundn_b(37);
    SHA256_Roundn_b(38);
	SHA256_Roundn_b(39);
	SHA256_Roundn_b(40);
    SHA256_Roundn_b(41);
	SHA256_Roundn_b(42);
	SHA256_Roundn_b(43);
    SHA256_Roundn_b(44);
	SHA256_Roundn_b(45);
	SHA256_Roundn_b(46);
    SHA256_Roundn_b(47);
	SHA256_Roundn_b(48);
	SHA256_Roundn_b(49);
    SHA256_Roundn_b(50);
	SHA256_Roundn_b(51);
	SHA256_Roundn_b(52);
    SHA256_Roundn_b(53);
	SHA256_Roundn_b(54);
	SHA256_Roundn_b(55);
    SHA256_Roundn_b(56);
	SHA256_Roundn_b(57);
	SHA256_Roundn_b(58);
    SHA256_Roundn_b(59);
    SHA256_Roundn_b(60);
	SHA256_Roundn_b(61);
	SHA256_Roundn_b(62);
    SHA256_Roundn_b(63);


    //intHash_Out[0] = hi[0] + a;
    //intHash_Out[1] = hi[1] + b;
    //intHash_Out[2] = hi[2] + c;
    //intHash_Out[3] = hi[3] + d;
    //intHash_Out[4] = hi[4] + e;
    //intHash_Out[5] = hi[5] + f;
    //intHash_Out[6] = hi[6] + g;
    //intHash_Out[7] = hi[7] + h;

	intHash7 = hi[7] + h;

	if (intHash7 == 0)
	{
		NonceOut[0] = 1;
		NonceOut[1] = (uint)get_global_id(0);
	}
}