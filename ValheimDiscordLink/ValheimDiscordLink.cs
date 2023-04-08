 1 using BepInEx;
 2 using HarmonyLib;
 3 using System;
 4 using System.Collections.Generic;
 5 using System.Linq;
 6 using System.Text;
 7 using System.Threading.Tasks;
 8 using Discord.Webhook;
 9 using Discord.WebSocket;
10 using System.IO;
11 using System.Reflection;
12 using Nini.Config;
13 
14 namespace ValheimDiscordPlugin
15 {
16     [BepInPlugin("com.bluetigeresw.valheimdiscordlink", "ValheimDiscordLink", "1.0.0")]
17     public class ValheimDiscordPlugin : BaseUnityPlugin
18     {
19         private DiscordSocketClient _discordClient;
20         private DiscordWebhookClient _discordWebhookClient;
21         private string _discordWebhookUrl;
22         private string _discordChannelId;
23         private string _discordCommandPrefix;
24         private List<string> _adminRoles;
25         private string _lastPlayers;
26 
27         public void Awake()
28         {
29             // Set up your BepInEx plugin here
30 
31             // Read configuration values from config.ini
32             var configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.ini");
33             var config = new IniConfigSource(configFile);
34 
35             _discordWebhookUrl = config["Discord"]["WebhookUrl"];
36             _discordChannelId = config["Discord"]["ChannelId"];
37             _discordCommandPrefix = config["Discord"]["CommandPrefix"];
38 
39             _adminRoles = new List<string>(config["Discord"]["AdminRoles"].Split(','));
40 
41             // Initialize your Discord bot and webhook client here
42             _discordClient = new DiscordSocketClient();
43             _discordWebhookClient = new DiscordWebhookClient(_discordWebhookUrl);
44             _discordClient.MessageReceived += OnDiscordMessageReceived;
45 
46             // Subscribe to events from the Valheim server
47             ZRoutedRpc.instance.Register("ChatMessage", new Action<long, string>(OnValheimChatMessage));
48             ZNet.instance.m_serverStartEvent += OnValheimServerStart;
49             ZNet.instance.m_serverStopEvent += OnValheimServerStop;
50             EventManager.instance.m_onEventStart += OnValheimEventStart;
51             EventManager.instance.m_onEventStop += OnValheimEventStop;
52             Player.m_onDeath += OnValheimPlayerDeath;
53         }
54 
55         public void Start()
56         {
57             // Connect to the Discord bot and start listening for messages
58             _discordClient.LoginAsync(Discord.TokenType.Bot, "your-bot-token");
59             _discordClient.StartAsync();
60         }
61 
62         public void Update()
63         {
64             // Check for changes in players online and send updated list to Discord
65             string currentPlayers = GetPlayers();
66             if (currentPlayers != _lastPlayers)
67             {
68                 _lastPlayers = currentPlayers;
69                 _discordWebhookClient.SendMessageAsync($"Players online: {currentPlayers}");
70             }
71 
72             // Update your plugin logic here
73         }
74 
75         public void OnDestroy()
76         {
77             // Clean up your plugin resources here
78             _discordClient.StopAsync();
79             ZRoutedRpc.instance.Clear();
80         }
81 
82         private async Task OnDiscordMessageReceived(SocketMessage message)
83         {
84             if (message.Channel.Id.ToString() == _discordChannelId)
85             {
86                 // Handle incoming messages from the configured Discord channel
87                 if (message.Content.StartsWith(_discordCommandPrefix))
88                 {
89                     string command = message.Content.Substring(_discordCommandPrefix.Length);
90 
91                     if (command == "players")
92                     {
93                         string currentPlayers = GetPlayers();
94                         await message.Channel.SendMessageAsync($"Players online: {currentPlayers}");
95                     }
96                     else if (command.StartsWith("kick"))
97                     {
98                         // Make sure the user has admin role
99                         var author = message.Author as SocketGuildUser;
100 
101                         if (author.Roles.Any(role => _adminRoles.Contains(role.Name)))
102                         {
103                             string[] args = command.Split(' ');
104 
105                             if (args.Length == 2)
106                             {
107                                 string playerName = args[1];
108                                 ZRoutedRpc.instance.InvokeRoutedRPC(ZNet.instance.GetServerPeer().m_uid, "Kick", playerName);
109                             }
110                             else
111                             {
112                                 await message.Channel.SendMessageAsync("Invalid syntax. Usage: " + _commandPrefix + "kick <playername>");
113                             }
114                         }
115                         else
116                         {
117                             await message.Channel.SendMessageAsync("You do not have permission to use this command.");
118                         }
119                     }
120                 }
121 
122                 // Send message from Discord to Valheim server chat
123                 string authorName = message.Author.Username;
124                 string messageContent = message.Content;
125                 ZRoutedRpc.instance.InvokeRoutedRPC(ZNet.instance.GetServerPeer().m_uid, "ChatMessage", authorName, messageContent);
126             }
127         }
128 
129         private void OnValheimChatMessage(long sender, string message)
130         {
131             // Send chat messages from Valheim server to Discord webhook channel
132             _discordWebhookClient.SendMessageAsync($"[Valheim] {message}");
133         }
134 
135         private void OnValheimServerStart()
136         {
137             // Send server starting event to Discord webhook channel
138             _discordWebhookClient.SendMessageAsync("[Valheim] Server starting.");
139         }
140 
141         private void OnValheimServerStop()
142         {
143             // Send server stopping event to Discord webhook channel
144             _discordWebhookClient.SendMessageAsync("[Valheim] Server stopping.");
145         }
146 
147         private void OnValheimEventStart(EventDef eventDef)
148         {
149             // Send event starting event to Discord webhook channel
150             _discordWebhookClient.SendMessageAsync($"[Valheim] Event starting: {eventDef.m_eventName}");
151         }
152 
153         private void OnValheimEventStop(EventDef eventDef)
154         {
155             // Send event stopping event to Discord webhook channel
156             _discordWebhookClient.SendMessageAsync($"[Valheim] Event stopping: {eventDef.m_eventName}");
157         }
158 
159         private void OnValheimPlayerDeath(Player player, DamageText damageText)
160         {
161             // Send player death with cause of death to Discord webhook channel
162             _discordWebhookClient.SendMessageAsync($"[Valheim] {player.GetPlayerName()} has died from {damageText.GetDamageText()}");
163         }
164 
165         private string GetPlayers()
166         {
167             // Get a list of all players currently online
168             List<string> players = new List<string>();
169 
170             foreach (ZNet.PlayerInfo playerInfo in ZNet.instance.GetPlayerList())
171             {
172                 if (playerInfo.m_characterID != 0)
173                 {
174                     players.Add(playerInfo.m_name);
175                 }
176             }
177 
178             return string.Join(", ", players);
179         }
180
181         private void LoadConfig()
182         {
183             // Load configuration values from config.ini file
184             var parser = new FileIniDataParser();
185             IniData data = parser.ReadFile("config.ini");
186 
187             _discordCommandPrefix = data["Discord"]["CommandPrefix"];
188 
189             string adminRolesString = data["Discord"]["AdminRoles"];
190             _adminRoles = adminRolesString.Split(',');
191 
192             _discordWebhookUrl = data["Discord"]["WebhookUrl"];
193             _discordChannelId = ulong.Parse(data["Discord"]["ChannelId"]);
194         }
195     }
196 }