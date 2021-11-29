/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version 
* 3 of the License.  See LICENSE for details
*
* 본 프로그램 및 관련 소스코드, 문서 등 모든 자료는 있는 그대로 제공이 됩니다.
* KSAN 프로젝트의 개발자 및 개발사는 이 프로그램을 사용한 결과에 따른 어떠한 책임도 지지 않습니다.
* KSAN 개발팀은 사전 공지, 허락, 동의 없이 KSAN 개발에 관련된 모든 결과물에 대한 LICENSE 방식을 변경 할 권리가 있습니다.
*/
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using log4net;
using System.Reflection;
using System.Threading;

namespace IfsSync2Data
{
    class TaskDataSqlManager
    {
        private readonly string CLASS_NAME = "TaskDataSqlManager";
        /********************************************************************************/
        private const string SQLITE_SEQUENCE = "sqlite_sequence";
        private const string STR_TASK_TABLE_NAME = "TaskList";
        private const string STR_PENDING_TABLE_NAME = "PendingList";
        private const string STR_SUCCESS_TABLE_NAME = "SuccessTaskList";
        private const string STR_FAILURE_TABLE_NAME = "FailureTaskList";
        private const string STR_INDEX_NAME = "ID";
        private const string STR_TASK_NAME = "TaskName";
        private const string STR_FILEPATH = "FilePath";
        private const string STR_NEW_FILE_PATH = "NewFilePath";
        private const string STR_SNAPSHOT_PATH = "SnapshotPath";
        private const string STR_FILE_SIZE = "FileSize";
        private const string STR_EVENTTIME = "EventTime";
        private const string STR_UPLOADTIME = "UploadTime";
        private const string STR_RESULT = "Result";
        /***************** Service Log Attribute ******************************/
        private const string STR_LOG_TABLE_NAME = "LogList";
        private const string STR_LOG_NAME = "Log";
        /***************** Service Log Attribute ******************************/
        private const int DEFAULT_LIMIT = 3000;

        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Mutex SqliteMutex;
        private readonly string FilePath;
        //private readonly string JobName;

        public readonly List<TaskData> TaskDatas = new List<TaskData>();

        public TaskDataSqlManager(string RootPath, string HostName, string JobName)
        {
            const string FUNCTION_NAME = "Init";

            CLASS_NAME += string.Format(":{0}:{1}", HostName, JobName);

            FilePath = MainData.CreateDBFileNameAndHostName(RootPath, HostName, JobName);
            string MUTEX_NAME = MainData.CreateMutexName(JobName);
            try
            {
                SqliteMutex = new Mutex(false, MUTEX_NAME, out bool CreatedNew);

                if (!CreatedNew) log.DebugFormat("[{0}:{1}] Mutex({2})", CLASS_NAME, FUNCTION_NAME, MUTEX_NAME);
                else log.DebugFormat("[{0}:{1}] Mutex({2}) create", CLASS_NAME, FUNCTION_NAME,MUTEX_NAME);

                if (!File.Exists(FilePath)) CreateDBFile();
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] Mutex({3}) fail : ", CLASS_NAME, FUNCTION_NAME, "Exception", MUTEX_NAME, e.Message);
            }
        }

