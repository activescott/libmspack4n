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
		private NativeMethods.mscabd_cabinet _nativeCabinet = new NativeMethods.mscabd_cabinet();
		private IntPtr _pNativeCabinet;
		private readonly string _cabinetFilename;
		private IntPtr _pCabinetFilenamePinned;
		private MSCabDecompressor _decompressor;		



		public MSCabinet(string cabinetFilename)
		{
			_cabinetFilename = cabinetFilename;
			_pCabinetFilenamePinned = Marshal.StringToCoTaskMemAnsi(_cabinetFilename);// needs to be pinned as we use the address in unmanaged code.
			_decompressor = MSCabDecompressor.Default;

			// open cabinet:
			_pNativeCabinet = NativeMethods.mspack_invoke_mscab_decompressor_open(Decompressor.Pointer, _pCabinetFilenamePinned);
			if (_pNativeCabinet == IntPtr.Zero)
			{
				var lasterror = NativeMethods.mspack_invoke_mscab_decompressor_last_error(Decompressor.Pointer);
				throw new Exception("Failed to open cabinet. Last error:" + lasterror);
			}
			//Marshal.PtrToStructure(_pNativeCabinet, _nativeCabinet);
			_nativeCabinet = (NativeMethods.mscabd_cabinet) Marshal.PtrToStructure(_pNativeCabinet, typeof (NativeMethods.mscabd_cabinet));
		}

		public string LocalFilePath
		{
			get { return _cabinetFilename; }
		}

		~MSCabinet()
		{
			Close(false);
		}
		
		public void Close(bool isDisposing)
		{
			Debug.Print("Disposing MSCabinet for {0}. isDisposing:{1}", _cabinetFilename, isDisposing);
			if (_pNativeCabinet != IntPtr.Zero)
			{
				//NOTE: Check here that the pointer is still valid. If we're finalizing it might have been finalized before us. In that case WE LEAK!
				if (!_decompressor.IsInvalidState)
				{
					NativeMethods.mspack_invoke_mscab_decompressor_close(_decompressor.Pointer, _pNativeCabinet);
					_pNativeCabinet = IntPtr.Zero;
				}
				else
				{
					//TODO: Find a better way to handle this with multiple instances of MSCabinet using a shared decompressor
					Debug.Fail("Leaking decompressor pointer because of finalization order.");
				}
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
			if (Decompressor.IsInvalidState)
				throw new InvalidOperationException("Decompressor not initialized.");
		}

		internal bool IsInvalidState
		{
			get { return _pNativeCabinet == IntPtr.Zero || Decompressor.IsInvalidState; }
		}

		public MSCabinetFlags Flags
		{
			get
			{
				ThrowOnInvalidState();
				return (MSCabinetFlags)_nativeCabinet.flags;
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

		internal MSCabDecompressor Decompressor
		{
			get { return _decompressor; }
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

		/// <summary>
		/// Appends specified cabinet to this one, forming or extending a cabinet set.
		/// </summary>
		/// <param name="nextCabinet">The cab to append to this one.</param>
		public void Append(MSCabinet nextCabinet)
		{
			var result = NativeMethods.mspack_invoke_mscab_decompressor_append(Decompressor.Pointer, _pNativeCabinet, nextCabinet._pNativeCabinet);
			if (result != NativeMethods.MSPACK_ERR.MSPACK_ERR_OK)
				throw new Exception(string.Format("Error '{0}' appending cab '{1}' to {2}.", result, nextCabinet._cabinetFilename, _cabinetFilename));
			
			// after a successul append remarshal over the nativeCabinet struct5ure as it now represents the combined state.
			_nativeCabinet = (NativeMethods.mscabd_cabinet)Marshal.PtrToStructure(_pNativeCabinet, typeof(NativeMethods.mscabd_cabinet));
			nextCabinet._nativeCabinet = (NativeMethods.mscabd_cabinet)Marshal.PtrToStructure(nextCabinet._pNativeCabinet, typeof(NativeMethods.mscabd_cabinet));
		}
	}

	/// <summary>
	/// Used with <see cref="MSCabinet.Flags"/>
	/// </summary>
	[Flags]
	public enum MSCabinetFlags
	{
		/** Cabinet header flag: cabinet has a predecessor */
		MSCAB_HDR_PREVCAB = 0x01,
		/** Cabinet header flag: cabinet has a successor */
		MSCAB_HDR_NEXTCAB = 0x02,
		/** Cabinet header flag: cabinet has reserved header space */
		MSCAB_HDR_RESV = 0x04
	}
}