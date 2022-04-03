﻿using System;
using System.Linq;

namespace Files.Helpers
{
    /// <summary>
    /// Some simple helper methods to convert doubles to fractions
    /// </summary>
    public static class Fractions
    {
        public static string ToFractions(this double number, int precision = 4)
        {
            int w, n, d;
            RoundToMixedFraction(number, precision, out w, out n, out d);
            var ret = $"{w}";
            if (w > 0)
            {
                if (n > 0)
                    ret = $"{w} {n}/{d}";
            }
            else
            {
                if (n > 0)
                    ret = $"{n}/{d}";
            }
            return ret;
        }

        private static void RoundToMixedFraction(double input, int accuracy, out int whole, out int numerator, out int denominator)
        {
            double dblAccuracy = (double)accuracy;
            whole = (int)(Math.Truncate(input));
            var fraction = Math.Abs(input - whole);
            if (fraction == 0)
            {
                numerator = 0;
                denominator = 1;
                return;
            }
            var n = Enumerable.Range(0, accuracy + 1).SkipWhile(e => (e / dblAccuracy) < fraction).First();
            var hi = n / dblAccuracy;
            var lo = (n - 1) / dblAccuracy;
            if ((fraction - lo) < (hi - fraction)) n--;
            if (n == accuracy)
            {
                whole++;
                numerator = 0;
                denominator = 1;
                return;
            }
            var gcd = GCD(n, accuracy);
            numerator = n / gcd;
            denominator = accuracy / gcd;
        }

        private static int GCD(int a, int b)
        {
            if (b == 0) return a;
            else return GCD(b, a % b);
        }
    }
}