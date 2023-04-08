 using BepInEx;
 using HarmonyLib;
 using System;
 using System.Collections.Generic;
 using System.Linq;
 using System.Text;
 using System.Threading.Tasks;
 using Discord.Webhook;
 using Discord.WebSocket;
 using System.IO;
 using System.Reflection;
 using Nini.Config;
 
 namespace ValheimDiscordLink
 {
     [BepInPlugin("com.bluetigeresw.valheimdiscordlink", "ValheimDiscordLink", "1.0.0")]
     [BepInProcess("valheim.exe")]
     public class ValheimDiscordLink : BaseUnityPlugin
     {
         private DiscordSocketClient _discordClient;
         private DiscordWebhookClient _discordWebhookClient;
         private string _discordWebhookUrl;
         private string _discordChannelId;
         private string _discordCommandPrefix;
         private List<string> _adminRoles;
         private string _lastPlayers;
 
         public void Awake()
         {
             // Set up your BepInEx plugin here
 
             // Read configuration values from config.ini
             var configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.ini");
             var config = new IniConfigSource(configFile);
 
             _discordWebhookUrl = config["Discord"]["WebhookUrl"];
             _discordChannelId = config["Discord"]["ChannelId"];
             _discordCommandPrefix = config["Discord"]["CommandPrefix"];
 
             _adminRoles = new List<string>(config["Discord"]["AdminRoles"].Split(','));
 
             // Initialize your Discord bot and webhook client here
             _discordClient = new DiscordSocketClient();
             _discordWebhookClient = new DiscordWebhookClient(_discordWebhookUrl);
             _discordClient.MessageReceived += OnDiscordMessageReceived;
 
             // Subscribe to events from the Valheim server
             ZRoutedRpc.instance.Register("ChatMessage", new Action<long, string>(OnValheimChatMessage));
             ZNet.instance.m_serverStartEvent += OnValheimServerStart;
             ZNet.instance.m_serverStopEvent += OnValheimServerStop;
             EventManager.instance.m_onEventStart += OnValheimEventStart;
             EventManager.instance.m_onEventStop += OnValheimEventStop;
             Player.m_onDeath += OnValheimPlayerDeath;
         }
 
         public void Start()
         {
             // Connect to the Discord bot and start listening for messages
             _discordClient.LoginAsync(Discord.TokenType.Bot, "your-bot-token");
             _discordClient.StartAsync();
         }
 
         public void Update()
         {
             // Check for changes in players online and send updated list to Discord
             string currentPlayers = GetPlayers();
             if (currentPlayers != _lastPlayers)
             {
                 _lastPlayers = currentPlayers;
                 _discordWebhookClient.SendMessageAsync($"Players online: {currentPlayers}");
             }
 
             // Update your plugin logic here
         }
 
         public void OnDestroy()
         {
             // Clean up your plugin resources here
             _discordClient.StopAsync();
             ZRoutedRpc.instance.Clear();
         }
 
         private async Task OnDiscordMessageReceived(SocketMessage message)
         {
             if (message.Channel.Id.ToString() == _discordChannelId)
             {
                 // Handle incoming messages from the configured Discord channel
                 if (message.Content.StartsWith(_discordCommandPrefix))
                 {
                     string command = message.Content.Substring(_discordCommandPrefix.Length);
 
                     if (command == "players")
                     {
                         string currentPlayers = GetPlayers();
                         await message.Channel.SendMessageAsync($"Players online: {currentPlayers}");
                     }
                     else if (command.StartsWith("kick"))
                     {
                         // Make sure the user has admin role
                         var author = message.Author as SocketGuildUser;
 
                         if (author.Roles.Any(role => _adminRoles.Contains(role.Name)))
                         {
                             string[] args = command.Split(' ');
 
                             if (args.Length == 2)
                             {
                                 string playerName = args[1];
                                 ZRoutedRpc.instance.InvokeRoutedRPC(ZNet.instance.GetServerPeer().m_uid, "Kick", playerName);
                             }
                             else
                             {
                                 await message.Channel.SendMessageAsync("Invalid syntax. Usage: " + _commandPrefix + "kick <playername>");
                             }
                         }
                         else
                         {
                             await message.Channel.SendMessageAsync("You do not have permission to use this command.");
                         }
                     }
                 }
 
                 // Send message from Discord to Valheim server chat
                 string authorName = message.Author.Username;
                 string messageContent = message.Content;
                 ZRoutedRpc.instance.InvokeRoutedRPC(ZNet.instance.GetServerPeer().m_uid, "ChatMessage", authorName, messageContent);
             }
         }
 
         private void OnValheimChatMessage(long sender, string message)
         {
             // Send chat messages from Valheim server to Discord webhook channel
             _discordWebhookClient.SendMessageAsync($"[Valheim] {message}");
         }
 
         private void OnValheimServerStart()
         {
             // Send server starting event to Discord webhook channel
             _discordWebhookClient.SendMessageAsync("[Valheim] Server starting.");
         }
 
         private void OnValheimServerStop()
         {
             // Send server stopping event to Discord webhook channel
             _discordWebhookClient.SendMessageAsync("[Valheim] Server stopping.");
         }
 
         private void OnValheimEventStart(EventDef eventDef)
         {
             // Send event starting event to Discord webhook channel
             _discordWebhookClient.SendMessageAsync($"[Valheim] Event starting: {eventDef.m_eventName}");
         }
 
         private void OnValheimEventStop(EventDef eventDef)
         {
             // Send event stopping event to Discord webhook channel
             _discordWebhookClient.SendMessageAsync($"[Valheim] Event stopping: {eventDef.m_eventName}");
         }
 
         private void OnValheimPlayerDeath(Player player, DamageText damageText)
         {
             // Send player death with cause of death to Discord webhook channel
             _discordWebhookClient.SendMessageAsync($"[Valheim] {player.GetPlayerName()} has died from {damageText.GetDamageText()}");
         }
 
         private string GetPlayers()
         {
             // Get a list of all players currently online
             List<string> players = new List<string>();
 
             foreach (ZNet.PlayerInfo playerInfo in ZNet.instance.GetPlayerList())
             {
                 if (playerInfo.m_characterID != 0)
                 {
                     players.Add(playerInfo.m_name);
                 }
             }
 
             return string.Join(", ", players);
         }

         private void LoadConfig()
         {
             // Load configuration values from config.ini file
             var parser = new FileIniDataParser();
             IniData data = parser.ReadFile("config.ini");
 
             _discordCommandPrefix = data["Discord"]["CommandPrefix"];
 
             string adminRolesString = data["Discord"]["AdminRoles"];
             _adminRoles = adminRolesString.Split(',');
 
             _discordWebhookUrl = data["Discord"]["WebhookUrl"];
             _discordChannelId = ulong.Parse(data["Discord"]["ChannelId"]);
         }
     }
 }