using System;
using System.Diagnostics;

namespace Etg.Yams.Utils
{
    public static class TraceUtils
    {
        public static void TraceAllErrors(string msg, AggregateException aggregateException)
        {
            foreach (Exception innerException in aggregateException.InnerExceptions)
            {
                Trace.TraceError("{0} Exception: {1}", msg, innerException);
            }
        }
    }
}
