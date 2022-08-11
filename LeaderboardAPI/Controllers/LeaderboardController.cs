using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic; 
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CoreHtmlToImage;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using LeaderboardAPI.Models;

namespace LeaderboardAPI.Controllers {

    public static class Storage {
        public static int LaptimesHash;
    }

    [Route("api/leaderboard")]
    [ApiController]
    public class LeaderboardController : ControllerBase {

        private readonly IHostingEnvironment env;
        public LeaderboardController(IHostingEnvironment env) {
            this.env = env;
        }

        [HttpGet("getleaderboardhtml")]
        public async Task<IActionResult> GetLeaderboardHtml(string url) {
            var decoded = System.Net.WebUtility.UrlDecode(url);
            List<LapTime> laps = await GetLapTimes(decoded);
            if (laps == null || laps.Count == 0) {
                return Ok();
            }
            var hash = laps.GetHashCode();
            if (hash == Storage.LaptimesHash) {
                return Ok();
            }
            if (laps != null && laps.Count > 0) {
                StringBuilder sb = new StringBuilder();
                sb.Append("<html style=\"padding-bottom: 0px; margin-bottom: 0px; display: block; height: auto;\"><link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/bootstrap@4.3.1/dist/css/bootstrap.min.css\" integrity=\"sha384-ggOyR0iXCbMQv3Xipma34MD+dH/1fQ784/j6cY/iJTQUOhcWr7x9JvoRxT2MZw1T\" crossorigin=\"anonymous\">");
                sb.Append("<body style=\"display: block; background-color:  #ff33cc;\"><div style=\"display: block; width: auto; height: 100%; !important; padding-bottom: 0px, margin-bottom: 0px;\"><table class=\"table table-dark table-responsive table-striped\" style=\"display: block; height: 100%;\"><tr><th>Pos.</th><th>Name</th><th>Car</th><th>Time</th><th>Gap</th></tr>");
                foreach (var lap in laps) {
                    sb.Append("<tr><td>");
                    sb.Append(lap.Position);
                    sb.Append("</td><td>");
                    sb.Append(lap.Name);
                    sb.Append("</td><td>");
                    sb.Append(lap.Car);
                    sb.Append("</td><td>");
                    sb.Append(lap.Time);
                    sb.Append("</td><td>");
                    sb.Append(lap.Gap);
                    sb.Append("</tr>");
                    //sb.Append("</td><td>");
                    //sb.Append(lap.Date);
                    //sb.Append("</td></tr>");
                }

                sb.Append("</table></div></body></html>");

                string html = sb.ToString();
                return Ok(html);
            }
            return Ok();
        }

        [HttpGet("getleaderboardimage")]
        public async Task<IActionResult> GetLeaderboardImage(string url) {
            var decoded = System.Net.WebUtility.UrlDecode(url);
            List<LapTime> laps = await GetLapTimes(decoded);
            if (laps == null || laps.Count == 0) {
                return Ok();
            }
            var hash = laps.GetHashCode();
            if (hash == Storage.LaptimesHash) {
                return Ok();
            }
            if (laps != null && laps.Count > 0) {
                StringBuilder sb = new StringBuilder();
                sb.Append("<html style=\"padding-bottom: 0px; margin-bottom: 0px; display: block; height: auto;\"><link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/bootstrap@4.3.1/dist/css/bootstrap.min.css\" integrity=\"sha384-ggOyR0iXCbMQv3Xipma34MD+dH/1fQ784/j6cY/iJTQUOhcWr7x9JvoRxT2MZw1T\" crossorigin=\"anonymous\">");
                sb.Append("<body style=\"display: block; background-color:  #ff33cc;\"><div style=\"display: block; width: auto; height: 100%; !important; padding-bottom: 0px, margin-bottom: 0px;\"><table class=\"table table-dark table-responsive table-striped\" style=\"display: block; height: 100%;\"><tr><th>Pos.</th><th>Name</th><th>Car</th><th>Time</th><th>Gap</th></tr>");
                foreach (var lap in laps) {
                    sb.Append("<tr><td>");
                    sb.Append(lap.Position);
                    sb.Append("</td><td>");
                    sb.Append(lap.Name);
                    sb.Append("</td><td>");
                    sb.Append(lap.Car);
                    sb.Append("</td><td>");
                    sb.Append(lap.Time);
                    sb.Append("</td><td>");
                    sb.Append(lap.Gap);
                    sb.Append("</tr>");
                    //sb.Append("</td><td>");
                    //sb.Append(lap.Date);
                    //sb.Append("</td></tr>");
                }

                sb.Append("</table></div></body></html>");

                string html = sb.ToString();

                byte[] img = new CoreHtmlToImage.HtmlConverter().FromHtmlString(html, 640, ImageFormat.Png, 100);

                var path = env.WebRootFileProvider.GetFileInfo("images/leaderboard.png")?.PhysicalPath;
                using (var ms = new MemoryStream(img)) {
                    using (var fs = new FileStream(path, FileMode.Create)) {
                        ms.WriteTo(fs);
                    }
                }
                return Ok();
            }
            return Ok();
        }

        [HttpGet("gettablebb")]
        public async Task<string> GetTableBBCode(string url) {

            var decoded = System.Net.WebUtility.UrlDecode(url);
            List<LapTime> laps = await GetLapTimes(decoded);

            if (laps != null && laps.Count > 0) {
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
                time.Date = node.ChildNodes.Last(x => x.Name == "td").InnerHtml;

                times.Add(time);
            }
            return times;
        }
    }

}

