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
	/// <summary>
	/// Manages bag volume creation, deletion, discovery, mounting and unmounting as well as mounting the main volume.
	/// </summary>
	public class VolumeMountManager {
		private static VolumeMountManager instance = new VolumeMountManager();
		public static readonly string CONFIG_FILE_NAME = "BagConf.sqlite";
		private static readonly MjFileSystemOperations fileSystem = new MjFileSystemOperations(); // The MjFS main volume

		private bool _mainMounted = false; // Flag indicating the mount status of the main volume
		private DatabaseManager dbMan = DatabaseManager.GetInstance(); // The configuration database manager

		private Dictionary<string, DatabaseOperations> _mountedBagVolumes; // A map containing all currently mounted bag volumes
		private Dictionary<string, DatabaseOperations> _discoveredBagVolumes; // A map containing all discovered bag volumes 

		private VolumeMountManager() {
			_mountedBagVolumes = new Dictionary<string, DatabaseOperations>();

			// Register eventhandler for changes to logical volumes
			var watcher = new ManagementEventWatcher();
			var query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
			watcher.EventArrived += new EventArrivedEventHandler(OnLogicalVolumesChanges);
			watcher.Query = query;
			watcher.Start();
		}

		public static VolumeMountManager GetInstance() {
			return instance;
		}

		#region Event handlers

		private void OnLogicalVolumesChanges(object sender, EventArrivedEventArgs e) {
			DebugLogger.Log("Logical volumes (un)mounted");
			MountBagVolumes(); 
		}

		#endregion


		#region Properties

		/// <summary>
		/// Return a copy of all mounted bag volumes.
		/// </summary>
		public Dictionary<string, DatabaseOperations> MountedBagVolumes {
			get {
				return new Dictionary<string, DatabaseOperations>(_mountedBagVolumes);
			}
		}

		/// <summary>
		/// Return a copy of all discovered bag volumes. Returns null if no bag volumes were discovered.
		/// </summary>
		public Dictionary<string, DatabaseOperations> DiscoveredBagVolumes {
			get {
				if (_discoveredBagVolumes == null) {
					return null;
				}
				return new Dictionary<string, DatabaseOperations>(_discoveredBagVolumes);
			}	
		}

		#endregion


		/// <summary>
		/// Return a map of all discovered bag volumes. Performs a scan of all volumes.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, DatabaseOperations> DiscoverBagVolumes() {
			Dictionary<string, DatabaseOperations> result = new Dictionary<string, DatabaseOperations>();
			// Scan all volumes for a configuration file
			foreach (DriveInfo dinfo in DriveInfo.GetDrives()) {
				DatabaseOperations op;

				if (_mountedBagVolumes.TryGetValue(dinfo.ToString(), out op)) {
					result.Add(dinfo.ToString(), op); // Bag volume already registered, re-use old entry
				} else {
					try {
						foreach (FileInfo finfo in dinfo.RootDirectory.GetFiles()) {
							if (finfo.Name == CONFIG_FILE_NAME) {
								// Found db
								DebugLogger.Log("Configuration on drive " + dinfo);
								// Open connection to db
								op = dbMan.OpenConnection(dinfo + CONFIG_FILE_NAME);
								result.Add(dinfo.ToString(), op);
								break;
							}
						}
					} catch (IOException) {
						// Drive unavailable of not enough rights. This is not critical, log it.
						DebugLogger.Log("Unable to scan files on drive " + dinfo);
					}
				}
			}
			_discoveredBagVolumes = result; // Update map
			return result; // Return the map as well
		}

		/// <summary>
		/// Mount all newly discovered bag volumes
		/// </summary>
		public void MountBagVolumes() {
			// Replace the current bagVolumes map with an updated one.
			_mountedBagVolumes = DiscoverBagVolumes();
		}

		/// <summary>
		/// Removes the volume from the mounted bags and closes the associated database connection
		/// </summary>
		/// <param name="drive"></param>
		public void UnmountBagVolume(string drive) {
			DebugLogger.Log("Unmounting bag volume on '" + drive + "'");
			DatabaseOperations op;
			if (_discoveredBagVolumes.TryGetValue(drive, out op)) {
				dbMan.CloseConnection(op);
				_mountedBagVolumes.Remove(drive);
			} else {
				throw new VolumeMountManagerException("Cannot unmount unregistered bag volume");
			}
		}

		/// <summary>
		/// Mount the main volume. Unmounting this volume is not possible.
		/// </summary>
		public void MountMainVolume() {
			if (!_mainMounted) {
				_mainMounted = true;
				string driveLetter = Helper.GetFreeDriveLetters()[0].ToString() + ":\\";
				Console.WriteLine("Mounting main volume to '" + driveLetter + "'");
				fileSystem.Drive = driveLetter;
				fileSystem.Mount(driveLetter, DokanOptions.DebugMode | DokanOptions.FixedDrive); // Blocking call
			} else {
				DebugLogger.Log("Main volume has already been mounted");
			}
		}

		/// <summary>
		/// Instantiate a new bag volume on the given volume
		/// </summary>
		/// <param name="dInfo"></param>
		public void CreateBagVolume(string drive, string bagLocation) {
			DiscoverBagVolumes(); // Make sure we have the latest data
			if (!_discoveredBagVolumes.Keys.Contains(drive.ToUpper())) {
				DatabaseOperations op = dbMan.OpenConnection(drive + CONFIG_FILE_NAME);
				op.AddTables(bagLocation.Substring(3));
				op.UpdateHash();
			} else {
				throw new VolumeMountManagerException("A bag has already been registed on this volume");
			}
		}
	}
}
