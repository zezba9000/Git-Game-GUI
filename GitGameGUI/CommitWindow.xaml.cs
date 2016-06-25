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
using System.Windows.Shapes;
using System.ComponentModel;

namespace GitGameGUI
{
	public enum CommitWindowTypes
	{
		CommitOnly,
		CommitPullPush
	}

	public partial class CommitWindow : Window
	{
		private CommitWindowTypes type;

		public CommitWindow(CommitWindowTypes type)
		{
			InitializeComponent();
			MainWindow.CanInteractWithUI(false);
			this.type = type;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			MainWindow.CanInteractWithUI(true);
			base.OnClosing(e);
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void commitButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(messageTextBox.Text))
			{
				MessageBox.Show("Must enter a commit message");
				return;
			}

			try
			{
				RepoUserControl.repo.Commit(messageTextBox.Text, RepoUserControl.signature, RepoUserControl.signature);

				if (type == CommitWindowTypes.CommitPullPush)
				{
					ChangesUserControl.Pull();
					if (RepoUserControl.repo.Index.Conflicts.Count() == 0) ChangesUserControl.Push();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to commit: " + ex.Message);
				return;
			}

			MainWindow.UpdateUI();
			Close();
		}
	}
}
