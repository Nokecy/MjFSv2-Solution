﻿using DokanNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.AccessControl;
using FileAccess = DokanNet.FileAccess;
using MjFSv2Lib.Manager;
using System.Data.SQLite;
using MjFSv2Lib.Util;
using MjFSv2Lib.Database;
using MjFSv2Lib.Model;
using System.Runtime.InteropServices;

namespace MjFSv2Lib.FileSystem {
	/// <summary>
	/// MjFS implementation for Dokan
	/// </summary>
	class FileSystemOperations : IDokanOperations {
		public string Drive { get; set; }
		private static readonly string _volumeLabel = "DefaultBag";
		private static readonly string _name = "MjFS";
		public readonly VolumeMountManager volMan = VolumeMountManager.GetInstance();

		private string GetPath(string path) {
			// This is, as of now, handled by the helper class
			return FileSystemHelper.ResolvePath(path);
		}
		
		public void Cleanup(string fileName, DokanFileInfo info) {
			if (info.Context != null && info.Context is FileStream) {
				(info.Context as FileStream).Dispose();
			}
			info.Context = null;

			if (info.DeleteOnClose) {
				if (info.IsDirectory) {
					Directory.Delete(GetPath(fileName));
				} else {
					File.Delete(GetPath(fileName));
				}
			}
		}
		
		public void CloseFile(string fileName, DokanFileInfo info) {
			if (info.Context != null && info.Context is FileStream) {
				(info.Context as FileStream).Dispose();
			}
			info.Context = null;
		}

		public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info) {
			/*
			WARNING: THIS BREAKS MJFS PATH RESOLUTION AND MAY CAUSE BSODS
			
			string basePath = FileSystemHelper.lastBasePath; // spoof it

			if (Path.GetFileName(fileName).Trim() != "" && basePath != null) {

				var path = basePath + Path.GetFileName(fileName);
				if (!info.IsDirectory) {
					// We don't do directories
					bool pathExists = true;
					bool pathIsDirectory = false;

					bool readWriteAttributes = (access & DataAccess) == 0;
					bool readAccess = (access & DataWriteAccess) == 0;

					try {
						pathExists = (Directory.Exists(path) || File.Exists(path));
						pathIsDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);
					} catch (IOException) { }

					switch (mode) {
						case FileMode.Open:

							if (pathExists) {
								if (readWriteAttributes || pathIsDirectory)
								// check if driver only wants to read attributes, security info, or open directory
								{
									if (pathIsDirectory && (access & FileAccess.Delete) == FileAccess.Delete
										&& (access & FileAccess.Synchronize) != FileAccess.Synchronize) //It is a DeleteFile request on a directory
										return DokanResult.AccessDenied;

									info.IsDirectory = pathIsDirectory;
									info.Context = new object();
									// must set it to someting if you return DokanError.Success

									return DokanResult.Success;
								}
							} else {
								return DokanResult.FileNotFound;
							}
							break;

						case FileMode.CreateNew:
							if (pathExists)
								return DokanResult.FileExists;
							break;

						case FileMode.Truncate:
							if (!pathExists)
								return DokanResult.FileNotFound;
							break;

						default:
							break;
					}

					try {
						info.Context = new FileStream(path, mode, readAccess ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite, share, 4096, options);

						if (mode == FileMode.CreateNew
							|| mode == FileMode.Create) //Files are always created as Archive
							attributes |= FileAttributes.Archive;
						File.SetAttributes(path, attributes);
					} catch (UnauthorizedAccessException) // don't have access rights
					  {
						return DokanResult.AccessDenied;
					} catch (DirectoryNotFoundException) {
						return DokanResult.PathNotFound;
					} catch (Exception ex) {
						uint hr = (uint)Marshal.GetHRForException(ex);
						switch (hr) {
							case 0x80070020: //Sharing violation
								return DokanResult.SharingViolation;
							default:
								throw ex;
						}
					}
				}
				return DokanResult.Success;
			} else {
				info.IsDirectory = true;
				info.Context = new object();
				return DokanResult.Success;
			}*/
			return DokanResult.Success;
		}

		public NtStatus DeleteFile(string fileName, DokanFileInfo info) {
			return DokanResult.NotImplemented;
		}

		public NtStatus DeleteDirectory(string fileName, DokanFileInfo info) {
			return DokanResult.NotImplemented;
		}

