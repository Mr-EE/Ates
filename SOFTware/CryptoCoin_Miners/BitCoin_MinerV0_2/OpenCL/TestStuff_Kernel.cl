__kernel void SingleAddition(__global const uint *A, __global uint *C)
{
	uint a = A[0];
	uint c = 0;
	uint i = 0;

	for (i = 0; i<= 640000000; i++)
	{
		c = a + c;
	}

	C[0] = c;
}