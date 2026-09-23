namespace WpfApp1
{
    public class Robot
    {
        public Vector2D Position { get; private set; }

        public double Angle { get; private set; }

        public Robot(double x, double y, double angle = 0)
        {
            Position = new Vector2D(x, y);
            Angle = angle;
        }

        public void SetPosition(Vector2D position)
        {
            Position = position;
        }

        public void SetAngle(double angle)
        {
            Angle = NormalizeAngle(angle);
        }

        public Vector2D ForwardVector
        {
            get
            {
                return new Vector2D(
                    System.Math.Cos(Angle),
                    System.Math.Sin(Angle)
                );
            }
        }

        public void MoveForward(double distance)
        {
            Position += ForwardVector * distance;
        }

        public static double NormalizeAngle(double angle)
        {
            while (angle > System.Math.PI)
                angle -= 2 * System.Math.PI;

            while (angle < -System.Math.PI)
                angle += 2 * System.Math.PI;

            return angle;
        }

        public static double AngleDifference(
            double targetAngle,
            double currentAngle)
        {
            return NormalizeAngle(
                targetAngle - currentAngle
            );
        }
    }
}