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
		private readonly Dictionary<string, FileSystemWatcher> _watchedDrives = new Dictionary<string, FileSystemWatcher>();
		private readonly VolumeMountManager vMan = VolumeMountManager.GetInstance();

		private SynchronizationManager() { }

		public static SynchronizationManager GetInstance() {
			return instance;
		}

		/// <summary>
		/// Return a copy of the list of all bag volumes currently being synchronized
		/// </summary>
		public List<string> SynchronizedBagVolumes {
			get {
				return new List<string>(_watchedDrives.Keys);
			}
		}

		/// <summary>
		/// Start synchronization of the given bag volume entry
		/// </summary>
		/// <param name="entry"></param>
		public void StartSynchronization(string drive) {

			if (!_watchedDrives.ContainsKey(drive)) {
				if (vMan.DiscoveredBagVolumes.Count == 0) {
					throw new SynchronizationManagerException("Unable to start synchronization: there are no registered bag volumes.");
				}
				FileSystemWatcher fsw = new FileSystemWatcher();
				DatabaseOperations op;
				if (!vMan.DiscoveredBagVolumes.TryGetValue(drive, out op)) {
					throw new SynchronizationManagerException("Unable to start synchronization: bag volume is not registered.");
				} 
				

				fsw.Path = drive + op.GetBagLocation() + "\\";
				fsw.EnableRaisingEvents = true;
				_watchedDrives.Add(drive, fsw);

				// Assign event handlers for the FileWathcer on this volume
				#region fsw event handlers
				fsw.Created += (s, e) => {
					FileInfo fInfo = new FileInfo(e.FullPath);
					DriveInfo dInfo = new DriveInfo(fInfo.Directory.Root.Name);
					DebugLogger.Log("Detected new file '" + fInfo.Name + "'");
					DatabaseOperations tempOp = VolumeMountManager.GetInstance().DiscoveredBagVolumes[dInfo.ToString()];
					Item fileItem = Helper.GetItemFromFileInfo(fInfo);
					if (fileItem != null) {
						try {
							tempOp.InsertItem(fileItem);
							tempOp.InsertDefaultItemTag(fileItem);
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
					DatabaseOperations tempOp = VolumeMountManager.GetInstance().DiscoveredBagVolumes[dInfo.ToString()];
					Item fileItem = Helper.GetItemFromId(fInfo.Name);
					if (fileItem != null) {
						try {
							tempOp.DeleteItem(fileItem);
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
					DatabaseOperations tempOp = VolumeMountManager.GetInstance().DiscoveredBagVolumes[dInfo.ToString()];
					Item fileItem = Helper.GetItemFromFileInfo(fInfo);
					Item oldFileItem = Helper.GetItemFromId(oldfInfo.Name);

					if (fileItem != null && oldfInfo != null) {
						try {
							tempOp.DeleteItem(oldFileItem);
							tempOp.InsertItem(fileItem);
							tempOp.InsertDefaultItemTag(fileItem);
						} catch (SQLiteException ex) {
							DebugLogger.Log("Database reports: \n" + ex.Message);
						}
					} else {
						DebugLogger.Log("Fileitem is null");
					}
				};
				#endregion
			}
		}

		/// <summary>
		/// Start synchronization of all given bag volumes
		/// </summary>
		/// <param name="bagVolumes"></param>
		public void StartSynchronization(List<string> bagVolumes) {
			foreach (string drive in bagVolumes) {
				StartSynchronization(drive);
			}
		}

		/// <summary>
		/// Stop synchronization of the given bag volume
		/// </summary>
		/// <param name="drive"></param>
		public void StopSynchronization(string drive) {
			FileSystemWatcher fsw;
			if (_watchedDrives.TryGetValue(drive, out fsw)) {
				fsw.Dispose();
				_watchedDrives.Remove(drive);
			} else {
				throw new SynchronizationManagerException("Cannot stop synchronization: there is no known bag volume on this given drive.");
			}
		}

		/// <summary>
		/// Stop synchronization of all registerd bag volumes
		/// </summary>
		public void StopSynchronization() {
			foreach (KeyValuePair<string, FileSystemWatcher> entry in _watchedDrives) {
				entry.Value.Dispose();
			}
			_watchedDrives.Clear();
		}

		/// <summary>
		/// Remove any items and their associated tags from the database and add new ones for the files currently in the bag location.
		/// This method can also be used to initialize a newly created bag volume already containing files.
		/// </summary>
		/// <param name="drive"></param>
		public void Resynchronize(string drive) {
			drive = drive.ToUpper();
			if (vMan.DiscoveredBagVolumes.Count == 0) {
				throw new SynchronizationManagerException("Unable to start synchronization: there are no registered bag volumes.");
			}
			DatabaseOperations op;
			if (!VolumeMountManager.GetInstance().DiscoveredBagVolumes.TryGetValue(drive, out op)) {
				throw new SynchronizationManagerException("Unable to start synchronization: bag volume is not registered.");
			}
			StopSynchronization(drive); // Stop synching
			op.TruncateTable("Item");
			op.TruncateTable("ItemTag");

			string path = drive + op.GetBagLocation() + "\\";
			DirectoryInfo dInfo = new DirectoryInfo(path);
			if (dInfo.Exists) {
				foreach (FileInfo fInfo in dInfo.GetFiles()) {
					Item fileItem = Helper.GetItemFromFileInfo(fInfo);
					op.InsertItem(fileItem);
					op.InsertDefaultItemTag(fileItem);
				}
			} else {
				throw new SynchronizationManagerException("Bag location does not exist on volume " + drive);
			}
		}
	}
}
