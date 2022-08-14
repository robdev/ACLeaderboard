using HtmlAgilityPack;
using LeaderboardAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace LeaderboardAPI.Controllers {
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration config;

        private readonly string statsBaseUrl;
        private readonly List<string> tracks;
        private readonly List<string> cars;
        private readonly List<string> driftCars;
        private readonly string serverBaseUrl;
        private static DateTime lastUpdated = DateTime.MinValue;

        private static List<DriftScore> driftScores = new List<DriftScore>();

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration) {
            _logger = logger;
            config = configuration;
            statsBaseUrl = config["StatsBaseUrl"];
            serverBaseUrl = config["ServerBaseUrl"];
            tracks = config["Tracks"].Split(",").ToList();
            cars = config["Cars"].Split(",").ToList();
            driftCars = config["DriftCars"].Split(",").ToList();
        }

        public async Task<IActionResult> Index(string track = "akagi", bool oneEntryPerDriver = true, bool driftCarsOnly = false, bool forceRefresh = false) {

            ////http://plugins.barnabysbestmotoring.emperorservers.com:9611/lapstat?track=ek_akagi-downhill_real&cars=ddm_mitsubishi_evo_iv_gsr,ddm_mugen_civic_ek9,ddm_nissan_skyline_bnr32,ddm_subaru_22b,ddm_toyota_mr2_sw20,ks_mazda_rx7_spirit_r,ks_nissan_skyline_r34,ks_toyota_ae86_tuned,ks_toyota_supra_mkiv,wdts_nissan_180sx,wdts_nissan_laurel_c33,wdts_nissan_silvia_s13,wdts_nissan_skyline_r32,wdts_toyota_ae86,wdts_toyota_mark_ii_jzx90&valid=1,2,0&date_from=&date_to=&ranking=1
            ////http://plugins.barnabysbestmotoring.emperorservers.com:9611/lapstat&track=ek_akagi-downhill_real&cars=ddm_mitsubishi_evo_iv_gsr,ddm_mugen_civic_ek9,ddm_nissan_skyline_bnr32,ddm_subaru_22b,ddm_toyota_mr2_sw20,ks_mazda_rx7_spirit_r,ks_nissan_skyline_r34,ks_toyota_ae86_tuned,ks_toyota_supra_mkiv,wdts_nissan_180sx,wdts_nissan_laurel_c33,wdts_nissan_silvia_s13,wdts_nissan_skyline_r32,wdts_toyota_ae86,wdts_toyota_mark_ii_jzx90&valid=1,2,0&date_from=&date_to=&ranking=1}
            var currentTrack = GetTrackIdFromUrl(track);
            List<SelectListItem> tracks = new List<SelectListItem>();

            StringBuilder url = new StringBuilder();
            url.Append(config["StatsBaseUrl"]);
            url.Append("?track=" + currentTrack);
            if (driftCarsOnly) {
                url.Append("&cars=" + string.Join(",", driftCars));
            } else {
                url.Append("&cars=" + string.Join(",", cars));
            }
            url.Append("&valid=1,2,0&date_from=&date_to=");
            if (oneEntryPerDriver) {
                url.Append("&ranking=1");
            }

            ViewBag.OneEntryPerDriver = oneEntryPerDriver;
            ViewBag.DriftCarsOnly = driftCarsOnly;
            ViewBag.track = tracks;
            ViewBag.CurrentTrack = track;
            ViewBag.LeaderboardName = GetTrackNameFromDirName(currentTrack);

            string driftUrl = config[""];

            ViewBag.LapTimes = await GetLapTimes(url.ToString());
            ViewBag.DriftScores = await GetDriftScores(forceRefresh);

            return View();
        }



        private async Task<List<DriftScore>> GetDriftScores(bool forceRefresh) {

            if (forceRefresh || ((driftScores == null || driftScores.Count == 0) || (lastUpdated != DateTime.MinValue && (DateTime.Now - lastUpdated).TotalHours > 0.5))) {
                List<DriftScore> scores = new List<DriftScore>();

                var parameters = new AzureFunctionParams();

                parameters.Url = config["ServerBaseUrl"] + "live-timing";
                parameters.UserName = config["ApiUserName"];
                parameters.Password = config["ApiPassword"];
                string json = JsonConvert.SerializeObject(parameters);

                string html = "";
                using (HttpClient client = new HttpClient()) {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, config["AzureFunctionUrl"]);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                  
                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode) {
                        html = await response.Content.ReadAsStringAsync();
                    } else {
                        return driftScores;
                    }
                }

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                List<HtmlNode> driverNodes = doc.DocumentNode
                    .Descendants()
                    .Where(x => x.HasClass("driver-row"))
                    .ToList();

                if (driverNodes.Count == 0) {
                    return driftScores;
                }
                foreach (var node in driverNodes) {
                    if (node.Descendants().Any(x => x.HasClass("drift-best-lap-score"))) {
                        var name = node.Descendants().Where(x => x.HasClass("driver-name"))
                            .ToList().First().InnerHtml;
                        int index = name.IndexOf("<");
                        if (index >= 0)
                            name = name.Substring(0, index);
                        var score = node.Descendants().Where(x => x.HasClass("drift-best-lap-score"))
                            .ToList().First().InnerHtml.Replace("Best Score: ", "");
                        if (!scores.Any(x => x.Name == name)) {
                            scores.Add(new DriftScore { Name = name, Score = int.Parse(score) });
                        }
                    }
                }
                if (scores.Count > 0) {
                    scores = scores.OrderByDescending(x => x.Score).ToList();
                    foreach (var score in scores) {
                        score.Position = (scores.IndexOf(score) + 1).ToString();
                    }
                }

                
                driftScores = scores;
                lastUpdated = DateTime.Now;
            }
            return driftScores;
        }

        private string GetTrackIdFromUrl(string track) {
            if (track == null) return "ek_akagi-downhill_real";
            switch (track.ToLower()) {
                case "akagi":
                case "ek_akagi-downhill_real":
                case "ek_akagi_downhill_real":
                case "akagi-downhill":
                case "akagidownhill":
                    return "ek_akagi-downhill_real";
                default:
                    return "ek_akagi-downhill_real";
            }
        }

        private async Task<List<LapTime>> GetLapTimes(string url) {
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
                var laptimeNodes = node.Descendants().Where(x => x.Name == "td").ToList();

                time.Position = laptimeNodes.ElementAt(0).InnerHtml;
                time.Position = time.Position.Remove(time.Position.Length - 1, 1);
                time.Name = laptimeNodes.ElementAt(1).InnerHtml;
                time.Car = GetCarNameFromDirName(laptimeNodes.ElementAt(2).InnerHtml.Replace("\\n", "").Trim());
                time.Time = laptimeNodes.ElementAt(3).InnerHtml;
                time.Gap = laptimeNodes.ElementAt(4).InnerHtml;
                if (time.Gap == "+00.000") {
                    time.Gap = " --.---";
                }

                times.Add(time);
            }
            return times;
        }

        private string GetTrackNameFromDirName(string dirName) {
            switch (dirName) {
                case "ek_akagi-downhill_real":
                    return "Akagi Downhill";
                default:
                    return dirName;
            }
        }

        private string GetCarNameFromDirName(string dirName) {
            switch (dirName) {
                case "ddm_mitsubishi_evo_iv_gsr":
                    return "Mitsubishi Lancer Evo IV GSR";
                case "ddm_mugen_civic_ek9":
                    return "Mugen Civic Type SS (EK9)";
                case "ddm_nissan_skyline_bnr32":
                    return "Nissan Skyline GT-R V-Spec II (R32)";
                case "ddm_subaru_22b":
                    return "Subaru Impreza 22B STi-Version";
                case "ddm_toyota_mr2_sw20":
                    return "Toyota MR2 GT-S";
                case "ks_mazda_rx7_spirit_r":
                    return "Mazda RX-7 Spirit R";
                case "ks_nissan_skyline_r34":
                    return "Nissan Skyline R34";
                case "ks_toyota_ae86_tuned":
                    return "Toyota AE86 Tuned";
                case "ks_toyota_supra_mkiv":
                    return "Toyota Supra MKIV";
                case "wdts_nissan_180sx":
                    return "Nissan 180SX WDT Street";
                case "wdts_nissan_laurel_c33":
                    return "Nissan Laurel C33 WDT Street";
                case "wdts_nissan_silvia_s13":
                    return "Nissan Silva S13 WDT Street";
                case "wdts_nissan_skyline_r32":
                    return "Nissan Skyline R32 WDT Street";
                case "wdts_toyota_ae86":
                    return "Toyota AE86 WDT Street";
                case "wdts_toyota_mark_ii_jzx90":
                    return "Toyota Mark II JZX90 WDT Street";

                default:
                    return dirName;
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
    [System.Serializable]
    public class AzureFunctionParams {
        public string Url { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
