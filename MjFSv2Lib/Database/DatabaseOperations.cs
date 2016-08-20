using MjFSv2Lib.Manager;
using MjFSv2Lib.Model;
using MjFSv2Lib.Util;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text;

namespace MjFSv2Lib.Database {
	/// <summary>
	/// Represents a database connection including all allowed operations on the database.
	/// </summary>
	public class DatabaseOperations {
		private readonly SQLiteConnection _connection;
		private String _bagPath;

		public DatabaseOperations (SQLiteConnection con) {
			this._connection = con;

			if (IsAlive()) {
				try {
					_bagPath = Path.GetPathRoot(con.FileName) + this.GetBagLocation() + "\\";
					DebugLogger.Log("Initialized DatabaseOperations for '" + _bagPath + "'");
				} catch(SQLiteException) {
					// IDK
				}		
			}		
        }

		/// <summary>
		/// Returns a boolean indicating whether the database file is in existence.
		/// </summary>
		/// <returns></returns>
		public bool IsAlive() {
			string path = _connection.FileName;
			if (path != null) {
				return File.Exists(path);
			} else {
				return false;
			}
		}

		/// <summary>
		/// Remove a tag from the given item
		/// </summary>
		/// <param name="item"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public int DeleteItemTag(Item item, Tag tag) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "DELETE FROM ItemTag WHERE tagId = @tagId AND itemId = @itemId";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@tagId", tag.Id);
			cmd.Parameters.AddWithValue("@Itemid", item.Id);
			return cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Delete the item, also removing any tag associations
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int DeleteItem(Item item) {
			return DeleteItemOnly(item) + DeleteItemTags(item);
		}


		/// <summary>
		/// Delete the item without removing tag associatations
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int DeleteItemOnly(Item item) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "DELETE FROM Item WHERE id = @id";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", item.Id);
			return cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Delete all tags from the given item
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int DeleteItemTags(Item item) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "DELETE FROM ItemTag WHERE  itemId = @id";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", item.Id);
			return cmd.ExecuteNonQuery();
		}

		public string GetBagLocation() {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT location FROM Config";
			cmd.Prepare();
			SQLiteDataReader r = cmd.ExecuteReader();
			if (r.Read()) {
				return r[0].ToString();
			} else {
				return null;
			}
		}

		public int GetVersion() {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT version FROM Config";
			cmd.Prepare();
			SQLiteDataReader r = cmd.ExecuteReader();
			if (r.Read()) {
				return Convert.ToInt32(r[0]);
			} else {
				return 0;
			}
		}

