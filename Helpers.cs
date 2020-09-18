using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace S4Bot
{
	public class Helpers
	{
   static string database = Config.Instance.Database.Database;

        public static async Task<bool> IsPasswordCorrect(string userPassword, string encryptedPassword, string encryptedSalt)
		{
			bool success = false;
			byte[] saltString = Convert.FromBase64String(encryptedSalt);
			byte[] passwordGuess = new Rfc2898DeriveBytes(userPassword, saltString, 24000).GetBytes(24);
			byte[] actualPassword = Convert.FromBase64String(encryptedPassword);
			uint difference = (uint)(passwordGuess.Length ^ actualPassword.Length);
			for (int i = 0; i < passwordGuess.Length && i < actualPassword.Length; i++)
			{
				difference = (uint)((int)difference | (passwordGuess[i] ^ actualPassword[i]));
			}
			if (difference != 0)
			{
				success = false;
			}
			else if (difference == 0)
			{
				success = true;
			}
			return success;
		}

		public static async Task<bool> IsUserLoggedIn(DiscordMember member)
		{
			bool loggedin = false;
			ulong memberID = member.Id;
			MySqlCommand checkUser = new MySqlCommand($"select * from {database}.discord_shop_accounts where discord_id = '{memberID}'", Bot._connection);
			using (MySqlDataReader reader = checkUser.ExecuteReader())
			{
				loggedin = (reader.HasRows ? true : false);
			}
			return loggedin;
		}

		public static async Task<int[]> GetUserAPAndPen(string id)
		{
			int ap = 0;
			int pen = 0;
			int[] user = new int[2];
			MySqlCommand selectCommand = new MySqlCommand("select * from " + database + ".players WHERE Id = '" + id + "'", Bot._connection);
			using (MySqlDataReader reader = selectCommand.ExecuteReader())
			{
				while (reader.Read())
				{
					ap = Convert.ToInt32(reader.GetString("AP"));
					pen = Convert.ToInt32(reader.GetString("PEN"));
				}
				user = new int[2]
				{
					ap,
					pen
				};
				reader.Close();
			}
			return user;
		}

		public static async Task<string> GetUserNickname(string id)
		{
			string nick = "";
			MySqlCommand selectCommand = new MySqlCommand("select * from "+ database +".accounts WHERE Id = '" + id + "'", Bot._connection);
			using (MySqlDataReader reader = selectCommand.ExecuteReader())
			{
				while (reader.Read())
				{
					nick = reader.GetString("Nickname");
				}
				reader.Close();
			}
			return nick;
		}

		public static async Task<int> GetUserID(CommandContext ctx)
		{
			int userID = 0;
			MySqlCommand getuserID = new MySqlCommand("SELECT * FROM "+ database +".discord_shop_accounts WHERE discord_id = '" + ctx.Member.Id + "'", Bot._connection);
			using (MySqlDataReader reader = getuserID.ExecuteReader())
			{
				while (reader.Read())
				{
					userID = reader.GetInt32("player_id");
				}
				reader.Close();
			}
			return userID;
		}
	}
}
