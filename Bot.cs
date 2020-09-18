using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace S4Bot
{
	public class Bot : IDisposable
	{

       static string server = Config.Instance.Database.Host;
        static string database = Config.Instance.Database.Database;
        static string uid = Config.Instance.Database.Username;
        static string password = Config.Instance.Database.Password;
        public static MySqlConnection _connection = 
            new MySqlConnection(string.Format("server={0};Port=3306;Database={1};user={2};password={3}", server, database, uid, password));

        public static DiscordClient _client;
    
		private CommandsNextModule _cnext;

		public static System.Timers.Timer playerCountTimer;

		public static DiscordClient Discord;

		public static CommandsNextModule Commands;

		public static InteractivityModule Interactivity;

		private string prefix = Config.Instance.BotPrefix;

		private string token = Config.Instance.BotToken;



        public InteractivityModule _interactivity
		{
			get;
			set;
		}

		public StartTimes StartTimes
		{
			get;
		}

		public Bot()
		{
			_client = new DiscordClient(new DiscordConfiguration
			{
				AutoReconnect = true,
				EnableCompression = true,
				LogLevel = LogLevel.Info,
				Token = token,
				TokenType = TokenType.Bot,
				UseInternalLogHandler = true
			});
			_client.UseInteractivity(new InteractivityConfiguration
			{
				PaginationBehaviour = TimeoutBehaviour.Ignore,
				PaginationTimeout = TimeSpan.FromMinutes(5.0),
				Timeout = TimeSpan.FromMinutes(2.0)
			});
			StartTimes = new StartTimes
			{
				BotStart = DateTime.Now,
				SocketStart = DateTime.MinValue
			};
			_cnext = _client.UseCommandsNext(new CommandsNextConfiguration
			{
				CaseSensitive = false,
				EnableDefaultHelp = true,
				EnableDms = true,
				EnableMentionPrefix = true,
				StringPrefix = prefix,
				IgnoreExtraArguments = true
			});
			_cnext.RegisterCommands<Commands>();
			_client.Ready += OnReadyAsync;
			_client.SocketClosed += OnSocketClosed;
			_client.Resumed += OnSessionResumed;
		}

		private async Task OnReadyAsync(ReadyEventArgs e)
		{
			try
			{
                _connection.Open();
                Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(string.Format("                Connected To Database:{1} on Host : {0}", server, database));
            }
			catch (Exception ex)
			{
				Exception exception = ex;
				Console.WriteLine(exception);
			}
			await Task.Yield();
			StartTimes.SocketStart = DateTime.Now;
			playerCountTimer = new System.Timers.Timer(2000.0);
			playerCountTimer.AutoReset = true;
			playerCountTimer.Start();
			playerCountTimer.Elapsed += delegate
			{
        WebClient str = new System.Net.WebClient();
        string count = str.DownloadString(Config.Instance.BotPlaying);

        _client.UpdateStatusAsync(new DiscordGame(count));
			};
		}

		private Task OnSessionResumed(ReadyEventArgs e)
		{
			_connection.Close();
			Console.WriteLine("Reconnected");
            using (Bot bot = new Bot())
            {
                bot.RunAsync().Wait();
            }
            return Task.CompletedTask;
		}

		private Task OnSocketClosed(SocketCloseEventArgs e)
		{
			_connection.Close();
			Console.WriteLine("Disconnected");
            using (Bot bot = new Bot())
            {
                bot.RunAsync().Wait();
            }
            return Task.CompletedTask;
		}

		public async Task RunAsync()
		{
			await _client.ConnectAsync();
			await Task.Delay(-1);
        }

        public void Dispose()
		{
			_client.Dispose();
			_cnext = null;
		}
	}
}
