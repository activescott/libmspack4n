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
	public sealed class MsCabinet : IDisposable
	{	
		private NativeMethods.mscabd_cabinet _nativeCabinet = new NativeMethods.mscabd_cabinet();
		private IntPtr _pNativeCabinet;
	    private IntPtr _pCabinetFilenamePinned;

	    public MsCabinet(string cabinetFilename)
		{
			LocalFilePath = cabinetFilename;
			_pCabinetFilenamePinned = Marshal.StringToCoTaskMemAnsi(LocalFilePath);// needs to be pinned as we use the address in unmanaged code.
			Decompressor = MsCabDecompressor.CreateInstance();

			// open cabinet:
			_pNativeCabinet = NativeMethods.mspack_invoke_mscab_decompressor_open(Decompressor, _pCabinetFilenamePinned);
			if (_pNativeCabinet == IntPtr.Zero)
			{
				var lasterror = NativeMethods.mspack_invoke_mscab_decompressor_last_error(Decompressor);
				throw new Exception("Failed to open cabinet. Last error:" + lasterror);
			}
			//Marshal.PtrToStructure(_pNativeCabinet, _nativeCabinet);
			_nativeCabinet = (NativeMethods.mscabd_cabinet) Marshal.PtrToStructure(_pNativeCabinet, typeof (NativeMethods.mscabd_cabinet));
		}

	    private string LocalFilePath { get; set; }

	    ~MsCabinet()
		{
			Close(false);
		}

	    private void Close(bool isDisposing)
		{
			Debug.Print("Disposing MSCabinet for {0}. isDisposing:{1}", LocalFilePath, isDisposing);
			if (_pNativeCabinet != IntPtr.Zero)
			{
				NativeMethods.mspack_invoke_mscab_decompressor_close(Decompressor, _pNativeCabinet);
				_pNativeCabinet = IntPtr.Zero;
			}

			if (Decompressor != IntPtr.Zero)
			{
				MsCabDecompressor.DestroyInstance(Decompressor);
				Decompressor = IntPtr.Zero;
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
			if (_pNativeCabinet == IntPtr.Zero)
				throw new InvalidOperationException("Cabinet not initialized.");
			if (Decompressor == IntPtr.Zero)
				throw new InvalidOperationException("Decompressor not initialized.");
		}

		internal bool IsInvalidState
		{
			get { return _pNativeCabinet == IntPtr.Zero || Decompressor == IntPtr.Zero; }
		}

		public MsCabinetFlags Flags
		{
			get
			{
				ThrowOnInvalidState();
				return (MsCabinetFlags)_nativeCabinet.flags;
			}
		}

		public string PrevName
		{
			get
			{
				ThrowOnInvalidState();
				return _nativeCabinet.prevname;
			}
		}

		public string NextName
		{
			get
			{
				ThrowOnInvalidState();
				return _nativeCabinet.nextname;
			}
		}

	    internal IntPtr Decompressor { get; private set; }

	    public IEnumerable<MsCompressedFile> GetFiles()
		{
			ThrowOnInvalidState();
			
			var pNextFile = _nativeCabinet.files;
	        var containedFile = pNextFile != IntPtr.Zero ? new MsCompressedFile(this, pNextFile) : null;

			while (containedFile != null)
			{
				yield return containedFile;
				containedFile = containedFile.Next;
			}
		}

		/// <summary>
		/// Appends specified cabinet to this one, forming or extending a cabinet set.
		/// </summary>
		/// <param name="nextCabinet">The cab to append to this one.</param>
		public void Append(MsCabinet nextCabinet)
		{
			var result = NativeMethods.mspack_invoke_mscab_decompressor_append(Decompressor, _pNativeCabinet, nextCabinet._pNativeCabinet);
			if (result != NativeMethods.MSPACK_ERR.MSPACK_ERR_OK)
				throw new Exception(string.Format("Error '{0}' appending cab '{1}' to {2}.", result, nextCabinet.LocalFilePath, LocalFilePath));
			
			// after a successul append remarshal over the nativeCabinet struct5ure as it now represents the combined state.
			_nativeCabinet = (NativeMethods.mscabd_cabinet)Marshal.PtrToStructure(_pNativeCabinet, typeof(NativeMethods.mscabd_cabinet));
			nextCabinet._nativeCabinet = (NativeMethods.mscabd_cabinet)Marshal.PtrToStructure(nextCabinet._pNativeCabinet, typeof(NativeMethods.mscabd_cabinet));
		}
	}
}