using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public static class MathHelpers
    {
        public static int Gcd(int x, int y)
        {
            return y == 0 ? Math.Abs(x) : Gcd(y, x % y);
        }
    }
}
