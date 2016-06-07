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
	public class SynchronizationManager {
		private static SynchronizationManager instance = new SynchronizationManager();
		private readonly Dictionary<string, FileSystemWatcher> watchDrives = new Dictionary<string, FileSystemWatcher>();


		private SynchronizationManager() { }

		public static SynchronizationManager GetInstance() {
			return instance;
		}

		public List<string> GetSynchronizedDrives() {
			return new List<string>(watchDrives.Keys);
		}

		public void StartSynchronization(string dinfo, DatabaseOperations op) {
			if (!watchDrives.ContainsKey(dinfo)) {
				FileSystemWatcher fsw = new FileSystemWatcher();
				fsw.Path = dinfo + op.GetLocation() + "\\";
				fsw.EnableRaisingEvents = true;
				watchDrives.Add(dinfo, fsw);

				fsw.Changed += (s, e) => {
					FileInfo finfo = new FileInfo(e.FullPath);
					DriveInfo drive = new DriveInfo(finfo.Directory.Root.Name);
					DebugLogger.Log("Detected file change on file '" + finfo.Name + "'");
					DatabaseOperations opr = VolumeMountManager.GetInstance().GetKnownBagConfigs()[drive.ToString()];
					Item i = Helper.GetItemFromFileInfo(finfo);
					if (i != null) {
						try {
							op.UpdateItem(i);
						} catch (SQLiteException) {
							//StopSynchronization(drive.ToString());
						}
					}
				};

				fsw.Created += (s, e) => {
					FileInfo finfo = new FileInfo(e.FullPath);
					DriveInfo drive = new DriveInfo(finfo.Directory.Root.Name);
					DebugLogger.Log("Detected new file '" + finfo.Name + "'");
					DatabaseOperations opr = VolumeMountManager.GetInstance().GetKnownBagConfigs()[drive.ToString()];


					Item i = Helper.GetItemFromFileInfo(finfo);
					if (i != null) {
						try {
							opr.InsertItem(i);
							opr.InsertDefaultItemTag(i);
						} catch (SQLiteException) {
							//StopSynchronization(drive.ToString());
						}
					}
				};

				fsw.Deleted += (s, e) => {
					FileInfo finfo = new FileInfo(e.FullPath);
					DriveInfo drive = new DriveInfo(finfo.Directory.Root.Name);
					DebugLogger.Log("Removed file '" + finfo.Name + "'");
					DatabaseOperations opr = VolumeMountManager.GetInstance().GetKnownBagConfigs()[drive.ToString()];


					Item i = Helper.GetItemFromFileInfo(finfo);
					if (i != null) {
						try {
							opr.DeleteItem(i);
						} catch (SQLiteException) {
							//StopSynchronization(drive.ToString());
						}
					}
				};
			}
		}

		public void StartSynchronization(Dictionary<string, DatabaseOperations> driveBagMap) {
			foreach (KeyValuePair<string, DatabaseOperations> entry in driveBagMap) {
				DebugLogger.Log("Add watcher for volume '" + entry.Key + "'");
				StartSynchronization(entry.Key, entry.Value);
			}
		}

		/// <summary>
		/// Stop synchronization of a single volume
		/// </summary>
		/// <param name="dinfo"></param>
		public void StopSynchronization(string dinfo) {
			FileSystemWatcher fsw;
			if (watchDrives.TryGetValue(dinfo, out fsw)) {
				fsw.Dispose();
				watchDrives.Remove(dinfo);
			} else {
				throw new SynchronizationManagerException("Cannot stop synchronization on unregistered volume");
			}
		}

		/// <summary>
		/// Stop synchronization of all volumes
		/// </summary>
		public void StopSynchronization() {
			foreach (KeyValuePair<string, FileSystemWatcher> entry in watchDrives) {
				entry.Value.Dispose();
			}
			watchDrives.Clear();
		}
	}
}
