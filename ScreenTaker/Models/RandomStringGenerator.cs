using System;
using System.Text;
using System.Threading;

namespace ScreenTaker.Models
{

    public static class StaticRandom
    {
        static int _seed = Environment.TickCount;

        static readonly ThreadLocal<Random> Random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        public static int Rand()
        {
            return Random.Value.Next();
        }

        public static int Rand(int maxValue)
        {
            return Random.Value.Next(maxValue);
        }

        public static int Rand(int minValue, int maxValue)
        {
            return Random.Value.Next(minValue, maxValue);
        }
    }

    public class RandomStringGenerator
    {
        public int Length { get; set; }
        public string Chars { get; set; }

        public string Next()
        {
            StringBuilder res = new StringBuilder(Length, Length);

            for (int i = 0; i < Length; ++i)
            {
                res.Append(Chars[StaticRandom.Rand(Chars.Length)]);
            }

            return res.ToString();
        }
    }
}