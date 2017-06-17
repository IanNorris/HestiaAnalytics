using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SighthoundAPI
{
	public class SighthoundSettings
	{
		public string Hostname;
		public uint Port;
		public string Username;
		public string Password;
		public string CertificateAuthorityThumbprint;

		public bool AllowAllObjectsRule;

		public int UpdateStateIntervalMS;
		public int UpdateIntervalMS;
	}
}
