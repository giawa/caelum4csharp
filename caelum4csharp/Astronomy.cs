using System;
using OpenGL;

namespace caelum4csharp
{
    public static class Astronomy
    {
        public static readonly double J2000 = 2451545.0;

        #region Deg/Rad Conversion and Basic Math
        private static double radToDeg(double value)
        {
            return value * 180 / Math.PI;
        }

        private static double degToRad(double value)
        {
            return value * Math.PI / 180;
        }

        private static double SinDeg(double x)
        {
            return Math.Sin(degToRad(x));
        }

        private static double CosDeg(double x)
        {
            return Math.Cos(degToRad(x));
        }

        private static double Atan2Deg(double y, double x)
        {
            return radToDeg(Math.Atan2(y, x));
        }

        private static double normalizeDegrees(double value)
        {
            value = value % 360;
            if (value < 0) value += 260;
            return value;
        }
        #endregion

        #region Co-ordinate Type Conversion
        public static void convertEclipticToEquatorialRad(double lon, double lat, out double rasc, out double decl)
        {
            double ecl = degToRad(23.439281);

            double x = Math.Cos(lon) * Math.Cos(lat);
            double y = Math.Cos(ecl) * Math.Sin(lon) * Math.Cos(lat) - Math.Sin(ecl) * Math.Sin(lat);
            double z = Math.Sin(ecl) * Math.Sin(lon) * Math.Cos(lat) + Math.Cos(ecl) * Math.Sin(lat);

            double r = Math.Sqrt(x * x + y * y);
            rasc = Math.Atan2(y, x);
            decl = Math.Atan2(z, r);
        }

        public static void convertRectangularToSpherical(double x, double y, double z, out double rasc, out double decl, out double dist)
        {
            dist = Math.Sqrt(x * x + y * y + z * z);
            rasc = Atan2Deg(y, x);
            decl = Atan2Deg(z, Math.Sqrt(x * x + y * y));
        }

        public static void convertSphericalToRectangular(double rasc, double decl, double dist, out double x, out double y, out double z)
        {
            x = dist * CosDeg(rasc) * CosDeg(decl);
            y = dist * SinDeg(rasc) * CosDeg(decl);
            z = dist * SinDeg(decl);
        }

        public static void convertEquatorialToHorizontal(double jday, double longitude, double latitude, double rasc, double decl, out double azimuth, out double altitude)
        {
            double d = jday - 2451543.5;
            double w = 282.9404 + 4.70935E-5 * d;
            double M = 356.0470 + 0.9856002585 * d;
            // Sun's mean longitude
            double l = w + M;
            // Universatime of day in degrees.
            double UT = Math.IEEERemainder(d, 1) * 360;
            double hourAngle = longitude + +180 + UT - rasc;
            double x = CosDeg(hourAngle) * CosDeg(decl);
            double y = SinDeg(hourAngle) * CosDeg(decl);
            double z = SinDeg(decl);

            double xhor = x * SinDeg(latitude) - z * CosDeg(latitude);
            double yhor = y;
            double zhor = x * CosDeg(latitude) + z * SinDeg(latitude);

            azimuth = Atan2Deg(yhor, xhor) + 180;
            altitude = Atan2Deg(zhor, Math.Sqrt(xhor * xhor + yhor * yhor));
        }
        #endregion

