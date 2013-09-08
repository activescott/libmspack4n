using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LibMSPackN;

namespace LibMSPackNTest
{
	class Program
	{
		static void Main()
		{
			IEnumerable<string> files = Directory.GetFiles(AppPath, "*.cab");
			foreach (var f in files)
				ExplodeCab(f);

			//NO WORKY!
			var root = Path.Combine(AppPath, "cabinetset");
			var cabFilename1  = Path.Combine(root, "Disk1.CAB");
			var cabFilename2 = Path.Combine(root, "Disk2.CAB");
			
			using (var cab1 = new MsCabinet(cabFilename1))
			using (var cab2 = new MsCabinet(cabFilename2))
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
				using (var cab = new MsCabinet(cabinetFilename))
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
		    var fileName = Path.GetFileName(cabinetFilename);
		    if (fileName != null)
		        outDir = Path.Combine(outDir, fileName.Replace(".cab", "_cab"));
		    return outDir;
		}

		private static void ExplodeCab(MsCabinet cab, string outDir)
		{
			string cabinetFilename;
			
			Directory.CreateDirectory(outDir);
			foreach (var file in cab.GetFiles())
			{
				var extractPath = Path.Combine(outDir, file.Filename);
				Console.WriteLine(" Extracting {0} to {1}.", file.Filename, extractPath);
				file.ExtractTo(extractPath);
			}
		}

	    private static string AppPath
		{
			get
			{
				var path = Assembly.GetExecutingAssembly().Location;
				path = Path.GetDirectoryName(path);
				return path;
			}
		}
	}
}
