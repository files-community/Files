using System;
using System.IO;

namespace ICSharpCode.SharpZipLib.Core
{
	/// <summary>
	/// PathFilter filters directories and files using a form of <see cref="System.Text.RegularExpressions.Regex">regular expressions</see>
	/// by full path name.
	/// See <see cref="NameFilter">NameFilter</see> for more detail on filtering.
	/// </summary>
	public class PathFilter : IScanFilter
	{
		#region Constructors

		/// <summary>
		/// Initialise a new instance of <see cref="PathFilter"></see>.
		/// </summary>
		/// <param name="filter">The <see cref="NameFilter">filter</see> expression to apply.</param>
		public PathFilter(string filter)
		{
			nameFilter_ = new NameFilter(filter);
		}

		#endregion Constructors

		#region IScanFilter Members

		/// <summary>
		/// Test a name to see if it matches the filter.
		/// </summary>
		/// <param name="name">The name to test.</param>
		/// <returns>True if the name matches, false otherwise.</returns>
		/// <remarks><see cref="Path.GetFullPath(string)"/> is used to get the full path before matching.</remarks>
		public virtual bool IsMatch(string name)
		{
			bool result = false;

			if (name != null)
			{
				string cooked = (name.Length > 0) ? Path.GetFullPath(name) : "";
				result = nameFilter_.IsMatch(cooked);
			}
			return result;
		}

		private readonly

		#endregion IScanFilter Members

		#region Instance Fields

		NameFilter nameFilter_;

		#endregion Instance Fields
	}

	/// <summary>
	/// ExtendedPathFilter filters based on name, file size, and the last write time of the file.
	/// </summary>
	/// <remarks>Provides an example of how to customise filtering.</remarks>
	public class ExtendedPathFilter : PathFilter
	{
		#region Constructors

		/// <summary>
		/// Initialise a new instance of ExtendedPathFilter.
		/// </summary>
		/// <param name="filter">The filter to apply.</param>
		/// <param name="minSize">The minimum file size to include.</param>
		/// <param name="maxSize">The maximum file size to include.</param>
		public ExtendedPathFilter(string filter,
			long minSize, long maxSize)
			: base(filter)
		{
			MinSize = minSize;
			MaxSize = maxSize;
		}

		/// <summary>
		/// Initialise a new instance of ExtendedPathFilter.
		/// </summary>
		/// <param name="filter">The filter to apply.</param>
		/// <param name="minDate">The minimum <see cref="DateTime"/> to include.</param>
		/// <param name="maxDate">The maximum <see cref="DateTime"/> to include.</param>
		public ExtendedPathFilter(string filter,
			DateTime minDate, DateTime maxDate)
			: base(filter)
		{
			MinDate = minDate;
			MaxDate = maxDate;
		}

		/// <summary>
		/// Initialise a new instance of ExtendedPathFilter.
		/// </summary>
		/// <param name="filter">The filter to apply.</param>
		/// <param name="minSize">The minimum file size to include.</param>
		/// <param name="maxSize">The maximum file size to include.</param>
		/// <param name="minDate">The minimum <see cref="DateTime"/> to include.</param>
		/// <param name="maxDate">The maximum <see cref="DateTime"/> to include.</param>
		public ExtendedPathFilter(string filter,
			long minSize, long maxSize,
			DateTime minDate, DateTime maxDate)
			: base(filter)
		{
			MinSize = minSize;
			MaxSize = maxSize;
			MinDate = minDate;
			MaxDate = maxDate;
		}

		#endregion Constructors

		#region IScanFilter Members

		/// <summary>
		/// Test a filename to see if it matches the filter.
		/// </summary>
		/// <param name="name">The filename to test.</param>
		/// <returns>True if the filter matches, false otherwise.</returns>
		/// <exception cref="System.IO.FileNotFoundException">The <see paramref="fileName"/> doesnt exist</exception>
		public override bool IsMatch(string name)
		{
			bool result = base.IsMatch(name);

			if (result)
			{
				var fileInfo = new FileInfo(name);
				result =
					(MinSize <= fileInfo.Length) &&
					(MaxSize >= fileInfo.Length) &&
					(MinDate <= fileInfo.LastWriteTime) &&
					(MaxDate >= fileInfo.LastWriteTime)
					;
			}
			return result;
		}

