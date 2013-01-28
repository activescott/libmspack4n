using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LibMSPackNTest
{
	class Program
	{
		static void Main()
		{
			IEnumerable<string> files = Directory.GetFiles(AppPath, "*.cab");
			files = files.Reverse();
			foreach (var f in files)
				ExplodeCab(f);
		}

		private static void ExplodeCab(string cabinetFilename)
		{
			Console.WriteLine("Files in " + cabinetFilename + ":");
			try
			{
				using (var cab = new LibMSPackN.MSCabinet(cabinetFilename))
				{
					var outDir = Path.Combine(AppPath, "test_output");
					outDir = Path.Combine(outDir, Path.GetFileName(cabinetFilename).Replace(".cab", "_cab"));
					Directory.CreateDirectory(outDir);
					foreach (var file in cab.GetFiles())
					{
						var extractPath = Path.Combine(outDir, file.Filename);
						Console.WriteLine(" Extracting {0} to {1}.", file.Filename, extractPath);
						file.ExtractTo(extractPath);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("ERROR! " + e.Message);
			}
			Console.WriteLine("");
			Console.WriteLine("");
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
