namespace MatchZy
{
    public interface IMatchDatabase
    {
        void InitializeDatabase(string directory);
        long InitMatch(string team1name, string team2name, string serverIp, bool isMatchSetup, long liveMatchId, int mapNumber, string seriesType, MatchConfig matchConfig);
        void UpdateTeamData(int matchId, string team1name, string team2name);
        Task SetMapEndData(long matchId, int mapNumber, string winnerName, int t1score, int t2score, int team1SeriesScore, int team2SeriesScore);
        Task SetMatchEndData(long matchId, string winnerName, int t1score, int t2score);
        Task UpdateMapStatsAsync(long matchId, int mapNumber, int t1score, int t2score);
        Task UpdatePlayerStatsAsync(long matchId, int mapNumber, Dictionary<ulong, Dictionary<string, object>> playerStatsDictionary);
        Task WritePlayerStatsToCsv(string filePath, long matchId, int mapNumber);
        Task UpdateDerankScoresAsync(long matchId, int t1score, int t2score);
        Task UpdateDerankPlayerStatsAsync(long matchId, int mapNumber, Dictionary<ulong, Dictionary<string, object>> playerStatsDictionary);
        Task SetDerankMatchFinished(long matchId);
        // Live player presence — drives the web scoreboard's green/red dots via
        // pg_notify('match_presence'). Connect upserts a row; disconnect stamps
        // disconnected_at. No-op on non-Postgres backends.
        Task SetDerankPlayerConnected(long matchId, ulong steamId);
        Task SetDerankPlayerDisconnected(long matchId, ulong steamId);
    }
}
