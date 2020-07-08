using System;
using System.Collections.Generic;
using System.IO;

namespace Vostok.Metrics.System.Helpers
{
    internal class ReusableFileReader
    {
        private readonly Lazy<StreamReader> reader;

        public ReusableFileReader(string path)
            => reader = new Lazy<StreamReader>(() => new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)));

        public string ReadFirstLine()
        {
            Reset();

            return reader.Value.ReadLine();
        }

        public IEnumerable<string> ReadLines()
        {
            Reset();

            while (!reader.Value.EndOfStream)
                yield return reader.Value.ReadLine();
        }

        private void Reset()
        {
            reader.Value.BaseStream.Seek(0, SeekOrigin.Begin);
            reader.Value.DiscardBufferedData();
        }
    }
}
