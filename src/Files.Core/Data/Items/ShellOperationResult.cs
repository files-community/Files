// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Text.Json.Serialization;

namespace Files.Core.Data.Items
{
	public enum OperationType
	{
		Create,
		Copy,
		Move,
		Rename,
		Delete
	}

	[JsonDerivedType(typeof(ShellOperationRequest), typeDiscriminator: "base")]
	[JsonDerivedType(typeof(ShellOperationDeleteRequest), typeDiscriminator: "delete")]
	[JsonDerivedType(typeof(ShellOperationCreateRequest), typeDiscriminator: "create")]
	[JsonDerivedType(typeof(ShellOperationRenameRequest), typeDiscriminator: "rename")]
	[JsonDerivedType(typeof(ShellOperationCopyMoveRequest), typeDiscriminator: "copymove")]
	public class ShellOperationRequest
	{
		public OperationType Operation { get; set; }
		public string ID { get; set; }

		public ShellOperationRequest(OperationType operation, string id)
			=> (Operation, ID) = (operation, id);
	}

	public class ShellOperationDeleteRequest : ShellOperationRequest
	{
		public string[] Sources { get; set; }
		public bool Permanently { get; set; }

		public ShellOperationDeleteRequest(OperationType operation, string id, string[] sources, bool permanently) : base(operation, id)
			=> (Sources, Permanently) = (sources, permanently);
	}

	public class ShellOperationCreateRequest : ShellOperationRequest
	{
		public string Source { get; set; }
		public string Template { get; set; }
		public byte[] Data { get; set; }
		public string CreateOption { get; set; }

		public ShellOperationCreateRequest(OperationType operation, string id, string source, string createOption, string template, byte[] data) : base(operation, id)
			=> (Source, CreateOption, Template, Data) = (source, createOption, template, data);
	}

	public class ShellOperationRenameRequest : ShellOperationRequest
	{
		public string Source { get; set; }
		public string Destination { get; set; }
		public bool Replace { get; set; }

		public ShellOperationRenameRequest(OperationType operation, string id, string source, string destination, bool replace) : base(operation, id)
			=> (Source, Destination, Replace) = (source, destination, replace);
	}

	public class ShellOperationCopyMoveRequest : ShellOperationRequest
	{
		public string[] Sources { get; set; }
		public string[] Destinations { get; set; }
		public bool Replace { get; set; }

		public ShellOperationCopyMoveRequest(OperationType operation, string id, string[] sources, string[] destinations, bool replace) : base(operation, id)
			=> (Sources, Destinations, Replace) = (sources, destinations, replace);
	}

	public class ShellOperationResult
	{
		public ShellOperationResult()
		{
			Items = new List<ShellOperationItemResult>();
		}

		/// <summary>
		/// File operation results: success and error code. Can contains multiple results for the same source file.
		/// E.g. if the shell shows a "replace" confirmation dialog, results can be both COPYENGINE_S_PENDING and COPYENGINE_S_USER_IGNORED.
		/// </summary>
		public List<ShellOperationItemResult> Items { get; set; }

		/// <summary>
		/// Final results of a file operation. Contains last status for each source file.
		/// </summary>
		public List<ShellOperationItemResult> Final =>
			Items.GroupBy(x => new { Src = x.Source, Dst = x.Destination }).Select(x => x.Last()).ToList();
	}

	public class ShellOperationItemResult
	{
		public bool Succeeded { get; set; }
		public int HResult { get; set; }
		public string Source { get; set; }
		public string Destination { get; set; }
	}
}
