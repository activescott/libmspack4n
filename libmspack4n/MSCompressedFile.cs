using System;
using System.Runtime.InteropServices;

namespace LibMSPackN
{
	/// <summary>
	/// Represents a file contained inside of a cab. Returned from <see cref="MSCabinet.GetFiles()"/>.
	/// </summary>
	public sealed class MSCompressedFile
	{
		private readonly MSCabinet _parentCabinet;
		private readonly NativeMethods.mscabd_file _nativeFile;
		private readonly IntPtr _pNativeFile;

		internal MSCompressedFile(MSCabinet parentCabinet, IntPtr pNativeFile)
		{
			//NOTE: we don't need to explicitly clean the nativeFile up. It is cleaned up by the parent cabinet.
			_parentCabinet = parentCabinet;
			_pNativeFile = pNativeFile;
			_nativeFile = (NativeMethods.mscabd_file)Marshal.PtrToStructure(_pNativeFile, typeof (NativeMethods.mscabd_file));
		}

		private void ThrowOnInvalidState()
		{
			if (_parentCabinet.IsInvalidState)
				throw new InvalidOperationException("Parent cabinet is no longer in a valid state. Ensure it was not disposed.");
			if (_pNativeFile == IntPtr.Zero)
				throw new InvalidOperationException("Native file is not initialized.");
		}

		public string Filename
		{
			get 
			{ 
				ThrowOnInvalidState();
				return _nativeFile.filename;
			}
		}

		public uint LengthInBytes 
		{
			get
			{
				ThrowOnInvalidState();
				return _nativeFile.length;
			}
		}

		public MSCompressedFile Next
		{
			get
			{
				MSCompressedFile next;
				if (_nativeFile.next != IntPtr.Zero)
					next = new MSCompressedFile(_parentCabinet, _nativeFile.next);
				else
					next = null;
				return next;
			}
		}

		public void ExtractTo(string destinationFilename)
		{
			ThrowOnInvalidState();
			IntPtr pDestinationFilename = IntPtr.Zero;
			try
			{
				pDestinationFilename = Marshal.StringToCoTaskMemAnsi(destinationFilename);
				var result = NativeMethods.mspack_invoke_mscab_decompressor_extract(_parentCabinet.Decompressor.Pointer, _pNativeFile, pDestinationFilename);
				if (result != NativeMethods.MSPACK_ERR.MSPACK_ERR_OK)
					throw new Exception(string.Format("Error '{0}' extracting file to {1}.", result, destinationFilename));
			}
			finally
			{
				if (pDestinationFilename != IntPtr.Zero)
					Marshal.FreeCoTaskMem(pDestinationFilename);
			}
		}
	}
}