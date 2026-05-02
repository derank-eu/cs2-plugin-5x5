using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;


namespace MatchZy
{

    public class MatchConfig
    {
        [JsonPropertyName("maplist")]
        public List<string> Maplist { get; set; } = new List<string>();

        [JsonPropertyName("maps_pool")]
        public List<string> MapsPool { get; set; } = new List<string>();

        [JsonPropertyName("maps_left_in_veto_pool")]
        public List<string> MapsLeftInVetoPool { get; set; } = new List<string>();

        [JsonPropertyName("map_ban_order")]
        public List<string> MapBanOrder { get; set; } = new List<string>();

        [JsonPropertyName("skip_veto")]
        public bool SkipVeto { get; set; } = true;

        [JsonPropertyName("match_id")]
        public long MatchId { get; set; }

        [JsonPropertyName("num_maps")]
        public int NumMaps { get; set; } = 1;

        [JsonPropertyName("players_per_team")]
        public int PlayersPerTeam { get; set; } = 5;

        [JsonPropertyName("min_players_to_ready")]
        public int MinPlayersToReady { get; set; } = 12;

        [JsonPropertyName("min_spectators_to_ready")]
        public int MinSpectatorsToReady { get; set; } = 0;

        [JsonPropertyName("current_map_number")]
        public int CurrentMapNumber { get; set; } = 0;

        [JsonPropertyName("map_sides")]
        public List<string> MapSides { get; set; } = new List<string>();

        [JsonPropertyName("series_can_clinch")]
        public bool SeriesCanClinch { get; set; } = true;

        [JsonPropertyName("scrim")]
        public bool Scrim { get; set; } = false;

        [JsonPropertyName("wingman")]
        public bool Wingman { get; set; } = false;

        // Derank fork: when true, LoadMatchFromJSON skips the post-load `changelevel`
        // even if IsMapReloadRequiredForGameMode/currentMap-mismatch would normally trigger it.
        // Default false → upstream MatchZy behavior, no change for any non-opt-in match.
        // Used by 2v2 wingman tournaments where the bot has already booted the server on
        // the target map in mg_wingman; the runtime changelevel resets gametype to
        // competitive 5v5 and breaks the match.
        [JsonPropertyName("skip_changelevel")]
        public bool SkipChangelevel { get; set; } = false;

        [JsonPropertyName("match_side_type")]
        public string MatchSideType { get; set; } = "standard";

        [JsonPropertyName("changed_cvars")]
        public Dictionary<string, string> ChangedCvars { get; set; } = new();

        [JsonPropertyName("original_cvars")]
        public Dictionary<string, string> OriginalCvars { get; set; } = new();

        [JsonPropertyName("spectators")]
        public JToken Spectators { get; set; } = new JObject();

        [JsonPropertyName("remote_log_url")]
        public string RemoteLogURL { get; set; } = "";

        [JsonPropertyName("remote_log_header_key")]
        public string RemoteLogHeaderKey { get; set; } = "";

        [JsonPropertyName("remote_log_header_value")]
        public string RemoteLogHeaderValue { get; set; } = "";
    }
}
