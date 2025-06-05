using System;
using System.Diagnostics;
using bot_helping_with_tasks_bot;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot_for_tasks
{
    class selectedCommand
    {
        static void Main(string[] args)
        {
            var client = new TelegramBotClient("7674284970:AAGQ7t5QqPLJ8gYuFedkkXEFitzPzVuek1k");
            client.StartReceiving(Update, Error);
            Console.ReadLine();
        }

        async static Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;
            ComputerControl computerControl = new ComputerControl();
            if (message.Text != null)
            {
                if (message.Text == "/start")
                {
                    var keyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "Перезагрузка", "Выключение" },
                        new KeyboardButton[] { "Состояние", "Процессы" },
                        new KeyboardButton[] { "Удалить процесс", "Запустить процесс" }
                    })
                    {
                        ResizeKeyboard = true // Автоматическая подстройка размера клавиатуры
                    };

                    // Отправка сообщения с клавиатурой
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите команду:", replyMarkup: keyboard);
                    return;
                }
                else
                {
                    // Сохраняем выбор пользователя в переменную
                    string selectedCommand = message.Text;

                    // Обработка выбора пользователя
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"Вы выбрали: {selectedCommand}");

                    // Здесь вы можете добавить дополнительную логику в зависимости от выбранной команды
                    switch (selectedCommand)
                    {
                        case "Перезагрузка":
                            computerControl.Reboot();
                            break;
                        case "Выключение":
                            computerControl.Shutdown();
                            break;
                        case "Состояние":
                            var statusMessages = computerControl.Status();
                            string combinedMessage = string.Join("\n", statusMessages);
                            await botClient.SendTextMessageAsync(message.Chat.Id, combinedMessage);
                            break;
                        case "Процессы":
                            // Логика для команды 4
                            break;
                        case "Удалить процесс":
                            //
                            break;
                        case "Запустить процесс":
                            //
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private static async Task Error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}