		public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info) {
			files = FileSystemHelper.FindFiles(fileName);
			return DokanResult.Success;
		}

		public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info) {
			files = new FileInformation[0];
			return DokanResult.NotImplemented;
		}

		public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info) {
			streams = new FileInformation[0];
			return DokanResult.NotImplemented;
		}

		public NtStatus FlushFileBuffers(string fileName, DokanFileInfo info) {
			try {
				((FileStream)(info.Context)).Flush();
				return DokanResult.Success;
			} catch (IOException) {
				return DokanResult.DiskFull;
			}
		}

		public NtStatus GetDiskFreeSpace(out long free, out long total, out long used, DokanFileInfo info) {
			//TODO: impl correct free space count
			var dinfo = DriveInfo.GetDrives().Where(di => di.RootDirectory.Name == Path.GetPathRoot("C:\\")).Single();

			used = dinfo.AvailableFreeSpace;
			total = dinfo.TotalSize;
			free = dinfo.TotalFreeSpace;
			return DokanResult.Success;
		}

		public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info) {
			// may be called with info.Context == null, but usually it isn't
			string path = GetPath(fileName);

			if (path == null) {
				FileInformation finfo = new FileInformation();
				finfo.FileName = "FAKE";
				finfo.Attributes = System.IO.FileAttributes.Directory;
				finfo.LastAccessTime = DateTime.Now;
				finfo.LastWriteTime = DateTime.Now;
				finfo.CreationTime = DateTime.Now;
				fileInfo = finfo;
			} else {
				FileSystemInfo finfo = new FileInfo(path);
				if (!finfo.Exists)
					finfo = new DirectoryInfo(path);

				fileInfo = new FileInformation {
					FileName = fileName,
					Attributes = finfo.Attributes,
					CreationTime = finfo.CreationTime,
					LastAccessTime = finfo.LastAccessTime,
					LastWriteTime = finfo.LastWriteTime,
					Length = (finfo is FileInfo) ? ((FileInfo)finfo).Length : 0,
				};
			}
			
			return DokanResult.Success;
		}

		public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info) {
			try {
				string path = GetPath(fileName);
				if (path == null) {
					security = null;
					return DokanResult.AccessDenied;
				} else {
					security = info.IsDirectory
							   ? (FileSystemSecurity)Directory.GetAccessControl(GetPath(fileName))
							   : File.GetAccessControl(GetPath(fileName));
					return DokanResult.Success;
				}

				
			} catch (UnauthorizedAccessException) {
				security = null;
				return DokanResult.AccessDenied;
			}
		}

		public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, DokanFileInfo info) {
			volumeLabel = _volumeLabel;
			fileSystemName = _name;
			features = FileSystemFeatures.None;
			return DokanResult.Success;
		}

		public NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info) {
			return DokanResult.Success;
		}

		public NtStatus Mounted(DokanFileInfo info) {
			return DokanResult.Success;
		}

		public NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info) {
			string oldpath = GetPath(oldName);
			string newpath = GetPath(newName);

			if (info.Context != null && info.Context is FileStream) {
				(info.Context as FileStream).Dispose();
			}
			info.Context = null;

			bool exist = false;
			if (info.IsDirectory)
				exist = Directory.Exists(newpath);
			else
				exist = File.Exists(newpath);

			if (!exist) {
				info.Context = null;
				if (info.IsDirectory)
					Directory.Move(oldpath, newpath);
				else
					File.Move(oldpath, newpath);
				return DokanResult.Success;
			} else if (replace) {
				info.Context = null;

				if (info.IsDirectory) //Cannot replace directory destination - See MOVEFILE_REPLACE_EXISTING
					return DokanResult.AccessDenied;

				File.Delete(newpath);
				File.Move(oldpath, newpath);
				return DokanResult.Success;
			}
			return DokanResult.FileExists;
		}

		public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info) {
			if (info.Context == null) // memory mapped read
			{
				string path = GetPath(fileName);
				if (path == null) {
					bytesRead = 0;
					return DokanResult.Success;
				} else {
					using (var stream = new FileStream(GetPath(fileName), FileMode.Open, System.IO.FileAccess.Read)) {
						stream.Position = offset;
						bytesRead = stream.Read(buffer, 0, buffer.Length);
					}
				}	
			} else // normal read
			  {
				var stream = info.Context as FileStream;
				lock (stream) //Protect from overlapped read
				{
					stream.Position = offset;
					bytesRead = stream.Read(buffer, 0, buffer.Length);
				}
			}

			return DokanResult.Success;
		}

		public NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info) {
			try {
				((FileStream)(info.Context)).SetLength(length);
				return DokanResult.Success;
			} catch (IOException) {
				return DokanResult.DiskFull;
			}
		}

		public NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info) {
			try {
				((FileStream)(info.Context)).SetLength(length);
				return DokanResult.Success;
			} catch (IOException) {
				return DokanResult.DiskFull;
			}
		}

		public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info) {
			try {
				File.SetAttributes(GetPath(fileName), attributes);
				return DokanResult.Success;
			} catch (UnauthorizedAccessException) {
				return DokanResult.AccessDenied;
			} catch (FileNotFoundException) {
				return DokanResult.FileNotFound;
			} catch (DirectoryNotFoundException) {
				return DokanResult.PathNotFound;
			}
		}

		public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info) {
			try {
				if (info.IsDirectory) {
					Directory.SetAccessControl(GetPath(fileName), (DirectorySecurity)security);
				} else {
					File.SetAccessControl(GetPath(fileName), (FileSecurity)security);
				}
				return DokanResult.Success;
			} catch (UnauthorizedAccessException) {
				return DokanResult.AccessDenied;
			}
		}

		public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info) {
			try {
				string path = GetPath(fileName);
				if (creationTime.HasValue)
					File.SetCreationTime(path, creationTime.Value);

				if (lastAccessTime.HasValue)
					File.SetLastAccessTime(path, lastAccessTime.Value);

				if (lastWriteTime.HasValue)
					File.SetLastWriteTime(path, lastWriteTime.Value);

				return DokanResult.Success;
            } catch (UnauthorizedAccessException) {
				return DokanResult.AccessDenied;
            } catch (FileNotFoundException) {
				return DokanResult.FileNotFound;
            } catch (Exception) {
				return DokanResult.Success; // I don't even care
			}
		}

		public NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info) {
			return DokanResult.Success;
		}

		public NtStatus Unmounted(DokanFileInfo info) {
			return DokanResult.Success;
		}

		public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info) {
			if (info.Context == null) {
				using (var stream = new FileStream(GetPath(fileName), FileMode.Open, System.IO.FileAccess.Write)) {
					stream.Position = offset;
					stream.Write(buffer, 0, buffer.Length);
					bytesWritten = buffer.Length;
				}
			} else {
				var stream = info.Context as FileStream;
				lock (stream) //Protect from overlapped write
				{
					stream.Position = offset;
					stream.Write(buffer, 0, buffer.Length);
				}
				bytesWritten = buffer.Length;
			}
			return DokanResult.Success;
		}
	}
}
