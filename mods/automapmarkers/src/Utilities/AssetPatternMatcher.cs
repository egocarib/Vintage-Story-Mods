using System;

namespace Egocarib.AutoMapMarkers.Utilities
{
    /// <summary>
    /// Compiles a single *-wildcard pattern string into an efficient matcher.
    /// Splits the pattern on * to get segments, then uses StartsWith/IndexOf/EndsWith
    /// checks to match input strings.
    /// </summary>
    public class AssetPatternMatcher
    {
        private readonly string[] _segments;
        private readonly bool _hasWildcard;

        public AssetPatternMatcher(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));

            if (pattern.Contains("*"))
            {
                _segments = pattern.Split('*');
                _hasWildcard = true;
            }
            else
            {
                _segments = new[] { pattern };
                _hasWildcard = false;
            }
        }

        /// <summary>
        /// Returns true if the input string matches this pattern.
        /// </summary>
        public bool Matches(string input)
        {
            if (input == null) return false;

            if (!_hasWildcard)
                return string.Equals(input, _segments[0], StringComparison.Ordinal);

            // First segment: StartsWith check
            if (_segments[0].Length > 0)
            {
                if (!input.StartsWith(_segments[0], StringComparison.Ordinal))
                    return false;
            }

            int cursor = _segments[0].Length;

            // Middle segments: sequential ordered IndexOf checks
            for (int i = 1; i < _segments.Length - 1; i++)
            {
                if (_segments[i].Length == 0) continue;

                int pos = input.IndexOf(_segments[i], cursor, StringComparison.Ordinal);
                if (pos < 0) return false;
                cursor = pos + _segments[i].Length;
            }

            // Last segment: EndsWith check (if non-empty)
            string lastSegment = _segments[_segments.Length - 1];
            if (lastSegment.Length > 0)
            {
                if (!input.EndsWith(lastSegment, StringComparison.Ordinal))
                    return false;
                // Overlap check: the suffix must not overlap with the last matched position
                if (input.Length - lastSegment.Length < cursor)
                    return false;
            }

            return true;
        }
    }
}
