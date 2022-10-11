using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Anvil.CSharp.Logging
{
    /// <summary>
    /// A context specific instance that provides a mechanism to emit logs through <see cref="Log"/>.
    /// Automatically provides contextual information to <see cref="Log"/> about caller context including:
    ///  - Optional, per instance, message prefix
    ///  - Caller type name
    ///  - Caller file path
    ///  - Caller name
    ///  - Caller line number
    /// </summary>
    public readonly struct Logger : ILogger
    {
        // NOTE: This is a duplicate of Anvil.CSharp.Reflection.TypeExtension.GetReadableName
        // because the logging namespace is isolated from the rest of Anvil, and thus cannot access that extension
        private static readonly Type s_NullableType = typeof(Nullable<>);
        private static string GetReadableName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            if (type.GetGenericTypeDefinition() == s_NullableType)
            {
                return $"{GetReadableName(type.GenericTypeArguments[0])}?";
            }

            // Remove the generic type count indicator (`n) from the type name
            // Avoid having to calculate the number of digits in the generic type count by assuming it's under 100
            int removeCount = 1 + (type.GenericTypeArguments.Length < 10 ? 1 : 2);
            string name = type.Name[..^removeCount];

            string genericTypeNames = type.GenericTypeArguments
                .Select(GetReadableName)
                .Aggregate((a, b) => $"{a}, {b}");

            return $"{name}<{genericTypeNames}>";
        }

        /// <summary>
        /// The name of the type this <see cref="Logger"/> represents.
        /// </summary>
        public readonly string DerivedTypeName;
        /// <summary>
        /// The custom prefix to prepend to all messages sent through this <see cref="Logger"/>.
        /// </summary>
        public readonly string MessagePrefix;

        /// <summary>
        /// Creates an instance of <see cref="Logger"/> from a <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to create the <see cref="Logger"/> instance for.</param>
        /// <param name="messagePrefix">
        /// An optional <see cref="string"/> to prefix to all messages through this logger.
        /// Useful when there are multiple types that share the same name which need to be differentiated.
        /// </param>
        public Logger(Type type, string messagePrefix = null) : this(GetReadableName(type), messagePrefix) { }
        /// <summary>
        /// Creates an instance of <see cref="Logger"/> from another instance.
        /// </summary>
        /// <param name="instance">The instance to create the <see cref="Logger"/> instance for.</param>
        /// <param name="messagePrefix">
        /// An optional <see cref="string"/> to prefix to all messages through this logger.
        /// Useful when there are multiple instances or types that share the same name which need to be differentiated.
        /// </param>
        public Logger(in object instance, string messagePrefix = null) : this(instance.GetType(), messagePrefix) { }

        private Logger(string derivedTypeName, string messagePrefix)
        {
            DerivedTypeName = derivedTypeName;
            MessagePrefix = messagePrefix;
        }

        /// <inheritdoc cref="ILogger.Debug"/>
        public void Debug(
            object message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerName = "",
            [CallerLineNumber] int callerLine = 0
        ) => Log.DispatchLog(
            LogLevel.Debug,
            string.Concat(MessagePrefix, message),
            DerivedTypeName,
            callerPath,
            callerName,
            callerLine);

        /// <inheritdoc cref="ILogger.Warning"/>
        public void Warning(
            object message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerName = "",
            [CallerLineNumber] int callerLine = 0
        ) => Log.DispatchLog(
            LogLevel.Warning,
            string.Concat(MessagePrefix, message),
            DerivedTypeName,
            callerPath,
            callerName,
            callerLine
        );

        /// <inheritdoc cref="ILogger.Error"/>
        public void Error(
            object message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerName = "",
            [CallerLineNumber] int callerLine = 0
        ) => Log.DispatchLog(
            LogLevel.Error,
            string.Concat(MessagePrefix, message),
            DerivedTypeName,
            callerPath,
            callerName,
            callerLine
        );

        /// <inheritdoc cref="ILogger.AtLevel"/>
        public void AtLevel(
            LogLevel level,
            object message,
            [CallerFilePath] string callerPath = "",
            [CallerMemberName] string callerName = "",
            [CallerLineNumber] int callerLine = 0
        ) => Log.DispatchLog(
            level,
            string.Concat(MessagePrefix, message),
            DerivedTypeName,
            callerPath,
            callerName,
            callerLine);

        public OneTimeLogger OneTime() => new OneTimeLogger(this);
    }
}