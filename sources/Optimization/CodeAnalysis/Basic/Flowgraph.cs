// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using static TerraFX.Optimization.Utilities.ExceptionUtilities;

namespace TerraFX.Optimization.CodeAnalysis;

/// <summary>Defines methods for working with <see cref="Flowgraph{T}"/> instances.</summary>
public static class Flowgraph
{
    /// <summary>Creates a new <see cref="Flowgraph{T}"/> instance.</summary>
    /// <typeparam name="T">The type of the basic blocks in the flowgraph.</typeparam>
    /// <param name="firstBlock">The first block in the flowgraph.</param>
    /// <returns>A new <see cref="Flowgraph{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="firstBlock"/> is <c>null</c>.</exception>
    public static Flowgraph<T> Create<T>(BasicBlock<T> firstBlock)
    {
        ArgumentNullException.ThrowIfNull(firstBlock);
        return new Flowgraph<T>(firstBlock);
    }

    /// <summary>Creates a new <see cref="Flowgraph{T}"/> instance with <see cref="Flowgraph{T}.FirstBlock" /> set to <c>null</c>.</summary>
    /// <typeparam name="T">The type of the basic blocks in the flowgraph.</typeparam>
    /// <returns>A new <see cref="Flowgraph{T}"/> instance.</returns>
    /// <remarks>This method is unsafe because it creates an invalid flowgraph which may cause undefined behavior if used prior to proper initialization.</remarks>
    public static Flowgraph<T> CreateUnsafe<T>()
    {
        return new Flowgraph<T>(
            firstBlock: null!
        );
    }

