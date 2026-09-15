using Newtonsoft.Json;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class PlayerData : ISaveData
    {
        public const int CurrentSchemaVersion = 1;

        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int SchemaVersion { get; set; }

        [JsonProperty("gold", Required = Required.Always)]
        public int Gold { get; set; }

        [JsonProperty("wins", Required = Required.Always)]
        public int Wins { get; set; }

        [JsonProperty("losses", Required = Required.Always)]
        public int Losses { get; set; }

        public PlayerData()
        {
            SchemaVersion = CurrentSchemaVersion;
        }

        public PlayerData(int gold, int wins, int losses)
        {
            SchemaVersion = CurrentSchemaVersion;
            Gold = gold;
            Wins = wins;
            Losses = losses;
        }

        public void Validate()
        {
            if (SchemaVersion != CurrentSchemaVersion)
                throw new JsonSerializationException("Unsupported player data schema: " + SchemaVersion);

            if (Gold < 0 || Wins < 0 || Losses < 0)
                throw new JsonSerializationException("Player progress values cannot be negative");
        }
    }
}
