// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace TerraFX.Optimization.CodeAnalysis;

/// <summary>Defines a flowgraph where each node defines a basic block of statements.</summary>
public sealed partial class Flowgraph<T> : IEnumerable<BasicBlock<T>>
{
    private BasicBlock<T> _firstBlock;

    internal Flowgraph(BasicBlock<T> firstBlock)
    {
        _firstBlock = firstBlock;
    }

    /// <summary>Gets or sets the first block in the flowgraph.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public BasicBlock<T> FirstBlock
    {
        get
        {
            return _firstBlock;
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _firstBlock = value;
        }
    }

    /// <summary>Determines whether this flowgraph contains a block.</summary>
    /// <param name="block">The block to check for.</param>
    /// <returns><c>true</c> if the flowgraph contains the block; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <c>null</c>.</exception>
    public bool ContainsBlock(BasicBlock<T> block)
    {
        ArgumentNullException.ThrowIfNull(block);
        var foundBlock = false;

        for (var currentBlock = _firstBlock; currentBlock != null; currentBlock = currentBlock.NextBlock)
        {
            if (block == currentBlock)
            {
                foundBlock = true;
                break;
            }
        }

        return foundBlock;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
    public Enumerator GetEnumerator() => new Enumerator(_firstBlock);

    /// <summary>Gets the number of blocks in the flowgraph.</summary>
    /// <returns>The number of blocks in the flowgraph.</returns>
    public int GetBlockCount()
    {
        var blockCount = 0;

        for (var currentBlock = _firstBlock; currentBlock != null; currentBlock = currentBlock.NextBlock)
        {
            blockCount++;
        }

        return blockCount;
    }

    IEnumerator<BasicBlock<T>> IEnumerable<BasicBlock<T>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
