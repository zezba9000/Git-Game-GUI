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
using System.Net;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GitGameGUI
{
	class VersionNumber
	{
		public int major, minor, patch, build;
	}

	public partial class CheckUpdatesWindow : Window
	{
		private WebClient client;
		#if WINDOWS
		private const string platform = "Windows";
		#elif MAC
		private const string platform = "Mac";
		#elif LINUX
		private const string platform = "Linux";
		#endif

		public CheckUpdatesWindow(string url)
		{
			InitializeComponent();
			MainWindow.CanInteractWithUI(false);

			try
			{
				client = new WebClient();
				client.DownloadStringCompleted += Client_DownloadStringCompleted;
				client.DownloadStringAsync(new Uri(url));
			}
			catch (Exception e)
			{
				MessageBox.Show("Failed to check for updates: " + e.Message);
				MainWindow.CanInteractWithUI(true);
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			MainWindow.CanInteractWithUI(true);
			base.OnClosing(e);
		}

		private VersionNumber GetVersionNumber(string version)
		{
			var result = new VersionNumber();
			var values = version.Split('.');
			int i = 0;
			foreach (var value in values)
			{
				int num = 0;
				int.TryParse(value, out num);
				if (i == 0) result.major = num;
				else if (i == 1) result.minor = num;
				else if (i == 2) result.patch = num;
				else if (i == 3) result.build = num;
				else break;

				++i;
			}

			return result;
		}

		private bool IsValidVersion(string currentVersion, string requiredVersion)
		{
			var v1 = GetVersionNumber(currentVersion);
			var v2 = GetVersionNumber(requiredVersion);
			if (v1.major > v2.major)
			{
				return true;
			}
			else if (v1.major == v2.major)
			{
				if (v1.minor > v2.minor)
				{
					return true;
				}
				else
				{
					if (v1.patch > v2.patch)
					{
						return true;
					}
					else
					{
						if (v1.build >= v2.build)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		private void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				MessageBox.Show("Failed to check for updates: " + e.Error.Message);
				client.Dispose();
				MainWindow.CanInteractWithUI(true);
				return;
			}

			if (e.Cancelled)
			{
				MessageBox.Show("Update check canceled!");
				client.Dispose();
				MainWindow.CanInteractWithUI(true);
				return;
			}

			try
			{
				// get git and lfs versions
				bool canCheckGit = true, canCheckGitLFS = true;
				string gitVersion = Tools.RunExeOutput("git", "version", null);
				string gitlfsVersion = Tools.RunExeOutput("git-lfs", "version", null);
				string gitlfsRequiredVersion = "0.0.0.0";

				var match = Regex.Match(gitVersion, @"git version (.*)\.windows");
				if (match.Success && match.Groups.Count == 2) gitVersion = match.Groups[1].Value;
				else canCheckGit = false;

				match = Regex.Match(gitlfsVersion, @"git-lfs/(.*) \(GitHub; windows amd64; go (.*); git ");
				if (match.Success && match.Groups.Count == 3)
				{
					gitlfsVersion = match.Groups[1].Value;
					gitlfsRequiredVersion = match.Groups[2].Value;
				}
				else canCheckGitLFS = false;

				// make sure the git version installed is supporeted by lfs
				if (!IsValidVersion(gitVersion, gitlfsRequiredVersion))
				{
					MessageBox.Show(string.Format("'git-lfs' version is not compatible with 'git' version installed!"));
					client.Dispose();
					Close();
					MainWindow.singleton.Close();
					return;
				}

				// check versions
				bool canCheckAppVersion = true;
				using (var reader = new StringReader(e.Result))
				using (var xmlReader = new XmlTextReader(reader))
				{
					while (xmlReader.Read())
					{
						if (canCheckAppVersion && xmlReader.Name == "AppVersion")
						{
							canCheckAppVersion = false;
							if (!IsValidVersion(VersionInfo.version, xmlReader.ReadInnerXml()))
							{
								MessageBox.Show("Your 'Git-Game-GUI' version is out of date.");
								Process.Start("http://reign-studios-services.com/GitGameGUI/index.html");
							}
						}
						else if (canCheckGit && xmlReader.Name == "GitVersion")
						{
							while (xmlReader.Read())
							{
								if (canCheckGit && xmlReader.Name == platform)
								{
									canCheckGit = false;
									if (!IsValidVersion(gitVersion, xmlReader.ReadInnerXml()))
									{
										MessageBox.Show("Your 'git' version is out of date.");
										Process.Start("https://git-scm.com/downloads");
									}
								}

								if (xmlReader.Name == "GitVersion") break;
							}
						}
						else if (canCheckGitLFS && xmlReader.Name == "Git_LFS_Version")
						{
							while (xmlReader.Read())
							{
								if (canCheckGitLFS && xmlReader.Name == platform)
								{
									canCheckGitLFS = false;
									if (!IsValidVersion(gitlfsVersion, xmlReader.ReadInnerXml()))
									{
										MessageBox.Show("Your 'git-lfs' version is out of date.");
										Process.Start("https://git-lfs.github.com/");
									}
								}

								if (xmlReader.Name == "GitVersion") break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to get version info!\nMake sure git and git-lfs are installed\nAlso make sure you're connected to the internet: \n\n" + ex.Message);
			}

			client.Dispose();
			Close();
		}
	}
}
