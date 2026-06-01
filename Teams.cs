using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;

namespace MatchZy
{

    public class Team 
    {
        [JsonPropertyName("id")]
        public string id = "";

        [JsonPropertyName("teamname")]
        public required string teamName;

        [JsonPropertyName("teamflag")]
        public string teamFlag = "";

        [JsonPropertyName("teamtag")]
        public string teamTag = "";

        [JsonPropertyName("teamplayers")]
        public JToken? teamPlayers;

        [JsonIgnore, Newtonsoft.Json.JsonIgnore]
        public HashSet<CCSPlayerController> coach = [];

        [JsonPropertyName("seriesscore")]
        public int seriesScore = 0;
    }

    public partial class MatchZy
    {
        [ConsoleCommand("css_coach", "Sets coach for the requested team")]
        public void OnCoachCommand(CCSPlayerController? player, CommandInfo command) 
        {
            HandleCoachCommand(player, command.ArgString);
        }

        [ConsoleCommand("css_uncoach", "Sets coach for the requested team")]
        public void OnUnCoachCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null || !player.PlayerPawn.IsValid) return;
            if (isPractice) {
                ReplyToUserCommand(player, "Uncoach command can only be used in match mode!");
                return;
            }

            if (matchzyTeam1.coach.Contains(player)) {
                player.Clan = "";
                matchzyTeam1.coach.Remove(player);
                SetPlayerVisible(player);
            }
            else if (matchzyTeam2.coach.Contains(player)) {
                player.Clan = "";
                matchzyTeam2.coach.Remove(player);
                SetPlayerVisible(player);
            }
            else {
                ReplyToUserCommand(player, "You are not coaching any team!");
                return;
            }

            if (player.InGameMoneyServices != null) player.InGameMoneyServices.Account = 0;

