using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
		
		public string fileInConflict
		{
			set {fileInConflictTextBox.Text = value;}
		}

		public MergeBinaryFileWindow()
		{
			InitializeComponent();
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
