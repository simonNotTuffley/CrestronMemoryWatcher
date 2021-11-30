using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Serilog; // For Basic SIMPL# Classes
			   // For Basic SIMPL#Pro classes
			   // For Threading
			   // For System Monitor Access
			   // For Generic Device Support


namespace MemoryWatcher.VCBox
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
				.WriteTo.Seq("http://54.153.113.63:5341/", apiKey: "iSOd6WceAdgrKzMI60HB").CreateLogger()
				.ForContext("InstallationFriendlyName", "VirtualMachineMemTracker1_fromGitHub");
		}

		private object threadCallbackFunc(object passedObject)
		{

			int count = 0;
			var interval = TimeSpan.FromSeconds(30);
			//	SystemMonitor.SetUpdateInterval(Convert.ToUInt16(interval.TotalSeconds));
			while (true)
			{
				try
				{
					count++;
					CrestronConsole.PrintLine($"threadCallbackFunc called for the {count} time");
					CrestronConsole.PrintLine("");
					PrintMemoryUsageToConsole();
					PrintMemoryUsageToSeq();
					CrestronConsole.PrintLine("***");
					Thread.Sleep(Convert.ToInt32(interval.TotalMilliseconds));
				}
				catch (Exception ex)
				{
					CrestronConsole.PrintLine("Had an exception, ignoring and going again");
					CrestronConsole.PrintLine(ex.Message);
				}
			}

			CrestronConsole.PrintLine("loop exiting");

			return null;
		}

		private void PrintMemoryUsageToConsole()
		{
			CrestronConsole.PrintLine("RAMFree " + CrestronEnvironment.SystemInfo.RamFree);
			CrestronConsole.PrintLine("HardwareVersion " + CrestronEnvironment.SystemInfo.HardwareVersion);
			CrestronConsole.PrintLine("SerialNumber " + CrestronEnvironment.SystemInfo.SerialNumber);
			CrestronConsole.PrintLine("TotalRamSize " + CrestronEnvironment.SystemInfo.TotalRamSize);
		}

		private void PrintMemoryUsageToSeq()
		{
			Log
				.ForContext("RAMFree", CrestronEnvironment.SystemInfo.RamFree)
				.ForContext("HardwareVersion", CrestronEnvironment.SystemInfo.HardwareVersion)
				.ForContext("SerialNumber", CrestronEnvironment.SystemInfo.SerialNumber)
				.ForContext("TotalRamSize", CrestronEnvironment.SystemInfo.TotalRamSize)

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