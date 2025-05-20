/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version 
* 3 of the License. See LICENSE for details
*
* 본 프로그램 및 관련 소스코드, 문서 등 모든 자료는 있는 그대로 제공이 됩니다.
* KSAN 프로젝트의 개발자 및 개발사는 이 프로그램을 사용한 결과에 따른 어떠한 책임도 지지 않습니다.
* KSAN 개발팀은 사전 공지, 허락, 동의 없이 KSAN 개발에 관련된 모든 결과물에 대한 LICENSE 방식을 변경 할 권리가 있습니다.
*/
using log4net;
using System.Data.SQLite;
using System.Text;

namespace IfsSync2Common
{
	public class ExtensionDbManager
	{
		#region Attributes
		readonly ILog _log = LogManager.GetLogger(typeof(ExtensionDbManager));
		readonly string _filePath = IfsSync2Utilities.GetDBFilePath(IfsSync2Constants.EXTENSION_NAME);
		readonly Mutex _sqliteMutex;
		#endregion

		public ExtensionDbManager()
		{
			try
			{
				_sqliteMutex = new Mutex(false, IfsSync2Constants.MUTEX_NAME_JOB_SQL, out bool createdNew);

				if (!createdNew) _log.Debug($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL})");
				else _log.Debug($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL}) create");
			}
			catch (Exception e)
			{
				_log.Error($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL}) fail", e);
				throw new InvalidOperationException($"Mutex({IfsSync2Constants.MUTEX_NAME_JOB_SQL}) fail", e);
			}
		}

		bool CreateDBFile()
		{
			try
			{
				IfsSync2Utilities.CreateFile(_filePath);

				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();
				using var cmd = new SQLiteCommand(conn)
				{
					//GlobalScheduleList
					CommandText = "\n" +
					$"Create Table IF NOT EXISTS '{IfsSync2Constants.DB_TABLE_EXTENSION_LIST}'(" +
								 $"'{IfsSync2Constants.DB_FIELD_ID}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
								 $"'{IfsSync2Constants.DB_FIELD_EXTENSION}' TEXT NOT NULL, " +
								 $"'{IfsSync2Constants.DB_FIELD_GROUP}' TEXT NULL);"
				};

				cmd.ExecuteNonQuery();

				_log.Debug($"Success : {cmd.CommandText}");
				return true;
			}
			catch (SQLiteException e) { _log.Error(e); return false; }
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public List<string> GetExtensionList()
		{
			var items = new List<string>();
			try
			{
				if (!File.Exists(_filePath)) DefaultExtensionList();

				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT * FROM '{IfsSync2Constants.DB_TABLE_EXTENSION_LIST}'" };
				using var rdr = cmd.ExecuteReader();

				while (rdr.Read())
				{
					var item = rdr.GetString(IfsSync2Constants.DB_FIELD_EXTENSION);
					if (!string.IsNullOrEmpty(item)) items.Add(item);
				}
				_log.Debug($"{IfsSync2Constants.DB_FIELD_EXTENSION} : {items.Count}");
			}
			catch (Exception e) { _log.Error(e); }
			finally { _sqliteMutex.ReleaseMutex(); }
			return items;
		}

		public bool Insert(string extension)
		{
			if (!File.Exists(_filePath) && !CreateDBFile()) return false;

			try
			{
				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"INSERT INTO '{IfsSync2Constants.DB_TABLE_EXTENSION_LIST}' ({IfsSync2Constants.DB_FIELD_EXTENSION}) VALUES ('{extension}');" };

				int result = cmd.ExecuteNonQuery();
				if (result > 0)
				{
					_log.Debug($"Success : {cmd.CommandText}");
					return true;
				}
				else
				{
					_log.Error($"Failed : {cmd.CommandText}");
					return false;
				}
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}
		public bool Insert(List<string> extensions)

		{
			if (!File.Exists(_filePath) && !CreateDBFile()) return false;

			try
			{
				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				var query = new StringBuilder();

				foreach (string ext in extensions)
				{
					query.Append($"INSERT INTO '{IfsSync2Constants.DB_TABLE_EXTENSION_LIST}' ({IfsSync2Constants.DB_FIELD_EXTENSION}) VALUES ('{ext}');\n");
				}

				using var cmd = new SQLiteCommand(conn) { CommandText = query.ToString() };

				int result = cmd.ExecuteNonQuery();
				if (result > 0)
				{
					_log.Debug($"Success({result}) : {cmd.CommandText}");
					return true;
				}
				else
				{
					_log.Error($"Failed({result}) : {cmd.CommandText}");
					return false;
				}
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public bool Delete(string extension)
		{
			if (!File.Exists(_filePath)) return false;

			try
			{
				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"DELETE FROM '{IfsSync2Constants.DB_TABLE_EXTENSION_LIST}' WHERE {IfsSync2Constants.DB_FIELD_EXTENSION} = '{extension}'" };

				int result = cmd.ExecuteNonQuery();
				if (result > 0)
				{
					_log.Debug($"Success({result}) : {cmd.CommandText}");
					return true;
				}
				else
				{
					_log.Error($"Failed({result}) : {cmd.CommandText}");
					return false;
				}
			}
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		public bool Check(string extension)
		{
			if (!File.Exists(_filePath) && !CreateDBFile()) return false;

			try
			{
				_sqliteMutex.WaitOne();
				using var conn = new SQLiteConnection($"Data Source={_filePath};Version=3;");
				conn.Open();

				using var cmd = new SQLiteCommand(conn) { CommandText = $"SELECT count(*) FROM '{IfsSync2Constants.DB_TABLE_EXTENSION_LIST}' WHERE {IfsSync2Constants.DB_FIELD_EXTENSION} = '{extension}';" };

				int result = Convert.ToInt32(cmd.ExecuteScalar());
				if (result > 0)
				{
					_log.Debug($"Success({result}) : {cmd.CommandText}");
					return true;
				}
				else
				{
					_log.Error($"Failed({result}) : {cmd.CommandText}");
					return false;
				}
			}
			catch (SQLiteException e) { _log.Error(e); return false; }
			catch (Exception e) { _log.Error(e); return false; }
			finally { _sqliteMutex.ReleaseMutex(); }
		}

		void DefaultExtensionList()
		{
			Insert([.. IfsSync2Constants.DEFAULT_EXTENSION_LIST.Replace(" ", "").Split(',')]);
		}
	}
}