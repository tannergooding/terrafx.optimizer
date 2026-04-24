// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using static TerraFX.Optimization.Utilities.ExceptionUtilities;

namespace TerraFX.Optimization.CodeAnalysis;

public readonly struct Instruction : IEquatable<Instruction>
{
    private readonly Opcode _opcode;
    private readonly Operand _operand;

    public Instruction(Opcode opcode, Operand operand)
    {
        _opcode = opcode;
        _operand = operand;
    }

    public int Length => _opcode.EncodingLength + _operand.Size;

    public Opcode Opcode => _opcode;

    public Operand Operand => _operand;

    public static bool operator ==(Instruction left, Instruction right)
        => (left._opcode == right._opcode)
        && (left._operand == right._operand);

    public static bool operator !=(Instruction left, Instruction right) => !(left == right);

    public static Instruction Decode(MetadataReader metadataReader, ref BlobReader ilReader)
    {
        int opcodeEncoding = ilReader.ReadByte();

        if (opcodeEncoding == 0xFE)
        {
            opcodeEncoding <<= 8;
            opcodeEncoding += ilReader.ReadByte();
        }

        var opcodeKind = (OpcodeKind)opcodeEncoding;
        var opcode = Opcode.Create(opcodeKind);

        var operandKind = opcode.OperandKind;
        var operandValue = (object?)null;

        switch (operandKind)
        {
            case OperandKind.InlineNone:
            {
                break;
            }

            case OperandKind.InlineBrTarget:
            case OperandKind.InlineI:
            {
                operandValue = ilReader.ReadInt32();
                break;
            }

            case OperandKind.InlineField:
            case OperandKind.InlineMethod:
            case OperandKind.InlineTok:
            case OperandKind.InlineType:
            {
                var token = ilReader.ReadInt32();
                operandValue = MetadataTokens.EntityHandle(token);
                break;
            }

            case OperandKind.InlineI8:
            {
                operandValue = ilReader.ReadInt64();
                break;
            }

            case OperandKind.InlineR:
            {
                operandValue = ilReader.ReadDouble();
                break;
            }

            case OperandKind.InlineSig:
            {
                var rowNumber = ilReader.ReadInt32();
                operandValue = MetadataTokens.StandaloneSignatureHandle(rowNumber);
                break;
            }

            case OperandKind.InlineString:
            {
                var offset = ilReader.ReadInt32();
                operandValue = MetadataTokens.UserStringHandle(offset);
                break;
            }

            case OperandKind.InlineSwitch:
            {
                var count = ilReader.ReadUInt32();
                var targets = new int[count];

                for (var i = 0; i < count; i++)
                {
                    targets[i] = ilReader.ReadInt32();
                }

                operandValue = targets;
                break;
            }

            case OperandKind.InlineVar:
            {
                operandValue = ilReader.ReadInt16();
                break;
            }

            case OperandKind.ShortInlineBrTarget:
            case OperandKind.ShortInlineI:
            case OperandKind.ShortInlineVar:
            {
                operandValue = ilReader.ReadSByte();
                break;
            }

            case OperandKind.ShortInlineR:
            {
                operandValue = ilReader.ReadSingle();
                break;
            }

            default:
            {
                ThrowForInvalidKind(opcode.OperandKind);
                break;
            }
        }

        var operand = new Operand(metadataReader, operandKind, operandValue);
        return new Instruction(opcode, operand);
    }

    public readonly bool Equals(Instruction other) => this == other;

    public override readonly bool Equals(object? obj) => (obj is Instruction other) && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(_opcode, _operand);

    public override readonly string ToString()
    {
        var builder = new StringBuilder();

        var opcodeName = _opcode.Name;
        _ = builder.Append(opcodeName);

        var operand = _operand.ToString();

        if (!string.IsNullOrEmpty(operand))
        {
            _ = builder.Append(' ', 16 - opcodeName.Length);
            _ = builder.Append(operand);
        }

        return builder.ToString();
    }
}
