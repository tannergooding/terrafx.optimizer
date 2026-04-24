// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using static TerraFX.Optimization.Utilities.ExceptionUtilities;

namespace TerraFX.Optimization.CodeAnalysis;

/// <summary>Represents a statement in a basic block.</summary>
/// <typeparam name="T">The type of the value in the statement.</typeparam>
public sealed class Statement<T> : IFormattable, ISpanFormattable, IUtf8SpanFormattable
{
    private Statement<T>? _nextStatement;
    private Statement<T>? _previousStatement;

    private T _value;
    private int _id;

    internal Statement(int id, T value)
    {
        _id = id;
        _value = value;
    }

    /// <summary>Gets or sets the identifier for this statement.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative.</exception>
    public int Id
    {
        get
        {
            return _id;
        }

        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _id = value;
        }
    }

    /// <summary>Gets or sets the statement that follows this statement in the control flow.</summary>
    public Statement<T>? NextStatement
    {
        get
        {
            return _nextStatement;
        }

        set
        {
            _nextStatement = value;
        }
    }

    /// <summary>Gets or sets the statement that precedes this statement in the control flow.</summary>
    public Statement<T>? PreviousStatement
    {
        get
        {
            return _previousStatement;
        }

        set
        {
            _previousStatement = value;
        }
    }

    /// <summary>Gets or sets the value for this statement.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public T Value
    {
        get
        {
            return _value;
        }

        set
        {
            if (default(T) is null)
            {
                ArgumentNullException.ThrowIfNull(value);
            }
            _value = value;
        }
    }

    /// <summary>Inserts the statement after another statement.</summary>
    /// <param name="statement">The statement after which to insert this statement.</param>
    /// <exception cref="ArgumentNullException"><paramref name="statement"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The current statement cannot be inserted after <paramref name="statement" /> as it would lose track of <see cref="PreviousStatement" /> or <see cref="NextStatement" />.</exception>
    public void InsertAfter(Statement<T> statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        var nextStatement = statement._nextStatement;

        if (_previousStatement is not null)
        {
            ThrowForInvalidInsertAfter(this, statement);
        }

        if (nextStatement is not null)
        {
            if (_nextStatement is not null)
            {
                ThrowForInvalidInsertAfter(this, statement);
            }

            nextStatement._previousStatement = this;
            _nextStatement = nextStatement;
        }

        statement._nextStatement = this;
        _previousStatement = statement;
    }

    /// <summary>Inserts the statement before another statement.</summary>
    /// <param name="statement">The statement before which to insert this statement.</param>
    /// <exception cref="ArgumentNullException"><paramref name="statement"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The current statement cannot be inserted before <paramref name="statement" /> as it would lose track of <see cref="PreviousStatement" /> or <see cref="NextStatement" />.</exception>
    public void InsertBefore(Statement<T> statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        var previousStatement = statement._previousStatement;

        if (_nextStatement is not null)
        {
            ThrowForInvalidInsertBefore(this, statement);
        }

        if (previousStatement is not null)
        {
            if (_previousStatement is not null)
            {
                ThrowForInvalidInsertBefore(this, statement);
            }

            previousStatement._nextStatement = this;
            _previousStatement = previousStatement;
        }

        statement._previousStatement = this;
        _nextStatement = statement;
    }

    /// <summary>Returns a string that represents the current statement.</summary>
    /// <returns>A string that represents the current statement.</returns>
    public override string ToString() => ToString(formatProvider: null);

    /// <summary>Returns a string that represents the current statement.</summary>
    /// <param name="formatProvider">The format provider to use.</param>
    /// <returns>A string that represents the current statement.</returns>
    public string ToString(IFormatProvider? formatProvider) => string.Create(formatProvider, $"STMT{_id:X4}");

    /// <summary>Tries to format the current statement as a UTF-8 string.</summary>
    /// <param name="utf8Destination">The destination span for the UTF-8 string.</param>
    /// <param name="bytesWritten">The number of bytes written to the destination span.</param>
    /// <param name="provider">The format provider to use.</param>
    /// <returns><see langword="true"/> if the statement was successfully formatted; otherwise, <see langword="false"/>.</returns>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, IFormatProvider? provider = null)
    {
        var succeeded = false;
        bytesWritten = 0;

        if (utf8Destination.Length >= 8)
        {
            if (_id.TryFormat(utf8Destination[4..], out bytesWritten, "X4", provider))
            {
                utf8Destination[0] = (byte)'S';
                utf8Destination[1] = (byte)'T';
                utf8Destination[2] = (byte)'M';
                utf8Destination[3] = (byte)'T';

                bytesWritten += 4;
                succeeded = true;
            }
        }

        return succeeded;
    }

    /// <summary>Tries to format the current statement as a UTF-16 string.</summary>
    /// <param name="destination">The destination span for the UTF-16 string.</param>
    /// <param name="charsWritten">The number of characters written to the destination span.</param>
    /// <param name="formatProvider">The format provider to use.</param>
    /// <returns><see langword="true"/> if the statement was successfully formatted; otherwise, <see langword="false"/>.</returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, IFormatProvider? formatProvider = null)
    {
        var succeeded = false;
        charsWritten = 0;

        if (destination.Length >= 8)
        {
            if (_id.TryFormat(destination[4..], out charsWritten, "X4", formatProvider))
            {
                destination[0] = 'S';
                destination[1] = 'T';
                destination[2] = 'M';
                destination[3] = 'T';

                charsWritten += 4;
                succeeded = true;
            }
        }

        return succeeded;
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(formatProvider);

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten, provider);

    bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(utf8Destination, out bytesWritten, provider);
}
