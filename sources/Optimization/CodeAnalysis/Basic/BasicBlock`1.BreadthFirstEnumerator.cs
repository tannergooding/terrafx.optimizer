// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TerraFX.Optimization.CodeAnalysis;

public partial class BasicBlock<T>
{
    /// <summary>Enumerates <see cref="BasicBlock{T}"/> instances in breadth-first order starting from a root block.</summary>
    public struct BreadthFirstEnumerator : IEnumerator, IEnumerator<BasicBlock<T>>
    {
        private readonly BasicBlock<T> _rootBlock;

        private readonly HashSet<BasicBlock<T>> _encounteredBlocks;
        private readonly Queue<BasicBlock<T>> _pendingBlocks;

        private BasicBlock<T>? _currentBlock;

        /// <summary>Creates a new <see cref="BreadthFirstEnumerator" /> instance.</summary>
        /// <param name="rootBlock">The root block from which to start the breadth-first traversal.</param>
        public BreadthFirstEnumerator(BasicBlock<T> rootBlock)
        {
            ArgumentNullException.ThrowIfNull(rootBlock);
            _rootBlock = rootBlock;

            _encounteredBlocks = [];
            _pendingBlocks = [];

            _ = _encounteredBlocks.Add(rootBlock);
            _pendingBlocks.Enqueue(rootBlock);
        }

        /// <inheritdoc cref="IEnumerator{T}.Current" />
        public readonly BasicBlock<T> Current
        {
            get
            {
                var currentBlock = _currentBlock;
                Debug.Assert(currentBlock is not null);
                return currentBlock;
            }
        }

        /// <inheritdoc cref="IEnumerator.MoveNext" />
        [MemberNotNullWhen(true, nameof(Current))]
        public bool MoveNext()
        {
            // We do a breadth-first traversal of the blocks using a queue-based algorithm
            // to track pending blocks, rather than using recursion, so that we can
            // process much more complex graphs.

            var succeeded = false;

            var encounteredBlocks = _encounteredBlocks;
            var pendingBlocks = _pendingBlocks;

            if (pendingBlocks.TryDequeue(out var currentBlock))
            {
                var childBlocks = currentBlock._childBlocks;
                var childBlockCount = childBlocks.Count;

                _ = encounteredBlocks.EnsureCapacity(encounteredBlocks.Count + childBlockCount);
                _ = pendingBlocks.EnsureCapacity(pendingBlocks.Count + childBlockCount);

                foreach (var childBlock in childBlocks)
                {
                    if (encounteredBlocks.Add(childBlock))
                    {
                        pendingBlocks.Enqueue(childBlock);
                    }
                }

                _currentBlock = currentBlock;
                Debug.Assert(Current is not null);
                succeeded = true;
            }

            return succeeded;
        }

        /// <inheritdoc cref="IEnumerator.Reset" />
        public void Reset()
        {
            var encounteredBlocks = _encounteredBlocks;
            encounteredBlocks.Clear();

            var pendingBlocks = _pendingBlocks;
            pendingBlocks.Clear();

            var rootBlock = _rootBlock;

            _ = encounteredBlocks.Add(rootBlock);
            pendingBlocks.Enqueue(rootBlock);

            _currentBlock = null;
        }

        readonly object IEnumerator.Current => Current;

        readonly void IDisposable.Dispose() { }
    }
}
