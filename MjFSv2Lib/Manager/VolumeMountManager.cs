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
		private static readonly DokanOptions MNT_OPS = DokanOptions.FixedDrive | DokanOptions.DebugMode;
		private static readonly FileSystemOperations fsOp = new FileSystemOperations(); // The MjFS Dokany implementation aka main volume

		private bool _mainMounted = false; // Flag indicating the mount status of the main volume
		private DatabaseManager dbMan = DatabaseManager.GetInstance(); // The configuration database manager

		private Dictionary<string, DatabaseOperations> _mountedBagVolumes = new Dictionary<string, DatabaseOperations>(); // A map containing all currently mounted bag volumes
		private Dictionary<string, DatabaseOperations> _discoveredBagVolumes = new Dictionary<string, DatabaseOperations>(); // A map containing all discovered bag volumes 

		private DateTime lastChange; // Indicates last volume mount event to reduce the amount of bag volume discoveries
		
		private VolumeMountManager() {
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
			if (lastChange != null) {
				DateTime now = DateTime.Now;
				if ((now - lastChange).TotalSeconds < 4) {
					// Discard this 'change' if the last change was less than 5 seconds ago
					return;
				} else {
					lastChange = now;
				}
			}

			MjDebug.Log("Logical volumes (un)mounted");
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
		/// Return a copy of all discovered bag volumes or null if <see cref="DiscoverBagVolumes"/> was never invoked before.
		/// </summary>
		public Dictionary<string, DatabaseOperations> DiscoveredBagVolumes {
			get {
				return new Dictionary<string, DatabaseOperations>(_discoveredBagVolumes);
			}	
		}

		#endregion


		/// <summary>
		/// Return a map of all discovered bag volumes. This method will scan all volumes. 
		/// Only use this methods when you explicitly need to re-discover all volumes, otherwise use <see cref="DiscoveredBagVolumes"/> to retrieve a list of already discovered volumes.
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
								MjDebug.Log("Found bag volume on '" + dinfo + "'");
								op = dbMan.OpenConnection(dinfo + CONFIG_FILE_NAME); // Open connection
								result.Add(dinfo.ToString(), op);
								break;
							}
						}
					} catch (IOException) {
						// Drive unavailable or not enough rights. This is not critical, log it.
						MjDebug.Log("Unable to scan files on drive " + dinfo);
					}
				}
			}
			_discoveredBagVolumes = result; // Update map
			return result; // Return the map as well
		}

		/// <summary>
		/// Forces a discovery of new bag volumes and mounts the result
		/// </summary>
		public void MountBagVolumes() {
			// Replace the current bagVolumes map with an updated one.
			_mountedBagVolumes = DiscoverBagVolumes();
		}

		public void MountBagVolume(string drive) {
			MjDebug.Log("Mounting bag volume on '" + drive + "'");
			DatabaseOperations op;
			if (_discoveredBagVolumes.TryGetValue(drive, out op)) {
				_mountedBagVolumes.Add(drive, op);
			} else {
				throw new VolumeMountManagerException("Cannot mount unregistered bag volume.");
			}
		}

		/// <summary>
		/// Removes the volume from the mounted bags and closes the associated database connection
		/// </summary>
		/// <param name="drive"></param>
		public void UnmountBagVolume(string drive) {
			MjDebug.Log("Unmounting bag volume on '" + drive + "'");
			DatabaseOperations op;
			if (_discoveredBagVolumes.TryGetValue(drive, out op)) {
				dbMan.CloseConnection(op);
				_mountedBagVolumes.Remove(drive);
			} else {
				throw new VolumeMountManagerException("Cannot unmount unregistered bag volume.");
			}
		}

		public void UnmountBagVolumes() {

		}

		/// <summary>
		/// Mount the main volume
		/// </summary>
		public void MountMainVolume() {
			if (!_mainMounted) {
				_mainMounted = true;
				string driveLetter = Helper.GetFreeDriveLetters()[0].ToString() + ":\\";
				Console.WriteLine("Mounting main volume to '" + driveLetter + "'");
				fsOp.Drive = driveLetter;
				fsOp.Mount(driveLetter, MNT_OPS); // Blocking call to Dokany
			} else {
				throw new VolumeMountManagerException("Main volume can only be mounted once.");
			}
		}

		/// <summary>
		/// Unmount the main volume - not used in current implementation
		/// </summary>
		public void UnmountMainVolume() {
			if(_mainMounted) {
				char driveLetter = fsOp.Drive.ToCharArray()[0];
				Console.WriteLine("Unmounting main volume from '" + driveLetter + "'");
				Dokan.Unmount(driveLetter);
			} else {
				throw new VolumeMountManagerException("Main volume can only be unmounted if it was mounted before.");
			}
		}

		/// <summary>
		/// Instantiate a new bag volume on the given volume and adds it to the list of discovered bag volumes.
		/// </summary>
		/// <param name="dInfo"></param>
		public DatabaseOperations CreateBagVolume(string drive, string bagLocation) {
			DiscoverBagVolumes(); // Make sure we have the latest data
			if (!_discoveredBagVolumes.Keys.Contains(drive.ToUpper())) {
				DatabaseOperations op = dbMan.OpenConnection(drive + CONFIG_FILE_NAME);				
				op.CreateTables(drive + "\\" + bagLocation.Substring(3));
				_discoveredBagVolumes.Add(drive.ToUpper(), op);
				return op;
			} else {
				throw new VolumeMountManagerException("A bag has already been registed on this volume. Cannot instantiate more than one bag volume on any given volume.");
			}
		}
	}
}
