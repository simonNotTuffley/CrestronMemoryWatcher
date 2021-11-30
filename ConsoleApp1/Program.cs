using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryWatcher.VCBox;

namespace ConsoleApp1
{
	class Program
	{
		static void Main(string[] args)
		{
			var memoryWatcherControlSystem = new MemoryWatcherControlSystem();

			memoryWatcherControlSystem.InitializeSystem();
		}
	}
}
