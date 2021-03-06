﻿using System;
using System.Collections.Generic;
using System.IO;

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
            if (reader == null)
                reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));

            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            reader.DiscardBufferedData();
        }
    }
}
