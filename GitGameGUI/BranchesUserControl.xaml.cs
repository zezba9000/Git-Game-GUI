using LibGit2Sharp;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GitGameGUI
{
	public partial class BranchesUserControl : UserControl
	{
		public static BranchesUserControl singleton;
		public static Branch activeBranch;

		public BranchesUserControl()
		{
			singleton = this;
			InitializeComponent();
			MainWindow.UpdateUICallback += UpdateUI;
		}

		private void UpdateUI()
		{
			// clear ui
			activeBranchComboBox.Items.Clear();
			otherBranchComboBox.Items.Clear();
			
			// check if repo exists
			if (RepoUserControl.repo == null) return;

			// fill ui
			try
			{
				var branches = RepoUserControl.repo.Branches;
				foreach (var branch in branches)
				{
					// make sure we don't show remotes that match locals
					if (branch.IsRemote)
					{
						if (branch.FriendlyName == "origin/HEAD" || branch.FriendlyName == "origin/master") continue;

						bool found = false;
						foreach (var otherBranch in branches)
						{
							if (branch.FriendlyName == otherBranch.FriendlyName) continue;
							if (branch.FriendlyName.Replace("origin/", "") == otherBranch.FriendlyName)
							{
								found = true;
								break;
							}
						}

						if (found) continue;
					}

					// add branch to list
					int i = activeBranchComboBox.Items.Add(branch.FriendlyName);
					if (!branch.IsRemote) otherBranchComboBox.Items.Add(branch.FriendlyName);
					if (branch.IsCurrentRepositoryHead)
					{
						activeBranchComboBox.SelectedIndex = i;
						activeBranch = branch;
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("Refresh Branches Error: " + e.Message);
			}
			
			activeBranchComboBox_SelectionChanged(null, null);
		}

		private void activeBranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainWindow.uiUpdating) return;

			try
			{
				string name = activeBranchComboBox.SelectedValue as string;

				// see if the user wants to add a remote to local
				if (name.Contains("origin/"))
				{
					name = name.Replace("origin/", "");
					if (MessageBox.Show("This branch is remote.\nWould you like to pull this branch locally?", "Alert", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
					{
						var branch = RepoUserControl.repo.CreateBranch(name);
						RepoUserControl.repo.Branches.Update(branch, b =>
						{
							b.Remote = "origin";
							b.UpstreamBranch = branch.CanonicalName;
						});

						activeBranchComboBox.Items.Add(branchNameTextBox.Text);
						RepoUserControl.repo.Checkout(branch);

						var options = new PullOptions();
						options.FetchOptions = new FetchOptions();
						options.FetchOptions.CredentialsProvider = (_url, _user, _cred) => RepoUserControl.credentials;
						options.FetchOptions.TagFetchMode = TagFetchMode.All;
						RepoUserControl.repo.Network.Pull(RepoUserControl.signature, options);
					}
					else
					{
						int i = 0;
						foreach (string item in activeBranchComboBox.Items)
						{
							if (item == activeBranch.FriendlyName)
							{
								activeBranchComboBox.SelectedIndex = i;
								return;
							}

							++i;
						}
					}
				}

				// change branch
				var selectedBranch = RepoUserControl.repo.Branches[name];
				if (activeBranch != selectedBranch)
				{
					var newBranch = RepoUserControl.repo.Checkout(selectedBranch);
					if (newBranch != selectedBranch) MessageBox.Show("Error checking out branch (do you have pending changes?)");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Checkout Branch Error: " + ex.Message);
			}
			
			RepoUserControl.Refresh();
		}

		private void mergeButton_Click(object sender, RoutedEventArgs e)
		{
			if (activeBranchComboBox.SelectedIndex < 0)
			{
				MessageBox.Show("Must select 'Active' branch\n(If none exists commit something first)");
				return;
			}

			if (otherBranchComboBox.SelectedIndex < 0)
			{
				MessageBox.Show("Must select 'Other' branch");
				return;
			}

			if (MessageBox.Show(string.Format("Are you sure you want to merge '{0}' with '{1}'", otherBranchComboBox.SelectedValue, activeBranchComboBox.SelectedValue), "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				var srcBround = RepoUserControl.repo.Branches[otherBranchComboBox.SelectedValue as string];
				RepoUserControl.repo.Merge(srcBround, RepoUserControl.signature);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Merge Branch Error: " + ex.Message);
			}
			
			ChangesUserControl.ResolveConflicts();
		}

		private void addNewBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (RepoUserControl.repo.Branches.Count() == 0)
			{
				MessageBox.Show("You must commit files to master before create new branches!");
				return;
			}

			if (string.IsNullOrEmpty(branchNameTextBox.Text))
			{
				MessageBox.Show("Must give the branch a name");
				return;
			}

			if (!Tools.IsSingleWord(branchNameTextBox.Text))
			{
				MessageBox.Show("No white space or special characters allowed");
				return;
			}

			if (MessageBox.Show(string.Format("Are you sure you want to add '{0}'", branchNameTextBox.Text), "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
			{
				return;
			}

			// see if the user wants to add remote info
			bool addRemoteInfo = false;
			if (MessageBox.Show("Will this branch be pushed to a remote server?", "Remote Tracking?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				addRemoteInfo = true;
			}

			try
			{
				var branch = RepoUserControl.repo.CreateBranch(branchNameTextBox.Text);
				if (addRemoteInfo)
				{
					RepoUserControl.repo.Branches.Update(branch, b =>
					{
						b.Remote = "origin";
						b.UpstreamBranch = branch.CanonicalName;
					});
				}

				activeBranchComboBox.Items.Add(branchNameTextBox.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Add new Branch Error: " + ex.Message);
				return;
			}

			RepoUserControl.Refresh();
			if (activeBranchComboBox.Items.Count != 0) activeBranchComboBox.SelectedIndex = activeBranchComboBox.Items.Count - 1;
		}

		private void deleteBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchComboBox.SelectedItem == null)
			{
				MessageBox.Show("Must select branch");
				return;
			}

			if (MessageBox.Show(string.Format("Are you sure you want to delete '{0}'", otherBranchComboBox.Text), "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				RepoUserControl.repo.Branches.Remove(otherBranchComboBox.Text);
				var remoteBranch = RepoUserControl.repo.Branches["origin/" + otherBranchComboBox.Text];
				if (remoteBranch != null) RepoUserControl.repo.Branches.Remove(remoteBranch);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Delete new Branch Error: " + ex.Message);
			}

			RepoUserControl.Refresh();
		}

		private void renameBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(branchNameTextBox.Text))
			{
				MessageBox.Show("Must give the branch a name");
				return;
			}

			if (!Tools.IsSingleWord(branchNameTextBox.Text))
			{
				MessageBox.Show("No white space or special characters allowed");
				return;
			}

			if (branchNameTextBox.Text == "origin" || branchNameTextBox.Text == "master" || branchNameTextBox.Text == "HEAD")
			{
				MessageBox.Show("Cannot name branch: (origin, master or HEAD)");
				return;
			}

			if (MessageBox.Show(string.Format("Are you sure you want to rename '{0}' to '{1}'", activeBranchComboBox.Text, branchNameTextBox.Text), "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				var branch = RepoUserControl.repo.Branches.Rename(activeBranchComboBox.Text, branchNameTextBox.Text);
				RepoUserControl.repo.Branches.Update(branch, b =>
				{
					b.Remote = "origin";
					b.UpstreamBranch = branch.CanonicalName;
				});
				activeBranchComboBox.Items.Add(branchNameTextBox.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Rename new Branch Error: " + ex.Message);
			}

			RepoUserControl.Refresh();
		}
	}
}
