using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClientBot
{
    public class ClientBot
    {
        private readonly ITelegramBotClient botClient;
        private readonly ApiService _apiService;
        private static Dictionary<long, string> _userOrders = new(); // Словарь для хранения заказов

        public ClientBot(string token, ApiService apiService)
        {
            botClient = new TelegramBotClient(token);
            _apiService = apiService;
        }

        public void StartReceiving()
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            botClient.StartReceiving(
                updateHandler: UpdateHandler,
                pollingErrorHandler: ErrorHandler,
                receiverOptions: receiverOptions
            );

            Console.WriteLine("Бот запущен");
            Console.ReadKey();
            Console.WriteLine("Бот остановлен");
        }

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            // Обработка фото-заказов
            if (update.Message?.Photo != null)
            {
                var photoFileId = update.Message.Photo.Last().FileId;
                var userId = update.Message.From.Id;

                _userOrders[userId] = photoFileId;

                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "📝 Укажите название товара и контакты для связи:",
                    replyMarkup: new ForceReplyMarkup { Selective = true },
                    cancellationToken: token
                );
                return;
            }

            if (update.Message?.ReplyToMessage != null && _userOrders.ContainsKey(update.Message.From.Id))
            {
                var orderDetails = update.Message.Text;
                var photoFileId = _userOrders[update.Message.From.Id];

                await SendOrderToAdmin(botClient, userId: update.Message.From.Id, photoFileId, orderDetails);
                _userOrders.Remove(update.Message.From.Id);
                return;
            }

            // Остальная логика обработки сообщений
            var message = update.Message;
            if (message?.Text != null)
            {
                if (message.Text == "/start")
                {
                    string welcomeMessage = "Здравствуйте и добро пожаловать в Azrv_BOT..."; // ваш текст
                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("Меню", "contact_manger") }
                    });
                    await botClient.SendTextMessageAsync(message.Chat.Id, welcomeMessage, replyMarkup: inlineKeyboard, cancellationToken: token);
                }
                // ... остальные обработчики текстовых команд
            }
            else if (update?.CallbackQuery != null)
            {
                await HandleCallbackQuery(botClient, update.CallbackQuery, token);
            }
        }

        private async Task SendOrderToAdmin(ITelegramBotClient botClient, long userId, string photoFileId, string orderDetails)
        {
            const long adminChatId = 8105555543; // Замените на реальный ID админа

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Подтвердить", $"approve_{userId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Отклонить", $"reject_{userId}")
                }
            });

            await botClient.SendPhotoAsync(
                chatId: adminChatId,
                photo: InputFile.FromFileId(photoFileId),
                caption: $"🛒 Новый заказ!\n\n👤 Пользователь: {userId}\n📋 Детали: {orderDetails}",
                replyMarkup: keyboard
            );
        }

        private async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken token)
        {
            var chatId = callbackQuery.Message?.Chat.Id;

            if (callbackQuery.Data == "contact_manger")
            {
                var menuKeyboard = new ReplyKeyboardMarkup(
                    new[]
                    {
                        new [] { new KeyboardButton("Сделать заказ"), new KeyboardButton("Узнать Курс") },
                        new[] { new KeyboardButton("Информация") },
                        new[] { new KeyboardButton("Приобрести товары в наличии")}
                    })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = false
                };

                await botClient.SendTextMessageAsync(chatId, "Выберите действия:", replyMarkup: menuKeyboard, cancellationToken: token);
            }
            else if (callbackQuery.Data.StartsWith("approve_") || callbackQuery.Data.StartsWith("reject_"))
            {
                var userId = long.Parse(callbackQuery.Data.Split('_')[1]);
                var action = callbackQuery.Data.StartsWith("approve_") ? "одобрен" : "отклонён";

                await botClient.SendTextMessageAsync(
                    chatId: userId,
                    text: $"Ваш заказ {action}!",
                    cancellationToken: token
                );

                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: $"Вы {action} заказ",
                    cancellationToken: token
                );
            }
        }

        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine("Ошибка " + exception.Message);
            await Task.CompletedTask;
        }
    }
}