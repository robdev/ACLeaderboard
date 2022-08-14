using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LeaderboardAPI.Models {

    public class LeaderboardViewModel {
        public List<LapTime> LapTimes { get; set; }
        public List<DriftScore>  DriftScore { get; set; }
    }
    public class LapTime {

        public string? Position { get; set; }
        public string? Name { get; set; }
        public string? Car { get; set; }
        public string? Time { get; set; }
        public string? Gap { get; set; }
        public string? Date { get; set; }
    }

    public class DriftScore {
        public string? Position { get; set; }
        public string? Name { get; set; }
        public int? Score { get; set; }
    }
}
