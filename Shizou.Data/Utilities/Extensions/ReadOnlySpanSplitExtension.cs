namespace Shizou.Data.Utilities.Extensions;

public static class ReadOnlySpanSplitExtension
{
    public static SplitEnumerator SplitSpan(this string str, char seperator, params char[] seperators)
    {
        return new SplitEnumerator(str, new[] { seperator }.Concat(seperators).ToArray());
    }

    public static SplitEnumerator SplitSpan(this ReadOnlySpan<char> str, char seperator, params char[] seperators)
    {
        return new SplitEnumerator(str, new[] { seperator }.Concat(seperators).ToArray());
    }

    public ref struct SplitEnumerator
    {
        private ReadOnlySpan<char> _str;
        private readonly ReadOnlySpan<char> _seperators;

        public ReadOnlySpan<char> Current { get; private set; }

        public SplitEnumerator GetEnumerator()
        {
            return this;
        }

        public SplitEnumerator(ReadOnlySpan<char> str, ReadOnlySpan<char> seperators)
        {
            _str = str;
            _seperators = seperators;
            Current = default;
        }

        public bool MoveNext()
        {
            if (_str.Length == 0)
                return false;
            var index = _str.IndexOfAny(_seperators);
            if (index == -1)
            {
                Current = _str;
                _str = ReadOnlySpan<char>.Empty;
                return true;
            }

            Current = _str.Slice(0, index);
            _str = _str.Slice(index + 1);
            return true;
        }
    }
}
