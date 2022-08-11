using HtmlAgilityPack;
using LeaderboardAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LeaderboardAPI.Controllers {
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger) {
            _logger = logger;
        }

        public async Task<IActionResult> Index() {
            ViewBag.LapTimes = await GetLapTimes("http://plugins.barnabysbestmotoring.emperorservers.com:9611/lapstat?track=ek_akagi-downhill_real&cars=ddm_mitsubishi_evo_iv_gsr,ddm_mugen_civic_ek9,ddm_nissan_skyline_bnr32,ddm_subaru_22b,ddm_toyota_mr2_sw20,ks_mazda_rx7_spirit_r,ks_nissan_skyline_r34,ks_toyota_ae86_tuned,ks_toyota_supra_mkiv,wdts_nissan_180sx,wdts_nissan_laurel_c33,wdts_nissan_silvia_s13,wdts_nissan_skyline_r32,wdts_toyota_ae86,wdts_toyota_mark_ii_jzx90&valid=1,2,0&date_from=&date_to=&groups=0&ranking=1");
            return View();
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
                var laptimeNodes = node.Descendants().Where(x => x.Attributes != null && x.Attributes.Count > 0 && x.Attributes.Contains("class") && x.Attributes["class"].Value == "bestLap").ToList();

                time.Position = laptimeNodes.ElementAt(0).InnerHtml;
                time.Position = time.Position.Remove(time.Position.Length - 1, 1);
                time.Name = laptimeNodes.ElementAt(1).InnerHtml;
                time.Car = GetCarNameFromDirName(laptimeNodes.ElementAt(2).InnerHtml.Replace("\\n", "").Trim());
                time.Time = laptimeNodes.ElementAt(3).InnerHtml;
                time.Gap = laptimeNodes.ElementAt(4).InnerHtml;
                if (time.Gap == "+00.000") {
                    time.Gap = " --.---";
                }
                //time.Date = node.ChildNodes.Last(x => x.Name == "td").InnerHtml;

                times.Add(time);
            }
            return times;
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

        public IActionResult Privacy() {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
