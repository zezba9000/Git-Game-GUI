using LibGit2Sharp;
using System;
using System.Linq;
using System.Windows;
using System.ComponentModel;

namespace GitGameGUI
{
	public partial class CommitWindow : Window
	{
		public CommitWindow()
		{
			InitializeComponent();
			MainWindow.CanInteractWithUI(false);
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
