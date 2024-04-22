using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace luval.mp.Metadata
{
    public class ImageMetadata
    {
        public ImageMetadata()
        {
            ExtendedProperties = new Dictionary<string, string>();
            GeoLocation = new ImageGeoLocation();
        }

        public DateTime? UtcDateTaken { get; set; }
        public ImageGeoLocation GeoLocation { get; set; }
        public Dictionary<string, string> ExtendedProperties { get; private set; }

    }
}
