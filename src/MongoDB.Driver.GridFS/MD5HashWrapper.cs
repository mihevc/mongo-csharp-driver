using System;
using System.IO;
using System.Security.Cryptography;

namespace MongoDB.Driver.GridFS
{
    public class Md5HashWrapper : IDisposable
    {
        protected int HashSizeValue = 128;
        protected internal byte[] HashValue;
        protected int State;

        private byte[] _rawData = new byte[0];
        private MD5 _hashAlgorithm = null;
        private bool _disposed;

        public virtual int HashSize => HashSizeValue;

        public virtual byte[] Hash
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(null);
                if (State != 0)
                    throw new Exception("Cryptography_HashNotYetFinalized");

                return (byte[])HashValue.Clone();
            }
        }

        public virtual int InputBlockSize => 1;

        public virtual int OutputBlockSize => 1;

        public virtual bool CanTransformMultipleBlocks => true;

        /// <summary>
        /// Gets a value indicating whether the current transform can be reused.
        /// </summary>
        /// 
        /// <returns>
        /// Always true.
        /// </returns>
        public virtual bool CanReuseTransform
        {
            get
            {
                return true;
            }
        }

        public Md5HashWrapper()
        {
            _hashAlgorithm = MD5.Create();
        }

        /// <summary>
        /// Computes the hash value for the specified <see cref="T:System.IO.Stream"/> object.
        /// </summary>
        /// 
        /// <returns>
        /// The computed hash code.
        /// </returns>
        /// <param name="inputStream">The input to compute the hash code for. </param><exception cref="T:System.ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ComputeHash(Stream inputStream)
        {
            if (_disposed)
                throw new ObjectDisposedException(null);

            var numArray1 = new byte[4096];
            int cbSize;
            do
            {
                cbSize = inputStream.Read(numArray1, 0, 4096);
                if (cbSize > 0)
                    HashCore(numArray1, 0, cbSize);
            }
            while (cbSize > 0);

            HashValue = HashFinal();
            var numArray2 = (byte[])HashValue.Clone();

            //this.Initialize();
            return numArray2;
        }

        /// <summary>
        /// Computes the hash value for the specified byte array.
        /// </summary>
        /// 
        /// <returns>
        /// The computed hash code.
        /// </returns>
        /// <param name="buffer">The input to compute the hash code for. </param><exception cref="T:System.ArgumentNullException"><paramref name="buffer"/> is null.</exception><exception cref="T:System.ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ComputeHash(byte[] buffer)
        {
            if (_disposed)
                throw new ObjectDisposedException(null);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            HashCore(buffer, 0, buffer.Length);
            HashValue = HashFinal();
            var numArray = (byte[])HashValue.Clone();
            //this.Initialize();
            return numArray;
        }

        /// <summary>
        /// Computes the hash value for the specified region of the specified byte array.
        /// </summary>
        /// 
        /// <returns>
        /// The computed hash code.
        /// </returns>
        /// <param name="buffer">The input to compute the hash code for. </param><param name="offset">The offset into the byte array from which to begin using data. </param><param name="count">The number of bytes in the array to use as data. </param><exception cref="T:System.ArgumentException"><paramref name="count"/> is an invalid value.-or-<paramref name="buffer"/> length is invalid.</exception><exception cref="T:System.ArgumentNullException"><paramref name="buffer"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset"/> is out of range. This parameter requires a non-negative number.</exception><exception cref="T:System.ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ComputeHash(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "ArgumentOutOfRange_NeedNonNegNum");
            if (count < 0 || count > buffer.Length)
                throw new ArgumentException("Argument_InvalidValue");
            if (buffer.Length - count < offset)
                throw new ArgumentException("Argument_InvalidOffLen");
            if (_disposed)
                throw new ObjectDisposedException(null);
            HashCore(buffer, offset, count);
            HashValue = HashFinal();
            var numArray = (byte[])HashValue.Clone();
            //this.Initialize();
            return numArray;
        }

        /// <summary>
        /// Computes the hash value for the specified region of the input byte array and copies the specified region of the input byte array to the specified region of the output byte array.
        /// </summary>
        /// 
        /// <returns>
        /// The number of bytes written.
        /// </returns>
        /// <param name="inputBuffer">The input to compute the hash code for. </param><param name="inputOffset">The offset into the input byte array from which to begin using data. </param><param name="inputCount">The number of bytes in the input byte array to use as data. </param><param name="outputBuffer">A copy of the part of the input array used to compute the hash code. </param><param name="outputOffset">The offset into the output byte array from which to begin writing data. </param><exception cref="T:System.ArgumentException"><paramref name="inputCount"/> uses an invalid value.-or-<paramref name="inputBuffer"/> has an invalid length.</exception><exception cref="T:System.ArgumentNullException"><paramref name="inputBuffer"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="inputOffset"/> is out of range. This parameter requires a non-negative number.</exception><exception cref="T:System.ObjectDisposedException">The object has already been disposed.</exception>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException(nameof(inputBuffer));
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(inputOffset), "ArgumentOutOfRange_NeedNonNegNum");
            if (inputCount < 0 || inputCount > inputBuffer.Length)
                throw new ArgumentException("Argument_InvalidValue");
            if (inputBuffer.Length - inputCount < inputOffset)
                throw new ArgumentException("Argument_InvalidOffLen");
            if (_disposed)
                throw new ObjectDisposedException(null);
            State = 1;
            HashCore(inputBuffer, inputOffset, inputCount);

            if (inputCount > 0)
            {
                var data = new byte[_rawData.Length + inputCount];
                Array.Copy(_rawData, data, _rawData.Length);
                Array.Copy(inputBuffer, inputOffset, data, _rawData.Length, inputCount);
                _rawData = data;
            }

            return inputCount;
        }

        /// <summary>
        /// Computes the hash value for the specified region of the specified byte array.
        /// </summary>
        /// 
        /// <returns>
        /// An array that is a copy of the part of the input that is hashed.
        /// </returns>
        /// <param name="inputBuffer">The input to compute the hash code for. </param><param name="inputOffset">The offset into the byte array from which to begin using data. </param><param name="inputCount">The number of bytes in the byte array to use as data. </param><exception cref="T:System.ArgumentException"><paramref name="inputCount"/> uses an invalid value.-or-<paramref name="inputBuffer"/> has an invalid offset length.</exception><exception cref="T:System.ArgumentNullException"><paramref name="inputBuffer"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="inputOffset"/> is out of range. This parameter requires a non-negative number.</exception><exception cref="T:System.ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null)
                throw new ArgumentNullException(nameof(inputBuffer));
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(inputOffset), "ArgumentOutOfRange_NeedNonNegNum");
            if (inputCount < 0 || inputCount > inputBuffer.Length)
                throw new ArgumentException("Argument_InvalidValue");
            if (inputBuffer.Length - inputCount < inputOffset)
                throw new ArgumentException("Argument_InvalidOffLen");
            if (_disposed)
                throw new ObjectDisposedException(null);
            HashCore(inputBuffer, inputOffset, inputCount);
            HashValue = HashFinal();
            byte[] numArray;
            if (inputCount != 0)
            {
                numArray = new byte[inputCount];
                Buffer.BlockCopy(inputBuffer, inputOffset, numArray, 0, inputCount);

                var data = new byte[_rawData.Length + inputCount];
                Array.Copy(_rawData, data, _rawData.Length);
                Array.Copy(inputBuffer, inputOffset, data, _rawData.Length, inputCount);
                _rawData = data;
            }
            else
                numArray = new byte[0];

            State = 0;
            return numArray;
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="T:System.Security.Cryptography.HashAlgorithm"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:System.Security.Cryptography.HashAlgorithm"/> class.
        /// </summary>
        public void Clear()
        {
            Dispose();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Security.Cryptography.HashAlgorithm"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            if (HashValue != null)
                Array.Clear(HashValue, 0, HashValue.Length);
            HashValue = null;
            _rawData = null;
            _disposed = true;
        }

        /// <summary>
        /// When overridden in a derived class, routes data written to the object into the hash algorithm for computing the hash.
        /// </summary>
        /// <param name="array">The input to compute the hash code for. </param><param name="ibStart">The offset into the byte array from which to begin using data. </param><param name="cbSize">The number of bytes in the byte array to use as data. </param>
        protected void HashCore(byte[] array, int ibStart, int cbSize)
        {
        }

        /// <summary>
        /// When overridden in a derived class, finalizes the hash computation after the last data is processed by the cryptographic stream object.
        /// </summary>
        /// 
        /// <returns>
        /// The computed hash code.
        /// </returns>
        protected byte[] HashFinal()
        {
            return _hashAlgorithm.ComputeHash(_rawData);
        }
    }
}
