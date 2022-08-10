using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using System.Text;

namespace ACLeaderboardAPI.Controllers {
    [Route("api/leaderboard")]
    [ApiController]
    public class LeaderboardController : ControllerBase {

        [HttpGet("gettablebb")]
        public async Task<string> GetTableBBCode(string url) {

            List<LapTime> laps = await GetLapTimes(url);

            if(laps != null && laps.Count > 0) {
                StringBuilder sb = new StringBuilder();
                sb.Append("[table][tr][th]Pos.[/th][th]Name[/th][th]Car[/th][th]Time[/th][th]Gap[/th][th]Date[/th][/tr]");
                foreach (var lap in laps) {
                    sb.Append("[tr]");
                    sb.Append(lap.Position);
                    sb.Append("[/tr][tr]");
                    sb.Append(lap.Name);
                    sb.Append("[/tr][tr]");
                    sb.Append(lap.Car);
                    sb.Append("[/tr][tr]");
                    sb.Append(lap.Time);
                    sb.Append("[/tr][tr]");
                    sb.Append(lap.Gap);
                    sb.Append("[/tr][tr]");
                    sb.Append(lap.Date);
                    sb.Append("[/tr]");
                }
                sb.Append("[/table]");

                return sb.ToString();
            }

            return "";
        }

        private async Task<List<LapTime>> GetLapTimes(string url) {
            // get the html from stracker
            string html = "";
            using (var client = new HttpClient()) {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await client.SendAsync(request);
                html = await response.Content.ReadAsStringAsync();
            }
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            List<HtmlNode> nodes = document.DocumentNode
                .Descendants()
                .Where(x => x.Attributes != null && x.Attributes.Count > 0 && x.Attributes.Contains("href") && x.Attributes["href"].Value.StartsWith("lapdetails"))
                .ToList();


            List<LapTime> times = new List<LapTime>();
            foreach (var node in nodes) {
                LapTime time = new LapTime();
                var laptimeNodes = node.Descendants().Where(x => x.Attributes != null && x.Attributes.Count > 0 && x.Attributes.Contains("class") && x.Attributes["class"].Value == "bestLap").ToList();
                time.Position = laptimeNodes.ElementAt(0).InnerHtml;
                time.Name = laptimeNodes.ElementAt(1).InnerHtml;
                time.Car = laptimeNodes.ElementAt(2).InnerHtml.Replace("\\n", "").Trim();
                time.Time = laptimeNodes.ElementAt(3).InnerHtml;
                time.Gap = laptimeNodes.ElementAt(4).InnerHtml;
                time.Date = node.ChildNodes.Last(x=>x.Name == "td").InnerHtml;

                times.Add(time);
            }
            return times;
        }
    }

    public class LapTime {
        public string? Position { get; set;}
        public string? Name { get; set; }
        public string? Car { get; set; }
        public string? Time { get; set; }
        public string? Gap { get; set; }
        public string? Date { get; set; }
    }
}
