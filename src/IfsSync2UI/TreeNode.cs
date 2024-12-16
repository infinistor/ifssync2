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
using System.ComponentModel;
using System.Windows.Media;

namespace IfsSync2UI
{
	public class TreeNode : INotifyPropertyChanged
	{

		private bool? _isChecked = false;
		public TreeNode Parent;

		public List<TreeNode> Children { get; set; }
		public string Name { get; set; }
		public string FullPath { get; set; }
		public ImageSource FileIcon { get; set; }

		public TreeNode()
		{
			Children = new List<TreeNode>();
		}

		public void Clear()
		{
			foreach (TreeNode child in this.Children)
			{
				child.Clear();
			}
			Children.Clear();
		}

		public void Initialize()
		{
			foreach (TreeNode child in this.Children)
			{
				child.Parent = this;
				if (_isChecked == true) child._isChecked = _isChecked;
				child.Initialize();
			}
		}

		public bool? IsChecked
		{
			get { return _isChecked; }
			set { SetIsChecked(value, true, true); }
		}

		public bool IsAllChildrenChecked()
		{
			if (Children.Count > 0)
			{
				int count = 0;
				foreach (var item in Children)
				{
					if (!item.IsChecked.Value) return false;
					else count++;
				}
				if (count == Children.Count) return true;
			}
			return false;
		}

		private void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
		{
			if (value == _isChecked) return;

			//if (value == true) Console.WriteLine("SetIsChecked true : " + FullPath);
			//else if (value == false) Console.WriteLine("SetIsChecked false : " + FullPath);
			//else Console.WriteLine("SetIsChecked NULL : " + FullPath);
			_isChecked = value;
			OnPropertyChanged("IsChecked");

			if (updateParent && Parent != null) Parent.VerifyCheckState();

			if (updateChildren && _isChecked.HasValue) Children.ForEach(c => c.SetIsChecked(_isChecked, true, false));
		}

		private void VerifyCheckState()
		{
			bool? state = null;
			for (int i = 0; i < Children.Count; ++i)
			{
				bool? current = Children[i].IsChecked;
				if (i == 0)
				{
					state = current;
				}
				else if (state != current)
				{
					state = null;
					break;
				}
			}
			SetIsChecked(state, false, true);
		}

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string prop)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}
	}
}