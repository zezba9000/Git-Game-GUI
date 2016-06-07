using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GitGUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		const string repoPath = @"D:\Dev\TEMP\Test";
		const string url = "http://10.0.0.44/zezba9000/TestProj.git";

		public MainWindow()
		{
			InitializeComponent();

			var repo = new Repository (repoPath);
            foreach (var item in repo.RetrieveStatus()) {
                if (item.State == FileStatus.ModifiedInWorkdir) {
                    var patch = repo.Diff.Compare<Patch> (new List<string>() { item.FilePath });
                    //Console.WriteLine ("~~~~ Patch file ~~~~");
                    //Console.WriteLine (patch.Content);
					diffTextBlock.Text = patch.Content;
                }
            }
		}

		private void cloneButton_Click(object sender, RoutedEventArgs e)
		{
			Repository.Clone(url, repoPath);
		}
		
		private void getChangedButton_Click(object sender, RoutedEventArgs e)
		{
			diffTextBlock.Text = "";
			using (var repo = new Repository(repoPath))
			{
				var repoStatus = repo.RetrieveStatus();
				bool changesFound = false;
				foreach (var fileStatus in repoStatus.Modified)
				{
					var tree = repo.ObjectDatabase.CreateBlob(fileStatus.FilePath);

					changesFound = true;
					Console.WriteLine("STATUS: " + fileStatus.State);
					changesListBox.Items.Add(fileStatus.FilePath);
					break;
				}

				if (!changesFound) Console.WriteLine("No Changes");
			}
		}

		private void changesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}
	}
}
