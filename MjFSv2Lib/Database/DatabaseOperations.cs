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

namespace MjFSv2Lib.Database {
	/// <summary>
	/// Represents a database connection including all allowed operations on the database.
	/// </summary>
	public class DatabaseOperations {
		private readonly SQLiteConnection _connection;

		public DatabaseOperations (SQLiteConnection con) {
			this._connection = con;
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
		public List<Item> GetItemsByCompositeTag(List<Tag> tags) {
			int c = tags.Count;

			//The following piece of code is a bit dramatic in that it creates an SQL query on-demand #YOTH16

			// Build FROM clause
			StringBuilder sbFrom = new StringBuilder("FROM");
			for (int i = 0; i < c; i++) {
				if (i != 0) {
					sbFrom.Append(",");
				}
				sbFrom.Append(" ItemTag a" + i);
			}

			// Build WHERE clause
			StringBuilder sbWhere = new StringBuilder(" WHERE ");
			for (int i = 0; i < c; i++) {
				sbWhere.Append("a" + i + ".tagId = @t" + i + " AND ");
			}

			// Build last part of WHERE clause
			StringBuilder sbClause = new StringBuilder();
			if (c > 1) {
				for (int i = 0; i < c; i++) {
					if (i != 0) {
						sbClause.Append(" = ");
					}
					sbClause.Append("a" + i + ".itemId");
				}
			} else {
				// Remove the final AND from the where clause
				sbWhere.Remove(sbWhere.Length - 5, 5);
			}

			// SQL prepared statement
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT a0.itemId " + sbFrom.ToString() + sbWhere.ToString() + sbClause.ToString();
			cmd.Prepare();

			// Fill all parameters with their corresponding values
			for (int i = 0; i < c; i++) {
				cmd.Parameters.AddWithValue("@t" + i, tags[i].Id);
			}
			SQLiteDataReader r = cmd.ExecuteReader();
			List<Item> itemList = new List<Item>();
			while (r.Read()) {
				itemList.Add(GetItem(r[0].ToString()));
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
				Tag t = new Tag(r[0].ToString(), true);
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
			cmd.CommandText = "SELECT * FROM Tag WHERE label = @label";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@label", label);
			SQLiteDataReader r = cmd.ExecuteReader();
			if (r.Read()) {
				return new Tag(r[0].ToString(), Convert.ToBoolean(r[1]));
			} else {
				return new Tag("No tag", false);
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
			cmd.CommandText = "SELECT id, label, rootVisible FROM ItemTag, Tag WHERE itemID = @itemID AND tagId = id";
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
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "SELECT tagID From ExtTagMap WHERE ext LIKE '%," + item.Extension + ",%'";
			cmd.Prepare();
			SQLiteDataReader r = cmd.ExecuteReader();
			if (r.Read()) {
				string tagId = r[0].ToString();
				return InsertItemTag(item, new Tag(tagId, false));
			} else {
				return 0;
			}
		}

		/// <summary>
		/// Insert an extension to tag mapping
		/// </summary>
		/// <param name="extension"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public int InsertExtTagMap(string extension, Tag tag) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "INSERT INTO `ExtTagMap`(`ext`,`tagId`) VALUES(@ext, @tagId);";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@ext", extension);
			cmd.Parameters.AddWithValue("@tagId", tag.Id);
			return cmd.ExecuteNonQuery();
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
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "INSERT INTO ItemTag(itemId, tagId) VALUES (@itemId, @tagId)";
			cmd.Prepare();
			cmd.Parameters.AddWithValue("@itemId", item.Id);
			cmd.Parameters.AddWithValue("@tagId", tag.Id);
			return cmd.ExecuteNonQuery();
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
			cmd.Parameters.AddWithValue("@id", tag.Id);
			cmd.Parameters.AddWithValue("@rootVisible", Convert.ToInt32(tag.RootVisible));
			return cmd.ExecuteNonQuery();
		}

		public int TruncateTable(string tableName) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			cmd.CommandText = "DELETE FROM " + tableName;
			cmd.Prepare();
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

		/// <summary>
		/// Add the tables to an empty database
		/// </summary>
		/// <param name="bagPath"></param>
		/// <returns></returns>
		public int AddTables(string bagPath) {
			SQLiteCommand cmd = new SQLiteCommand(_connection);
			string databaseTables = Properties.Resources.database;
			cmd.CommandText = databaseTables;
			cmd.Parameters.AddWithValue("@defaultBag", bagPath);
			cmd.Parameters.AddWithValue("@version", DatabaseManager.CURRENT_DB_VER);
			cmd.Prepare();
			return cmd.ExecuteNonQuery();
		}
	}
}
