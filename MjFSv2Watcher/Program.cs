using MjFSv2Lib.Database;
using MjFSv2Lib.Manager;
using MjFSv2Lib.Model;
using MjFSv2Lib.Util;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MjFSv2Watcher {
	class Program {
		private VolumeMountManager vMan = VolumeMountManager.GetInstance();

		[STAThread]
		static void Main(string[] args) {
			Console.WriteLine("MjFS v2 Watcher - An SQL based file system");
			Console.WriteLine();
			if (!Helper.IsUserAdministrator()) {
				Console.WriteLine("Please run this program with administrator privilleges.");
				Console.Read();
			} else {
				Program p = new Program();
				p.CreateDriveBagMap();
				p.CreateWatcher();
				p.ProcessInput(Console.ReadLine());
			}
		}

		public void CreateWatcher() {
			CreateDriveBagMap();
			SynchronizationManager.GetInstance().StartSynchronization(vMan.DiscoveredBagVolumes);
		}

		public void ProcessInput(string input) {
			Console.WriteLine("Processsing input '" + input + "'");

			string[] cInput = input.ToLower().Split(new char[] { ' ' });

			string command = cInput[0];
			string[] args = new string[cInput.Length -1];
			
			Array.Copy(cInput, 1, args, 0, cInput.Length - 1);

			if (command == "help") {
				PrintUsage();
			} else if (command == "ls") {
				PrintList();
			} else if (command == "add") {
				if (args.Length > 0) {
					AddBagVolume(args);
				} else {
					AddBagVolumeGUI();
				}
			} else if (command == "stat") {
				PrintStats(args);
			} else if (command == "remove") {
				DeleteBagVolume(args);
			} else if (command == "synch") {
				Synch(args);
			} else {
				Console.WriteLine("Unknown command '" + command + "'");
			}
			Console.WriteLine();
			ProcessInput(Console.ReadLine());
		}

		void PrintUsage() {
			Console.WriteLine("ls -- list all volumes configured for MjFS");
			Console.WriteLine("stat [volume index] -- show detailed status of the given volume");
			Console.WriteLine("add [path to dir] -- configure the given drive for MjFS with the given folder as bag");
			Console.WriteLine("add -- configure any drive for MjFS");
			Console.WriteLine("remove [volume index] -- remove the MjFS configuration of the given volume");
			Console.WriteLine("synch [volume index] [on|off]");
		}

		void PrintList() {
			CreateDriveBagMap(true);
			int i = 0;
			foreach(KeyValuePair<string, DatabaseOperations> entry in vMan.DiscoveredBagVolumes) {
				Console.WriteLine("[" + i + "] " + entry.Key);
				i++;
			}
		}

		void Synch(string[] args) {
			if (args.Length == 2) {
				KeyValuePair<string, DatabaseOperations> entry = GetBagVolumeInfo(args[0]);
				SynchronizationManager synchMan = SynchronizationManager.GetInstance();
				if (args[1].ToLower() == "on") {
					synchMan.StartSynchronization(entry);
				} else if (args[1].ToLower() == "off") {
					synchMan.StopSynchronization(entry.Key);
				} else {
					PrintError("Invalid argument '" + args[1] + "'");
				}

			} else {
				PrintError("Invalid amount of arguments");
			}
		}

		KeyValuePair<string, DatabaseOperations> GetBagVolumeInfo(string input) {
			int index = 0;
			try {
				index = Convert.ToInt32(input);
			} catch (FormatException ex) {	
				return new KeyValuePair<string, DatabaseOperations>(input.ToUpper(), vMan.DiscoveredBagVolumes[input.ToUpper()]);
			} 
			return GetBagVolumeFromIndex(index);
		}

		KeyValuePair<string, DatabaseOperations> GetBagVolumeFromIndex(int index) {
			Dictionary<string, DatabaseOperations> bagVolumes = vMan.DiscoveredBagVolumes;
			CreateDriveBagMap();

			if (index > bagVolumes.Count - 1) {
				PrintError(new IndexOutOfRangeException());
				return new KeyValuePair<string, DatabaseOperations>();
			}

			string dinfo = bagVolumes.Keys.ElementAt<string>(index);
			DatabaseOperations op = bagVolumes[dinfo];

			return new KeyValuePair<string, DatabaseOperations>(dinfo, op);
		}

		void PrintStats(string[] args) {
			if (args.Length > 0) {
				try {
					KeyValuePair<string, DatabaseOperations> volPair = GetBagVolumeInfo(args[0]);

					Console.WriteLine("Volume: " + volPair.Key);
					Console.WriteLine("Database version: " + volPair.Value.GetVersion());
					Console.WriteLine("Bag location: " + volPair.Value.GetLocation());
					Console.WriteLine("Synch status: " + (SynchronizationManager.GetInstance().SynchronizedBagVolumes.Contains(volPair.Key) ? "synchronized" : "not synchronized"));
				} catch (Exception e) {
					PrintError(e);
				}				
			} else {
				PrintError("Invalid number of arguments");
			}

			
		}

		void PrintError(Exception ex) {
			Console.WriteLine("An error occurred! The system reports the following: \n" + ex.Message);
		}

		void PrintError(string msg) {
			Console.WriteLine("An error occurred! The system reports the following: " + msg);
		}

		/// <summary>
		/// Create the map of drives to database operations. If the map already exists, skip.
		/// </summary>
		void CreateDriveBagMap() {
			CreateDriveBagMap(false);
		}

		/// <summary>
		/// Create the map of drives to database operations
		/// </summary>
		/// <param name="force">Force create the map in case it did already exist</param>
		void CreateDriveBagMap(bool force) {
			if (force) {
				vMan.DiscoverBagVolumes();
			} else {
				if (vMan.DiscoveredBagVolumes == null) {
					vMan.DiscoverBagVolumes();
				}
			}
		}

		void AddBagVolume(string[] args) {
			if (args.Length > 0) {
				string path = args[0];
				FileAttributes attr = File.GetAttributes(path);
				if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
					string drive = System.IO.Path.GetPathRoot(path);
					try {
						vMan.CreateBagVolume(drive, path);
						Console.WriteLine("Successfully created a bag volume on " + drive);
					} catch (VolumeMountManagerException e) {
						PrintError(e);
					}
				} else {
					PrintError("Given path is not a directory");
				}
			} else {
				PrintError("Invalid number of arguments");
			}
		}

		void AddBagVolumeGUI() {
			FolderBrowserDialog bd = new FolderBrowserDialog();
			bd.Description = "Select a folder which contains all your files and will function as the volume's bag";
			bd.RootFolder = Environment.SpecialFolder.MyComputer;

			if(bd.ShowDialog() == DialogResult.OK) {
				string drive = System.IO.Path.GetPathRoot(bd.SelectedPath);		
				try {
					vMan.CreateBagVolume(drive, bd.SelectedPath);
					Console.WriteLine("Successfully created a bag volume on " + drive);
				} catch (VolumeMountManagerException e) {
					PrintError(e);
				}
			} 
		}

		void DeleteBagVolume(string[] args) {
			if (args.Length > 0) {
				int index = 0;
				try {
					index = Convert.ToInt32(args[0]);
				} catch (FormatException ex) {
					PrintError(ex);
				}

				CreateDriveBagMap();

				if (index > vMan.DiscoveredBagVolumes.Count - 1) {
					PrintError(new IndexOutOfRangeException());
					return;
				}

				string drive = vMan.DiscoveredBagVolumes.Keys.ElementAt<string>(index);
				vMan.UnmountBagVolume(drive);

				File.Delete(drive + VolumeMountManager.CONFIG_FILE_NAME);

				CreateDriveBagMap(true); // Force update
				Console.WriteLine("Successfully removed bag volume on " + drive);
			} else {
				PrintError("Invalid number of arguments");
			}
		}
	}

	
}
