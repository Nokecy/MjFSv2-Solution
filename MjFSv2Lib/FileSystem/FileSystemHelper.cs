using DokanNet;
using MjFSv2Lib.Database;
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

namespace MjFSv2Lib.FileSystem {
	static class FileSystemHelper {
		private static readonly VolumeMountManager volMan = VolumeMountManager.GetInstance();
		public static string lastBasePath; // Last bag location from which files were loaded

		/// <summary>
		/// Turn any MjFS path in to a valid path to a file located inside a bag
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string ResolvePath(string path) {
			Dictionary<string, DatabaseOperations> bagVolumes = volMan.MountedBagVolumes;
			List<DriveInfo> removable = new List<DriveInfo>();

			if (bagVolumes.Count == 1) {
				// There is a single bag mounted. Directly return the item's location.
				KeyValuePair<string, DatabaseOperations> entry = bagVolumes.First();
				string driveLetter = entry.Key;
				string bagLocation = entry.Value.GetBagLocation();
				string result = driveLetter + bagLocation + "\\" + Path.GetFileName(path);
				lastBasePath = driveLetter + bagLocation + "\\";
				if (File.Exists(result)) {
					return result;
				}		
			} else if (bagVolumes.Count > 1) {
				// Multiple bags mounted. Look through all to confirm the item's location.
				lastBasePath = bagVolumes.First().Key + bagVolumes.First().Value.GetBagLocation() + "\\";

				foreach (KeyValuePair<string, DatabaseOperations> entry in bagVolumes) {
					string fileName = Path.GetFileName(path);
					if (entry.Value.GetItem(fileName) != null) {
						string driveLetter = entry.Key;
						string bagLocation = entry.Value.GetBagLocation();
						string result = driveLetter + bagLocation + "\\" + fileName;
						if (File.Exists(result)) {
							return result;
						}
					} else {
						continue;
					}
				}
			} else {
				// There are no mounted bags. No need to search.
			}
			return null;
		}

		/// <summary>
		/// Finds a collection of files for the given directory
		/// </summary>
		/// <param name="directoryPath"></param>
		/// <returns></returns>
		public static IList<FileInformation> FindFiles(string directoryPath) {
			List<FileInformation> result = new List<FileInformation>();
			List<string> dupTags = new List<string>();
			HashSet<string> tags = Helper.GetTagsFromPath(directoryPath);
			List<Item> items = new List<Item>();
			List<DriveInfo> deprecateNextList = new List<DriveInfo>();

			foreach (KeyValuePair<string, DatabaseOperations> entry in volMan.MountedBagVolumes) {
				try {
					if (directoryPath == "\\") {
						// Display all tags marked rootVisible from the DB
						foreach (Tag tag in entry.Value.GetRootTags()) {
							if (!dupTags.Contains(tag.Id)) {
								dupTags.Add(tag.Id);
								FileInformation finfo = new FileInformation();
								finfo.FileName = Helper.StringToProper(tag.Id);
								finfo.Attributes = System.IO.FileAttributes.Directory;
								finfo.LastAccessTime = DateTime.Now;
								finfo.LastWriteTime = DateTime.Now;
								finfo.CreationTime = DateTime.Now;
								result.Add(finfo);
							}
						}
					} else {
						// Create a set of 'inner tags' by removing the tags already in the path
						List<string> innerTags = entry.Value.GetInnerTags(tags.ToList()).Except(tags).ToList();
						// Add the set of innerTags 
						foreach (string tag in innerTags) {
							FileInformation finfo = new FileInformation();
							finfo.FileName = Helper.StringToProper(tag);
							finfo.Attributes = System.IO.FileAttributes.Directory;
							finfo.LastAccessTime = DateTime.Now;
							finfo.LastWriteTime = DateTime.Now;
							finfo.CreationTime = DateTime.Now;
							result.Add(finfo);
						}
						items.AddRange(entry.Value.GetItemsByCompositeTag(tags.ToList<string>()));
					}
				} catch (SQLiteException ex) {
					// Display any exceptions, but continue working. We will remove this drive later.
					DebugLogger.Log(ex.StackTrace + "\n" + ex.Message);
					deprecateNextList.Add(new DriveInfo(entry.Key));
				}
			}

			// Unmount any entry that caused an exception
			foreach (DriveInfo dinfo in deprecateNextList) {
				volMan.UnmountBagVolume(dinfo.ToString());
			}

			// Add any found files
			foreach (Item it in items) {
				result.Add(Helper.GetFileInformationFromItem(it));
			}

			return result;
		}
	}
}
