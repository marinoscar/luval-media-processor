using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace luval.mp.Metadata
{
    public static class MediaMetadataReader
    {

        public static ImageMetadata FromFile2(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException("filePath");
            if (!File.Exists(filePath)) throw new FileNotFoundException("Invalid file path provided", filePath);
            using (var stream = File.OpenRead(filePath))
            {
                return FromStream2(stream);
            }
            return null;
        }

        public static ImageMetadata FromStream2(Stream stream)
        {
            var result = new ImageMetadata();
            var directories = ImageMetadataReader.ReadMetadata(stream);
            var tags = directories.SelectMany(i => i.Tags).ToList();
            foreach (var dir in directories)
            {
                Debug.WriteLine(dir.Name);
                foreach (var tag in dir.Tags)
                {
                    var val = dir.GetObject(tag.TagType);
                    if (val != null && tag.TagName != null)
                        result.ExtendedProperties[tag.TagName] = Convert.ToString(val);

                    Debug.WriteLine("Tag: {0} and Value: {1}", tag.TagName, val);
                    if (tag.TagName.ToLower().Contains("gps location"))
                    {
                        var cor = new Cordinate();
                        cor.ParseIsoString(tag.Description);
                        result.GeoLocation = new ImageGeoLocation() { Latitude = cor.Latitude, Longitude = cor.Longitude };
                    }
                    if (tag.TagName.ToLower().Equals("gps latitude"))
                    {
                        result.GeoLocation.Latitude = Cordinate.ParseFromDegMinAndSec(tag.Description);
                    }
                    if (tag.TagName.ToLower().Equals("gps longitude"))
                    {
                        result.GeoLocation.Longitude = Cordinate.ParseFromDegMinAndSec(tag.Description);
                    }
                }
            }
            if (result.ExtendedProperties.ContainsKey("Date/Time Original"))
            {
                var dt = result.ExtendedProperties["Date/Time Original"];
                string tz = null;
                if(result.ExtendedProperties.ContainsKey("Time Zone Original"))
                    tz = result.ExtendedProperties["Time Zone Original"];

                var time = Cordinate.ParseDateAndTime(dt, tz);
                if (time != null) result.UtcDateTaken = time.Value.UtcDateTime;
            }

            var gps = directories
                             .OfType<GpsDirectory>()
                             .FirstOrDefault();
            if(gps != null)
            {
                var location = gps.GetGeoLocation();
                result.GeoLocation = new ImageGeoLocation() { Latitude = location.Latitude, Longitude = location.Latitude };
            }


            return result;
        }

        public static ImageMetadata FromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException("filePath");
            if (!File.Exists(filePath)) throw new FileNotFoundException("Invalid file path provided", filePath);
            using (var fs = File.OpenRead(filePath))
            {
                return FromStream(fs);
            }

        }

        public static ImageMetadata FromStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            stream.Position = 0;
            var info = SixLabors.ImageSharp.Image.Identify(stream);
            if (info == null) return null;
            return new ImageMetadata()
            {
                GeoLocation = LocationFromMetadata(info.Metadata.ExifProfile.Values),
                UtcDateTaken = GetDate(info.Metadata.ExifProfile.Values)
            };
        }

        private static DateTime? GetDate(IReadOnlyList<IExifValue> values)
        {
            var offset = TimeSpan.FromSeconds(0);
            var dt = values.FirstOrDefault(i => Convert.ToString(i.Tag) == "DateTimeOriginal");
            var os = values.FirstOrDefault(i => Convert.ToString(i.Tag) == "OffsetTimeOriginal");
            if (dt == null) return null;
            var strDate = Convert.ToString(dt.GetValue());
            DateTime value = new DateTime();
            if (!DateTime.TryParseExact(strDate, "yyyy:MM:dd HH:mm:ss",
                CultureInfo.CurrentCulture, DateTimeStyles.None, out value))
            {
                return null;
            }
            if (os != null && TimeSpan.TryParse(Convert.ToString(os.GetValue()), out offset))
            {
                value = value.AddHours(offset.TotalHours);
            }
            return value;
        }

        private static ImageGeoLocation LocationFromMetadata(IReadOnlyList<IExifValue> values)
        {
            var lonRef = values.FirstOrDefault(i => Convert.ToString(i.Tag) == "GPSLongitudeRef");
            var latRef = values.FirstOrDefault(i => Convert.ToString(i.Tag) == "GPSLatitudeRef");
            var lat = values.FirstOrDefault(i => Convert.ToString(i.Tag) == "GPSLatitude");
            var lon = values.FirstOrDefault(i => Convert.ToString(i.Tag) == "GPSLongitude");
            var alt = values.FirstOrDefault(i => Convert.ToString(i.Tag) == "GPSAltitude");
            return new ImageGeoLocation()
            {
                Latitude = GetDouble(lat, latRef),
                Longitude = GetDouble(lon, lonRef),
                Altitude = GetAltitude(alt)
            };
        }

        private static double? GetAltitude(IExifValue? gpsRead)
        {
            var res = 0d;
            if (gpsRead == null) return null;
            var strDouble = Convert.ToString(gpsRead.GetValue());
            if (double.TryParse(strDouble, out res)) return res;
            else return null;
        }

        private static double? GetDouble(IExifValue? gpsRead, IExifValue? gpsRef)
        {
            var gpsRefValue = string.Empty;
            if (gpsRead == null) return null;

            if (gpsRef != null)
            {
                gpsRefValue = Convert.ToString(gpsRef.GetValue());
            }

            var rational = (SixLabors.ImageSharp.Rational[])gpsRead.GetValue();
            if (rational.Length < 3) return null;

            uint degreesNumerator = rational[0].Numerator;
            uint degreesDenominator = rational[0].Denominator;
            double degrees = degreesNumerator / (double)degreesDenominator;


            uint minutesNumerator = rational[1].Numerator;
            uint minutesDenominator = rational[1].Denominator;
            double minutes = minutesNumerator / (double)minutesDenominator;

            uint secondsNumerator = rational[2].Numerator;
            uint secondsDenominator = rational[2].Denominator;
            double seconds = secondsNumerator / (double)secondsDenominator;

            double coorditate = degrees + (minutes / 60d) + (seconds / 3600d);

            if (gpsRefValue == "S" || gpsRefValue == "W")
            {
                coorditate = coorditate * -1;
            }
            return double.NaN.Equals(coorditate) ? null : coorditate;
        }

    }
}
