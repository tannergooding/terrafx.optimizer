// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;

namespace TerraFX.Optimization.CodeAnalysis;

/// <summary>Defines methods for working with <see cref="Statement{T}"/> instances.</summary>
public static class Statement
{
    /// <summary>Creates a new <see cref="Statement{T}"/> instance.</summary>
    /// <typeparam name="T">The type of the value in the statement.</typeparam>
    /// <param name="id">The identifier for the statement.</param>
    /// <param name="value">The value of the statement.</param>
    /// <returns>A new <see cref="Statement{T}"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is negative.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static Statement<T> Create<T>(int id, T value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id);

        if (default(T) is null)
        {
            ArgumentNullException.ThrowIfNull(value);
        }
        return new Statement<T>(id, value);
    }

    /// <summary>Creates a new <see cref="Statement{T}"/> instance with <see cref="Statement{T}.Id" /> set to <c>0</c> and <see cref="Statement{T}.Value" /> set to <c>default</c>.</summary>
    /// <typeparam name="T">The type of the value in the statement.</typeparam>
    /// <returns>A new <see cref="Statement{T}"/> instance.</returns>
    /// <remarks>This method is unsafe because it creates an invalid statement which may cause undefined behavior if used prior to proper initialization.</remarks>
    public static Statement<T> CreateUnsafe<T>()
    {
        return new Statement<T>(
            id: 0,
            value: default!
        );
    }
}
