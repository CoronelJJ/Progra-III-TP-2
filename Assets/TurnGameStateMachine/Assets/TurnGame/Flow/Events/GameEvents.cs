namespace TurnGame.Flow.Events
{
    // ---- Turn flow: each state publishes its "...Requested" on Enter (after subscribing to the answer).

    public readonly struct TurnStartRequested { }
    public readonly struct TurnIntroFinished { }

    public readonly struct RollElementsRequested { }
    public readonly struct ElementsRolled { }

    public readonly struct BuildPieceRequested { }
    public readonly struct PieceBuilt { }

    public readonly struct PlacePieceRequested { }
    public readonly struct PiecePlaced { }

    /// <summary>
    /// Emitted by the input/board scripts while the piece moves (valid or not).
    /// The state machine does NOT listen to it: it is consumed by the preview/feedback system.
    /// Adapt the payload to your coordinates (cell, Vector2Int, ...).
    /// </summary>
    public readonly struct PieceMoved
    {
        public readonly float X;
        public readonly float Y;
        public readonly bool IsValid;
        public PieceMoved(float x, float y, bool isValid) { X = x; Y = y; IsValid = isValid; }
    }

    public readonly struct UpdateBoardRequested { }
    public readonly struct BoardUpdated { }

    public readonly struct CheckBoardRequested { }
    public readonly struct PlayDetected { }
    public readonly struct NoPlayDetected { }

    public readonly struct ResolvePlayRequested { }
    public readonly struct PlayResolved { }

    public readonly struct PlayFeedbackRequested { }
    public readonly struct FeedbackFinished { }

    public readonly struct TurnEndRequested { }
    public readonly struct NextTurnReady { }

    // ---- Global events handled by GameMachine (not by individual states)

    public readonly struct PauseRequested { }
    public readonly struct ResumeRequested { }
    public readonly struct GameOverReached { }

    /// <summary>Machine -> systems: freeze input/animations/audio.</summary>
    public readonly struct GamePaused { }
    /// <summary>Machine -> systems: unfreeze.</summary>
    public readonly struct GameResumed { }

    // ---- Game over
    public readonly struct GameOverScreenRequested { }
    public readonly struct GameOverDismissed { }
}
