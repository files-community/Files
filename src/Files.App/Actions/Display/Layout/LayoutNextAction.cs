using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class LayoutNextAction : IAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "Next".GetLocalizedResource();
		public HotKey HotKey { get; } = (HotKey)"Ctrl+B";

		public Task ExecuteAsync()
		{
			context.LayoutType = context.LayoutType switch
			{
				LayoutTypes.Details => LayoutTypes.Tiles,
				LayoutTypes.Tiles => LayoutTypes.GridSmall,
				LayoutTypes.GridSmall => LayoutTypes.GridMedium,
				LayoutTypes.GridMedium => LayoutTypes.GridLarge,
				LayoutTypes.GridLarge => LayoutTypes.Columns,
				LayoutTypes.Columns when context.IsLayoutAdaptiveEnabled => LayoutTypes.Adaptive,
				LayoutTypes.Columns => LayoutTypes.Details,
				LayoutTypes.Adaptive => LayoutTypes.Details,
				_ => LayoutTypes.None,
			};

			return Task.CompletedTask;
		}
	}
}
