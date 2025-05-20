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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IfsSync2Common;

namespace IfsSync2Sender
{
	/// <summary>
	/// 볼륨 스냅샷 서비스(VSS) 관리 클래스
	/// </summary>
	public class ShadowCopyManager : IDisposable
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(ShadowCopyManager));
		private List<ShadowCopy> _shadowCopies;
		private bool _disposed = false;

		/// <summary>
		/// 생성자
		/// </summary>
		public ShadowCopyManager()
		{
			_shadowCopies = [];
		}

		/// <summary>
		/// 지정된 경로 목록에 대한 스냅샷 생성
		/// </summary>
		/// <param name="paths">경로 목록</param>
		/// <returns>생성된 스냅샷 목록</returns>
		public List<ShadowCopy> CreateShadowCopies(IEnumerable<string> paths)
		{
			if (paths == null)
				return [];

			// 볼륨 목록 찾기
			List<string> volumeList = GetVolumeList(paths);

			// NTFS 및 로컬 디스크에 대한 스냅샷 생성
			_shadowCopies = CreateShadowCopiesForVolumes(volumeList);

			if (_shadowCopies.Count > 0)
			{
				log.Debug("VSS Activation");
			}

			return _shadowCopies;
		}

		/// <summary>
		/// 경로 목록에서 볼륨 목록 추출
		/// </summary>
		/// <param name="paths">경로 목록</param>
		/// <returns>볼륨 목록</returns>
		private List<string> GetVolumeList(IEnumerable<string> paths)
		{
			List<string> volumeList = [];
			foreach (var directory in paths)
			{
				string root = Path.GetPathRoot(directory);
				if (IfsSync2Utilities.CheckUNCFolder(root)) continue;
				if (!volumeList.Contains(root)) volumeList.Add(root);
			}
			return volumeList;
		}

		/// <summary>
		/// 볼륨 목록에 대한 스냅샷 생성
		/// </summary>
		/// <param name="volumeList">볼륨 목록</param>
		/// <returns>생성된 스냅샷 목록</returns>
		private List<ShadowCopy> CreateShadowCopiesForVolumes(List<string> volumeList)
		{
			List<ShadowCopy> shadowCopyList = [];
			foreach (string item in volumeList)
			{
				try
				{
					var drive = new DriveInfo(item);
					if (drive.DriveFormat == "NTFS" && drive.DriveType == DriveType.Fixed)
					{
						var shadow = new ShadowCopy(item);
						shadowCopyList.Add(shadow);
					}

					log.Debug($"Shadow Copy Directory : {item}");
				}
				catch (Exception e) { log.Error(e); }
			}
			return shadowCopyList;
		}

		/// <summary>
		/// 작업에 스냅샷 경로 설정
		/// </summary>
		/// <param name="task">작업 데이터</param>
		/// <param name="shadowCopies">스냅샷 목록</param>
		public void ApplySnapshotPathToTask(TaskData task, List<ShadowCopy> shadowCopies)
		{
			if (task == null || shadowCopies == null || shadowCopies.Count == 0)
				return;

			// 업로드 작업에만 스냅샷 경로 적용
			if (task.TaskType == TaskData.TaskTypeList.Upload)
			{
				foreach (ShadowCopy shadow in shadowCopies)
				{
					if (task.FilePath.StartsWith(shadow.VolumeName))
					{
						task.SnapshotPath = shadow.GetSnapshotPath(task.FilePath);
						break;
					}
				}
			}
		}

		/// <summary>
		/// 스냅샷 해제
		/// </summary>
		/// <param name="shadowCopies">해제할 스냅샷 목록</param>
		public void ReleaseShadowCopies(List<ShadowCopy> shadowCopies)
		{
			if (shadowCopies != null && shadowCopies.Count > 0)
			{
				foreach (var shadow in shadowCopies) shadow.Dispose();
				shadowCopies.Clear();
				log.Debug("VSS Deactivation and deletion");
			}
		}

		/// <summary>
		/// VSS가 필요한지 확인
		/// </summary>
		/// <param name="taskList">작업 목록</param>
		/// <param name="vssFileExtensions">VSS 대상 파일 확장자 목록</param>
		/// <returns>VSS 필요 여부</returns>
		public bool CheckNeedVSS(IEnumerable<TaskData> taskList, IEnumerable<string> vssFileExtensions)
		{
			if (taskList == null || !taskList.Any()) return false;
			if (vssFileExtensions == null || !vssFileExtensions.Any()) return false;

			return taskList.Any(data => vssFileExtensions.Any(ext => data.FilePath.EndsWith(ext)));
		}

		/// <summary>
		/// 리소스 해제
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 리소스 해제
		/// </summary>
		/// <param name="disposing">관리되는 리소스 해제 여부</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					ReleaseShadowCopies(_shadowCopies);
				}
				_disposed = true;
			}
		}
	}
}