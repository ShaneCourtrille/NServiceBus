namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    using ApprovalTests;
    using NUnit.Framework;

    [TestFixture]
    public class StructConventionsTests
    {
        [Test]
        public void ApproveStructsWhichDontFollowStructGuidelines()
        {
            var approvalBuilder = new StringBuilder();
            approvalBuilder.AppendLine(@"-------------------------------------------------- REMEMBER --------------------------------------------------
CONSIDER defining a struct instead of a class if instances of the type are small and commonly short-lived or are commonly embedded in other objects.

AVOID defining a struct unless the type has all of the following characteristics:
   * It logically represents a single value, similar to primitive types(int, double, etc.).
   * It has an instance size under 16 bytes.
   * It is immutable.
   * It will not have to be boxed frequently.

In all other cases, you should define your types as classes.
-------------------------------------------------- REMEMBER --------------------------------------------------
");

            var assembly = typeof(Endpoint).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsValueType || type.IsEnum || type.IsSpecialName) continue;

                var offendedRules = new List<string> { $"{type.FullName} violates the following rules:" };

                InspectSizeOfStruct(type, offendedRules);
                InspectWhetherStructReferenceReferenceTypes(type, offendedRules);

                if (offendedRules.Count <= 1) continue;
                foreach (var offendedRule in offendedRules)
                {
                    approvalBuilder.AppendLine(offendedRule);
                }
                approvalBuilder.AppendLine();
            }

            Approvals.Verify(approvalBuilder.ToString());
        }

        static void InspectWhetherStructReferenceReferenceTypes(Type type, List<string> offendedRules)
        {
            var mutabilityRules = new List<string> { "   - It is mutable. Following members are mutable:" };

            var fields = type.GetFields();
            foreach (var fieldInfo in fields)
            {
                if(fieldInfo.FieldType == typeof(string)) continue;

                if (fieldInfo.FieldType.IsClass || fieldInfo.FieldType.IsInterface)
                {
                    mutabilityRules.Add($"      - Field {fieldInfo.Name} of type { fieldInfo.FieldType } is potentially mutable.");
                }
            }

            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string)) continue;

                if (property.PropertyType.IsClass || property.PropertyType.IsInterface || property.CanWrite)
                {
                    mutabilityRules.Add($"      - Property {property.Name} of type { property.PropertyType } is potentially mutable.");
                }
            }

            if(mutabilityRules.Count > 1)
                offendedRules.AddRange(mutabilityRules);
        }

        static void InspectSizeOfStruct(Type type, List<string> offendedRules)
        {
            try
            {
                var sizeOf = Marshal.SizeOf(type);
                if (IsLargerThanSixteenBytes(sizeOf))
                {
                    offendedRules.Add($"   - With {sizeOf} bytes it is larger than 16 bytes.");
                }
            }
            catch (Exception)
            {
                offendedRules.Add($"   - The size cannot be determined. Probably this type violates all struct rules.");
            }
        }

        static bool IsLargerThanSixteenBytes(int sizeOf)
        {
            return sizeOf > 16;
        }
    }
}