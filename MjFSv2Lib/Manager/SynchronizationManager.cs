using MjFSv2Lib.Database;
using MjFSv2Lib.Model;
using MjFSv2Lib.Util;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
				FileSystemWatcher fsw = new FileSystemWatcher();
				fsw.Path = drive + entry.Value.GetLocation() + "\\";
				fsw.EnableRaisingEvents = true;
				watchDrives.Add(drive, fsw);

				fsw.Changed += (s, e) => {
					FileInfo fInfo = new FileInfo(e.FullPath);
					DriveInfo dInfo = new DriveInfo(fInfo.Directory.Root.Name);
					DebugLogger.Log("Detected file change on file '" + fInfo.Name + "'");
					DatabaseOperations op = VolumeMountManager.GetInstance().DiscoveredBagVolumes[dInfo.ToString()];
					Item fileItem = Helper.GetItemFromFileInfo(fInfo);
					if (fileItem != null) {
						try {
							op.UpdateItem(fileItem);
						} catch (SQLiteException ex) {
							DebugLogger.Log("Database reports: \n" + ex.Message);
						}
					}
				};

				fsw.Created += (s, e) => {
					FileInfo fInfo = new FileInfo(e.FullPath);
					DriveInfo dInfo = new DriveInfo(fInfo.Directory.Root.Name);
					DebugLogger.Log("Detected new file '" + fInfo.Name + "'");
					DatabaseOperations op = VolumeMountManager.GetInstance().DiscoveredBagVolumes[dInfo.ToString()];
					Item fileItem = Helper.GetItemFromFileInfo(fInfo);
					if (fileItem != null) {
						try {
							op.InsertItem(fileItem);
							op.InsertDefaultItemTag(fileItem);
						} catch (SQLiteException ex) {
							DebugLogger.Log("Database reports: \n" + ex.Message);
						}
					}
				};

				fsw.Deleted += (s, e) => {
					FileInfo fInfo = new FileInfo(e.FullPath);
					DriveInfo dInfo = new DriveInfo(fInfo.Directory.Root.Name);
					DebugLogger.Log("Removed file '" + fInfo.Name + "'");
					DatabaseOperations op = VolumeMountManager.GetInstance().DiscoveredBagVolumes[dInfo.ToString()];
					Item fileItem = Helper.GetItemFromFileInfo(fInfo);
					if (fileItem != null) {
						try {
							op.DeleteItem(fileItem);
						} catch(SQLiteException ex) {
							DebugLogger.Log("Database reports: \n" + ex.Message);
						}
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
