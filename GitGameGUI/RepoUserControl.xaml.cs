﻿using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace GitGameGUI
{
	public partial class RepoUserControl : UserControl
	{
		private static RepoUserControl singleton;
		
		public static Repository repo;
		public static string repoPath;

		public static Signature signature;
		public static UsernamePasswordCredentials credentials;
		public static XML.RepoSettings repoSettings;
		public static XML.RepoUserSettings repoUserSettings;
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
            var filter = new Filters.GitLFS("lfs", filteredFiles);
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

		public static void SaveSettings()
		{
			if (repo != null)
			{
				// save gui settings
				Settings.Save<XML.RepoSettings>(repoPath + "\\" + Settings.RepoFilename, repoSettings);
				Settings.Save<XML.RepoUserSettings>(repoPath + "\\" + Settings.RepoUserFilename, repoUserSettings);

				// save repo settings
				var origin = repo.Network.Remotes["origin"];
				if (origin != null) repo.Network.Remotes.Update(origin, r => r.Url = singleton.repoURLTextBox.Text);
			}
		}

		public static void Dispose()
		{
			SaveSettings();
			if (repo != null)
			{
				repo.Dispose();
				repo = null;
			}
		}

		private void FinishedUpdatingUICallback()
		{
			if (repo == null && activeRepoComboBox.SelectedItem != null) OpenRepo(activeRepoComboBox.Text);
		}

		public static void Refresh()
		{
			OpenRepo(repoPath);
		}
		
		public static void OpenRepo(string repoPath, bool useExistingUserPass = false)
		{
			MainWindow.uiUpdating = true;

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

					// load repo url
					singleton.repoURLTextBox.Text = "";
					foreach (var remote in repo.Network.Remotes)
					{
						singleton.repoURLTextBox.Text = repo.Network.Remotes["origin"].Url;
						break;
					}

					// load repo settings
					repoSettings = Settings.Load<XML.RepoSettings>(repoPath + "\\" + Settings.RepoFilename);
					repoUserSettings = Settings.Load<XML.RepoUserSettings>(repoPath + "\\" + Settings.RepoUserFilename);
					singleton.gitLFSSupportCheckBoxSkip = true;
					singleton.gitLFSSupportCheckBox.IsChecked = repoSettings.lfsSupport;
					singleton.gitLFSSupportCheckBoxSkip = false;
					singleton.gitignoreExistsCheckBox.IsChecked = repoSettings.validateGitignore;
					if (useExistingUserPass)
					{
						repoUserSettings.signatureName = singleton.sigNameTextBox.Text;
						repoUserSettings.signatureEmail = singleton.sigEmailTextBox.Text;
						repoUserSettings.username = singleton.usernameTextBox.Text;
						repoUserSettings.password = singleton.passTextBox.Password;
					}
					else
					{
						singleton.sigNameTextBox.Text = repoUserSettings.signatureName;
						singleton.sigEmailTextBox.Text = repoUserSettings.signatureEmail;
						singleton.usernameTextBox.Text = repoUserSettings.username;
						singleton.passTextBox.Password = repoUserSettings.password;
					}

					// make sure git-lfs is installed
					if (repoSettings.lfsSupport && !CheckUpdatesWindow.gitlfsInstalled)
					{
						MessageBox.Show("Git-LFS is not installed and is required for this repo.");

						// dispose current
						signature = null;
						credentials = null;
						RepoUserControl.repoPath = null;
						if (repo != null)
						{
							repo.Dispose();
							repo = null;
						}

						MainWindow.uiUpdating = false;
						MainWindow.UpdateUI();
						return;
					}

					// check for lfs
					singleton.CheckGitLFS(false);

					// check for .gitignore file
					if (repoSettings.validateGitignore)
					{
						if (!File.Exists(repoPath + "\\.gitignore"))
						{
							MessageBox.Show("No '.gitignore' file exists.\nMake sure you add one!");
						}
					}

					// create signature
					signature = new Signature(repoUserSettings.signatureName, repoUserSettings.signatureEmail, DateTimeOffset.UtcNow);

					// create credentials
					credentials = new UsernamePasswordCredentials
					{
						Username = repoUserSettings.username,
						Password = repoUserSettings.password
					};

					// trim repository list
					if (MainWindow.appSettings.repositories.Count > 10)
					{
						MainWindow.appSettings.repositories.RemoveAt(MainWindow.appSettings.repositories.Count - 1);
					}

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

					if (!File.Exists(repoPath + "\\" + Settings.RepoFilename)) Settings.Save<XML.RepoSettings>(repoPath + "\\" + Settings.RepoFilename, repoSettings);
					if (!File.Exists(repoPath + "\\" + Settings.RepoUserFilename)) Settings.Save<XML.RepoUserSettings>(repoPath + "\\" + Settings.RepoUserFilename, repoUserSettings);
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

			MainWindow.uiUpdating = false;
			MainWindow.UpdateUI();
		}

		private bool CheckGitLFS(bool forceAttrCheck, bool notInstalledDefaultValue = false)
		{
			if (repoSettings.lfsSupport && !CheckUpdatesWindow.gitlfsInstalled)
			{
				MessageBox.Show("Git-LFS is not installed.");
				gitLFSSupportCheckBoxSkip = true;
				repoSettings.lfsSupport = notInstalledDefaultValue;
				gitLFSSupportCheckBox.IsChecked = notInstalledDefaultValue;
				gitLFSSupportCheckBoxSkip = false;
				return false;
			}

			// make sure the user wants this check
			if (!repoSettings.lfsSupport)
			{
				gitLFSSupportCheckBoxSkip = true;
				gitLFSSupportCheckBox.IsChecked = false;
				gitLFSSupportCheckBoxSkip = false;
				return false;
			}

			// check if already init
			if (!Directory.Exists(repoPath + "\\.git\\lfs") || !File.Exists(repoPath + "\\.gitattributes"))
			{
				// ask user for default git lfs support
				if (MessageBox.Show("Git-LFS not found or fully init.\nDo you want to init Git-LFS?", "Git-LFS?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
				{
					repoSettings.lfsSupport = false;
					gitLFSSupportCheckBoxSkip = true;
					gitLFSSupportCheckBox.IsChecked = false;
					gitLFSSupportCheckBoxSkip = false;
					return false;
				}
			}

			// init git lfs
			if (!Directory.Exists(repoPath + "\\.git\\lfs"))
			{
				Tools.RunExe("git-lfs", "install", null);
				if (!Directory.Exists(repoPath + "\\.git\\lfs"))
				{
					MessageBox.Show("Git-LFS install failed! (Try manually)");
					repoSettings.lfsSupport = false;
					gitLFSSupportCheckBoxSkip = true;
					gitLFSSupportCheckBox.IsChecked = false;
					gitLFSSupportCheckBoxSkip = false;
					return false;
				}
			}

			// add default ext to git lfs
			if ((!File.Exists(repoPath + "\\.gitattributes") || forceAttrCheck) && MessageBox.Show("Do you want to add Git-Game-GUI Git-LFS ext types?", "Git-LFS Ext?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				foreach (string ext in MainWindow.appSettings.defaultGitLFS_Exts)
				{
					Tools.RunExe("git-lfs", string.Format("track \"*{0}\"", ext), null);
				}

				if (!File.Exists(repoPath + "\\.gitattributes"))
				{
					MessageBox.Show("Git-LFS track .ext(s) failed! (.gitattributes doesn't exist)");
				}
			}

			if (!File.Exists(repoPath + "\\.gitattributes"))
			{
				using (var writer = File.CreateText(repoPath + "\\.gitattributes"))
				{
					// this will be an empty file...
				}
			}
			
			return true;
		}

		private void createButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(repoURLTextBox.Text))
			{
				if (MessageBox.Show("If you dont add a URL the repo will be local only.\nDo you want to continue?", "Warning", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
				{
					return;
				}
			}

			string remoteURL = repoURLTextBox.Text;
			try
			{
				var dlg = new System.Windows.Forms.FolderBrowserDialog();
				dlg.ShowNewFolderButton = true;
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					// init git repo
					Repository.Init(dlg.SelectedPath, false);
					OpenRepo(dlg.SelectedPath, true);
					if (!string.IsNullOrEmpty(remoteURL))
					{
						repo.Network.Remotes.Add("origin", remoteURL);
						repoURLTextBox.Text = remoteURL;
					}
				}
				else
				{
					return;
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
			if (string.IsNullOrEmpty(repoURLTextBox.Text))
			{
				MessageBox.Show("Must enter a URL");
				return;
			}

			try
			{
				var dlg = new System.Windows.Forms.FolderBrowserDialog();
				dlg.ShowNewFolderButton = true;
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					var options = new CloneOptions();
					options.IsBare = false;
					options.CredentialsProvider = (_url, _user, _cred) => credentials;
					Repository.Clone(repoURLTextBox.Text, dlg.SelectedPath, options);
					OpenRepo(dlg.SelectedPath, true);
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

			SaveSettings();
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

		bool gitLFSSupportCheckBoxSkip = false;
		private void gitLFSSupportCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (repoSettings == null || MainWindow.uiUpdating) return;
			if (gitLFSSupportCheckBoxSkip)
			{
				gitLFSSupportCheckBoxSkip = false;
				return;
			}

			if (gitLFSSupportCheckBox.IsChecked == true)
			{
				try
				{
					repoSettings.lfsSupport = true;
					CheckGitLFS(true, false);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Add Git-LFS Error: " + ex.Message);
				}
			}
			else
			{
				if (MessageBox.Show("Are you sure you want to remove Git-LFS?\nIf you commit or pushed while using Git-LFS, its suggested you re-base your repo.", "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
				{
					gitLFSSupportCheckBoxSkip = true;
					gitLFSSupportCheckBox.IsChecked = true;
					return;
				}

				try
				{
					repoSettings.lfsSupport = false;

					// untrack lfs filters
					if (File.Exists(repoPath + "\\.gitattributes"))
					{
						string data = File.ReadAllText(repoPath + "\\.gitattributes");
						var values = Regex.Matches(data, @"(\*\..*)? filter=lfs diff=lfs merge=lfs");
						foreach (Match value in values)
						{
							if (value.Groups.Count != 2) continue;
							Tools.RunExe("git-lfs", string.Format("untrack \"{0}\"", value.Groups[1].Value), null);
						}
					}

					// remove lfs repo files
					Tools.RunExe("git-lfs", "uninstall", null);
					if (File.Exists(repoPath + "\\.git\\hooks\\pre-push")) File.Delete(repoPath + "\\.git\\hooks\\pre-push");
					if (Directory.Exists(repoPath + "\\.git\\lfs")) Directory.Delete(repoPath + "\\.git\\lfs", true);
					
					// TODO: ask user if they want to rebase the repo
				}
				catch (Exception ex)
				{
					MessageBox.Show("Remove Git-LFS Error: " + ex.Message);
				}
			}
		}

		private void gitignoreExistsCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (repoSettings != null || MainWindow.uiUpdating) repoSettings.validateGitignore = gitignoreExistsCheckBox.IsChecked == true ? true : false;
		}

		private void sigNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (repoUserSettings != null || MainWindow.uiUpdating) repoUserSettings.signatureName = sigNameTextBox.Text;
		}

		private void sigEmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (repoUserSettings != null || MainWindow.uiUpdating) repoUserSettings.signatureEmail = sigEmailTextBox.Text;
		}

		private void usernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (MainWindow.uiUpdating) return;
			if (repoUserSettings != null) repoUserSettings.username = usernameTextBox.Text;
			if (credentials != null) credentials.Username = usernameTextBox.Text;
		}

		private void passTextBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (MainWindow.uiUpdating) return;
			if (repoUserSettings != null) repoUserSettings.password = passTextBox.Password;
			if (credentials != null) credentials.Password = passTextBox.Password;
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

			if (MessageBox.Show(string.Format("Are you sure you want to add Git-LFS ext: '{0}'", gitLFSExtTextBox.Text), "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				Tools.RunExe("git-lfs", string.Format("track \"*{0}\"", gitLFSExtTextBox.Text), null);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Add Git-LFS Ext Error: " + ex.Message);
			}
		}
	}
}
