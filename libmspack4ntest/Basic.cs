using System;
using System.IO;
using System.Linq;
using LibMSPackN;
using Xunit;

namespace LibMSPackNTest
{
	public class Basic
	{
        [Theory]
		[InlineData("normal_2files_1folder.cab", 2)]
        public void ExplodeTestCabs(string filename, int expectedFileCount)
        {
			string path = Path.Combine(AppContext.BaseDirectory, "test_files", filename);
			ExplodeAndVerifyCab(path, expectedFileCount);
		}

        [Fact(Skip = "Test cabinet files cannot be opened properly")]
        public void CabinetSet()
        {
			string root = Path.Combine(AppContext.BaseDirectory, "test_files", "cabinetset");
			string cabFilename1  = Path.Combine(root, "Disk1.CAB");
			string cabFilename2 = Path.Combine(root, "Disk2.CAB");

			Assert.True(File.Exists(cabFilename1));
			Assert.True(File.Exists(cabFilename2));

			using (var cab1 = new MSCabinet(cabFilename1))
			using (var cab2 = new MSCabinet(cabFilename2))
			{
				cab1.Append(cab2);
				ExplodeCab(cab1, OutDir(cabFilename1));
			}
		}

		private static void ExplodeAndVerifyCab(string cabinetFilename, int fileCount)
		{
			Console.WriteLine("Files in " + cabinetFilename + ":");

			bool errors = false;
			string outDir = OutDir(cabinetFilename);

			try
			{
				Assert.True(File.Exists(cabinetFilename));

				using (var cab = new MSCabinet(cabinetFilename))
				{
					ExplodeCab(cab, outDir);
				}
			}
			catch (Exception e)
			{
				errors = true;
				Console.WriteLine("ERROR! " + e.Message);
			}

			Assert.False(errors);

			Assert.True(Directory.Exists(outDir));

			var outputFiles = Directory.GetFiles(outDir, "*", SearchOption.AllDirectories);
			Assert.Equal(fileCount, outputFiles.Count());

			Directory.Delete(outDir, true);

			Console.WriteLine("");
			Console.WriteLine("");
		}

		private static string OutDir(string cabinetFilename)
		{
			string outDir = Path.Combine(AppContext.BaseDirectory, "test_output");
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
	}
}
