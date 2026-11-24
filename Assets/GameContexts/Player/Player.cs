using System;

namespace TurnGame.Core
{
    public sealed class Player : IPlayer
    {
        public int Id { get; }
        public string Name { get; }

        public Player(int id, string name)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override string ToString() => $"Player {Id} ({Name})";
    }
}
