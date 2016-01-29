using System;

namespace Etg.Yams.Storage
{
    public class BinariesNotFoundException : Exception
    {
        public BinariesNotFoundException()
        {
        }

        public BinariesNotFoundException(string msg) : base(msg)
        {
        }

        public BinariesNotFoundException(string msg, Exception ex) : base(msg, ex)
        {
        }
    }
}