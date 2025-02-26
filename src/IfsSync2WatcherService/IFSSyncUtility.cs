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
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using IfsSync2Data;
using System.Collections.Generic;

namespace IfsSync2WatcherService
{
	class IFSSyncUtility
	{
		private const string STR_IP = "ip";
		private const string STR_HOSTNAME = "hostname";
		private const string STR_PORT = "port";
		private const string STR_MAC = "mac";
		private const string STR_OS = "os";
		private const string STR_GROUP = "group";
		private const string STR_PCNAME = "pcName";


		private const int PORT_NUMBER = 58443;

		private const string STR_ERR_MSG = "err_msg";
		private const string STR_RET = "ret";

		private const string STR_USERID = "userid";
		private const string STR_S3PROXY = "s3proxy";
		private const string STR_ACCESS_KEY = "access_key";
		private const string STR_ACCESS_SECRET = "access_secret";
		private const string STR_TENANT_KEY = "tenant";

		private const string STR_TARGET_USERID = "Target_Userid";
		private const string STR_CONF = "conf";

		private const string STR_SENDER_PAUSE = "sender_pause";
		private const string STR_SENDER_PAUSE_ON = "ON";
		private const string STR_SENDER_WAIT_MS = "sender_wait_ms";
		private const string STR_FETCH_COUNT = "fetch_count";
		private const string STR_DEBUG = "debug";
		private const string STR_SENDER_SCHEDULE = "sender_schedule";
		private const string STR_SENDER_SCHEDULE_ON = "ON";
		private const string STR_SENDER_START_TIME = "sender_start_time";
		private const string STR_SENDER_END_TIME = "sender_end_time";
		private const string STR_DELETE_COUNT = "delete_count";
		private const string STR_REV = "rev";

		private const string STR_JOBS = "jobs";
		private const string STR_JOB_PATH = "Path";
		private const string STR_JOB_WHITEFILE = "WhiteFile";
		private const string STR_JOB_WHITEFILEEXT = "WhiteFileExt";
		private const string STR_JOB_BLACKPATH = "BlackPath";
		private const string STR_JOB_BLACKFILE = "BlackFile";
		private const string STR_JOB_BLACKFILEEXT = "BlackFileExt";
		private const string STR_JOB_REMOVE = "Remove";
		private const string STR_JOB_REMOVE_ENABLE = "enable";

		private const string STR_SENDER_ALIVE = "senderAlive";
		private const string STR_LISTEN_ALIVE = "listenAlive";
		private const string STR_INI_STATUS = "iniStatus";
		private const string STR_MON_REMAIN = "monRemain";
		private const string STR_FAIL_REMAIN = "failRemain";

		private const string STR_USER = "user";
		private const string STR_COMPANY = "company";
		private const string STR_TYPE = "type";
		private const string STR_VERSION = "Version";

		private const string STR_TYPE_DEFAULT = "DEF";
		private const string STR_TYPE_SECURE = "SEC";//Secure Version
		private const string STR_TYPE_NETWORK = "NET";

		private const int DEFAULT_SENDER_DELAY = 1000;
		private const int DEFAULT_FETCH_COUNT = 1000;
		private const int DEFAULT_DELETE_COUNT = 1000;

