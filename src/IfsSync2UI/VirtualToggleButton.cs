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
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace IfsSync2UI
{
	public static class VirtualToggleButton
	{
		public static readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.RegisterAttached("IsChecked", typeof(bool?), typeof(VirtualToggleButton),
				new FrameworkPropertyMetadata((bool?)false,
					FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
					new PropertyChangedCallback(OnIsCheckedChanged)));

		public static bool? GetIsChecked(DependencyObject d)
		{
			return (bool?)d.GetValue(IsCheckedProperty);
		}

		public static void SetIsChecked(DependencyObject d, bool? value)
		{
			d.SetValue(IsCheckedProperty, value);
		}

		private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is UIElement pseudobutton)
			{
				bool? newValue = (bool?)e.NewValue;
				if (newValue == true)
				{
					RaiseCheckedEvent(pseudobutton);
				}
				else if (newValue == false)
				{
					RaiseUncheckedEvent(pseudobutton);
				}
				else
				{
					RaiseIndeterminateEvent(pseudobutton);
				}
			}
		}

		public static readonly DependencyProperty IsThreeStateProperty =
			DependencyProperty.RegisterAttached("IsThreeState", typeof(bool), typeof(VirtualToggleButton),
				new FrameworkPropertyMetadata(false));

		public static bool GetIsThreeState(DependencyObject d)
		{
			return (bool)d.GetValue(IsThreeStateProperty);
		}

		public static void SetIsThreeState(DependencyObject d, bool value)
		{
			d.SetValue(IsThreeStateProperty, value);
		}

		public static readonly DependencyProperty IsVirtualToggleButtonProperty =
			DependencyProperty.RegisterAttached("IsVirtualToggleButton", typeof(bool), typeof(VirtualToggleButton),
				new FrameworkPropertyMetadata(false,
					new PropertyChangedCallback(OnIsVirtualToggleButtonChanged)));

		public static bool GetIsVirtualToggleButton(DependencyObject d)
		{
			return (bool)d.GetValue(IsVirtualToggleButtonProperty);
		}

		public static void SetIsVirtualToggleButton(DependencyObject d, bool value)
		{
			d.SetValue(IsVirtualToggleButtonProperty, value);
		}

		private static void OnIsVirtualToggleButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is IInputElement element)
			{
				if ((bool)e.NewValue)
				{
					element.MouseLeftButtonDown += OnMouseLeftButtonDown;
					element.KeyDown += OnKeyDown;
				}
				else
				{
					element.MouseLeftButtonDown -= OnMouseLeftButtonDown;
					element.KeyDown -= OnKeyDown;
				}
			}
		}

		internal static RoutedEventArgs RaiseCheckedEvent(UIElement target)
		{
			if (target == null) return null;

			RoutedEventArgs args = new() { RoutedEvent = ToggleButton.CheckedEvent };
			RaiseEvent(target, args);
			return args;
		}

		internal static RoutedEventArgs RaiseUncheckedEvent(UIElement target)
		{
			if (target == null) return null;

			RoutedEventArgs args = new() { RoutedEvent = ToggleButton.UncheckedEvent };
			RaiseEvent(target, args);
			return args;
		}

		internal static RoutedEventArgs RaiseIndeterminateEvent(UIElement target)
		{
			if (target == null) return null;

			RoutedEventArgs args = new()
			{
				RoutedEvent = ToggleButton.IndeterminateEvent
			};
			RaiseEvent(target, args);
			return args;
		}

		private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			UpdateIsChecked(sender as DependencyObject);
		}

		private static void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.OriginalSource == sender)
			{
				if (e.Key == Key.Space)
				{
					if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) return;

					UpdateIsChecked(sender as DependencyObject);
					e.Handled = true;

				}
				else if (e.Key == Key.Enter && (bool)(sender as DependencyObject).GetValue(KeyboardNavigation.AcceptsReturnProperty))
				{
					UpdateIsChecked(sender as DependencyObject);
					e.Handled = true;
				}
			}
		}

		private static void UpdateIsChecked(DependencyObject d)
		{
			bool? isChecked = GetIsChecked(d);
			if (isChecked == true)
			{
				SetIsChecked(d, GetIsThreeState(d) ? null : false);
			}
			else
			{
				SetIsChecked(d, isChecked.HasValue);
			}
		}

		private static void RaiseEvent(DependencyObject target, RoutedEventArgs args)
		{
			if (target is UIElement)
			{
				(target as UIElement).RaiseEvent(args);
			}
			else if (target is ContentElement)
			{
				(target as ContentElement).RaiseEvent(args);
			}
		}
	}
}