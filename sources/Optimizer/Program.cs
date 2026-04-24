// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using TerraFX.Optimization.CodeAnalysis;

namespace TerraFX.Optimizer;

internal static class Program
{
    public static void Main()
    {
        using var peStream = File.OpenRead("TerraFX.Optimization.dll");
        using var peReader = new PEReader(peStream);

        var metadataReader = peReader.GetMetadataReader();

        foreach (var methodDefinitionHandle in metadataReader.MethodDefinitions)
        {
            var methodDefinitionInfo = CompilerInfo.Instance.Resolve(methodDefinitionHandle, metadataReader);
            Debug.Assert(methodDefinitionInfo is not null);

            var relativeVirtualAddress = methodDefinitionInfo.RelativeVirtualAddress;

            if (relativeVirtualAddress == 0)
            {
                continue;
            }

            Console.WriteLine(methodDefinitionInfo);

            var methodBody = peReader.GetMethodBody(relativeVirtualAddress);
            var flowgraph = Flowgraph.Decode(metadataReader, methodBody, (instruction) => instruction);
            var offset = 0;

            foreach (var block in flowgraph)
            {
                Console.WriteLine($"  // BB{block.Id:X2}");

                foreach (var statement in block)
                {
                    var instruction = statement.Value;

                    Console.Write("    ");
                    Console.WriteLine(GetDisplayString(instruction, ref offset));
                }
            }
        }
    }

    public static string GetDisplayString(Instruction instruction, ref int offset)
    {
        var builder = new StringBuilder();

        _ = builder.Append("IL_");
        _ = builder.Append(offset.ToString("X4", CultureInfo.InvariantCulture));

        _ = builder.Append(':');
        _ = builder.Append(' ', 2);

        var opcodeName = instruction.Opcode.Name;
        _ = builder.Append(opcodeName);

        var nextOffset = offset + instruction.Length;
        var operand = GetDisplayString(instruction.Operand, nextOffset);

        if (!string.IsNullOrEmpty(operand))
        {
            _ = builder.Append(' ', 16 - opcodeName.Length);
            _ = builder.Append(operand);
        }

        offset = nextOffset;
        return builder.ToString();
    }

    public static string GetDisplayString(Operand operand, int nextOffset)
    {
        operand.Value = operand.Value;
        var value = operand.Value;

        if (value is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        if (value is int[] targetOffsets)
        {
            _ = builder.Append('(');
            var lastIndex = targetOffsets.Length - 1;

            for (var i = 0; i < lastIndex; i++)
            {
                _ = AppendOffset(builder, nextOffset + targetOffsets[i]);
                _ = builder.Append(',');
                _ = builder.Append(' ');
            }

            _ = AppendOffset(builder, nextOffset + targetOffsets[lastIndex]);
            _ = builder.Append(')');
        }
        else if (value is FieldDefinitionInfo fieldDefinitionInfo)
        {
            _ = builder.Append(fieldDefinitionInfo);
        }
        else if (value is MethodDefinitionInfo methodDefinitionInfo)
        {
            _ = builder.Append(methodDefinitionInfo);
        }
        else if (value is MemberReferenceInfo memberReferenceInfo)
        {
            _ = builder.Append(memberReferenceInfo);
        }
        else if (value is MethodSpecificationInfo methodSpecificationInfo)
        {
            _ = builder.Append(methodSpecificationInfo);
        }
        else if (value is StandaloneSignatureInfo standaloneSignatureInfo)
        {
            _ = builder.Append(standaloneSignatureInfo);
        }
        else if (value is TypeDefinitionInfo typeDefinitionInfo)
        {
            _ = builder.Append(typeDefinitionInfo);
        }
        else if (value is TypeReferenceInfo typeReferenceInfo)
        {
            _ = builder.Append(typeReferenceInfo);
        }
        else if (value is TypeSpecificationInfo typeSpecificationInfo)
        {
            _ = builder.Append(typeSpecificationInfo);
        }
        else if (value is float float32Value)
        {
            _ = builder.Append(float32Value);
        }
        else if (value is double float64Value)
        {
            _ = builder.Append(float64Value);
        }
        else if (value is sbyte int8Value)
        {
            if (operand.Kind is OperandKind.ShortInlineBrTarget)
            {
                _ = AppendOffset(builder, nextOffset + int8Value);
            }
            else
            {
                _ = builder.Append(int8Value);
            }
        }
        else if (value is short int16Value)
        {
            _ = builder.Append(int16Value);
        }
        else if (value is int int32Value)
        {
            if (operand.Kind is OperandKind.InlineBrTarget)
            {
                _ = AppendOffset(builder, nextOffset + int32Value);
            }
            else
            {
                _ = builder.Append(int32Value);
            }
        }
        else if (value is long int64Value)
        {
            _ = builder.Append(int64Value);
        }
        else if (value is string stringValue)
        {
            _ = builder.Append('"');
            _ = builder.Append(stringValue);
            _ = builder.Append('"');
        }
        else
        {
            throw new UnreachableException();
        }

        return builder.ToString();

        static StringBuilder AppendOffset(StringBuilder builder, int offset)
        {
            _ = builder.Append("IL_");
            return builder.Append(offset.ToString("X4", CultureInfo.InvariantCulture));
        }
    }
}
