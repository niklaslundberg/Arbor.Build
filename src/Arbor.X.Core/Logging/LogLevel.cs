using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbor.X.Core.Logging
{
    public struct LogLevel : IEquatable<LogLevel>
    {
        public bool Equals(LogLevel other)
        {
            return Level == other.Level;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LogLevel && Equals((LogLevel) obj);
        }

        public override int GetHashCode()
        {
            return Level;
        }

        public static bool operator ==(LogLevel left, LogLevel right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LogLevel left, LogLevel right)
        {
            return !left.Equals(right);
        }

        static readonly LogLevel _critical = new LogLevel("critical", "Critical", 1);
        static readonly LogLevel _error = new LogLevel("error", "Error", 2);
        static readonly LogLevel _warning = new LogLevel("warning", "Warning", 4);
        static readonly LogLevel _information = new LogLevel("information", "Information", 8);
        static readonly LogLevel _verbose = new LogLevel("verbose", "Verbose", 16);
        public static readonly LogLevel Debug = new LogLevel("debug", "Debug", 32);
        readonly string _displayName;
        readonly string _invariantName;
        readonly int _level;
        
        LogLevel(string invariantName, string displayName, int level)
        {
            _invariantName = invariantName;
            _displayName = displayName;
            _level = level;
        }

        public string DisplayName
        {
            get { return _displayName ?? Default.DisplayName; }
        }

        public int Level
        {
            get { return _level == 0 ? Default._level : _level; }
        }

        public string InvariantName
        {
            get { return _invariantName ?? Default.InvariantName; }
        }

        public static IEnumerable<LogLevel> AllValues
        {
            get
            {
                yield return Critical;
                yield return Error;
                yield return Warning;
                yield return Information;
                yield return Verbose;
                yield return Debug;
            }
        }

        public static LogLevel Information
        {
            get { return _information; }
        }

        public static LogLevel Default
        {
            get { return Information; }
        }

        public static LogLevel Verbose
        {
            get { return _verbose; }
        }

        public static LogLevel Error
        {
            get { return _error; }
        }

        public static LogLevel Critical
        {
            get { return _critical; }
        }

        public static LogLevel Warning
        {
            get { return _warning; }
        }
        
        public static LogLevel TryParse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Default;
            }

            LogLevel found = AllValues.SingleOrDefault(
                level => level._invariantName.Equals(value, StringComparison.InvariantCultureIgnoreCase));
            
            return found;
        }

        public static LogLevel TryParse(int value)
        {
            LogLevel found = AllValues.SingleOrDefault(
                level => level._level == value);
            
            return found;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public static implicit operator string(LogLevel logLevel)
        {
            return logLevel.DisplayName;
        }
    }
}