using System.Collections.Generic;
using System.Reflection;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge
{
    public static class OpcodeNameLookup
    {
        private static readonly Dictionary<ushort, string> Names = ReadNames();

        public static string? Find(ushort opcode)
        {
            return Names.TryGetValue(opcode, out var name) ? name : null;
        }

        private static Dictionary<ushort, string> ReadNames()
        {
            var names = new Dictionary<ushort, string>();

            foreach (var field in typeof(Opcode).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(ushort))
                {
                    names.TryAdd((ushort)field.GetRawConstantValue()!, field.Name);
                }
            }

            return names;
        }
    }
}
