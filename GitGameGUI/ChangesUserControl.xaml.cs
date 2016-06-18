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

namespace GitGameGUI
{
	public class FileItem
	{
		public BitmapImage icon;
		public BitmapImage Icon {get {return icon;}}

		public string filename;
		public string Filename {get {return filename;}}

		public FileItem()
		{
			filename = "ERROR";
		}

		public FileItem(string iconFilename, string filename)
		{
			icon = new BitmapImage(new Uri("pack://application:,,,/" + iconFilename));
			this.filename = filename;
		}
	}

	public partial class ChangesUserControl : UserControl
	{
		public static ChangesUserControl singleton;

		public ChangesUserControl()
		{
			singleton = this;
			InitializeComponent();
			MainWindow.UpdateUICallback += UpdateUI;
		}

		private void UpdateUI()
		{
			// clear ui
			diffTextBox.Text = "";
			bool changesFound = false;
			unstagedChangesListView.Items.Clear();
			stagedChangesListView.Items.Clear();

			// check if repo exists
			if (RepoUserControl.repo == null) return;

			// fill ui
			var repoStatus = RepoUserControl.repo.RetrieveStatus();
			foreach (var fileStatus in repoStatus)
			{
				changesFound = true;
				bool stateHandled = false;
				var state = fileStatus.State;
				if ((state & FileStatus.ModifiedInWorkdir) != 0) {unstagedChangesListView.Items.Add(new FileItem("Icons/modified.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.ModifiedInIndex) != 0) {stagedChangesListView.Items.Add(new FileItem("Icons/modified.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.NewInWorkdir) != 0) {unstagedChangesListView.Items.Add(new FileItem("Icons/new.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.NewInIndex) != 0) {stagedChangesListView.Items.Add(new FileItem("Icons/new.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.DeletedFromWorkdir) != 0) {unstagedChangesListView.Items.Add(new FileItem("Icons/deleted.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.DeletedFromIndex) != 0) {stagedChangesListView.Items.Add(new FileItem("Icons/deleted.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.RenamedInWorkdir) != 0) {unstagedChangesListView.Items.Add(new FileItem("Icons/renamed.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.RenamedInIndex) != 0) {stagedChangesListView.Items.Add(new FileItem("Icons/renamed.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.TypeChangeInWorkdir) != 0) {unstagedChangesListView.Items.Add(new FileItem("Icons/typeChanged.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.TypeChangeInIndex) != 0) {stagedChangesListView.Items.Add(new FileItem("Icons/typeChanged.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.Conflicted) != 0) {unstagedChangesListView.Items.Add(new FileItem("Icons/typeChanged.png", fileStatus.FilePath)); stateHandled = true;}
				if ((state & FileStatus.Ignored) != 0) {stateHandled = true;}
				if ((state & FileStatus.Unreadable) != 0)
				{
					stateHandled = true;
					string fullpath = RepoUserControl.repoPath + "\\" + fileStatus.FilePath;
					if (File.Exists(fullpath))
					{
						// disable readonly if this is the cause
						var attributes = File.GetAttributes(fullpath);
						if ((attributes & FileAttributes.ReadOnly) != 0) File.SetAttributes(fullpath, FileAttributes.Normal);
						else
						{
							MessageBox.Show("Problem will file read (please fix and refresh)\nCause: " + fileStatus.FilePath);
							continue;
						}

						// check to make sure file is now readable
						attributes = File.GetAttributes(fullpath);
						if ((attributes & FileAttributes.ReadOnly) != 0) MessageBox.Show("File is not readable (you will need to fix the issue and refresh\nCause: " + fileStatus.FilePath);
						else MessageBox.Show("Problem will file read (please fix and refresh)\nCause: " + fileStatus.FilePath);
					}
					else
					{
						MessageBox.Show("Expected file doesn't exist: " + fileStatus.FilePath);
					}
				}

				if (!stateHandled)
				{
					MessageBox.Show("Unsuported File State: " + state);
				}
			}

			if (!changesFound) Console.WriteLine("No Changes, now do some stuff!");
		}

		private void refreshChangedButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.UpdateUI();
		}

		private void RefreshDiffText(ListView listView)
		{
			foreach (var item in RepoUserControl.repo.RetrieveStatus())
			{
				if (item.FilePath != ((FileItem)listView.SelectedValue).filename) continue;

				// check if file still exists
				if (!File.Exists(RepoUserControl.repoPath + "\\" + item.FilePath))
				{
					diffTextBox.Text = "<< File Doesn't Exist >>";
					continue;
				}

				// check if binary file
				var blob = RepoUserControl.repo.ObjectDatabase.CreateBlob(item.FilePath);
				if (blob.IsBinary || Tools.IsBinaryFileType(item.FilePath))
				{
					diffTextBox.Text = "<< Binary File >>";
					continue;
				}

				// check for text types
				var state = item.State;
				if ((state & FileStatus.ModifiedInWorkdir) != 0)
				{
					var patch = RepoUserControl.repo.Diff.Compare<Patch>(new List<string>(){item.FilePath});
					var match = Regex.Match(patch.Content, @"@@\s-\d*\s\+\d*\s@@\n(.*)", RegexOptions.Singleline);
					if (match.Success && match.Groups.Count == 2) diffTextBox.Text = match.Groups[1].Value.Replace("\\ No newline at end of file\n", "");
					else diffTextBox.Text = patch.Content;
				}
				else if ((state & FileStatus.ModifiedInIndex) != 0)
				{
					diffTextBox.Text = blob.GetContentText();
				}
				else if ((state & FileStatus.NewInWorkdir) != 0 || (state & FileStatus.NewInIndex) != 0)
				{
					diffTextBox.Text = blob.GetContentText();
				}
				else if ((state & FileStatus.DeletedFromWorkdir) != 0 || (state & FileStatus.DeletedFromIndex) != 0)
				{
					diffTextBox.Text = blob.GetContentText();
				}
				else if ((state & FileStatus.RenamedInWorkdir) != 0 || (state & FileStatus.RenamedInIndex) != 0)
				{
					diffTextBox.Text = blob.GetContentText();
				}
				else if ((state & FileStatus.TypeChangeInWorkdir) != 0 || (state & FileStatus.TypeChangeInIndex) != 0)
				{
					diffTextBox.Text = blob.GetContentText();
				}
				else if ((state& FileStatus.Conflicted) != 0)
				{
					diffTextBox.Text = blob.GetContentText();
				}
				else if ((state & FileStatus.Ignored) != 0)
				{
					// do nothing...
				}
				else if ((state & FileStatus.Unreadable) != 0)
				{
					MessageBox.Show("Something is wrong. The file is not readable!");
				}
				else
				{
					MessageBox.Show("Unsuported FileStatus: " + state);
				}
			}
		}

		private void unstagedChangesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainWindow.uiUpdating) return;
			
			if (unstagedChangesListView.SelectedItem == null)
			{
				diffTextBox.Text = "";
				return;
			}

			stagedChangesListView.SelectedItem = null;
			RefreshDiffText(unstagedChangesListView);
		}

		private void stagedChangesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainWindow.uiUpdating) return;
			
			if (stagedChangesListView.SelectedItem == null)
			{
				diffTextBox.Text = "";
				return;
			}

			unstagedChangesListView.SelectedItem = null;
			RefreshDiffText(stagedChangesListView);
		}

