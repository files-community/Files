#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Threading;

    /// <summary>
    /// SevenZip Extractor/Compressor base class. Implements Password string, ReportErrors flag.
    /// </summary>
    public abstract class SevenZipBase : MarshalByRefObject
    {
        private readonly bool _reportErrors;
        private readonly int _uniqueID;
        private static readonly List<int> Identifiers = new List<int>();

        /// <summary>
        /// True if the instance of the class needs to be recreated in new thread context; otherwise, false.
        /// </summary>
        protected internal bool NeedsToBeRecreated;

        internal virtual void SaveContext()
        {
            Context = SynchronizationContext.Current;
            NeedsToBeRecreated = true;
        }

        internal virtual void ReleaseContext()
        {
            Context = null;
            NeedsToBeRecreated = true;
            GC.SuppressFinalize(this);
        }

        private delegate void EventHandlerDelegate<T>(EventHandler<T> handler, T e) where T : EventArgs;

        internal void OnEvent<T>(EventHandler<T> handler, T e, bool synchronous) where T : EventArgs
        {
            try
            {
                if (handler != null)
                {
                    switch (EventSynchronization)
                    {
                        case EventSynchronizationStrategy.AlwaysAsynchronous:
                            synchronous = false;
                            break;
                        case EventSynchronizationStrategy.AlwaysSynchronous:
                            synchronous = true;
                            break;
                    }

                    if (Context == null)
                    {
                        // Usual synchronous call
                        handler(this, e);
                    }
                    else
                    {
                        var callback = new SendOrPostCallback(obj =>
                        {
                            var array = (object[])obj;
                            ((EventHandler<T>)array[0])(array[1], (T)array[2]);
                        });

                        if (synchronous)
                        {
                            // Could be just handler(this, e);
                            Context.Send(callback, new object[] { handler, this, e });
                        }
                        else
                        {
                            Context.Post(callback, new object[] { handler, this, e });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddException(ex);
            }
        }

        internal SynchronizationContext Context { get; set; }

        /// <summary>
        /// Gets or sets the event synchronization strategy.
        /// </summary>
        public EventSynchronizationStrategy EventSynchronization { get; set; }

        /// <summary>
        /// Gets the unique identifier of this SevenZipBase instance.
        /// </summary>
        public int UniqueID => _uniqueID;

        /// <summary>
        /// User exceptions thrown during the requested operations, for example, in events.
        /// </summary>
        private readonly List<Exception> _exceptions = new List<Exception>();

        private static int GetUniqueID()
        {
            lock (Identifiers)
            {
                int id;

                var rnd = new Random(DateTime.Now.Millisecond);

                do
                {
                    id = rnd.Next(int.MaxValue);
                }
                while (Identifiers.Contains(id));

                Identifiers.Add(id);

                return id;
            }
        }

        /// <summary>
        /// Initializes a new instance of the SevenZipBase class.
        /// </summary>
        /// <param name="password">The archive password.</param>
        protected SevenZipBase(string password = "")
        {
            Password = password;
            _reportErrors = true;
            _uniqueID = GetUniqueID();
        }

        /// <summary>
        /// Removes the UniqueID from the list.
        /// </summary>
        ~SevenZipBase()
        {
            // This lock probably isn't necessary but just in case...
            lock (Identifiers)
            {
                Identifiers.Remove(_uniqueID);
            }
        }

        /// <summary>
        /// Gets or sets the archive password
        /// </summary>
        public string Password { get; protected set; }

        /// <summary>
        /// Gets or sets throw exceptions on archive errors flag
        /// </summary>
        internal bool ReportErrors => _reportErrors;

        /// <summary>
        /// Gets the user exceptions thrown during the requested operations, for example, in events.
        /// </summary>
        internal ReadOnlyCollection<Exception> Exceptions => new ReadOnlyCollection<Exception>(_exceptions);

        internal void AddException(Exception e)
        {
            _exceptions.Add(e);
        }

        internal void ClearExceptions()
        {
            _exceptions.Clear();
        }

        internal bool HasExceptions => _exceptions.Count > 0;

        /// <summary>
        /// Throws the specified exception when is able to.
        /// </summary>
        /// <param name="e">The exception to throw.</param>
        /// <param name="handler">The handler responsible for the exception.</param>
        internal bool ThrowException(CallbackBase handler, params Exception[] e)
        {
            if (_reportErrors && (handler == null || !handler.Canceled))
            {
                throw e[0];
            }

            return false;
        }

        internal void ThrowUserException()
        {
            if (HasExceptions)
            {
                throw new SevenZipException(SevenZipException.USER_EXCEPTION_MESSAGE);
            }
        }

        /// <summary>
        /// Throws exception if HRESULT != 0.
        /// </summary>
        /// <param name="hresult">Result code to check.</param>
        /// <param name="message">Exception message.</param>
        /// <param name="handler">The class responsible for the callback.</param>
        internal void CheckedExecute(int hresult, string message, CallbackBase handler)
        {
            if (hresult != (int)OperationResult.Ok || handler.HasExceptions)
            {
                if (!handler.HasExceptions)
                {
                    if (hresult < -2000000000)
                    {
                        SevenZipException exception;

                        switch (hresult)
                        {
                            case -2146233067:
                                exception = new SevenZipException("Operation is not supported. (0x80131515: E_NOTSUPPORTED)");
                                break;
                            case -2147024784:
                                exception = new SevenZipException("There is not enough space on the disk. (0x80070070: ERROR_DISK_FULL)");
                                break;
                            case -2147024864:
                                exception = new SevenZipException("The file is being used by another process. (0x80070020: ERROR_SHARING_VIOLATION)");
                                break;
                            case -2147024882:
                                exception = new SevenZipException("There is not enough memory (RAM). (0x8007000E: E_OUTOFMEMORY)");
                                break;
                            case -2147024809:
                                exception = new SevenZipException("Invalid arguments provided. (0x80070057: E_INVALIDARG)");
                                break;
                            case -2147467263:
                                exception = new SevenZipException("Functionality not implemented. (0x80004001: E_NOTIMPL)");
                                break;
                            case -2147024891:
                                exception = new SevenZipException("Access is denied. (0x80070005: E_ACCESSDENIED)");
                                break;
                            default:
                                exception = new SevenZipException(
                                    $"Execution has failed due to an internal SevenZipSharp issue (0x{hresult:x} / {hresult}).\n" +
                                    "Please report it to https://github.com/squid-box/SevenZipSharp/issues/, include the release number, 7z version used, and attach the archive.");
                                break;
                        }

                        ThrowException(handler, exception);
                    }
                    else
                    {
                        ThrowException(handler,
                                       new SevenZipException(message + hresult.ToString(CultureInfo.InvariantCulture) +
                                                             '.'));
                    }
                }
                else
                {
                    ThrowException(handler, handler.Exceptions[0]);
                }
            }
        }

        /// <summary>
        /// Changes the path to the 7-zip native library.
        /// </summary>
        /// <param name="libraryPath">The path to the 7-zip native library.</param>
        public static void SetLibraryPath(string libraryPath)
        {
            SevenZipLibraryManager.SetLibraryPath(libraryPath);
        }

        /// <summary>
        /// Gets the current library features.
        /// </summary>
        //[CLSCompliant(false)]
        //public static LibraryFeature CurrentLibraryFeatures => SevenZipLibraryManager.CurrentLibraryFeatures;

        /// <summary>
        /// Determines whether the specified System.Object is equal to the current SevenZipBase.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current SevenZipBase.</param>
        /// <returns>true if the specified System.Object is equal to the current SevenZipBase; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is SevenZipBase instance))
            {
                return false;
            }

            return _uniqueID == instance._uniqueID;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns> A hash code for the current SevenZipBase.</returns>
        public override int GetHashCode()
        {
            return _uniqueID;
        }

        /// <summary>
        /// Returns a System.String that represents the current SevenZipBase.
        /// </summary>
        /// <returns>A System.String that represents the current SevenZipBase.</returns>
        public override string ToString()
        {
            var type = "SevenZipBase";

            if (this is SevenZipExtractor)
            {
                type = "SevenZipExtractor";
            }

            if (this is SevenZipCompressor)
            {
                type = "SevenZipCompressor";
            }

            return $"{type} [{_uniqueID}]";
        }
    }
}

#endif
