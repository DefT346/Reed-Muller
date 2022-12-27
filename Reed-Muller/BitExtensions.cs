using System;
using System.Collections.Generic;
using System.Text;

namespace Reed_Muller
{
    public static class BitExtensions
    {
        public static int GetBit(this int b, int index) => (b >> index) & 1;

        public static void SetBit(this ref int intValue, int bitPosition, int bit)
        {
            if (bit == 1) intValue |= (1 << bitPosition);
            else intValue &= ~(1 << bitPosition);
        }
    }
}
