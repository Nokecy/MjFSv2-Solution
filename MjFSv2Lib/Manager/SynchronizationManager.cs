using MjFSv2Lib.Database;
using MjFSv2Lib.Model;
using MjFSv2Lib.Util;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace MjFSv2Lib.Manager {
	/// <summary>
	/// Manages synchronization of bag volumes
	/// </summary>
	public class SynchronizationManager {
		private static SynchronizationManager instance = new SynchronizationManager();
		private readonly Dictionary<string, FileSystemWatcher> watchDrives = new Dictionary<string, FileSystemWatcher>();

		private SynchronizationManager() { }

		public static SynchronizationManager GetInstance() {
			return instance;
		}

		/// <summary>
		/// Return a copy of the list of all bag volumes currently being synchronized
		/// </summary>
		public List<string> SynchronizedBagVolumes {
			get {
				return new List<string>(watchDrives.Keys);
			}
		}

		/// <summary>
		/// Start synchronization of the given bag volume entry
		/// </summary>
		/// <param name="entry"></param>
		public void StartSynchronization(KeyValuePair<string, DatabaseOperations> entry) {
			string drive = entry.Key;
			if (!watchDrives.ContainsKey(drive)) {
				if (VolumeMountManager.GetInstance().DiscoveredBagVolumes.Count == 0) {
					throw new SynchronizationManagerException("Before starting synchronization on any bag volume, please first discoved bag volumes on VolumeMountManager!");
				}
				FileSystemWatcher fsw = new FileSystemWatcher();
				fsw.Path = drive + entry.Value.GetBagLocation() + "\\";
				fsw.EnableRaisingEvents = true;
				watchDrives.Add(drive, fsw);

				fsw.Created += (s, e) => {
					FileInfo fInfo = new FileInfo(e.FullPath);
					DriveInfo dInfo = new DriveInfo(fInfo.Directory.Root.Name);
					DebugLogger.Log("Detected new file '" + fInfo.Name + "'");
					DatabaseOperations op = VolumeMountManager.GetInstance().DiscoveredBagVolumes[dInfo.ToString()];
					Item fileItem = Helper.GetItemFromFileInfo(fInfo);
					if (fileItem != null) {
						try {
							op.InsertItem(fileItem);
							//op.InsertDefaultItemTag(fileItem);
						} catch (SQLiteException ex) {
							DebugLogger.Log("Database reports: \n" + ex.Message);
						}
					} else {
						DebugLogger.Log("Fileitem is null");
					}
				};

				fsw.Deleted += (s, e) => {
					FileInfo fInfo = new FileInfo(e.FullPath);
					DriveInfo dInfo = new DriveInfo(fInfo.Directory.Root.Name);
					DebugLogger.Log("Removed file '" + fInfo.Name + "'");
					DatabaseOperations op = VolumeMountManager.GetInstance().DiscoveredBagVolumes[dInfo.ToString()];
					Item fileItem = Helper.GetItemFromId(fInfo.Name);
					if (fileItem != null) {
						try {
							op.DeleteItem(fileItem);
						} catch (SQLiteException ex) {
							DebugLogger.Log("Database reports: \n" + ex.Message);
						}
					} else {
						DebugLogger.Log("Fileitem is null");
					}
				};

				fsw.Renamed += (s, e) => {
					FileInfo fInfo = new FileInfo(e.FullPath);
					FileInfo oldfInfo = new FileInfo(e.OldFullPath);
					DriveInfo dInfo = new DriveInfo(fInfo.Directory.Root.Name);
					DebugLogger.Log("Renamed file '" + oldfInfo.Name + "' to '" + fInfo.Name + "'");
					DatabaseOperations op = VolumeMountManager.GetInstance().DiscoveredBagVolumes[dInfo.ToString()];
					Item fileItem = Helper.GetItemFromFileInfo(fInfo);
					Item oldFileItem = Helper.GetItemFromId(oldfInfo.Name);

					if (fileItem != null && oldfInfo != null) {
						try {
							op.DeleteItem(oldFileItem);
							op.InsertItem(fileItem);
						} catch (SQLiteException ex) {
							DebugLogger.Log("Database reports: \n" + ex.Message);
						}
					} else {
						DebugLogger.Log("Fileitem is null");
					}
				};
			}
		}

		/// <summary>
		/// Start synchronization of all given bag volumes
		/// </summary>
		/// <param name="bagVolumes"></param>
		public void StartSynchronization(Dictionary<string, DatabaseOperations> bagVolumes) {
			foreach (KeyValuePair<string, DatabaseOperations> entry in bagVolumes) {
				StartSynchronization(entry);
			}
		}

		/// <summary>
		/// Stop synchronization of the given bag volume
		/// </summary>
		/// <param name="drive"></param>
		public void StopSynchronization(string drive) {
			FileSystemWatcher fsw;
			if (watchDrives.TryGetValue(drive, out fsw)) {
				fsw.Dispose();
				watchDrives.Remove(drive);
			} else {
				throw new SynchronizationManagerException("Cannot stop synchronization on unregistered volume");
			}
		}

		/// <summary>
		/// Stop synchronization of all registerd bag volumes
		/// </summary>
		public void StopSynchronization() {
			foreach (KeyValuePair<string, FileSystemWatcher> entry in watchDrives) {
				entry.Value.Dispose();
			}
			watchDrives.Clear();
		}
	}
}
