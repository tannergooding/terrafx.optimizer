// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static TerraFX.Optimization.Utilities.ExceptionUtilities;

namespace TerraFX.Optimization.CodeAnalysis;

/// <summary>Defines a basic block of statements, for use in a control flow graph.</summary>
public sealed partial class BasicBlock<T> : IEnumerable<Statement<T>>, IFormattable, ISpanFormattable, IUtf8SpanFormattable
{
    private readonly HashSet<BasicBlock<T>> _childBlocks;
    private readonly HashSet<BasicBlock<T>> _parentBlocks;

    private BasicBlock<T>? _nextBlock;
    private BasicBlock<T>? _previousBlock;

    private Statement<T> _firstStatement;
    private int _id;

    internal BasicBlock(int id, Statement<T> firstStatement)
    {
        _childBlocks = [];
        _parentBlocks = [];

        _firstStatement = firstStatement;
        _id = id;
    }

    /// <summary>Gets the blocks that can be reached directly from this block via control flow.</summary>
    public IReadOnlySet<BasicBlock<T>> ChildBlocks => _childBlocks;

    /// <summary>Gets or sets the first statement of this block.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public Statement<T> FirstStatement
    {
        get
        {
            return _firstStatement;
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _firstStatement = value;
        }
    }

    /// <summary>Gets or sets the identifier for this block.</summary>
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

    /// <summary>Gets the number of blocks that can directly reach this block via control flow.</summary>
    public int InDegree => _parentBlocks.Count;

    /// <summary>Gets or sets the block that follows this block.</summary>
    /// <remarks>The next block does not necessarily have control flow from this block.</remarks>
    public BasicBlock<T>? NextBlock
    {
        get
        {
            return _nextBlock;
        }

        set
        {
            _nextBlock = value;
        }
    }

    /// <summary>Gets the number of blocks that can be reached directly from this block via control flow.</summary>
    public int OutDegree => _childBlocks.Count;

    /// <summary>Gets the blocks that can reach this block directly via control flow.</summary>
    public IReadOnlySet<BasicBlock<T>> ParentBlocks => _parentBlocks;

    /// <summary>Gets or sets the block that precedes this block.</summary>
    /// <remarks>The previous block does not necessarily have control flow into this block.</remarks>
    public BasicBlock<T>? PreviousBlock
    {
        get
        {
            return _previousBlock;
        }

        set
        {
            _previousBlock = value;
        }
    }

    /// <summary>Adds a block that can be reached directly from this block via control flow.</summary>
    /// <param name="block">The block to add as a child.</param>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <c>null</c>.</exception>
    public void AddChildBlock(BasicBlock<T> block)
    {
        ArgumentNullException.ThrowIfNull(block);
        _ = _childBlocks.Add(block);
        _ = block._parentBlocks.Add(this);
    }

    /// <summary>Adds a set of blocks that can be reached directly from this block via control flow.</summary>
    /// <param name="blocks">The blocks to add as children.</param>
    /// <exception cref="ArgumentNullException"><paramref name="blocks"/> is <c>null</c> or contains <c>null</c> elements.</exception>
    public void AddChildBlocks(IEnumerable<BasicBlock<T>> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        var childBlocks = _childBlocks;

        if (blocks.TryGetNonEnumeratedCount(out var blockCount))
        {
            _ = childBlocks.EnsureCapacity(childBlocks.Count + blockCount);
        }

        foreach (var block in blocks)
        {
            AddChildBlock(block);
        }
    }

    /// <summary>Adds a set of blocks that can be reached directly from this block via control flow.</summary>
    /// <param name="blocks">The blocks to add as children.</param>
    /// <exception cref="ArgumentNullException"><paramref name="blocks"/> is <c>null</c> or contains <c>null</c> elements.</exception>
    public void AddChildBlocks(params ReadOnlySpan<BasicBlock<T>> blocks)
    {
        var childBlocks = _childBlocks;
        _ = childBlocks.EnsureCapacity(childBlocks.Count + blocks.Length);

        foreach (var block in blocks)
        {
            AddChildBlock(block);
        }
    }

    /// <summary>Determines whether a block can be reached from this block via control flow.</summary>
    /// <param name="block">The block to check for reachability.</param>
    /// <returns><c>true</c> if the block can be reached; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <c>null</c>.</exception>
    public bool CanReach(BasicBlock<T> block)
    {
        // Determining if this block can reach the target block is a simple traversal
        // where we return true if the traversed block is ever the target block.

        ArgumentNullException.ThrowIfNull(block);
        var foundBlock = false;

        foreach (var childBlock in TraverseDepthFirst())
        {
            if (childBlock == block)
            {
                foundBlock = true;
                break;
            }
        }

        return foundBlock;
    }

    /// <summary>Clears the set of blocks that can be reached directly from this block via control flow.</summary>
    public void ClearChildBlocks()
    {
        var childBlocks = _childBlocks;

        foreach (var childBlock in childBlocks)
        {
            _ = childBlock._parentBlocks.Remove(this);
        }

        childBlocks.Clear();
    }

