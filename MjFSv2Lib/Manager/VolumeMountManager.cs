﻿using DokanNet;
using MjFSv2Lib.Database;
using MjFSv2Lib.FileSystem;
using MjFSv2Lib.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;

namespace MjFSv2Lib.Manager {
	public class VolumeMountManager {
		private static VolumeMountManager instance = new VolumeMountManager();
		public static readonly string CONFIG_FILE_NAME = "BagConf.sqlite";
		private static readonly MjFileSystemOperations fileSystem = new MjFileSystemOperations();

		private bool _mounted = false;
		private DatabaseManager dbMan = DatabaseManager.GetInstance();

		private Dictionary<string, DatabaseOperations> _driveBagMap;
		private Dictionary<string, DatabaseOperations> _knownDriveBagConfigs;

		private VolumeMountManager() {
			_driveBagMap = new Dictionary<string, DatabaseOperations>();

			// Register eventhandler for incoming USB devices
			var watcher = new ManagementEventWatcher();
			var query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
			watcher.EventArrived += new EventArrivedEventHandler(OnDeviceChangedEvent);
			watcher.Query = query;
			watcher.Start();
		}

		public static VolumeMountManager GetInstance() {
			return instance;
		}

		private void OnDeviceChangedEvent(object sender, EventArrivedEventArgs e) {
			DebugLogger.Log("Volume (un)mounted");
			MountAllBags();
		}

		/// <summary>
		/// Retrieve a list of all drives with a bag
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, DatabaseOperations> GetMountedBagDrives() {
			return new Dictionary<string, DatabaseOperations>(_driveBagMap);
		}

		/// <summary>
		/// Removes the volume from the mounted bags and closes the associated database connection
		/// </summary>
		/// <param name="dinfo"></param>
		public void UnmountBagDrive(DriveInfo dinfo) {
			DebugLogger.Log("Unmounting bag on volume " + dinfo);
			DatabaseOperations op;
			if (_knownDriveBagConfigs.TryGetValue(dinfo.ToString(), out op)) {
				dbMan.CloseConnection(op);
				_driveBagMap.Remove(dinfo.ToString());
			} else {
				DebugLogger.Log("This volume is not known to the volume manger and can therefore not be unmounted");
			}
		}

		/// <summary>
		/// Get a map of all volumes with a connection to their database as it currently is.
		/// </summary>
		/// <returns>Null if no configurations are currently known</returns>
		public Dictionary<string, DatabaseOperations> GetKnownBagConfigs() {
			
			if (_knownDriveBagConfigs == null) {
				return null;
			}
			DebugLogger.Log("Total of " + _knownDriveBagConfigs.Count + " known bag configs");
			return new Dictionary<string, DatabaseOperations>(_knownDriveBagConfigs);
		}

		/// <summary>
		/// Get an updated map of all volumes with a connection to their database
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, DatabaseOperations> GetBagConfigurations() {
			Dictionary<string, DatabaseOperations> result = new Dictionary<string, DatabaseOperations>();
			// Scan all volumes for a configuration file
			foreach (DriveInfo dinfo in DriveInfo.GetDrives()) {
				DatabaseOperations op;

				// If the drive was already registered add the old db connection
				if (_driveBagMap.TryGetValue(dinfo.ToString(), out op)) {
					result.Add(dinfo.ToString(), op);
				} else {
					try {
						foreach (FileInfo finfo in dinfo.RootDirectory.GetFiles()) {
							if (finfo.Name == CONFIG_FILE_NAME) {
								// Found a configuration file
								DebugLogger.Log("Configuration on drive " + dinfo);
								// Open a connection to the database
								op = dbMan.OpenConnection(dinfo + CONFIG_FILE_NAME);
								result.Add(dinfo.ToString(), op);
								break;
							}
						}
					} catch (IOException) {
						DebugLogger.Log("Unable to scan files on drive " + dinfo);
					}
				}

			}
			_knownDriveBagConfigs = result;
			return result;
		}

		/// <summary>
		/// Scan all volumes for bag configurations and mount them to the main volume
		/// </summary>
		public void MountAllBags() {
			// Replace the current driveBag map with an updated one.
			_driveBagMap = GetBagConfigurations();
		}

		/// <summary>
		/// Mount the main value hosting all the bags
		/// </summary>
		public void Mount() {
			if (!_mounted) {
				// The filesystem only has to be mounted once!
				_mounted = true;
				string driveLetter = Helper.GetFreeDriveLetters()[0].ToString() + ":\\";
				Console.WriteLine("Mounted MJFS volume to drive " + driveLetter);
				// Note the method below is blocking so code after this VV line will not be executed
				fileSystem.Drive = driveLetter;
				fileSystem.Mount(driveLetter, DokanOptions.DebugMode | DokanOptions.FixedDrive);
			} else {
				DebugLogger.Log("Volume has already been mounted!");
			}
		}

		/// <summary>
		/// Instantiate a new bag configuration on the given volume
		/// </summary>
		/// <param name="dinfo"></param>
		public void CreateBagVolume(DriveInfo dinfo, string bagLocation) {
			DatabaseOperations op = dbMan.OpenConnection(dinfo + CONFIG_FILE_NAME);
			op.AddTables(bagLocation.Replace(dinfo.ToString(), ""));
			op.UpdateHash();
		}
	}
}
