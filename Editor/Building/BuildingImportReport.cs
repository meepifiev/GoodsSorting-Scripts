using System.Collections.Generic;
using System.Text;

namespace _Project.Editor.Building
{
    public class BuildingImportReport
    {
        private readonly List<string> _warnings = new List<string>();
        private readonly List<string> _ambiguousItems = new List<string>();
        private readonly Dictionary<string, int> _counters = new Dictionary<string, int>();

        public IReadOnlyList<string> Warnings => _warnings;
        public IReadOnlyList<string> AmbiguousItems => _ambiguousItems;

        public void Warn(string message)
        {
            _warnings.Add(message);
        }

        public void AddAmbiguousItem(string description)
        {
            _ambiguousItems.Add(description);
        }

        public void Count(string counter, int amount = 1)
        {
            _counters.TryGetValue(counter, out int current);
            _counters[counter] = current + amount;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("[Building import] done.");

            foreach (KeyValuePair<string, int> pair in _counters)
            {
                builder.AppendLine("  " + pair.Key + ": " + pair.Value);
            }

            builder.AppendLine("  ambiguous item -> prefab mappings: " + _ambiguousItems.Count);

            foreach (string item in _ambiguousItems)
            {
                builder.AppendLine("    " + item);
            }

            builder.AppendLine("  warnings: " + _warnings.Count);

            foreach (string warning in _warnings)
            {
                builder.AppendLine("    " + warning);
            }

            return builder.ToString();
        }
    }
}
