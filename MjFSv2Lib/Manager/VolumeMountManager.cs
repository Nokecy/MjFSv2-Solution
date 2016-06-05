using DokanNet;
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

		private Dictionary<DriveInfo, DatabaseOperations> _driveBagMap;

		private VolumeMountManager() {
			_driveBagMap = new Dictionary<DriveInfo, DatabaseOperations>();

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
		public Dictionary<DriveInfo, DatabaseOperations> GetMountedBagDrives() {
			return new Dictionary<DriveInfo, DatabaseOperations>(_driveBagMap);
		}

		/// <summary>
		/// Removes the volume from the mounted bags and closes the associated database connection
		/// </summary>
		/// <param name="dinfo"></param>
		public void UnmountBagDrive(DriveInfo dinfo) {
			DebugLogger.Log("Unmounted bag on " + dinfo);
			DatabaseOperations op;
			if (_driveBagMap.TryGetValue(dinfo, out op)) {
				dbMan.CloseConnection(op);
				_driveBagMap.Remove(dinfo);
			} else {
				DebugLogger.Log("Unable to unmount bag since it was never registered with the manager anyway.");
			}
		}

		/// <summary>
		/// Get a map of all volumes with a connection to their database
		/// </summary>
		/// <returns></returns>
		public Dictionary<DriveInfo, DatabaseOperations> GetBagConfigurations() {
			Dictionary<DriveInfo, DatabaseOperations> result = new Dictionary<DriveInfo, DatabaseOperations>();
			// Scan all volumes for a configuration file
			foreach (DriveInfo dinfo in DriveInfo.GetDrives()) {
				DatabaseOperations op;

				// If the drive was already registered add the old db connection
				if (_driveBagMap.TryGetValue(dinfo, out op)) {
					result.Add(dinfo, op);
				} else {
					try {
						foreach (FileInfo finfo in dinfo.RootDirectory.GetFiles()) {
							if (finfo.Name == CONFIG_FILE_NAME) {
								// Found a configuration file
								DebugLogger.Log("Configuration on drive " + dinfo);
								// Open a connection to the database
								op = dbMan.OpenConnection(dinfo + CONFIG_FILE_NAME);
								result.Add(dinfo, op);
								break;
							}
						}
					} catch (IOException) {
						DebugLogger.Log("Unable to scan files on drive " + dinfo);
					}
				}

			}
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
	}
}
