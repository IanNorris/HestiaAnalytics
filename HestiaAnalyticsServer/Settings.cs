using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HestiaAnalyticsServer
{
	public class AzureCognitiveAPI
	{
		public string RegionName;
		public string SubscriptionKey;
		public string CertificateAuthorityThumbprint;
	}

	public class SighthoundAPI
	{
		public string Hostname;
		public uint Port;
		public string Username;
		public string Password;
		public string CertificateAuthorityThumbprint;
	}

	public class Settings
	{
		public AzureCognitiveAPI AzureCognitiveAPI;
		public SighthoundAPI SighthoundAPI;

		public static Settings LoadSettings( string AppName )
		{
			string ProgramData1 = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			string ProgramData2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string SettingsFilename;
			string SettingsFilename1 = Path.Combine(ProgramData1, AppName, "Settings.json");
			string SettingsFilename2 = Path.Combine(ProgramData2, AppName, "Settings.json");

			string SettingsData;
			if (File.Exists(SettingsFilename1))
			{
				SettingsFilename = SettingsFilename1;
				SettingsData = File.ReadAllText(SettingsFilename);
			}
			else if (File.Exists(SettingsFilename2))
			{
				SettingsFilename = SettingsFilename2;
				SettingsData = File.ReadAllText(SettingsFilename);
			}
			else
			{
				System.Console.Error.WriteLine($"Settings file could not be found at \"{SettingsFilename1}\" or \"{SettingsFilename2}\".");
				System.Environment.Exit(1);
				return null;
			}

			Settings Settings = JsonConvert.DeserializeObject<Settings>(SettingsData);
			if (Settings == null)
			{
				System.Console.Error.WriteLine($"Unable to process settings file could not be found at {SettingsFilename}");
				return null;
			}
			else
			{
				return Settings;
			}
		}
	}
}