		#endregion IScanFilter Members

		#region Properties

		/// <summary>
		/// Get/set the minimum size/length for a file that will match this filter.
		/// </summary>
		/// <remarks>The default value is zero.</remarks>
		/// <exception cref="ArgumentOutOfRangeException">value is less than zero; greater than <see cref="MaxSize"/></exception>
		public long MinSize
		{
			get { return minSize_; }
			set
			{
				if ((value < 0) || (maxSize_ < value))
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				minSize_ = value;
			}
		}

		/// <summary>
		/// Get/set the maximum size/length for a file that will match this filter.
		/// </summary>
		/// <remarks>The default value is <see cref="System.Int64.MaxValue"/></remarks>
		/// <exception cref="ArgumentOutOfRangeException">value is less than zero or less than <see cref="MinSize"/></exception>
		public long MaxSize
		{
			get { return maxSize_; }
			set
			{
				if ((value < 0) || (minSize_ > value))
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				maxSize_ = value;
			}
		}

		/// <summary>
		/// Get/set the minimum <see cref="DateTime"/> value that will match for this filter.
		/// </summary>
		/// <remarks>Files with a LastWrite time less than this value are excluded by the filter.</remarks>
		public DateTime MinDate
		{
			get
			{
				return minDate_;
			}

			set
			{
				if (value > maxDate_)
				{
					throw new ArgumentOutOfRangeException(nameof(value), "Exceeds MaxDate");
				}

				minDate_ = value;
			}
		}

		/// <summary>
		/// Get/set the maximum <see cref="DateTime"/> value that will match for this filter.
		/// </summary>
		/// <remarks>Files with a LastWrite time greater than this value are excluded by the filter.</remarks>
		public DateTime MaxDate
		{
			get
			{
				return maxDate_;
			}

			set
			{
				if (minDate_ > value)
				{
					throw new ArgumentOutOfRangeException(nameof(value), "Exceeds MinDate");
				}

				maxDate_ = value;
			}
		}

		#endregion Properties

		#region Instance Fields

		private long minSize_;
		private long maxSize_ = long.MaxValue;
		private DateTime minDate_ = DateTime.MinValue;
		private DateTime maxDate_ = DateTime.MaxValue;

		#endregion Instance Fields
	}

	/// <summary>
	/// NameAndSizeFilter filters based on name and file size.
	/// </summary>
	/// <remarks>A sample showing how filters might be extended.</remarks>
	[Obsolete("Use ExtendedPathFilter instead")]
	public class NameAndSizeFilter : PathFilter
	{
		/// <summary>
		/// Initialise a new instance of NameAndSizeFilter.
		/// </summary>
		/// <param name="filter">The filter to apply.</param>
		/// <param name="minSize">The minimum file size to include.</param>
		/// <param name="maxSize">The maximum file size to include.</param>
		public NameAndSizeFilter(string filter, long minSize, long maxSize)
			: base(filter)
		{
			MinSize = minSize;
			MaxSize = maxSize;
		}

		/// <summary>
		/// Test a filename to see if it matches the filter.
		/// </summary>
		/// <param name="name">The filename to test.</param>
		/// <returns>True if the filter matches, false otherwise.</returns>
		public override bool IsMatch(string name)
		{
			bool result = base.IsMatch(name);

			if (result)
			{
				var fileInfo = new FileInfo(name);
				long length = fileInfo.Length;
				result =
					(MinSize <= length) &&
					(MaxSize >= length);
			}
			return result;
		}

		/// <summary>
		/// Get/set the minimum size for a file that will match this filter.
		/// </summary>
		public long MinSize
		{
			get { return minSize_; }
			set
			{
				if ((value < 0) || (maxSize_ < value))
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				minSize_ = value;
			}
		}

		/// <summary>
		/// Get/set the maximum size for a file that will match this filter.
		/// </summary>
		public long MaxSize
		{
			get { return maxSize_; }
			set
			{
				if ((value < 0) || (minSize_ > value))
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				maxSize_ = value;
			}
		}

		#region Instance Fields

		private long minSize_;
		private long maxSize_ = long.MaxValue;

		#endregion Instance Fields
	}
}
