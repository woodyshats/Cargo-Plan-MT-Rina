using System.Collections.Generic;

namespace CargoPlanApp.Models
{
    public class CargoPlanData
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public Dictionary<string, double> TankMaxVolumes { get; set; } = new Dictionary<string, double>();
        public List<TankState> TankStates { get; set; } = new List<TankState>();
        public List<BallastTankState> BallastStates { get; set; } = new List<BallastTankState>();
        public VesselInfo VesselInfo { get; set; } = new VesselInfo();
    }

    public class TankState
    {
        public string TankNo { get; set; }
        public string Grade { get; set; }
        public double? Weight { get; set; }
        public double? Temperature { get; set; }
    }

    public class BallastTankState
    {
        public string TankNo { get; set; }
        public double? WaterVolume { get; set; }
    }

    public class VesselInfo
    {
        public string Date { get; set; }
        public string Voyage { get; set; }
        public string PortOfLoading { get; set; }
        public string PortOfDischarge { get; set; }
        public string Terminal { get; set; }
        public string PlanType { get; set; }
        public double? Draft { get; set; }
        public double? WaterDensity { get; set; }
    }
}