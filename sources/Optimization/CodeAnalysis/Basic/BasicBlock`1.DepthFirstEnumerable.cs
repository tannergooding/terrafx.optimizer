// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace TerraFX.Optimization.CodeAnalysis;

public partial class BasicBlock<T>
{
    /// <summary>Provides depth-first enumeration of <see cref="BasicBlock{T}"/> instances starting from a root block.</summary>
    public readonly struct DepthFirstEnumerable : IEnumerable<BasicBlock<T>>
    {
        private readonly BasicBlock<T> _rootBlock;

        /// <summary>Creates a new <see cref="DepthFirstEnumerable" /> instance.</summary>
        /// <param name="rootBlock">The root block from which to start the depth-first traversal.</param>
        public DepthFirstEnumerable(BasicBlock<T> rootBlock)
        {
            ArgumentNullException.ThrowIfNull(rootBlock);
            _rootBlock = rootBlock;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public DepthFirstEnumerator GetEnumerator() => new DepthFirstEnumerator(_rootBlock);

        IEnumerator<BasicBlock<T>> IEnumerable<BasicBlock<T>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
