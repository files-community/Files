﻿// Copyright (c) Mahmoud Al-Qudsi, NeoSmart Technoogies. All rights reserved.  
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using Windows.Foundation;

namespace Files.App.UserControls
{
	/// <summary>
	/// Code modified from https://gist.github.com/mqudsi/ceb4ecee76eb4c32238a438664783480
	/// </summary>
	public static class WebView2Extensions
	{
		public static void Navigate(this WebView2 webview, Uri url)
		{
			webview.Source = url;
		}

		private enum PropertyAction
		{
			Read = 0,
			Write = 1,
		}

		private struct WebMessage
		{
			public Guid Guid { get; set; }
		}

		private struct MethodWebMessage
		{
			public string Id { get; set; }
			public string Method { get; set; }
			public string Args { get; set; }
		}

		private struct PropertyWebMessage
		{
			public string Id { get; set; }
			public string Property { get; set; }
			public PropertyAction Action { get; set; }
			public string Value { get; set; }
		}

		public static List<TypedEventHandler<WebView2, CoreWebView2WebMessageReceivedEventArgs>> _handlers = new List<TypedEventHandler<WebView2, CoreWebView2WebMessageReceivedEventArgs>>();
		public static async Task AddWebAllowedObject<T>(this WebView2 webview, string name, T @object)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"window.{name} = {{ ");

			// Test webview for our sanity
			await webview.ExecuteScriptAsync($@"console.log(""Sanity check from iMessage"");");

