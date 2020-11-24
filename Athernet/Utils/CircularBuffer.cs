using System;

namespace Athernet.Utils
{
    /// <summary>
    /// A very basic circular buffer implementation
    /// </summary>
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly object _lockObject;
        private int _writePosition;
        private int _readPosition;

        /// <summary>
        /// Create a new circular buffer
        /// </summary>
        /// <param name="size">Max buffer size in <c>T</c></param>
        public CircularBuffer(int size)
        {
            _buffer = new T[size];
            _lockObject = new object();
        }

        /// <summary>
        /// Write data to the buffer
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="offset">Offset into data</param>
        /// <param name="count">Number of items to write</param>
        /// <returns>number of items written</returns>
        public void Write(T[] data, int offset, int count)
        {
            lock (_lockObject)
            {
                var tsWritten = 0;
                // write to end
                var writeToEnd = Math.Min(_buffer.Length - _writePosition, count);
                Array.Copy(data, offset, _buffer, _writePosition, writeToEnd);
                _writePosition += writeToEnd;
                _writePosition %= _buffer.Length;
                tsWritten += writeToEnd;
                if (tsWritten >= count) return;
                System.Diagnostics.Debug.Assert(_writePosition == 0);
                // must have wrapped round. Write to start
                Array.Copy(data, offset + tsWritten, _buffer, _writePosition, count - tsWritten);
                _writePosition += (count - tsWritten);
            }
        }

        /// <summary>
        /// Write data to the buffer
        /// </summary>
        /// <param name="data">Data to write</param>
        public void Write(in T data)
        {
            lock (_lockObject)
            {
                _writePosition %= _buffer.Length;
                _buffer[_writePosition] = data;
                _writePosition = (_writePosition + 1) % _buffer.Length;
            }
        }

        /// <summary>
        /// Read from the buffer
        /// </summary>
        /// <param name="data">Buffer to read into</param>
        /// <param name="offset">Offset into read buffer</param>
        /// <param name="count">items to read</param>
        public void Read(T[] data, int offset, int count)
        {
            lock (_lockObject)
            {
                var tsRead = 0;
                var readToEnd = Math.Min(_buffer.Length - _readPosition, count);
                Array.Copy(_buffer, _readPosition, data, offset, readToEnd);
                tsRead += readToEnd;
                _readPosition += readToEnd;
                _readPosition %= _buffer.Length;

                // must have wrapped round. Read from start
                System.Diagnostics.Debug.Assert(_readPosition == 0);
                Array.Copy(_buffer, _readPosition, data, offset + tsRead, count - tsRead);
                _readPosition += (count - tsRead);
            }
        }

        /// <summary>
        /// Read from the buffer
        /// </summary>
        /// <param name="data">Data to read</param>
        public void Read(out T data)
        {
            lock (_lockObject)
            {
                data = _buffer[_readPosition];
                _readPosition++;
                _readPosition %= _buffer.Length;
            }
        }

        /// <summary>
        /// Peek from the buffer
        /// </summary>
        /// <param name="data">Buffer to peek into</param>
        /// <param name="offset">Offset into peek buffer</param>
        /// <param name="count">items to read</param>
        public void Peek(T[] data, int offset, int count)
        {
            lock (_lockObject)
            {
                var readPosition = _readPosition;
                var tsRead = 0;
                var readToEnd = Math.Min(_buffer.Length - readPosition, count);
                Array.Copy(_buffer, readPosition, data, offset, readToEnd);
                tsRead += readToEnd;
                readPosition += readToEnd;
                readPosition %= _buffer.Length;

                // must have wrapped round. Read from start
                System.Diagnostics.Debug.Assert(readPosition == 0);
                Array.Copy(_buffer, readPosition, data, offset + tsRead, count - tsRead);
            }
        }
        
        /// <summary>
        /// Peek from the buffer
        /// </summary>
        /// <param name="data">Data to peek</param>
        public void Peek(out T data)
        {
            lock (_lockObject)
            {
                data = _buffer[_readPosition];
            }
        }
        
        /// <summary>
        /// Peek from the buffer
        /// </summary>
        /// <returns>Data to peek</returns>
        public T Peek()
        {
            lock (_lockObject)
            {
                return _buffer[_readPosition];
            }
        }

        /// <summary>
        /// Maximum length of this circular buffer
        /// </summary>
        public int MaxLength => _buffer.Length;

        /// <summary>
        /// Resets the buffer
        /// </summary>
        public void Reset()
        {
            lock (_lockObject)
            {
                ResetInner();
            }
        }

        private void ResetInner()
        {
            _readPosition = 0;
            _writePosition = 0;
        }

        /// <summary>
        /// Advances the buffer, discarding items
        /// </summary>
        /// <param name="count">items to advance</param>
        public void Advance(int count)
        {
            lock (_lockObject)
            {
                _readPosition += count;
                _readPosition %= MaxLength;
            }
        }
    }
}