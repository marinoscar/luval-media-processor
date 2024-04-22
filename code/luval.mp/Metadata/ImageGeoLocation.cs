using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace luval.mp.Metadata
{
    /// <summary>
    /// Image geo location
    /// </summary>
    public class ImageGeoLocation
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }

        public override string ToString()
        {
            return String.Format("{0};{1};{2}", Latitude, Longitude, Altitude);
        }
    }
}
