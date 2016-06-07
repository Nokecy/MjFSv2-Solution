﻿using MjFSv2Lib.Database;
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
		private Dictionary<DriveInfo, DatabaseOperations> driveBagMap;

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
			SynchronizationManager.GetInstance().StartSynchronization(vMan.GetKnownBagConfigs());
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
		}

		void PrintList() {
			CreateDriveBagMap(true);
			int i = 0;
			foreach(KeyValuePair<string, DatabaseOperations> entry in vMan.GetKnownBagConfigs()) {
				Console.WriteLine("[" + i + "] " + entry.Key);
				i++;
			}
		}

		void PrintStats(string[] args) {
			if (args.Length > 0) {
				int index = 0;
				try {
					index = Convert.ToInt32(args[0]);
				} catch (FormatException ex) {
					PrintError(ex);
				} catch (IndexOutOfRangeException ex) {
					PrintError(ex);
				}

				CreateDriveBagMap();

				if (index > vMan.GetKnownBagConfigs().Count - 1) {
					PrintError(new IndexOutOfRangeException());
					return;
				}

				string dinfo = vMan.GetKnownBagConfigs().Keys.ElementAt<string>(index);
				DatabaseOperations op = vMan.GetKnownBagConfigs()[dinfo];

				Console.WriteLine("Volume: " + dinfo);
				Console.WriteLine("Database version: " + op.GetVersion());
				Console.WriteLine("Bag location: " + op.GetLocation());
				Console.WriteLine("Watch status: " + (SynchronizationManager.GetInstance().GetSynchronizedDrives().Contains(dinfo) ? "actively watched" : "not watching"));
			} else {
				PrintError("Please provide an index");
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
				vMan.GetBagConfigurations();
			} else {
				if (vMan.GetKnownBagConfigs() == null) {
					vMan.GetBagConfigurations();
				}
			}
		}

		void AddBagVolume(string[] args) {
			if (args.Length > 0) {
				string path = args[0];
				FileAttributes attr = File.GetAttributes(path);
				if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
					string driveRoot = System.IO.Path.GetPathRoot(path);
					DriveInfo driveInfo = new DriveInfo(driveRoot);
					CreateDriveBagMap();
					if (!driveBagMap.ContainsKey(driveInfo)) {
						vMan.CreateBagVolume(driveInfo, path);
						Console.WriteLine("Succesfully created a bag volume on " + driveInfo.ToString());
					} else {
						PrintError("This volume already has been configured for MjFS");
					}
				} else {
					PrintError("Given path is not a directory");
				}
			} else {
				PrintError("Please provide a path");
			}
		}

		void AddBagVolumeGUI() {
			CreateDriveBagMap();
			FolderBrowserDialog bd = new FolderBrowserDialog();
			bd.Description = "Select a folder which contains all your files and will function as the volume's bag";
			bd.RootFolder = Environment.SpecialFolder.MyComputer;

			if(bd.ShowDialog() == DialogResult.OK) {
				string driveRoot = System.IO.Path.GetPathRoot(bd.SelectedPath);
				DriveInfo driveInfo = new DriveInfo(driveRoot);

				if (!driveBagMap.ContainsKey(driveInfo)) {
					vMan.CreateBagVolume(driveInfo, bd.SelectedPath);
					Console.WriteLine("Successfully created a bag volume on " + driveInfo.ToString());
				} else {
					// Drive is already present
					PrintError("This volume already has been configured for MjFS");
				}
			} else {
				PrintError("Action aborted by user");
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

				if (index > driveBagMap.Count - 1) {
					PrintError(new IndexOutOfRangeException());
					return;
				}

				DriveInfo dinfo = driveBagMap.Keys.ElementAt<DriveInfo>(index);
				vMan.UnmountBagDrive(dinfo);

				File.Delete(dinfo + VolumeMountManager.CONFIG_FILE_NAME);

				CreateDriveBagMap(true);
				Console.WriteLine("Successfully removed bag volume on " + dinfo.ToString());
			} else {
				PrintError("Please provide an index");
			}
		}
	}

	
}
