using Microsoft.VisualStudio.Text;
using System;

namespace HumansTxtLanguageService.Syntax
{
    public interface ISyntacticParser
    {
        SyntaxTree Parse(ITextSnapshot snapshot);
    }

    public static class CommonScanner
    {
        public static char? Peek(this ITextSnapshot snapshot, SnapshotPoint point, int delta = 0)
        {
            SnapshotPoint peekPoint = point + delta;

            if (peekPoint >= snapshot.Length)
                return null;

            return peekPoint.GetChar();
        }

        public static string PeekString(this ITextSnapshot snapshot, SnapshotPoint point, int length)
        {
            if (point >= snapshot.Length)
                return null;

            return new SnapshotSpan(point, Math.Min(length, snapshot.Length - point)).GetText();
        }

        public static bool IsAtExact(this ITextSnapshot snapshot, SnapshotPoint point, string text)
        {
            return snapshot.PeekString(point, text.Length) == text;
        }



        public static SnapshotSpan ReadExact(this ITextSnapshot snapshot, ref SnapshotPoint point, string text)
        {
            SnapshotPoint start = point;

            if (snapshot.PeekString(point, text.Length) == text)
                point = point + text.Length;

            return new SnapshotSpan(start, point);
        }

        public static SnapshotSpan ReadWhiteSpace(this ITextSnapshot snapshot, ref SnapshotPoint point)
        {
            return snapshot.ReadWhile(ref point, Char.IsWhiteSpace, rewindWhiteSpace: false);
        }

        public static SnapshotSpan ReadWhile(this ITextSnapshot snapshot, ref SnapshotPoint point, Predicate<char> predicate, bool rewindWhiteSpace = true)
        {
            SnapshotPoint start = point;

            while (
                point.Position < snapshot.Length &&
                predicate(point.GetChar())
            )
                point = point + 1;

            if (rewindWhiteSpace)
                snapshot.RewindWhiteSpace(start, ref point);

            return new SnapshotSpan(start, point);
        }

        public static SnapshotSpan ReadTo(this ITextSnapshot snapshot, ref SnapshotPoint point, string delimiter, bool rewindWhiteSpace = true)
        {
            SnapshotPoint start = point;

            while (
                point.Position < snapshot.Length &&
                !snapshot.IsAtExact(point, delimiter)
            )
                point = point + 1;

            if (rewindWhiteSpace)
                snapshot.RewindWhiteSpace(start, ref point);

            return new SnapshotSpan(start, point);
        }

        internal static void RewindWhiteSpace(this ITextSnapshot snapshot, SnapshotPoint start, ref SnapshotPoint point)
        {
            while (
                point - 1 >= start &&
                Char.IsWhiteSpace((point - 1).GetChar())
            )
                point = point - 1;
        }
    }
}