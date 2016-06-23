using LibGit2Sharp;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace GitGameGUI
{
	public class LFSFilter : Filter
    {
		private Process process;
		FilterMode mode;

        public LFSFilter(string name, IEnumerable<FilterAttributeEntry> attributes) : base(name, attributes)
        {
        }

        protected override void Clean(string path, string root, Stream input, Stream output)
        {
			try
			{
				// write all file data to stdin
				input.CopyTo(process.StandardInput.BaseStream);
				input.Flush();
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message);
			}
        }

        protected override void Complete(string path, string root, Stream output)
        {
			try
			{
				// finalize stdin and wait for git-lfs to finish
				process.StandardInput.Flush();
				process.StandardInput.Close();
				if (mode == FilterMode.Clean) process.WaitForExit();
			
				// write git-lfs pointer for 'clean' to git or file data for 'smudge' to working copy
				process.StandardOutput.BaseStream.Flush();
				process.StandardOutput.BaseStream.CopyTo(output);
				output.Flush();
				output.Close();
				process.StandardOutput.Close();
			
				if (mode == FilterMode.Smudge) process.WaitForExit();
				process.Dispose();
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message);
			}
        }

        protected override void Create(string path, string root, FilterMode mode)
        {
			this.mode = mode;

			try
			{
				// launch git-lfs
				process = new Process();
				process.StartInfo.FileName = "git-lfs";
				process.StartInfo.Arguments = mode == FilterMode.Clean ? "clean" : "smudge";
				process.StartInfo.WorkingDirectory = RepoUserControl.repoPath;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.CreateNoWindow = false;
				process.StartInfo.UseShellExecute = false;
				process.Start();
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message);
			}
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void Smudge(string path, string root, Stream input, Stream output)
        {
            try
			{
				// write git-lfs pointer to stdin
				input.CopyTo(process.StandardInput.BaseStream);
				input.Flush();
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message);
			}
        }
    }

    public partial class RepoUserControl : UserControl
	{
		private static RepoUserControl singleton;
		
		public static Repository repo;
		public static string repoPath;

		public static Signature signature;
		public static UsernamePasswordCredentials credentials;
		public static XML.RepoSettings repoSettings;
		public static string mergeToolPath;
		bool canTriggerRepoChange = true;

        FilterRegistration lfsFilter;

		public RepoUserControl()
		{
			singleton = this;
			InitializeComponent();
			MainWindow.UpdateUICallback += UpdateUI;
			MainWindow.FinishedUpdatingUICallback += FinishedUpdatingUICallback;

			// create lfs filter
            var filteredFiles = new List<FilterAttributeEntry>()
            {
                new FilterAttributeEntry("lfs")
            };
            var filter = new LFSFilter("lfs", filteredFiles);
            lfsFilter = GlobalSettings.RegisterFilter(filter);
			
			// create signature
			signature = new Signature("default", "default", DateTimeOffset.UtcNow);

			// create credentials
			credentials = new UsernamePasswordCredentials
			{
				Username = "default",
				Password = "default"
			};
		}

		private void UpdateUI()
		{
			// update app settings
			switch (MainWindow.appSettings.mergeDiffTool)
			{
				case "Meld": mergeDiffToolComboBox.SelectedIndex = 0; break;
				case "kDiff3": mergeDiffToolComboBox.SelectedIndex = 1; break;
				case "P4Merge": mergeDiffToolComboBox.SelectedIndex = 2; break;
				case "DiffMerge": mergeDiffToolComboBox.SelectedIndex = 3; break;
			}
			
			activeRepoComboBox.Items.Clear();
			foreach (var repoSetting in MainWindow.appSettings.repositories)
			{
				activeRepoComboBox.Items.Add(repoSetting.path);
			}
			
			if (activeRepoComboBox.Items.Count != 0) activeRepoComboBox.SelectedIndex = 0;
		}

		public static void Dispose()
		{
			if (repo != null)
			{
				Settings.Save<XML.RepoSettings>(repoPath + "\\.gitgamegui", repoSettings);
				repo.Dispose();
				repo = null;
			}
		}

		private void FinishedUpdatingUICallback()
		{
			if (repo == null && activeRepoComboBox.SelectedItem != null) OpenRepo(activeRepoComboBox.Text);
		}
		
		public static void OpenRepo(string repoPath)
		{
			// dispose current
			signature = null;
			credentials = null;
			RepoUserControl.repoPath = null;
			if (repo != null)
			{
				repo.Dispose();
				repo = null;
			}
			
			try
			{
				if (!string.IsNullOrEmpty(repoPath))
				{
					// load repo
					RepoUserControl.repoPath = repoPath;
					repo = new Repository(repoPath);

					// load repo settings
					repoSettings = Settings.Load<XML.RepoSettings>(repoPath + "\\.gitgamegui");
					singleton.gitLFSSupportCheckBox.IsChecked = repoSettings.validateLFS;
					singleton.gitignoreExistsCheckBox.IsChecked = repoSettings.validateGitignore;
					singleton.autoCommitMergeCheckBox.IsChecked = repoSettings.autoCommit;
					singleton.autoPushRemoteMergeCheckBox.IsChecked = repoSettings.autoPush;
					singleton.sigNameTextBox.Text = repoSettings.signatureName;
					singleton.sigEmailTextBox.Text = repoSettings.signatureEmail;
					singleton.usernameTextBox.Text = repoSettings.username;
					singleton.passTextBox.Text = repoSettings.password;

					// create signature
					signature = new Signature(repoSettings.signatureName, repoSettings.signatureEmail, DateTimeOffset.UtcNow);

					// create credentials
					credentials = new UsernamePasswordCredentials
					{
						Username = repoSettings.username,
						Password = repoSettings.password
					};

					// add to settings
					bool exists = false;
					foreach (var repoSetting in MainWindow.appSettings.repositories)
					{
						if (repoSetting.path == repoPath)
						{
							exists = true;
							MainWindow.appSettings.repositories.Remove(repoSetting);
							MainWindow.appSettings.repositories.Insert(0, repoSetting);
							singleton.canTriggerRepoChange = false;
							singleton.activeRepoComboBox.Items.Remove(repoPath);
							singleton.activeRepoComboBox.Items.Insert(0, repoPath);
							singleton.activeRepoComboBox.SelectedIndex = 0;
							singleton.canTriggerRepoChange = true;
							break;
						}
					}

					if (!exists)
					{
						var repoSetting = new XML.Repository();
						repoSetting.path = repoPath;
						MainWindow.appSettings.repositories.Insert(0, repoSetting);
						singleton.activeRepoComboBox.Items.Insert(0, repoPath);
					}

					if (!File.Exists(repoPath + "\\.gitgamegui")) Settings.Save<XML.RepoSettings>(repoPath + "\\.gitgamegui", repoSettings);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("Load Repo Error: " + e.Message);
				signature = null;
				RepoUserControl.repoPath = null;
				if (repo != null)
				{
					repo.Dispose();
					repo = null;
				}

				// remove bad repo path
				foreach (var repoSetting in MainWindow.appSettings.repositories.ToArray())
				{
					if (repoSetting.path == repoPath) MainWindow.appSettings.repositories.Remove(repoSetting);
				}
				
				singleton.activeRepoComboBox.Items.Remove(repoPath);
				singleton.activeRepoComboBox.SelectedItem = null;
			}
			
			MainWindow.UpdateUI();
		}

		private void AddDefaultGitLFS()
		{
			// init git lfs
			Tools.RunCmd("git-lfs install", repoPath);

			// add default ext to git lfs
			if (MessageBox.Show("Do you want to add Git-Game-GUI Git-LFS ext types?", "Git-LFS Ext?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				foreach (string ext in MainWindow.appSettings.gitLFS_IgnoreExts)
				{
					Tools.RunCmd(string.Format("git-lfs track \"*{0}\"", ext), repoPath);
				}
			}
		}

		private void createButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var dlg = new System.Windows.Forms.FolderBrowserDialog();
				dlg.ShowNewFolderButton = true;
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					// init git repo
					Repository.Init(dlg.SelectedPath);
					OpenRepo(dlg.SelectedPath);

					// ask user for default git lfs support
					if (MessageBox.Show("Do you want to init Git-LFS?", "Git-LFS?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
					{
						AddDefaultGitLFS();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Create Repo Error: " + ex.Message);
				return;
			}

			MessageBox.Show("Finished Successfully!");
		}

		private void cloneButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var dlg = new System.Windows.Forms.FolderBrowserDialog();
				dlg.ShowNewFolderButton = true;
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					var options = new CloneOptions();
					options.IsBare = false;
					options.CredentialsProvider = (_url, _user, _cred) => credentials;
					Repository.Clone(repoPathTextBox.Text, dlg.SelectedPath, options);
					OpenRepo(dlg.SelectedPath);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Clone Repo Error: " + ex.Message);
			}
		}

		private void openRepoButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var dlg = new System.Windows.Forms.FolderBrowserDialog();
				dlg.ShowNewFolderButton = false;
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					OpenRepo(dlg.SelectedPath);
					activeRepoComboBox.SelectedIndex = 0;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Open Repo Error: " + ex.Message);
			}
		}

		private void clearRepoListButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.appSettings.repositories.Clear();
			activeRepoComboBox.Items.Clear();
		}

		private void activeRepoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainWindow.uiUpdating || !canTriggerRepoChange) return;

			if (activeRepoComboBox.Items.Count != 0) OpenRepo(activeRepoComboBox.SelectedItem as string);
			else OpenRepo(null);
		}

		private void mergeDiffToolComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (mergeDiffToolComboBox.SelectedValue == null)
			{
				mergeToolPath = "";
				return;
			}

			MainWindow.appSettings.mergeDiffTool = ((ComboBoxItem)mergeDiffToolComboBox.SelectedValue).Content as string;
			string programFilesx86, programFilesx64;
			Tools.GetProgramFilesPath(out programFilesx86, out programFilesx64);
			switch (MainWindow.appSettings.mergeDiffTool)
			{
				case "Meld": mergeToolPath = programFilesx86 + "\\Meld\\Meld.exe"; break;
				case "kDiff3": mergeToolPath = programFilesx64 + "\\KDiff3\\kdiff3.exe"; break;
				case "P4Merge": mergeToolPath = programFilesx64 + "\\Perforce\\p4merge.exe"; break;
				case "DiffMerge": mergeToolPath = programFilesx64 + "\\SourceGear\\Common\\\\DiffMerge\\sgdm.exe"; break;
			}
		}

		private void gitLFSSupportCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (repoSettings != null) repoSettings.validateLFS = gitLFSSupportCheckBox.IsChecked == true ? true : false;
		}

		private void gitignoreExistsCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (repoSettings != null) repoSettings.validateGitignore = gitignoreExistsCheckBox.IsChecked == true ? true : false;
		}

		private void autoCommitMergeCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (repoSettings != null) repoSettings.autoCommit = autoCommitMergeCheckBox.IsChecked == true ? true : false;
		}

		private void autoPushRemoteMergeCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (repoSettings != null) repoSettings.autoPush = autoPushRemoteMergeCheckBox.IsChecked == true ? true : false;
		}

		private void sigNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (repoSettings != null) repoSettings.signatureName = sigNameTextBox.Text;
		}

		private void sigEmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (repoSettings != null) repoSettings.signatureEmail = sigEmailTextBox.Text;
		}

		private void usernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (repoSettings != null) repoSettings.username = usernameTextBox.Text;
			if (credentials != null) credentials.Username = usernameTextBox.Text;
		}

		private void passTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (repoSettings != null) repoSettings.password = passTextBox.Text;
			if (credentials != null) credentials.Password = passTextBox.Text;
		}

		private void addGitLFSExtButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(gitLFSExtTextBox.Text) || gitLFSExtTextBox.Text.Length == 1)
			{
				MessageBox.Show("Must enter a valid ext type");
				return;
			}

			if (gitLFSExtTextBox.Text[0] != '.')
			{
				MessageBox.Show("Invalid ext format (must prefix with '.')");
				return;
			}

			if (MessageBox.Show(string.Format("Are you sure you want to add ext: {0}\nThis is permanent!", gitLFSExtTextBox.Text), "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				Tools.RunCmd(string.Format("git-lfs track \"*{0}\"", gitLFSExtTextBox.Text), repoPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Add Git-LFS Ext Error: " + ex.Message);
			}
		}

		private void addGitLFSButton_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to add Git-LFS?", "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				AddDefaultGitLFS();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Add Gir-LFS Error: " + ex.Message);
			}
		}

		private void removeGitLFSButton_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to remove Git-LFS?\nIf you commit/pushed while using Git-LFS, its suggested you re-create your remote repo.", "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				Tools.RunCmd("git-lfs uninit", repoPath);
				if (File.Exists(repoPath + "\\.gitattributes")) File.Delete(repoPath + "\\.gitattributes");
				if (Directory.Exists(repoPath + "\\.git\\lfs")) Directory.Delete(repoPath + "\\.git\\lfs", true);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Remove Gir-LFS Error: " + ex.Message);
			}
		}
	}
}
