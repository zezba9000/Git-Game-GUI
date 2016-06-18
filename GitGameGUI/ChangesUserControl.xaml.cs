using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
	public class FileItem
	{
		public BitmapImage icon;
		public BitmapImage Icon {get {return icon;}}

		public string filename;
		public string Filename {get {return filename;}}

		public FileItem()
		{
			
		}

		public FileItem(string iconFilename, string filename)
		{
			icon = new BitmapImage(new Uri("pack://application:,,,/" + iconFilename));
			this.filename = filename;
		}
	}

	public partial class ChangesUserControl : UserControl
	{
		public ChangesUserControl()
		{
			InitializeComponent();

			RepoUserControl.RepoChangedCallback += RepoChanged;
			BranchesUserControl.BranchChangedCallback += BranchChanged;
		}

		private void RepoChanged()
		{
			refreshChangedButton_Click(null, null);
		}

		private void BranchChanged()
		{
			refreshChangedButton_Click(null, null);
		}

		private void refreshChangedButton_Click(object sender, RoutedEventArgs e)
		{
			diffTextBlock.Text = "";
			var repoStatus = RepoUserControl.repo.RetrieveStatus();
			bool changesFound = false;
			changesListView.Items.Clear();
			changesListView2.Items.Clear();
			foreach (var fileStatus in repoStatus)
			{
				changesFound = true;
				Console.WriteLine("STATUS: " + fileStatus.State);

				if ((fileStatus.State & FileStatus.ModifiedInWorkdir) != 0) changesListView.Items.Add(new FileItem("Icons/modified.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.ModifiedInIndex) != 0) changesListView2.Items.Add(new FileItem("Icons/modified.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.NewInWorkdir) != 0) changesListView.Items.Add(new FileItem("Icons/new.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.NewInIndex) != 0) changesListView2.Items.Add(new FileItem("Icons/new.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.DeletedFromWorkdir) != 0) changesListView.Items.Add(new FileItem("Icons/deleted.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.DeletedFromIndex) != 0) changesListView2.Items.Add(new FileItem("Icons/deleted.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.RenamedInWorkdir) != 0) changesListView.Items.Add(new FileItem("Icons/renamed.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.RenamedInIndex) != 0) changesListView2.Items.Add(new FileItem("Icons/renamed.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.TypeChangeInWorkdir) != 0) changesListView.Items.Add(new FileItem("Icons/typeChanged.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.TypeChangeInIndex) != 0) changesListView2.Items.Add(new FileItem("Icons/typeChanged.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.Conflicted) != 0) changesListView.Items.Add(new FileItem("Icons/typeChanged.png", fileStatus.FilePath));
				else if ((fileStatus.State & FileStatus.Ignored) != 0)
				{
					// do nothing...
				}
				else
				{
					MessageBox.Show("Unsuported File State: " + fileStatus.State);
				}
			}

			if (!changesFound) Console.WriteLine("No Changes");
		}

		private void changesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			e.Handled = true;
			if (changesListView.SelectedItem == null)
			{
				diffTextBlock.Text = "";
				return;
			}

			changesListView2.SelectedItem = null;
			
			foreach (var item in RepoUserControl.repo.RetrieveStatus())
			{
				if (item.FilePath != ((FileItem)changesListView.SelectedValue).filename) continue;

				if (item.State == FileStatus.ModifiedInWorkdir)
				{
					var patch = RepoUserControl.repo.Diff.Compare<Patch>(new List<string>() { item.FilePath });
					diffTextBlock.Text = patch.Content;
				}
				else if (item.State == FileStatus.NewInWorkdir)
				{
					var blob = RepoUserControl.repo.ObjectDatabase.CreateBlob(item.FilePath);
					if (!blob.IsBinary) diffTextBlock.Text = blob.GetContentText();
					else diffTextBlock.Text = "<< Binary File >>";
				}
				else if (item.State == FileStatus.Conflicted)
				{
					//var result = repo.Merge(activeBranch, null);
					//if (result.Status == MergeStatus.Conflicts)
					{
						foreach (var conflict in RepoUserControl.repo.Index.Conflicts)
						{
							var branch = RepoUserControl.repo.Branches["master"];
							var tip = branch.Tip;
							var blob = tip[conflict.Theirs.Path].Target as Blob;
							string value = blob.GetContentText();
							//repo.Index.Add(conflict.Ours.Path);// submit resolved file
						}
					}
					
					var patch = RepoUserControl.repo.Diff.Compare<Patch>(new List<string>() { item.FilePath });
					diffTextBlock.Text = patch.Content;
				}
			}
		}

		private void changesListView2_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			e.Handled = true;
			if (changesListView2.SelectedItem == null)
			{
				diffTextBlock.Text = "";
				return;
			}

			changesListView.SelectedItem = null;
			
			foreach (var item in RepoUserControl.repo.RetrieveStatus())
			{
				if (item.FilePath != ((FileItem)changesListView2.SelectedValue).filename) continue;

				if (item.State == FileStatus.ModifiedInIndex)
				{
					var patch = RepoUserControl.repo.Diff.Compare<Patch>(new List<string>() { item.FilePath });
					diffTextBlock.Text = patch.Content;
				}
				else if (item.State == FileStatus.NewInIndex)
				{
					var blob = RepoUserControl.repo.ObjectDatabase.CreateBlob(item.FilePath);
					if (!blob.IsBinary) diffTextBlock.Text = blob.GetContentText();
					else diffTextBlock.Text = "<< Binary File >>";
				}
			}
		}

		private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			changesListView.SelectedItem = null;
			changesListView2.SelectedItem = null;
			diffTextBlock.Text = "";
		}

		private void fileItemImage_MouseDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			var image = sender as Image;

			// stage file
			foreach (var item in changesListView.Items)
			{
				var fileItem = ((FileItem)item);
				if (image.Source == fileItem.icon)
				{
					RepoUserControl.repo.Stage(fileItem.filename);
					changesListView.Items.Remove(item);
					changesListView2.Items.Add(item);
					return;
				}
			}

			// unstage file
			foreach (var item in changesListView2.Items)
			{
				var fileItem = ((FileItem)item);
				if (image.Source == fileItem.icon)
				{
					RepoUserControl.repo.Unstage(fileItem.filename);
					changesListView2.Items.Remove(item);
					changesListView.Items.Add(item);
					return;
				}
			}
		}

		private void stageAllButton_Click(object sender, RoutedEventArgs e)
		{
			var items = new FileItem[changesListView.Items.Count];
			changesListView.Items.CopyTo(items, 0);
			foreach (var item in items)
			{
				RepoUserControl.repo.Stage(item.filename);
				changesListView.Items.Remove(item);
				changesListView2.Items.Add(item);
			}
		}

		private void unstageAllButton_Click(object sender, RoutedEventArgs e)
		{
			var items = new FileItem[changesListView2.Items.Count];
			changesListView2.Items.CopyTo(items, 0);
			foreach (var item in items)
			{
				RepoUserControl.repo.Unstage(item.filename);
				changesListView2.Items.Remove(item);
				changesListView.Items.Add(item);
			}
		}

		private void commitStagedButton_Click(object sender, RoutedEventArgs e)
		{
			RepoUserControl.repo.Commit(commitMessageTextBox.Text, RepoUserControl.signature, RepoUserControl.signature);
		}

		private void pushChangesButton_Click(object sender, RoutedEventArgs e)
		{
			RepoUserControl.repo.Network.Push(BranchesUserControl.activeBranch);
		}

		private void pullChangesButton_Click(object sender, RoutedEventArgs e)
		{
			var options = new PullOptions();
			RepoUserControl.repo.Network.Pull(RepoUserControl.signature, options);
			// TODO: check for merge issues
			BranchesUserControl.BranchChanged();
		}

		private bool resolveChange(FileItem item)
		{
			// get info
			string fullPath = string.Format("{0}\\{1}", RepoUserControl.repoPath, item.filename);
			var conflict = RepoUserControl.repo.Index.Conflicts[item.filename];
			var ours = RepoUserControl.repo.Lookup<Blob>(conflict.Ours.Id);
			var theirs = RepoUserControl.repo.Lookup<Blob>(conflict.Theirs.Id);
			
			// copy base and parse
			File.Copy(fullPath, fullPath + ".base", true);
			string baseFile = File.ReadAllText(fullPath);
			var match = Regex.Match(baseFile, @"(<<<<<<<\s*\w*[\r\n]*).*(=======[\r\n]*).*(>>>>>>>\s*\w*[\r\n]*)", RegexOptions.Singleline);
			if (match.Success && match.Groups.Count == 4)
			{
				baseFile = baseFile.Replace(match.Groups[1].Value, "").Replace(match.Groups[2].Value, "").Replace(match.Groups[3].Value, "");
				File.WriteAllText(fullPath + ".base", baseFile);
			}

			// hash base file
			byte[] baseHash = null;
			using (var md5 = MD5.Create())
			{
				using (var stream = File.OpenRead(fullPath + ".base"))
				{
					baseHash = md5.ComputeHash(stream);
				}
			}

			// copy ours
			using (var oursStream = ours.GetContentStream())
			using (var stream = new FileStream(fullPath + ".ours", FileMode.Create, FileAccess.Write, FileShare.None))
			{
				oursStream.CopyTo(stream);
			}

			// copy thiers
			using (var theirStream = theirs.GetContentStream())
			using (var stream = new FileStream(fullPath + ".thiers", FileMode.Create, FileAccess.Write, FileShare.None))
			{
				theirStream.CopyTo(stream);
			}

			// start external merge tool
			var process = new Process();
			process.StartInfo.FileName = RepoUserControl.mergeToolPath;
			if (MainWindow.appSettings.mergeDiffTool == "Meld") process.StartInfo.Arguments = string.Format("{0}.ours {0}.base {0}.thiers", fullPath);
			else if (MainWindow.appSettings.mergeDiffTool == "kDiff3") process.StartInfo.Arguments = string.Format("{0}.ours {0}.base {0}.thiers", fullPath);
			else if (MainWindow.appSettings.mergeDiffTool == "P4Merge") process.StartInfo.Arguments = string.Format("{0}.base {0}.ours {0}.thiers {0}.base", fullPath);
			else if (MainWindow.appSettings.mergeDiffTool == "DiffMerge") process.StartInfo.Arguments = string.Format("{0}.ours {0}.base {0}.thiers", fullPath);
			process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
			if (!process.Start())
			{
				MessageBox.Show("Failed to start Merge tool (is it installed?)");

				// delete temp files
				if (File.Exists(fullPath + ".base")) File.Delete(fullPath + ".base");
				if (File.Exists(fullPath + ".ours")) File.Delete(fullPath + ".ours");
				if (File.Exists(fullPath + ".thiers")) File.Delete(fullPath + ".thiers");

				return false;
			}

			process.WaitForExit();

			// get new base has
			byte[] baseHashChange = null;
			using (var md5 = MD5.Create())
			{
				using (var stream = File.OpenRead(fullPath + ".base"))
				{
					baseHashChange = md5.ComputeHash(stream);
				}
			}

			// check if file was modified
			bool wasModified = false;
			if (!baseHashChange.SequenceEqual(baseHash))
			{
				wasModified = true;
				File.Copy(fullPath + ".base", fullPath, true);
				RepoUserControl.repo.Index.Add(item.filename);
			}

			// delete temp files
			if (File.Exists(fullPath + ".base")) File.Delete(fullPath + ".base");
			if (File.Exists(fullPath + ".ours")) File.Delete(fullPath + ".ours");
			if (File.Exists(fullPath + ".thiers")) File.Delete(fullPath + ".thiers");

			// finish
			return wasModified;
		}

		private void resolveSelectedButton_Click(object sender, RoutedEventArgs e)
		{
			// check for common mistakes
			if (changesListView.SelectedIndex < 0)
			{
				MessageBox.Show("Must select 'Un-Staged' file");
				return;
			}

			try
			{
				var item = changesListView.SelectedItem as FileItem;
				if (RepoUserControl.repo.RetrieveStatus(item.filename) != FileStatus.Conflicted)
				{
					MessageBox.Show("This file is not in conflict");
					return;
				}

				if (resolveChange(item)) BranchesUserControl.BranchChanged();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to resolve file: " + ex.Message);
				return;
			}
		}

		private void resolveAllButton_Click(object sender, RoutedEventArgs e)
		{
			bool filesMerged = false;
			int conflictedFiles = 0;
			try
			{
				foreach (FileItem item in changesListView.Items)
				{
					if (RepoUserControl.repo.RetrieveStatus(item.filename) == FileStatus.Conflicted)
					{
						++conflictedFiles;
						if (resolveChange(item)) filesMerged = true;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to resolve some files: " + ex.Message);
				return;
			}

			if (filesMerged) BranchesUserControl.BranchChanged();

			if (conflictedFiles == 0)
			{
				MessageBox.Show("No files in conflict");
				return;
			}
		}
	}
}
