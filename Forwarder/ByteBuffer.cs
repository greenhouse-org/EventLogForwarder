using System;
using System.IO;
using System.Text;

namespace Forwarder
{
    public class ByteBuffer : TextWriter
    {
        /// <summary>
        /// Scratch space for Write(char)
        /// </summary>
        private char[] scratch = new char[2];

        private byte[] buf;

        /// <summary>
        /// Bytes written to buffer.
        /// </summary>
        private int len;

        public int Length
        {
            get { return len; }
        }

        public int Capacity
        {
            get { return buf.Length; }
        }

        private Encoding encoding;

        public override Encoding Encoding
        {
            get { return encoding; }
        }

        private bool singleByteEncoding;

        /// <summary>
        /// The threshold above which you should use Buffer.BlockCopy rather than ByteArray.Copy
        /// </summary>
        private const int CopyThreshold = 12;

        /// <summary>
        /// Determines which copy routine to use based on the number of bytes to be copied.
        /// </summary>
        internal static void Copy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            if (count > CopyThreshold)
            {
                Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
            }
            else
            {
                int stop = srcOffset + count;
                for (int i = srcOffset; i < stop; i++)
                {
                    dst[dstOffset++] = src[i];
                }
            }
        }

        public void Reset()
        {
            len = 0;
        }

        public void Grow(int n)
        {
            if (len + n > this.buf.Length)
            {
                byte[] buf = new byte[2 * this.buf.Length + n];
                Copy(this.buf, 0, buf, 0, len);
                this.buf = buf;
            }
        }

        public void Write(byte[] bytes)
        {
            Grow(bytes.Length);
            Copy(bytes, 0, buf, len, bytes.Length);
            len += bytes.Length;
        }

        public void Write(byte c)
        {
            Grow(1);
            buf[len++] = c;
        }

        public override void Write(char c)
        {
            if (singleByteEncoding && c < 0x80)
            {
                Grow(1);
                buf[len++] = (byte)c;
            }
            else
            {
                Grow(encoding.GetMaxByteCount(1));
                scratch[0] = c;
                len += encoding.GetBytes(scratch, 0, 1, buf, len);
            }
        }

        public override void Write(char[] chars)
        {
            Write(chars, 0, chars.Length);
        }

        public override void Write(char[] chars, int index, int count)
        {
            Grow(encoding.GetByteCount(chars, index, count));
            len += encoding.GetBytes(chars, index, count, buf, len);
        }

        public override void Write(string str)
        {
            Write(str, encoding);
        }

        public void Write(string str, Encoding encoding)
        {
            Grow(encoding.GetByteCount(str));
            len += encoding.GetBytes(str, 0, str.Length, buf, len);
        }

        public byte[] GetBytes()
        {
            byte[] b = new byte[len];
            Copy(buf, 0, b, 0, len);
            return b;
        }

        public override string ToString()
        {
            return ToString(encoding);
        }

        public string ToString(Encoding encoding)
        {
            return encoding.GetString(buf, 0, len);
        }

        public void WriteTo(Stream outputStream)
        {
            outputStream.Write(buf, 0, len);
        }

        public ByteBuffer()
            : this(Encoding.UTF8, 64)
        {
        }

        public ByteBuffer(int size)
            : this(Encoding.UTF8, size)
        {
        }

        public ByteBuffer(Encoding encoding)
            : this(encoding, 64)
        {
        }

        public ByteBuffer(Encoding encoding, int size)
        {
            if (size < 0)
            {
                throw new ArgumentException("size", "negative value");
            }
            this.encoding = encoding ??
                throw new ArgumentNullException("encoding");

            singleByteEncoding = encoding == Encoding.ASCII ||
                encoding == Encoding.UTF8;

            buf = new byte[size];
        }
    }
}
