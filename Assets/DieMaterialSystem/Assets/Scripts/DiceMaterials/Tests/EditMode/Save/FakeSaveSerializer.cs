using System;

namespace DiceMaterials.Tests
{
    /// <summary>Serializador de texto propio: si el estado pierde un campo al guardar, los tests lo detectan.</summary>
    public sealed class FakeSaveSerializer : ISaveSerializer
    {
        public string Serialize(GameSaveData d)
        {
            string dice = d.dice == null
                ? "null"
                : string.Join("|", d.dice.version, Uri.EscapeDataString(d.dice.seedText ?? ""), d.dice.seedValue,
                              d.dice.fingerprint, d.dice.materialsDrawn, d.dice.facesDrawn, d.dice.piecesDrawn);
            return d.version + ";" + dice;
        }

        public GameSaveData Deserialize(string content)
        {
            var parts = content.Split(';');
            var data = new GameSaveData { version = int.Parse(parts[0]) };
            if (parts[1] != "null")
            {
                var f = parts[1].Split('|');
                data.dice = new DiceSystemState
                {
                    version = int.Parse(f[0]), seedText = Uri.UnescapeDataString(f[1]), seedValue = f[2], fingerprint = f[3],
                    materialsDrawn = long.Parse(f[4]), facesDrawn = long.Parse(f[5]), piecesDrawn = long.Parse(f[6]),
                };
            }
            return data;
        }
    }
}
