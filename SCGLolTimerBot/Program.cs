using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using Newtonsoft.Json;
using SCGLolTimerBot.Helper;
using SCGLolTimerBot.Models;
using Spectre.Console;
using Color = NetCord.Color;
using Timer = SCGLolTimerBot.Models.Timer;

namespace SCGLolTimerBot;

class Program
{
    private static readonly string TokenFile = "guardianToken.txt";

    private static async Task Main(string[] args)
    {
        if (!File.Exists("appsettings.json") || AskForChangingData())
            SetCustomData();
        AnsiConsole.Clear();
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var setDay = (DayOfWeek)Convert.ToInt32(config.GetSection("Timer").GetSection("Day").Value!);
        if (DateTime.Now.DayOfWeek != setDay)
            return;
        var client = GetClient();
        CreateRequests(client, config);
        
        await client.StartAsync();
        await Task.Delay(-1);
    }

    private static GatewayClient GetClient()
    {
        GatewayClient client = new(new BotToken(File.ReadAllText(TokenFile)),
            new GatewayClientConfiguration()
            {
                Intents = GatewayIntents.All | GatewayIntents.GuildUsers | GatewayIntents.GuildModeration
            });
        return client;
    }

    private static void CreateRequests(GatewayClient client, IConfiguration config)
    {
        client.Ready += readyEventArgs =>
        {
            var setHour = Convert.ToInt32(config.GetSection("Timer").GetSection("Hour").Value!);
            var setMinute = Convert.ToInt32(config.GetSection("Timer").GetSection("Minute").Value!);
            var setDay = (DayOfWeek)Convert.ToInt32(config.GetSection("Timer").GetSection("Day").Value!);
            
            AnsiConsole.MarkupLineInterpolated($"Logged in as {readyEventArgs.User.Username}, please [red]don't shutdown[/] the application!");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine($"Sending memo on {setDay.ToString()} at {setHour:D2}:{setMinute:D2}");
            _ = Task.Run(async () =>
            {
                bool alreadySent = false;
                while (true)
                {
                    
                    var sendToChannelId = Convert.ToUInt64(config.GetSection("ChannelId").Value!);
                    var availableChannelId = Convert.ToUInt64(config.GetSection("AvailableChannelId").Value!);
                    var roleId = Convert.ToUInt64(config.GetSection("Leader").Value!);
                    var teamId = Convert.ToUInt64(config.GetSection("Team").Value!);
                    
                    var now = DateTime.Now;
                    if (now.DayOfWeek == setDay && now.Hour == setHour && now.Minute == setMinute)
                    {
                        if (!alreadySent)
                        {
                            await client.Rest.SendMessageAsync(sendToChannelId, CreateEmbed(availableChannelId, roleId));
                            await client.Rest.SendMessageAsync(sendToChannelId, $"<@&{teamId}>");
                            alreadySent = true;
                        }
                    }
                    else
                    {
                        alreadySent = false;
                    }

                    await Task.Delay(1000);
                }
            });
            return ValueTask.CompletedTask;
        };
        
        
        //Antworten auf Anfragen
        client.MessageCreate += async message =>
        {
            Console.WriteLine($"{DateTime.Now}: {message.Author.Username}: {message.Content}");
            var users = message.Guild!.Users.Values.ToList();
            if (!message.Author.IsBot && message.Content.ToCharArray()[0] == '!')
            {
                if (message.Author.Id == 1025579600794894386)
                {
                    if (message.Content == "!ping")
                        await client.Rest.SendMessageAsync(message.ChannelId, "Pong!");
                }
                else
                {
                    await client.Rest.SendMessageAsync(message.ChannelId,
                        "Darf ich leider nicht ausführen. :(");
                }
            }
        };
    }

