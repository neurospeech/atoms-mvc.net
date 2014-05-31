using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NeuroSpeech.Atoms.Mvc
{
    /// <summary>
    /// A semi-generic Stream implementation for Response.Filter with
    /// an event interface for handling Content transformations via
    /// Stream or String.    
    /// <remarks>
    /// Use with care for large output as this implementation copies
    /// the output into a memory stream and so increases memory usage.
    /// </remarks>
    /// </summary>    
    public class ResponseFilterStream : Stream
    {
        /// <summary>
        /// The original stream
        /// </summary>
        Stream _stream;


        /// <summary>
        /// Stream that original content is read into
        /// and then passed to TransformStream function
        /// </summary>
        MemoryStream _cacheStream = new MemoryStream(5000);

        public byte[] Buffer {
            get {
                return _cacheStream.ToArray();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="responseStream"></param>
        public ResponseFilterStream(Stream responseStream)
        {
            _stream = responseStream;
        }


        /// <summary>
        /// 
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }
        /// <summary>
        /// 
        /// </summary>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// 
        /// </summary>
        public override long Length
        {
            get { return 0; }
        }

        /// <summary>
        /// 
        /// </summary>
        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public override long Seek(long offset, System.IO.SeekOrigin direction)
        {
            return _stream.Seek(offset, direction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="length"></param>
        public override void SetLength(long length)
        {
            _stream.SetLength(length);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Close()
        {
            _stream.Close();
            if (Closed != null) {
                Closed();
            }
        }

        public Action Closed { get; set; }

        /// <summary>
        /// Override flush by writing out the cached stream data
        /// </summary>
        public override void Flush()
        {

            // default flush behavior
            _stream.Flush();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }


        /// <summary>
        /// Overriden to capture output written by ASP.NET and captured
        /// into a cached stream that is written out later when Flush()
        /// is called.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _cacheStream.Write(buffer, offset, count);
            _stream.Write(buffer, offset, count);

        }

    }
}
