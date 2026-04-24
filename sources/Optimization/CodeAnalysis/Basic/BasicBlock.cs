// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;

namespace TerraFX.Optimization.CodeAnalysis;

/// <summary>Defines methods for working with <see cref="BasicBlock{T}"/> instances.</summary>
public static class BasicBlock
{
    /// <summary>Creates a new <see cref="BasicBlock{T}"/> instance.</summary>
    /// <typeparam name="T">The type of the statements in the basic block.</typeparam>
    /// <param name="id">The identifier for the basic block.</param>
    /// <param name="firstStatement">The first statement in the basic block.</param>
    /// <returns>A new <see cref="BasicBlock{T}"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="firstStatement"/> is <c>null</c>.</exception>
    public static BasicBlock<T> Create<T>(int id, Statement<T> firstStatement)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);
        ArgumentNullException.ThrowIfNull(firstStatement);
        return new BasicBlock<T>(id, firstStatement);
    }

    /// <summary>Creates a new <see cref="BasicBlock{T}"/> instance with <see cref="BasicBlock{T}.Id" /> set to <c>0</c> and <see cref="BasicBlock{T}.FirstStatement" /> set to <c>null</c>.</summary>
    /// <typeparam name="T">The type of the statements in the basic block.</typeparam>
    /// <returns>A new <see cref="BasicBlock{T}"/> instance.</returns>
    /// <remarks>This method is unsafe because it creates an invalid basic block which may cause undefined behavior if used prior to proper initialization.</remarks>
    public static BasicBlock<T> CreateUnsafe<T>()
    {
        return new BasicBlock<T>(
            id: 0,
            firstStatement: null!
        );
    }
}
