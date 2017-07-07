using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManyConsole;

namespace HestiaAnalyticsCLI
{
	public class Program
	{
		static IEnumerable<ConsoleCommand> Commands;

		static int RunNextCommand(string[] Arguments)
		{
			return ConsoleCommandDispatcher.DispatchCommand(Commands, Arguments, Console.Out);
		}

		static int Main(string[] Arguments)
		{
			Commands = ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));

			return RunNextCommand(Arguments);
		}
	}
}