		private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
		{
			unstagedChangesListView.SelectedItem = null;
			stagedChangesListView.SelectedItem = null;
			diffTextBox.Text = "";
		}

		private void FileItemImage_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var image = sender as Image;

			// stage file
			foreach (var item in unstagedChangesListView.Items)
			{
				var fileItem = ((FileItem)item);
				if (image.Source == fileItem.icon)
				{
					if ((RepoUserControl.repo.RetrieveStatus(fileItem.filename) & FileStatus.Conflicted) != 0)
					{
						MessageBox.Show("File must be resolved before it can be staged");
						return;
					}

					RepoUserControl.repo.Stage(fileItem.filename);
					unstagedChangesListView.Items.Remove(item);
					stagedChangesListView.Items.Add(item);
					return;
				}
			}

			// unstage file
			foreach (var item in stagedChangesListView.Items)
			{
				var fileItem = ((FileItem)item);
				if (image.Source == fileItem.icon)
				{
					RepoUserControl.repo.Unstage(fileItem.filename);
					stagedChangesListView.Items.Remove(item);
					unstagedChangesListView.Items.Add(item);
					return;
				}
			}
		}

		private void stageAllButton_Click(object sender, RoutedEventArgs e)
		{
			var items = new FileItem[unstagedChangesListView.Items.Count];
			unstagedChangesListView.Items.CopyTo(items, 0);
			foreach (var item in items)
			{
				RepoUserControl.repo.Stage(item.filename);
				unstagedChangesListView.Items.Remove(item);
				stagedChangesListView.Items.Add(item);
			}
		}

