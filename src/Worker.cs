using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ShumanBot
{
    public class Worker : BackgroundService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<Worker> _logger;
        private Timer _timer;
        private TelegramBotClient botClient;

        public Worker(IConfiguration configuration, ILogger<Worker> logger)
        {
            this.configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var token = configuration["bot_token"];
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new NullReferenceException("bot_token not found in settings or empty");
            }
            
            _rusTimeZone = TimeZoneInfo.FindSystemTimeZoneById("North Asia Standard Time");
            _gmtZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            DateTime utcTime = DateTime.UtcNow;
            DateTime russianTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, _rusTimeZone);
            var newDay = russianTime.Date.AddDays(1);
            var remainder =  (int)(newDay - russianTime).TotalMilliseconds;

            //get midnight russia start time to get full image
            var timeToStart = russianTime.AddMilliseconds(remainder);
            
            DateTime cst = TimeZoneInfo.ConvertTime(timeToStart, _rusTimeZone, _gmtZone);
            _logger.LogInformation($"Timer to start @ {cst.ToString("F")}");

            //create bot and display information
            botClient = new TelegramBotClient(token);
            var me = await botClient.GetMeAsync(stoppingToken);
            _logger.LogInformation($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");

            //create a delay time to send message to channels
            int.TryParse(configuration["update_hours"], out var hours);
            var delay = 1000 * 60 * 60 * hours;

            //start timer on midnight
            _timer = new Timer(async x => await SendMessage(x, stoppingToken), null, remainder, delay);

            //TEST timer when exiting this methods
            //_timer = new Timer(async (x) => await SendMessage(null, stoppingToken), null, 2000, 672000);
        }

        private async Task SendMessage(object state, CancellationToken cancellationToken)
        {
            try
            {
                var horseChannel = -1001196741187;

                DateTime utcTime = DateTime.UtcNow;
                DateTime russianTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, _rusTimeZone);

                var gmt = GetSchumannTime(timeZoneTo: "GMT Standard Time", convertFrom: russianTime);

                //send shuman photo                
                var eastEU = GetSchumannTime("W. Europe Standard Time", convertFrom: russianTime);
                var rus = GetSchumannTime("Russian Standard Time", convertFrom: russianTime);
                var pak = GetSchumannTime("Pakistan Standard Time", convertFrom: russianTime);
                var aus = GetSchumannTime("AUS Central Standard Time", convertFrom: russianTime);
                var cst = GetSchumannTime("Central Standard Time", convertFrom: russianTime);
                
                var caption = $"{russianTime.ToString("F")} - Space Observation System {Environment.NewLine + Environment.NewLine}";
                var ran = new Random();
                caption += "🌜 " + LightQuotes[ran.Next(0, LightQuotes.Length)] + Environment.NewLine + Environment.NewLine;

                caption += "🌌 A Resonances" + Environment.NewLine;
                caption += "〽️ B Frequencies" + Environment.NewLine;
                caption += "🔉 C Amplitudes" + Environment.NewLine;
                caption += "❓ D Q-factors" + Environment.NewLine + Environment.NewLine;                               
                
                caption += $"🇷🇺 {russianTime.ToString("t")} RU UTC+7" + Environment.NewLine;
                caption += $"🇬🇧 {gmt.ToString("t")} GB  UTC" + Environment.NewLine;
                caption += $"🇪🇺 {eastEU.ToString("t")} EUE UTC+1" + Environment.NewLine;
                caption += $"🇷🇺 {rus.ToString("t")} RU  UTC+3" + Environment.NewLine;
                caption += $"🇵🇰 {pak.ToString("t")} PK  UTC+4" + Environment.NewLine;
                caption += $"🇦🇺 {aus.ToString("t")} AU  UTC+8" + Environment.NewLine;
                caption += $"🇺🇸 {cst.ToString("t")} CST UTC-6" + Environment.NewLine + Environment.NewLine;

                caption += Environment.NewLine + "http://sosrff.tsu.ru";

                List<InputMediaPhoto> media = new List<InputMediaPhoto>();

                var httpClient = new HttpClient();

                var url = "http://sosrff.tsu.ru/new/";
                var stream = await httpClient.GetAsync(url + "/shm.jpg");
                var mp = new InputMediaPhoto(new InputMedia(content: stream.Content.ReadAsStream(), "shm.jpg"));
                mp.Caption = caption;                
                media.Add(mp);

                stream = await httpClient.GetAsync(url + "/srf.jpg");
                var freq = new InputMediaPhoto(new InputMedia(content: stream.Content.ReadAsStream(), "srf.jpg"));
                media.Add(freq);

                stream = await httpClient.GetAsync(url + "/sra.jpg");
                var amp = new InputMediaPhoto(new InputMedia(content: stream.Content.ReadAsStream(), "sra.jpg"));
                media.Add(amp);

                stream = await httpClient.GetAsync(url + "/srq.jpg");
                var qFac = new InputMediaPhoto(new InputMedia(content: stream.Content.ReadAsStream(), "srq.jpg"));
                media.Add(qFac);

                await botClient.SendMediaGroupAsync(media, chatId: new ChatId(horseChannel));

                //var msg = await botClient.pho(new Telegram.Bot.Types.ChatId(horseChannel),
                //    file, caption, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw;
            }
        }

        private string[] LightQuotes = new string[]
        {
            "The pessimist sees only the tunnel; the optimist sees the light at the end of the tunnel; the realist sees the tunnel and the light – and the next tunnel.",
            "The light at the end of the tunnel is not an illusion. The tunnel is.",
            "Moonlight drowns out all but the brightest stars.",
            "We can easily forgive a child who is afraid of the dark; the real tragedy of life is when men are afraid of the light.",
            "In order for the light to shine so brightly, the darkness must be present.",
            "It’s not easy to be Light when you’ve been Dark. It’s almost too much to ask anyone.",
            "Darkness will always try to extinguish the light. The light will always try to repress the darkness.",
            "our path is illuminated by the light, yet darkness lets the stars shine bright.",
            "When the Sun of compassion arises darkness evaporates and the singing birds come from nowhere.",
            "How far that little candle throws his beams! So shines a good deed in a weary world",
            "There is no darkness so dense, so menacing, or so difficult that it cannot be overcome by light",
            "I warn you, the trip will not be easy. Once you choose to walk in the light, your path will lead you places you do not want to go",
            "For a man who walks in the light, to stay humble is not to walk in the dark; you don’t need to project yourself to be thought an honest man",
            "The path of light is the quest for knowledge.",
            "People are like stained-glass windows. They sparkle and shine when the sun is out, but when the darkness sets in, their true beauty is revealed only if there is a light from within.",
            "At times our own light goes out and is rekindled by a spark from another person. Each of us has cause to think with deep gratitude of those who have lighted the flame within us."
        };
        private TimeZoneInfo _rusTimeZone;
        private TimeZoneInfo _gmtZone;

        private static DateTime GetSchumannTime(string timeZoneFrom = "North Asia Standard Time", string timeZoneTo = "GMT Standard Time", DateTime? convertFrom = null)
        {
            //DateTime timeUtc = convertFrom.HasValue ? convertFrom.Value : DateTime.UtcNow;
            try
            {
                TimeZoneInfo cstZoneF = TimeZoneInfo.FindSystemTimeZoneById(timeZoneFrom);
                TimeZoneInfo cstZoneT = TimeZoneInfo.FindSystemTimeZoneById(timeZoneTo);
                DateTime cstTime = TimeZoneInfo.ConvertTime(convertFrom.Value, cstZoneF, cstZoneT);
                return cstTime;
            }
            catch (TimeZoneNotFoundException)
            {
                Console.WriteLine($"The registry does not define the time zone.");
            }
            catch (InvalidTimeZoneException)
            {
                Console.WriteLine($"Registry data on the time has been corrupted.");
            }

            return DateTime.MinValue;
        }
    }
}
