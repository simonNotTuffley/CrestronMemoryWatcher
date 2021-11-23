using System;
using Crestron.SimplSharp;                              // For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                           // For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread; // For Threading
using Crestron.SimplSharpPro.Diagnostics;               // For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport; // For Generic Device Support
using Serilog;


namespace MemoryWatcher
{
	public class MemoryWatcherControlSystem : CrestronControlSystem
	{
		private Thread _thread;


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
				InitialiseSeriLogAndSeq();

				CrestronConsole.PrintLine("Logger Created");

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

		public void InitialiseSeriLogAndSeq()
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug() //Most sinks get this level and and 
				.Enrich.FromLogContext() //Takes in the { } stuff
				.WriteTo.Seq("http://54.153.113.63:5341/", apiKey: "MUrWSQdZlCH3ALTMxsmv").CreateLogger()
				.ForContext("InstallationFriendlyName", "MemoryWatcherTT");
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
				PrintMemoryUsageToSeq();
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

		private void PrintMemoryUsageToSeq()
		{
			Log
				.ForContext("CPUUtilization", SystemMonitor.CPUUtilization)
				.ForContext("CPUUtilizationRAW", SystemMonitor.CPUUtilizationRAW)
				.ForContext("MaximumCPUUtilization", SystemMonitor.MaximumCPUUtilization)
				.ForContext("RAMFree", SystemMonitor.RAMFree)
				.ForContext("RAMFreeMinimum", SystemMonitor.RAMFreeMinimum)
				.ForContext("TotalRAMSize ", SystemMonitor.TotalRAMSize)
				.Debug("MemoryWatchReport");
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