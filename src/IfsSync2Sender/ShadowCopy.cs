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
using Alphaleonis.Win32.Vss;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace IfsSync2Sender
{
    public class ShadowCopy
    {
        readonly bool ComponentMode = false;

        IVssBackupComponents _backup;

        Snapshot _snap;

        public string VolumeName { get; set; }

        public ShadowCopy()
        {
        }

        public void Setup(string volumeName)
        {
            VolumeName = volumeName;
            Discovery(volumeName);
            PreBackup();
        }

        public void Init()
        {
            if (_backup != null) Dispose();
            InitializeBackup();
        }

        public void Dispose()
        {
            try { Complete(true); } catch { }

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

        void InitializeBackup()
        {
            IVssFactory vss = VssFactoryProvider.Default.GetVssFactory();

            _backup = vss.CreateVssBackupComponents();
            _backup.InitializeForBackup(null);
            _backup.GatherWriterMetadata();
        }

        void Discovery(string fullPath)
        {
            if (ComponentMode)
                ExamineComponents(fullPath);
            else
                _backup.FreeWriterMetadata();

            _snap = new Snapshot(_backup);
            _snap.AddVolume(Path.GetPathRoot(fullPath));
        }

        void ExamineComponents(string fullPath)
        {
            IList<IVssExamineWriterMetadata> writer_mds = _backup.WriterMetadata;

            foreach (IVssExamineWriterMetadata metadata in writer_mds)
            {
                Trace.WriteLine("Examining metadata for " + metadata.WriterName);

                foreach (IVssWMComponent cmp in metadata.Components)
                {
                    Trace.WriteLine("  Component: " + cmp.ComponentName);
                    Trace.WriteLine("  Component info: " + cmp.Caption);

                    foreach (VssWMFileDescriptor file in cmp.Files)
                    {
                        Trace.WriteLine("    Path: " + file.Path);
                        Trace.WriteLine("       Spec: " + file.FileSpecification);
                    }
                }
            }
        }

        void PreBackup()
        {
            Debug.Assert(_snap != null);

            _backup.SetBackupState(ComponentMode, true, VssBackupType.Full, false);
            _backup.PrepareForBackup();
            _snap.Copy();
        }

        public string GetSnapshotPath(string localPath)
        {
            Trace.WriteLine("New volume: " + _snap.Root);

            if (Path.IsPathRooted(localPath))
            {
                string root = Path.GetPathRoot(localPath);
                localPath = localPath.Replace(root, String.Empty);
            }
            string slash = Path.DirectorySeparatorChar.ToString();
            if (!_snap.Root.EndsWith(slash) && !localPath.StartsWith(slash))
                localPath = localPath.Insert(0, slash);
            localPath = localPath.Insert(0, _snap.Root);

            Trace.WriteLine("Converted path: " + localPath);

            return localPath;
        }

        public Stream GetStream(string localPath)
        {
            return File.OpenRead(GetSnapshotPath(localPath));
        }

        void Complete(bool succeeded)
        {
            if (ComponentMode)
            {
                IList<IVssExamineWriterMetadata> writers = _backup.WriterMetadata;
                foreach (IVssExamineWriterMetadata metadata in writers)
                {
                    foreach (IVssWMComponent component in metadata.Components)
                    {
                        _backup.SetBackupSucceeded(
                              metadata.InstanceId, metadata.WriterId,
                              component.Type, component.LogicalPath,
                              component.ComponentName, succeeded);
                    }
                }
                _backup.FreeWriterMetadata();
            }

            try
            {
                _backup.BackupComplete();
            }
            catch (VssBadStateException) { }
        }

        string FileToPathSpecification(VssWMFileDescriptor file)
        {
            string path = Environment.ExpandEnvironmentVariables(file.Path);

            if (!String.IsNullOrEmpty(file.AlternateLocation))
                path = Environment.ExpandEnvironmentVariables(file.AlternateLocation);

            string spec = file.FileSpecification.Replace("*.*", "*");

            return Path.Combine(path, file.FileSpecification);
        }
    }


    public class Snapshot : IDisposable
    {
        private readonly IVssBackupComponents _backup;
        VssSnapshotProperties _props;
        Guid _set_id;
        Guid _snap_id;

        public Snapshot(IVssBackupComponents backup)
        {
            _backup = backup;
            _set_id = backup.StartSnapshotSet();
        }
        
        public void Dispose()
        {
            try { Delete(); } catch { }
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
            _backup.DeleteSnapshotSet(_set_id, true);
        }

        public string Root
        {
            get
            {
                if (_props == null)
                    _props = _backup.GetSnapshotProperties(_snap_id);
                return _props.SnapshotDeviceObject;
            }
        }
    }
}
