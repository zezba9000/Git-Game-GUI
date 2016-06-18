using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitGUI
{
	static class Tools
	{
		public static void GetProgramFilesPath(out string programFilesx86, out string programFilesx64)
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))) programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			else programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			programFilesx64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Replace(" (x86)", "");
		}
	}
}
