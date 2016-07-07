﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GitGameGUI
{
	public enum MergeBinaryResults
	{
		KeepMine,
		UserTheirs,
		Cancel
	}

	public partial class MergeBinaryFileWindow : Window
	{
		public MergeBinaryResults result = MergeBinaryResults.Cancel;
		private bool isClosed;
		
		public MergeBinaryFileWindow(string fileInConflict)
		{
			InitializeComponent();
			fileInConflictTextBox.Text = fileInConflict;
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			isClosed = true;
		}

		private Task IsWindowClosedTask()
		{
			while (!isClosed) Thread.Sleep(1);
			return Task.CompletedTask;
		}

		public async Task WaitForClose()
		{
			Func<Task> foo = IsWindowClosedTask;
			await Task.Run(foo);
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			result = MergeBinaryResults.Cancel;
			Close();
		}

		private void keepMineButton_Click(object sender, RoutedEventArgs e)
		{
			result = MergeBinaryResults.KeepMine;
			Close();
		}

		private void useTheirsButton_Click(object sender, RoutedEventArgs e)
		{
			result = MergeBinaryResults.UserTheirs;
			Close();
		}
	}
}
