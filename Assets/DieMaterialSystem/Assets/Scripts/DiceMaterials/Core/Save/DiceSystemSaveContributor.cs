using System;

namespace DiceMaterials
{
    public sealed class DiceSystemSaveContributor : ISaveContributor
    {
        private readonly IDiceRollSystem system;

        public DiceSystemSaveContributor(IDiceRollSystem system)
        {
            this.system = system ?? throw new ArgumentNullException(nameof(system));
        }

        public void Capture(GameSaveData data) => data.dice = system.CaptureState();

        public void Restore(GameSaveData data)
        {
            if (data.dice != null) system.Restore(data.dice);
        }
    }
}
