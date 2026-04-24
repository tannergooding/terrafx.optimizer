// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TerraFX.Optimization.CodeAnalysis;

public partial class BasicBlock<T>
{
    /// <summary>Enumerates <see cref="Statement{T}"/> instances in a basic block.</summary>
    public struct Enumerator : IEnumerator<Statement<T>>
    {
        private readonly Statement<T> _firstStatement;
        private Statement<T>? _currentStatement;

        /// <summary>Creates a new <see cref="Enumerator" /> instance.</summary>
        /// <param name="firstStatement">The first statement in the block.</param>
        public Enumerator(Statement<T> firstStatement)
        {
            ArgumentNullException.ThrowIfNull(firstStatement);
            _firstStatement = firstStatement;
            _currentStatement = null;
        }

        /// <inheritdoc cref="IEnumerator{T}.Current" />
        public readonly Statement<T> Current
        {
            get
            {
                var currentStatement = _currentStatement;
                Debug.Assert(currentStatement is not null);
                return currentStatement;
            }
        }

        /// <inheritdoc cref="IEnumerator.MoveNext" />
        [MemberNotNullWhen(true, nameof(Current))]
        public bool MoveNext()
        {
            var currentStatement = _currentStatement;

            if (currentStatement is not null)
            {
                currentStatement = currentStatement.NextStatement;
            }
            else
            {
                currentStatement = _firstStatement;
            }

            var succeeded = false;

            if (currentStatement is not null)
            {
                _currentStatement = currentStatement;
                Debug.Assert(Current is not null);
                succeeded = true;
            }

            return succeeded;
        }

        /// <inheritdoc cref="IEnumerator.Reset" />
        public void Reset()
        {
            _currentStatement = null;
        }

        readonly object IEnumerator.Current => Current;

        readonly void IDisposable.Dispose() { }
    }
}
