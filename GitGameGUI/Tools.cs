using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitGameGUI
{
	static class Tools
	{
		public static void GetProgramFilesPath(out string programFilesx86, out string programFilesx64)
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))) programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			else programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			programFilesx64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Replace(" (x86)", "");
		}

		public static bool IsBinaryFileType(string filename)
		{
			string ext = System.IO.Path.GetExtension(filename);
			switch (ext)
			{
				case ".zip":
					return true;
			}

			return false;
		}
	}
}
