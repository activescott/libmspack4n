using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LibMSPackN;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibMSPackNTest
{
    [TestClass]
	public class Basic
	{
        [TestMethod]
        public void ExplodeAllTestCabs()
        {
            IEnumerable<string> files = Directory.GetFiles(AppPath, "*.cab");
            foreach (var f in files)
                ExplodeCab(f);
        }

        [TestMethod,Ignore]//This has never worked. Not sure why.
        public void CabinetSet()
        {
			//NO WORKY!
			var root = Path.Combine(AppPath, "cabinetset");
			var cabFilename1  = Path.Combine(root, "Disk1.CAB");
			var cabFilename2 = Path.Combine(root, "Disk2.CAB");
			
			using (var cab1 = new MSCabinet(cabFilename1))
			using (var cab2 = new MSCabinet(cabFilename2))
			{
				cab1.Append(cab2);
				ExplodeCab(cab1, OutDir(cabFilename1));
			}
			Console.ReadLine();
		}

		private static void ExplodeCab(string cabinetFilename)
		{
			Console.WriteLine("Files in " + cabinetFilename + ":");
			try
			{
				using (var cab = new LibMSPackN.MSCabinet(cabinetFilename))
				{
					var outDir = OutDir(cabinetFilename);
					ExplodeCab(cab, outDir);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("ERROR! " + e.Message);
			}
			Console.WriteLine("");
			Console.WriteLine("");
		}

		private static string OutDir(string cabinetFilename)
		{
			var outDir = Path.Combine(AppPath, "test_output");
			outDir = Path.Combine(outDir, Path.GetFileName(cabinetFilename).Replace(".cab", "_cab"));
			return outDir;
		}

		private static void ExplodeCab(MSCabinet cab, string outDir)
		{
			Directory.CreateDirectory(outDir);
			foreach (var file in cab.GetFiles())
			{
				var extractPath = Path.Combine(outDir, file.Filename);
				Console.WriteLine(" Extracting {0} to {1}.", file.Filename, extractPath);
				file.ExtractTo(extractPath);
			}
		}

		protected static string AppPath
		{
			get
			{
				string path = Assembly.GetExecutingAssembly().Location;
				path = Path.GetDirectoryName(path);
				return path;
			}
		}
	}
}
