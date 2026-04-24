// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace TerraFX.Optimization.CodeAnalysis;

public partial class BasicBlock<T>
{
    /// <summary>Provides breadth-first enumeration of <see cref="BasicBlock{T}"/> instances starting from a root block.</summary>
    public readonly struct BreadthFirstEnumerable : IEnumerable<BasicBlock<T>>
    {
        private readonly BasicBlock<T> _rootBlock;

        /// <summary>Creates a new <see cref="BreadthFirstEnumerable" /> instance.</summary>
        /// <param name="rootBlock">The root block from which to start the breadth-first traversal.</param>
        public BreadthFirstEnumerable(BasicBlock<T> rootBlock)
        {
            ArgumentNullException.ThrowIfNull(rootBlock);
            _rootBlock = rootBlock;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public BreadthFirstEnumerator GetEnumerator() => new BreadthFirstEnumerator(_rootBlock);

        IEnumerator<BasicBlock<T>> IEnumerable<BasicBlock<T>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
