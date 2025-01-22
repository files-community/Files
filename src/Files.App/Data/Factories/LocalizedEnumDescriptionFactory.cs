// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Factories
{
	/// <summary>
	/// Generates localization description for an enum value.
	/// </summary>
	internal static class LocalizedEnumDescriptionFactory
	{
		private static Dictionary<DetailsViewSizeKind, string> DetailsViewSizeKinds { get; } = [];
		private static Dictionary<ListViewSizeKind, string> ListViewSizeKinds { get; } = [];
		private static Dictionary<CardsViewSizeKind, string> CardsViewSizeKinds { get; } = [];
		private static Dictionary<GridViewSizeKind, string> GridViewSizeKinds { get; } = [];
		private static Dictionary<ColumnsViewSizeKind, string> ColumnsViewSizeKinds { get; } = [];

		public static string Get(DetailsViewSizeKind value)
		{
			if (DetailsViewSizeKinds.Count == 0)
			{
				DetailsViewSizeKinds.Add(DetailsViewSizeKind.Compact, Strings.Compact.GetLocalizedResource());
				DetailsViewSizeKinds.Add(DetailsViewSizeKind.Small, Strings.Small.GetLocalizedResource());
				DetailsViewSizeKinds.Add(DetailsViewSizeKind.Medium, Strings.Medium.GetLocalizedResource());
				DetailsViewSizeKinds.Add(DetailsViewSizeKind.Large, Strings.Large.GetLocalizedResource());
				DetailsViewSizeKinds.Add(DetailsViewSizeKind.ExtraLarge, Strings.ExtraLarge.GetLocalizedResource());
			}

			var stringValue = DetailsViewSizeKinds.GetValueOrDefault(value)!;
			return stringValue;
		}

		public static string Get(ListViewSizeKind value)
		{
			if (ListViewSizeKinds.Count == 0)
			{
				ListViewSizeKinds.Add(ListViewSizeKind.Compact, Strings.Compact.GetLocalizedResource());
				ListViewSizeKinds.Add(ListViewSizeKind.Small, Strings.Small.GetLocalizedResource());
				ListViewSizeKinds.Add(ListViewSizeKind.Medium, Strings.Medium.GetLocalizedResource());
				ListViewSizeKinds.Add(ListViewSizeKind.Large, Strings.Large.GetLocalizedResource());
				ListViewSizeKinds.Add(ListViewSizeKind.ExtraLarge, Strings.ExtraLarge.GetLocalizedResource());
			}

			var stringValue = ListViewSizeKinds.GetValueOrDefault(value)!;
			return stringValue;
		}

		public static string Get(CardsViewSizeKind value)
		{
			if (CardsViewSizeKinds.Count == 0)
			{
				CardsViewSizeKinds.Add(CardsViewSizeKind.Small, Strings.Small.GetLocalizedResource());
				CardsViewSizeKinds.Add(CardsViewSizeKind.Medium, Strings.Medium.GetLocalizedResource());
				CardsViewSizeKinds.Add(CardsViewSizeKind.Large, Strings.Large.GetLocalizedResource());
				CardsViewSizeKinds.Add(CardsViewSizeKind.ExtraLarge, Strings.ExtraLarge.GetLocalizedResource());
			}

			var stringValue = CardsViewSizeKinds.GetValueOrDefault(value)!;
			return stringValue;
		}

		public static string Get(GridViewSizeKind value)
		{
			if (GridViewSizeKinds.Count == 0)
			{
				GridViewSizeKinds.Add(GridViewSizeKind.Small, Strings.Small.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.Medium, Strings.Medium.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.Three, Strings.MediumP.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.Four, Strings.MediumPP.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.Five, Strings.MediumPPP.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.Six, Strings.MediumPPPP.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.Seven, Strings.MediumPPPPP.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.Large, Strings.Large.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.Nine, Strings.LargeP.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.Ten, Strings.LargePP.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.Eleven, Strings.LargePPP.GetLocalizedResource());
				GridViewSizeKinds.Add(GridViewSizeKind.ExtraLarge, Strings.ExtraLarge.GetLocalizedResource());
			}

			var stringValue = GridViewSizeKinds.GetValueOrDefault(value)!;
			return stringValue;
		}

		public static string Get(ColumnsViewSizeKind value)
		{
			if (ColumnsViewSizeKinds.Count == 0)
			{
				ColumnsViewSizeKinds.Add(ColumnsViewSizeKind.Compact, Strings.Compact.GetLocalizedResource());
				ColumnsViewSizeKinds.Add(ColumnsViewSizeKind.Small, Strings.Small.GetLocalizedResource());
				ColumnsViewSizeKinds.Add(ColumnsViewSizeKind.Medium, Strings.Medium.GetLocalizedResource());
				ColumnsViewSizeKinds.Add(ColumnsViewSizeKind.Large, Strings.Large.GetLocalizedResource());
				ColumnsViewSizeKinds.Add(ColumnsViewSizeKind.ExtraLarge, Strings.ExtraLarge.GetLocalizedResource());
			}

			var stringValue = ColumnsViewSizeKinds.GetValueOrDefault(value)!;
			return stringValue;
		}
	}
}