    /// <summary>Determines whether this block contains a statement.</summary>
    /// <param name="statement">The statement to check for.</param>
    /// <returns><c>true</c> if the block contains the statement; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="statement"/> is <c>null</c>.</exception>
    public bool ContainsStatement(Statement<T> statement)
    {
        ArgumentNullException.ThrowIfNull(statement);
        var foundStatement = false;

        for (var currentStatement = _firstStatement; currentStatement != null; currentStatement = currentStatement.NextStatement)
        {
            if (statement == currentStatement)
            {
                foundStatement = true;
                break;
            }
        }

        return foundStatement;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
    public Enumerator GetEnumerator() => new Enumerator(_firstStatement);

    /// <summary>Gets the number of statements in the block.</summary>
    /// <returns>The number of statements in the block.</returns>
    public int GetStatementCount()
    {
        var statementCount = 0;

        for (var currentStatement = _firstStatement; currentStatement != null; currentStatement = currentStatement.NextStatement)
        {
            statementCount++;
        }

        return statementCount;
    }

    /// <summary>Inserts the block after another block.</summary>
    /// <param name="block">The block after which to insert this block.</param>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The current block cannot be inserted after <paramref name="block" /> as it would lose track of <see cref="PreviousBlock" /> or <see cref="NextBlock" />.</exception>
    public void InsertAfter(BasicBlock<T> block)
    {
        ArgumentNullException.ThrowIfNull(block);
        var nextBlock = block._nextBlock;

        if (_previousBlock is not null)
        {
            ThrowForInvalidInsertAfter(this, block);
        }

        if (nextBlock is not null)
        {
            if (_nextBlock is not null)
            {
                ThrowForInvalidInsertAfter(this, block);
            }

            nextBlock._previousBlock = this;
            _nextBlock = nextBlock;
        }

        block._nextBlock = this;
        _previousBlock = block;
    }

    /// <summary>Inserts the block before another block.</summary>
    /// <param name="block">The block before which to insert this block.</param>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">The current block cannot be inserted before <paramref name="block" /> as it would lose track of <see cref="PreviousBlock" /> or <see cref="NextBlock" />.</exception>
    public void InsertBefore(BasicBlock<T> block)
    {
        ArgumentNullException.ThrowIfNull(block);
        var previousBlock = block._previousBlock;

        if (_nextBlock is not null)
        {
            ThrowForInvalidInsertBefore(this, block);
        }

        if (previousBlock is not null)
        {
            if (_previousBlock is not null)
            {
                ThrowForInvalidInsertBefore(this, block);
            }

            previousBlock._nextBlock = this;
            _previousBlock = previousBlock;
        }

        block._previousBlock = this;
        _nextBlock = block;
    }

    /// <summary>Determines whether the specified block is adjacent to this block.</summary>
    /// <param name="block">The block to check for adjacency.</param>
    /// <returns><see langword="true"/> if the specified block is a child or parent of this block; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <c>null</c>.</exception>
    public bool IsAdjacent(BasicBlock<T> block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return _childBlocks.Contains(block) || _parentBlocks.Contains(block);
    }

    /// <summary>Removes a block that can no longer be directly reached from this block.</summary>
    /// <param name="block">The child block to remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <c>null</c>.</exception>
    public void RemoveChildBlock(BasicBlock<T> block)
    {
        ArgumentNullException.ThrowIfNull(block);
        _ = block._parentBlocks.Remove(this);
        _ = _childBlocks.Remove(block);
    }

    /// <summary>Returns a string that represents the current block.</summary>
    /// <returns>A string that represents the current block.</returns>
    public override string ToString() => ToString(formatProvider: null);

    /// <summary>Returns a string that represents the current block.</summary>
    /// <param name="formatProvider">The format provider to use.</param>
    /// <returns>A string that represents the current block.</returns>
    public string ToString(IFormatProvider? formatProvider) => string.Create(formatProvider, $"BB{_id:X4}");

    /// <summary>Provides an enumerable for traversing the block in breadth-first order.</summary>
    /// <returns>An enumerable for breadth-first traversal.</returns>
    public BreadthFirstEnumerable TraverseBreadthFirst() => new BreadthFirstEnumerable(this);

    /// <summary>Provides an enumerable for traversing the block in depth-first order.</summary>
    /// <returns>An enumerable for depth-first traversal.</returns>
    public DepthFirstEnumerable TraverseDepthFirst() => new DepthFirstEnumerable(this);

    /// <summary>Tries to format the current block as a UTF-8 string.</summary>
    /// <param name="utf8Destination">The destination span for the UTF-8 string.</param>
    /// <param name="bytesWritten">The number of bytes written to the destination span.</param>
    /// <param name="provider">The format provider to use.</param>
    /// <returns><see langword="true"/> if the block was successfully formatted; otherwise, <see langword="false"/>.</returns>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, IFormatProvider? provider = null)
    {
        var succeeded = false;
        bytesWritten = 0;

        if (utf8Destination.Length >= 6)
        {
            if (_id.TryFormat(utf8Destination[2..], out bytesWritten, "X4", provider))
            {
                utf8Destination[0] = (byte)'B';
                utf8Destination[1] = (byte)'B';

                bytesWritten += 2;
                succeeded = true;
            }
        }

        return succeeded;
    }

    /// <summary>Tries to format the current block as a UTF-16 string.</summary>
    /// <param name="destination">The destination span for the UTF-16 string.</param>
    /// <param name="charsWritten">The number of characters written to the destination span.</param>
    /// <param name="formatProvider">The format provider to use.</param>
    /// <returns><see langword="true"/> if the block was successfully formatted; otherwise, <see langword="false"/>.</returns>
    public bool TryFormat(Span<char> destination, out int charsWritten, IFormatProvider? formatProvider = null)
    {
        var succeeded = false;
        charsWritten = 0;

        if (destination.Length >= 6)
        {
            if (_id.TryFormat(destination[2..], out charsWritten, "X4", formatProvider))
            {
                destination[0] = 'B';
                destination[1] = 'B';

                charsWritten += 2;
                succeeded = true;
            }
        }

        return succeeded;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<Statement<T>> IEnumerable<Statement<T>>.GetEnumerator() => GetEnumerator();

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(formatProvider);

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(destination, out charsWritten, provider);

    bool IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => TryFormat(utf8Destination, out bytesWritten, provider);
}
