using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO; // For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                           // For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread; // For Threading
using Crestron.SimplSharpPro.Diagnostics;               // For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport; // For Generic Device Support


namespace MemoryWatcher
{
	public class MemoryWatcherControlSystem : CrestronControlSystem
	{
		private Thread _thread;
		private const string fileLocation = "\\nvram\\MemWatcher.csv";

		public MemoryWatcherControlSystem() : base()
		{
			try
			{
				Thread.MaxNumberOfUserThreads = 20;

				//Subscribe to the controller events (System, Program, and Ethernet)
				CrestronEnvironment.SystemEventHandler += new SystemEventHandler(_ControllerSystemEventHandler);
				CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(_ControllerProgramEventHandler);
				CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(_ControllerEthernetEventHandler);
			}
			catch (Exception e)
			{
				ErrorLog.Error("Error in the constructor: {0}", e.Message);
			}
		}

		public override void InitializeSystem()
		{
			try
			{
				var header = "DateTime,SystemMonitor.CPUUtilization,SystemMonitor.CPUUtilizationRAW,SystemMonitor.MaximumCPUUtilization,SystemMonitor.RAMFree,SystemMonitor.RAMFreeMinimum,SystemMonitor.TotalRAMSize";
				using (var csvFile = File.AppendText(fileLocation))
				{
					csvFile.WriteLine(header);
				}
				CrestronConsole.PrintLine("File Created");
				
				CrestronConsole.PrintLine("Creating thread");
				_thread = new Thread(threadCallbackFunc, null, Thread.eThreadStartOptions.CreateSuspended)
				{
					Priority = Thread.eThreadPriority.LowestPriority
				};

				CrestronConsole.PrintLine("Starting thread");
				_thread.Start();
			}
			catch (Exception e)
			{
				ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
			}
		}
		private object threadCallbackFunc(object passedObject)
		{
			int count = 0;
			var interval = TimeSpan.FromSeconds(30);
			SystemMonitor.SetUpdateInterval(Convert.ToUInt16(interval.TotalSeconds));
			while (true)
			{
				count++;
				CrestronConsole.PrintLine($"threadCallbackFunc called for the {count} time");
				CrestronConsole.PrintLine("");
				PrintMemoryUsageToConsole();
				PrintMemoryUsageToCsv();
				CrestronConsole.PrintLine("***");
				Thread.Sleep(Convert.ToInt32(interval.TotalMilliseconds));
			}
		}

		private void PrintMemoryUsageToConsole()
		{
			CrestronConsole.PrintLine("CPUUtilization " + SystemMonitor.CPUUtilization);
			CrestronConsole.PrintLine("CPUUtilizationRAW " + SystemMonitor.CPUUtilizationRAW);
			CrestronConsole.PrintLine("MaximumCPUUtilization " + SystemMonitor.MaximumCPUUtilization);
			CrestronConsole.PrintLine("RAMFree " + SystemMonitor.RAMFree);
			CrestronConsole.PrintLine("RAMFreeMinimum " + SystemMonitor.RAMFreeMinimum);
			CrestronConsole.PrintLine("TotalRAMSize " + SystemMonitor.TotalRAMSize);
		}

		private void PrintMemoryUsageToCsv()
		{
			var dateString = DateTime.UtcNow.ToString("G");
			
			var row = $"{dateString},{SystemMonitor.CPUUtilization},{SystemMonitor.CPUUtilizationRAW},{SystemMonitor.MaximumCPUUtilization},{SystemMonitor.RAMFree},{SystemMonitor.RAMFreeMinimum},{SystemMonitor.TotalRAMSize}";
			
			using (var csvFile = File.AppendText(fileLocation))
			{
				csvFile.WriteLine(row);
			}
		}

		void _ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
		{
			CrestronConsole.PrintLine("_ControllerEthernetEventHandler called");
		}

		void _ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
		{
			CrestronConsole.PrintLine("_ControllerProgramEventHandler called");
			if (programStatusEventType == eProgramStatusEventType.Stopping)
			{
				_thread.Abort();
			}
		}

		void _ControllerSystemEventHandler(eSystemEventType systemEventType)
		{
			CrestronConsole.PrintLine("_ControllerSystemEventHandler called");

		}
	}
}