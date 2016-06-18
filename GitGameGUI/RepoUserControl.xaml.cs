using LibGit2Sharp;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
	public partial class RepoUserControl : UserControl
	{
		private static RepoUserControl singleton;
		
		public static Repository repo;
		public static string repoPath;
		public static Signature signature;
		public static XML.RepoSettings repoSettings;
		public static string mergeToolPath;

		public RepoUserControl()
		{
			singleton = this;
			InitializeComponent();
			MainWindow.UpdateUICallback += UpdateUI;
			MainWindow.FinishedUpdatingUICallback += FinishedUpdatingUICallback;
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
					singleton.nameTextBox.Text = repoSettings.signatureName;
					singleton.emailTextBox.Text = repoSettings.signatureEmail;

					// create signature
					signature = new Signature(repoSettings.signatureName, repoSettings.signatureEmail, DateTimeOffset.UtcNow);

					// add to settings
					bool exists = false;
					foreach (var repoSetting in MainWindow.appSettings.repositories)
					{
						if (repoSetting.path == repoPath)
						{
							exists = true;
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
			}
			
			MainWindow.UpdateUI();
		}

		private void cloneButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var dlg = new System.Windows.Forms.FolderBrowserDialog();
				dlg.ShowNewFolderButton = true;
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					Repository.Clone(repoPathTextBox.Text, dlg.SelectedPath);
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
			activeRepoComboBox.SelectedItem = null;
			activeRepoComboBox.Items.Clear();
		}

		private void activeRepoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainWindow.uiUpdating) return;

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
	}
}