		public Item GetItem(string id) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT * FROM Item WHERE id = @id";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", id);
			SQLiteDataReader r = cmd.ExecuteReader();

			if (r.Read()) {
				return new Item(r[0].ToString(),
					r[1].ToString(), r[2].ToString(),
					Convert.ToInt64(r[3].ToString()), Convert.ToDateTime(r[5].ToString()),
					Convert.ToDateTime(r[6].ToString()), Convert.ToDateTime(r[7].ToString()),
					Helper.GetFileAttributesFromInt(Convert.ToInt32(r[4].ToString())));
			} else {
				return null;
			}
		}

		/// <summary>
		/// Return all items for the given list of tags
		/// </summary>
		/// <param name="tags"></param>
		/// <returns></returns>
		public List<Item> GetItemsByCompositeTag(List<string> tags) {
			int c = tags.Count;
			List<Item> itemList = new List<Item>();

			// Build JOIN clauses
			StringBuilder sbJoin = new StringBuilder();
			for (int i = 0; i < c; i++) {
				sbJoin.Append("INNER JOIN ItemTag it" + i + " ON it" + i + ".tagId = @t" + i + " AND it" + i + ".itemId = i.id ");
			}

			// SQL prepared statement
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT i.* FROM Item i " + sbJoin.ToString();
			cmd.Prepare();

			// Fill all parameters with their corresponding values
			for (int i = 0; i < c; i++) {
				cmd.Parameters.AddWithValue("@t" + i, tags[i].ToLower());
			}
			SQLiteDataReader r = cmd.ExecuteReader();
			while (r.Read()) {
				Item it = new Item(r[0].ToString(),
					r[1].ToString(), r[2].ToString(),
					Convert.ToInt64(r[3].ToString()), Convert.ToDateTime(r[5].ToString()),
					Convert.ToDateTime(r[6].ToString()), Convert.ToDateTime(r[7].ToString()),
					Helper.GetFileAttributesFromInt(Convert.ToInt32(r[4].ToString())));

				itemList.Add(it);
			}
			return itemList;
		}

		/// <summary>
		/// Return all tags that have their root visibility set to true
		/// </summary>
		/// <returns></returns>
		public List<Tag> GetRootTags() {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT id FROM Tag WHERE rootVisible = 1";
			cmd.Prepare();
			SQLiteDataReader r = cmd.ExecuteReader();

			List<Tag> tagList = new List<Tag>();
			while (r.Read()) {
				Tag t = new Tag(r[0].ToString().ToLower(), true);
				tagList.Add(t);
			}
			return tagList;
		}

		/// <summary>
		/// Get a tag object by its label
		/// </summary>
		/// <param name="label"></param>
		/// <returns></returns>
		public Tag GetTag(string label) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT * FROM Tag WHERE id = @id";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", label);
			SQLiteDataReader r = cmd.ExecuteReader();
			if (r.Read()) {
				return new Tag(r[0].ToString(), Convert.ToBoolean(r[1]));
			} else {
				return null;
			}

		}

		/// <summary>
		/// Get the collection of all tags in the database
		/// </summary>
		/// <returns></returns>
		public List<Tag> GetTags() {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT * FROM Tag";
			cmd.Prepare();
			SQLiteDataReader r = cmd.ExecuteReader();

			List<Tag> tagList = new List<Tag>();
			while (r.Read()) {
				tagList.Add(new Tag(r[0].ToString(), Convert.ToBoolean(r[1])));
			}
			return tagList;
		}

		/// <summary>
		/// Return all tags associated with the given item
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public List<Tag> GetTagsByItem(Item item) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT id, rootVisible FROM ItemTag, Tag WHERE itemID = @itemID AND tagId = id";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@itemId", item.Id);
			SQLiteDataReader r = cmd.ExecuteReader();

			List<Tag> tagList = new List<Tag>();
			while (r.Read()) {
				tagList.Add(new Tag(r[0].ToString(), Convert.ToBoolean(r[1])));
			}
			return tagList;
		}

		/// <summary>
		/// Associate tags to an item based on extension
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int InsertDefaultItemTag(Item item) {
			int lines = 0;
			foreach (string t in TagHelper.ObtainTag(item, _bagPath)) {
				lines += InsertItemTag(item, t);
			}

			return lines;
		}

		public List<string> GetInnerTags(List<string> tags) {
			int c = tags.Count;
			List<string> itemList = new List<string>();
			if (c > 0) {
				//The following piece of code is a bit dramatic in that it creates an SQL query on-demand #YOTH16

				// Build JOIN clauses
				StringBuilder sbJoin = new StringBuilder();
				for (int i = 0; i < c; i++) {
					sbJoin.Append("INNER JOIN ItemTag it" + i + " ON it" + i + ".tagId = @t" + i + " AND it" + i + ".itemId = i.id ");
				}

				// SQL prepared statement
				SQLiteCommand cmd = new SQLiteCommand(_connection);
				cmd.CommandText = "SELECT DISTINCT `tagId` FROM ItemTag WHERE `itemId` IN (SELECT i.id FROM Item i " + sbJoin.ToString() + ")";
				cmd.Prepare();
				
				// Fill all parameters with their corresponding values
				for (int i = 0; i < c; i++) {
					cmd.Parameters.AddWithValue("@t" + i, tags[i].ToLower());
				}
				SQLiteDataReader r = cmd.ExecuteReader();
				
				while (r.Read()) {
					itemList.Add(r[0].ToString());
				}
			}		
			return itemList;
		}

		/// <summary>
		/// Insert an item into the database
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int InsertItem(Item item) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "INSERT INTO `Item`(`id`,`name`,`ext`,`size`,`attr`,`lat`,`lwt`,`ct`) VALUES (@id, @name, @ext, @size, @attr, @lat, @lwt, @ct)";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", item.Id);
			cmd.Parameters.AddWithValue("@name", item.Name);
			cmd.Parameters.AddWithValue("@ext", item.Extension);
			cmd.Parameters.AddWithValue("@size", item.Size);
			cmd.Parameters.AddWithValue("@attr", item.Attributes);
			cmd.Parameters.AddWithValue("@lat", item.LastAccesTime);
			cmd.Parameters.AddWithValue("@lwt", item.LastWriteTime);
			cmd.Parameters.AddWithValue("@ct", item.CreationTime);
			return cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Associate the tag to the given item
		/// </summary>
		/// <param name="item"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public int InsertItemTag(Item item, Tag tag) {
			return InsertItemTag(item, tag.Id);
		}

		public int InsertItemTag(Item item, string tag) {
			if (tag != null) {
				SQLiteCommand cmd = new SQLiteCommand(_connection);
				cmd.CommandText = "INSERT INTO ItemTag(itemId, tagId) VALUES (@itemId, @tagId)";
				cmd.Prepare();
				cmd.Parameters.AddWithValue("@itemId", item.Id);
				cmd.Parameters.AddWithValue("@tagId", tag.ToLower().Trim());
				return cmd.ExecuteNonQuery();
			} else {
				return -1;
			}
		}

		/// <summary>
		/// Insert a tag into the database
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		public int InsertTag(Tag tag) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "INSERT INTO Tag(id, rootVisible) VALUES (@id, @rootVisible)";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", tag.Id.ToLower().Trim());
			cmd.Parameters.AddWithValue("@rootVisible", Convert.ToInt32(false));
			return cmd.ExecuteNonQuery();
		}

		public int InsertTag(string tag) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "INSERT INTO Tag(id, rootVisible) VALUES (@id, 0)";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", tag.ToLower().Trim());
			return cmd.ExecuteNonQuery();
		}

		
		public int UpdateItem(Item item) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "UPDATE `Item` SET name = @name, ext = @ext, size=@size WHERE id = @id;";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@id", item.Id);
			cmd.Parameters.AddWithValue("@name", item.Name);
			cmd.Parameters.AddWithValue("@ext", item.Extension);
			cmd.Parameters.AddWithValue("@size", item.Size);
			return cmd.ExecuteNonQuery();
		}

		#region Hashing and hash checker methods

		public int UpdateHash() {
			string hash;
			using (var md5 = System.Security.Cryptography.MD5.Create()) {
				using (var stream = new FileStream(_connection.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "UPDATE Config SET hash = @hash;";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@hash", hash);
			return cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Check whether the database is in a sound state, e.g. the internal hashcode matches that of the file
		/// </summary>
		/// <returns></returns>
		public bool IsSound() {
			string hash;
			using (var md5 = System.Security.Cryptography.MD5.Create()) {
				using (var stream = new FileStream(_connection.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
				}
			}
			string dbHash = GetHash();
			if (hash != dbHash) {
				return false;
			} else {
				return true;
			}
		}

		/// <summary>
		/// Return the hash stored in the database
		/// </summary>
		/// <returns></returns>
		public string GetHash() {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT hash FROM Config";
			cmd.Prepare();
			SQLiteDataReader r = cmd.ExecuteReader();
			if (r.Read()) {
				return r[0].ToString();
			} else {
				return null;
			}
		}

		#endregion

		/// <summary>
		/// Add the tables to an empty database
		/// </summary>
		/// <param name="bagPath"></param>
		/// <returns></returns>
		public int AddTables(string bagPath) {
			this._bagPath = Path.GetPathRoot(_connection.FileName) + bagPath; // set the path to the bag for the first time.
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			string databaseTables = Properties.Resources.database;
			cmd.CommandText = databaseTables;
			cmd.Parameters.AddWithValue("@defaultBag", bagPath);
			cmd.Parameters.AddWithValue("@version", DatabaseManager.CURRENT_DB_VER);
			cmd.Prepare();
			return cmd.ExecuteNonQuery();
		}

		public int TruncateTable(string tableName) {
			//TODO: don't use direct user input
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "DELETE FROM " + tableName;
			cmd.Prepare();
			return cmd.ExecuteNonQuery();
		}


	}
}
