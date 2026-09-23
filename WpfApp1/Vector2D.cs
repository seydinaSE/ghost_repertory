namespace WpfApp1
{
    public struct Vector2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Vector2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double Length
        {
            get
            {
                return System.Math.Sqrt(X * X + Y * Y);
            }
        }

        public Vector2D Normalized()
        {
            double length = Length;

            if (length < 0.000001)
                return new Vector2D(0, 0);

            return new Vector2D(
                X / length,
                Y / length
            );
        }

        public static Vector2D operator +(Vector2D a, Vector2D b)
        {
            return new Vector2D(
                a.X + b.X,
                a.Y + b.Y
            );
        }

        public static Vector2D operator -(Vector2D a, Vector2D b)
        {
            return new Vector2D(
                a.X - b.X,
                a.Y - b.Y
            );
        }

        public static Vector2D operator *(Vector2D a, double scalar)
        {
            return new Vector2D(
                a.X * scalar,
                a.Y * scalar
            );
        }

        public static double Dot(Vector2D a, Vector2D b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static double Distance(Vector2D a, Vector2D b)
        {
            return (a - b).Length;
        }
    }
}