using Mirror;

namespace ScaryGame.Networking
{
    /// Broadcast by the host to every connected client (including the host's own
    /// local client) when the host presses Start Game in the lobby.
    /// For now the only reaction is a Debug.LogError on each machine — the real
    /// "load Level_1" flow is designed in Lobby, level loading design.md and comes later.
    public struct StartGameMessage : NetworkMessage { }
}
