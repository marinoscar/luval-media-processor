﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace luval.mp.Metadata
{
    [Serializable()]
    public class Cordinate : ICloneable, IXmlSerializable, IFormattable
    {
        #region Private fields   // Internal storage
        private float latitude;  // Expressed in seconds of degree, positive values for north
        private float longitude; // Expressed in seconds of degree, positive values for east
        #endregion

        #region Constructors
        public Cordinate()
        {
            Latitude = Longitude = 0.0f;
        }
        public Cordinate(float lat, float lon)  // Values expressed in degrees, for user convenience
        {
            Latitude = lat;
            Longitude = lon;
        }
        #endregion

        #region Properties
        public float Latitude
        {
            set
            {
                latitude = value * 3600.0f;
            }
            get
            {
                return latitude / 3600.0f;  // return degrees 
            }
        }
        public float Longitude
        {
            set
            {
                longitude = value * 3600.0f;
            }
            get
            {
                return longitude / 3600.0f;  // return degrees
            }
        }
        #endregion

        #region Public methods
        // Multi-argument setters
        public void SetD(float latDeg, float lonDeg)
        {
            latitude = latDeg * 3600;  // Convert to seconds
            longitude = lonDeg * 3600; // Convert to seconds
        }
        public void SetDM(float latDeg, float latMin, bool north, float lonDeg, float lonMin, bool east)
        {
            latitude = (latDeg * 3600 + latMin * 60) * (north ? 1 : -1);
            longitude = (lonDeg * 3600 + lonMin * 60) * (east ? 1 : -1);
        }
        public void SetDMS(float latDeg, float latMin, float latSec, bool north, float lonDeg, float lonMin, float lonSec, bool east)
        {
            latitude = (latDeg * 3600 + latMin * 60 + latSec) * (north ? 1 : -1);
            longitude = (lonDeg * 3600 + lonMin * 60 + lonSec) * (east ? 1 : -1);
        }

        // Multi-argument getters
        public void GetD(out float latDeg, out float lonDeg)
        {
            latDeg = this.latitude / 3600.0f;
            lonDeg = this.longitude / 3600.0f;
        }
        public void GetDM(out float latDeg, out float latMin, out bool north, out float lonDeg, out float lonMin, out bool east)
        {
            north = this.latitude >= 0;
            double a = Math.Abs(this.latitude);
            latDeg = (float)Math.Truncate(a / 3600.0);
            latMin = (float)(a - latDeg * 3600.0) / 60.0f;

            east = this.longitude >= 0;
            double b = Math.Abs(this.longitude);
            lonDeg = (float)Math.Truncate(b / 3600.0);
            lonMin = (float)(b - lonDeg * 3600.0) / 60.0f;
        }
        public void GetDMS(out float latDeg, out float latMin, out float latSec, out bool north, out float lonDeg, out float lonMin, out float lonSec, out bool east)
        {
            north = this.latitude >= 0;
            double a = Math.Abs(this.latitude);
            latDeg = (float)Math.Truncate(a / 3600.0);
            a -= latDeg * 3600.0;
            latMin = (float)Math.Truncate(a / 60.0);
            latSec = (float)(a - latMin * 60.0);

            east = this.longitude >= 0;
            double b = Math.Abs(this.longitude);
            lonDeg = (float)Math.Truncate(b / 3600.0);
            b -= lonDeg * 3600.0;
            lonMin = (float)Math.Truncate(b / 60.0);
            lonSec = (float)(b - lonMin * 60.0);
        }

        // Distance in meters
        public float Distance(Cordinate other)
        {
            const double rad = Math.PI / 180;

            // Use the Harversine Formula (Great circle), have a look to:
            // http://en.wikipedia.org/wiki/Great-circle_distance
            // http://williams.best.vwh.net/avform.htm (Aviation Formulary)
            double lon1 = rad * -Longitude;
            double lat1 = rad * Latitude;
            double lon2 = rad * -other.Longitude;
            double lat2 = rad * other.Latitude;

            //	d=2*asin(sqrt((sin((lat1-lat2)/2))^2 + cos(lat1)*cos(lat2)*(sin((lon1-lon2)/2))^2))
            double d = 2 * Math.Asin(Math.Sqrt(
                Math.Pow(Math.Sin((lat1 - lat2) / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin((lon1 - lon2) / 2), 2)
            ));
            return (float)(1852 * 60 * d / rad);
        }
        // Parsing method
        public void ParseIsoString(string isoStr)
        {
            // Parse coordinate in the following ISO 6709 formats:
            // Latitude and Longitude in Degrees:
            // �DD.DDDD�DDD.DDDD/         (eg +12.345-098.765/)
            // Latitude and Longitude in Degrees and Minutes:
            // �DDMM.MMMM�DDDMM.MMMM/     (eg +1234.56-09854.321/)
            // Latitude and Longitude in Degrees, Minutes and Seconds:
            // �DDMMSS.SSSS�DDDMMSS.SSSS/ (eg +123456.7-0985432.1/)
            // Latitude, Longitude (in Degrees) and Altitude:
            // �DD.DDDD�DDD.DDDD�AAA.AAA/         (eg +12.345-098.765+15.9/)
            // Latitude, Longitude (in Degrees and Minutes) and Altitude:
            // �DDMM.MMMM�DDDMM.MMMM�AAA.AAA/     (eg +1234.56-09854.321+15.9/)
            // Latitude, Longitude (in Degrees, Minutes and Seconds) and Altitude:
            // �DDMMSS.SSSS�DDDMMSS.SSSS�AAA.AAA/ (eg +123456.7-0985432.1+15.9/)
            if (string.IsNullOrWhiteSpace(isoStr)) return;

            if (isoStr.Length < 18)  // Check for minimum length
                isoStr = null;

            if (string.IsNullOrWhiteSpace(isoStr)) return;

            if (!isoStr.EndsWith("/"))  // Check for trailing slash
                isoStr = null;
            isoStr = isoStr.Remove(isoStr.Length - 1); // Remove trailing slash

            string[] parts = isoStr.Split(new char[] { '+', '-' }, StringSplitOptions.None);
            if (parts.Length < 3 || parts.Length > 4)  // Check for parts count
                parts = null;
            if (parts[0].Length != 0)  // Check if first part is empty
                parts = null;

            int point = parts[1].IndexOf('.');
            if (point != 2 && point != 4 && point != 6) // Check for valid lenght for lat/lon
                parts = null;
            if (point != parts[2].IndexOf('.') - 1) // Check for lat/lon decimal positions
                parts = null;

            NumberFormatInfo fi = NumberFormatInfo.InvariantInfo;

            // Parse latitude and longitude values, according to format
            if (point == 2)
            {
                latitude = float.Parse(parts[1], fi) * 3600;
                longitude = float.Parse(parts[2], fi) * 3600;
            }
            else if (point == 4)
            {
                latitude = float.Parse(parts[1].Substring(0, 2), fi) * 3600 + float.Parse(parts[1].Substring(2), fi) * 60;
                longitude = float.Parse(parts[2].Substring(0, 3), fi) * 3600 + float.Parse(parts[2].Substring(3), fi) * 60;
            }
            else  // point==8
            {
                latitude = float.Parse(parts[1].Substring(0, 2), fi) * 3600 + float.Parse(parts[1].Substring(2, 2), fi) * 60 + float.Parse(parts[1].Substring(4), fi);
                longitude = float.Parse(parts[2].Substring(0, 3), fi) * 3600 + float.Parse(parts[2].Substring(3, 2), fi) * 60 + float.Parse(parts[2].Substring(5), fi);
            }
            // Parse altitude, just to check if it is valid
            if (parts.Length == 4)
                float.Parse(parts[3], fi);

            // Add proper sign to lat/lon
            if (isoStr[0] == '-')
                latitude = -latitude;
            if (isoStr[parts[1].Length + 1] == '-')
                longitude = -longitude;
        }

        public static double? ParseFromDegMinAndSec(string location)
        {
            double? result = null;
            var items = "°'\"".ToCharArray();
            if (string.IsNullOrWhiteSpace(location)) return result;
            var chars = location.ToCharArray();
            if (!(location.Contains(items[0]) && location.Contains(items[1]) && location.Contains(items[2]))) return result;
            var data = new string(location.ToCharArray());
            var degrees = Convert.ToDouble(Regex.Match(data, ".*°").Value.Trim().Replace("°", ""));
            var mins = Convert.ToDouble(Regex.Match(data, "°.*'").Value.Trim().Replace("°", "").Replace("'", ""));
            var secs = Convert.ToDouble(Regex.Match(data, "'.*\"").Value.Trim().Replace("'", "").Replace("\"", ""));
            if (degrees < 0)
                result = degrees + (mins * -1) / 60 + (secs * -1) / 3600;
            else
                result = degrees + mins / 60 + secs / 3600;
            return Math.Round(result.Value, 5);
        }

        public static DateTimeOffset? ParseDateAndTime(string dateTime, string timezone)
        {
            var result = default(DateTimeOffset?);
            var offset = new TimeSpan();
            if (string.IsNullOrWhiteSpace(dateTime)) return result;
            var parts = dateTime.Split(' ');
            var dateStr = parts[0];
            var timeStr = parts[1];
            var dateParts = dateStr.Split(':').Select(i => Convert.ToInt32(i)).ToList();
            var time = TimeSpan.Parse(timeStr);
            if (!string.IsNullOrWhiteSpace(timezone))
                offset = TimeSpan.Parse(timezone);
            result = new DateTimeOffset(dateParts[0], dateParts[1], dateParts[2], time.Hours, time.Minutes, time.Seconds, offset);
            return result;
        }

        #endregion

        #region Overrides
        public override string ToString()
        {
            return this.ToString(null);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != this.GetType())
                return false;

            Cordinate other = (Cordinate)obj;
            return (this.latitude == other.latitude) && (this.longitude == other.longitude);
        }
        public override int GetHashCode()
        {
            return (latitude + longitude).GetHashCode();
        }
        #endregion

        #region IFormattable Members
        // Not really IFormattable member
        public string ToString(string format)
        {
            return this.ToString(format, null);
        }
        // ToString version with formatting
        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            float latDeg, latMin, latSec, lonDeg, lonMin, lonSec;
            bool north, east;

            if (format == null)
                format = "DMS";

            NumberFormatInfo fi = NumberFormatInfo.InvariantInfo;

            switch (format.ToUpper())
            {
                case "":
                case "DMS":
                    this.GetDMS(out latDeg, out latMin, out latSec, out north, out lonDeg, out lonMin, out lonSec, out east);
                    sb.AppendFormat(fi, "{0:0#}�{1:0#}'{2:0#.0}\"{3} ", latDeg, latMin, latSec, (north ? "N" : "S"));
                    sb.AppendFormat(fi, "{0:0##}�{1:0#}'{2:0#.0}\"{3}", lonDeg, lonMin, lonSec, (east ? "E" : "W"));
                    break;
                case "D":
                    sb.AppendFormat(fi, "{0:0#.0000}�{1} ", Math.Abs(Latitude), (latitude > 0 ? "N" : "S"));
                    sb.AppendFormat(fi, "{0:0##.0000}�{1}", Math.Abs(Longitude), (longitude > 0 ? "E" : "W"));
                    break;
                case "DM":
                    this.GetDM(out latDeg, out latMin, out north, out lonDeg, out lonMin, out east);
                    sb.AppendFormat(fi, "{0:0#}�{1:0#.00}'{2} ", latDeg, latMin, (north ? "N" : "S"));
                    sb.AppendFormat(fi, "{0:0##}�{1:0#.00}'{2}", lonDeg, lonMin, (east ? "E" : "W"));
                    break;
                case "ISO":
                    // Write coordinate according with ISO 6709
                    // Latitude and Longitude in Degrees, Minutes and Seconds:
                    // �DDMMSS.SSSS�DDDMMSS.SSSS/ (eg +123456.7-0985432.1/)

                    this.GetDMS(out latDeg, out latMin, out latSec, out north, out lonDeg, out lonMin, out lonSec, out east);
                    sb.AppendFormat(fi, "{0}{1:0#}{2:0#}{3:0#.0#}", (north ? "+" : "-"), latDeg, latMin, latSec);
                    sb.AppendFormat(fi, "{0}{1:0##}{2:0#}{3:0#.0#}/", (east ? "+" : "-"), lonDeg, lonMin, lonSec);
                    break;
                default:
                    throw new Exception("Coordinate.ToString(): Invalid formatting string.");
            }
            return sb.ToString();
        }
        #endregion

        #region ICloneable Members
        object ICloneable.Clone()
        {
            return new Cordinate(this.Latitude, this.Longitude);
        }
        #endregion

        #region IXmlSerializable Members
        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            try
            {
                string str = reader.ReadElementContentAsString().Trim();
                this.ParseIsoString(str);
            }
            catch
            {
                throw new Exception("Coordinate.ReadXml: Error parsing coordinate");
            }
        }
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteString(this.ToString("ISO"));
        }
        #endregion
    }

    [Serializable()]
    public class CoordinateList : List<Cordinate>, IXmlSerializable
    {
        #region Constructors
        public CoordinateList()
        {
        }
        #endregion

        #region Overrides
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (Cordinate coord in this)
            {
                sb.Append(coord.ToString("ISO"));
            }
            return sb.ToString();
        }
        #endregion

        #region Public methods
        // Parsing method
        public void ParseIsoString(string isoStr)
        {
            string[] coords = isoStr.Split('/');

            this.Clear();   // Clear previous list
            for (int i = 1; i < coords.Length; i++)
            {
                Cordinate coord = new Cordinate();
                coord.ParseIsoString(coords[i - 1] + "/");
                this.Add(coord);
            }
        }
        #endregion

        #region IXmlSerializable Members
        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            try
            {
                string str = reader.ReadElementContentAsString().Trim();
                this.ParseIsoString(str);
            }
            catch
            {
                throw new Exception("CoordinateList.ReadXml: Error parsing coordinate list");
            }
        }
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteString(this.ToString());
        }
        #endregion
    }
}
