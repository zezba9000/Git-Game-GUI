using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		public static bool IsTextFileType(string filename)
		{
			string ext = System.IO.Path.GetExtension(filename);
			switch (ext)
			{
				// word docs
				case ".txt":
				case ".rtf":
				case ".doc":
				case ".docx":

				// code docs
				case ".cs":
				case ".cpp":
				case ".c":
				case ".hpp":
				case ".h":
				case ".js":

				// mark up docs
				case ".xml":
				case ".html":
				case ".htm":
				case ".css":
					return true;
			}

			return false;
		}

		public static bool IsBinaryFileType(string filename)
		{
			string ext = System.IO.Path.GetExtension(filename);
			switch (ext)
			{
				case ".zip":
				case ".7z":
				case ".rar":
				case ".bin":
				case ".hex":
				case ".raw":
				case ".data":
					return true;
			}

			return false;
		}

		public static void RunCmd(string operation, string workingDirectory)
		{
			var process = new Process();
			process.StartInfo.FileName = "cmd";
			process.StartInfo.WorkingDirectory = workingDirectory;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.Start();
			process.StandardInput.WriteLine(operation);
			process.StandardInput.Flush();
			process.StandardInput.Close();
			process.WaitForExit();
		}

		public static bool IsSingleWord(string value)
		{
			foreach (char c in value)
			{
				switch (c)
				{
					case 'a':
					case 'b':
					case 'c':
					case 'd':
					case 'e':
					case 'f':
					case 'g':
					case 'h':
					case 'i':
					case 'j':
					case 'k':
					case 'l':
					case 'm':
					case 'n':
					case 'o':
					case 'p':
					case 'q':
					case 'r':
					case 's':
					case 't':
					case 'u':
					case 'v':
					case 'w':
					case 'x':
					case 'y':
					case 'z':

					case 'A':
					case 'B':
					case 'C':
					case 'D':
					case 'E':
					case 'F':
					case 'G':
					case 'H':
					case 'I':
					case 'J':
					case 'K':
					case 'L':
					case 'M':
					case 'N':
					case 'O':
					case 'P':
					case 'Q':
					case 'R':
					case 'S':
					case 'T':
					case 'U':
					case 'V':
					case 'W':
					case 'X':
					case 'Y':
					case 'Z':
					
					continue;
					default: return false;
				}
			}

			return true;
		}
	}
}
