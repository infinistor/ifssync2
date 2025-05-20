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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using IfsSync2Common;
using System.Collections.Generic;
using IfsSync2WatcherService.Models;

namespace IfsSync2WatcherService
{
	public static class IfsPortalManager
	{
		private static readonly HttpClient httpClient = new HttpClient(new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
		})
		{
			Timeout = TimeSpan.FromMilliseconds(IfsSync2Constants.CURL_TIMEOUT_DELAY)
		};

		#region API 응답 관련 상수
		const string ERR_MSG = "err_msg";
		const string RET = "ret";
		#endregion

		#region 사용자 정보 관련 상수
		const string USERID = "userid";
		const string S3PROXY = "s3proxy";
		const string ACCESS_KEY = "access_key";
		const string ACCESS_SECRET = "access_secret";
		const string TENANT_KEY = "tenant";
		const string TARGET_USERID = "Target_Userid";
		#endregion

		#region 설정 관련 상수
		const string CONF = "conf";
		const string SENDER_PAUSE = "sender_pause";
		const string SENDER_PAUSE_ON = "ON";
		const string SENDER_WAIT_MS = "sender_wait_ms";
		const string FETCH_COUNT = "fetch_count";
		const string DEBUG = "debug";
		const string SENDER_SCHEDULE = "sender_schedule";
		const string SENDER_SCHEDULE_ON = "ON";
		const string SENDER_START_TIME = "sender_start_time";
		const string SENDER_END_TIME = "sender_end_time";
		const string DELETE_COUNT = "delete_count";
		const string REV = "rev";
		#endregion

		#region 작업 관련 상수
		const string JOBS = "jobs";
		const string JOB_PATH = "Path";
		const string JOB_WHITEFILE = "WhiteFile";
		const string JOB_WHITEFILEEXT = "WhiteFileExt";
		const string JOB_BLACKPATH = "BlackPath";
		const string JOB_BLACKFILE = "BlackFile";
		const string JOB_BLACKFILEEXT = "BlackFileExt";
		const string JOB_REMOVE = "Remove";
		const string JOB_REMOVE_ENABLE = "enable";
		#endregion

