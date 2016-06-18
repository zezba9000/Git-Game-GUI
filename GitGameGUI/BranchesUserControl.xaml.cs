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
	public delegate void BranchChangedCallbackMethod();

	public partial class BranchesUserControl : UserControl
	{
		public static event BranchChangedCallbackMethod BranchChangedCallback;
		public static Branch activeBranch;
		private bool canTriggerBranchChange;

		public BranchesUserControl()
		{
			InitializeComponent();

			RepoUserControl.RepoChangedCallback += RepoChanged;
		}

		private void RepoChanged()
		{
			canTriggerBranchChange = false;
			workingBranchComboBox.Items.Clear();
			
			try
			{
				foreach (var branch in RepoUserControl.repo.Branches)
				{
					if (branch.IsRemote) continue;
					int i = workingBranchComboBox.Items.Add(branch.FriendlyName);
					otherBranchComboBox.Items.Add(branch.FriendlyName);
					if (branch.IsCurrentRepositoryHead)
					{
						workingBranchComboBox.SelectedIndex = i;
						activeBranch = branch;
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("Refresh Branches Error: " + e.Message);
			}

			canTriggerBranchChange = true;
			workingBranchComboBox_SelectionChanged(null, null);
		}

		public static void BranchChanged()
		{
			if (BranchChangedCallback != null) BranchChangedCallback();
		}

		private void workingBranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!canTriggerBranchChange) return;

			try
			{
				var branch = RepoUserControl.repo.Branches[workingBranchComboBox.SelectedValue as string];
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
			
			BranchChanged();
		}

		private void mergeButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchComboBox.SelectedIndex < 0)
			{
				MessageBox.Show("Must select 'Source' branch");
				return;
			}

			try
			{
				var srcBround = RepoUserControl.repo.Branches[otherBranchComboBox.SelectedValue as string];
				var sig = new Signature("Andrew Witte", "zezba9000@gmail.com", DateTimeOffset.UtcNow);
				RepoUserControl.repo.Merge(srcBround, sig);
				BranchChanged();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Merge Branch Error: " + ex.Message);
			}

			// TODO: check for merge issues
		}
	}
}
