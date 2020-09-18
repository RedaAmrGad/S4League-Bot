using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hjson;
using Newtonsoft.Json;
using System.IO;
namespace S4Bot
{
  
  public class Config
  {

      private static readonly string save_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.hjson");

      static Config()
      {
        if (!File.Exists(save_path))
        {
          Instance = new Config();
          Instance.Save();
          return;
        }

        using (var stream = new FileStream(save_path, FileMode.Open, FileAccess.Read))
        {
          Instance = JsonConvert.DeserializeObject<Config>(HjsonValue.Load(stream).ToString(Stringify.Plain));
        }
      }

      public Config()
      {
        BotToken = "";
        BotPrefix = "";
        BotPlaying = "";
        Database = new DatabaseConfig();
        OwnerId = 0;
        nametagcost = 0;
        clanlogocost = 0;
        apexchangerate = 0;
        }

        public static Config Instance { get; }
      [JsonProperty("bot_token")] public string BotToken { get; set; }
      [JsonProperty("bot_prefix")] public string BotPrefix { get; set; }
      [JsonProperty("bot_playing")] public string BotPlaying { get; set; }
      [JsonProperty("database")] public DatabaseConfig Database { get; set; }
      [JsonProperty("NametagCost")] public int nametagcost { get; set; }
      [JsonProperty("ClanLogoCost")] public int clanlogocost { get; set; }
      [JsonProperty("ExchangeRate")] public int apexchangerate { get; set; }
     [JsonProperty("owner")] public ulong OwnerId { get; set; }
        private void Save()
      {
        string json = JsonConvert.SerializeObject(this, Formatting.None);
        File.WriteAllText(save_path, JsonValue.Parse(json).ToString(Stringify.Hjson));
      }
      public class DatabaseConfig
      {
        public DatabaseConfig()
        {
          Host = "localhost";
        }

        [JsonProperty("host")] public string Host { get; set; }
        [JsonProperty("username")] public string Username { get; set; }
        [JsonProperty("password")] public string Password { get; set; }
        [JsonProperty("database")] public string Database { get; set; }
      }
    }
  


public class StartTimes
  {
    internal DateTime BotStart;

    internal DateTime SocketStart;
  }
}
