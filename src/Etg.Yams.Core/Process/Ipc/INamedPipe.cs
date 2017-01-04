using System;
using System.IO;

namespace Etg.Yams.Process.Ipc
{
    public interface INamedPipe : IDisposable
    {
        Stream Stream { get; }
        bool IsConnected { get; }
        string PipeName { get; }
        void Connect();
        void Disconnect();
    }
}