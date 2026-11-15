// Copyright (c) Mahmoud Al-Qudsi, NeoSmart Technoogies. All rights reserved.  
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using Windows.Foundation;
using Windows.UI;

namespace Files.App.Extensions
{
	using WebViewMessageReceivedHandler = TypedEventHandler<WebView2, CoreWebView2WebMessageReceivedEventArgs>;

	internal struct WebMessage
	{
		public Guid Guid { get; set; }
	}

	internal struct MethodWebMessage
	{
		public long Id { get; set; }
		public string Method { get; set; }
		public string Args { get; set; }
	}

	[JsonSourceGenerationOptions(
		PropertyNameCaseInsensitive = true)]
	[JsonSerializable(typeof(WebMessage))]
	[JsonSerializable(typeof(MethodWebMessage))]
	[JsonSerializable(typeof(JsonElement[]))]
	[JsonSerializable(typeof(object[]))]
	[JsonSerializable(typeof(Boolean))]
	internal partial class WebMessageJsonContext : JsonSerializerContext
	{
	}

	/// <summary>
	/// Code modified from https://gist.github.com/mqudsi/ceb4ecee76eb4c32238a438664783480
	/// </summary>
	public static class WebView2Extensions
	{
		public static void Navigate(this WebView2 webview, Uri url)
		{
			webview.Source = url;
		}

		private static readonly JsonSerializerOptions _unsafeRelaxedJsonEscapingOptions = new(WebMessageJsonContext.Default.Options)
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};
		private static readonly WebMessageJsonContext _jsonContext = new(_unsafeRelaxedJsonEscapingOptions);
		public static List<WebViewMessageReceivedHandler> _handlers = new();

		public static async Task AddWebAllowedObject<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(this WebView2 webview, string name, T @object)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"window.{name} = {{ ");

			var methodsGuid = Guid.NewGuid();
			var methodInfo = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);
			var methods = new Dictionary<string, MethodInfo>(methodInfo.Length);
			foreach (var method in methodInfo)
			{
				var functionName = $"{char.ToLower(method.Name[0])}{method.Name.Substring(1)}";
				sb.AppendLine($@"{functionName}: function() {{ window.chrome.webview.postMessage(JSON.stringify({{ guid: ""{methodsGuid}"", id: this._callbackIndex++, method: ""{functionName}"", args: JSON.stringify([...arguments]) }})); const promise = new Promise((accept, reject) => this._callbacks.set(this._callbackIndex, {{ accept: accept, reject: reject }})); return promise; }},");
				methods.Add(functionName, method);
			}

			sb.AppendLine($@"_callbacks: new Map(),");
			sb.Append($@"_callbackIndex: 0,");
			sb.AppendLine("}");

			try
			{
				await webview.ExecuteScriptAsync($"{sb}").AsTask();
			}
			catch (Exception)
			{
			}

			var handler = (WebViewMessageReceivedHandler)(async (_, e) =>
			{
				var rawMessage = e.TryGetWebMessageAsString();
				var message = JsonSerializer.Deserialize(rawMessage, _jsonContext.WebMessage);
				if (message.Guid != methodsGuid)
					return;

				var methodMessage = JsonSerializer.Deserialize(rawMessage, _jsonContext.MethodWebMessage);
				var method = methods[methodMessage.Method];
				try
				{
					var args = JsonSerializer.Deserialize(methodMessage.Args, _jsonContext.JsonElementArray)!.Zip(method.GetParameters(), (val, args) => new { val, args.ParameterType }).Select(item => item.val.Deserialize(_jsonContext.GetTypeInfo(item.ParameterType)!));
					var result = method.Invoke(@object, args.ToArray());
					if (result is object)
					{
						var resultType = result.GetType();
						//dynamic? task = null;
						if (resultType.Name.StartsWith("TaskToAsyncOperationAdapter")
							|| resultType.IsInstanceOfType(typeof(IAsyncInfo)))
						{
							if (resultType.GenericTypeArguments.Length > 0)
							{
								/*var asTask = typeof(WindowsRuntimeSystemExtensions)
									.GetMethods(BindingFlags.Public | BindingFlags.Static)
									.Where(method => method.GetParameters().Length == 1
										&& method.Name == "AsTask"
										&& method.ToString()!.Contains("Windows.Foundation.IAsyncOperation`1[TResult]"))
									.FirstOrDefault();

								asTask = asTask!.MakeGenericMethod(resultType.GenericTypeArguments[0]);
								task = (Task?)asTask.Invoke(null, new[] { result });*/
							}
							else
							{
								//task = WindowsRuntimeSystemExtensions.AsTask((dynamic)result);
							}
							App.Logger.LogWarning("Terminal: Task/Awaiter return type is currently not supported on AOT");
						}
						else
						{
							/*var awaiter = resultType?.GetMethod(nameof(Task.GetAwaiter));
							if (awaiter is object)
								task = result;*/
						}
						/*if (task is object)
							result = await task;*/
					}
					var json = JsonSerializer.Serialize(result, _jsonContext.Object);
					await webview.ExecuteScriptAsync($@"{name}._callbacks.get({methodMessage.Id}).accept(JSON.parse({json})); {name}._callbacks.delete({methodMessage.Id});");
				}
				catch (Exception ex)
				{
					var json = JsonSerializer.Serialize(ex.ToString(), _jsonContext.String);
					await webview.ExecuteScriptAsync($@"{name}._callbacks.get({methodMessage.Id}).reject(JSON.parse({json})); {name}._callbacks.delete({methodMessage.Id});");
				}
			});

			_handlers.Add(handler);
			webview.WebMessageReceived += handler;
		}

		public static async Task<string?> InvokeScriptAsync(this WebView2 webview, string function, params object[] args)
		{
			var array = JsonSerializer.Serialize(args, _jsonContext.ObjectArray);
			string? result = null;
			await webview.DispatcherQueue.EnqueueAsync(async () =>
			{
				var script = $"{function}(...{array});";
				try
				{
					result = await webview.ExecuteScriptAsync(script).AsTask();
					result = JsonSerializer.Deserialize(result, _jsonContext.String);
				}
				catch (Exception)
				{
				}
			}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);

			return result;
		}
	}
}