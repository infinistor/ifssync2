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
using Alphaleonis.Win32.Vss;
using log4net;
using System;
using System.Diagnostics;
using System.IO;

namespace IfsSync2Sender
{
	public class ShadowCopy : IDisposable
	{
		static readonly ILog _log = LogManager.GetLogger(typeof(ShadowCopy));

		readonly bool ComponentMode = false;
		IVssBackupComponents _backup;
		Snapshot _snap;

		public string VolumeName { get; private set; }

		public ShadowCopy(string volumeName)
		{
			if (string.IsNullOrEmpty(volumeName))
				throw new ArgumentNullException(nameof(volumeName), "볼륨 이름이 필요합니다");

			VolumeName = volumeName;
			string volumeRoot = Path.GetPathRoot(volumeName);

			try
			{
				InitializeBackup();
				PrepareVolume(volumeRoot);
				CreateSnapshot();
			}
			catch (Exception e)
			{
				_log.Error($"볼륨 {volumeName}의 스냅샷 생성 실패", e);
				Dispose();
				throw;
			}
		}

		private void InitializeBackup()
		{
			IVssFactory vss = VssFactoryProvider.Default.GetVssFactory();
			_backup = vss.CreateVssBackupComponents();
			_backup.InitializeForBackup(null);
			_backup.GatherWriterMetadata();

			if (ComponentMode)
				ExamineComponents();
			else
				_backup.FreeWriterMetadata();
		}

		private void PrepareVolume(string volumeRoot)
		{
			_snap = new Snapshot(_backup);
			_snap.AddVolume(volumeRoot);
		}

		private void CreateSnapshot()
		{
			_backup.SetBackupState(ComponentMode, true, VssBackupType.Full, false);
			_backup.PrepareForBackup();
			_snap.Copy();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			Complete(true);

			if (_snap != null)
			{
				_snap.Dispose();
				_snap = null;
			}

			if (_backup != null)
			{
				_backup.Dispose();
				_backup = null;
			}
		}

		void ExamineComponents()
		{
			var writer_mds = _backup.WriterMetadata;

			foreach (var metadata in writer_mds)
			{
				Trace.TraceInformation("Examining metadata for " + metadata.WriterName);

				foreach (IVssWMComponent cmp in metadata.Components)
				{
					Trace.TraceInformation("  Component: " + cmp.ComponentName);
					Trace.TraceInformation("  Component info: " + cmp.Caption);

					foreach (VssWMFileDescriptor file in cmp.Files)
					{
						Trace.TraceInformation("    Path: " + file.Path);
						Trace.TraceInformation("       Spec: " + file.FileSpecification);
					}
				}
			}
		}

		void Complete(bool succeeded)
		{
			if (_backup == null) return;

			if (ComponentMode)
			{
				try
				{
					var writers = _backup.WriterMetadata;
					foreach (var metadata in writers)
					{
						foreach (var component in metadata.Components)
						{
							_backup.SetBackupSucceeded(
								  metadata.InstanceId, metadata.WriterId,
								  component.Type, component.LogicalPath,
								  component.ComponentName, succeeded);
						}
					}
					_backup.FreeWriterMetadata();
				}
				catch (Exception e)
				{
					_log.Error("WriterMetadata 처리 중 오류", e);
					// 정리 작업 시도
					try { _backup.FreeWriterMetadata(); } catch { /* 무시 */ }
				}
			}

			try
			{
				_backup.BackupComplete();
			}
			catch (Exception e)
			{
				_log.Error("BackupComplete", e);
			}
		}

		public string GetSnapshotPath(string localPath)
		{
			if (_snap == null)
				throw new InvalidOperationException("스냅샷이 생성되지 않았거나 이미 해제되었습니다");
			if (string.IsNullOrEmpty(localPath))
				throw new ArgumentNullException(nameof(localPath), "경로가 필요합니다");

			Trace.TraceInformation("New volume: " + _snap.Root);

			if (Path.IsPathRooted(localPath))
			{
				string root = Path.GetPathRoot(localPath);
				localPath = localPath.Replace(root, String.Empty);
			}
			string slash = Path.DirectorySeparatorChar.ToString();
			if (!_snap.Root.EndsWith(slash) && !localPath.StartsWith(slash))
				localPath = localPath.Insert(0, slash);
			localPath = localPath.Insert(0, _snap.Root);

			Trace.TraceInformation("Converted path: " + localPath);

			return localPath;
		}
	}

	public class Snapshot(IVssBackupComponents backup) : IDisposable
	{
		readonly ILog _log = LogManager.GetLogger(typeof(Snapshot));
		readonly IVssBackupComponents _backup = backup;
		readonly Guid _set_id = backup.StartSnapshotSet();

		VssSnapshotProperties _props;
		Guid _snap_id;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			Delete();
		}

		public void AddVolume(string volumeName)
		{
			if (_backup.IsVolumeSupported(volumeName))
				_snap_id = _backup.AddToSnapshotSet(volumeName);
			else
				throw new VssVolumeNotSupportedException(volumeName);
		}

		public void Copy()
		{
			_backup.DoSnapshotSet();
		}

		public void Delete()
		{
			if (_backup == null) return;

			try
			{
				_backup.DeleteSnapshotSet(_set_id, true);
			}
			catch (Exception e)
			{
				_log.Error($"스냅샷 삭제 실패 (ID: {_set_id})", e);
			}
		}

		public string Root
		{
			get
			{
				if (_backup == null)
					throw new ObjectDisposedException(nameof(Snapshot), "스냅샷이 이미 해제되었습니다");
					
				try
				{
					_props ??= _backup.GetSnapshotProperties(_snap_id);
					return _props.SnapshotDeviceObject;
				}
				catch (Exception ex)
				{
					_log.Error($"스냅샷 속성 접근 실패 (ID: {_snap_id})", ex);
					throw new InvalidOperationException("스냅샷 루트 경로를 가져올 수 없습니다", ex);
				}
			}
		}
	}
}
