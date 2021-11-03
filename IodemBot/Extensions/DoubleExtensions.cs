using System;

namespace IodemBot
{
    public static class DoubleExtensions
    {
        public static int IntLop<T>(this double number, Func<double, T> mathFunc)
        {
            return Convert.ToInt32(mathFunc.Invoke(number));
        }
    }
}