// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using WinRT;

namespace Files.App.Converters
{
	partial class StatusCenterStateToStateIconConverter : IValueConverter
	{
		[DynamicWindowsRuntimeCast(typeof(Geometry))]
		public object? Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is StatusCenterItemIconKind state)
			{
				var pathMarkup = state switch
				{
					StatusCenterItemIconKind.Copy => Application.Current.Resources["App.Theme.PathIcon.ActionCopy"] as string,
					StatusCenterItemIconKind.Move => Application.Current.Resources["App.Theme.PathIcon.ActionMove"] as string,
					StatusCenterItemIconKind.Delete => Application.Current.Resources["App.Theme.PathIcon.ActionDelete"] as string,
					StatusCenterItemIconKind.Recycle => Application.Current.Resources["App.Theme.PathIcon.ActionDelete"] as string,
					StatusCenterItemIconKind.Extract => Application.Current.Resources["App.Theme.PathIcon.ActionExtract"] as string,
					StatusCenterItemIconKind.Compress => Application.Current.Resources["App.Theme.PathIcon.ActionExtract"] as string,
					StatusCenterItemIconKind.Successful => Application.Current.Resources["App.Theme.PathIcon.ActionSuccess"] as string,
					StatusCenterItemIconKind.Error => Application.Current.Resources["App.Theme.PathIcon.ActionInfo"] as string,
					StatusCenterItemIconKind.Git => Application.Current.Resources["App.Theme.PathIcon.ActionGit"] as string,
					StatusCenterItemIconKind.InstallFont => Application.Current.Resources["App.Theme.PathIcon.ActionInstallFont"] as string,
					_ => ""
				};

				if (string.IsNullOrEmpty(pathMarkup))
					return null;

				return XamlBindingHelper.ConvertValue(typeof(Geometry), pathMarkup) as Geometry;
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
