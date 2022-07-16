#if UNMANAGED

namespace SevenZip
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    internal class CallbackBase : MarshalByRefObject
    {
        private readonly string _password;
        private readonly bool _reportErrors;

        /// <summary>
        /// User exceptions thrown during the requested operations, for example, in events.
        /// </summary>
        private readonly List<Exception> _exceptions = new List<Exception>();

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the CallbackBase class.
        /// </summary>
        protected CallbackBase()
        {
            _password = "";
            _reportErrors = true;
        }

        /// <summary>
        /// Initializes a new instance of the CallbackBase class.
        /// </summary>
        /// <param name="password">The archive password.</param>
        protected CallbackBase(string password)
        {
            if (String.IsNullOrEmpty(password))
            {
                throw new SevenZipException("Empty password was specified.");
            }
            _password = password;
            _reportErrors = true;
        }
        #endregion

        /// <summary>
        /// Gets or sets the archive password
        /// </summary>
        public string Password => _password;

        /// <summary>
        /// Gets or sets the value indicating whether the current procedure was cancelled.
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// Gets or sets throw exceptions on archive errors flag
        /// </summary>
        public bool ReportErrors => _reportErrors;

        /// <summary>
        /// Gets the user exceptions thrown during the requested operations, for example, in events.
        /// </summary>
        public ReadOnlyCollection<Exception> Exceptions => new ReadOnlyCollection<Exception>(_exceptions);

        public void AddException(Exception e)
        {
            _exceptions.Add(e);
        }

        public void ClearExceptions()
        {
            _exceptions.Clear();
        }

        public bool HasExceptions => _exceptions.Count > 0;

        /// <summary>
        /// Throws the specified exception when is able to.
        /// </summary>
        /// <param name="e">The exception to throw.</param>
        /// <param name="handler">The handler responsible for the exception.</param>
        public bool ThrowException(CallbackBase handler, params Exception[] e)
        {
            if (_reportErrors && (handler == null || !handler.Canceled))
            {
                throw e[0];
            }
            return false;
        }

        /// <summary>
        /// Throws the first exception in the list if any exists.
        /// </summary>
        /// <returns>True means no exceptions.</returns>
        public bool ThrowException()
        {
            if (HasExceptions && _reportErrors)
            {
                throw _exceptions[0];
            }
            return true;
        }

        public void ThrowUserException()
        {
            if (HasExceptions)
            {
                throw new SevenZipException(SevenZipException.USER_EXCEPTION_MESSAGE);
            }
        }
    }
}

#endif
