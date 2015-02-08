﻿using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace midspace.adminscripts
{
    public class ServerConfig
    {
        /// <summary>
        /// The format of the config file name.
        /// </summary>
        private const string ConfigFileNameFormat = "Config_{0}.cfg";
        private const string MotdFileNameFormat = "Motd_{0}.txt";

        private string ConfigFileName;

        /// <summary>
        /// Used for saving and loading things.
        /// </summary>
        private ServerConfigurationStruct Config;

        public List<BannedPlayer> ForceBannedPlayer { get { return Config.ForceBannedPlayers; } }

        public ServerConfig()
        {
            Config = new ServerConfigurationStruct()
            {
                //init default values
                WorldLocation = MyAPIGateway.Utilities.ConfigDedicated.LoadWorld,
                MotdFileSuffix = ReplaceForbiddenChars(MyAPIGateway.Utilities.ConfigDedicated.WorldName),
                MotdHeadLine = "",
                MotdShowInChat = false,
                ForceBannedPlayers = new List<BannedPlayer>(),
            };
            ConfigFileName = string.Format(ConfigFileNameFormat, MyAPIGateway.Session.WorldID);
            LoadOrCreateConfig();
            LoadOrCreateMotdFile();
        }

        public void Save()
        {
            Config.WorldLocation = MyAPIGateway.Utilities.ConfigDedicated.LoadWorld;
            WriteConfig();
        }

        private void LoadOrCreateConfig()
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(ConfigFileName, typeof(ServerConfig)))
                WriteConfig();
            else
                LoadConfig();
        }

        private void WriteConfig()
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(ConfigFileName, typeof(ServerConfig));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML(Config));
            writer.Flush();
            writer.Close();
        }

        private void LoadConfig()
        {
            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(ConfigFileName, typeof(ServerConfig));

            var xmlText = reader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(xmlText))
                return;

            try
            {
                Config = MyAPIGateway.Utilities.SerializeFromXML<ServerConfigurationStruct>(xmlText);
            }
            catch (Exception ex)
            {
                ChatCommandLogic.Instance.AdminNotification = string.Format(@"There is an error in the config file. It couldn't be read. The server was started with default settings.

Message:
{0}

If you can't find the error, simply delete the file. The server will create a new one with default settings on restart.", ex.Message);
            }

            CommandMessageOfTheDay.HeadLine = Config.MotdHeadLine;
            CommandMessageOfTheDay.ShowInChat = Config.MotdShowInChat;
        }

        private void LoadOrCreateMotdFile()
        {
            var file = string.Format(MotdFileNameFormat, ReplaceForbiddenChars(Config.MotdFileSuffix));

            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(file, typeof(ChatCommandLogic)))
                CreateMotdConfig(file);

            TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(file, typeof(ChatCommandLogic));
            var text = reader.ReadToEnd();
            if (!string.IsNullOrEmpty(text))
            {
                //prepare MOTD, replace variables
                var dedicatedConfig = MyAPIGateway.Utilities.ConfigDedicated;


                text = text.Replace("%SERVER_NAME%", dedicatedConfig.ServerName);
                text = text.Replace("%WORLD_NAME%", dedicatedConfig.WorldName);
                //text = text.Replace("%SERVER_IP%", dedicatedConfig.IP); returns the 'listen ip' default: 0.0.0.0
                text = text.Replace("%SERVER_PORT%", dedicatedConfig.ServerPort.ToString());

                CommandMessageOfTheDay.MessageOfTheDay = text;
                CommandMessageOfTheDay.ShowInChat = Config.MotdShowInChat;
            }
            reader.Close();
        }

        /// <summary>
        /// Create motd file
        /// </summary>
        private void CreateMotdConfig(string file)
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(file, typeof(ChatCommandLogic));
            writer.Flush();
            writer.Close();
        }

        /// <summary>
        /// Replaces the chars from the given string that aren't allowed for a filename with a whitespace.
        /// </summary>
        /// <param name="originalText"></param>
        /// <returns></returns>
        public static string ReplaceForbiddenChars(string originalText)
        {
            //could be done in one single line but like this we have a better overview
            var convertedText = originalText.Replace('\\', ' ');
            convertedText = convertedText.Replace('/', ' ');
            convertedText = convertedText.Replace(':', ' ');
            convertedText = convertedText.Replace('*', ' ');
            convertedText = convertedText.Replace('?', ' ');
            convertedText = convertedText.Replace('"', ' ');
            convertedText = convertedText.Replace('<', ' ');
            convertedText = convertedText.Replace('>', ' ');
            convertedText = convertedText.Replace('|', ' ');

            return convertedText;
        }
    }

    /// <summary>
    /// Contains the settings from the file.
    /// </summary>
    public struct ServerConfigurationStruct
    {
        public string WorldLocation;

        /// <summary>
        /// The suffix for the motd file. For a better identification.
        /// </summary>
        public string MotdFileSuffix;
        public string MotdHeadLine;
        public bool MotdShowInChat;
        public List<BannedPlayer> ForceBannedPlayers;
        /// <summary>
        /// The permissions in string form. No need to initialize the permissions on the server since it is transmitted as a string anyway.
        /// </summary>
        //public string CommandPermissions = "";

    }

    public struct BannedPlayer
    {
        public ulong SteamId;
        public string PlayerName;
    }
}
