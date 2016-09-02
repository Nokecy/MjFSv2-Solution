using DokanNet;
using MjFSv2Lib.Database;
using MjFSv2Lib.Domain;
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
				string bagLocation = entry.Value.BagLocation;
				string result = bagLocation + Path.GetFileName(path);
				lastBasePath = bagLocation;
				MjDebug.Log("Resolved " + path + " to " + result);
				if (File.Exists(result)) {
					return result;
				}		
			} else if (bagVolumes.Count > 1) {
				// Multiple bags mounted. Look through all to confirm the item's location.
				lastBasePath = bagVolumes.First().Key + bagVolumes.First().Value.BagLocation;

				foreach (KeyValuePair<string, DatabaseOperations> entry in bagVolumes) {
					string fileName = Path.GetFileName(path);
					if (entry.Value.GetItem(fileName) != null) {
						string driveLetter = entry.Key;
						string bagLocation = entry.Value.BagLocation;
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
			List<DriveInfo> deprecateNextList = new List<DriveInfo>();

			foreach (KeyValuePair<string, DatabaseOperations> entry in volMan.MountedBagVolumes) {
				try {
					if (directoryPath == "\\") {
						// Display all tags marked rootVisible from the DB
						foreach (MetaTable tag in entry.Value.GetRootTables()) {
							if (!dupTags.Contains(tag.tableName)) {
								dupTags.Add(tag.tableName);
								FileInformation finfo = new FileInformation();
								finfo.FileName = Helper.StringToProper(tag.friendlyName);
								finfo.Attributes = System.IO.FileAttributes.Directory;
								finfo.LastAccessTime = DateTime.Now;
								finfo.LastWriteTime = DateTime.Now;
								finfo.CreationTime = DateTime.Now;
								result.Add(finfo);
							}
						}
					} else {
						DatabaseOperations op = entry.Value;
						MetaTable table = op.GetTableByFriendlyName(tags.First<string>());

						if (tags.Count == 1) {
							// TODO: this is just a test
							foreach(ItemMeta item in op.GetItems(table.tableName)) {
								FileInformation finfo = new FileInformation();
								finfo.FileName = item.name + "." + item.ext;
								finfo.Attributes = (FileAttributes)Enum.Parse(typeof(FileAttributes), item.attr);
								finfo.LastAccessTime = Convert.ToDateTime(item.lat);
								finfo.LastWriteTime = Convert.ToDateTime(item.lwt);
								finfo.CreationTime = Convert.ToDateTime(item.ct);
								result.Add(finfo);
							}

							List<MetaTable> tables = op.GetExtendingTables(table.tableName);
							tables.Add(table);

							// Create a set of folders to further sort the items 
							foreach (MetaTable extTable in tables) {
								foreach(MetaAlias alias in op.GetAliases(extTable.tableName)) {
									// Add a folder for this alias
									FileInformation finfo = new FileInformation();
									finfo.FileName = "By " + alias.alias;
									finfo.Attributes = System.IO.FileAttributes.Directory;
									finfo.LastAccessTime = DateTime.Now;
									finfo.LastWriteTime = DateTime.Now;
									finfo.CreationTime = DateTime.Now;
									result.Add(finfo);
								}
							}

						}






						/*
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
						items.AddRange(entry.Value.GetItemsByCompositeTag(tags.ToList<string>()));*/
					}
				} catch (SQLiteException ex) {
					// Display any exceptions, but continue working. We will remove this drive later.
					MjDebug.Log(ex.StackTrace + "\n" + ex.Message, LogSeverity.MEDIUM);
					deprecateNextList.Add(new DriveInfo(entry.Key));
				}
			}

			// Unmount any entry that caused an exception
			foreach (DriveInfo dinfo in deprecateNextList) {
				volMan.UnmountBagVolume(dinfo.ToString());
			}

			return result;
		}
	}
}