		private void unstageAllButton_Click(object sender, RoutedEventArgs e)
		{
			var items = new FileItem[stagedChangesListView.Items.Count];
			stagedChangesListView.Items.CopyTo(items, 0);
			foreach (var item in items)
			{
				RepoUserControl.repo.Unstage(item.filename);
				stagedChangesListView.Items.Remove(item);
				unstagedChangesListView.Items.Add(item);
			}
		}

		private void commitStagedButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(commitMessageTextBox.Text))
			{
				MessageBox.Show("Must enter a commit message");
				return;
			}

			try
			{
				RepoUserControl.repo.Commit(commitMessageTextBox.Text, RepoUserControl.signature, RepoUserControl.signature);
				commitMessageTextBox.Text = "";
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to commit: " + ex.Message);
			}

			MainWindow.UpdateUI();
		}

		private void pushChangesButton_Click(object sender, RoutedEventArgs e)
		{
			RepoUserControl.repo.Network.Push(BranchesUserControl.activeBranch);
		}

		private void pullChangesButton_Click(object sender, RoutedEventArgs e)
		{
			var options = new PullOptions();
			RepoUserControl.repo.Network.Pull(RepoUserControl.signature, options);
			ResolveConflicts();
		}

		public static void ResolveConflicts()
		{
			// update ui before issue check
			MainWindow.UpdateUI();

			// check for merge issues and invoke resolve
			if (RepoUserControl.repo.Index.Conflicts.Count() != 0) singleton.resolveAllButton_Click(null, null);

			// update ui after issue check
			MainWindow.UpdateUI();
		}

		private async Task<bool> resolveChange(FileItem item)
		{
			// get info
			string fullPath = string.Format("{0}\\{1}", RepoUserControl.repoPath, item.filename);
			var conflict = RepoUserControl.repo.Index.Conflicts[item.filename];
			var ours = RepoUserControl.repo.Lookup<Blob>(conflict.Ours.Id);
			var theirs = RepoUserControl.repo.Lookup<Blob>(conflict.Theirs.Id);

			// check if files are binary (if so open select source tool)
			if (ours.IsBinary || theirs.IsBinary)
			{
				// open merge tool\
				var mergeBinaryFileWindow = new MergeBinaryFileWindow();
				mergeBinaryFileWindow.Owner = MainWindow.singleton;
				mergeBinaryFileWindow.fileInConflict = item.filename;
				mergeBinaryFileWindow.Show();
				await mergeBinaryFileWindow.WaitForClose();
				if (mergeBinaryFileWindow.result == MergeBinaryResults.Cancel) return false;

				// check if we want theirs and copy
				if (mergeBinaryFileWindow.result == MergeBinaryResults.UserTheirs)
				{
					using (var theirStream = theirs.GetContentStream())
					using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						theirStream.CopyTo(stream);
					}
				}

				RepoUserControl.repo.Index.Add(item.filename);
				return true;
			}
			
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

		private async void resolveSelectedButton_Click(object sender, RoutedEventArgs e)
		{
			// check for common mistakes
			if (unstagedChangesListView.SelectedIndex < 0)
			{
				MessageBox.Show("Must select 'Un-Staged' file");
				return;
			}

			try
			{
				var item = unstagedChangesListView.SelectedItem as FileItem;
				if ((RepoUserControl.repo.RetrieveStatus(item.filename) & FileStatus.Conflicted) == 0)
				{
					MessageBox.Show("This file is not in conflict");
					return;
				}

				if (await resolveChange(item)) UpdateUI();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to resolve file: " + ex.Message);
				return;
			}
		}

		private async void resolveAllButton_Click(object sender, RoutedEventArgs e)
		{
			int conflictedFiles = 0;
			try
			{
				foreach (FileItem item in unstagedChangesListView.Items)
				{
					if ((RepoUserControl.repo.RetrieveStatus(item.filename) & FileStatus.Conflicted) != 0)
					{
						++conflictedFiles;
						if (await resolveChange(item) == false) break;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to resolve some files: " + ex.Message);
				return;
			}

			UpdateUI();
			if (conflictedFiles == 0)
			{
				MessageBox.Show("No files in conflict");
				return;
			}
		}
	}
}
