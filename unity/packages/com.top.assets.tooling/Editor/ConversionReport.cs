using System;
using System.Collections.Generic;
using Top.Assets.Conversion.Pipeline;
using Top.Logging;

namespace Top.Assets.Tooling
{
    /// <summary>
    /// What the importer says when it is done: how one unit came out, or how
    /// a whole batch did.
    /// </summary>
    internal static class ConversionReport
    {
        internal static void Unit(string name, ConversionOutcome outcome)
        {
            Log.Info($"{name}: {outcome.ToString().ToLowerInvariant()}");
        }

        /// <summary>
        /// Realizes every unit a batch converts, as it converts them, then
        /// reports the totals.
        /// </summary>
        internal static void Batch<TResult>(string units, IEnumerable<TResult> results,
            Action<TResult> realize) where TResult : UnitResult
        {
            int converted = 0, skipped = 0, failed = 0;

            foreach (var result in results)
            {
                realize(result);

                switch (result.Outcome)
                {
                    case ConversionOutcome.Converted:
                        converted++;
                        break;

                    case ConversionOutcome.Skipped:
                        skipped++;
                        break;

                    default:
                        failed++;
                        break;
                }
            }

            Log.Info($"{units}: {converted} converted, {skipped} skipped, {failed} failed");
        }
    }
}
