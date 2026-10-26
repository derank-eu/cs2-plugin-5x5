using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json.Linq;

namespace MatchZy;

public enum PlacementAction { None, ChangeTeam, SwitchTeam }

public enum RosterSlot { None, Team1, Team2, Spectator }

// Team placement rules with no engine calls, so cs2-plugin/tests can run them.
// The plugin wraps these with the real player calls (GetPlayerTeam,
// JoinAssignedTeam, the jointeam listener).
public static class TeamPlacement
{
    // The side a steamid belongs on, from the match rosters and each team's
    // current side ("CT" / "TERRORIST"). None means the player is not in this match.
    public static CsTeam RosterTeam(string steamId, JToken? team1Players, string? team1Side, JToken? team2Players, string? team2Side, JToken? spectators)
    {
        return FindSlot(steamId, team1Players, team2Players, spectators) switch
        {
            RosterSlot.Team1 => SideToTeam(team1Side),
            RosterSlot.Team2 => SideToTeam(team2Side),
            RosterSlot.Spectator => CsTeam.Spectator,
            _ => CsTeam.None,
        };
    }

    // Which roster holds the steamid. Rosters are steamid -> name objects.
    public static RosterSlot FindSlot(string steamId, JToken? team1Players, JToken? team2Players, JToken? spectators)
    {
        if (InRoster(team1Players, steamId)) return RosterSlot.Team1;
        if (InRoster(team2Players, steamId)) return RosterSlot.Team2;
        if (InRoster(spectators, steamId)) return RosterSlot.Spectator;
        return RosterSlot.None;
    }

    private static bool InRoster(JToken? roster, string steamId) => roster is JObject players && players[steamId] != null;

    public static CsTeam SideToTeam(string? side) => side switch
    {
        "CT" => CsTeam.CounterTerrorist,
        "TERRORIST" => CsTeam.Terrorist,
        _ => CsTeam.None,
    };

    // What to do with a player who belongs on `assigned`. Only players in an
    // active match (setup or veto) who are not coaching get placed.
    public static PlacementAction Decide(CsTeam current, CsTeam assigned, bool matchActive, bool isCoach)
    {
        if (!matchActive || isCoach) return PlacementAction.None;
        if (assigned == CsTeam.None || current == assigned) return PlacementAction.None;
        // No team yet: ChangeTeam is the jointeam path and gives a proper spawn.
        if (current is CsTeam.None or CsTeam.Spectator) return PlacementAction.ChangeTeam;
        return PlacementAction.SwitchTeam;
    }

    // jointeam is refused when it would put a player anywhere but their roster side.
    public static bool BlockJoinTeam(bool matchActive, int joiningTeam, CsTeam assigned) => matchActive && joiningTeam != (int)assigned;
}
