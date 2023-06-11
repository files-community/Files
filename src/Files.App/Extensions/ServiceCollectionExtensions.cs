// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ServicesImplementation;
using Microsoft.Extensions.DependencyInjection;

namespace Files.App.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddDialogViewModels(this IServiceCollection services)
		{
			foreach (Type type in DialogService.GetDialogViewModelTypes())
			{
				services.AddSingleton(type);
			}

			return services;
		}
	}
}