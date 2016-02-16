using System;

namespace Etg.Yams.Storage
{
    public class DuplicateBinariesException : Exception
    {
        public DuplicateBinariesException()
        {
        }

        public DuplicateBinariesException(string msg) : base(msg)
        {
        }

        public DuplicateBinariesException(string msg, Exception ex) : base(msg, ex)
        {
        }
    }
}