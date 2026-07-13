using System.Reflection;
using System.Reflection.Emit;
using Terraria;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Resolvers;

public static class JournalLegacyDirectDropAnalyzer
{
    private static readonly Lazy<Catalog> Entries = new(CreateCatalog);
    private static readonly OpCode[] OneByteOpCodes = new OpCode[0x100];
    private static readonly OpCode[] TwoByteOpCodes = new OpCode[0x100];

    static JournalLegacyDirectDropAnalyzer()
    {
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opCode)
            {
                continue;
            }

            var value = unchecked((ushort)opCode.Value);
            if (value < 0x100)
            {
                OneByteOpCodes[value] = opCode;
                continue;
            }

            if ((value & 0xff00) == 0xfe00)
            {
                TwoByteOpCodes[value & 0xff] = opCode;
            }
        }
    }

    public static IReadOnlyList<JournalLegacyNpcDrop> GetNpcDrops(int targetItemId)
    {
        return Entries.Value.NpcDrops
            .Where(entry => entry.TargetItemId == targetItemId)
            .ToArray();
    }

    public static IReadOnlyList<JournalLegacyNpcDrop> GetAllNpcDrops() => Entries.Value.NpcDrops;

    public static IReadOnlyList<JournalLegacyItemDrop> GetItemDrops(int targetItemId)
    {
        return Entries.Value.ItemDrops
            .Where(entry => entry.TargetItemId == targetItemId)
            .ToArray();
    }

    public static IReadOnlyList<JournalLegacyItemDrop> GetAllItemDrops() => Entries.Value.ItemDrops;

    internal static MemberInfo[] GetReferencedMembers(MethodInfo method)
    {
        try
        {
            return ReadInstructions(method)
                .Select(static instruction => instruction.Operand)
                .OfType<MemberInfo>()
                .Distinct()
                .ToArray();
        }
        catch (Exception exception)
        {
            ProgressionJournal.Instance?.Logger.Debug(
                $"Failed to inspect referenced members in '{method.DeclaringType?.FullName}.{method.Name}'."
                + $"{Environment.NewLine}{exception}");
            return [];
        }
    }

    private static Catalog CreateCatalog()
    {
        var itemTypes = ModContent.GetContent<ModItem>()
            .GroupBy(static item => item.GetType())
            .ToDictionary(static group => group.Key, static group => group.First().Type);
        List<JournalLegacyNpcDrop> npcDrops = [];
        List<JournalLegacyItemDrop> itemDrops = [];

        foreach (var npc in ModContent.GetContent<ModNPC>())
        {
            AnalyzeSourceMethods(
                npc.GetType(),
                ["OnKill", "NPCLoot", "OnChatButtonClicked"],
                itemTypes,
                drop => npcDrops.Add(new JournalLegacyNpcDrop(
                    npc.Type,
                    drop.TargetItemId,
                    drop.DropRate,
                    drop.StackMin,
                    drop.StackMax,
                    drop.SourceMethod)));
        }

        foreach (var item in ModContent.GetContent<ModItem>())
        {
            AnalyzeSourceMethods(
                item.GetType(),
                ["RightClick", "OpenBossBag", "PostUpdate"],
                itemTypes,
                drop => itemDrops.Add(new JournalLegacyItemDrop(
                    item.Type,
                    drop.TargetItemId,
                    drop.DropRate,
                    drop.StackMin,
                    drop.StackMax,
                    drop.SourceMethod)));
        }

        return new Catalog(
            DeduplicateNpcDrops(npcDrops),
            DeduplicateItemDrops(itemDrops));
    }

    private static JournalLegacyNpcDrop[] DeduplicateNpcDrops(List<JournalLegacyNpcDrop> drops)
    {
        return drops
            .GroupBy(static drop => new
            {
                drop.SourceNpcType,
                drop.TargetItemId,
                drop.StackMin,
                drop.StackMax
            })
            .Select(static group => group
                .OrderByDescending(static drop => drop.DropRate)
                .First())
            .ToArray();
    }

    private static JournalLegacyItemDrop[] DeduplicateItemDrops(List<JournalLegacyItemDrop> drops)
    {
        return drops
            .Where(static drop => drop.SourceItemId != drop.TargetItemId)
            .GroupBy(static drop => new
            {
                drop.SourceItemId,
                drop.TargetItemId,
                drop.StackMin,
                drop.StackMax
            })
            .Select(static group => group
                .OrderByDescending(static drop => drop.DropRate)
                .First())
            .ToArray();
    }

    private static void AnalyzeSourceMethods(
        Type sourceType,
        string[] methodNames,
        Dictionary<Type, int> itemTypes,
        Action<AnalyzedDrop> append)
    {
        foreach (var method in sourceType.GetMethods(
                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (!methodNames.Contains(method.Name, StringComparer.Ordinal))
            {
                continue;
            }

            try
            {
                AnalyzeMethod(method, itemTypes, append);
            }
            catch (Exception exception)
            {
                ProgressionJournal.Instance?.Logger.Debug(
                    $"Failed to inspect legacy direct drops in '{sourceType.FullName}.{method.Name}'.{Environment.NewLine}{exception}");
            }
        }
    }

    private static void AnalyzeMethod(
        MethodInfo method,
        Dictionary<Type, int> itemTypes,
        Action<AnalyzedDrop> append)
    {
        var instructions = ReadInstructions(method);
        for (var spawnIndex = 0; spawnIndex < instructions.Length; spawnIndex++)
        {
            if (!IsItemSpawnCall(instructions[spawnIndex].Operand as MethodBase))
            {
                continue;
            }

            var itemTypeIndex = FindTargetItemTypeInstruction(instructions, spawnIndex, itemTypes, out var targetItemId);
            if (itemTypeIndex < 0)
            {
                continue;
            }

            var stack = ResolveStack(instructions, itemTypeIndex, spawnIndex);
            var guard = ResolveGuard(instructions, itemTypeIndex, spawnIndex);
            if (!guard.Known || stack.PositiveProbability <= 0f)
            {
                continue;
            }

            var dropRate = guard.Rate;
            if (stack.PositiveProbability < 1f && dropRate >= 0f)
            {
                dropRate *= stack.PositiveProbability;
            }

            append(new AnalyzedDrop(
                targetItemId,
                dropRate,
                stack.Min,
                stack.Max,
                $"{method.DeclaringType?.FullName}.{method.Name}"));
        }
    }

    private static int FindTargetItemTypeInstruction(
        IlInstruction[] instructions,
        int spawnIndex,
        Dictionary<Type, int> itemTypes,
        out int targetItemId)
    {
        targetItemId = 0;
        var startIndex = Math.Max(0, spawnIndex - 96);
        for (var index = spawnIndex - 1; index >= startIndex; index--)
        {
            if (!TryGetModItemType(instructions[index].Operand as MethodBase, out var itemType)
                || !itemTypes.TryGetValue(itemType, out targetItemId))
            {
                continue;
            }

            return index;
        }

        return -1;
    }

    private static StackRange ResolveStack(IlInstruction[] instructions, int itemTypeIndex, int spawnIndex)
    {
        for (var index = itemTypeIndex + 1; index < spawnIndex; index++)
        {
            if (instructions[index].Operand is not MethodBase method
                || !string.Equals(method.Name, "Next", StringComparison.Ordinal)
                || !string.Equals(method.DeclaringType?.FullName, "Terraria.Utilities.UnifiedRandom", StringComparison.Ordinal))
            {
                continue;
            }

            var parameterCount = method.GetParameters().Length;
            if (parameterCount == 1
                && TryGetPreviousIntConstant(instructions, index, 1, out var maximum)
                && maximum > 0)
            {
                var positiveValues = Math.Max(0, maximum - 1);
                return positiveValues == 0
                    ? new StackRange(1, 1, 0f)
                    : new StackRange(1, maximum - 1, positiveValues / (float)maximum);
            }

            if (parameterCount != 2
                || !TryGetPreviousIntConstant(instructions, index, 2, out var minimum)
                || !TryGetPreviousIntConstant(instructions, index, 1, out maximum)
                || maximum <= minimum) continue;
            {
                var positiveMinimum = Math.Max(1, minimum);
                var positiveMaximum = maximum - 1;
                var positiveValues = Math.Max(0, positiveMaximum - positiveMinimum + 1);
                var totalValues = maximum - minimum;
                return positiveValues == 0
                    ? new StackRange(1, 1, 0f)
                    : new StackRange(
                        positiveMinimum,
                        positiveMaximum,
                        positiveValues / (float)totalValues);
            }
        }

        if (TryGetNextIntConstant(instructions, itemTypeIndex, spawnIndex, out var stack)
            && stack > 0)
        {
            return new StackRange(stack, stack, 1f);
        }

        return new StackRange(1, 1, 1f);
    }

    private static GuardRate ResolveGuard(IlInstruction[] instructions, int itemTypeIndex, int spawnIndex)
    {
        var spawnOffset = instructions[spawnIndex].Offset;
        var dynamicCondition = false;
        var rate = 1f;
        var startIndex = Math.Max(0, itemTypeIndex - 48);
        for (var index = startIndex; index < itemTypeIndex; index++)
        {
            if (!IsConditionalBranch(instructions[index].OpCode)
                || instructions[index].Operand is not int targetOffset
                || targetOffset <= spawnOffset)
            {
                continue;
            }

            if (!TryResolveNextBoolProbability(instructions, index, out var predicateProbability))
            {
                dynamicCondition = true;
                continue;
            }

            rate *= IsBranchWhenFalse(instructions[index].OpCode)
                ? predicateProbability
                : 1f - predicateProbability;
        }

        return dynamicCondition
            ? new GuardRate(false, 0f)
            : new GuardRate(true, Math.Clamp(rate, 0f, 1f));
    }

    private static bool TryResolveNextBoolProbability(
        IlInstruction[] instructions,
        int branchIndex,
        out float probability)
    {
        probability = 0f;
        var callIndex = branchIndex - 1;
        if (callIndex < 0
            || instructions[callIndex].Operand is not MethodBase method
            || !string.Equals(method.Name, "NextBool", StringComparison.Ordinal))
        {
            return false;
        }

        var parameterCount = method.GetParameters()
            .Count(static parameter => parameter.ParameterType.FullName != "Terraria.Utilities.UnifiedRandom");
        if (parameterCount == 0)
        {
            probability = 0.5f;
            return true;
        }

        if (parameterCount == 1
            && TryGetPreviousIntConstant(instructions, callIndex, 1, out var denominator)
            && denominator > 0)
        {
            probability = 1f / denominator;
            return true;
        }

        if (parameterCount != 2
            || !TryGetPreviousIntConstant(instructions, callIndex, 2, out var numerator)
            || !TryGetPreviousIntConstant(instructions, callIndex, 1, out denominator)
            || denominator <= 0) return false;
        probability = Math.Clamp(numerator / (float)denominator, 0f, 1f);
        return true;

    }

    private static bool TryGetPreviousIntConstant(
        IlInstruction[] instructions,
        int beforeIndex,
        int ordinalFromEnd,
        out int value)
    {
        var found = 0;
        for (var index = beforeIndex - 1; index >= Math.Max(0, beforeIndex - 8); index--)
        {
            if (!TryGetIntConstant(instructions[index], out value))
            {
                continue;
            }

            found++;
            if (found == ordinalFromEnd)
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    private static bool TryGetNextIntConstant(
        IlInstruction[] instructions,
        int afterIndex,
        int beforeIndex,
        out int value)
    {
        for (var index = afterIndex + 1; index < Math.Min(beforeIndex, afterIndex + 8); index++)
        {
            if (TryGetIntConstant(instructions[index], out value))
            {
                return true;
            }

            if (instructions[index].Operand is MethodBase)
            {
                break;
            }
        }

        value = 0;
        return false;
    }

    private static bool TryGetIntConstant(IlInstruction instruction, out int value)
    {
        if (instruction.OpCode == OpCodes.Ldc_I4_M1)
        {
            value = -1;
            return true;
        }

        if (instruction.OpCode == OpCodes.Ldc_I4_0)
        {
            value = 0;
            return true;
        }

        if (instruction.OpCode == OpCodes.Ldc_I4_1)
        {
            value = 1;
            return true;
        }

        if (instruction.OpCode == OpCodes.Ldc_I4_2)
        {
            value = 2;
            return true;
        }

        if (instruction.OpCode == OpCodes.Ldc_I4_3)
        {
            value = 3;
            return true;
        }

        if (instruction.OpCode == OpCodes.Ldc_I4_4)
        {
            value = 4;
            return true;
        }

        if (instruction.OpCode == OpCodes.Ldc_I4_5)
        {
            value = 5;
            return true;
        }

        if (instruction.OpCode == OpCodes.Ldc_I4_6)
        {
            value = 6;
            return true;
        }

        if (instruction.OpCode == OpCodes.Ldc_I4_7)
        {
            value = 7;
            return true;
        }

        if (instruction.OpCode == OpCodes.Ldc_I4_8)
        {
            value = 8;
            return true;
        }

        if (instruction.OpCode is var opCode && (opCode == OpCodes.Ldc_I4 || opCode == OpCodes.Ldc_I4_S)
                                             && instruction.Operand is int constant)
        {
            value = constant;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryGetModItemType(MethodBase? method, out Type itemType)
    {
        itemType = null!;
        if (method is not MethodInfo { IsGenericMethod: true } methodInfo
            || !string.Equals(methodInfo.Name, nameof(ModContent.ItemType), StringComparison.Ordinal)
            || methodInfo.DeclaringType != typeof(ModContent))
        {
            return false;
        }

        var genericArguments = methodInfo.GetGenericArguments();
        if (genericArguments.Length != 1
            || !typeof(ModItem).IsAssignableFrom(genericArguments[0]))
        {
            return false;
        }

        itemType = genericArguments[0];
        return true;
    }

    private static bool IsItemSpawnCall(MethodBase? method)
    {
        return method is not null
            && ((method.DeclaringType == typeof(Item)
                    && string.Equals(method.Name, nameof(Item.NewItem), StringComparison.Ordinal))
                || (method.DeclaringType == typeof(Player)
                    && method.Name.StartsWith("QuickSpawnItem", StringComparison.Ordinal))
                || IsLegacyDropHelper(method));
    }

    private static bool IsLegacyDropHelper(MethodBase method)
    {
        if (!string.Equals(method.Name, "DropLoot", StringComparison.Ordinal)
            && !string.Equals(method.Name, "DropItem", StringComparison.Ordinal))
        {
            return false;
        }

        var parameters = method.GetParameters();
        return parameters.Length >= 2
            && parameters.Count(static parameter => parameter.ParameterType == typeof(int)) >= 2;
    }

    private static bool IsConditionalBranch(OpCode opCode)
    {
        return opCode.FlowControl == FlowControl.Cond_Branch
            && opCode != OpCodes.Switch;
    }

    private static bool IsBranchWhenFalse(OpCode opCode) =>
        opCode == OpCodes.Brfalse || opCode == OpCodes.Brfalse_S;

    private static IlInstruction[] ReadInstructions(MethodInfo method)
    {
        var body = method.GetMethodBody();
        var bytes = body?.GetILAsByteArray();
        if (bytes is null || bytes.Length == 0)
        {
            return [];
        }

        var module = method.Module;
        var declaringTypeArguments = method.DeclaringType?.GetGenericArguments();
        var methodArguments = method.GetGenericArguments();
        List<IlInstruction> result = [];
        var position = 0;
        while (position < bytes.Length)
        {
            var offset = position;
            var first = bytes[position++];
            var opCode = first == 0xfe
                ? TwoByteOpCodes[bytes[position++]]
                : OneByteOpCodes[first];
            var operand = ReadOperand(
                bytes,
                ref position,
                opCode.OperandType,
                module,
                declaringTypeArguments,
                methodArguments);
            result.Add(new IlInstruction(offset, opCode, operand));
        }

        return result.ToArray();
    }

    private static object? ReadOperand(
        byte[] bytes,
        ref int position,
        OperandType operandType,
        Module module,
        Type[]? declaringTypeArguments,
        Type[]? methodArguments)
    {
        switch (operandType)
        {
            case OperandType.InlineNone:
                return null;
            case OperandType.ShortInlineI:
                return (int)(sbyte)bytes[position++];
            case OperandType.InlineI:
                return ReadInt32(bytes, ref position);
            case OperandType.InlineI8:
                var int64 = BitConverter.ToInt64(bytes, position);
                position += sizeof(long);
                return int64;
            case OperandType.ShortInlineR:
                var single = BitConverter.ToSingle(bytes, position);
                position += sizeof(float);
                return single;
            case OperandType.InlineR:
                var doubleValue = BitConverter.ToDouble(bytes, position);
                position += sizeof(double);
                return doubleValue;
            case OperandType.ShortInlineBrTarget:
                return position + 1 + (sbyte)bytes[position++];
            case OperandType.InlineBrTarget:
                var branchDelta = ReadInt32(bytes, ref position);
                return position + branchDelta;
            case OperandType.InlineSwitch:
                var count = ReadInt32(bytes, ref position);
                var baseOffset = position + count * sizeof(int);
                var targets = new int[count];
                for (var index = 0; index < count; index++)
                {
                    targets[index] = baseOffset + ReadInt32(bytes, ref position);
                }

                return targets;
            case OperandType.ShortInlineVar:
                return (int)bytes[position++];
            case OperandType.InlineVar:
                var variable = BitConverter.ToUInt16(bytes, position);
                position += sizeof(ushort);
                return (int)variable;
            case OperandType.InlineString:
                var stringToken = ReadInt32(bytes, ref position);
                return ResolveToken(() => module.ResolveString(stringToken));
            case OperandType.InlineMethod:
                var methodToken = ReadInt32(bytes, ref position);
                return ResolveToken(() => module.ResolveMethod(
                    methodToken,
                    declaringTypeArguments,
                    methodArguments));
            case OperandType.InlineField:
                var fieldToken = ReadInt32(bytes, ref position);
                return ResolveToken(() => module.ResolveField(
                    fieldToken,
                    declaringTypeArguments,
                    methodArguments));
            case OperandType.InlineType:
                var typeToken = ReadInt32(bytes, ref position);
                return ResolveToken(() => module.ResolveType(
                    typeToken,
                    declaringTypeArguments,
                    methodArguments));
            case OperandType.InlineTok:
                var memberToken = ReadInt32(bytes, ref position);
                return ResolveToken(() => module.ResolveMember(
                    memberToken,
                    declaringTypeArguments,
                    methodArguments));
            case OperandType.InlineSig:
                return ReadInt32(bytes, ref position);
            default:
                throw new InvalidOperationException($"Unsupported IL operand type: {operandType}.");
        }
    }

    private static object? ResolveToken(Func<object?> resolve)
    {
        try
        {
            return resolve();
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static int ReadInt32(byte[] bytes, ref int position)
    {
        var value = BitConverter.ToInt32(bytes, position);
        position += sizeof(int);
        return value;
    }

    private sealed record Catalog(
        JournalLegacyNpcDrop[] NpcDrops,
        JournalLegacyItemDrop[] ItemDrops);

    private sealed record IlInstruction(int Offset, OpCode OpCode, object? Operand);
    private sealed record AnalyzedDrop(
        int TargetItemId,
        float DropRate,
        int StackMin,
        int StackMax,
        string SourceMethod);
    private sealed record StackRange(int Min, int Max, float PositiveProbability);
    private sealed record GuardRate(bool Known, float Rate);
}

public sealed record JournalLegacyNpcDrop(
    int SourceNpcType,
    int TargetItemId,
    float DropRate,
    int StackMin,
    int StackMax,
    string SourceMethod);

public sealed record JournalLegacyItemDrop(
    int SourceItemId,
    int TargetItemId,
    float DropRate,
    int StackMin,
    int StackMax,
    string SourceMethod);