            ReplyToUserCommand(player, "You are now not coaching any team!");
        }

        [ConsoleCommand("matchzy_addplayer", "Adds player to the provided team")]
        [ConsoleCommand("get5_addplayer", "Adds player to the provided team")]
        public void OnAddPlayerCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player != null || command == null) return;
            if (!isMatchSetup) {
                command.ReplyToCommand("No match is setup!");
                return;
            }
            if (IsHalfTimePhase())
            {
                command.ReplyToCommand("Cannot add players during halftime. Please wait until the next round starts.");
                return;
            }
            // Mid-round adds put the joiner on a side decided by stale teamSides
            // (the side flips at round-end). Require freezetime so the side
            // mapping the bot saw matches what the player will spawn into.
            // Bot pipeline must call pause + wait-for-freezetime before addplayer.
            if (isMatchLive && !IsFreezeTime())
            {
                command.ReplyToCommand("Cannot add players mid-round. Wait for freezetime (start of next round).");
                return;
            }
            if (command.ArgCount < 3)
            {
                command.ReplyToCommand("Usage: matchzy_addplayer <steam64> <team> \"<name>\"");
                return;
            }

            string playerSteamId = command.ArgByIndex(1);
            string playerTeam = command.ArgByIndex(2);
            string playerName = command.ArgByIndex(3);
            bool success;
            if (playerTeam == "team1")
            {
                success = AddPlayerToTeam(playerSteamId, playerName, matchzyTeam1.teamPlayers);
            } else if (playerTeam == "team2")
            {
                success = AddPlayerToTeam(playerSteamId, playerName, matchzyTeam2.teamPlayers);
            } else if (playerTeam == "spec")
            {
                success = AddPlayerToTeam(playerSteamId, playerName, matchConfig.Spectators);
            } else 
            {
                command.ReplyToCommand("Unknown team: must be one of team1, team2, spec");
                return; 
            }
            if (!success)
            {
                command.ReplyToCommand($"Failed to add player {playerName} to {playerTeam}. They may already be on a team or you provided an invalid Steam ID.");
                return;
            }
            command.ReplyToCommand($"Player {playerName} added to {playerTeam} successfully!");
        }

        // Roster-only add for live substitutions. Unlike matchzy_addplayer this
        // does NOT require freezetime: it only inserts the steamid into the team
        // roster (teamPlayers) so the player passes the GetPlayerTeam() == None
        // kick gate in EventPlayerConnectFullHandler and can connect at ANY time.
        // It does NOT switch the player's side — CS2 spawns roster members onto
        // their team at the next round start, so a mid-round connect just waits
        // (dead/spectating) until the next round, which is the desired behavior.
        // The bot calls this the instant a substitute accepts, so the sub never
        // races the freezetime-gated addplayer and gets kicked.
        [ConsoleCommand("matchzy_rosteradd", "Adds player to a team roster without requiring freezetime (live-sub whitelist)")]
        public void OnRosterAddCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player != null || command == null) return;
            if (!isMatchSetup) {
                command.ReplyToCommand("No match is setup!");
                return;
            }
            if (command.ArgCount < 3)
            {
                command.ReplyToCommand("Usage: matchzy_rosteradd <steam64> <team> \"<name>\"");
                return;
            }

            string playerSteamId = command.ArgByIndex(1);
            string playerTeam = command.ArgByIndex(2);
            string playerName = command.ArgByIndex(3);
            bool success;
            if (playerTeam == "team1")
            {
                success = AddPlayerToTeam(playerSteamId, playerName, matchzyTeam1.teamPlayers);
            } else if (playerTeam == "team2")
            {
                success = AddPlayerToTeam(playerSteamId, playerName, matchzyTeam2.teamPlayers);
            } else if (playerTeam == "spec")
            {
                success = AddPlayerToTeam(playerSteamId, playerName, matchConfig.Spectators);
            } else
            {
                command.ReplyToCommand("Unknown team: must be one of team1, team2, spec");
                return;
            }
            if (!success)
            {
                // Already on a team is the idempotent-retry case — report it
                // distinctly so the bot can treat it as success.
                command.ReplyToCommand($"Failed to add player {playerName} to {playerTeam}. They may already be on a team or you provided an invalid Steam ID.");
                return;
            }

            // If the substitute is ALREADY connected (they joined before this
            // command landed and were sitting on the kick edge), assign their
            // team now so they aren't stuck unassigned. SwitchPlayerTeam is a
            // no-op when they're already on the right side; mid-round CS2 holds
            // them until the next spawn, which is fine.
            CCSPlayerController? connected = Utilities.GetPlayerFromSteamId(ulong.TryParse(playerSteamId, out ulong sid) ? sid : 0);
            if (IsPlayerValid(connected))
            {
                CsTeam assignedTeam = GetPlayerTeam(connected!);
                if (assignedTeam != CsTeam.None) SwitchPlayerTeam(connected!, assignedTeam);
            }

            command.ReplyToCommand($"Player {playerName} roster-added to {playerTeam} successfully!");
        }

        [ConsoleCommand("matchzy_removeplayer", "Removes the player from all the teams")]
        [ConsoleCommand("get5_removeplayer", "Removes the player from all the teams")]
        [CommandHelper(minArgs: 1, usage: "<steam64>")]
        public void OnRemovePlayerCommand(CCSPlayerController? player, CommandInfo? command)
        {
            if (player != null || command == null) return;
            if (!isMatchSetup) {
                command.ReplyToCommand("No match is setup!");
                return;
            }
            if (IsHalfTimePhase())
            {
                command.ReplyToCommand("Cannot remove players during halftime. Please wait until the next round starts.");
                return;
            }
            if (isMatchLive && !IsFreezeTime())
            {
                command.ReplyToCommand("Cannot remove players mid-round. Wait for freezetime (start of next round).");
                return;
            }

            string arg = command.GetArg(1);

            if (!ulong.TryParse(arg, out ulong steamId))
            {
                command.ReplyToCommand($"Invalid Steam64");
            }

            bool success = RemovePlayerFromTeam(steamId.ToString());
            if (success)
            {
                command.ReplyToCommand($"Successfully removed player {steamId}");
                CCSPlayerController? removedPlayer = Utilities.GetPlayerFromSteamId(steamId);
                if (IsPlayerValid(removedPlayer))
                {
                    Log($"Kicking player {removedPlayer!.PlayerName} - Not a player in this game (removed).");
                    PrintToAllChat($"Kicking player {removedPlayer!.PlayerName} - Not a player in this game.");
                    KickPlayer(removedPlayer);
                }
            }
            else
            {
                command.ReplyToCommand($"Player {steamId} not found in any team or the Steam ID was invalid.");
            }
        }

        public bool AddPlayerToTeam(string steamId, string name, JToken? team)
        {
            if (matchzyTeam1.teamPlayers != null && matchzyTeam1.teamPlayers[steamId] != null) return false;
            if (matchzyTeam2.teamPlayers != null && matchzyTeam2.teamPlayers[steamId] != null) return false;
            if (matchConfig.Spectators != null && matchConfig.Spectators[steamId] != null) return false;

            if (team is JObject jObjectTeam)
            {
                jObjectTeam.Add(steamId, name);
                LoadClientNames();
                return true;
            }
            else if (team is JArray jArrayTeam)
            {
                jArrayTeam.Add(name);
                LoadClientNames();
                return true;
            }
            return false;
        }

        public bool RemovePlayerFromTeam(string steamId)
        {
            List<JToken?> teams = [matchzyTeam1.teamPlayers, matchzyTeam2.teamPlayers, matchConfig.Spectators];

            foreach (var team in teams)
            {
                if (team is null) continue;
                if (team is JObject jObjectTeam)
                {
                    jObjectTeam.Remove(steamId);
                    return true;
                }
                else if (team is JArray jArrayTeam)
                {
                    jArrayTeam.Remove(steamId);
                    return true;
                }
            }
            return false;
        }
    }
}
