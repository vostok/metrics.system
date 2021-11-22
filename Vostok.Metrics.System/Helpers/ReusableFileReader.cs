using System;
using System.Collections.Generic;
using System.IO;
using Vostok.Commons.Environment;

namespace Vostok.Metrics.System.Helpers
{
    internal class ReusableFileReader : IDisposable
    {
        private readonly string path;
        private volatile StreamReader reader;

        public ReusableFileReader(string path)
            => this.path = path;

        public string ReadFirstLine()
        {
            Reset();

            return reader.ReadLine();
        }

        public IEnumerable<string> ReadLines()
        {
            Reset();

            while (!reader.EndOfStream)
                yield return reader.ReadLine();
        }

        public void Dispose()
            => reader?.BaseStream.Dispose();

        private void Reset()
        {
            if (reader == null || RuntimeDetector.IsDotNet60AndNewer)
            {
                reader?.BaseStream.Dispose();
                reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
            }

            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            reader.DiscardBufferedData();
        }
    }
}