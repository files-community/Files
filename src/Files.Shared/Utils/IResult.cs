using System;

namespace Files.Shared.Utils
{
	/// <summary>
	/// Represents a result of an action.
	/// </summary>
	public interface IResult
	{
		/// <summary>
		/// Gets the value that determines whether the action completed successfully or not.
		/// </summary>
		bool Successful { get; }

		/// <summary>
		/// Gets the exception associated with the action, if any.
		/// </summary>
		Exception? Exception { get; }
	}

	/// <summary>
	/// Represents a result of an action with a return value.
	/// </summary>
	/// <typeparam name="T">The type of value associated with the result.</typeparam>
	public interface IResult<out T> : IResult
	{
		/// <summary>
		/// Gets the value of the result.
		/// </summary>
		T? Value { get; }
	}

	/// <inheritdoc cref="IResult"/>
	public interface IResultWithMessage : IResult
	{
		/// <summary>
		/// Gets the message describing result of the action.
		/// <remarks>
		/// The message should not be used for displaying in the view, but rather for logs and debug dumps.</remarks>
		/// </summary>
		string? Message { get; }
	}

	/// <inheritdoc cref="IResult{T}"/>
	public interface IResultWithMessage<out T> : IResultWithMessage, IResult<T>
	{
	}
}
