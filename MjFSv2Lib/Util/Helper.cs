using DokanNet;
using MjFSv2Lib.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace MjFSv2Lib.Util {
	public class Helper {

		/// <summary>
		/// Test whether the current user has administrator rights
		/// </summary>
		/// <returns></returns>
		public static bool IsUserAdministrator() {
			//bool value to hold our return value
			bool isAdmin;
			WindowsIdentity user = null;
			try {
				//get the currently logged in user
				user = WindowsIdentity.GetCurrent();
				WindowsPrincipal principal = new WindowsPrincipal(user);
				isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
			} catch (UnauthorizedAccessException) {
				isAdmin = false;
			} catch (Exception) {
				isAdmin = false;
			} finally {
				if (user != null)
					user.Dispose();
			}
			return isAdmin;
		}


		/// <summary>
		/// Create an Item with the given FileInfo
		/// </summary>
		/// <param name="finfo"></param>
		/// <returns></returns>
		public static Item GetItemFromFileInfo(FileInfo finfo) {
			try {
				string name = finfo.Name.Split(new Char[] { '.' })[0];
				string ext = finfo.Extension.Substring(1, finfo.Extension.Length - 1);
				return new Item(finfo.Name, name, ext, finfo.Length, finfo.LastAccessTime, finfo.LastWriteTime, finfo.CreationTime, finfo.Attributes);
			} catch (FileNotFoundException) {
				return null;
			}
		}

		public static Item GetItemFromId(string id) {
			string name = id.Split(new Char[] { '.' })[0];
			return new Item(id, name, name, 0, new DateTime(), new DateTime(), new DateTime(), new FileAttributes());
		}

		/// <summary>
		/// Create a FileInformation object for the given Item
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static FileInformation GetFileInformationFromItem(Item item) {
			FileInformation finfo = new FileInformation();
			finfo.FileName = item.Name + "." + item.Extension;
			finfo.Attributes = System.IO.FileAttributes.Normal;
			finfo.LastAccessTime = item.LastAccesTime;
			finfo.LastWriteTime = item.LastWriteTime;
			finfo.CreationTime = item.CreationTime;
			finfo.Length = item.Size;
			return finfo;
		}

		/// <summary>
		/// Convert a path to a list of Tags
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static HashSet<string> GetTagsFromPath(string path) {
			HashSet<string> tagList = new HashSet<string>();
			foreach (string directory in path.Split(new Char[] { '\\' })) {
				if (directory.Trim() != "") {
					tagList.Add(directory.ToLower());
				}
			}
			return tagList;
		}

		/// <summary>
		/// Return a FileAttributes enum constant from given integer
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public static FileAttributes GetFileAttributesFromInt(int i) {
			return (FileAttributes)Enum.Parse(typeof(FileAttributes), i.ToString());
		}

		/// <summary>
		/// Return the pascal-case string
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string StringToProper(string s) {
			return char.ToUpper(s[0]) + s.Substring(1);
		}

		/// <summary>
		/// Retrieve a list containg drive letters of unmounted drives
		/// </summary>
		/// <returns></returns>
		public static ArrayList GetFreeDriveLetters() {
			// Populate free drive letters
			ArrayList driveLetters = new ArrayList(26); // Allocate space for alphabet
			for (int i = 67; i < 91; i++) // increment from ASCII values for A-Z
			{
				driveLetters.Add(Convert.ToChar(i)); // Add uppercase letters to possible drive letters
			}

			foreach (String drive in Directory.GetLogicalDrives()) {
				driveLetters.Remove(drive[0]); // removed used drive letters from possible drive letters
			}
			return driveLetters;
		}
	}
}
