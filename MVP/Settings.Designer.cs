﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SMan {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.0.3.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ReBoot your mind")]
        public string ServerName {
            get {
                return ((string)(this["ServerName"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string GamePassword {
            get {
                return ((string)(this["GamePassword"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string AdminPassword {
            get {
                return ((string)(this["AdminPassword"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public int Difficulty {
            get {
                return ((int)(this["Difficulty"]));
            }
            set {
                this["Difficulty"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public int Mode {
            get {
                return ((int)(this["Mode"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UsedForTakeover {
            get {
                return ((bool)(this["UsedForTakeover"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avatars/1f/1fa222692" +
            "912b7d56a65e6ff8593f5cb8b4236fa_full.jpg")]
        public string BannerLink {
            get {
                return ((string)(this["BannerLink"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("A fine selection of workshop maps, no player collisions, what a splendid place to" +
            " be!")]
        public string ServerMOTD {
            get {
                return ((string)(this["ServerMOTD"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://steamcommunity.com/id/reboot")]
        public string WebsiteLink {
            get {
                return ((string)(this["WebsiteLink"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <string>KF-BurningParis</string>
  <string>KF-Bioticslab</string>
  <string>KF-Outpost</string>
  <string>KF-VolterManor</string>
  <string>KF-Catacombs</string>
  <string>KF-EvacuationPoint</string>
  <string>KF-Farmhouse</string>
  <string>KF-BlackForest</string>
  <string>KF-Prison</string>
  <string>KF-ContainmentStation</string>
  <string>KF-HostileGrounds</string>
  <string>KF-InfernalRealm</string>
  <string>KF-ZedLanding</string>
  <string>KF-Nuked</string>
  <string>KF-TheDescent</string>
  <string>KF-TragicKingdom</string>
  <string>KF-Nightmare</string>
  <string>KF-KrampusLair</string>
  <string>KF-DieSector</string>
  <string>KF-PowerCore_Holdout</string>
  <string>KF-Lockdown</string>
  <string>KF-Airship</string>
  <string>KF-ShoppingSpree</string>
  <string>KF-MonsterBall</string>
  <string>KF-Santasworkshop</string>
  <string>KF-Spillway</string>
  <string>KF-SteamFortress</string>
  <string>KF-AshwoodAsylum</string>
  <string>KF-Sanitarium</string>
  <string>KF-Biolapse</string>
  <string>KF-Desolation</string>
  <string>KF-HellmarkStation</string>
  <string>KF-Elysium</string>
  <string>KF-Dystopia2029</string>
  <string>KF-Moonbase</string>
  <string>KF-Netherhold</string>
  <string>KF-CarillonHamlet</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection StockMaps {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["StockMaps"]));
            }
            set {
                this["StockMaps"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1802174804")]
        public string Collection {
            get {
                return ((string)(this["Collection"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection WorkshopMaps {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["WorkshopMaps"]));
            }
            set {
                this["WorkshopMaps"] = value;
            }
        }
    }
}
