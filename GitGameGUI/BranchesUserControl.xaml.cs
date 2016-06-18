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

namespace GitGUI
{
	public partial class BranchesUserControl : UserControl
	{
		public static Branch activeBranch;

		public BranchesUserControl()
		{
			InitializeComponent();
			MainWindow.UpdateUICallback += UpdateUI;
		}

		private void UpdateUI()
		{
			// clear ui
			activeBranchComboBox.Items.Clear();
			
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
			
			workingBranchComboBox_SelectionChanged(null, null);
		}

		private void workingBranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
				var sig = new Signature("Andrew Witte", "zezba9000@gmail.com", DateTimeOffset.UtcNow);
				RepoUserControl.repo.Merge(srcBround, sig);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Merge Branch Error: " + ex.Message);
			}

			MainWindow.UpdateUI();

			// TODO: check for merge issues
		}

		private void addNewBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(newBranchTextBox.Text))
			{
				MessageBox.Show("Must give the branch a name");
				return;
			}

			try
			{
				RepoUserControl.repo.CreateBranch(newBranchTextBox.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Create mew Branch Error: " + ex.Message);
			}

			MainWindow.UpdateUI();
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
			if (string.IsNullOrEmpty(newBranchTextBox.Text))
			{
				MessageBox.Show("Must give the branch a name");
				return;
			}

			try
			{
				RepoUserControl.repo.Branches.Rename(activeBranchComboBox.Text, newBranchTextBox.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Create mew Branch Error: " + ex.Message);
			}

			MainWindow.UpdateUI();
		}
	}
}
