using MjFSv2Lib.Database;
using MjFSv2Lib.Manager;
using MjFSv2Lib.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
				p.ProcessInput(Console.ReadLine());
			}
		}

		public void ProcessInput(string input) {
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
			} else {
				Console.WriteLine("Unknown command '" + command + "'");
			}
			Console.WriteLine();
			ProcessInput(Console.ReadLine()); // Process next set of input
		}

		void PrintUsage() {
			Console.WriteLine("{0, -30} {1, -40}", new string[] { "ls", "Show a list of all currently present bag volumes" });
			Console.WriteLine("{0, -30} {1, -40}", new string[] { "stat [volume]", "Show detailed status information for a bag volume" });
			Console.WriteLine("{0, -30} {1, -40}", new string[] { "add [path to bag]", "Add a bag volume by specifying the bag location" });
			Console.WriteLine("{0, -30} {1, -40}", new string[] { "add", "Add a bag volume using directory picker dialog" });
			Console.WriteLine("{0, -30} {1, -40}", new string[] { "remove [volume]", "Unregister the volume as bag volume" });
		}

		void PrintList() {
			CreateDriveBagMap(true);
			int i = 0;
			foreach(KeyValuePair<string, DatabaseOperations> entry in vMan.DiscoveredBagVolumes) {
				Console.WriteLine("[" + i + "] " + entry.Key);
				i++;
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
					KeyValuePair<string, DatabaseOperations> entry = GetBagVolumeInfo(args[0]);
					Console.WriteLine("Volume: " + entry.Key);
					Console.WriteLine("Database version: " + entry.Value.GetVersion());
					Console.WriteLine("Bag location: " + entry.Value.GetBagLocation());
				} catch (Exception e) {
					PrintError(e);
				}				
			} else {
				PrintError("Invalid number of arguments");
			}	
		}

		void PrintError(Exception ex) {
			Console.WriteLine("An error occurred! The system reports: \n" + ex.Message);
		}

		void PrintError(string msg) {
			Console.WriteLine("An error occurred! The system reports: " + msg);
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
						CreateDriveBagMap(true);
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
					CreateDriveBagMap(true);
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
				GC.Collect();
				GC.WaitForPendingFinalizers();
				File.Delete(drive + VolumeMountManager.CONFIG_FILE_NAME);

				CreateDriveBagMap(true); // Force update
				Console.WriteLine("Successfully removed bag volume on " + drive);
			} else {
				PrintError("Invalid number of arguments");
			}
		}
	}

	
}
