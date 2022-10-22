using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Anvil.CSharp.Logging
{
    /// <summary>
    /// A wrapper for <see cref="Logger"/> that only allows a message to be emitted from a given call site once per session.
    /// A call site is determined to be unique by a combination of its file path and line number.
    /// </summary>
    /// <remarks>
    /// This type's methods are not thread-safe.
    /// It's assumed all calls into this type are coming from a single thread.
    /// TODO: #126 - Make logging thread safe
    /// </remarks>
    public readonly struct OneTimeLogger : ILogger
    {
        private static readonly HashSet<int> s_CalledLogSites = new HashSet<int>();

        /// <summary>
        /// Resets <see cref="OneTimeLogger"/>'s to allow one time logs to be emitted one more time.
        /// </summary>
        public static void Reset()
        {
            s_CalledLogSites.Clear();
        }


        private readonly Logger m_Logger;


        internal OneTimeLogger(in Logger logger)
        {
            m_Logger = logger;
        }

        /// <inheritdoc cref="ILogger.Debug"/>
        public void Debug(object message, [CallerMemberName] string callerName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            if (!ShouldEmitLog(callerPath, callerLine))
            {
                return;
            }

            m_Logger.Debug(message, callerName, callerPath, callerLine);
        }

        /// <inheritdoc cref="ILogger.Warning"/>
        public void Warning(object message, [CallerMemberName] string callerName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            if (!ShouldEmitLog(callerPath, callerLine))
            {
                return;
            }

            m_Logger.Warning(message, callerName, callerPath, callerLine);
        }

        /// <inheritdoc cref="ILogger.Error"/>
        public void Error(object message, [CallerMemberName] string callerName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            if (!ShouldEmitLog(callerPath, callerLine))
            {
                return;
            }

            m_Logger.Error(message, callerName, callerPath, callerLine);
        }

        /// <inheritdoc cref="ILogger.AtLevel"/>
        public void AtLevel(
            LogLevel level,
            object message,
            [CallerMemberName] string callerName = "",
            [CallerFilePath] string callerPath = "",
            [CallerLineNumber] int callerLine = 0)
        {
            if (!ShouldEmitLog(callerPath, callerLine))
            {
                return;
            }

            m_Logger.AtLevel(level, message, callerName, callerPath, callerLine);
        }

        private bool ShouldEmitLog(string callerPath, int callerLine)
        {
            int hash = (callerPath, callerLine).GetHashCode();
            return s_CalledLogSites.Add(hash);
        }
    }
}