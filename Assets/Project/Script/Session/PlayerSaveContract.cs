using System;
using PowerMath.PlayerData;

namespace PowerMath.Session
{
    // Runs against the raw document before defaults can hide absence or corruption.
    public static class PlayerSaveContract
    {
        public static int Inspect(JsonValue student, out bool isNewPlayer)
        {
            if (!FirestoreJsonNavigator.TryGetMapFields(student, out var fields))
                throw new FormatException("Invalid student data.");
            isNewPlayer = !fields.TryGet("gamedata", out var value);
            if (isNewPlayer) return 0;
            if (!FirestoreJsonNavigator.TryGetMapFields(value, out var game))
                throw new FormatException("Player data is not a map. Recovery is required.");
            foreach (string name in new[] { "profile", "wallet", "progression", "loadout", "activeRun", "academic", "economy", "analytics", "lastRunSettlement", "preferences", "onboarding", "tutorial" })
                if (game.TryGet(name, out var section) && !FirestoreJsonNavigator.TryGetMapFields(section, out _))
                    throw new FormatException("Invalid player section: " + name);
            if (!game.TryGet("schemaVersion", out var schema))
            {
                // Unversioned deployed saves may already contain V2 presentation receipts.
                // The additive planner preserves these fields instead of replaying destructive migrations.
                return 0;
            }
            if (!FirestoreJsonNavigator.TryReadInteger(schema, out long version) || version < 0)
                throw new FormatException("Invalid player schema version.");
            if (version > PlayerSchemaMigrator.CurrentSchemaVersion)
                throw new NotSupportedException("This save requires a newer game version.");
            return (int)version;
        }
    }
}