		private static string GetMacAddress()
		{
			return NetworkInterface.GetAllNetworkInterfaces()[0].GetPhysicalAddress().ToString();
		}
		private static string GetIPAddress()
		{
			string ip = string.Empty;
			IPAddress[] hosts = Dns.GetHostAddresses(Dns.GetHostName());
			foreach (var host in hosts)
			{
				if (host.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				{
					ip = host.ToString();
					break;
				}
			}
			return ip;
		}
		public static UserData GetGlobalUser(string addresss, string PcName)
		{
			string url = addresss + (addresss.EndsWith('/') ? "" : "/") + IfsSync2Constants.WATCHER_SERVICE_GET_USER;

			OperatingSystem os = Environment.OSVersion;

			// 객체로 정의하여 직렬화
			var requestData = new UserInfoRequest
			{
				Ip = GetIPAddress(),
				Hostname = Dns.GetHostName(),
				Port = IfsSync2Constants.PORT_NUMBER,
				Mac = GetMacAddress(),
				Os = os.Platform.ToString().ToUpper()[..3],
				Group = "0",
				PcName = PcName
			};

			string jsonData = JsonConvert.SerializeObject(requestData);
			var content = new StringContent(jsonData, Encoding.UTF8, IfsSync2Constants.CURL_CONTENT_TYPE);

			var response = httpClient.PostAsync(url, content).GetAwaiter().GetResult();
			response.EnsureSuccessStatusCode();

			string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			var userObj = JObject.Parse(responseText);

			if (!int.TryParse(userObj[RET].ToString(), out int ret))
				throw new FormatException(RET + " is Not int");
			if (ret != 0)
				throw new InvalidOperationException(userObj[ERR_MSG].ToString());

			//Get User Data
			UserData User = new()
			{
				URL = userObj[S3PROXY].ToString(),
				UserName = userObj[USERID].ToString(),
				AccessKey = userObj[ACCESS_KEY].ToString(),
				SecretKey = userObj[ACCESS_SECRET].ToString(),
				StorageName = userObj[TENANT_KEY].ToString(),
				Debug = false
			};

			return User;
		}
		public static GlobalConfigData GetGlobalConfig(string _URL, string UserName, int UserID)
		{
			GlobalConfigData config = new();
			string URL;
			if (_URL.EndsWith('/')) URL = _URL + IfsSync2Constants.WATCHER_SERVICE_GET_JOBS + UserName;
			else URL = _URL + "/" + IfsSync2Constants.WATCHER_SERVICE_GET_JOBS + UserName;

			var response = httpClient.GetAsync(URL).GetAwaiter().GetResult();
			response.EnsureSuccessStatusCode();

			string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			JObject jobj = JObject.Parse(responseText);

			if (!int.TryParse(jobj[RET].ToString(), out int ret))
			{
				throw new FormatException(RET + " is Not int");
			}
			if (ret != 0)
			{
				string ErrorMsg = jobj[ERR_MSG].ToString();
				throw new InvalidOperationException(ErrorMsg);
			}

			string TargetUserid = jobj[TARGET_USERID].ToString();

			if (int.TryParse(jobj[CONF][SENDER_WAIT_MS].ToString(), out int senderDelay)) config.SenderDelay = senderDelay;
			else config.SenderDelay = IfsSync2Constants.DEFAULT_SENDER_DELAY;

			if (int.TryParse(jobj[CONF][FETCH_COUNT].ToString(), out int fetchCount)) config.FetchCount = fetchCount;
			else config.FetchCount = IfsSync2Constants.DEFAULT_FETCH_COUNT;

			if (int.TryParse(jobj[CONF][DELETE_COUNT].ToString(), out int deleteCount)) config.DeleteCount = deleteCount;
			else config.DeleteCount = IfsSync2Constants.DEFAULT_DELETE_COUNT;

			if (SENDER_PAUSE_ON.Equals(jobj[CONF][SENDER_PAUSE].ToString())) config.SenderPause = true;
			else config.SenderPause = false;

			config.S3Proxy = jobj[CONF][S3PROXY].ToString();

			string rev = jobj[CONF][REV].ToString();
			string debug = jobj[CONF][DEBUG].ToString();

			string senderSchedule = jobj[CONF][SENDER_SCHEDULE].ToString();
			string senderStartTime = jobj[CONF][SENDER_START_TIME].ToString();
			string senderEndTime = jobj[CONF][SENDER_END_TIME].ToString();

			JArray jobArray = JArray.Parse(jobj[CONF][JOBS].ToString());

			List<JobData> jobList = [];

			if (!TargetUserid.Equals(UserName))
				throw new ArgumentException(string.Format("UserName Error : {0} != {1}", UserName, TargetUserid));

			foreach (var job in jobArray)
			{
				JobData jobData = new()
				{
					IsGlobalUser = true,
					UserId = UserID
				};

				//JArray pathArray = JArray.Parse(job[JOB_PATH].ToString());
				//foreach (var path in pathArray) jobData.Path.Add(path.ToString());
				jobData.Path.Add(job[JOB_PATH].ToString());

				JArray whiteFileArray = JArray.Parse(job[JOB_WHITEFILE].ToString());
				foreach (var whiteFile in whiteFileArray) jobData.WhiteFile.Add(whiteFile.ToString());

				JArray whiteFileExtArray = JArray.Parse(job[JOB_WHITEFILEEXT].ToString());
				foreach (var whiteFileExt in whiteFileExtArray) jobData.WhiteFileExt.Add(whiteFileExt.ToString());

				JArray blackPathArray = JArray.Parse(job[JOB_BLACKPATH].ToString());
				foreach (var blackPath in blackPathArray) jobData.BlackPath.Add(blackPath.ToString());

				JArray blackFileArray = JArray.Parse(job[JOB_BLACKFILE].ToString());
				foreach (var blackFile in blackFileArray) jobData.BlackFile.Add(blackFile.ToString());

				JArray blackFileExtArray = JArray.Parse(job[JOB_BLACKFILEEXT].ToString());
				foreach (var blackFileExt in blackFileExtArray) jobData.BlackFileExt.Add(blackFileExt.ToString());

				if (job[JOB_REMOVE].ToString().Equals(JOB_REMOVE_ENABLE)) jobData.Remove = true;
				else jobData.Remove = false;

				if (senderSchedule.Equals(SENDER_SCHEDULE_ON))
				{
					jobData.Policy = JobData.PolicyType.Schedule;

					GetStringToTime(senderStartTime, out int startHours, out int startMins);
					GetStringToTime(senderEndTime, out int endHours, out int endMins);

					Schedule schedule = new();
					schedule.SetAtTime(startHours, startMins);
					schedule.AddDay(WeekDays.Every);

					if (startHours > endHours) schedule.ForHours = Schedule.MaxHours - startHours + endHours;

					jobData.ScheduleList.Add(schedule);
				}
				else
					jobData.Policy = JobData.PolicyType.RealTime;

				jobList.Add(jobData);
				config.JobList = jobList;
			}

			return config;
		}
		public static bool SendAlive(string address, string id, AliveData Data, out string errorMsg)
		{
			try
			{
				OperatingSystem os = Environment.OSVersion;
				string url = address + IfsSync2Constants.WATCHER_SERVICE_PUT_ALIVE;

				// 객체로 정의하여 직렬화
				var requestData = new AliveRequest
				{
					UserId = id,
					Os = os.Platform.ToString().ToUpper()[..3],
					SenderAlive = Data.SenderAlive,
					ListenAlive = Data.ListenAlive,
					IniStatus = Data.IniStatus,
					MonRemain = Data.MonRemain,
					FailRemain = Data.FailRemain,
					PcName = Environment.MachineName
				};

				string jsonData = JsonConvert.SerializeObject(requestData);
				var content = new StringContent(jsonData, Encoding.UTF8, IfsSync2Constants.CURL_CONTENT_TYPE);

				var response = httpClient.PostAsync(url, content).GetAwaiter().GetResult();
				response.EnsureSuccessStatusCode();

				string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseText);

				if (apiResponse.ReturnCode != 0)
				{
					errorMsg = apiResponse.ErrorMessage;
					return false;
				}
				else
				{
					errorMsg = string.Empty;
					return true;
				}
			}
			catch (Exception e)
			{
				errorMsg = e.Message;
				return false;
			}
		}
		private static void GetStringToTime(string strTime, out int hours, out int mins)
		{
			string[] result = strTime.Split(':');

			hours = Convert.ToInt32(result[0]);
			mins = Convert.ToInt32(result[1]);
		}

		public static void CheckUpdate(string URL, string ID, string Version)
		{
			OperatingSystem os = Environment.OSVersion;
			string url = URL + IfsSync2Constants.WATCHER_SERVICE_VERSION_CHECK;

			// 객체로 정의하여 직렬화
			var requestData = new VersionCheckRequest
			{
				UserId = ID,
				PcName = Environment.MachineName,
				Os = os.Platform.ToString().ToLower()[..3],
				Company = IfsSync2Constants.COMPANY_NAME,
				Type = "SEC",
				Version = Version
			};

			string jsonData = JsonConvert.SerializeObject(requestData);
			var content = new StringContent(jsonData, Encoding.UTF8, IfsSync2Constants.CURL_CONTENT_TYPE);

			var response = httpClient.PostAsync(url, content).GetAwaiter().GetResult();
			response.EnsureSuccessStatusCode();

			string responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseText);

			if (apiResponse.ReturnCode != 0)
			{
				throw new InvalidOperationException(apiResponse.ErrorMessage);
			}
		}
	}
}
