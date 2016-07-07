using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace GitGameGUI
{
	public partial class HistoryUserControl : UserControl
	{
		public HistoryUserControl()
		{
			InitializeComponent();
		}

		private void openGitkButton_Click(object sender, RoutedEventArgs e)
		{
			// get gitk path
			string programFilesx86, programFilesx64;
			Tools.GetProgramFilesPath(out programFilesx86, out programFilesx64);

			// open gitk
			var process = new Process();
			process.StartInfo.FileName = programFilesx64 + "\\Git\\cmd\\gitk.exe";
			process.StartInfo.WorkingDirectory = string.Format("{0}", RepoUserControl.repoPath);
			process.StartInfo.Arguments = "";
			process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
			if (!process.Start())
			{
				MessageBox.Show("Failed to start Merge tool (is it installed?)");
				return;
			}

			process.WaitForExit();
			MainWindow.UpdateUI();
		}
	}
}
