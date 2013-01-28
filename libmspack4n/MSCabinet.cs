using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LibMSPackN
{
	/// <summary>
	/// Represents a .cab file.
	/// </summary>
	/// <remarks>
	/// Example usage:
	/// <code>
	/// const string cabinetFilename = @"c:\somecab.cab";
	/// using (var cabinet = new MSCabinet(cabinetFilename))
	/// {
	/// 	var outputDir = Path.Combine(Assembly.GetExecutingAssembly().Location, Path.GetFileNameWithoutExtension(cabinetFilename));
	/// 	foreach (MSCabFile containedFile in cabinet.GetFiles())
	/// 	{
	/// 		Debug.Print(containedFile.Filename);
	/// 		Debug.Print(containedFile.LengthInBytes.ToString(CultureInfo.InvariantCulture));
	/// 		containedFile.ExtractTo(Path.Combine(outputDir, containedFile.Filename));
	/// 	}
	/// }
	/// </code>
	/// </remarks>
	public sealed class MSCabinet : IDisposable
	{
		private IntPtr _pDecompressor;
		private readonly NativeMethods.mscabd_cabinet _nativeCabinet = new NativeMethods.mscabd_cabinet();
		private IntPtr _pNativeCabinet;
		private readonly string _cabinetFilename;
		private IntPtr _pCabinetFilenamePinned;

		public MSCabinet(string cabinetFilename)
		{
			_cabinetFilename = cabinetFilename;
			_pCabinetFilenamePinned = Marshal.StringToCoTaskMemAnsi(_cabinetFilename);// needs to be pinned as we use the address in unmanaged code.
			// init decompressor: Note: this is a bit overkill to create a decompressor for every cabinet, but it seems to be the only safe way to be able to properly finalize both the decompressor and the cab. I also presume the cost of creating new decompressors is very small.
			_pDecompressor = NativeMethods.mspack_create_cab_decompressor(IntPtr.Zero);
			if (_pDecompressor == IntPtr.Zero)
				throw new Exception("Failed to create cab_decompressor.");
			
			// open cabinet:
			_pNativeCabinet = NativeMethods.mspack_invoke_mscab_decompressor_open(_pDecompressor, _pCabinetFilenamePinned);
			if (_pNativeCabinet == IntPtr.Zero)
			{
				var lasterror = NativeMethods.mspack_invoke_mscab_decompressor_last_error(_pDecompressor);
				throw new Exception("Failed to open cabinet. Last error:" + lasterror);
			}
			//Marshal.PtrToStructure(_pNativeCabinet, _nativeCabinet);
			_nativeCabinet = (NativeMethods.mscabd_cabinet) Marshal.PtrToStructure(_pNativeCabinet, typeof (NativeMethods.mscabd_cabinet));
		}

		~MSCabinet()
		{
			Close(false);
		}

		public void Close(bool isDisposing)
		{
			Debug.Print("Disposing forMSCabinet for {0}. isDisposing:{1}", _cabinetFilename, isDisposing);
			if (isDisposing)
			{
				// no managed GC objects to cleanup
			}
			if (_pNativeCabinet != IntPtr.Zero)
			{
				Debug.Assert(_pDecompressor != IntPtr.Zero, "Decompressor already destroyed?");
				NativeMethods.mspack_invoke_mscab_decompressor_close(_pDecompressor, _pNativeCabinet);
				_pNativeCabinet = IntPtr.Zero;
			}
			if (_pDecompressor != IntPtr.Zero)
			{
				NativeMethods.mspack_destroy_cab_decompressor(_pDecompressor);
				_pDecompressor = IntPtr.Zero;
			}
			if (_pCabinetFilenamePinned!= IntPtr.Zero)
			{
				Marshal.FreeCoTaskMem(_pCabinetFilenamePinned);
				_pCabinetFilenamePinned = IntPtr.Zero;
			}
			GC.SuppressFinalize(this);
		}

		void IDisposable.Dispose()
		{
			Close(true);
		}

		[DebuggerStepThrough, DebuggerHidden]
		private void ThrowOnInvalidState()
		{
			if (_pNativeCabinet == null)
				throw new InvalidOperationException("Cabinet not initialized.");
			if (_pDecompressor == IntPtr.Zero)
				throw new InvalidOperationException("Decompressor not initialized.");
		}

		internal bool IsInvalidState
		{
			get { return _pNativeCabinet == IntPtr.Zero || _pDecompressor == IntPtr.Zero; }
		}

		internal IntPtr Decompressor
		{
			get
			{
				ThrowOnInvalidState();
				return _pDecompressor;
			}
		}

		public IEnumerable<MSCompressedFile> GetFiles()
		{
			ThrowOnInvalidState();
			
			IntPtr pNextFile = _nativeCabinet.files;
			MSCompressedFile containedFile;
			if (pNextFile != IntPtr.Zero)
				containedFile = new MSCompressedFile(this, pNextFile);
			else
				containedFile = null;

			while (containedFile != null)
			{
				yield return containedFile;
				containedFile = containedFile.Next;
			}
		}
	}
}