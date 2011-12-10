using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MindTouch.Clacks.Server {
    internal static class Logger {

        private static readonly bool _loggingIsOff = true;

        static Logger() {
            try {
                Assembly.Load("log4net");
                _loggingIsOff = false;
            } catch { }
        }

        public static ILog CreateLog() {
            var frame = new System.Diagnostics.StackFrame(1, false);
            var type = frame.GetMethod().DeclaringType;
            return _loggingIsOff ? (ILog)new NoLog() : new Log4NetLogger(type);
        }

        internal interface ILog {
            bool IsDebugEnabled { get; }
            bool IsInfoEnabled { get; }
            bool IsWarnEnabled { get; }
            bool IsErrorEnabled { get; }
            bool IsFatalEnabled { get; }
            void Debug(object message);
            void Debug(object message, Exception exception);
            void DebugFormat(string format, params object[] args);
            void DebugFormat(string format, object arg0);
            void DebugFormat(string format, object arg0, object arg1);
            void DebugFormat(string format, object arg0, object arg1, object arg2);
            void DebugFormat(IFormatProvider provider, string format, params object[] args);
            void Info(object message);
            void Info(object message, Exception exception);
            void InfoFormat(string format, params object[] args);
            void InfoFormat(string format, object arg0);
            void InfoFormat(string format, object arg0, object arg1);
            void InfoFormat(string format, object arg0, object arg1, object arg2);
            void InfoFormat(IFormatProvider provider, string format, params object[] args);
            void Warn(object message);
            void Warn(object message, Exception exception);
            void WarnFormat(string format, params object[] args);
            void WarnFormat(string format, object arg0);
            void WarnFormat(string format, object arg0, object arg1);
            void WarnFormat(string format, object arg0, object arg1, object arg2);
            void WarnFormat(IFormatProvider provider, string format, params object[] args);
            void Error(object message);
            void Error(object message, Exception exception);
            void ErrorFormat(string format, params object[] args);
            void ErrorFormat(string format, object arg0);
            void ErrorFormat(string format, object arg0, object arg1);
            void ErrorFormat(string format, object arg0, object arg1, object arg2);
            void ErrorFormat(IFormatProvider provider, string format, params object[] args);
            void Fatal(object message);
            void Fatal(object message, Exception exception);
            void FatalFormat(string format, params object[] args);
            void FatalFormat(string format, object arg0);
            void FatalFormat(string format, object arg0, object arg1);
            void FatalFormat(string format, object arg0, object arg1, object arg2);
            void FatalFormat(IFormatProvider provider, string format, params object[] args);
        }

        private class NoLog : ILog {
            public bool IsDebugEnabled {
                get { return false; }
            }

            public bool IsInfoEnabled {
                get { return false; }
            }

            public bool IsWarnEnabled {
                get { return false; }
            }

            public bool IsErrorEnabled {
                get { return false; }
            }

            public bool IsFatalEnabled {
                get { return false; }
            }

            public void Debug(object message) { }
            public void Debug(object message, Exception exception) { }
            public void DebugFormat(string format, params object[] args) { }
            public void DebugFormat(string format, object arg0) { }
            public void DebugFormat(string format, object arg0, object arg1) { }
            public void DebugFormat(string format, object arg0, object arg1, object arg2) { }
            public void DebugFormat(IFormatProvider provider, string format, params object[] args) { }
            public void Info(object message) { }
            public void Info(object message, Exception exception) { }
            public void InfoFormat(string format, params object[] args) { }
            public void InfoFormat(string format, object arg0) { }
            public void InfoFormat(string format, object arg0, object arg1) { }
            public void InfoFormat(string format, object arg0, object arg1, object arg2) { }
            public void InfoFormat(IFormatProvider provider, string format, params object[] args) { }
            public void Warn(object message) { }
            public void Warn(object message, Exception exception) { }
            public void WarnFormat(string format, params object[] args) { }
            public void WarnFormat(string format, object arg0) { }
            public void WarnFormat(string format, object arg0, object arg1) { }
            public void WarnFormat(string format, object arg0, object arg1, object arg2) { }
            public void WarnFormat(IFormatProvider provider, string format, params object[] args) { }
            public void Error(object message) { }
            public void Error(object message, Exception exception) { }
            public void ErrorFormat(string format, params object[] args) { }
            public void ErrorFormat(string format, object arg0) { }
            public void ErrorFormat(string format, object arg0, object arg1) { }
            public void ErrorFormat(string format, object arg0, object arg1, object arg2) { }
            public void ErrorFormat(IFormatProvider provider, string format, params object[] args) { }
            public void Fatal(object message) { }
            public void Fatal(object message, Exception exception) { }
            public void FatalFormat(string format, params object[] args) { }
            public void FatalFormat(string format, object arg0) { }
            public void FatalFormat(string format, object arg0, object arg1) { }
            public void FatalFormat(string format, object arg0, object arg1, object arg2) { }
            public void FatalFormat(IFormatProvider provider, string format, params object[] args) { }
        }

        private class Log4NetLogger : ILog {

            //--- Fields ---
            private readonly log4net.ILog _rootLogger;
            private readonly Type _type;
            private readonly log4net.Core.ILogger _logger;

            //--- Constructors ---
            public Log4NetLogger(Type type) {
                _type = type;
                _rootLogger = log4net.LogManager.GetLogger(_type);
                _logger = _rootLogger.Logger;
            }

            //--- Properties ---
            public bool IsDebugEnabled {
                get { return _rootLogger.IsDebugEnabled; }
            }

            public bool IsInfoEnabled {
                get { return _rootLogger.IsInfoEnabled; }
            }

            public bool IsWarnEnabled {
                get { return _rootLogger.IsWarnEnabled; }
            }

            public bool IsErrorEnabled {
                get { return _rootLogger.IsFatalEnabled; }
            }

            public bool IsFatalEnabled {
                get { return _rootLogger.IsFatalEnabled; }
            }

            //--- Methods ---
            public void Debug(object message) {
                if(!IsDebugEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Debug, message, null);
            }

            public void Debug(object message, Exception exception) {
                if(!IsDebugEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Debug, message, exception);
            }

            public void DebugFormat(string format, params object[] args) {
                if(!IsDebugEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Debug, string.Format(CultureInfo.InvariantCulture, format, args), null);
            }

            public void DebugFormat(string format, object arg0) {
                if(!IsDebugEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Debug, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
            }

            public void DebugFormat(string format, object arg0, object arg1) {
                if(!IsDebugEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Debug, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
            }

            public void DebugFormat(string format, object arg0, object arg1, object arg2) {
                if(!IsDebugEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Debug, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
            }

            public void DebugFormat(IFormatProvider provider, string format, params object[] args) {
                if(!IsDebugEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Debug, string.Format(provider, format, args), null);
            }

            public void Info(object message) {
                if(!IsInfoEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Info, message, null);
            }

            public void Info(object message, Exception exception) {
                if(!IsInfoEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Info, message, exception);
            }

            public void InfoFormat(string format, params object[] args) {
                if(!IsInfoEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Info, string.Format(CultureInfo.InvariantCulture, format, args), null);
            }

            public void InfoFormat(string format, object arg0) {
                if(!IsInfoEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Info, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
            }

            public void InfoFormat(string format, object arg0, object arg1) {
                if(!IsInfoEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Info, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
            }

            public void InfoFormat(string format, object arg0, object arg1, object arg2) {
                if(!IsInfoEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Info, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
            }

            public void InfoFormat(IFormatProvider provider, string format, params object[] args) {
                if(!IsInfoEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Info, string.Format(provider, format, args), null);
            }

            public void Warn(object message) {
                if(!IsWarnEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Warn, message, null);
            }

            public void Warn(object message, Exception exception) {
                if(!IsWarnEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Warn, message, exception);
            }

            public void WarnFormat(string format, params object[] args) {
                if(!IsWarnEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Warn, string.Format(CultureInfo.InvariantCulture, format, args), null);
            }

            public void WarnFormat(string format, object arg0) {
                if(!IsWarnEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Warn, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
            }

            public void WarnFormat(string format, object arg0, object arg1) {
                if(!IsWarnEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Warn, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
            }

            public void WarnFormat(string format, object arg0, object arg1, object arg2) {
                if(!IsWarnEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Warn, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
            }

            public void WarnFormat(IFormatProvider provider, string format, params object[] args) {
                if(!IsWarnEnabled) {
                    return;
                }
                _logger.Log(_type, log4net.Core.Level.Warn, string.Format(provider, format, args), null);
            }

            public void Error(object message) {
                _logger.Log(_type, log4net.Core.Level.Error, message, null);
            }

            public void Error(object message, Exception exception) {
                _logger.Log(_type, log4net.Core.Level.Error, message, exception);
            }

            public void ErrorFormat(string format, params object[] args) {
                _logger.Log(_type, log4net.Core.Level.Error, string.Format(CultureInfo.InvariantCulture, format, args), null);
            }

            public void ErrorFormat(string format, object arg0) {
                _logger.Log(_type, log4net.Core.Level.Error, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
            }

            public void ErrorFormat(string format, object arg0, object arg1) {
                _logger.Log(_type, log4net.Core.Level.Error, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
            }

            public void ErrorFormat(string format, object arg0, object arg1, object arg2) {
                _logger.Log(_type, log4net.Core.Level.Error, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
            }

            public void ErrorFormat(IFormatProvider provider, string format, params object[] args) {
                _logger.Log(_type, log4net.Core.Level.Error, string.Format(provider, format, args), null);
            }

            public void Fatal(object message) {
                _logger.Log(_type, log4net.Core.Level.Fatal, message, null);
            }

            public void Fatal(object message, Exception exception) {
                _logger.Log(_type, log4net.Core.Level.Fatal, message, exception);
            }

            public void FatalFormat(string format, params object[] args) {
                _logger.Log(_type, log4net.Core.Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, args), null);
            }

            public void FatalFormat(string format, object arg0) {
                _logger.Log(_type, log4net.Core.Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, arg0), null);
            }

            public void FatalFormat(string format, object arg0, object arg1) {
                _logger.Log(_type, log4net.Core.Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1), null);
            }

            public void FatalFormat(string format, object arg0, object arg1, object arg2) {
                _logger.Log(_type, log4net.Core.Level.Fatal, string.Format(CultureInfo.InvariantCulture, format, arg0, arg1, arg2), null);
            }

            public void FatalFormat(IFormatProvider provider, string format, params object[] args) {
                _logger.Log(_type, log4net.Core.Level.Fatal, string.Format(provider, format, args), null);
            }
        }
    }
}
