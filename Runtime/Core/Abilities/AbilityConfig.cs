namespace _Project.Core.Abilities
{
    public readonly struct AbilityConfig
    {
        public readonly AbilityType Type;
        public readonly int InitialCount;
        public readonly bool FreeWhenEmpty;
        public readonly int UnlockLevel;

        public AbilityConfig(AbilityType type, int initialCount, bool freeWhenEmpty, int unlockLevel)
        {
            Type = type;
            InitialCount = initialCount;
            FreeWhenEmpty = freeWhenEmpty;
            UnlockLevel = unlockLevel;
        }
    }
}
