using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Guardado en archivos JSON: {persistentDataPath}/saves/{slot}.json.
    /// Escribe primero a un .tmp y luego lo copia sobre el definitivo, para no dejar un archivo truncado si algo falla a mitad.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string root;

        public FileSaveStorage(string rootDirectory = null)
        {
            root = rootDirectory ?? Path.Combine(Application.persistentDataPath, "saves");
        }

        public void Write(string slot, string content)
        {
            string path = PathFor(slot);
            Directory.CreateDirectory(root);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, content, new UTF8Encoding(false));
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }

        public bool TryRead(string slot, out string content)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) { content = null; return false; }
            content = File.ReadAllText(path, Encoding.UTF8);
            return true;
        }

        public bool Exists(string slot) => File.Exists(PathFor(slot));

        public void Delete(string slot)
        {
            string path = PathFor(slot);
            if (File.Exists(path)) File.Delete(path);
        }

        private string PathFor(string slot)
        {
            if (string.IsNullOrEmpty(slot)) throw new ArgumentException("Slot vacío.", nameof(slot));
            foreach (char c in slot)
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                    throw new ArgumentException($"Slot inválido '{slot}': solo letras, dígitos, '_' y '-'.", nameof(slot));
            return Path.Combine(root, slot + ".json");
        }
    }
}
