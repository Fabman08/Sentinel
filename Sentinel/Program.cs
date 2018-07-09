namespace Sentinel
{
    using System;    
    using System.Collections.Generic;
    using System.Media;
    using System.Net;
    using System.Net.Http;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;

    using Sentinel.Properties;

    public class Program
    {
        private const int NumberOfAttempts = 5;

        private const int TimeOut = 1000;

        private const string MessageError = @"Invalid value";

        private static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);

        private static readonly Dictionary<Sounds, SoundPlayer> Sounds = new Dictionary<Sounds, SoundPlayer>();

        private static ExecutionStatus globalStatus = ExecutionStatus.SendRequest;

        private static RequestType requestType;

        public static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eArgs) =>
                {
                    PlaySound(Sentinel.Sounds.Cancelled);
                    Console.SetCursorPosition(0, LineConstants.InfoQuit);
                    QuitEvent.Set();
                    eArgs.Cancel = true;
                };

            PreLoadSounds();
            
            WriterHelper.Writeheader();

            requestType = GetRequestType();

            var urlToCheck = GetUrlToCheck();            

            WriterHelper.WriteInfo(LineConstants.InfoGlobalStatus, text: $@"Global Status: { globalStatus }");
            WriterHelper.WriteInfo(LineConstants.InfoGlobalAttempts, text: $@"Current Attempts left: { NumberOfAttempts }");
            WriterHelper.WriteInfo(LineConstants.InfoCurrentException, text: @"Current Exception: -");
            WriterHelper.WriteInfo(LineConstants.InfoResponseStatus, text: @"Response: -");
            
            Task.Run(() => SendRequest(urlToCheck, requestType, NumberOfAttempts));

            PlaySound(Sentinel.Sounds.Startup);

            QuitEvent.WaitOne();
        }

        private static string GetUrlToCheck()
        {
            Console.SetCursorPosition(0, LineConstants.UrlCheck);
            Console.WriteLine(@"Please type a url/IP to check");
            var urlToCheck = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(urlToCheck))
            {
                WriterHelper.WriteInputError(urlToCheck, LineConstants.UrlCheckError, MessageError);
                GetUrlToCheck();
            }

            if (requestType == RequestType.WebRequest && !urlToCheck.StartsWith("http://") && !urlToCheck.StartsWith("https://"))
            {
                WriterHelper.WriteInputError(urlToCheck, LineConstants.UrlCheckError, @"Please prepend http:// or https://");
                GetUrlToCheck();
            }

            return urlToCheck;
        }

        private static RequestType GetRequestType()
        {
            Console.SetCursorPosition(0, LineConstants.RequestType);
            Console.WriteLine(@"Trasmission type :");
            Console.WriteLine(@"1. Ping");
            Console.WriteLine(@"2. Web Request");
            Console.WriteLine(@"Choose your destiny: ");

            var requestTypeTyped = Console.ReadLine();

            if (!Enum.TryParse(requestTypeTyped, out RequestType requestType) || !Enum.IsDefined(typeof(RequestType), requestType))
            {                
                WriterHelper.WriteInputError(requestTypeTyped, LineConstants.RequestTypeError, MessageError);
                GetRequestType();
            }

            return requestType;
        }

        private static async Task SendRequest(string urlToCheck, RequestType requestType, int localAttempts)
        {            
            try
            {                
                if (requestType == RequestType.Ping)
                {
                    await SendPing(urlToCheck);
                }
                else
                {
                    await SendWebRequest(urlToCheck);                                        
                }

                localAttempts = NumberOfAttempts;

                WriterHelper.WriteInfo(LineConstants.InfoGlobalAttempts, ColumnConstants.InfoGlobalAttempts, localAttempts.ToString());
                WriterHelper.WriteInfo(LineConstants.InfoCurrentException, ColumnConstants.InfoCurrentException, "-");

                if (globalStatus == ExecutionStatus.AlarmFired)
                {
                    globalStatus = ExecutionStatus.SendRequest;
                    WriterHelper.WriteInfo(LineConstants.InfoGlobalStatus, ColumnConstants.InfoGlobalStatus, globalStatus.ToString());

                    PlayVictory();
                }

                if (globalStatus == ExecutionStatus.Warning)
                {
                    globalStatus = ExecutionStatus.SendRequest;
                    WriterHelper.WriteInfo(LineConstants.InfoGlobalStatus, ColumnConstants.InfoGlobalStatus, globalStatus.ToString());

                    PlaySound(Sentinel.Sounds.Startup);
                }
            }
            catch (Exception e)
            {
                WriterHelper.WriteInfo(LineConstants.InfoCurrentException,  ColumnConstants.InfoCurrentException, e.Message);

                if (globalStatus != ExecutionStatus.AlarmFired)
                {
                    globalStatus = ExecutionStatus.Warning;
                    WriterHelper.WriteInfo(LineConstants.InfoGlobalStatus, ColumnConstants.InfoGlobalStatus, globalStatus.ToString());

                    localAttempts--;
                    WriterHelper.WriteInfo(LineConstants.InfoGlobalAttempts, ColumnConstants.InfoGlobalAttempts, localAttempts.ToString());
                }
                
                if (localAttempts <= 0)
                {
                    globalStatus = globalStatus != ExecutionStatus.AlarmFired ? ExecutionStatus.AttempsExcedeed : globalStatus;
                    WriterHelper.WriteInfo(LineConstants.InfoGlobalStatus, ColumnConstants.InfoGlobalStatus, globalStatus.ToString());
                }
            }
            finally
            {
                switch (globalStatus)
                {
                    case ExecutionStatus.SendRequest:
                    case ExecutionStatus.AlarmFired:
                        Thread.Sleep(5000);

                        await SendRequest(urlToCheck, requestType, localAttempts);
                        break;

                    case ExecutionStatus.Warning:
                        
                        PlayAlarm(localAttempts);

                        Thread.Sleep(5000);

                        await SendRequest(urlToCheck, requestType, localAttempts);
                        break;

                    case ExecutionStatus.AttempsExcedeed:
                        globalStatus = ExecutionStatus.AlarmFired;

                        WriterHelper.WriteInfo(LineConstants.InfoGlobalStatus, ColumnConstants.InfoGlobalStatus, globalStatus.ToString());
                        
                        PlayAlarm(localAttempts);
                        
                        Thread.Sleep(5000);                        
                        await SendRequest(urlToCheck, requestType, localAttempts);
                        break;                    
                }            
            }                                                        
        }

        private static async Task SendPing(string urlToCheck)
        {
            using (var ping = new Ping())
            {
                var result = ping.Send(urlToCheck);

                WriterHelper.WriteInfo(LineConstants.InfoResponseStatus, text: $@"Response: Status: {result.Status}, RoundtripTime: {result.RoundtripTime}");                

                if (result.Status != IPStatus.Success || result.RoundtripTime > TimeOut)
                {
                    throw new Exception("Ping fault");
                }
            }
        }

        private static async Task SendWebRequest(string urlToCheck)
        {
            var httpClient = new HttpClient
                                 {
                                     Timeout = TimeSpan.FromMilliseconds(TimeOut),
                                     BaseAddress = new Uri(urlToCheck)
                                 };

            var responseMessage = await httpClient.GetAsync(string.Empty);
            
            if (responseMessage.StatusCode != HttpStatusCode.OK)
            {
                WriterHelper.WriteInfo(LineConstants.InfoResponseStatus, text: $@"Response: Status: {responseMessage.StatusCode}");
                throw new Exception(responseMessage.Content.ToString());
            }            
        }

        private static void PlayAlarm(int localAttempts)
        {
            switch (localAttempts)
            {
                case 4:
                    PlaySound(Sentinel.Sounds.EnemyApproaching);
                    break;

                case 3:
                    PlaySound(Sentinel.Sounds.OurBaseIsUnderAttack);
                    break;

                case 2:
                    PlaySound(Sentinel.Sounds.NuclearWeaponLaunched);
                    break;
                
                case 1:
                    PlaySound(Sentinel.Sounds.NuclearWarheadApproaching);
                    break;

                case 0:
                    PlaySound(Sentinel.Sounds.StructureLost);                        
                break;
            }            
        }

        private static void PlayVictory()
        {            
            PlaySound(Sentinel.Sounds.Reinforcement);            
            PlaySound(Sentinel.Sounds.EnemyStructureDestoryed);
        }

        private static void PlaySound(Sounds soundsKey)
        {
            var simpleSound = Sounds[soundsKey];
            simpleSound.PlaySync();
        }

        private static void PreLoadSounds()
        {
            Sounds.Add(Sentinel.Sounds.Startup, new SoundPlayer(Resources.Unit_ready));
            Sounds.Add(Sentinel.Sounds.EnemyApproaching, new SoundPlayer(Resources.Enemy_approaching));
            Sounds.Add(Sentinel.Sounds.OurBaseIsUnderAttack, new SoundPlayer(Resources.Our_base_is_under_attack));
            Sounds.Add(Sentinel.Sounds.NuclearWeaponLaunched, new SoundPlayer(Resources.Nuclear_weapon_launched));
            Sounds.Add(Sentinel.Sounds.NuclearWarheadApproaching, new SoundPlayer(Resources.Nuclear_warhead_approaching));
            Sounds.Add(Sentinel.Sounds.StructureLost, new SoundPlayer(Resources.Structure_lost));
            Sounds.Add(Sentinel.Sounds.Reinforcement, new SoundPlayer(Resources.Reinforcement_have_arrived));
            Sounds.Add(Sentinel.Sounds.EnemyStructureDestoryed, new SoundPlayer(Resources.Enemy_structure_destroyed));
            Sounds.Add(Sentinel.Sounds.Cancelled, new SoundPlayer(Resources.Cancelled));
        }
    }
}
