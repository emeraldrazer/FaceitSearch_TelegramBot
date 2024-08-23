using Faceit_TelegramBot.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Faceit_TelegramBot.util
{
    public static class Text
    {
        private static readonly int MAX_LENGTH_MESSAGE = 4096;

        public static IEnumerable<string> SplitMessageIntoParts(string message, int maxPartLength)
        {
            List<string> parts = new List<string>();
            int startIndex = 0;

            while (startIndex < message.Length)
            {
                int length = Math.Min(maxPartLength, message.Length - startIndex);
                int endIndex = startIndex + length;

                if (endIndex < message.Length && message[endIndex] != ' ' && message[endIndex - 1] != ' ')
                {
                    int lastSpaceIndex = message.LastIndexOf(' ', endIndex, length);
                    if (lastSpaceIndex > startIndex)
                    {
                        endIndex = lastSpaceIndex;
                    }
                }

                parts.Add(message.Substring(startIndex, endIndex - startIndex));
                startIndex = endIndex;
            }

            return parts;
        }

        public static async Task SendMessage(ITelegramBotClient client, long chatId, string responseString, CancellationToken cancellationToken)
        {
            if (responseString.Length <= MAX_LENGTH_MESSAGE)
            {
                await client.SendTextMessageAsync(chatId, responseString, cancellationToken: cancellationToken);
            }
            else
            {
                IEnumerable<string> messageParts = SplitMessageIntoParts(responseString, MAX_LENGTH_MESSAGE);

                foreach (string messagePart in messageParts)
                {
                    await client.SendTextMessageAsync(chatId, messagePart, cancellationToken: cancellationToken);
                }
            }
        }

        public static async Task SendMessageWithInline(ITelegramBotClient client, long chatId, string responseString, SearchResponseModel response, CancellationToken cancellationToken)
        {
            var buttons = new List<InlineKeyboardButton[]>();

            if (response.payload.players.total_count > 0)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Players", "faceit:players") });
            }

            if (response.payload.teams.total_count > 0)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Teams", "faceit:teams") });
            }

            if (response.payload.hubs.total_count > 0)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Hubs", "faceit:hubs") });
            }

            if (response.payload.tournaments.total_count > 0)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Tournaments", "faceit:tournaments") });
            }

            if (response.payload.organizers.total_count > 0)
            {
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Organizers", "faceit:organizers") });
            }

            buttons.Add(new[] { InlineKeyboardButton.WithUrl("Visit Response", response.url) });

            var inlineKeyboard = new InlineKeyboardMarkup(buttons);

            await client.SendTextMessageAsync(
               chatId: chatId,
               text: responseString,
               replyMarkup: inlineKeyboard,
               cancellationToken: cancellationToken
           );
        }

        public static async Task SendMessageWithCustomInline(ITelegramBotClient client, long chatId, string responseString, CancellationToken cancellationToken)
        {

        }
    }
}
