namespace SMan;

public class Multi
{
    public static void Main()
    {
        KF2.Clean();
        KF2.Update();
        var IDs = KF2.GetIDs(1802174804);
        var Maps = KF2.GetMaps(new[] { "KF-BurningParis", "KF-Bioticslab", "KF-Outpost", "KF-VolterManor", "KF-Catacombs", "KF-EvacuationPoint", "KF-Farmhouse", "KF-BlackForest", "KF-Prison", "KF-ContainmentStation", "KF-HostileGrounds", "KF-InfernalRealm", "KF-ZedLanding", "KF-Nuked", "KF-TheDescent", "KF-TragicKingdom", "KF-Nightmare", "KF-KrampusLair", "KF-DieSector", "KF-PowerCore_Holdout", "KF-Lockdown", "KF-Airship", "KF-ShoppingSpree", "KF-MonsterBall", "KF-Santasworkshop", "KF-Spillway", "KF-SteamFortress", "KF-AshwoodAsylum", "KF-Sanitarium", "KF-Biolapse", "KF-Desolation", "KF-HellmarkStation", "KF-Elysium", "KF-Dystopia2029", "KF-Moonbase", "KF-Netherhold", "KF-CarillonHamlet" }, IDs);
        var Scrap = new KF2()
        {
            ServerName = "ReBoot your mind",
            AdminPassword = "ficken",
            GamePassword = "pons",
            BannerLink = "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avatars/1f/1fa222692912b7d56a65e6ff8593f5cb8b4236fa_full.jpg",
            WebsiteLink = "http://steamcommunity.com/id/reboot",
            ServerMOTD = new[] { "A fine selection of workshop maps, no player collisions, what a splendid place to be!" },
            UsedForTakeover = false,
            Difficulty = KF2.Difficultues.Hard,
            GameLength = KF2.GameLengths.Normal,
        };
        Scrap.Run(Maps.Item1.Concat(Maps.Item2), IDs);
    }
}