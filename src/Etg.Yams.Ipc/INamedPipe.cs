using System;
using System.IO;

namespace Etg.Yams.Ipc
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