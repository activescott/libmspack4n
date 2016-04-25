using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

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
			string longDestinationFilename = longFilename(destinationFilename);
			try
			{
				pDestinationFilename = Marshal.StringToCoTaskMemAnsi(longDestinationFilename);
				var result = NativeMethods.mspack_invoke_mscab_decompressor_extract(_parentCabinet.Decompressor, _pNativeFile, pDestinationFilename);
				if (result != NativeMethods.MSPACK_ERR.MSPACK_ERR_OK)
					throw new Exception(string.Format("Error '{0}' extracting file to {1}.", result, longDestinationFilename));

				// Long-form filenames are not supported by the .NET system libraries,
				// so we must do win32 calls to set file time-stamps and attributes.
				SafeFileHandle h = NativeMethods.CreateFile(
					longDestinationFilename,
					NativeMethods.FileAccess.FILE_READ_ATTRIBUTES | NativeMethods.FileAccess.FILE_WRITE_ATTRIBUTES,
					NativeMethods.FileShare.FILE_SHARE_READ | NativeMethods.FileShare.FILE_SHARE_WRITE | NativeMethods.FileShare.FILE_SHARE_DELETE,
					IntPtr.Zero,
					NativeMethods.CreationDisposition.OPEN_EXISTING,
					NativeMethods.FileAttributes.FILE_ATTRIBUTE_NORMAL | NativeMethods.FileAttributes.FILE_FLAG_BACKUP_SEMANTICS,
					IntPtr.Zero);
				if (h.IsInvalid)
					throw new Exception(string.Format("Error {0} opening {1}.", Marshal.GetLastWin32Error(), longDestinationFilename));
				using (h)
				{
					var modifiedTime = GetModifiedTime().ToFileTime();
					if (!NativeMethods.SetFileTime(
							h.DangerousGetHandle(),
							ref modifiedTime, IntPtr.Zero, ref modifiedTime))
						throw new Exception(string.Format("Error {0} setting times for {1}.", Marshal.GetLastWin32Error(), longDestinationFilename));
				}
				var theAttributes = NativeMethods.GetFileAttributes(longDestinationFilename);
				if (theAttributes == (uint)NativeMethods.FileAttributes.INVALID_FILE_ATTRIBUTES)
					throw new Exception(string.Format("Error {0} getting attributes of {1}.", Marshal.GetLastWin32Error(), longDestinationFilename));
	
				if ((_nativeFile.attribs & NativeMethods.mscabd_file_attribs.MSCAB_ATTRIB_ARCH) == NativeMethods.mscabd_file_attribs.MSCAB_ATTRIB_ARCH)
					theAttributes |= (uint)FileAttributes.Archive;
				if ((_nativeFile.attribs & NativeMethods.mscabd_file_attribs.MSCAB_ATTRIB_HIDDEN) == NativeMethods.mscabd_file_attribs.MSCAB_ATTRIB_HIDDEN)
					theAttributes |= (uint)FileAttributes.Hidden;
				if ((_nativeFile.attribs & NativeMethods.mscabd_file_attribs.MSCAB_ATTRIB_RDONLY) == NativeMethods.mscabd_file_attribs.MSCAB_ATTRIB_RDONLY)
					theAttributes |= (uint)FileAttributes.ReadOnly;
				if ((_nativeFile.attribs & NativeMethods.mscabd_file_attribs.MSCAB_ATTRIB_SYSTEM) == NativeMethods.mscabd_file_attribs.MSCAB_ATTRIB_SYSTEM)
					theAttributes |= (uint)FileAttributes.System;
				if (!NativeMethods.SetFileAttributes(longDestinationFilename, theAttributes))
					throw new Exception(string.Format("Error {0} setting attributes of {1}.", Marshal.GetLastWin32Error(), longDestinationFilename));
			}
			finally
			{
				if (pDestinationFilename != IntPtr.Zero)
					Marshal.FreeCoTaskMem(pDestinationFilename);
			}
		}

		// Convert a filename (assumed to be absolute) to the "extended" form
		// that permits it to be longer than 260 characters.
		private string longFilename(string filename)
		{
			if (filename.StartsWith("\\\\?\\"))
				return filename;		// Already on long form.
			else if (filename.StartsWith("\\\\"))
				return "\\\\?\\UNC\\" + filename.Substring(2);
			else
				return "\\\\?\\" + filename;
		}

		private DateTime GetModifiedTime()
		{
			return new DateTime(_nativeFile.date_y, _nativeFile.date_m, _nativeFile.date_d, _nativeFile.time_h, _nativeFile.time_m, _nativeFile.time_s);
		}
	}
}