using System;
using Haggis.Domain.Model;

namespace HaggisRuntime.MonoGameUI
{
    // Placeholder for MonoGame UI. Replace with MonoGame Game subclass when adding MonoGame.
    // Intended structure:
    // - Initialize with HaggisGameState
    // - Update handles input and AI
    // - Draw renders cards and panels
    public class Game1
    {
        private readonly HaggisGameState _state;

        public Game1(HaggisGameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        // Temporary stub to avoid MonoGame dependency until packages/projects are added.
        public void Run()
        {
            // No-op. In real MonoGame, call base.Run() and implement Update/Draw.
        }
    }
}