    private static MessageProperties CreateEmbed(ulong channelId, ulong leaderId)
    {
        var today = DateTime.Today;
        var cw = (int)today.DayOfWeek >= (int)DayOfWeek.Thursday && (int)today.DayOfWeek <= (int)DayOfWeek.Sunday 
            ? ISOWeek.GetWeekOfYear(today)+1 
            : ISOWeek.GetWeekOfYear(today);
        var cwDayPeriod = CalendarWeekHelper.GetCalendarWeek(today.Year, cw);
        var embed = new EmbedProperties()
        {
            Title = $"Erinnerung für KW {cw} ({cwDayPeriod.MinDate:dd.MM.} - {cwDayPeriod.MaxDate:dd.MM.yy})",
            Description = $"Vergesst nicht eure **verfügbaren** Zeiten für nächste Woche in <#{channelId}> zu aktualisieren!\nBitte bei plötzlichen Terminänderungen dem <@&{leaderId}> Bescheid geben.",
            Color = new Color(0xff0000),
            Thumbnail = new EmbedThumbnailProperties(
                "https://cdn.discordapp.com/attachments/725042990363443302/1506289074405773353/Kopie_von_SgC_Lol_Team_Logo.png?ex=6a0db884&is=6a0c6704&hm=544e71ac965b84a6f2cd088f4f49f8518a5cc4e23781aba4074dc2cd8916f409&")
        };

        var message = new MessageProperties() {
            Embeds = [embed]
        };
        
        return message;
    }

    private static void SetBotToken()
    {
        string? token;
        if (!File.Exists(TokenFile))
            token = AnsiConsole.Ask<string>("Please enter your [blue]Bot-Token[/] to activate the application");
        else
        {
            AnsiConsole.MarkupLine("Enter your [blue]Bot-Token[/] here [italic cyan](leave empty if you don't want to change your token)[/]");
            token = Console.ReadLine();
        }
        if (!string.IsNullOrEmpty(token))    
            File.WriteAllText(TokenFile, token);
        AnsiConsole.MarkupLine(string.IsNullOrEmpty(token) ? "[green]Bot token not set.[/]" :"[green]Bot token successfully provided.[/]");
        AnsiConsole.WriteLine("Press enter to continue setup...");
        Console.ReadLine();
        AnsiConsole.Clear();
    }

    private static void SetCustomData()
    {
        AnsiConsole.MarkupLine("[bold cyan]Setup of SGCLolMemo-Bot:[/]");
        AnsiConsole.WriteLine();
        SetBotToken();
        var channelId = AnsiConsole.Ask<ulong>("First, enter the [blue]channel ID[/] for the message's destination");
        var availableChannelId = AnsiConsole.Ask<ulong>("Then the [blue]thread ID[/] for the linked thread");
        var teamRole = AnsiConsole.Ask<ulong>("After that, enter the [blue]role ID[/] of your [cyan]team[/]");
        var leader = AnsiConsole.Ask<ulong>("Now enter the [blue]role ID[/] for the [yellow]contact person[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[cyan]Now we need to specify the time at which the bot should always send the message.[/]");
        var hour = AnsiConsole.Ask<int>("First, specify at what [blue]HOUR[/] the bot should respond");
        var minute = AnsiConsole.Ask<int>("Finally, just enter the [blue]MINUTE[/]");
        
        var dayOfWeek = AnsiConsole.Prompt(new SelectionPrompt<DayOfWeek>().Title("Select the [blue]DAY[/] for sending the message:")
            .AddChoices(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday));

        var botData = new BotData
        {
            ChannelId = channelId,
            AvailableChannelId = availableChannelId,
            Team = teamRole,
            Leader = leader,
            Timer = new Timer
            {
                Day =  dayOfWeek,
                Hour = hour,
                Minute = minute,
            }
        };
        
        var jsonString = JsonConvert.SerializeObject(botData);
        File.WriteAllText("appsettings.json", jsonString);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Setup completed successfully.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine($"Bot is sending every {dayOfWeek.ToString()} at {hour.ToString("D2")}:{minute.ToString("D2")} a memo");
        AnsiConsole.WriteLine($"in the Channel '{channelId}'");
        AnsiConsole.WriteLine($"for the Thread '{availableChannelId}'");
        AnsiConsole.WriteLine($"with everyone with this Role '{leader}' as contact person.");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold red]Note[/]");
        AnsiConsole.MarkupLine("[italic red]If today's day of the week does not match the set day of the week, the program will automatically shut down after the setup prompt.[/]");
        AnsiConsole.WriteLine("Press enter to start bot...");
        Console.ReadLine();
    }

    private static bool AskForChangingData()
    {
        return AnsiConsole.Confirm("Wanna change the bot setup?");
    }
}