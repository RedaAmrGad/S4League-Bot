using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace S4Bot
{
    public static class Logger
    {

        public static StringBuilder LogString = new StringBuilder();
        public static void Information(string str)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Information: "+str);
            LogString.Append(str).Append(Environment.NewLine);

            File.AppendAllText("log.txt", "Information: " + LogString.ToString());
            LogString.Clear();
        }
        public static void Error(string str)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error!!: "+str);
            LogString.Append(str).Append(Environment.NewLine);
            File.AppendAllText("log.txt", "Error!!: " + LogString.ToString());
            LogString.Clear();
        }
        public static void Warning(string str)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Warning!!: "+str);
            LogString.Append(str).Append(Environment.NewLine);
            File.AppendAllText( "log.txt", "Warning!!: " + LogString.ToString());
            LogString.Clear();
        }
    }
    public class Commands
    {
        static ulong OwnerID = Config.Instance.OwnerId;
        static string host = Config.Instance.Database.Host;
        static string uid = Config.Instance.Database.Username;
        static string password = Config.Instance.Database.Password;
        static string database = Config.Instance.Database.Database;

        [Command("register")]
        [Description("Register a new account")]
        public async Task Register(CommandContext ctx)
        {
            try
            {
                InteractivityModule interactivity = ctx.Client.GetInteractivityModule();
                if (Helpers.IsUserLoggedIn(ctx.Member).Result)
                {
                    await ctx.RespondAsync("You're logged in , logout first!");
                    return;
                }
                DiscordMember member = ctx.Member;
                Task<DiscordDmChannel> DMchannel = member.CreateDmChannelAsync();
                await ctx.RespondAsync("Sent a direct message asking for new username and password");
                await DMchannel.Result.SendMessageAsync("What is your new username?");
                Task<MessageContext> username = interactivity.WaitForMessageAsync((DiscordMessage x) => x.Author == ctx.Member && x.Content != null && !ctx.Member.IsBot, TimeSpan.FromSeconds(60.0));
                string userUsername = username.Result.Message.Content;
                if (userUsername.Length < 6 || userUsername.Length > 15 || userUsername==null) { await DMchannel.Result.SendMessageAsync("Username Length Must Be More Than 6 & Less Than 15 !"); return; }
                if (!(new Regex("^[a-zA-Z0-9.*_-]*$").IsMatch(userUsername))){ await DMchannel.Result.SendMessageAsync("Username Contains Invalid Characters !"); return; }
                await DMchannel.Result.SendMessageAsync("What is your new password?");
                Task<MessageContext> password = interactivity.WaitForMessageAsync((DiscordMessage x) => x.Author == ctx.Member && x.Content != null && !ctx.Member.IsBot, TimeSpan.FromSeconds(60.0));
                string userPassword = password.Result.Message.Content;
                if (userPassword.Length < 8 || userPassword.Length > 15 || userPassword==null) { await DMchannel.Result.SendMessageAsync("Password Length Must Be More Than 8 & Less Than 15!"); return; }
                await DMchannel.Result.SendMessageAsync("Registering your account, Wait A Confirmation Message");
                //Generating Salt , Hashing Password & Inserting New Account
                var newSalt = new byte[24];
                using (var csprng = new RNGCryptoServiceProvider())
                    csprng.GetBytes(newSalt);
                var hash = new byte[24];
                using (var pbkdf2 = new Rfc2898DeriveBytes(userPassword, newSalt, 24000))
                    hash = pbkdf2.GetBytes(24);
                string pass = Convert.ToBase64String(hash);
                string salt = Convert.ToBase64String(newSalt);
                MySqlCommand insert = new MySqlCommand($"INSERT INTO accounts (Username, Password, Salt) VALUES ('{userUsername}','{pass}','{salt}')", Bot._connection);
                insert.ExecuteNonQuery();
                await DMchannel.Result.SendMessageAsync("Registered Successfully, Have Fun Playing");
                Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id}  Registered Account {userUsername}");
            
                
            }
            catch (Exception e)
            {
                Logger.Warning(e.StackTrace);
                DiscordMember member = ctx.Member;
                Task<DiscordDmChannel> DMchannel = member.CreateDmChannelAsync();
                await DMchannel.Result.SendMessageAsync("Registration Failed - Maybe Username Exists");
                Logger.Error($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} Failed To Register");
                throw;
            }
        }


        [Command("login")]
        [Description("Login with your ingame account")]
        public async Task LogInDB(CommandContext ctx)
		{
			try
			{
				InteractivityModule interactivity = ctx.Client.GetInteractivityModule();
                MySqlCommand checkuserLogged = new MySqlCommand($"SELECT * FROM {database}.discord_shop_accounts where discord_id = '{ctx.Member.Id}'", Bot._connection);
                using (MySqlDataReader reader = checkuserLogged.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.HasRows)
                        {
                            await ctx.RespondAsync("You are already logged in...");
                            return;
                        }
                        string blocked = reader["isBlocked"].ToString();
                        if (blocked == "yes")
                        {
                            await ctx.RespondAsync("You're blocked. DM an Admin");
                            return;
                        }
                    }
                }
                DiscordMember member = ctx.Member;
				Task<DiscordDmChannel> DMchannel = member.CreateDmChannelAsync();
                await ctx.RespondAsync("Sent a direct message asking for username and password");
                await DMchannel.Result.SendMessageAsync("What is your username?");
				Task<MessageContext> username = interactivity.WaitForMessageAsync((DiscordMessage x) => x.Author == ctx.Member && x.Content != null && !ctx.Member.IsBot, TimeSpan.FromSeconds(60.0));
				string userUsername = username.Result.Message.Content;
				await DMchannel.Result.SendMessageAsync("What is your password? (we are not storing any passwords.)");
				Task<MessageContext> password = interactivity.WaitForMessageAsync((DiscordMessage x) => x.Author == ctx.Member && x.Content != null && !ctx.Member.IsBot, TimeSpan.FromSeconds(60.0));
				string userPassword = password.Result.Message.Content;
				await DMchannel.Result.SendMessageAsync("Logging in, Wait Another Confirmation Message...");
				string encryptedPass = "";
				string encryptedSalt = "";
				string databaseUserId = "";
				MySqlCommand getUserIndex = new MySqlCommand("SELECT * FROM "+ database +".accounts WHERE username = '" + userUsername + "'", Bot._connection);
				using (MySqlDataReader mysqlReader = getUserIndex.ExecuteReader())
				{
					while (mysqlReader.Read())
					{
						encryptedPass = mysqlReader["Password"].ToString();
						encryptedSalt = mysqlReader["Salt"].ToString();
						databaseUserId = mysqlReader["Id"].ToString();
					}
				}
				Task<bool> checkPassword = Helpers.IsPasswordCorrect(userPassword, encryptedPass, encryptedSalt);
				if (checkPassword.Result.Equals(obj: true))
				{
					MySqlCommand insert = new MySqlCommand($"INSERT INTO discord_shop_accounts (discord_id, player_id, isBlocked) VALUES ('{ctx.Member.Id}','{databaseUserId}','no')", Bot._connection);
					insert.ExecuteNonQuery();
					await DMchannel.Result.SendMessageAsync("Logged In Successfully");
                    Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} Logged In using {userUsername}");

                }
                else
				{
					await DMchannel.Result.SendMessageAsync("Wrong ID or Password !");
                    Logger.Error($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} Used Wrong ID/Password");
                    return;
                }
            }
			catch (Exception e)
			{
                Logger.Warning(e.StackTrace);
				throw;
			}
		}


        [Command("logout")]
        [Description("Logs out from your account")]
        public async Task LogOutDB(CommandContext ctx)
        {
            try
            {
                if (!Helpers.IsUserLoggedIn(ctx.Member).Result)
                {
                    await ctx.RespondAsync("You're not logged in.");
                    return;
                }
                MySqlCommand insert = new MySqlCommand($"DELETE FROM {database}.discord_shop_accounts where discord_id = '{ctx.Member.Id}'", Bot._connection);
                insert.ExecuteNonQuery();
                await ctx.RespondAsync("Logged Out");
                Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} Logged Out");
                return;
            }

            catch (Exception e)
            {
                Logger.Warning(e.StackTrace);
                throw;
            }
        }


        [Command("changepw")]
        [Description("Changes your account password")]
        public async Task ChangePw(CommandContext ctx)
        {
            try
            {

                DiscordMember member = ctx.Member;
                Task<DiscordDmChannel> DMchannel = member.CreateDmChannelAsync();
                InteractivityModule interactivity = ctx.Client.GetInteractivityModule();
                Task<int> userID = Helpers.GetUserID(ctx);
                int[] userMisc = Helpers.GetUserAPAndPen(Convert.ToString(userID.Result)).Result;

                if (!Helpers.IsUserLoggedIn(ctx.Member).Result)
                {
                    await ctx.RespondAsync("You're not logged in.");
                    return;
                }

                await ctx.RespondAsync("Sent a direct message asking for new password");
                await DMchannel.Result.SendMessageAsync("What is your new password?");
                Task<MessageContext> password = interactivity.WaitForMessageAsync((DiscordMessage x) => x.Author == ctx.Member && x.Content != null && !ctx.Member.IsBot, TimeSpan.FromSeconds(60.0));
                string userPassword = password.Result.Message.Content;
                if (userPassword.Length < 8 || userPassword.Length > 15 || userPassword == null) { await DMchannel.Result.SendMessageAsync("Password Length Must Be More Than 8 & Less Than 15!"); return; }
                //Inserts New Passowrd
                var newSalt = new byte[24];
                using (var csprng = new RNGCryptoServiceProvider())
                    csprng.GetBytes(newSalt);
                var hash = new byte[24];
                using (var pbkdf2 = new Rfc2898DeriveBytes(userPassword, newSalt, 24000))
                    hash = pbkdf2.GetBytes(24);
                string pass = Convert.ToBase64String(hash);
                string salt = Convert.ToBase64String(newSalt);
                MySqlCommand insert = new MySqlCommand($"UPDATE `{database}`.`accounts` SET `Password`= '{pass}', `Salt`= '{salt}' WHERE  `Id`= {userID.Result}", Bot._connection);
                insert.ExecuteNonQuery();
                await DMchannel.Result.SendMessageAsync("Password Changed Successfully");
                Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} Changed His Password");
                return;
            }
            catch (Exception e)
            {
                Logger.Warning(Convert.ToString(e));
                throw;
            }

        }

    [Command("nametag")]
    [Description("Add a nametag before your nickname")]
      public async Task BuyItem(CommandContext ctx, [Description("you can find availabe nametags at #nametags")] string nameTag)
		{
			try
			{
				if (!Helpers.IsUserLoggedIn(ctx.Member).Result)
				{
					await ctx.RespondAsync("You're not logged in.");
                    return;

                }
				else
				{
					List<string> nameTagList = File.ReadAllLines("nametags.txt").ToList();
					Task<int> userID = Helpers.GetUserID(ctx);
					int[] userMisc = Helpers.GetUserAPAndPen(Convert.ToString(userID.Result)).Result;
					int userAP = userMisc[0];
					int userPEN = userMisc[1];
                    int apCost = Config.Instance.nametagcost;
					int penCost = 0;
					if (userAP >= apCost && userPEN >= penCost)
					{
						string niick = "";
						if (nameTagList.Contains(nameTag))
						{
							MySqlCommand getCurrentNick = new MySqlCommand($"SELECT Nickname from {database}.accounts WHERE Id = {userID.Result}", Bot._connection);
							using (MySqlDataReader reader = getCurrentNick.ExecuteReader())
							{
								while (reader.Read())
								{
									niick = reader.GetString("Nickname");
								}
							}
							string replaced = niick;
							string regexPat = "(\\[\\S*\\])";
							Match regexField = Regex.Match(replaced, regexPat);
							if (niick.Contains("[") && niick.Contains("]"))
							{
								replaced = replaced.Replace(regexField.Groups[0].Value, string.Empty);
							}
							string newNick = nameTag + replaced;
							MySqlCommand updateNickname = new MySqlCommand($"UPDATE {database}.accounts SET Nickname = '{newNick}' WHERE Id = {userID.Result}", Bot._connection);
							updateNickname.ExecuteNonQuery();
							await ctx.RespondAsync("Changed Nickname!");
							MySqlCommand updateAPPEN = new MySqlCommand($"UPDATE {database}.players SET AP = AP-{apCost}, PEN = PEN-{penCost} WHERE Id = {userID.Result}", Bot._connection);
							updateAPPEN.ExecuteNonQuery();
                            Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} Bought {nameTag} nametag");
                        }
						else if (nameTagList.Contains(nameTag))
						{
							await ctx.RespondAsync("Nametag does not exist.");
						}
					}
					else if (apCost <= userAP && penCost <= userPEN)
					{
						await ctx.RespondAsync("An Error Occurred");
					}
					else
					{
						await ctx.RespondAsync("Not enough AP/PEN");
					}
				}
			}
			catch (Exception e)
			{
				Logger.Warning(Convert.ToString(e));
				throw;
			}
		}

		[Command("exchange")]
        [Description("Buy Pen with AP")]
        public async Task ExchangeAPToPen(CommandContext ctx, [Description("App Amount to be exchanged with pen")] int APamount)
		{
			try
			{
				if (APamount.ToString() == string.Empty)
				{
					await ctx.RespondAsync("Please enter an amount you want to exchange.");
				}
				else if (!Helpers.IsUserLoggedIn(ctx.Member).Result)
				{
					await ctx.RespondAsync("You're not logged in.");
                    return;
                }
				else
				{
					Task<int> userID = Helpers.GetUserID(ctx);
					int[] userMisc = Helpers.GetUserAPAndPen(Convert.ToString(userID.Result)).Result;
					int userAP = userMisc[0];
					int num = userMisc[1];
					if (APamount > userAP)
					{
						await ctx.RespondAsync($"You only got {userAP}. Cancelled.");
					}
					else
					{
						int calculusmaximus = APamount * Config.Instance.apexchangerate;
						MySqlCommand updatePEN = new MySqlCommand($"UPDATE {database}.players SET PEN = PEN+{calculusmaximus}, AP = AP-{APamount} WHERE Id = {userID.Result}", Bot._connection);
						updatePEN.ExecuteNonQuery();
						await ctx.RespondAsync("Thanks for exchanging! make sure to check your PEN in the profile command.");
                        Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} exchanged {APamount} AP and got {calculusmaximus} Pen");

                    }
                }
			}
			catch (Exception ex)
			{
                Logger.Warning(Convert.ToString(ex));
                throw;
			}
		}

		[Command("profile")]
        [Description("Shows Your Logged Profile")]
        public async Task ShowUserAPPEN(CommandContext ctx)
		{
			try
			{
				if (!Helpers.IsUserLoggedIn(ctx.Member).Result)
				{
					await ctx.RespondAsync("You're not logged in.");
                    return;
                }
				else
				{
					string userID = Convert.ToString(Helpers.GetUserID(ctx).Result);
					int[] user = await Helpers.GetUserAPAndPen(userID);
					DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
					embed.Title = ctx.Member.Username + "#" + ctx.Member.Discriminator;
					embed.Color = DiscordColor.Violet;
					embed.AddField("Current Nickname", Helpers.GetUserNickname(userID).Result);
					embed.AddField("AP", user[0].ToString());
					embed.AddField("PEN", user[1].ToString());
					await ctx.RespondAsync(null, is_tts: false, embed);
				}
			}
			catch (Exception e)
			{
                Logger.Warning(Convert.ToString(e));
                throw;
			}
		}

		[Command("clanlogo")]
        [Description("Add a clan mark to your clan")]
        public async Task ChangeClanPicture(CommandContext ctx)
		{
			try
			{
				if (!Helpers.IsUserLoggedIn(ctx.Member).Result)
				{
					await ctx.RespondAsync("You're not logged in.");
                    return;
                }
				else
				{
					InteractivityModule interactivity = ctx.Client.GetInteractivityModule();
					Task<int> userID = Helpers.GetUserID(ctx);
					int[] userMisc = Helpers.GetUserAPAndPen(Convert.ToString(userID.Result)).Result;
					int userAP = userMisc[0];
					int userPEN = userMisc[1];
					bool cancelled = false;
					int apCost = Config.Instance.clanlogocost;
					int penCost = 0;
					DiscordMember member = ctx.Member;
					Task<DiscordDmChannel> DMchannel = member.CreateDmChannelAsync();
					if (apCost > userAP || penCost > userPEN)
					{
						await DMchannel.Result.SendMessageAsync("You dont have enough AP or PEN for that.");
					}
					else
					{
						await DMchannel.Result.SendMessageAsync("Are you sure that you want to purchase a Clanmark?");
						Task<MessageContext> confirmation = interactivity.WaitForMessageAsync((DiscordMessage x) => x.Author == ctx.Member && x.Content != null && !ctx.Member.IsBot, TimeSpan.FromSeconds(60.0));
						if (confirmation.Result.Message.Content.Contains("no"))
						{
							await DMchannel.Result.SendMessageAsync("Cancelled.");
						}
						else if (!cancelled)
						{
							string clanID = "";
							await DMchannel.Result.SendMessageAsync("What is your Clan name?");
							Task<MessageContext> getClanName = interactivity.WaitForMessageAsync((DiscordMessage x) => x.Author == ctx.Member && !ctx.Member.IsBot, TimeSpan.FromSeconds(60.0));
							string clanName = string.Join(" ", getClanName.Result.Message.Content);
							MySqlCommand getClanID = new MySqlCommand("SELECT Id FROM "+ database +".clubs WHERE Name = '" + clanName + "'", Bot._connection);
							using (MySqlDataReader mySqlDataReader = getClanID.ExecuteReader())
							{
								if (mySqlDataReader.HasRows)
								{
									while (mySqlDataReader.Read())
									{
										clanID = mySqlDataReader.GetString("Id");
									}
								}
								else
								{
									clanID = "NotFound";
								}
								mySqlDataReader.Close();
							}
							if (clanID == "NotFound")
							{
								await DMchannel.Result.SendMessageAsync("Couldn't find this Clan.. Exiting.");
							}
							else
							{
								string ClubID = "";
								string Rank = "";
								MySqlCommand getMasterOfClan = new MySqlCommand($"SELECT * FROM {database}.club_players WHERE PlayerId = {userID.Result} and ClubId = {clanID}", Bot._connection);
								using (MySqlDataReader reader = getMasterOfClan.ExecuteReader())
								{
									if (!reader.HasRows)
									{
										await DMchannel.Result.SendMessageAsync("You are not the Master of that Clan");
										return;
									}
									while (reader.Read())
									{
										ClubID = reader.GetString("ClubId");
										Rank = reader.GetString("Rank");
									}
									reader.Close();
								}
								if (Rank != "1")
								{
									await DMchannel.Result.SendMessageAsync("You're not the leader");
								}
								else
								{
									await DMchannel.Result.SendMessageAsync("Which Border do you want? see #clanmarks");
									Task<MessageContext> background = interactivity.WaitForMessageAsync((DiscordMessage x) => x.Author == ctx.Member && x.Content != null && !ctx.Member.IsBot, TimeSpan.FromSeconds(60.0));
									string result3 = background.Result.Message.Content;
									await DMchannel.Result.SendMessageAsync("Which Symbol do you want?");
									Task<MessageContext> icon = interactivity.WaitForMessageAsync((DiscordMessage x) => x.Author == ctx.Member && x.Content != null && !ctx.Member.IsBot, TimeSpan.FromSeconds(60.0));
									result3 = result3 + "-" + icon.Result.Message.Content + "-";
									await DMchannel.Result.SendMessageAsync("Which Background do you want?");
									Task<MessageContext> shading = interactivity.WaitForMessageAsync((DiscordMessage x) => x.Author == ctx.Member && x.Content != null && !ctx.Member.IsBot, TimeSpan.FromSeconds(60.0));
									result3 += shading.Result.Message.Content;
									MySqlCommand updateLogo = new MySqlCommand("UPDATE "+ database +".clubs SET Icon = '" + result3 + "' WHERE Id = " + ClubID, Bot._connection);
									updateLogo.ExecuteNonQuery();
									MySqlCommand takeAPPEN = new MySqlCommand($"UPDATE {database}.players SET AP = AP-{apCost}, PEN = PEN-{penCost} WHERE Id = {userID.Result}", Bot._connection);
									takeAPPEN.ExecuteNonQuery();
									await DMchannel.Result.SendMessageAsync("Logo was updated successfully!");
                                    Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} Bought {result3} clanlogo for {ClubID} Clan");

                                }
                            }
						}
					}
				}
			}
			catch (Exception error)
			{
                Logger.Warning(Convert.ToString(error));
                throw;
			}
		}

		[RequireRolesAttribute(new string[]
		{
			"Admin"
		})]
		[Command("color")]
		[Description("changes the color of the item")]
		public async Task ChangeColorOfItem(CommandContext ctx, [Description("Item ID?")] string ItemID, [Description("Change da Color")] int colorCode)
		{
			MySqlConnection conn = new MySqlConnection("Server=" + host + ";Database="+ database +";Uid="+ uid + ";SslMode=none;Password="+password);
			conn.Open();
			MySqlCommand checker = new MySqlCommand("SELECT * FROM game.shop_items WHERE Id = '" + ItemID + "'", conn);
			MySqlDataAdapter adapter = new MySqlDataAdapter(checker);
			string changeItem = "UPDATE "+ database +".shop_items SET Colors = @set WHERE Id = @id";
			MySqlCommand updateItem = new MySqlCommand(changeItem, conn);
			updateItem.Parameters.AddWithValue("@id", ItemID);
			updateItem.Parameters.AddWithValue("@set", colorCode);
			adapter.UpdateCommand = updateItem;
			updateItem.ExecuteNonQuery();
			DiscordEmbedBuilder embed = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Goldenrod,
				Author = new DiscordEmbedBuilder.EmbedAuthor
				{
					Name = ctx.Member.DisplayName
				},
				Title = "Color Set",
				Description = $"{ItemID} was set to {colorCode}",
				Timestamp = DateTimeOffset.Now
			};
			conn.Close();
			await ctx.RespondAsync("", is_tts: false, embed);
		}

		[RequireRolesAttribute(new string[]
		{
			"Admin"
		})]
		[Command("set")]
		[Description("removes or adds items from the shop")]
		public async Task AddOrRemoveItemsFromShop(CommandContext ctx, [Description("The Item ID you want to change")] string ItemID, [Description("1 = Enable || 2 = Disable")] int setter)
		{
			try
			{
				MySqlConnection conn = new MySqlConnection("Server=" + host + ";Database="+ database +";Uid="+ uid + ";SslMode=none;Password="+password);
				conn.Open();
				MySqlCommand checker = new MySqlCommand("SELECT * FROM shop_iteminfos WHERE ShopItemId = '" + ItemID + "'", conn);
				MySqlDataAdapter adapter = new MySqlDataAdapter(checker);
				string changeItem = "UPDATE shop_iteminfos SET IsEnabled = @set WHERE ShopItemId = @id";
				MySqlCommand updateItem = new MySqlCommand(changeItem, conn);
				updateItem.Parameters.AddWithValue("@id", ItemID);
				updateItem.Parameters.AddWithValue("@set", setter);
				adapter.UpdateCommand = updateItem;
				updateItem.ExecuteNonQuery();
				DiscordEmbedBuilder embed = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Goldenrod,
					Author = new DiscordEmbedBuilder.EmbedAuthor
					{
						Name = ctx.Member.DisplayName
					},
					Title = "SET"
				};
				string setterString = "";
				switch (setter)
				{
				case 1:
					setterString = "Enabled";
					break;
				case 0:
					setterString = "Disabled";
					break;
				}
				embed.Description = ItemID + " was set to " + setterString;
				embed.Timestamp = DateTimeOffset.Now;
				conn.Close();
				await ctx.RespondAsync("", is_tts: false, embed);
			}
			catch (Exception e)
			{
				await ctx.RespondAsync(e.Message);
			}
		}

		[RequireRolesAttribute(new string[]
		{
			"Bot"
		})]
		[Command("add")]
		[Description("Adds a desired currency to the user's balance")]
		public async Task AddAPOrPenToUser(CommandContext ctx, [Description("Write 'ap' or 'pen' to add the desired currency")] string apPen, [Description("requires the nickname of that user")] string nickname, [Description("the amount you want to add")] int amount)
		{
			try
			{
				MySqlConnection conn3 = new MySqlConnection("Server="+host+";Database="+ database +";Uid="+ uid + ";SslMode=none;Password="+password);
				conn3.Open();
				int playerID_DB = 0;
				MySqlCommand check_for_user_id = new MySqlCommand("SELECT Id FROM accounts WHERE Nickname = '" + nickname + "'", conn3);
				using (MySqlDataReader reader = check_for_user_id.ExecuteReader())
				{
					while (reader.Read())
					{
						string actualID = reader["Id"].ToString();
						playerID_DB = Convert.ToInt32(actualID);
					}
				}
				conn3.Close();
         MySqlConnection conn2 = new MySqlConnection("Server=" + host + ";Database="+ database +";Uid="+ uid + ";SslMode=none;Password="+password);
				conn2.Open();
				MySqlCommand check_user_hasRows = new MySqlCommand($"SELECT * FROM players WHERE Id = '{playerID_DB}'", conn2);
				MySqlDataAdapter adapter = new MySqlDataAdapter(check_user_hasRows);
                if (playerID_DB == 0 )
                {
                    await ctx.RespondAsync("Nickname Doesn't Exist!");
                    return;
                }
				if (apPen == "pen")
				{
					string add_user_pen = "UPDATE players SET PEN = (PEN + @amount) WHERE Id = @id";
					MySqlCommand updatePen = new MySqlCommand(add_user_pen, conn2);
					updatePen.Parameters.AddWithValue("@amount", amount);
					updatePen.Parameters.AddWithValue("@id", playerID_DB);
					adapter.UpdateCommand = updatePen;
					updatePen.ExecuteNonQuery();
					conn2.Close();
				}
				else if (apPen == "ap")
				{
					string add_user_ap = "UPDATE players SET AP = (AP + @amount) WHERE Id = @id";
					MySqlCommand updateAP = new MySqlCommand(add_user_ap, conn2);
					updateAP.Parameters.AddWithValue("@amount", amount);
					updateAP.Parameters.AddWithValue("@id", playerID_DB);
					adapter.UpdateCommand = updateAP;
					updateAP.ExecuteNonQuery();
					conn2.Close();
				}
                await ctx.RespondAsync("", is_tts: false, new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Goldenrod,
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = ctx.Member.DisplayName
                    },
                    Title = "ADD",
                    Description = $"{amount} {apPen.ToUpper()} was added to ID:{playerID_DB} || Nickname:{nickname}",
                    Timestamp = DateTimeOffset.Now

                }) ;
               Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} added {amount} {apPen.ToUpper()} to ID:{playerID_DB} || Nickname:{nickname}");


            }
            catch (Exception e)
			{
				await ctx.RespondAsync(e.Message);
			}
		}

        [RequireRolesAttribute(new string[]
        {
            "Bot"
        })]
        [Command("reduce")]
        [Description("reduce a desired currency from the user's balance")]
        public async Task removeAPOrPen(CommandContext ctx, [Description("Write 'ap' or 'pen' to reduce the desired currency")] string apPen, [Description("requires the nickname of that user")] string nickname, [Description("the amount you want to add")] int amount)
        {
            try
            {
                MySqlConnection conn3 = new MySqlConnection("Server=" + host + ";Database=" + database + ";Uid=" + uid + ";SslMode=none;Password=" + password);
                conn3.Open();
                int playerID_DB = 0;
                MySqlCommand check_for_user_id = new MySqlCommand("SELECT Id FROM accounts WHERE Nickname = '" + nickname + "'", conn3);
                using (MySqlDataReader reader = check_for_user_id.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string actualID = reader["Id"].ToString();
                        playerID_DB = Convert.ToInt32(actualID);
                    }
                }
                conn3.Close();
                MySqlConnection conn2 = new MySqlConnection("Server=" + host + ";Database=" + database + ";Uid=" + uid + ";SslMode=none;Password=" + password);
                conn2.Open();
                MySqlCommand check_user_hasRows = new MySqlCommand($"SELECT * FROM players WHERE Id = '{playerID_DB}'", conn2);
                MySqlDataAdapter adapter = new MySqlDataAdapter(check_user_hasRows);
                if (playerID_DB == 0)
                {
                    await ctx.RespondAsync("Nickname Doesn't Exist!");
                    return;
                }
                if (apPen == "pen")
                {
                    string add_user_pen = "UPDATE players SET PEN = (PEN - @amount) WHERE Id = @id";
                    MySqlCommand updatePen = new MySqlCommand(add_user_pen, conn2);
                    updatePen.Parameters.AddWithValue("@amount", amount);
                    updatePen.Parameters.AddWithValue("@id", playerID_DB);
                    adapter.UpdateCommand = updatePen;
                    updatePen.ExecuteNonQuery();
                    conn2.Close();
                }
                else if (apPen == "ap")
                {
                    string add_user_ap = "UPDATE players SET AP = (AP - @amount) WHERE Id = @id";
                    MySqlCommand updateAP = new MySqlCommand(add_user_ap, conn2);
                    updateAP.Parameters.AddWithValue("@amount", amount);
                    updateAP.Parameters.AddWithValue("@id", playerID_DB);
                    adapter.UpdateCommand = updateAP;
                    updateAP.ExecuteNonQuery();
                    conn2.Close();
                }
                await ctx.RespondAsync("", is_tts: false, new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Goldenrod,
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = ctx.Member.DisplayName
                    },
                    Title = "REDUCE",
                    Description = $"{amount} {apPen.ToUpper()} was reduced from ID:{playerID_DB} || Nickname:{nickname}",
                    Timestamp = DateTimeOffset.Now

                });
                Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} reduced {amount} {apPen.ToUpper()} from ID:{playerID_DB} || Nickname:{nickname}");


            }
            catch (Exception e)
            {
                await ctx.RespondAsync(e.Message);
            }
        }
        [Command("avatar")]
		[RequireRolesAttribute(new string[]
		{
			"Admin"
		})]
		public async Task ChangeAvatarTask(CommandContext ctx, string url)
		{
			Random rnd = new Random();
			string currendRnd = rnd.Next(1, 10000).ToString();
			if (!Directory.Exists("Avatar"))
			{
				Directory.CreateDirectory("Avatar");
			}
			string pngjpg = "";
			if (url.EndsWith(".jpg"))
			{
				pngjpg = ".jpg";
			}
			else if (url.EndsWith(".png"))
			{
				pngjpg = ".png";
			}
			else if (url.EndsWith(".jpeg"))
			{
				pngjpg = ".jpeg";
			}
			try
			{
				Uri myUri = new Uri(url);
				using (WebClient myWebClient = new WebClient())
				{
					myWebClient.DownloadFileAsync(myUri, "Avatar/" + currendRnd + pngjpg);
				}
				await Task.Delay(1000);
				await ctx.Client.EditCurrentUserAsync(null, new FileStream("Avatar/" + currendRnd + pngjpg, FileMode.Open));
				await ctx.RespondAsync("Changed Profile Pic");
			}
			catch (Exception e)
			{
				await ctx.RespondAsync(e.Message);
			}
			finally
			{
				File.Delete("Avatar/" + currendRnd + pngjpg);
			}
		}

		[Command("EXIT")]
		[Description("Only Admins can use this command")]
        [RequireRolesAttribute(new string[]
        {
      "Admin"
        })]
        public async Task BotExit(CommandContext ctx)
		{
			//if (ctx.Member.Id == OwnerID)
			//{
				await ctx.RespondAsync("Bot Exited");
				Environment.Exit(0);
		    //}
		}

		[Command("hackban")]
		[Description("Ban an user by their ID. The user does not need to be in the guild.")]
		[Aliases(new string[]
		{
			"hb"
		})]
		[RequireRolesAttribute(new string[]
		{
      "Bot"
		})]
		public async Task HackBanAsync(CommandContext ctx, [Description("ID of user to ban")] ulong id, [RemainingText] [Description("Reason to ban this member")] string reason = "")
		{
			if (ctx.Member.Id == id)
			{
				await ctx.RespondAsync("You can't do that to yourself! You have so much to live for!");
				return;
			}
			string ustr = $"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id})";
			string rstr = string.IsNullOrWhiteSpace(reason) ? "" : (": " + reason);
			await ctx.Guild.BanMemberAsync(id, 7, ustr + rstr);
			await ctx.RespondAsync("User hackbanned successfully.");
			await ctx.RespondAsync($"Hackbanned ID: {id}\n{rstr}");
            Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} Banned {id}");

        }

        [Command("userinfo")]
		[Description("Shows ingame information of a player")]
        [Aliases(new string[]
        {
            "uf"
        })]
        public async Task SendUserInfo(CommandContext ctx, string nickname)
		{
			MySqlConnection conn3 = new MySqlConnection("Server=" + host + ";Database="+ database +";Uid="+ uid + ";SslMode=none;Password="+password);
			conn3.Open();
			int playerID_DB = 0;
			MySqlCommand check_for_user_id = new MySqlCommand("SELECT Id FROM accounts WHERE Nickname = '" + nickname + "'", conn3);
			using (MySqlDataReader mySqlDataReader = check_for_user_id.ExecuteReader())
			{
				while (mySqlDataReader.Read())
				{
					string actualID = mySqlDataReader["Id"].ToString();
					playerID_DB = Convert.ToInt32(actualID);
				}
			}
            if (playerID_DB == 0)
            {
                await ctx.RespondAsync("Nickname Doesn't Exist!");
                return;
            }
            conn3.Close();
			MySqlConnection conn2 = new MySqlConnection("Server=" + host + ";Database="+ database +";Uid="+ uid + ";SslMode=none;Password="+password);
			conn2.Open();
			MySqlCommand check_user_hasRows = new MySqlCommand($"SELECT * FROM players WHERE Id = '{playerID_DB}'", conn2);
			MySqlDataAdapter adapter = new MySqlDataAdapter(check_user_hasRows);
			string select_user_info = $"SELECT Level, PEN, AP, TotalExperience, TotalMatches, TotalWins, TotalLosses, PlayTime   FROM players WHERE Id = '{playerID_DB}'";
			MySqlCommand userinfo = adapter.SelectCommand = new MySqlCommand(select_user_info, conn2);
			string level = "";
			string pen = "";
			string ap = "";
            string exp = "";
            string matches = "";
            string win = "";
            string lose = "";
            string playtime = "";

            using (MySqlDataReader reader = userinfo.ExecuteReader())
			{
				while (reader.Read())
				{
					level = reader["Level"].ToString();
					pen = reader["PEN"].ToString();
					ap = reader["AP"].ToString();
                    exp = reader["TotalExperience"].ToString();
                    matches = reader["TotalMatches"].ToString();
                    win = reader["TotalWins"].ToString();
                    lose = reader["TotalLosses"].ToString();
                    playtime = reader["PlayTime"].ToString();
                }
			}
			DiscordEmbedBuilder embed = new DiscordEmbedBuilder
			{
				Title = "Info of: " + nickname,
				Description = "Level: " + level + "\nPEN: " + pen + "\nAP: " + ap + "\nEXP: " + exp + "\nMatches: " + matches + "\nWin: " + win + "\nLose: " + lose + "\nPlayTime: " + playtime,
                Color = DiscordColor.Gold,
				Timestamp = DateTime.Now
			};
			await ctx.RespondAsync("", is_tts: false, embed);
            Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} Showed info for {nickname}");

            conn2.Close();
		}



		[Command("uptime")]
		[Aliases(new string[]
		{
			"up"
		})]
        [RequireRolesAttribute(new string[]
        {
      "Bot"
        })]
        [Description("How long the bot has been running for")]
		public async Task GetUptime(CommandContext Context)
		{
			await Context.RespondAsync($"I've been awake for {(DateTime.Now.Hour - Process.GetCurrentProcess().StartTime.Hour)} Hours");
		}


		[Command("kick")]
		[Description("Kicks a user")]
		[RequirePermissions(Permissions.BanMembers)]
        [RequireRolesAttribute(new string[]
        {
      "Bot"
        })]
        public async Task Kick(CommandContext ctx, DiscordMember target, string reason = "No reason provided.")
		{
			await target.RemoveAsync("[Kick by " + ctx.User.Username + "#" + ctx.User.Discriminator + "] " + reason);
			await ctx.RespondAsync($"\ud83d\udd28 Succesfully ejected **{target.Username}#{target.Discriminator} (`{target.Id}`)**");
            Logger.Information($"{ctx.Member.Username}#{ctx.Member.Discriminator} - ID: {ctx.Member.Id} Kicked {target}");

        }

        public async Task ExecuteGroupAsync(CommandContext ctx, [Description("Member to get information about.")] DiscordMember usr)
		{
			await UserInfoAsync(ctx, usr);
		}

		[Command("user")]
		[Aliases(new string[]
		{
			"u"
		})]
        [RequireRolesAttribute(new string[]
        {
      "Bot"
        })]
        [Description("Returns information about a specific user.")]
		public async Task UserInfoAsync(CommandContext ctx, [Description("Member to get information about")] DiscordMember usr)
		{
			DiscordEmbedBuilder embed = new DiscordEmbedBuilder().WithColor(new DiscordColor("#C1272D")).WithTitle($"@{usr.Username}#{usr.Discriminator} - ID: {usr.Id}");
			if (usr.IsBot)
			{
				embed.Title += " __[BOT]__ ";
			}
			if (usr.IsOwner)
			{
				embed.Title += " __[OWNER]__ ";
			}
			embed.Description = "Registered on     : " + usr.CreationTimestamp.DateTime.ToString(CultureInfo.InvariantCulture) + "\nJoined Guild on  : " + usr.JoinedAt.DateTime.ToString(CultureInfo.InvariantCulture);
			StringBuilder roles = new StringBuilder();
			foreach (DiscordRole r in usr.Roles)
			{
				roles.Append("[" + r.Name + "] ");
			}
			if (roles.Length == 0)
			{
				roles.Append("*None*");
			}
			embed.AddField("Roles", roles.ToString());
			Permissions permsobj = usr.PermissionsIn(ctx.Channel);
			string perms = permsobj.ToPermissionString();
			if (((permsobj & Permissions.Administrator) | (permsobj & Permissions.AccessChannels)) == Permissions.None)
			{
				perms = "**[!] User can't see this channel!**\n" + perms;
			}
			if (perms == string.Empty)
			{
				perms = "*None*";
			}
			embed.AddField("Permissions", perms);
			embed.WithFooter($"{ctx.Guild.Name} / #{ctx.Channel.Name} / {DateTime.Now}");
			await ctx.RespondAsync(null, is_tts: false, embed);
		}
	}
}
