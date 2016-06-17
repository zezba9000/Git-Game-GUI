using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.ComponentModel;

namespace GitGUI
{
	public delegate void SettingsChangedCallbackMethod();

	public partial class MainWindow : Window
	{
		public static SettingsChangedCallbackMethod SettingsChangedCallback;
		public static XML.AppSettings appSettings;

		public MainWindow()
		{
			InitializeComponent();

			// load settings
			appSettings = Settings.Load<XML.AppSettings>(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\GitGameGUI\\Settings.xml");
			if (SettingsChangedCallback != null) SettingsChangedCallback();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			RepoUserControl.Dispose();
			Settings.Save<XML.AppSettings>(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\GitGameGUI\\Settings.xml", appSettings);
			base.OnClosing(e);
		}
	}
}
