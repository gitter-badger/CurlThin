//
// Easy.cs
//
// Author:
//   Aaron Bockover <aaron@abock.org>
//
// Copyright 2014 Aaron Bockover
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;
using System.Text;
using CurlThin.Enums;

namespace CurlThin
{
	public enum HeaderKind
	{
		Status,
		KeyValue,
		BodyDelimiter
	}

	public delegate void DataHandler (byte [] data);
	public delegate void HeaderHandler (HeaderKind kind, string name, string value);

	public class CurlEasy : IDisposable
	{
	    internal IntPtr Handle { get; private set; }

	    public CurlEasy ()
		{
			Handle = CurlNative.Easy.Init ();
		}

		~CurlEasy ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (Handle != IntPtr.Zero) {
			    CurlNative.Easy.Cleanup (Handle);
				Handle = IntPtr.Zero;
			}
		}

	    private void CheckDisposed ()
		{
			if (Handle == IntPtr.Zero)
				throw new ObjectDisposedException (nameof(CurlEasy));
		}

		public void Perform ()
		{
			CheckDisposed ();
			var code = CurlNative.Easy.Perform (Handle);
			if (code != CURLcode.OK)
				throw new CurlException (code);
		}

		internal void SetOpt (CURLoption option, int value)
		{
			CheckDisposed ();
			var code = CurlNative.Easy.SetOpt (Handle, option, value);
			if (code != CURLcode.OK)
				throw new CurlException (code);
		}

		internal void SetOpt (CURLoption option, string value)
		{
			CheckDisposed ();
			var code = CurlNative.Easy.SetOpt (Handle, option, value);
			if (code != CURLcode.OK)
				throw new CurlException (code);
		}

		internal void SetOpt (CURLoption option, IntPtr value)
		{
			CheckDisposed ();
			var code = CurlNative.Easy.SetOpt (Handle, option, value);
			if (code != CURLcode.OK)
				throw new CurlException (code);
		}

		internal void SetOpt (CURLoption option, CurlNative.Easy.DataHandler value)
		{
			CheckDisposed ();
			var code = CurlNative.Easy.SetOpt (Handle, option, value);
			if (code != CURLcode.OK)
				throw new CurlException (code);
		}

		internal int GetInfoInt32 (CURLINFO info)
		{
			CheckDisposed ();
            var code = CurlNative.Easy.GetInfo(Handle, info, out int value);
            if (code != CURLcode.OK)
				throw new CurlException (code);
			return value;
		}

		internal IntPtr GetInfoIntPtr (CURLINFO info)
		{
			CheckDisposed ();
            var code = CurlNative.Easy.GetInfo(Handle, info, out IntPtr value);
            if (code != CURLcode.OK)
				throw new CurlException (code);
			return value;
		}

		internal double GetInfoDouble (CURLINFO info)
		{
			CheckDisposed ();
            var code = CurlNative.Easy.GetInfo(Handle, info, out double value);
            if (code != CURLcode.OK)
				throw new CurlException (code);
			return value;
		}

	    private UIntPtr NativeWriteHandler (IntPtr data, UIntPtr size, UIntPtr nmemb, IntPtr userdata)
		{
			var length = (int)size * (int)nmemb;

			if (dataHandler != null) {
				var buffer = new byte [length];
				Marshal.Copy (data, buffer, 0, length);
				dataHandler (buffer);
			}

			return (UIntPtr)length;
		}

	    private UIntPtr NativeHeaderHandler (IntPtr data, UIntPtr size, UIntPtr nmemb, IntPtr userdata)
		{
			var length = (int)size * (int)nmemb;

			if (headerHandler != null) {
				var buffer = new byte [length];
				Marshal.Copy (data, buffer, 0, length);
				ParseHeader (Encoding.UTF8.GetString (buffer), headerHandler);
			}

			return (UIntPtr)length;
		}

	    private void ParseHeader (string header, HeaderHandler handler)
		{
			if (handler == null)
				throw new ArgumentNullException (nameof(handler));

			if (header == null) {
				return;
			}
		    if (header == "\r\n") {
		        handler (HeaderKind.BodyDelimiter, null, null);
		        return;
		    }

		    header = header.TrimEnd ('\r', '\n');
			var colonOffset = header.IndexOf (':');

			if (header.StartsWith ("HTTP/") && colonOffset < 0) {
				handler (HeaderKind.Status, header, null);
			} else if (colonOffset > 0) {
				handler (HeaderKind.KeyValue,
					header.Substring (0, colonOffset).Trim (),
					header.Substring (colonOffset + 1).Trim ()
				);
			}
		}

	    private string url;
		public string Url {
			get => url;
		    set { SetOpt (CURLoption.URL, value); url = value; }
		}

	    private DataHandler dataHandler;
		public DataHandler WriteHandler {
			get => dataHandler;
		    set {
				dataHandler = value;
				if (value == null)
					SetOpt (CURLoption.WRITEFUNCTION, IntPtr.Zero);
				else
					SetOpt (CURLoption.WRITEFUNCTION, NativeWriteHandler);
			}
		}

	    private HeaderHandler headerHandler;
		public HeaderHandler HeaderHandler {
			get => headerHandler;
		    set {
				headerHandler = value;
				if (value == null)
					SetOpt (CURLoption.HEADERFUNCTION, IntPtr.Zero);
				else
					SetOpt (CURLoption.HEADERFUNCTION, NativeHeaderHandler);
			}
		}

	    private CURLPROTO protocols;
		public CURLPROTO Protocols {
			get => protocols;
		    set {
				SetOpt (CURLoption.PROTOCOLS, (int)value);
				protocols = value;
			}
		}

	    private bool followLocation;
		public bool FollowLocation {
			get => followLocation;
		    set {
				SetOpt (CURLoption.FOLLOWLOCATION, value ? 1 : 0);
				followLocation = value;
			}
		}

	    private bool autoReferer;
		public bool AutoReferer {
			get => autoReferer;
		    set {
				SetOpt (CURLoption.AUTOREFERER, value ? 1 : 0);
				autoReferer = value;
			}
		}

	    private Version httpVersion;
		public Version HttpVersion {
			get => httpVersion;
		    set {
				if (value.Major == 1 && (value.Minor == 0 || value.Minor == 1)) {
					SetOpt (CURLoption.HTTP_VERSION, value.Minor + 1);
					httpVersion = value;
				} else {
					SetOpt (CURLoption.HTTP_VERSION, 0);
					httpVersion = default (Version);
				}
			}
		}

	    private string httpMethod;
		public string HttpMethod {
			get => httpMethod;
		    set {
				var m = (value ?? "GET").ToUpper ();

				switch (m) {
				case "GET":
					SetOpt (CURLoption.HTTPGET, 1);
					break;
				case "POST":
					SetOpt (CURLoption.HTTPPOST, 1);
					break;
				case "PUT":
					SetOpt (CURLoption.PUT, 1);
					break;
				default:
					throw new CurlException ("Unsupported HTTP method: " + m);
				}

				httpMethod = m;
			}
		}

		public int ResponseCode => GetInfoInt32 (CURLINFO.RESPONSE_CODE);
	}
}