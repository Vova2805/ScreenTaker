using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ScreenTaker.Models
{
    public class RandomStringGenerator
    {
        public int Length { get; set; }
        public string Chars { get; set; }

        Random _rand = new Random();

        public string Next()
        {
            StringBuilder res = new StringBuilder(Length, Length);

            for (int i = 0; i < Length; ++i)
            {
                res.Append(Chars[_rand.Next(Chars.Length)]);
            }

            return res.ToString();
        }
    }
}