using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace AzureTestUtils
{
    public class StorageEmulatorProxy
    {
        private const string DefaultStorageEmulatorLocation = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe";
        private const string StorageEmulatorLocationKey = "StorageEmulatorLocation";
        private readonly string _storageEmulatorLocation;

        public StorageEmulatorProxy()
        {
            _storageEmulatorLocation = ConfigurationManager.AppSettings[StorageEmulatorLocationKey];
            if (_storageEmulatorLocation == null)
            {
                Trace.TraceWarning("Storage Emulator Location was not found in the app.config. The default location will be used");
                _storageEmulatorLocation = DefaultStorageEmulatorLocation;
            }
            Trace.TraceInformation("The Storage Emulator Location is " + _storageEmulatorLocation);
            if (!File.Exists(_storageEmulatorLocation))
            {
                throw new Exception("Could not find the storage emulator exe at " + _storageEmulatorLocation);
            }
        }

        public void StartEmulator()
        {
            ExecuteCommandOnEmulator("start");
        }

        public void StopEmulator()
        {
            ExecuteCommandOnEmulator("stop");
        }

        public void ClearBlobStorage()
        {
            ExecuteCommandOnEmulator("clear blob");
        }

        private void ExecuteCommandOnEmulator(string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                Arguments = arguments,
                FileName = _storageEmulatorLocation
            };
            Process proc = new Process
            {
                StartInfo = start
            };

            proc.Start();
            proc.WaitForExit();
        }
    }
}
