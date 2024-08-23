using System;
using System.Net;
using System.Net.Http;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Requests;
using Newtonsoft.Json;
using System.Diagnostics;
using Faceit_TelegramBot.util;
using Faceit_TelegramBot.Model;
using Faceit_TelegramBot.api;
using static System.IO.File;
using Faceit_TelegramBot.model;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace FaceitTelegramBot
{
    class Program
    {
        private static FaceitAPI API;
        private static TelegramBotClient _client;
        private static CancellationTokenSource _cts;
        private static SearchResponseModel lastRequest;
        private static string dataDirectory = string.Empty;
        private static long? GROUPCHAT_ID = 0;
        private static string? BOT_TOKEN = string.Empty;

        public static void Main(string[] args)
        {
            Console.Title = "Faceit Search Telegram Bot";
            Console.WriteLine("[+] Bot is online!");

            DotNetEnv.Env.Load();
            BOT_TOKEN = Environment.GetEnvironmentVariable("BOT_TOKEN");

            if (String.IsNullOrEmpty(BOT_TOKEN))
            {
                Console.WriteLine("Please add necessary environment variables");
                return;
            }

            if (!long.TryParse(Environment.GetEnvironmentVariable("GROUPCHAT_ID"), out long res))
            {
                return;
            }

            GROUPCHAT_ID = res;

            string currentDirectory = Directory.GetCurrentDirectory();
            string projectRoot = currentDirectory;

            while (!Directory.Exists(Path.Combine(projectRoot, "data")))
            {
                projectRoot = Directory.GetParent(projectRoot)?.FullName;
                if (projectRoot == null)
                {
                    throw new Exception("Project root directory not found.");
                }
            }

            dataDirectory = Path.Combine(projectRoot, "data");

            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            _cts = new ();
            _client = new(BOT_TOKEN);
            API = new();

            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = []
            };

            _client.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cts.Token
            );

            Process.GetCurrentProcess().WaitForExit();
            _cts.Cancel();
        }

        static async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Type == UpdateType.Message && update.Message != null)
                {
                    Message? message = update.Message;

                    if (message != null)
                    {
                        await Console.Out.WriteLineAsync($"[+] Received query: '{message.Text!}' by user: '{message.From!.Username}' - '{message.From!.Id}'");

                        await SaveToAnalytics(message);
                        await ProcessQuery(message.Text!);
                    }
                }
                else if(update.Type == UpdateType.CallbackQuery || update.Message != null)
                {
                    CallbackQuery? callbackQuery = update.CallbackQuery;

                    if(callbackQuery != null)
                    {
                        await client.AnswerCallbackQueryAsync(callbackQuery.Id, $"Recevied: {callbackQuery.Data}");

                        string id = string.Empty;
                        string[] splitID = callbackQuery.Data!.Split(':');

                        if(splitID.Length == 3) 
                        {
                            id = splitID[2];
                        }

                        switch (splitID[1])
                        {
                            case "players":
                                await HandlePlayersCallback(callbackQuery, id);
                                break;
                            case "teams":
                                await HandleTeamsCallback(callbackQuery, id);
                                break;
                            case "hubs":
                                await HandleHubsCallback(callbackQuery, id);
                                break;
                            case "tournaments":
                                await HandleTournamentsCallback(callbackQuery, id);
                                break;
                            case "organizers":
                                await HandleOrganizersCallback(callbackQuery, id);
                                break;
                            default:
                                await client.SendTextMessageAsync((long)GROUPCHAT_ID!, "Unknown callback data.");
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"Main.HandleUpdateAsync() Error -> {ex.Message}");
            }
        }

        static async Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
        {
            await Console.Out.WriteLineAsync($"ERROR OCCURED -> {exception.Message}\nSTACKTRACE:\n{exception.StackTrace}");
        }

        static async Task ProcessQuery(string query)
        {
            if (string.IsNullOrEmpty(query)) return;
            int limit = 0;
            int offset = 0;
            int startIndex = 0;
            int endIndex = query.Length;


            if (query.Contains('[') && query.Contains("]"))
            {
                int indexOfBracketOpen = query.IndexOf('[');
                int indexOfBracketClosing = query.IndexOf(']');
                endIndex = indexOfBracketOpen;

                string numberString = query.Substring(indexOfBracketOpen + 1, indexOfBracketClosing - indexOfBracketOpen - 1);

                if (int.TryParse(numberString, out int res))
                {
                    limit = res;
                    await Console.Out.WriteLineAsync($"Parsed limit: {res}");
                }

                int indexOfOffsetBracketOpen = query.IndexOf('[', indexOfBracketClosing + 1);
                int indexOfOffsetBracketClosing = query.IndexOf(']', indexOfBracketClosing + 1);

                if (indexOfOffsetBracketOpen != -1 && indexOfOffsetBracketClosing != -1)
                {
                    string offsetString = query.Substring(indexOfOffsetBracketOpen + 1, indexOfOffsetBracketClosing - indexOfOffsetBracketOpen - 1);

                    if (int.TryParse(offsetString, out int offsetResult))
                    {
                        offset = offsetResult;
                        await Console.Out.WriteLineAsync($"Parsed offset: {offsetResult}");
                    }
                }
            }

            SearchResponseModel? response = await API.Search(offset, limit, query.Substring(startIndex, endIndex));

            if (response != null)
            {
                lastRequest = response;

                string formatMessage() => $"Time Sent: {DateTimeOffset.FromUnixTimeMilliseconds(response.time).UtcDateTime}\n" +
                    $"--------------SERVER----------------\n" +
                    $"Offset: {response.payload.offset}\n" +
                    $"Limit: {response.payload.limit}\n" +
                    $"Environment: {response.env}\n" +
                    $"Version: {response.version}\n" +
                    $"---------------TOTAL----------------\n" +
                    $"Players: {response.payload.players.total_count}\n" +
                    $"Teams: {response.payload.teams.total_count}\n" +
                    $"Hubs: {response.payload.hubs.total_count}\n" +
                    $"Tournaments: {response.payload.tournaments.total_count}\n" +
                    $"Organizers: {response.payload.organizers.total_count}\n";

                await Text.SendMessageWithInline(_client, (long)GROUPCHAT_ID!, formatMessage(), response, _cts.Token);
            }
        }

        static async Task SaveToAnalytics(Message msg)
        {
            string path = Path.Combine(dataDirectory, "user_activity.json");

            if (!Exists(path))
            {
                await WriteAllTextAsync(path, "[]");
            }

            string readContent = await ReadAllTextAsync(path);

            AnalyticsModel save = new()
            {
                time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                telegram_id = msg.From!.Id,
                telegram_username = msg.From!.Username,
                query = msg.Text,
            };

            JArray json;

            try
            {
                json = JArray.Parse(readContent);
            }
            catch (JsonReaderException)
            {
                json = new JArray();
            }

            JObject serializedSave = JObject.Parse(JsonConvert.SerializeObject(save));

            json.Add(serializedSave);

            await WriteAllTextAsync(path, json.ToString());
        }

        private static async Task HandleOrganizersCallback(CallbackQuery query, string id = "")
        {
            if (lastRequest == null)
            {
                await _client.SendTextMessageAsync(query.Message!.Chat.Id, "No previous request data found.");
            }
            else
            {
                if (query.Data == "faceit:organizers")
                {
                    List<List<InlineKeyboardButton>> rowsList = new();
                    List<InlineKeyboardButton> buttonRow = new();

                    foreach (ResultOrg organizer in lastRequest.payload.organizers.results!)
                    {
                        buttonRow.Add(InlineKeyboardButton.WithCallbackData(organizer.name, $"faceit:organizers:{organizer.guid}"));

                        if (buttonRow.Count == 1)
                        {
                            rowsList.Add(new List<InlineKeyboardButton>(buttonRow));
                            buttonRow = new List<InlineKeyboardButton>();
                        }
                    }

                    InlineKeyboardMarkup inlineKeyboard = new(rowsList.ToArray());

                    await _client.SendTextMessageAsync(
                        chatId: (long)GROUPCHAT_ID!,
                        text: "Choose Organizer: ",
                        replyMarkup: inlineKeyboard
                    );
                }
                else
                {
                    ResultOrg orgRes = null!;
                    string msgToSend = string.Empty;

                    foreach (ResultOrg org in lastRequest.payload.organizers.results!)
                    {
                        if (org.guid == id)
                        {
                            orgRes = org;
                            break;
                        }
                    }

                    msgToSend += $"ID/GUID: {id}\n" +
                        $"Name: {orgRes!.name}\n" +
                        $"Partner: {orgRes.partner}\n" +
                        $"Active: {orgRes.active}\n\n" +
                        $"Games:\n{string.Join(", ", orgRes.games!)}\n\n" +
                        $"Countries:\n:{string.Join(", ", orgRes.countries!)}\n\n" +
                        $"Regions:\n{string.Join(", ", orgRes.regions!)}";


                    // fix (sometime bugs)
                    //var buttons = new List<InlineKeyboardButton[]>
                    //{
                    //    ([InlineKeyboardButton.WithUrl("Avatar", hubRes.avatar)])
                    //};

                    //var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                    await _client.SendTextMessageAsync(
                       chatId: (long)GROUPCHAT_ID!,
                       text: msgToSend,
                       //replyMarkup: inlineKeyboard,
                       cancellationToken: _cts.Token
                   );
                }
            }
        }

        private static async Task HandleTournamentsCallback(CallbackQuery query, string id = "")
        {
            if (lastRequest == null)
            {
                await _client.SendTextMessageAsync(query.Message!.Chat.Id, "No previous request data found.");
            }
            else
            {
                if (query.Data == "faceit:tournaments")
                {
                    List<List<InlineKeyboardButton>> rowsList = new();
                    List<InlineKeyboardButton> buttonRow = new();

                    foreach (ResultTour tournament in lastRequest.payload.tournaments.results!)
                    {
                        buttonRow.Add(InlineKeyboardButton.WithCallbackData(tournament.name, $"faceit:tournaments:{tournament.guid}"));

                        if (buttonRow.Count == 1)
                        {
                            rowsList.Add(new List<InlineKeyboardButton>(buttonRow));
                            buttonRow = new List<InlineKeyboardButton>();
                        }
                    }

                    InlineKeyboardMarkup inlineKeyboard = new(rowsList.ToArray());

                    await _client.SendTextMessageAsync(
                        chatId: (long)GROUPCHAT_ID!,
                        text: "Choose Tournament: ",
                        replyMarkup: inlineKeyboard
                    );
                }
                else
                {
                    ResultTour tourRes = null!;
                    string msgToSend = string.Empty;

                    foreach (ResultTour hub in lastRequest.payload.tournaments.results!)
                    {
                        if (hub.id == id)
                        {
                            tourRes = hub;
                            break;
                        }
                    }

                    msgToSend += $"ID/GUID: {id}\n" +
                        $"Name: {tourRes!.name}\n" +
                        $"Region: {tourRes.region}\n" +
                        $"Type: {tourRes.type}\n" +
                        $"Status: {tourRes.status}\n" +
                        $"StartDate: {tourRes.start_date}\n" +
                        $"Number of Players: {tourRes.number_of_players}\n" +
                        $"Number of players Joined: {tourRes.number_of_players_joined}\n" +
                        $"Number of Players CheckedIn: {tourRes.number_of_players_checkedin}\n" +
                        $"Number of Players Participants: {tourRes.number_of_players_participants}\n" +
                        $"Min Skill Level: {tourRes.skill_level_min}\n" +
                        $"Max Skill Level: {tourRes.skill_level_max}\n" +
                        $"Min Skill: {tourRes.min_skill}\n" +
                        $"Max Skill: {tourRes.max_skill}\n" +
                        $"Join: {tourRes.join}\n" +
                        $"Membership Type: {tourRes.membership_type}\n" +
                        $"Team Size: {tourRes.team_size}\n" +
                        $"Prize Type: {tourRes.prize_type}\n" +
                        $"Total Prize Type: {tourRes.total_prize_label}\n" +
                        $"Subscriptions Count: {tourRes.subscriptions_count}";

                    // fix (sometime bugs)
                    //var buttons = new List<InlineKeyboardButton[]>
                    //{
                    //    ([InlineKeyboardButton.WithUrl("Avatar", hubRes.avatar)])
                    //};

                    //var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                    await _client.SendTextMessageAsync(
                       chatId: (long)GROUPCHAT_ID!,
                       text: msgToSend,
                       //replyMarkup: inlineKeyboard,
                       cancellationToken: _cts.Token
                   );
                }
            }
        }

        private static async Task HandleHubsCallback(CallbackQuery query, string id = "")
        {
            if (lastRequest == null)
            {
                await _client.SendTextMessageAsync(query.Message!.Chat.Id, "No previous request data found.");
            }
            else
            {
                if (query.Data == "faceit:hubs")
                {
                    List<List<InlineKeyboardButton>> rowsList = new();
                    List<InlineKeyboardButton> buttonRow = new();

                    foreach (ResultHub hub in lastRequest.payload.hubs.results!)
                    {
                        buttonRow.Add(InlineKeyboardButton.WithCallbackData(hub.name, $"faceit:hubs:{hub.guid}"));

                        if (buttonRow.Count == 1)
                        {
                            rowsList.Add(new List<InlineKeyboardButton>(buttonRow));
                            buttonRow = new List<InlineKeyboardButton>();
                        }
                    }

                    InlineKeyboardMarkup inlineKeyboard = new(rowsList.ToArray());

                    await _client.SendTextMessageAsync(
                        chatId: (long)GROUPCHAT_ID!,
                        text: "Choose Hub: ",
                        replyMarkup: inlineKeyboard
                    );
                }
                else
                {
                    ResultHub hubRes = null!;
                    string msgToSend = string.Empty;

                    foreach (ResultHub hub in lastRequest.payload.hubs.results!)
                    {
                        if (hub.id == id)
                        {
                            hubRes = hub;
                            break;
                        }
                    }

                    msgToSend += $"ID/GUID: {id}\n" +
                        $"Name: {hubRes!.name}\n" +
                        $"Region: {hubRes.region}\n" +
                        $"Min Skill Level: {hubRes.minSkillLevel}\n" +
                        $"Max Skill Level: {hubRes.maxSkillLevel}\n" +
                        $"Number of Members: {hubRes.membersCount}\n" +
                        $"Join: {hubRes.join}\n\n" +
                        $"Organizer:\n" +
                        $"ID: {hubRes.organizer.id}\n" +
                        $"Type: {hubRes.organizer.type}\n" +
                        $"Name: {hubRes.organizer.name}";

                    // fix (sometime bugs)
                    //var buttons = new List<InlineKeyboardButton[]>
                    //{
                    //    ([InlineKeyboardButton.WithUrl("Avatar", hubRes.avatar)])
                    //};

                    //var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                    await _client.SendTextMessageAsync(
                       chatId: (long)GROUPCHAT_ID!,
                       text: msgToSend,
                       //replyMarkup: inlineKeyboard,
                       cancellationToken: _cts.Token
                   );
                }
            }
        }

        private static async Task HandleTeamsCallback(CallbackQuery query, string id = "")
        {
            if (lastRequest == null)
            {
                await _client.SendTextMessageAsync(query.Message!.Chat.Id, "No previous request data found.");
            }
            else
            {
                if (query.Data == "faceit:teams")
                {
                    List<List<InlineKeyboardButton>> rowsList = new();
                    List<InlineKeyboardButton> buttonRow = new();

                    foreach (ResultTeam team in lastRequest.payload.teams.results!)
                    {
                        buttonRow.Add(InlineKeyboardButton.WithCallbackData(team.name, $"faceit:teams:{team.id}"));

                        if (buttonRow.Count == 1)
                        {
                            rowsList.Add(new List<InlineKeyboardButton>(buttonRow));
                            buttonRow = new List<InlineKeyboardButton>();
                        }
                    }

                    InlineKeyboardMarkup inlineKeyboard = new(rowsList.ToArray());

                    await _client.SendTextMessageAsync(
                        chatId: (long)GROUPCHAT_ID!,
                        text: "Choose Team: ",
                        replyMarkup: inlineKeyboard
                    );
                }
                else
                {
                    ResultTeam teamRes = null!;
                    string msgToSend = string.Empty;

                    foreach (ResultTeam team in lastRequest.payload.teams.results!)
                    {
                        if (team.id == id)
                        {
                            teamRes = team;
                            break;
                        }
                    }

                    msgToSend += $"ID/GUID: {id}\n" +
                        $"Name: {teamRes!.name}\n" +
                        $"Game: {teamRes.game}\n" +
                        $"Tag: {teamRes.tag}\n" +
                        $"Type: {teamRes.type}\n" +
                        $"Verified: {teamRes.verified}";

                    // fix (sometime bugs)
                    //var buttons = new List<InlineKeyboardButton[]>
                    //{
                    //    ([InlineKeyboardButton.WithUrl("Avatar", teamRes.avatar)])
                    //};

                    //var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                    await _client.SendTextMessageAsync(
                       chatId: (long)GROUPCHAT_ID!,
                       text: msgToSend,
                       //replyMarkup: inlineKeyboard,
                       cancellationToken: _cts.Token
                   );
                }
            }
        }

        private static async Task HandlePlayersCallback(CallbackQuery query, string id = "")
        {
            if (lastRequest == null)
            {
                await _client.SendTextMessageAsync(query.Message!.Chat.Id, "No previous request data found.");
            }
            else
            {
                if(query.Data == "faceit:players")
                {
                    List<List<InlineKeyboardButton>> rowsList = new();
                    List<InlineKeyboardButton> buttonRow = new();

                    foreach (ResultPlayer player in lastRequest.payload.players.results!)
                    {
                        buttonRow.Add(InlineKeyboardButton.WithCallbackData(player.nickname, $"faceit:players:{player.id}"));

                        if (buttonRow.Count == 1)
                        {
                            rowsList.Add(new List<InlineKeyboardButton>(buttonRow));
                            buttonRow = new List<InlineKeyboardButton>();
                        }
                    }

                    InlineKeyboardMarkup inlineKeyboard = new(rowsList.ToArray());

                    await _client.SendTextMessageAsync(
                        chatId: (long)GROUPCHAT_ID!,
                        text: "Choose Player: ",
                        replyMarkup: inlineKeyboard
                    );
                }
                else
                {
                    ResultPlayer playerRes = null!;
                    string msgToSend = string.Empty;

                    foreach (ResultPlayer player in lastRequest.payload.players.results!)
                    {
                        if(player.id == id)
                        {
                            playerRes = player; 
                            break;
                        }
                    }

                    msgToSend += $"ID/GUID: {id}\n" +
                        $"Nickname: {playerRes!.nickname}\n" +
                        $"Status: {playerRes.status}\n" +
                        $"Country: {playerRes.country}\n" +
                        $"Verified: {playerRes.verified}\n" +
                        $"Verification LVL: {playerRes.verification_level}\n\nGames Played:\n";

                    foreach (Faceit_TelegramBot.Model.Game game in playerRes.games)
                    {
                        msgToSend += $"{game.name.ToUpper()} - Level {game.skill_level}\n";
                    }

                    // fix (sometime bugs)
                    //var buttons = new List<InlineKeyboardButton[]>
                    //{
                    //    ([InlineKeyboardButton.WithUrl("Avatar", playerRes.avatar)])
                    //};

                    //var inlineKeyboard = new InlineKeyboardMarkup(buttons);

                    await _client.SendTextMessageAsync(
                       chatId: (long)GROUPCHAT_ID!,
                       text: msgToSend,
                       //replyMarkup: inlineKeyboard,
                       cancellationToken: _cts.Token
                   );
                }
            }
        }
    }
}
