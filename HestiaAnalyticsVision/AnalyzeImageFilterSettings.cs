using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HestiaAnalyticsVision
{
	public class AnalyzeImageFilterSettings
	{
		public string[] Ignore;
		public Dictionary<string, float> MinimumConfidence;
		public double MinConfidence;
		public double MinDescriptionConfidence;
	}
}
