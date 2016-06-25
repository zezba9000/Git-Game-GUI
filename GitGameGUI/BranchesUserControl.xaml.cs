using LibGit2Sharp;
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
				foreach (var branch in RepoUserControl.repo.Branches)
				{
					if (branch.IsRemote) continue;
					int i = activeBranchComboBox.Items.Add(branch.FriendlyName);
					otherBranchComboBox.Items.Add(branch.FriendlyName);
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
				var branch = RepoUserControl.repo.Branches[activeBranchComboBox.SelectedValue as string];
				if (activeBranch != branch)
				{
					var newBranch = RepoUserControl.repo.Checkout(branch);
					if (newBranch != branch) MessageBox.Show("Error checking out branch (do you have pending changes?)");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Checkout Branch Error: " + ex.Message);
			}
			
			MainWindow.UpdateUI();
		}

		private void mergeButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchComboBox.SelectedIndex < 0)
			{
				MessageBox.Show("Must select 'Other' branch");
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

			try
			{
				RepoUserControl.repo.CreateBranch(branchNameTextBox.Text);
				activeBranchComboBox.Items.Add(branchNameTextBox.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Create mew Branch Error: " + ex.Message);
				return;
			}

			if (activeBranchComboBox.Items.Count != 0) activeBranchComboBox.SelectedIndex = activeBranchComboBox.Items.Count - 1;
		}

		private void deleteBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchComboBox.SelectedItem == null)
			{
				MessageBox.Show("Must select branch");
				return;
			}

			try
			{
				RepoUserControl.repo.Branches.Remove(otherBranchComboBox.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Create mew Branch Error: " + ex.Message);
			}

			MainWindow.UpdateUI();
		}

		private void renameBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(branchNameTextBox.Text))
			{
				MessageBox.Show("Must give the branch a name");
				return;
			}

			try
			{
				RepoUserControl.repo.Branches.Rename(activeBranchComboBox.Text, branchNameTextBox.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Create mew Branch Error: " + ex.Message);
			}

			MainWindow.UpdateUI();
		}
	}
}
