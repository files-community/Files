using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class LayoutPreviousAction : IAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "Previous".GetLocalizedResource();

		public Task ExecuteAsync()
		{
			context.LayoutType = context.LayoutType switch
			{
				LayoutTypes.Details => LayoutTypes.Adaptive,
				LayoutTypes.Tiles => LayoutTypes.Details,
				LayoutTypes.GridSmall => LayoutTypes.Tiles,
				LayoutTypes.GridMedium => LayoutTypes.GridSmall,
				LayoutTypes.GridLarge => LayoutTypes.GridMedium,
				LayoutTypes.Columns => LayoutTypes.GridLarge,
				LayoutTypes.Adaptive => LayoutTypes.Columns,
				_ => LayoutTypes.None,
			};

			return Task.CompletedTask;
		}
	}
}