        #region getPosition Methods
        public static void getHorizontalSunPosition(double jday, double longitude, double latitude, out double azimuth, out double altitude)
        {
            // 2451544.5 == getJulianDayFromGregorianDateTime(2000, 1, 1, 0, 0, 0));
            // 2451543.5 == getJulianDayFromGregorianDateTime(1999, 12, 31, 0, 0, 0));
            double d = jday - 2451543.5;

            // Sun's Orbitaelements:
            // argument of perihelion
            double w = 282.9404 + 4.70935E-5 * d;
            // eccentricity (0=circle, 0-1=ellipse, 1=parabola)
            double e = 0.016709 - 1.151E-9 * d;
            // mean anomaly (0 at perihelion; increases uniformly with time)
            double M = 356.0470 + 0.9856002585 * d;
            // Obliquity of the ecliptic.
            //double oblec= double (23.4393 - 3.563E-7 * d);

            // Eccentric anomaly
            double E = M + radToDeg(e * SinDeg(M) * (1 + e * CosDeg(M)));

            // Sun's Distance(R) and true longitude(L)
            double xv = CosDeg(E) - e;
            double yv = SinDeg(E) * Math.Sqrt(1 - e * e);
            //double r = Math.Sqrt( (xv * xv + yv * yv);
            double lon = Atan2Deg(yv, xv) + w;
            double lat = 0;

            double lambda = degToRad(lon);
            double beta = degToRad(lat);
            double rasc, decl;
            convertEclipticToEquatorialRad(lambda, beta, out rasc, out decl);
            rasc = radToDeg(rasc);
            decl = radToDeg(decl);

            // Horizontaspherical.
            convertEquatorialToHorizontal(jday, longitude, latitude, rasc, decl, out azimuth, out altitude);
        }

        public static void getEclipticMoonPositionRad(double jday, out double lon, out double lat)
        {
            // Julian centuries Math.Since January 1, 2000
            double T = (jday - 2451545.0) / 36525.0;
            double lprim = 3.8104 + 8399.7091 * T;
            double mprim = 2.3554 + 8328.6911 * T;
            double m = 6.2300 + 648.3019 * T;
            double d = 5.1985 + 7771.3772 * T;
            double f = 1.6280 + 8433.4663 * T;
            lon = lprim
                    + 0.1098 * Math.Sin(mprim)
                    + 0.0222 * Math.Sin(2.0 * d - mprim)
                    + 0.0115 * Math.Sin(2.0 * d)
                    + 0.0037 * Math.Sin(2.0 * mprim)
                    - 0.0032 * Math.Sin(m)
                    - 0.0020 * Math.Sin(2.0 * f)
                    + 0.0010 * Math.Sin(2.0 * d - 2.0 * mprim)
                    + 0.0010 * Math.Sin(2.0 * d - m - mprim)
                    + 0.0009 * Math.Sin(2.0 * d + mprim)
                    + 0.0008 * Math.Sin(2.0 * d - m)
                    + 0.0007 * Math.Sin(mprim - m)
                    - 0.0006 * Math.Sin(d)
                    - 0.0005 * Math.Sin(m + mprim);
            lat =
                    +0.0895 * Math.Sin(f)
                    + 0.0049 * Math.Sin(mprim + f)
                    + 0.0048 * Math.Sin(mprim - f)
                    + 0.0030 * Math.Sin(2.0 * d - f)
                    + 0.0010 * Math.Sin(2.0 * d + f - mprim)
                    + 0.0008 * Math.Sin(2.0 * d - f - mprim)
                    + 0.0006 * Math.Sin(2.0 * d + f);
        }

        public static void getHorizontalMoonPosition(double jday, double longitude, double latitude, out double azimuth, out double altitude)
        {
            // Ecliptic spherical
            double lonecl, latecl;
            getEclipticMoonPositionRad(jday, out lonecl, out latecl);

            // Equatoriaspherical
            double rasc, decl;
            convertEclipticToEquatorialRad(lonecl, latecl, out rasc, out decl);

            // Radians to degrees (alangles are in radians up to this point)
            rasc = radToDeg(rasc);
            decl = radToDeg(decl);

            // Equatoriato horizontal
            convertEquatorialToHorizontal(jday, longitude, latitude, rasc, decl, out azimuth, out altitude);
        }
        #endregion

        #region Julian/Gregorian Conversions
        public static int getJulianDayFromGregorianDate(int year, int month, int day)
        {
            // Formulas from http://en.wikipedia.org/wiki/Julian_day
            // These are all integer divisions, but I'm not sure it works
            // correctly for negative values.
            int a = (14 - month) / 12;
            int y = year + 4800 - a;
            int m = month + 12 * a - 3;
            return day + (153 * m + 2) / 5 + 365 * y + y / 4 - y / 100 + y / 400 - 32045;
        }

