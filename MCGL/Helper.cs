using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCGL
{
    public class Coordinates
    {
        protected double x, y, z;

        public Coordinates(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return x + " " + y + " " + z;
        }

        public string To2DString()
        {
            return x + " " + z;
        }

        public Coordinates Add(double x, double y, double z)
        {
            return new Coordinates(this.x + x, this.y + y, this.z + z);
        }

        public Coordinates Sub(double x, double y, double z)
        {
            return Add(-x, -y, -z);
        }
    }

    public class RelativeCoordinates : Coordinates
    {
        public static readonly RelativeCoordinates Zero = new RelativeCoordinates(0, 0, 0);
        public static readonly RelativeCoordinates Above = new RelativeCoordinates(0, 1, 0);
        public static readonly RelativeCoordinates Below = new RelativeCoordinates(0, -1, 0);
        public static readonly RelativeCoordinates North = new RelativeCoordinates(0, 0, -1);
        public static readonly RelativeCoordinates East = new RelativeCoordinates(1, 0, 0);
        public static readonly RelativeCoordinates South = new RelativeCoordinates(0, 0, 1);
        public static readonly RelativeCoordinates West = new RelativeCoordinates(-1, 0, 0);

        public RelativeCoordinates(double x, double y, double z) : base(x, y, z)
        {
        }

        public override  string ToString()
        {
            return "~" + x + " ~" + y + " ~" + z;
        }

        new public string To2DString()
        {
            return "~" + x + " ~" + z;
        }

        new public RelativeCoordinates Add(double x, double y, double z)
        {
            return new RelativeCoordinates(this.x + x, this.y + y, this.z + z);
        }

        new public RelativeCoordinates Sub(double x, double y, double z)
        {
            return Add(-x, -y, -z);
        }
    }

    public class MixedCoordinates : Coordinates
    {
        protected bool xRelative, yRelative, zRelative;

        public MixedCoordinates(double x, bool xRelative, double y, bool yRelative, double z, bool zRelative) : base(x, y, z)
        {
            this.xRelative = xRelative;
            this.yRelative = yRelative;
            this.zRelative = zRelative;
        }

        public override string ToString()
        {
            return
                (xRelative ? "~" + x : "" + x) + " "
                + (yRelative ? "~" + y : "" + y) + " "
                + (zRelative ? "~" + z : "" + z);
        }

        new public string To2DString()
        {
            return
                (xRelative ? "~" + x : "" + x) + " "
                + (zRelative ? "~" + z : "" + z);
        }

        new public MixedCoordinates Add(double x, double y, double z)
        {
            return new MixedCoordinates(this.x + x, xRelative, this.y + y, yRelative, this.z + z, zRelative);
        }

        new public MixedCoordinates Sub(double x, double y, double z)
        {
            return Add(-x, -y, -z);
        }
    }

    public class Rotation : Coordinates
    {
        public Rotation(double x, double y) : base(x, y, 0)
        {
        }

        new public string ToString()
        {
            return x + " " + y;
        }

        new public Rotation Add(double x, double y, double z)
        {
            return Add(x, y);
        }

        new public Rotation Sub(double x, double y, double z)
        {
            return Sub(x, y);
        }

        public Rotation Add(double x, double y)
        {
            return new Rotation(this.x + x, this.y + y);
        }

        public Rotation Sub(double x, double y)
        {
            return Add(-x, -y);
        }
    }

    public class RelativeRotation : Rotation
    {
        public RelativeRotation(double x, double y) : base(x, y)
        {
        }

        new public string ToString()
        {
            return "~" + x + " ~" + y;
        }

        new public RelativeRotation Add(double x, double y, double z)
        {
            return Add(x, y);
        }

        new public RelativeRotation Sub(double x, double y, double z)
        {
            return Sub(x, y);
        }

        new public RelativeRotation Add(double x, double y)
        {
            return new RelativeRotation(this.x + x, this.y + y);
        }

        new public RelativeRotation Sub(double x, double y)
        {
            return Add(-x, -y);
        }
    }

    public class MixedRotation : Rotation
    {
        bool xRelative;
        bool yRelative;

        public MixedRotation(double x, bool xRelative, double y, bool yRelative) : base(x, y)
        {
            this.xRelative = xRelative;
            this.yRelative = yRelative;
        }

        new public string ToString()
        {
            return
                (xRelative ? "~" + x : "" + x) + " "
                + (yRelative ? "~" + y : "" + y);
        }

        new public MixedRotation Add(double x, double y, double z)
        {
            return Add(x, y);
        }

        new public MixedRotation Sub(double x, double y, double z)
        {
            return Sub(x, y);
        }

        new public MixedRotation Add(double x, double y)
        {
            return new MixedRotation(this.x + x, xRelative, this.y + y, yRelative);
        }

        new public MixedRotation Sub(double x, double y)
        {
            return Add(-x, -y);
        }
    }

    public class Area
    {
        public Coordinates min;
        public Coordinates max;

        public Area(Coordinates min, Coordinates max)
        {
            this.min = min;
            this.max = max;
        }

        new public string ToString()
        {
            return min.ToString() + " " + max.ToString();
        }
    }
}
