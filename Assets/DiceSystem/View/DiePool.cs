using System;
using System.Collections.Generic;
using TurnGame.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DiceMaterials.Runtime
{
    /// <summary>
    /// Pool de Die para el tipo de dado de la partida (un solo prefab, no cambia).
    /// Scoped al Player: al disponer el scope destruye todos los dados, activos o no.
    /// </summary>
    public sealed class DiePool : IDieFactory, IDisposable
    {
        private readonly Die prefab;
        private readonly IMessageBroker broker;
        private readonly IFaceStyleSource styleSource;
        private readonly Stack<Die> inactive = new Stack<Die>();
        private readonly HashSet<Die> active = new HashSet<Die>();
        private readonly Transform root;
        private bool disposed;

        public DiePool(DiePrefabMapSO prefabMap, IDieTypeSource dieType,
                       IMessageBroker broker, IFaceStyleSource styleSource)
        {
            if (prefabMap == null) throw new ArgumentNullException(nameof(prefabMap));
            if (dieType == null) throw new ArgumentNullException(nameof(dieType));
            this.broker = broker ?? throw new ArgumentNullException(nameof(broker));
            this.styleSource = styleSource ?? throw new ArgumentNullException(nameof(styleSource));

            if (!prefabMap.TryGetPrefab(dieType.FaceCount, out prefab))
                throw new InvalidOperationException(
                    $"DiePrefabMap '{prefabMap.name}' no tiene prefab para dados de {dieType.FaceCount} caras.");

            root = new GameObject("DicePool").transform;
        }

        public int ActiveCount => active.Count;
        public int InactiveCount => inactive.Count;

        /// <summary>Crea dados por adelantado (basta con el máximo de dados simultáneos).</summary>
        public void Prewarm(int count)
        {
            ThrowIfDisposed();
            for (int i = 0; i < count; i++)
            {
                var die = CreateNew();
                die.gameObject.SetActive(false);
                inactive.Push(die);
            }
        }

        public Die Get(DieData data)
        {
            ThrowIfDisposed();
            if (data == null) throw new ArgumentNullException(nameof(data));

            Die die = inactive.Count > 0 ? inactive.Pop() : CreateNew();
            die.transform.SetParent(null, false);
            die.gameObject.SetActive(true);
            die.Init(data, broker, styleSource);
            active.Add(die);
            return die;
        }

        public void Release(Die die)
        {
            if (disposed || die == null) return;
            if (!active.Remove(die))
                throw new InvalidOperationException($"El Die '{die.name}' no pertenece a este pool o ya fue devuelto.");

            die.Release();
            die.gameObject.SetActive(false);
            die.transform.SetParent(root, false);
            inactive.Push(die);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            foreach (var die in active)
            {
                if (die == null) continue;
                die.Release();
                Object.Destroy(die.gameObject);
            }
            active.Clear();

            while (inactive.Count > 0)
            {
                var die = inactive.Pop();
                if (die != null) Object.Destroy(die.gameObject);
            }

            if (root != null) Object.Destroy(root.gameObject);
        }

        private Die CreateNew()
        {
            var die = Object.Instantiate(prefab, root);
            die.name = prefab.name;
            return die;
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(DiePool));
        }
    }
}
