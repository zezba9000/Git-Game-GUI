using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace GitGameGUI
{
	namespace XML
	{
		public class Repository
		{
			[XmlText] public string path = "";
		}

		[XmlRoot("AppSettings")]
		public class AppSettings
		{
			[XmlAttribute("MergeDiffTool")] public string mergeDiffTool = "Meld";
			[XmlElement("Repository")] public List<Repository> repositories = new List<Repository>();
		}

		[XmlRoot("RepoSettings")]
		public class RepoSettings
		{
			[XmlAttribute("SignatureName")] public string signatureName = "First Last";
			[XmlAttribute("SignatureEmail")] public string signatureEmail = "username@email.com";
			[XmlAttribute("ValidateLFS")] public bool validateLFS = true;
			[XmlAttribute("ValidateGitignore")] public bool validateGitignore = true;
			[XmlAttribute("AutoCommit")] public bool autoCommit = true;
			[XmlAttribute("AutoPush")] public bool autoPush = true;
		}
	}

	static class Settings
	{
		public static T Load<T>(string filename) where T : new()
		{
			if (!File.Exists(filename)) return new T();

			try
			{
				var xml = new XmlSerializer(typeof(T));
				using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					return (T)xml.Deserialize(stream);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("Load Settings Error: " + e.Message);
				return new T();
			}
		}

		public static bool Save<T>(string filename, T settings)
		{
			string path = Path.GetDirectoryName(filename);
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);

			try
			{
				var xml = new XmlSerializer(typeof(T));
				using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					xml.Serialize(stream, settings);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("Save Settings Error: " + e.Message);
				return false;
			}

			return true;
		}
	}
}
