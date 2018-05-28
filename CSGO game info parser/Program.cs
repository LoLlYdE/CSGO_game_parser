using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace CSGO_game_info_parser {
    class Program {

        private static HtmlDocument document;

        static void Main(string[] args) {
            //assuming first arg is html file location
            loadHTML(args[0]);
            //assuming second arg is player id
            List<HtmlNode> nodes = parseHTML();
            List<Game> games =  parseNodes(nodes);
            doMathMagic(games);
            Console.ReadLine();
        }

        static void doMathMagic(List<Game> games) {
            int gamesCounter = 0, totalKills = 0, totalDeaths = 0, totalMVP = 0, totalAssist = 0;
            foreach (Game game in games) {
                gamesCounter++;
                foreach (Team team in game.Teams) {
                    for (int i = 0; i < 5; i++) {
                        totalKills += team.players[i].kills;
                        totalDeaths += team.players[i].deaths;
                        totalMVP += team.players[i].mvp;
                        totalAssist += team.players[i].assists;

                    }
                }
            }

            Console.WriteLine("total games: " + gamesCounter + Environment.NewLine
                            + "total kills: " + totalKills + Environment.NewLine
                            + "total deaths: " + totalDeaths + Environment.NewLine
                            + "total MVP's: " + totalMVP + Environment.NewLine
                            + "total assists: " + totalAssist + Environment.NewLine);
        }

        static void loadHTML(string filepath) {
            document = new HtmlDocument();
            document.Load(filepath);
        }

        static List<HtmlNode> parseHTML() {
            Console.WriteLine("parsehtml");
            HtmlNode node = document.GetElementbyId("personaldata_elements_container");
            HtmlNode data = node.FirstChild.NextSibling;
            List<HtmlNode> games = new List<HtmlNode> { };
            for (int i = 0; i < 8; i++) {
                games.Add(data.ChildNodes[1].ChildNodes[(i*2)+2]);
            }
            for (int i = 2; i < data.ChildNodes.Count-1; i++) {
                games.Add(data.ChildNodes[i]);
            }
            return games;
        }

        static List<Game> parseNodes(List<HtmlNode> nodes) {
            Console.WriteLine("parsenodes");
            List<Game> games = new List<Game> { };
            foreach (var node in nodes) {
                var temp = new Game();
                // set general info
                {
                    temp.map = removeWhitespaceExceptSpace(node.ChildNodes[1].ChildNodes[3].ChildNodes[1].ChildNodes[0].ChildNodes[1].FirstChild.InnerText); // trust me with this. it looks bad. I know.
                    var str = removeWhitespaceExceptSpace(node.ChildNodes[1].ChildNodes[3].ChildNodes[1].ChildNodes[2].ChildNodes[1].FirstChild.InnerText); // this looks worse but works
                    temp.time = Convert.ToDateTime(str);
                    //temp.time = DateTime.ParseExact(str,
                    //                                "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture); // TODO: implement timezones
                    str = removeWhitespaceExceptSpace(node.ChildNodes[1].ChildNodes[3].ChildNodes[1].ChildNodes[4].ChildNodes[1].FirstChild.InnerText);
                    str = str.Substring(str.IndexOf(' ') + 1); // isolate just the time
                    temp.waittime = TimeSpan.Parse(str);
                    str = removeWhitespaceExceptSpace(node.ChildNodes[1].ChildNodes[3].ChildNodes[1].ChildNodes[6].ChildNodes[1].FirstChild.InnerText);
                    str = str.Substring(str.IndexOf(' ') + 1); // isolate just the time
                    try {
                        temp.gametime = TimeSpan.ParseExact(str, "mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (FormatException e) {

                        temp.gametime = TimeSpan.Parse("00:" + str);
                    }
                }
                // set specific info
                {
                    Team team_one = new Team(), team_two = new Team();
                    List<HtmlNode> relevantNodes = new List<HtmlNode> { };

                    for (int i = 1; i < 12; i++) {
                        relevantNodes.Add(node.ChildNodes[3].ChildNodes[1].ChildNodes[1].ChildNodes[2*i]); // extract all relevant player/score nodes
                    }
                    var str = relevantNodes[5].InnerText; // extract final score and remove it from the nodes
                    relevantNodes.RemoveAt(5);
                    string[] scores = str.Split(' ');
                    team_one.score = Int32.Parse(scores[0]);
                    team_two.score = Int32.Parse(scores[2]);

                    team_one.players = new Player[5];
                    team_two.players = new Player[5];
                    for (int i = 0; i < 5; i++) {
                        team_one.players[i] = parsePlayer(relevantNodes[i]);
                    }

                    for (int i = 0; i < 5; i++) {
                        team_two.players[i] = parsePlayer(relevantNodes[i + 5]);
                    }

                    temp.Teams = new Team[] { team_one, team_two };
                }
                //Console.WriteLine(temp.ToString());
                games.Add(temp);
            }
            return games;
        }

        static private Player parsePlayer(HtmlNode node) {
            Player player = new Player();
            player.name = removeWhitespaceExceptSpace(node.ChildNodes[1].InnerText);
            player.ping = Int32.Parse(removeWhitespaceExceptSpace(node.ChildNodes[3].InnerText));
            player.kills = Int32.Parse(removeWhitespaceExceptSpace(node.ChildNodes[5].InnerText));
            player.assists = Int32.Parse(removeWhitespaceExceptSpace(node.ChildNodes[7].InnerText));
            player.deaths = Int32.Parse(removeWhitespaceExceptSpace(node.ChildNodes[9].InnerText));
            if (node.ChildNodes[11].InnerText == "") {
                player.mvp = 0;
            }
            else if (removeAllNonNumbers(node.ChildNodes[11].InnerText) == "") {
                player.mvp = 1;
            }
            else {
                player.mvp = Int32.Parse(removeAllNonNumbers(node.ChildNodes[11].InnerText));
            }
            player.hs_percent = float.Parse("0." + removeAllNonNumbers(node.ChildNodes[13].InnerText));
            player.score = Int32.Parse(removeAllNonNumbers(node.ChildNodes[15].InnerText));

            if(removeWhitespaceExceptSpace(node.ChildNodes[17].InnerText) != "") {
                if (node.ChildNodes[17].Attributes[2].Value.ToLower().Contains("red")) {
                    if (removeWhitespaceExceptSpace(node.ChildNodes[17].InnerText).ToLower() == "vac") {
                        player.banstatus = Banstatus.VAC_AFTER;
                    }
                    else {
                        player.banstatus = Banstatus.GAME_AFTER;
                    }
                }
                else {
                    if (removeWhitespaceExceptSpace(node.ChildNodes[17].InnerText).ToLower() == "vac") {
                        player.banstatus = Banstatus.VAC_BEFORE;
                    }
                    else {
                        player.banstatus = Banstatus.GAME_BEFORE;
                    }
                }
            }
            else {
                player.banstatus = Banstatus.NO_BAN;
            }



            return player;
        }

        static string removeWhitespaceExceptSpace(string toReplace) {
            return Regex.Replace(toReplace, @"[^\S ]+", "");
        }

        static string removeAllNonNumbers(string toReplace) {
            return Regex.Replace(toReplace, @"[^\d]", "");
        }

        struct Game {
            public String map;
            public DateTime time;
            public TimeSpan waittime, gametime;
            public Team[] Teams;
            public override string ToString() {
                return "map: " + map + Environment.NewLine
                    + "time: " + time + Environment.NewLine
                    + "waittime: " + waittime + Environment.NewLine
                    + "gametime: " + gametime + Environment.NewLine;

            }
        }

        struct Team {
            public Player[] players;
            public int score;
        }

        struct Player {
            public string name, steam_id;
            public int ping, kills, assists, deaths, mvp, score;
            public float hs_percent;
            public Banstatus banstatus;
        }

        enum Banstatus {
            NO_BAN,
            VAC_BEFORE,
            VAC_AFTER,
            GAME_BEFORE,
            GAME_AFTER
        }
    }
}