        public int TaskCount { get { return TaskDatas.Count; } }
        public long TaskSize { get
            {
                long Sum = 0;
                foreach (TaskData Task in TaskDatas) Sum += Task.FileSize;
                return Sum;
            } }
        private bool CreateDBFile()
        {
            const string FUNCTION_NAME = "CreateDBFile";
            try
            {
                MainData.CreateDirectory(FilePath);
                SQLiteConnection.CreateFile(FilePath);
                
                SqliteMutex.WaitOne();
                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath)))
                {
                    conn.Open();
                    SQLiteCommand cmd = new SQLiteCommand(conn)
                    {
                        //Task List
                        CommandText =
                        string.Format(
                        "Create Table '{0}'(" + //STR_TASK_TABLE_NAME
                                     "'{1}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +//STR_INDEX_NAME
                                     "'{2}' TEXT NOT NULL, "    + //STR_TASK_NAME  
                                     "'{3}' TEXT NOT NULL, "    + //STR_FILEPATH   
                                     "'{4}' TEXT, "             + //STR_NEW_FILE_PATH
                                     "'{5}' INTEGER DEFAULT 0," + //STR_FILESIZE
                                     "'{6}' TEXT NOT NULL, "    + //STR_EVENTTIME
                                     "'{7}' TEXT);"             , //STR_UPLOADTIME 
                                     STR_TASK_TABLE_NAME,
                                     STR_INDEX_NAME,
                                     STR_TASK_NAME,
                                     STR_FILEPATH,
                                     STR_NEW_FILE_PATH,
                                     STR_FILE_SIZE,
                                     STR_EVENTTIME,
                                     STR_UPLOADTIME) +
                        //Panding List
                        string.Format(
                        "Create Table '{0}'(" + //STR_PENDING_TABLE_NAME
                                     "'{1}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +//STR_INDEX_NAME
                                     "'{2}' TEXT NOT NULL, "    + //STR_TASK_NAME  
                                     "'{3}' TEXT NOT NULL, "    + //STR_FILEPATH   
                                     "'{4}' TEXT, "             + //STR_NEW_FILE_PATH
                                     "'{5}' TEXT NOT NULL, "    + //STR_SNAPSHOT_PATH  
                                     "'{6}' INTEGER DEFAULT 0," + //STR_FILESIZE
                                     "'{7}' TEXT NOT NULL, "    + //STR_EVENTTIME  
                                     "'{8}' TEXT);"             , //STR_RESULT 
                                     STR_PENDING_TABLE_NAME,
                                     STR_INDEX_NAME,
                                     STR_TASK_NAME,
                                     STR_FILEPATH,
                                     STR_NEW_FILE_PATH,
                                     STR_SNAPSHOT_PATH,
                                     STR_FILE_SIZE,
                                     STR_EVENTTIME,
                                     STR_RESULT) +
                        ////Success
                        string.Format(
                        "Create Table '{0}'(" + //STR_TASK_TABLE_NAME
                                     "'{1}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +//STR_INDEX_NAME
                                     "'{2}' TEXT NOT NULL, "    + //STR_TASK_NAME  
                                     "'{3}' TEXT NOT NULL, "    + //STR_FILEPATH
                                     "'{4}' TEXT, "             + //STR_NEW_FILE_PATH   
                                     "'{5}' INTEGER DEFAULT 0," + //STR_FILESIZE
                                     "'{6}' TEXT NOT NULL, "    + //STR_EVENTTIME
                                     "'{7}' TEXT);"             , //STR_UPLOADTIME 
                                     STR_SUCCESS_TABLE_NAME,
                                     STR_INDEX_NAME,
                                     STR_TASK_NAME,
                                     STR_FILEPATH,
                                     STR_NEW_FILE_PATH,
                                     STR_FILE_SIZE,
                                     STR_EVENTTIME,
                                     STR_UPLOADTIME) +
                        //Failure
                        string.Format(
                        "Create Table '{0}'(" + //STR_TASK_TABLE_NAME
                                     "'{1}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +//STR_INDEX_NAME
                                     "'{2}' TEXT NOT NULL, "    + //STR_TASK_NAME  
                                     "'{3}' TEXT NOT NULL, "    + //STR_FILEPATH   
                                     "'{4}' TEXT, "             + //STR_NEW_FILE_PATH
                                     "'{5}' INTEGER DEFAULT 0," + //STR_FILESIZE
                                     "'{6}' TEXT NOT NULL, "    + //STR_EVENTTIME  
                                     "'{7}' TEXT NOT NULL, "    + //STR_UPLOADTIME 
                                     "'{8}' TEXT);"             , //STR_RESULT 
                                     STR_FAILURE_TABLE_NAME,
                                     STR_INDEX_NAME,
                                     STR_TASK_NAME,
                                     STR_FILEPATH,
                                     STR_NEW_FILE_PATH,
                                     STR_FILE_SIZE,
                                     STR_EVENTTIME,
                                     STR_UPLOADTIME,
                                     STR_RESULT) +
                        //Log List
                        string.Format(
                        "Create Table '{0}'(" + //STR_LOG_TABLE_NAME
                                     "'{1}' INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +//STR_INDEX_NAME
                                     "'{2}' TEXT NOT NULL);", //STR_LOG_NAME 
                                     STR_LOG_TABLE_NAME,
                                     STR_INDEX_NAME,
                                     STR_LOG_NAME) +
                                 "PRAGMA foreign_keys = On;"
                    };
                    cmd.ExecuteNonQuery();
                    conn.Close();

                    log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                }
                return true;
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }

        }
        public bool DeleteDBFile()
        {
            const string FUNCTION_NAME = "DeleteDBFile";

            if (File.Exists(FilePath))
            {
                AllClear();
                try
                {
                    SqliteMutex.WaitOne();

                    for(int i=0;i<5;i++)
                    {
                        if (new FileInfo(FilePath).Exists) File.Delete(FilePath);
                        else break;
                        Thread.Sleep(100);
                    }

                    log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, FilePath);

                    return true;
                }
                catch (Exception e)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                    return false;
                }
                finally
                {
                    SqliteMutex.ReleaseMutex();
                }
            }
            return false;
        }

        public bool Insert(TaskData Data)
        {
            const string FUNCTION_NAME = "Insert";
            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }

                SqliteMutex.WaitOne();
                conn.Open();
                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format(
                                    "INSERT INTO '{0}'  ( {1},   {3},   {5},  {7},   {9})" +
                                               " VALUES ('{2}', '{4}', '{6}', {8}, '{10}')",
                                    STR_TASK_TABLE_NAME,
                                    STR_TASK_NAME, Data.TaskName,
                                    STR_FILEPATH, Data.FilePath,
                                    STR_NEW_FILE_PATH, Data.NewFilePath,
                                    STR_FILE_SIZE, Data.FileSize,
                                    STR_EVENTTIME, Data.EventTime)
                };
                int result = cmd.ExecuteNonQuery();
                conn.Close();

                if (result > 0)
                {
                    log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                    return true;
                }
                else
                {
                    log.ErrorFormat("[{0}:{1}:{2}] Fail : {3}", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText);
                    return false;
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool Update(TaskData Data)
        {
            const string FUNCTION_NAME = "Update";
            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }
                SqliteMutex.WaitOne();
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = string.Format(
                                    "DELETE FROM '{0}' WHERE {1} = {2};",
                                    STR_TASK_TABLE_NAME, STR_INDEX_NAME, Data.Index);

                    if (Data.UploadFlag)
                    {
                        cmd.CommandText += string.Format(
                                    "\nINSERT INTO '{0}' ( {1},   {3},   {5},  {7},   {9} ,  {11} )" +
                                               " VALUES  ('{2}', '{4}', '{6}', {8}, '{10}', '{12}');",
                                    STR_SUCCESS_TABLE_NAME,
                                    STR_TASK_NAME, Data.TaskName,
                                    STR_FILEPATH, Data.FilePath,
                                    STR_NEW_FILE_PATH, Data.NewFilePath,
                                    STR_FILE_SIZE, Data.FileSize,
                                    STR_UPLOADTIME, Data.UploadTime,
                                    STR_EVENTTIME, Data.EventTime);
                    }
                    else
                    {
                        cmd.CommandText += string.Format(
                                    "\nINSERT INTO '{0}' ( {1},   {3},   {5},  {7},   {9},   {11} ,  {13} )" +
                                               " VALUES  ('{2}', '{4}', '{6}', {8}, '{10}', '{12}', '{14}');",
                                    STR_FAILURE_TABLE_NAME,
                                    STR_TASK_NAME, Data.TaskName,
                                    STR_FILEPATH, Data.FilePath,
                                    STR_NEW_FILE_PATH, Data.NewFilePath,
                                    STR_FILE_SIZE, Data.FileSize,
                                    STR_EVENTTIME, Data.EventTime,
                                    STR_UPLOADTIME, Data.UploadTime,
                                    STR_RESULT, Data.Result.Replace("'", "\""));
                    }
                    int result = cmd.ExecuteNonQuery();
                    conn.Close();

                    if (result > 0)
                    {
                        log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                        return true;
                    }
                    else
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] Fail : {3}", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText);
                        return false;
                    }
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool Delete(TaskData Data)
        {
            const string FUNCTION_NAME = "Delete";
            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }
                SqliteMutex.WaitOne();
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = string.Format(
                                    "DELETE FROM '{0}' WHERE {1} = {2};",
                                    STR_TASK_TABLE_NAME, STR_INDEX_NAME, Data.Index);
                    int result = cmd.ExecuteNonQuery();
                    conn.Close();

                    if (result > 0)
                    {
                        log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                        return true;
                    }
                    else
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] Fail : {3}", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText);
                        return false;
                    }
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool Panding(TaskData Data)
        {
            const string FUNCTION_NAME = "Panding";
            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }
                SqliteMutex.WaitOne();
                conn.Open();

                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {

                    cmd.CommandText = string.Format(
                                    "DELETE FROM '{0}' WHERE {1} = {2};",
                                    STR_TASK_TABLE_NAME, STR_INDEX_NAME, Data.Index);
                    cmd.CommandText += string.Format(
                                "\nINSERT INTO '{0}' ( {1},   {3},   {5},  {7},   {9},   {11} ,  {13} )" +
                                           " VALUES  ('{2}', '{4}', '{6}', {8}, '{10}', '{12}', '{14}');",
                                STR_FAILURE_TABLE_NAME,
                                STR_TASK_NAME, Data.TaskName,
                                STR_FILEPATH, Data.FilePath,
                                STR_NEW_FILE_PATH, Data.NewFilePath,
                                STR_FILE_SIZE, Data.FileSize,
                                STR_EVENTTIME, Data.EventTime,
                                STR_UPLOADTIME, Data.UploadTime,
                                STR_RESULT, Data.Result.Replace("'", "\""));

                    int result = cmd.ExecuteNonQuery();
                    conn.Close();

                    if (result > 0)
                    {
                        log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                        return true;
                    }
                    else
                    {
                        log.ErrorFormat("[{0}:{1}:{2}] Fail : {3}", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText);
                        return false;
                    }
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public List<TaskData> GetSuccessList(int StartIndex = 0, int Limit = DEFAULT_LIMIT)
        {
            const string FUNCTION_NAME = "GetSuccessList";

            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    throw new Exception();
                }

                SqliteMutex.WaitOne();
                conn.Open();

                List<TaskData> SuccessList = new List<TaskData>();

                SQLiteCommand cmd = new SQLiteCommand(conn)
                { CommandText = string.Format("SELECT * FROM '{0}' Limit {1} OFFSET {2}", STR_SUCCESS_TABLE_NAME, Limit, StartIndex) };

                SQLiteDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    TaskData Data = new TaskData
                    {
                        Index       = Convert.ToInt64(rdr[STR_INDEX_NAME]),
                        StrTaskName = rdr[STR_TASK_NAME].ToString(),
                        FilePath    = rdr[STR_FILEPATH].ToString(),
                        NewFilePath = rdr[STR_NEW_FILE_PATH].ToString(),
                        FileSize    = Convert.ToInt64(rdr[STR_FILE_SIZE]),
                        EventTime   = rdr[STR_EVENTTIME].ToString(),
                        UploadTime  = rdr[STR_UPLOADTIME].ToString()
                    };

                    SuccessList.Add(Data);
                }
                rdr.Close();
                conn.Close();

                log.DebugFormat("[{0}:{1}] List Count : {2}", CLASS_NAME, FUNCTION_NAME, SuccessList.Count);
                return SuccessList;
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                throw e;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                throw e;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public List<TaskData> GetFailureList(int StartIndex = 0, int Limit = DEFAULT_LIMIT)
        {
            const string FUNCTION_NAME = "GetFailureList";

            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    throw new Exception();
                }

                SqliteMutex.WaitOne();
                conn.Open();

                List<TaskData> FailureList = new List<TaskData>();

                SQLiteCommand cmd = new SQLiteCommand(conn)
                { CommandText = string.Format("SELECT * FROM '{0}' Limit {1} OFFSET {2}", STR_FAILURE_TABLE_NAME, Limit, StartIndex) };

                SQLiteDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    TaskData Data = new TaskData
                    {
                        Index       = Convert.ToInt64(rdr[STR_INDEX_NAME]),
                        StrTaskName = rdr[STR_TASK_NAME].ToString(),
                        FilePath    = rdr[STR_FILEPATH].ToString(),
                        NewFilePath = rdr[STR_NEW_FILE_PATH].ToString(),
                        FileSize    = Convert.ToInt64(rdr[STR_FILE_SIZE]),
                        EventTime   = rdr[STR_EVENTTIME].ToString(),
                        UploadTime  = rdr[STR_UPLOADTIME].ToString(),
                        Result      = rdr[STR_RESULT].ToString()
                    };

                    FailureList.Add(Data);
                }
                rdr.Close();
                conn.Close();

                log.DebugFormat("[{0}:{1}] List Count : {2}", CLASS_NAME, FUNCTION_NAME, FailureList.Count);
                return FailureList;
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                throw e;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                throw e;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }

        public bool Clear()
        {
            const string FUNCTION_NAME = "Clear";
            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }

                SqliteMutex.WaitOne();
                conn.Open();
                //UPDATE TaskList SET UploadTime = 'test', UploadFlag = true WHERE ID = 1;
                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format( "DELETE FROM '{0}';", STR_TASK_TABLE_NAME)
                };

                int result = cmd.ExecuteNonQuery();
                conn.Close();

                if (result > 0)
                {
                    log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                    return true;
                }
                else
                {
                    log.ErrorFormat("[{0}:{1}:{2}] Fail : {3}", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText);
                    return false;
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool AllClear()
        {
            const string FUNCTION_NAME = "Clear";
            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }

                SqliteMutex.WaitOne();
                conn.Open();
                //UPDATE TaskList SET UploadTime = 'test', UploadFlag = true WHERE ID = 1;
                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format("DELETE FROM '{0}'; DELETE FROM '{1}'; DELETE FROM '{2}'; DELETE FROM '{3}'; DELETE FROM '{4}'; DELETE FROM '{5}';",
                                                STR_TASK_TABLE_NAME, STR_PENDING_TABLE_NAME, STR_SUCCESS_TABLE_NAME, STR_FAILURE_TABLE_NAME, STR_LOG_TABLE_NAME, SQLITE_SEQUENCE)
                };

                int result = cmd.ExecuteNonQuery();
                conn.Close();

                if (result > 0)
                {
                    log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                    return true;
                }
                else
                {
                    log.ErrorFormat("[{0}:{1}:{2}] Fail : {3}", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText);
                    return false;
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public bool GetList(int Limit = 3000)
        {
            const string FUNCTION_NAME = "GetList";

            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }

                SqliteMutex.WaitOne();
                conn.Open();

                TaskDatas.Clear();

                SQLiteCommand cmd = new SQLiteCommand(conn)
                { CommandText = string.Format("SELECT * FROM '{0}' Limit {1}", STR_TASK_TABLE_NAME, Limit) };
                
                SQLiteDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    TaskData Data = new TaskData
                    {
                        Index       = Convert.ToInt64(rdr[STR_INDEX_NAME]),
                        StrTaskName = rdr[STR_TASK_NAME].ToString(),
                        FilePath    = rdr[STR_FILEPATH].ToString(),
                        NewFilePath = rdr[STR_NEW_FILE_PATH].ToString(),
                        FileSize    = Convert.ToInt64(rdr[STR_FILE_SIZE]),
                        EventTime   = rdr[STR_EVENTTIME].ToString(),
                        UploadTime  = rdr[STR_UPLOADTIME].ToString()
                    };

                    TaskDatas.Add(Data);
                }
                rdr.Close();
                conn.Close();

                log.DebugFormat("[{0}:{1}] List Count : {2}", CLASS_NAME, FUNCTION_NAME, TaskDatas.Count);
                return true;
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        
        public bool InsertLog(string Msg)
        {
            const string FUNCTION_NAME = "InsertLog";
            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    return false;
                }

                SqliteMutex.WaitOne();
                conn.Open();
                string MyLog = string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Msg);
                SQLiteCommand cmd = new SQLiteCommand(conn)
                {
                    CommandText = string.Format("INSERT INTO '{0}' ({1}) VALUES ('{2}')", STR_LOG_TABLE_NAME, STR_LOG_NAME, MyLog)
                };
                int result = cmd.ExecuteNonQuery();
                conn.Close();

                if (result > 0)
                {
                    log.DebugFormat("[{0}:{1}] Success : {2}", CLASS_NAME, FUNCTION_NAME, cmd.CommandText);
                    return true;
                }
                else
                {
                    log.ErrorFormat("[{0}:{1}:{2}] Fail : {3}", CLASS_NAME, FUNCTION_NAME, "ExecuteNonQuery", cmd.CommandText);
                    return false;
                }
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
        public List<string> GetLog(long StartIndex = 0)
        {

            const string FUNCTION_NAME = "GetLog";

            try
            {
                SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0};Version=3;", FilePath));
                if (conn == null)
                {
                    log.ErrorFormat("[{0}:{1}:{2}] SQLiteConnection fail", CLASS_NAME, FUNCTION_NAME, "SQLiteConnection");
                    throw new Exception();
                }

                SqliteMutex.WaitOne();
                conn.Open();

                List<string> LogList = new List<string>();

                SQLiteCommand cmd = new SQLiteCommand(conn)
                { CommandText = string.Format("SELECT * FROM {0} LIMIT 50000 OFFSET {1};", STR_LOG_TABLE_NAME, StartIndex) };

                SQLiteDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    LogList.Add(rdr[STR_LOG_NAME].ToString());
                }
                rdr.Close();
                conn.Close();

                log.DebugFormat("[{0}:{1}] List Count : {2}", CLASS_NAME, FUNCTION_NAME, LogList.Count);
                return LogList;
            }
            catch (SQLiteException e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "SQLiteException", e.Message);
                throw e;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                throw e;
            }
            finally
            {
                SqliteMutex.ReleaseMutex();
            }
        }
    }
}
