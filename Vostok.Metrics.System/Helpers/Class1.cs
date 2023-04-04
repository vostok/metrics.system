#if NET6_0_OR_GREATER
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

#endif

namespace Vostok.Metrics.System.Helpers;

#if NET6_0_OR_GREATER
    internal static class SpanSplitter
    {
        public static Enumerable Split(this ReadOnlySpan<char> source, ReadOnlySpan<char> separators,
            bool removeEmptyEntries = false)
        {
            return new Enumerable(source, separators, removeEmptyEntries);
        }

        public readonly ref struct Enumerable
        {
            private readonly ReadOnlySpan<char> source;
            private readonly ReadOnlySpan<char> separators;
            private readonly bool removeEmptyEntries;

            public Enumerable(ReadOnlySpan<char> source, ReadOnlySpan<char> separators, bool removeEmptyEntries)
            {
                this.source = source;
                this.separators = separators;
                this.removeEmptyEntries = removeEmptyEntries;
            }

            public Enumerator GetEnumerator() => new Enumerator(source, separators, removeEmptyEntries);
        }

        public ref struct Enumerator
        {
            private readonly ReadOnlySpan<char> source;
            private readonly ReadOnlySpan<char> separators;
            private readonly bool removeEmptyEntries;

            private int index;

            private int firstChar;
            private int state;


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(ReadOnlySpan<char> source, ReadOnlySpan<char> separators, bool removeEmptyEntries)
            {
                this.source = source;
                this.separators = separators;
                this.removeEmptyEntries = removeEmptyEntries;
                firstChar = 0;
                state = 0;
                index = 0;
                Current = ReadOnlySpan<char>.Empty;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (index < source.Length)
                {
                    var charIsSeparator = separators.Contains(source[index]);
                    if (TryProcessChar(charIsSeparator))
                        return true;
                    index++;
                }

                if (TryProcessChar(true))
                {
                    state = 2;
                    return true;
                }

                if (Current.Length > 0)
                    Current = Span<char>.Empty; //erase
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            private bool TryProcessChar(bool charIsSeparator)
            {
                switch (state)
                {
                    case 0: //char
                        if (charIsSeparator)
                        {
                            state = 1;
                            if (index > firstChar || !removeEmptyEntries)
                            {
                                Current = source.Slice(firstChar, index - firstChar);
                                index++;
                                return true;
                            }
                        }

                        break;
                    case 1: //delimiter
                        if (!charIsSeparator)
                        {
                            firstChar = index;
                            state = 0;
                            break;
                        }

                        if (!removeEmptyEntries)
                        {
                            index++;
                            Current = Span<char>.Empty;
                            return true;
                        }

                        break;
                }

                return false;
            }


            public ReadOnlySpan<char> Current { get; private set; }
        }
    }



    /// <summary>
    /// string reader 
    /// </summary>
    internal class StreamLinesReader : IDisposable
    {
        private const double growFactor = 1.2;
        private const int maxBytesForChar = 4;
        private readonly byte[] bytesBuffer;
        private readonly bool ownsStream;
        private readonly Stream stream;
        private int bytesBufferLength;
        private int bytesReadFromBuffer;


        private char[] charBuffer; //charBuffer contains chars from 'start' of line. cant contain more than 1 line
        private int charBufferCharsRead;
        private int charBufferLength;
        private int charBufferStringStart;
        private bool consumeN;

        private bool lastByteRead;
        private bool resizeAllowed;

        /// <summary>
        /// correct usage: byteBufferSize big, charsBufferSize not big (and will be auto resized to longest string)
        /// if byteBufferSize is small, stream reading is not effective, and chars converted via small chunks
        /// </summary>
        /// <param name="stream">source stream</param>
        /// <param name="byteBufferSize">buffer size fro stream reading.</param>
        /// <param name="charsBufferSize">initial char bufferr size for string conversion. resized up to 1.2*(max_string_length + 1)</param>
        /// <param name="ownsStream">if true, Dispose will call stream.Dispose</param>
        public StreamLinesReader(Stream stream,
            int byteBufferSize = 4096,
            int charsBufferSize = 100,
            bool ownsStream = true)
        {
            if (byteBufferSize < maxBytesForChar)
                byteBufferSize = maxBytesForChar; //must fit 1 char UTF8

            if (charsBufferSize <= 2)
                charsBufferSize = 2; //safe: 2-char code point should fit

            this.stream = stream;
            this.ownsStream = ownsStream;
            bytesBuffer = new byte[byteBufferSize];
            charBuffer = new char[charsBufferSize];
            ResetBuffers();
        }

        /// <summary>
        /// true if end of stream is reached.
        /// becames true after TryReadLine firstly returns true
        /// </summary>
        public bool IsEOF { get; private set; }

        internal int CharBufferSize => charBuffer.Length;

        public void Dispose()
        {
            if (ownsStream)
                stream?.Dispose();
        }

        /// <summary>
        /// Reset stream position to 0 and clear internal buffers
        /// </summary>
        public void ResetToZero()
        {
            stream.Position = 0;
            ResetBuffers();
        }

        /// <summary>
        /// clear internal buffers
        /// call after stream position changed externally
        /// next string reads will start from new stream position
        /// </summary>
        public void ResetBuffers()
        {
            bytesBufferLength = 0;
            bytesReadFromBuffer = 0;

            charBufferCharsRead = 0;
            charBufferLength = 0;
            charBufferStringStart = 0;
            consumeN = false;

            resizeAllowed = true;
            lastByteRead = false;
            IsEOF = false;
        }

        public bool TryReadLine(out ReadOnlySpan<char> result)
        {
            while (!IsEOF)
            {
                ReadNextData();
                var nextStringStart = -1;

                if (IsEOF)
                    if (charBufferStringStart < charBufferLength)
                        nextStringStart = charBufferLength + 1;

                for (; charBufferCharsRead < charBufferLength; charBufferCharsRead++)
                {
                    var ch = charBuffer[charBufferCharsRead]; //end of line = any of "\n" or "\r" or "\r\n"
                    switch (ch)
                    {
                        case '\n':
                            if (consumeN)
                            {
                                consumeN = false;
                                charBufferStringStart++;
                                break;
                            }

                            consumeN = false;
                            nextStringStart = charBufferCharsRead + 1;
                            break;
                        case '\r':
                            nextStringStart = charBufferCharsRead + 1;
                            consumeN = true;
                            break;
                        default:
                            consumeN = false;
                            break;
                    }

                    if (nextStringStart >= 0)
                    {
                        charBufferCharsRead++; //skip this char, it was read
                        break;
                    }
                }


                if (nextStringStart >= 0)
                {
                    resizeAllowed = false;
                    var readOnlySpan = new ReadOnlySpan<char>(charBuffer, charBufferStringStart,
                        nextStringStart - charBufferStringStart - 1);
                    charBufferStringStart = nextStringStart;
                    result = readOnlySpan;
                    return true;
                }
            }

            result = ReadOnlySpan<char>.Empty;
            return false;
        }

        private void ReadNextData()
        {
            if (charBufferCharsRead >= charBufferLength) //check all chars are processed 
            {
                ReadNextBytes();
                AppendChars();
            }
        }

        private void AppendChars()
        {
            //note expect: charBufferCharsRead >= charBufferLength
            var srcToCopy = ReadOnlySpan<char>.Empty;
            if (resizeAllowed)
            {
                if (charBufferLength >= charBuffer.Length)
                {
                    //need resize
                    //do resize

                    var old = charBuffer;
                    var newLength =
                        Math.Max((int)(old.Length * growFactor), old.Length + 2); //new buffer should be larger.
                    charBuffer = new char[newLength];
                    srcToCopy = new ReadOnlySpan<char>(old, 0, charBufferLength);
                } //else - no need to resize. maybe bytesBuffer is small, convert bytes to [charBufferLength, charBuffer.Length)
            }
            else
            {
                //dont resize, shift buffer.
                //copy from [charBufferStringStart to charBufferLength)
                srcToCopy = new ReadOnlySpan<char>(charBuffer, charBufferStringStart,
                    charBufferLength - charBufferStringStart);
                resizeAllowed = true;
                charBufferCharsRead -= charBufferStringStart;
                charBufferLength -= charBufferStringStart;
                charBufferStringStart = 0;
            }

            if (srcToCopy.Length > 0)
                srcToCopy.CopyTo(new Span<char>(charBuffer));

            var status = Utf8.ToUtf16(
                new ReadOnlySpan<byte>(bytesBuffer, bytesReadFromBuffer, bytesBufferLength - bytesReadFromBuffer),
                new Span<char>(charBuffer, charBufferLength, charBuffer.Length - charBufferLength),
                out var bytesConverted, out var charsConverted, false, false);

            bytesReadFromBuffer += bytesConverted;
            charBufferLength += charsConverted;

            if (status == OperationStatus.InvalidData)
                FailBadBytes();

            if (lastByteRead)
                IsEOF = status == OperationStatus.Done && charsConverted == 0; //also should be bytesConverted==0
        }

        private void ReadNextBytes()
        {
            if (bytesReadFromBuffer + maxBytesForChar <
                bytesBufferLength) //not all buffer drained, do nothing. can convert >=1 valid char
                return;
            if (bytesReadFromBuffer < bytesBufferLength) //something remains. max 2 bytes for UTF8
            {
                //copy to beginning
                new Span<byte>(bytesBuffer, bytesReadFromBuffer, bytesBufferLength - bytesReadFromBuffer).CopyTo(
                    new Span<byte>(bytesBuffer));
            }

            bytesBufferLength -= bytesReadFromBuffer;
            bytesReadFromBuffer = 0; //copied bytes will be re-read while char conversion

            var bytesRead = stream.Read(bytesBuffer, bytesBufferLength, bytesBuffer.Length - bytesBufferLength);
            if (bytesRead == 0)
                lastByteRead = true;

            bytesBufferLength += bytesRead;
        }

        private void FailBadBytes()
        {
            throw new InvalidOperationException("Cant convert bytes to chars via UTF8");
        }
    }



#endif