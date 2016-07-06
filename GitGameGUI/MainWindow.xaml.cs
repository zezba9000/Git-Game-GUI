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

namespace GitGameGUI
{
	public delegate void UpdateUICallbackMethod();

	public partial class MainWindow : Window
	{
		public static MainWindow singleton;
		private int lastSelectedTab = -1;

		public static UpdateUICallbackMethod UpdateUICallback, FinishedUpdatingUICallback;
		public static bool uiUpdating;
		public static XML.AppSettings appSettings;

		public MainWindow()
		{
			singleton = this;
			InitializeComponent();

			// load settings
			appSettings = Settings.Load<XML.AppSettings>(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\GitGameGUI\\Settings.xml");
			if (appSettings.defaultGitLFS_Exts.Count == 0)
			{
				appSettings.defaultGitLFS_Exts.AddRange(new List<string>()
				{
					".psd", ".jpg", ".jpeg", ".png", ".bmp", ".tga",// image types
					".mpeg", ".mov", ".avi", ".mp4", ".wmv",// video types
					".wav", ".mp3", ".ogg", ".wma", ".acc",// audeo types
					".zip", ".7z", ".rar", ".tar", ".gz",// compression types
					".bin", ".data", ".raw", ".hex",// unknown binary types
				});
			}
			
			UpdateUI();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			RepoUserControl.Dispose();
			Settings.Save<XML.AppSettings>(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\GitGameGUI\\Settings.xml", appSettings);
			base.OnClosing(e);
		}

		public static void UpdateUI()
		{
			uiUpdating = true;
			if (UpdateUICallback != null) UpdateUICallback();
			uiUpdating = false;

			if (FinishedUpdatingUICallback != null) FinishedUpdatingUICallback();
		}

		public static void CanInteractWithUI(bool enabled)
		{
			singleton.tabControl.IsEnabled = enabled;
		}

		private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (tabControl.SelectedIndex != 3 && lastSelectedTab == 3)
			{
				RepoUserControl.SaveSettings();
				UpdateUI();
			}

			lastSelectedTab = tabControl.SelectedIndex;
		}
	}
}