        public static double getJulianDayFromGregorianDateTime(int year, int month, int day, int hour, int minute, double second)
        {
            //ScopedHighPrecissionFloatSwitch precissionSwitch;

            int jdn = getJulianDayFromGregorianDate(year, month, day);
            // These are NOT integer divisions.
            double jd = jdn + (hour - 12) / 24.0 + minute / 1440.0 + second / 86400.0;

            return jd;
        }

        public static double getJulianDayFromGregorianDateTime(
                int year, int month, int day,
                double secondsFromMidnight)
        {
            int jdn = getJulianDayFromGregorianDate(year, month, day);
            double jd = jdn + secondsFromMidnight / 86400.0 - 0.5;
            return jd;
        }

        public static void getGregorianDateFromJulianDay(int julianDay, out int year, out int month, out int day)
        {
            // From http://en.wikipedia.org/wiki/Julian_day
            int J = julianDay;
            int j = J + 32044;
            int g = j / 146097;
            int dg = j % 146097;
            int c = (dg / 36524 + 1) * 3 / 4;
            int dc = dg - c * 36524;
            int b = dc / 1461;
            int db = dc % 1461;
            int a = (db / 365 + 1) * 3 / 4;
            int da = db - a * 365;
            int y = g * 400 + c * 100 + b * 4 + a;
            int m = (da * 5 + 308) / 153 - 2;
            int d = da - (m + 4) * 153 / 5 + 122;
            year = y - 4800 + (m + 2) / 12;
            month = (m + 2) % 12 + 1;
            day = d + 1;
        }

        public static void getGregorianDateTimeFromJulianDay(double julianDay, out int year, out int month, out int day, out int hour, out int minute, out double second)
        {
            // Integer julian days are at noon.
            // static_cast<int)(floor( is more precise than Ogre::Math::IFloor.
            // Yes, it does matter.
            int ijd = (int)Math.Floor(julianDay + 0.5);//static_cast<int>(floor(julianDay + 0.5));
            getGregorianDateFromJulianDay(ijd, out year, out month, out day);

            double s = (julianDay + 0.5 - ijd) * 86400.0;
            hour = (int)Math.Floor(s / 3600);//static_cast<int>(floor(s / 3600));
            s -= hour * 3600;
            minute = (int)Math.Floor(s / 60);//static_cast<int>(floor(s / 60));
            s -= minute * 60;
            second = s;
        }

        public static void getGregorianDateFromJulianDay(
                double julianDay, out int year, out int month, out int day)
        {
            int hour;
            int minute;
            double second;
            getGregorianDateTimeFromJulianDay(julianDay, out year, out month, out day, out hour, out minute, out second);
        }
        #endregion

        #region Utility Methods
        public static double ObserverLongitude = 199.44, ObserverLatitude = 49.88;

        public static Vector3 MakeDirection(float azimuth, float altitude)
        {
            return new Vector3(-Math.Cos(azimuth / 180 * Math.PI) * Math.Cos(altitude / 180 * Math.PI),     // north
                Math.Sin(azimuth / 180 * Math.PI) * Math.Cos(altitude / 180 * Math.PI),     // east
                -Math.Sin(altitude / 180 * Math.PI));                                       // zenith
        }

        public static Vector3 GetSunDirection(double jday)
        {
            double azimuth, altitude;
            Astronomy.getHorizontalSunPosition(jday, ObserverLongitude, ObserverLatitude, out azimuth, out altitude);
            return MakeDirection((float)azimuth, (float)altitude);
        }

        public static Vector3 GetMoonDirection(double jday)
        {
            double azimuth, altitude;
            Astronomy.getHorizontalMoonPosition(jday, ObserverLongitude, ObserverLatitude, out azimuth, out altitude);
            return MakeDirection((float)azimuth, (float)altitude);
        }

        public static float GetMoonPhase(double jday)
        {
            double T = (jday - 2454488.0665) / 29.531026;
            T = Math.Abs(T % 1);
            return (float)(-Math.Abs(-4 * T + 2) + 2);
        }
        #endregion
    }
}