			var methodsGuid = Guid.NewGuid();
			var methodInfo = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance);
			var methods = new Dictionary<string, MethodInfo>(methodInfo.Length);
			foreach (var method in methodInfo)
			{
				var functionName = $"{char.ToLower(method.Name[0])}{method.Name.Substring(1)}";
				sb.AppendLine($@"{functionName}: function() {{ window.chrome.webview.postMessage(JSON.stringify({{ guid: ""{methodsGuid}"", id: this._callbackIndex++, method: ""{functionName}"", args: JSON.stringify([...arguments]) }})); const promise = new Promise((accept, reject) => this._callbacks.set(this._callbackIndex, {{ accept: accept, reject: reject }})); return promise; }},");
				methods.Add(functionName, method);
			}

			var propertiesGuid = Guid.NewGuid();
			var propertyInfo = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			var properties = new Dictionary<string, PropertyInfo>(propertyInfo.Length);
			//foreach (var property in propertyInfo)
			//{
			//    var propertyName = $"{char.ToLower(property.Name[0])}{property.Name.Substring(1)}";
			//    if (property.CanRead)
			//    {
			//        sb.AppendLine($@"get {propertyName}() {{ window.chrome.webview.postMessage(JSON.stringify({{ guid: ""{propertiesGuid}"", id: this._callbackIndex++, property: ""{propertyName}"", action: ""{(int) PropertyAction.Read}"" }})); const promise = new Promise((accept, reject) => this._callbacks.set(this._callbackIndex, {{ accept: accept, reject: reject }})); return promise; }},");
			//    }
			//    if (property.CanWrite)
			//    {
			//        sb.AppendLine($@"set {propertyName}(value) {{ window.chrome.webview.postMessage(JSON.stringify({{ guid: ""{propertiesGuid}"", id: this._callbackIndex++, property: ""{propertyName}"", action: ""{(int)PropertyAction.Write}"", value: JSON.stringify(value) }}));  const promise = new Promise((accept, reject) => this._callbacks.set(this._callbackIndex, {{ accept: accept, reject: reject }})); return promise; }},");

			//    }
			//    properties[propertyName] = property;
			//}

			// Add a map<int, (promiseAccept, promiseReject)> to the object used to resolve results
			sb.AppendLine($@"_callbacks: new Map(),");
			// And a shared counter to index into that map
			sb.Append($@"_callbackIndex: 0,");

			sb.AppendLine("}");

			try
			{
				//await webview.ExecuteScriptAsync($"try {{ {sb} }} catch (ex) {{ console.error(ex); }}").AsTask();
				await webview.ExecuteScriptAsync($"{sb}").AsTask();
			}
			catch (Exception ex)
			{
				// So we can see it in the JS debugger
			}

			var handler = (TypedEventHandler<WebView2, CoreWebView2WebMessageReceivedEventArgs>)(async (_, e) =>
			{
				var message = JsonConvert.DeserializeObject<WebMessage>(e.TryGetWebMessageAsString());
				if (message.Guid == methodsGuid)
				{
					var methodMessage = JsonConvert.DeserializeObject<MethodWebMessage>(e.TryGetWebMessageAsString());
					var method = methods[methodMessage.Method];
					try
					{
						var result = method.Invoke(@object, JsonConvert.DeserializeObject<object[]>(methodMessage.Args));
						if (result is object)
						{
							var resultType = result.GetType();
							dynamic task = null;
							if (resultType.Name.StartsWith("TaskToAsyncOperationAdapter")
								|| resultType.IsInstanceOfType(typeof(IAsyncInfo)))
							{
								// IAsyncOperation that needs to be converted to a task first
								if (resultType.GenericTypeArguments.Length > 0)
								{
									var asTask = typeof(WindowsRuntimeSystemExtensions)
										.GetMethods(BindingFlags.Public | BindingFlags.Static)
										.Where(method => method.GetParameters().Length == 1
											&& method.Name == "AsTask"
											&& method.ToString().Contains("Windows.Foundation.IAsyncOperation`1[TResult]"))
										.FirstOrDefault();

									//var asTask = typeof(WindowsRuntimeSystemExtensions)
									//    .GetMethod(nameof(WindowsRuntimeSystemExtensions.AsTask),
									//            new[] { typeof(IAsyncOperation<>).MakeGenericType(resultType.GenericTypeArguments[0]) }
									//    );

									asTask = asTask.MakeGenericMethod(resultType.GenericTypeArguments[0]);
									task = (Task)asTask.Invoke(null, new[] { result });
								}
								else
								{
									task = WindowsRuntimeSystemExtensions.AsTask((dynamic)result);
								}
							}
							else
							{
								var awaiter = resultType.GetMethod(nameof(Task.GetAwaiter));
								if (awaiter is object)
								{
									task = (dynamic)result;
								}
							}
							if (task is object)
							{
								result = await task;
							}
						}
						var json = JsonConvert.SerializeObject(result);
						await webview.ExecuteScriptAsync($@"{name}._callbacks.get({methodMessage.Id}).accept(JSON.parse({json})); {name}._callbacks.delete({methodMessage.Id});");
					}
					catch (Exception ex)
					{
						var json = JsonConvert.SerializeObject(ex, new JsonSerializerSettings() { Error = (_, e) => e.ErrorContext.Handled = true });
						await webview.ExecuteScriptAsync($@"{name}._callbacks.get({methodMessage.Id}).reject(JSON.parse({json})); {name}._callbacks.delete({methodMessage.Id});");
						//throw;
					}
				}
				else if (message.Guid == propertiesGuid)
				{
					var propertyMessage = JsonConvert.DeserializeObject<PropertyWebMessage>(e.TryGetWebMessageAsString());
					var property = properties[propertyMessage.Property];
					try
					{
						object result;
						if (propertyMessage.Action == PropertyAction.Read)
						{
							result = property.GetValue(@object);
						}
						else
						{
							var value = JsonConvert.DeserializeObject(propertyMessage.Value, property.PropertyType);
							property.SetValue(@object, value);
							result = new object();
						}

						var json = JsonConvert.SerializeObject(result);
						await webview.ExecuteScriptAsync($@"{name}._callbacks.get({propertyMessage.Id}).accept(JSON.parse({json})); {name}._callbacks.delete({propertyMessage.Id});");
					}
					catch (Exception ex)
					{
						var json = JsonConvert.SerializeObject(ex, new JsonSerializerSettings() { Error = (_, e) => e.ErrorContext.Handled = true });
						//await webview.ExecuteScriptAsync($@"{name}._callbacks.get({propertyMessage.Id}).reject(JSON.parse({json})); {name}._callbacks.delete({propertyMessage.Id});");
						//throw;
					}
				}
			});

			_handlers.Add(handler);
			webview.WebMessageReceived += handler;
		}

		public static async Task<string> InvokeScriptAsync(this WebView2 webview, string function, params object[] args)
		{
			var array = JsonConvert.SerializeObject(args);
			string result = null;
			// Tested and checked: this dispatch is required, even though the web view is in a different process
			await webview.DispatcherQueue.EnqueueAsync(async () =>
			{
				var script = $"{function}(...{array});";
				try
				{
					result = await webview.ExecuteScriptAsync(script).AsTask();
					result = JsonConvert.DeserializeObject<string>(result);
				}
				catch (Exception ex)
				{
				}
			}, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);

			return result;
		}
	}
}