using HtmlAgilityPack;
using LeaderboardAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LeaderboardAPI.Controllers.api {
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController : ControllerBase {

        private readonly ILogger<LeaderboardController> _logger;
        private readonly IConfiguration config;

        private readonly string statsBaseUrl;
        private readonly string serverBaseUrl;
        private readonly List<string> tracks;
        private readonly List<string> cars;
        private readonly List<string> driftCars;

        public LeaderboardController(ILogger<LeaderboardController> logger, IConfiguration configuration) {
            _logger = logger;
            config = configuration;
            statsBaseUrl = config["StatsBaseUrl"];
            serverBaseUrl = config["ServerBaseUrl"];
            tracks = config["Tracks"].Split(",").ToList();
            cars = config["Cars"].Split(",").ToList();
            driftCars = config["DriftCars"].Split(",").ToList();
        }

        [HttpGet()]

        public async Task<LeaderboardViewModel> Get(string track, bool oneEntryPerDriver = true, bool driftCarsOnly = false) {
            //http://plugins.barnabysbestmotoring.emperorservers.com:9611/lapstat?track=ek_akagi-downhill_real&cars=ddm_mitsubishi_evo_iv_gsr,ddm_mugen_civic_ek9,ddm_nissan_skyline_bnr32,ddm_subaru_22b,ddm_toyota_mr2_sw20,ks_mazda_rx7_spirit_r,ks_nissan_skyline_r34,ks_toyota_ae86_tuned,ks_toyota_supra_mkiv,wdts_nissan_180sx,wdts_nissan_laurel_c33,wdts_nissan_silvia_s13,wdts_nissan_skyline_r32,wdts_toyota_ae86,wdts_toyota_mark_ii_jzx90&valid=1,2,0&date_from=&date_to=&ranking=1
            //http://plugins.barnabysbestmotoring.emperorservers.com:9611/lapstat&track=ek_akagi-downhill_real&cars=ddm_mitsubishi_evo_iv_gsr,ddm_mugen_civic_ek9,ddm_nissan_skyline_bnr32,ddm_subaru_22b,ddm_toyota_mr2_sw20,ks_mazda_rx7_spirit_r,ks_nissan_skyline_r34,ks_toyota_ae86_tuned,ks_toyota_supra_mkiv,wdts_nissan_180sx,wdts_nissan_laurel_c33,wdts_nissan_silvia_s13,wdts_nissan_skyline_r32,wdts_toyota_ae86,wdts_toyota_mark_ii_jzx90&valid=1,2,0&date_from=&date_to=&ranking=1}
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

            LeaderboardViewModel vm = new LeaderboardViewModel();
            vm.LapTimes = await GetLapTimes(url.ToString());
            vm.DriftScore = await GetDriftScores("");


                return new LeaderboardViewModel();
        }

        private async Task<List<DriftScore>> GetDriftScores(string url) {
            string loginUrl = serverBaseUrl + "/login";
            Dictionary<string, StringValues> kvp = new System.Collections.Generic.Dictionary<string, StringValues>();
            kvp.Add("username", config["ApiUserName"]);
            kvp.Add("password", config["ApiPassword"]);
            FormCollection keys = new FormCollection(kvp);
            
            string html = "";
            using (var client = new HttpClient()) {

                HttpRequestMessage login = new HttpRequestMessage(HttpMethod.Post, loginUrl);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await client.SendAsync(request);
                html = await response.Content.ReadAsStringAsync();
            }

            HtmlDocument document = new HtmlDocument();

            return new List<DriftScore>();
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

    }
}