    /// <summary>Decodes a method body into a flowgraph.</summary>
    /// <typeparam name="T">The type of the values in the statements for the flowgraph.</typeparam>
    /// <param name="metadataReader">The metadata reader.</param>
    /// <param name="methodBody">The method body block.</param>
    /// <param name="valueFactory">A factory function to create values for the statements.</param>
    /// <returns>A flowgraph representing the method body.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metadataReader"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="methodBody"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is <c>null</c>.</exception>
    public static Flowgraph<T> Decode<T>(MetadataReader metadataReader, MethodBodyBlock methodBody, Func<Instruction, T> valueFactory)
    {
        ArgumentNullException.ThrowIfNull(metadataReader);
        ArgumentNullException.ThrowIfNull(methodBody);
        ArgumentNullException.ThrowIfNull(valueFactory);

        // This is a single pass algorithm which decodes the IL instructions and
        // constructs the flowgraph at the same time. When encountering a forward
        // branch we pre-emptively create the target block and insert it into the
        // sequence. This is fine because we will eventually decode the first statement
        // of that block as part of the linear decoding. However, when encountering a
        // backward branch we need to search backwards for the appropriate block
        // containing the target statement and potentially split it into two blocks.
        //
        // One of the key invariants of the algorithm is that created blocks and
        // statements have their Ids set to the IL offset of the instruction they
        // represent. This allows for us to ensure correct sorting while building the
        // flowgraph and to then fix up the IDs later to be sequential when we validate
        // that the flowgraph is well-formed.

        var ilReader = methodBody.GetILReader();

        var firstInstruction = Instruction.Decode(metadataReader, ref ilReader);
        var firstStatement = Statement.Create(0, valueFactory(firstInstruction));
        var firstBlock = BasicBlock.Create(0, firstStatement);

        var previousStatement = firstStatement;
        var currentBlock = firstBlock;
        var nextOffset = ilReader.Offset;

        while (ilReader.RemainingBytes != 0)
        {
            var offset = nextOffset;

            var instruction = Instruction.Decode(metadataReader, ref ilReader);
            nextOffset = ilReader.Offset;

            var currentStatement = Statement.Create(offset, valueFactory(instruction));

            if (!TryGetNextBlock(offset, currentBlock, out var nextBlock))
            {
                // We are continuing in the same block, so continue the current
                // sequence by inserting ourselves after the previosu statement.
                currentStatement.InsertAfter(previousStatement);
            }
            else
            {
                // We are the first statement of a new block, so we should have
                // terminated the last sequence and need to start a new one.

                Debug.Assert(nextBlock.FirstStatement is null);
                Debug.Assert(previousStatement.NextStatement is null);

                currentBlock = nextBlock;
                currentBlock.FirstStatement = currentStatement;
            }

            var operand = instruction.Operand;

            switch (operand.Kind)
            {
                case OperandKind.InlineBrTarget:
                {
                    Debug.Assert(operand.Value is int, "Expected a 4-byte signed branch target.");
                    var targetOffset = nextOffset + (int)operand.Value;
                    ProcessBranchTarget(targetOffset, currentBlock);
                    EnsureNextBlockExistsForBranch(nextOffset, currentBlock, ilReader.RemainingBytes, instruction.Opcode.ControlFlow);
                    break;
                }

                case OperandKind.InlineSwitch:
                {
                    Debug.Assert(operand.Value is int[], "Expected an array of 4-byte signed branch targets.");
                    var targets = (int[])operand.Value;

                    for (var i = 0; i < targets.Length; i++)
                    {
                        var targetOffset = nextOffset + targets[i];
                        ProcessBranchTarget(targetOffset, currentBlock);
                    }
                    EnsureNextBlockExistsForBranch(nextOffset, currentBlock, ilReader.RemainingBytes, instruction.Opcode.ControlFlow);
                    break;
                }

                case OperandKind.ShortInlineBrTarget:
                {
                    Debug.Assert(operand.Value is sbyte, "Expected a 1-byte signed branch target.");
                    var targetOffset = nextOffset + (sbyte)operand.Value;
                    ProcessBranchTarget(targetOffset, currentBlock);
                    EnsureNextBlockExistsForBranch(nextOffset, currentBlock, ilReader.RemainingBytes, instruction.Opcode.ControlFlow);
                    break;
                }

                case OperandKind.InlineNone:
                case OperandKind.InlineField:
                case OperandKind.InlineI:
                case OperandKind.InlineI8:
                case OperandKind.InlineMethod:
                case OperandKind.InlineR:
                case OperandKind.InlineSig:
                case OperandKind.InlineString:
                case OperandKind.InlineTok:
                case OperandKind.InlineType:
                case OperandKind.InlineVar:
                case OperandKind.ShortInlineI:
                case OperandKind.ShortInlineR:
                case OperandKind.ShortInlineVar:
                {
                    // Nothing to handle
                    break;
                }

                default:
                {
                    ThrowUnreachableException();
                    break;
                }
            }

            previousStatement = currentStatement;
        }

        var flowgraph = Create(firstBlock);
        {
            var blockId = 0;

            for (var block = firstBlock; block is not null; block = block.NextBlock)
            {
                block.Id = blockId++;
            }

            var statementId = 0;

            for (var statement = firstStatement; statement is not null; statement = statement.NextStatement)
            {
                statement.Id = statementId++;
            }
        }
        return flowgraph;

        static void EnsureNextBlockExistsForBranch(int targetOffset, BasicBlock<T> currentBlock, int remainingBytes, ControlFlowKind controlFlow)
        {
            if (remainingBytes != 0)
            {
                var nextBlock = currentBlock.NextBlock;

                if ((nextBlock is null) || (nextBlock.Id != targetOffset))
                {
                    nextBlock = BasicBlock.CreateUnsafe<T>();
                    nextBlock.Id = targetOffset;
                    nextBlock.InsertAfter(currentBlock);
                }

                if (controlFlow == ControlFlowKind.Cond_branch)
                {
                    currentBlock.AddChildBlock(nextBlock);
                }
            }
            else if (controlFlow == ControlFlowKind.Cond_branch)
            {
                // We have a conditional branch, which means we can
                // fallthrough into the next block, but we don't have
                // any more instructions to decode.
                ThrowForInvalidBranchTarget(targetOffset);
            }
        }

        static bool TryGetNextBlock(int targetOffset, BasicBlock<T> currentBlock, [NotNullWhen(true)] out BasicBlock<T>? nextBlock)
        {
            var candidateBlock = currentBlock.NextBlock;
            var foundBlock = false;

            if ((candidateBlock is not null) && (candidateBlock.Id == targetOffset))
            {
                nextBlock = candidateBlock;
                foundBlock = true;
            }
            else
            {
                nextBlock = null;
            }

            return foundBlock;
        }

        static void ProcessBranchTarget(int targetOffset, BasicBlock<T> currentBlock)
        {
            var candidateBlock = currentBlock;

            if (currentBlock.Id < targetOffset)
            {
                // We are branching forward, so search forward for the target block and create it
                // if it doesn't exist. We shouldn't have any forward blocks that have statements
                // in them at this point since we won't have decoded that IL yet.

                BasicBlock<T> previousCandidateBlock;

                do
                {
                    previousCandidateBlock = candidateBlock;
                    candidateBlock = candidateBlock.NextBlock;
                }
                while ((candidateBlock is not null) && (candidateBlock.Id < targetOffset));

                if ((candidateBlock is null) || (candidateBlock.Id > targetOffset))
                {
                    candidateBlock = BasicBlock.CreateUnsafe<T>();
                    candidateBlock.Id = targetOffset;
                    candidateBlock.InsertAfter(previousCandidateBlock);
                }
            }
            else if (currentBlock.Id > targetOffset)
            {
                // We are branching backward, so search backwards for the target block and split it
                // if necessary. We will have already decoded the IL for all these blocks, so they
                // should all be well-formed. However, we may have an invalid target and the statement
                // won't be locatable, so we need to throw in that case.

                do
                {
                    candidateBlock = candidateBlock.PreviousBlock;
                }
                while ((candidateBlock is not null) && (candidateBlock.Id > targetOffset));

                Debug.Assert(candidateBlock is not null);

                if (candidateBlock.Id != targetOffset)
                {
                    Debug.Assert(candidateBlock.Id < targetOffset);

                    var candidateStatement = candidateBlock.FirstStatement;
                    Statement<T> previousCandidateStatement;

                    do
                    {
                        previousCandidateStatement = candidateStatement;
                        candidateStatement = candidateStatement.NextStatement;
                    }
                    while ((candidateStatement is not null) && (candidateStatement.Id < targetOffset));

                    if ((candidateStatement is not null) && (candidateStatement.Id == targetOffset))
                    {
                        var targetBlock = BasicBlock.CreateUnsafe<T>();
                        targetBlock.Id = targetOffset;
                        targetBlock.InsertAfter(candidateBlock);

                        previousCandidateStatement.NextStatement = null;
                        targetBlock.FirstStatement = candidateStatement;
                        candidateStatement.PreviousStatement = null;
                    }
                    else
                    {
                        ThrowForInvalidBranchTarget(targetOffset);
                    }
                }
            }

            currentBlock.AddChildBlock(candidateBlock);
        }
    }
}
