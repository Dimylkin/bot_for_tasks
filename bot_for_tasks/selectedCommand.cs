using System.Diagnostics;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace bot_for_tasks
{
    class selectedCommand
    {
        private readonly computerControl? _;

        static void Main(string[] args)
        {
            var client = new TelegramBotClient("8088463398:AAEPRGD9tS804ODqnAqbt69RbrWt887iOGU");
            client.StartReceiving(Update, Error);
            Console.ReadLine();
        }

        private static Dictionary<long, bool> _authenticatedUsers = new Dictionary<long, bool>();
        private static Dictionary<long, string> _awaitingInput = new Dictionary<long, string>();

        async static Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;
            if (message?.Text == null) return;

            long chatId = message.Chat.Id;

            if (!_authenticatedUsers.TryGetValue(chatId, out var isAuthenticated) || !isAuthenticated)
            {
                if (!_awaitingInput.ContainsKey(chatId))
                {
                    await botClient.SendMessage(chatId, "🔒 Введите пароль для доступа:");
                    _awaitingInput[chatId] = "waiting_password";
                    return;
                }

                if (_awaitingInput[chatId] == "waiting_password")
                {
                    string userPassword = message.Text;

                    if (computerControl.CheckPassword(userPassword))
                    {
                        _authenticatedUsers[chatId] = true;
                        _awaitingInput.Remove(chatId);
                        await botClient.SendMessage(chatId, "✅ Доступ разрешён!");
                        await ShowMainMenu(botClient, chatId);
                    }
                    else
                    {
                        await botClient.SendMessage(chatId, "❌ Неверный пароль!");
                    }
                }
                return;
            }
            switch (message.Text)
            {
                case "/start":
                    await ShowMainMenu(botClient, chatId);
                    break;
                case "Перезагрузка":
                    computerControl.Reboot();
                    await botClient.SendMessage(chatId, "🔄 Компьютер перезагружается...");
                    break;
                case "Выключение":
                    computerControl.Shutdown();
                    await botClient.SendMessage(chatId, "⏻ Компьютер выключается...");
                    break;
                case "Состояние":
                    var choise_condition = new ReplyKeyboardMarkup(
                    [
                        new[] { new KeyboardButton("Подробный"), new KeyboardButton("Базовый") },
                        new[] { new KeyboardButton("Назад") }
                    ])
                    {
                        ResizeKeyboard = true
                    };

                    await botClient.SendMessage(chatId, "Какой вывод вам нужен?", replyMarkup: choise_condition);
                    _awaitingInput[chatId] = "waiting_status_choice";
                    break;
                case "Процессы":
                    var choise_process = new ReplyKeyboardMarkup(
                    [
                        new[] { new KeyboardButton("Запущенные процессы"), new KeyboardButton("Информация по процессу") },
                        new[] { new KeyboardButton("Завершить процесс"), new KeyboardButton("Запустить процесс") },
                        new[] { new KeyboardButton("Назад") },
                    ])
                    {
                        ResizeKeyboard = true
                    };
                    await botClient.SendMessage(chatId, "Выберите действие с процессом:", replyMarkup: choise_process);
                    _awaitingInput[chatId] = "waiting_process_choice";
                    break;
                default:
                    if (_awaitingInput.TryGetValue(chatId, out var inputType))
                    {
                        switch (inputType)
                        {
                            case "waiting_status_choice":
                                await HandleStatusChoice(botClient, chatId, message);
                                break;
                            case "waiting_process_choice":
                                await HandleProcessChoice(botClient, chatId, message);
                                break;
                            case "waiting_process_info":
                                string input = message.Text.Trim();

                                List<string> processInfo;

                                if (int.TryParse(input, out int processIds))
                                {
                                    processInfo = computerControl.InfoProcess(id: processIds);
                                }
                                else
                                {
                                    processInfo = computerControl.InfoProcess(name: input);
                                }

                                if (processInfo.Count == 0)
                                {
                                    await botClient.SendMessage(chatId, "Не удалось получить информацию о процессе");
                                }
                                else if (processInfo.Count == 1)
                                {
                                    await botClient.SendMessage(chatId, processInfo[0]);
                                }
                                else
                                {
                                    StringBuilder response = new StringBuilder();
                                    response.AppendLine($"Найдено процессов: {processInfo.Count / 2}");

                                    foreach (string info in processInfo)
                                    {
                                        if (info == "────────────────────")
                                        {
                                            response.AppendLine(info);
                                        }
                                        else
                                        {
                                            response.AppendLine(info);
                                        }
                                    }

                                    string responseText = response.ToString();
                                    if (responseText.Length > 4000)
                                    {
                                        await botClient.SendMessage(chatId, responseText.Substring(0, 4000));
                                        await botClient.SendMessage(chatId, "И ещё " + (processInfo.Count / 2 - 3) + " процессов...");
                                    }
                                    else
                                    {
                                        await botClient.SendMessage(chatId, responseText);
                                    }
                                }

                                await ShowMainMenu(botClient, chatId);
                                break;
                            case "waiting_process_end":
                                string input_process_close = message.Text.Trim();

                                if (int.TryParse(input_process_close, out int processId))
                                {
                                    string info = computerControl.KillProcess(processId: processId);
                                    await botClient.SendMessage(chatId, info);
                                }
                                else
                                {
                                    string info = computerControl.KillProcess(processName: input_process_close);
                                    await botClient.SendMessage(chatId, info);
                                }

                                await ShowMainMenu(botClient, chatId);
                                break;
                            case "waiting_process_start":
                                string filePath = message.Text.Trim();
                                string result = computerControl.StartProcess(filePath);

                                await botClient.SendMessage(chatId, result);
                                await ShowMainMenu(botClient, chatId);
                                break;
                        }
                    }
                    break;
            }
        }


        private static async Task HandleStatusChoice(ITelegramBotClient botClient, long chatId, Message message)
        {
            List<string> statusMessages;
            bool level;
            if (message.Text == "Базовый")
            {
                level = false;
                statusMessages = computerControl.Status(level);
            }
            else if (message.Text == "Подробный")
            {
                level = true;
                statusMessages = computerControl.Status(level);
            }
            else if (message.Text == "Назад")
            {
                await ShowMainMenu(botClient, chatId);
                return;
            }
            else
            {
                var operation = await botClient.SendMessage(chatId, "Пожалуйста, выберите один из предложенных вариантов");
                return;
            }

            _awaitingInput.Remove(chatId);
            await botClient.SendMessage(chatId, string.Join("\n", statusMessages));
            await ShowMainMenu(botClient, chatId);
        }
        private static async Task HandleProcessChoice(ITelegramBotClient botClient, long chatId, Message message)
        {
            if (message.Text == "Запущенные процессы")
            {
                List<string> processes = computerControl.Processes("Запущенные процессы");

                string tempFile = Path.GetTempFileName();
                await File.WriteAllLinesAsync(tempFile, processes);

                await using (var stream = System.IO.File.OpenRead(tempFile))
                {
                    await botClient.SendDocument(chatId, InputFile.FromStream(stream, "processes.txt"), caption: "📁 Полный список процессов");
                }

                File.Delete(tempFile);
                await ShowMainMenu(botClient, chatId);
            }
            else if (message.Text == "Информация по процессу")
            {
                await botClient.SendMessage(chatId, "Введите ID процесса или его имя:");
                _awaitingInput[chatId] = "waiting_process_info";
                return;
            }
            else if (message.Text == "Завершить процесс")
            {
                await botClient.SendMessage(chatId, "Введите ID процесса или его имя:");
                _awaitingInput[chatId] = "waiting_process_end";
                return;
            }
            else if (message.Text == "Запустить процесс")
            {
                await botClient.SendMessage(chatId, "Введите путь до файла:");
                _awaitingInput[chatId] = "waiting_process_start";
                return;
            }
            else if (message.Text == "Назад")
            {
                await ShowMainMenu(botClient, chatId);
                return;
            }
        }

        private static async Task ShowMainMenu(ITelegramBotClient botClient, long chatId)
        {
            var choise_operation = new ReplyKeyboardMarkup(
            [
                new[] { new KeyboardButton("Перезагрузка"), new KeyboardButton("Выключение") },
                new[] { new KeyboardButton("Состояние"), new KeyboardButton("Процессы") },
            ])
            {
                ResizeKeyboard = true
            };

            await botClient.SendMessage(chatId, "Выберите действие:", replyMarkup: choise_operation);
        }


        private static async Task Error(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            try
            {
                string errorMessage = $"⚠️ Произошла ошибка:\n" +
                                    $"• Источник: {source}\n" +
                                    $"• Тип: {exception.GetType().Name}\n" +
                                    $"• Сообщение: {exception.Message}\n" +
                                    $"• StackTrace: {exception.StackTrace}";

                Console.WriteLine(errorMessage);

                long adminId = 1182102008;
                try
                {
                    await client.SendMessage(chatId: adminId, text: $"🚨 Ошибка бота:\n{exception.Message}", cancellationToken: token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Не удалось уведомить администратора: {ex.Message}");
                }

                if (exception is ApiRequestException apiException)
                {
                    Console.WriteLine($"API Error Code: {apiException.ErrorCode}");
                    Console.WriteLine($"API Description: {apiException.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка в обработчике ошибок: {ex}");
            }
            finally
            {
                await Task.CompletedTask;
            }
        }
    }
}