		private static string GetMacAddress()
		{
			return NetworkInterface.GetAllNetworkInterfaces()[0].GetPhysicalAddress().ToString();
		}
		private static string GetIPAddress()
		{
			string IP = string.Empty;
			IPAddress[] Host = Dns.GetHostAddresses(Dns.GetHostName());
			foreach (var Item in Host)
			{
				if (Item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				{
					IP = Item.ToString();
					break;
				}
			}
			return IP;
		}
		public static UserData GetGlobalUser(string _URL, string PcName)
		{
			try
			{
				string URL;
				if (_URL.EndsWith("/")) URL = _URL + MainData.WATCHER_SERVICE_GET_USER;
				else URL = _URL + "/" + MainData.WATCHER_SERVICE_GET_USER;

				OperatingSystem os = Environment.OSVersion;
				string data = string.Format("{{" +
								"\"{0}\":\"{1}\", " +
								"\"{2}\":\"{3}\", " +
								"\"{4}\":\"{5}\", " +
								"\"{6}\":\"{7}\", " +
								"\"{8}\":\"{9}\", " +
								"\"{10}\":\"{11}\"," +
								"\"{12}\":\"{13}\"}}",
								STR_IP, GetIPAddress(),
								STR_HOSTNAME, Dns.GetHostName(),
								STR_PORT, PORT_NUMBER,
								STR_MAC, GetMacAddress(),
								STR_OS, os.Platform.ToString().ToUpper()[..3],
								STR_GROUP, "0",
								STR_PCNAME, PcName);

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
				request.Method = MainData.CURL_STR_POST_METHOD;
				request.ContentType = MainData.CURL_STR_CONTENT_TYPE;
				request.Timeout = MainData.CURL_TIMEOUT_DELAY;
				//Error evasion
				ServerCertificateValidationCallback.Ignore();
				// POST할 데이타를 Request Stream에 쓴다
				byte[] bytes = Encoding.ASCII.GetBytes(data);
				request.ContentLength = bytes.Length; // 바이트수 지정

				using (Stream reqStream = request.GetRequestStream())
				{
					reqStream.Write(bytes, 0, bytes.Length);
				}

				// Response 처리
				string responseText = string.Empty;
				using (WebResponse resp = request.GetResponse())
				{
					Stream respStream = resp.GetResponseStream();
					using (StreamReader sr = new(respStream)) { responseText = sr.ReadToEnd(); };
				};

				JObject UserObj = JObject.Parse(responseText);


				if (!int.TryParse(UserObj[STR_RET].ToString(), out int ret))
				{
					throw new Exception(STR_RET + " is Not int");
				}
				if (ret != 0)
				{
					string ErrorMsg = UserObj[STR_ERR_MSG].ToString();
					throw new Exception(ErrorMsg);
				}

				//Get User Data
				UserData User = new()
				{
					URL = UserObj[STR_S3PROXY].ToString(),
					UserName = UserObj[STR_USERID].ToString(),
					AccessKey = UserObj[STR_ACCESS_KEY].ToString(),
					SecretKey = UserObj[STR_ACCESS_SECRET].ToString(),
					StorageName = UserObj[STR_TENANT_KEY].ToString(),
					Debug = false
				};

				return User;
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		public static GlobalConfigData GetGlobalConfig(string _URL, string UserName, int UserID)
		{
			GlobalConfigData Config = new();
			try
			{
				string URL;
				if (_URL.EndsWith("/")) URL = _URL + MainData.WATCHER_SERVICE_GET_JOBS + UserName;
				else URL = _URL + "/" + MainData.WATCHER_SERVICE_GET_JOBS + UserName;

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
				request.Method = MainData.CURL_STR_GET_METHOD;
				request.ContentType = MainData.CURL_STR_CONTENT_TYPE;
				request.Timeout = MainData.CURL_TIMEOUT_DELAY;
				ServerCertificateValidationCallback.Ignore();

				// Response 처리
				string responseText = string.Empty;
				using (WebResponse resp = request.GetResponse())
				{
					Stream respStream = resp.GetResponseStream();
					using StreamReader sr = new(respStream);
					responseText = sr.ReadToEnd();
				}

				JObject jobj = JObject.Parse(responseText);

				if (!int.TryParse(jobj[STR_RET].ToString(), out int ret))
				{
					throw new Exception(STR_RET + " is Not int");
				}
				if (ret != 0)
				{
					string ErrorMsg = jobj[STR_ERR_MSG].ToString();
					throw new Exception(ErrorMsg);
				}

				string TargetUserid = jobj[STR_TARGET_USERID].ToString();

				if (int.TryParse(jobj[STR_CONF][STR_SENDER_WAIT_MS].ToString(), out int SenderDelay)) Config.SenderDelay = SenderDelay;
				else Config.SenderDelay = DEFAULT_SENDER_DELAY;

				if (int.TryParse(jobj[STR_CONF][STR_FETCH_COUNT].ToString(), out int FetchCount)) Config.FetchCount = FetchCount;
				else Config.FetchCount = DEFAULT_FETCH_COUNT;

				if (int.TryParse(jobj[STR_CONF][STR_DELETE_COUNT].ToString(), out int DeleteCount)) Config.DeleteCount = DeleteCount;
				else Config.DeleteCount = DEFAULT_DELETE_COUNT;

				if (STR_SENDER_PAUSE_ON.Equals(jobj[STR_CONF][STR_SENDER_PAUSE].ToString())) Config.SenderPause = true;
				else Config.SenderPause = false;

				Config.S3Proxy = jobj[STR_CONF][STR_S3PROXY].ToString();

				string Rev = jobj[STR_CONF][STR_REV].ToString();
				string Debug = jobj[STR_CONF][STR_DEBUG].ToString();

				string SenderSchedule = jobj[STR_CONF][STR_SENDER_SCHEDULE].ToString();
				string SenderStartTime = jobj[STR_CONF][STR_SENDER_START_TIME].ToString();
				string SenderEndTime = jobj[STR_CONF][STR_SENDER_END_TIME].ToString();

				JArray JobArray = JArray.Parse(jobj[STR_CONF][STR_JOBS].ToString());

				List<JobData> JobList = new();

				if (!TargetUserid.Equals(UserName))
					throw new Exception(string.Format("UserName Error : {0} != {1}", UserName, TargetUserid));

				foreach (var Job in JobArray)
				{
					JobData jobData = new()
					{
						IsGlobalUser = true,
						UserID = UserID
					};

					//JArray PathArray = JArray.Parse(Job[STR_JOB_PATH].ToString());
					//foreach (var Path in PathArray) jobData.Path.Add(Path.ToString());
					jobData.Path.Add(Job[STR_JOB_PATH].ToString());

					JArray WhiteFileArray = JArray.Parse(Job[STR_JOB_WHITEFILE].ToString());
					foreach (var WhiteFile in WhiteFileArray) jobData.WhiteFile.Add(WhiteFile.ToString());

					JArray WhiteFileExtArray = JArray.Parse(Job[STR_JOB_WHITEFILEEXT].ToString());
					foreach (var WhiteFileExt in WhiteFileExtArray) jobData.WhiteFileExt.Add(WhiteFileExt.ToString());

					JArray BlackPathArray = JArray.Parse(Job[STR_JOB_BLACKPATH].ToString());
					foreach (var BlackPath in BlackPathArray) jobData.BlackPath.Add(BlackPath.ToString());

					JArray BlackFileArray = JArray.Parse(Job[STR_JOB_BLACKFILE].ToString());
					foreach (var BlackFile in BlackFileArray) jobData.BlackFile.Add(BlackFile.ToString());

					JArray BlackFileExtArray = JArray.Parse(Job[STR_JOB_BLACKFILEEXT].ToString());
					foreach (var BlackFileExt in BlackFileExtArray) jobData.BlackFileExt.Add(BlackFileExt.ToString());

					if (Job[STR_JOB_REMOVE].ToString().Equals(STR_JOB_REMOVE_ENABLE)) jobData.Remove = true;
					else jobData.Remove = false;

					if (SenderSchedule.Equals(STR_SENDER_SCHEDULE_ON))
					{
						jobData.Policy = JobData.PolicyName.Schedule;

						GetStringToTime(SenderStartTime, out int StartHours, out int StartMins);
						GetStringToTime(SenderEndTime, out int EndHours, out int EndMins);

						Schedule schedule = new();
						schedule.SetAtTime(StartHours, StartMins);
						schedule.AddWeek(Schedule.EVERY);

						if (StartHours > EndHours) schedule.ForHours = (Schedule.MaxHours - StartHours) + EndHours;

						jobData.ScheduleList.Add(schedule);
					}
					else
						jobData.Policy = JobData.PolicyName.RealTime;

					JobList.Add(jobData);
					Config.JobList = JobList;
				}

				return Config;
			}
			catch (Exception e)
			{
				throw e;
			}
		}
		public static bool SendAlive(string URL, string ID, AliveData Data, out string Error)
		{
			try
			{
				OperatingSystem os = Environment.OSVersion;
				string url = URL + MainData.WATCHER_SERVICE_PUT_ALIVE;
				string data = string.Format("{{" +
								"\"{0}\":\"{1}\", " +
								"\"{2}\":\"{3}\", " +
								"\"{4}\":\"{5}\", " +
								"\"{6}\":\"{7}\", " +
								"\"{8}\":\"{9}\", " +
								"\"{10}\":\"{11}\", " +
								"\"{12}\":\"{13}\", " +
								"\"{14}\":\"{15}\"}}",
								STR_USERID, ID,
								STR_OS, os.Platform.ToString().ToUpper()[..3],
								STR_SENDER_ALIVE, Data.SenderAlive,
								STR_LISTEN_ALIVE, Data.ListenAlive,
								STR_INI_STATUS, Data.IniStatus,
								STR_MON_REMAIN, Data.MonRemain,
								STR_FAIL_REMAIN, Data.FailRemain,
								STR_PCNAME, Environment.MachineName);

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
				request.Method = MainData.CURL_STR_POST_METHOD;
				request.ContentType = MainData.CURL_STR_CONTENT_TYPE;
				request.Timeout = MainData.CURL_TIMEOUT_DELAY;
				//request.Headers.Add("Authorization", "BASIC SGVsbG8=");
				ServerCertificateValidationCallback.Ignore();
				// POST할 데이타를 Request Stream에 쓴다
				byte[] bytes = Encoding.ASCII.GetBytes(data);
				request.ContentLength = bytes.Length; // 바이트수 지정

				using (Stream reqStream = request.GetRequestStream()) { reqStream.Write(bytes, 0, bytes.Length); }

				// Response 처리
				string responseText = string.Empty;
				using (WebResponse resp = request.GetResponse())
				{
					Stream respStream = resp.GetResponseStream();
					using StreamReader sr = new(respStream);
					responseText = sr.ReadToEnd();
				}
				JObject jobj = JObject.Parse(responseText);


				if (!int.TryParse(jobj[STR_RET].ToString(), out int ret))
				{
					Error = STR_RET + " is Not int";
					return false;
				}

				if (ret != 0)
				{
					Error = jobj[STR_ERR_MSG].ToString();
					return false;
				}
				else
				{
					Error = string.Empty;
					return true;
				}
			}
			catch (Exception e)
			{
				Error = e.Message;
				return false;
			}

		}
		private static void GetStringToTime(string StrTime, out int Hours, out int Mins)
		{
			string[] result = StrTime.Split(':');

			Hours = Convert.ToInt32(result[0]);
			Mins = Convert.ToInt32(result[1]);
		}

		public static void CheckUpdate(string URL, string ID, string Version)
		{
			OperatingSystem os = Environment.OSVersion;
			string url = URL + MainData.WATCHER_SERVICE_VERSION_CHECK;
			string data = string.Format("{{" +
							"\"{0}\":\"{1}\", " +
							"\"{2}\":\"{3}\", " +
							"\"{4}\":\"{5}\", " +
							"\"{6}\":\"{7}\", " +
							"\"{8}\":\"{9}\"}} ",
							STR_USER, ID,
							STR_OS, os.Platform.ToString().ToLower()[..3],
							STR_COMPANY, MainData.COMPANY_NAME,
							STR_TYPE, "SEC",
							STR_VERSION, Version);

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = MainData.CURL_STR_POST_METHOD;
			request.ContentType = MainData.CURL_STR_CONTENT_TYPE;
			request.Timeout = MainData.CURL_TIMEOUT_DELAY;

			ServerCertificateValidationCallback.Ignore();
			// POST할 데이타를 Request Stream에 쓴다
			byte[] bytes = Encoding.ASCII.GetBytes(data);
			request.ContentLength = bytes.Length; // 바이트수 지정

			using (Stream reqStream = request.GetRequestStream())
			{
				reqStream.Write(bytes, 0, bytes.Length);
			}
			//Response 처리
			string responseText = string.Empty;
			using (WebResponse resp = request.GetResponse())
			{
				Stream respStream = resp.GetResponseStream();
				using StreamReader sr = new(respStream); responseText = sr.ReadToEnd();
			}

			JObject UserObj = JObject.Parse(responseText);

			if (!int.TryParse(UserObj[STR_RET].ToString(), out int ret))
			{
				throw new Exception(STR_RET + " is Not int");
			}
			if (ret != 0)
			{
				string ErrorMsg = UserObj[STR_ERR_MSG].ToString();
				throw new Exception(ErrorMsg);
			}

		}
	}

	public class ServerCertificateValidationCallback
	{
		public static void Ignore()
		{
			if (ServicePointManager.ServerCertificateValidationCallback == null)
			{
				ServicePointManager.ServerCertificateValidationCallback +=
				delegate (object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
				{
					return true;
				};
			}
		}
	}
}
