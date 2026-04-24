// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TerraFX.Optimization.CodeAnalysis;

public partial class Flowgraph<T>
{
    /// <summary>Enumerates <see cref="BasicBlock{T}"/> instances in a flowgraph.</summary>
    public struct Enumerator : IEnumerator<BasicBlock<T>>
    {
        private readonly BasicBlock<T> _firstBlock;
        private BasicBlock<T>? _currentBlock;

        /// <summary>Creates a new <see cref="Enumerator" /> instance.</summary>
        /// <param name="firstBlock">The first block in the flowgraph.</param>
        public Enumerator(BasicBlock<T> firstBlock)
        {
            ArgumentNullException.ThrowIfNull(firstBlock);
            _firstBlock = firstBlock;
            _currentBlock = null;
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
            var currentBlock = _currentBlock;

            if (currentBlock is not null)
            {
                currentBlock = currentBlock.NextBlock;
            }
            else
            {
                currentBlock = _firstBlock;
            }

            var succeeded = false;

            if (currentBlock is not null)
            {
                _currentBlock = currentBlock;
                Debug.Assert(Current is not null);
                succeeded = true;
            }

            return succeeded;
        }

        /// <inheritdoc cref="IEnumerator.Reset" />
        public void Reset()
        {
            _currentBlock = null;
        }

        readonly object IEnumerator.Current => Current;

        readonly void IDisposable.Dispose() { }
    }
}
