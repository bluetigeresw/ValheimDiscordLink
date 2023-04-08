using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Webhook;
using Discord.WebSocket;

namespace ValheimDiscordPlugin
{
    [BepInPlugin("com.example.valheimdiscordplugin", "ValheimDiscordPlugin", "1.0.0")]
    public class ValheimDiscordPlugin : BaseUnityPlugin
    {
        private DiscordSocketClient _discordClient;
        private DiscordWebhookClient _discordWebhookClient;
        private string _discordWebhookUrl;
        private string _discordChannelName;

        public void Awake()
        {
            // Set up your BepInEx plugin here
            
            // Initialize your Discord bot and webhook client here
            _discordClient = new DiscordSocketClient();
            _discordWebhookClient = new DiscordWebhookClient(_discordWebhookUrl);
            _discordClient.MessageReceived += OnDiscordMessageReceived;
        }

        public void Start()
        {
            // Connect to the Discord bot and start listening for messages
            _discordClient.LoginAsync(Discord.TokenType.Bot, "your-bot-token");
            _discordClient.StartAsync();
        }

        public void Update()
        {
            // Update your plugin logic here
        }

        public void OnDestroy()
        {
            // Clean up your plugin resources here
            _discordClient.StopAsync();
        }

        private async Task OnDiscordMessageReceived(SocketMessage message)
        {
            if (message.Channel.Name == _discordChannelName)
            {
                // Handle incoming messages from the configured Discord channel
                if (message.Content.ToLower() == "!players")
                {
                    // List players online
                    // Send the player list to the Discord webhook channel
                }
                else if (message.Content.ToLower().StartsWith("!kick"))
                {
                    // Check if the user is an admin role
                    // Kick the specified user in the Valheim server
                }
                else
                {
                    // Send the message to the Valheim server chat
                }
            }
        }

        private void OnValheimServerMessageReceived(string message)
        {
            // Handle incoming messages from the Valheim server
            if (message.Contains("Player connected:"))
            {
                // Send a message to the Discord webhook channel when a player connects
            }
            else if (message.Contains("Player disconnected:"))
            {
                // Send a message to the Discord webhook channel when a player disconnects
            }
            else if (message.Contains("Server started"))
            {
                // Send a message to the Discord webhook channel when the server starts
            }
            else if (message.Contains("Server stopping"))
            {
                // Send a message to the Discord webhook channel when the server stops
            }
            else
            {
                // Send the message to the configured Discord channel
                _discordWebhookClient.SendMessageAsync(message);
            }
        }
    }
}
