using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private readonly Robot robot;
        private readonly DispatcherTimer timer;

    private Vector2D waypoint;
        private Vector2D effectiveWaypoint;

        private bool hasWaypoint = false;

        private enum RobotState
        {
            Idle,
            Rotating,
            Translating
        }

        private RobotState state = RobotState.Idle;

        // -------------------------------------------------------
        // PARAMETRES DE SIMULATION
        // -------------------------------------------------------

        private const double TranslationSpeed = 3.0;
        private const double RotationSpeed = 2.0;

        private const double PositionTolerance = 0.05;
        private const double AngleTolerance = 0.01;

        // -------------------------------------------------------
        // PARAMETRES DU TERRAIN
        // -------------------------------------------------------

        private const double Scale = 30.0;

        private const double LeftMargin = 35.0;
        private const double TopMargin = 30.0;
        private const double RightMargin = 40.0;
        private const double BottomMargin = 30.0;

        // -------------------------------------------------------
        // CONSTRUCTEUR
        // -------------------------------------------------------

        public MainWindow()
        {
            InitializeComponent();

            robot = new Robot(2, 2, 0);

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(20)
            };

            timer.Tick += Timer_Tick;
            timer.Start();

            Loaded += MainWindow_Loaded;

            SimulationCanvas.SizeChanged +=
                SimulationCanvas_SizeChanged;
        }

        // -------------------------------------------------------
        // INITIALISATION
        // -------------------------------------------------------

        private void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            DrawGrid();
            UpdateDisplay();
            DrawRobot();
        }

        // -------------------------------------------------------
        // REDIMENSIONNEMENT
        // -------------------------------------------------------

        private void SimulationCanvas_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            if (!IsInitialized)
                return;

            DrawGrid();
            DrawRobot();
        }

        // -------------------------------------------------------
        // BOUCLE DE SIMULATION
        // -------------------------------------------------------

        private void Timer_Tick(
            object? sender,
            EventArgs e)
        {
            const double dt = 0.020;

            switch (state)
            {
                case RobotState.Rotating:
                    UpdateRotation(dt);
                    break;

                case RobotState.Translating:
                    UpdateTranslation(dt);
                    break;
            }

            UpdateDisplay();
            DrawRobot();
        }

        // -------------------------------------------------------
        // ROTATION
        // -------------------------------------------------------

        private void UpdateRotation(double dt)
        {
            Vector2D direction =
                effectiveWaypoint - robot.Position;

            if (direction.Length < PositionTolerance)
            {
                state = RobotState.Idle;
                return;
            }

            double targetAngle =
                Math.Atan2(
                    direction.Y,
                    direction.X);

            double angleDifference =
                Robot.AngleDifference(
                    targetAngle,
                    robot.Angle);

            if (Math.Abs(angleDifference) < AngleTolerance)
            {
                robot.SetAngle(targetAngle);
                state = RobotState.Translating;
                return;
            }

            double maxRotation =
                RotationSpeed * dt;

            double rotation =
                Math.Clamp(
                    angleDifference,
                    -maxRotation,
                    maxRotation);

            robot.SetAngle(
                robot.Angle + rotation);
        }

        // -------------------------------------------------------
        // TRANSLATION
        // -------------------------------------------------------

        private void UpdateTranslation(double dt)
        {
            Vector2D axis = robot.ForwardVector;

            // Projection du TARGET ORIGINAL sur l'axe de déplacement
            Vector2D targetProjection =
                ProjectPointOnRobotAxis(waypoint);

            double distance =
                Vector2D.Distance(
                    robot.Position,
                    targetProjection
                );

            // Le robot est arrivé sur la projection du Target
            if (distance < PositionTolerance)
            {
                robot.SetPosition(targetProjection);
                state = RobotState.Idle;
                return;
            }

            double forwardDistance =
                Vector2D.Dot(
                    targetProjection - robot.Position,
                    axis
                );

            // Sécurité : la projection est derrière le robot
            if (forwardDistance < 0)
            {
                state = RobotState.Idle;
                return;
            }

            double movement =
                TranslationSpeed * dt;

            movement =
                Math.Min(
                    movement,
                    distance
                );

            robot.MoveForward(movement);
        }

        // -------------------------------------------------------
        // PROJECTION SUR L'AXE DU ROBOT
        // -------------------------------------------------------

        private Vector2D ProjectPointOnRobotAxis(
            Vector2D point)
        {
            Vector2D axis =
                robot.ForwardVector;

            Vector2D robotToPoint =
                point - robot.Position;

            double projectionLength =
                Vector2D.Dot(
                    robotToPoint,
                    axis);

            return robot.Position +
                   axis * projectionLength;
        }

        // -------------------------------------------------------
        // LANCEMENT VERS UN WAYPOINT
        // -------------------------------------------------------

        private void GoToWaypoint(
            Vector2D target)
        {
            waypoint = target;

            effectiveWaypoint =
                CalculateEffectiveWaypoint(
                    waypoint);

            hasWaypoint = true;

            Vector2D direction =
                effectiveWaypoint -
                robot.Position;

            if (direction.Length < PositionTolerance)
            {
                state = RobotState.Idle;
                return;
            }

            state = RobotState.Rotating;
        }

        // -------------------------------------------------------
        // CALCUL WAYPOINT EFFECTIF
        // -------------------------------------------------------

        private Vector2D CalculateEffectiveWaypoint(
            Vector2D target)
        {
            if (AngleOffsetCheckBox.IsChecked != true)
                return target;

            if (!double.TryParse(
                    AngleOffsetTextBox.Text,
                    out double offset))
            {
                offset = 0;
            }

            Vector2D direction =
                target - robot.Position;

            if (direction.Length < 0.000001)
                return target;

            double angle =
                Math.Atan2(
                    direction.Y,
                    direction.X);

            double modifiedAngle =
                angle + offset;

            double distance =
                direction.Length;

            return robot.Position +
                   new Vector2D(
                       Math.Cos(modifiedAngle),
                       Math.Sin(modifiedAngle)) *
                   distance;
        }

        // -------------------------------------------------------
        // CHANGEMENT DU MODE ECART ANGULAIRE
        // -------------------------------------------------------

        private void AngleOffsetCheckBox_Changed(
            object sender,
            RoutedEventArgs e)
        {
            if (!hasWaypoint)
            {
                DrawRobot();
                return;
            }

            effectiveWaypoint =
                CalculateEffectiveWaypoint(
                    waypoint);

            Vector2D direction =
                effectiveWaypoint -
                robot.Position;

            if (direction.Length < PositionTolerance)
            {
                state = RobotState.Idle;
            }
            else
            {
                state = RobotState.Rotating;
            }

            UpdateDisplay();
            DrawRobot();
        }

        // -------------------------------------------------------
        // AFFICHAGE DES INFORMATIONS
        // -------------------------------------------------------

        private void UpdateDisplay()
        {
            RobotPositionText.Text =
                $"X = {robot.Position.X:F2}\n" +
                $"Y = {robot.Position.Y:F2}";

            RobotAngleText.Text =
                $"Angle = {robot.Angle:F3} rad\n" +
                $"Angle = " +
                $"{robot.Angle * 180.0 / Math.PI:F1}°";

            if (!hasWaypoint)
            {
                WaypointText.Text =
                    "Aucun waypoint";

                ProjectionText.Text =
                    "Aucune projection";

                DistanceText.Text =
                    "Distance = --";

                return;
            }

            WaypointText.Text =
                $"Original : " +
                $"({waypoint.X:F2} ; {waypoint.Y:F2})\n" +
                $"Effectif : " +
                $"({effectiveWaypoint.X:F2} ; " +
                $"{effectiveWaypoint.Y:F2})";

            Vector2D projection =
                ProjectPointOnRobotAxis(
                    effectiveWaypoint);

            ProjectionText.Text =
                $"X = {projection.X:F2}\n" +
                $"Y = {projection.Y:F2}";

            double distance =
                Vector2D.Distance(
                    robot.Position,
                    projection);

            DistanceText.Text =
                $"Distance projection = " +
                $"{distance:F2}\n" +
                $"État = {state}";
        }

        // -------------------------------------------------------
        // DESSIN DU ROBOT
        // -------------------------------------------------------

        private void DrawRobot()
        {
            if (SimulationCanvas.ActualWidth <= 0 ||
                SimulationCanvas.ActualHeight <= 0)
            {
                return;
            }

            Point center =
                WorldToScreen(robot.Position);

            Vector2D forward =
                robot.ForwardVector;

            Vector2D right =
                new Vector2D(
                    -forward.Y,
                    forward.X);

            Point nose =
                WorldToScreen(
                    robot.Position +
                    forward * 0.7);

            Point left =
                WorldToScreen(
                    robot.Position -
                    forward * 0.45 +
                    right * 0.4);

            Point rightPoint =
                WorldToScreen(
                    robot.Position -
                    forward * 0.45 -
                    right * 0.4);

            RobotTriangle.Points.Clear();

            RobotTriangle.Points.Add(nose);
            RobotTriangle.Points.Add(left);
            RobotTriangle.Points.Add(rightPoint);

            // Axe du robot

            Point axisEnd =
                WorldToScreen(
                    robot.Position +
                    forward * 2);

            RobotAxis.X1 = center.X;
            RobotAxis.Y1 = center.Y;
            RobotAxis.X2 = axisEnd.X;
            RobotAxis.Y2 = axisEnd.Y;

            // Waypoint effectif

            if (hasWaypoint)
            {
                Point wp =
                    WorldToScreen(
                        effectiveWaypoint);

                Canvas.SetLeft(
                    WaypointMarker,
                    wp.X - 7);

                Canvas.SetTop(
                    WaypointMarker,
                    wp.Y - 7);

                WaypointMarker.Visibility =
                    Visibility.Visible;

                Vector2D projection =
                    ProjectPointOnRobotAxis(
                        effectiveWaypoint);

                Point proj =
                    WorldToScreen(projection);

                Canvas.SetLeft(
                    ProjectionMarker,
                    proj.X - 5);

                Canvas.SetTop(
                    ProjectionMarker,
                    proj.Y - 5);

                ProjectionMarker.Visibility =
                    Visibility.Visible;

                ProjectionLine.X1 =
                    center.X;

                ProjectionLine.Y1 =
                    center.Y;

                ProjectionLine.X2 =
                    proj.X;

                ProjectionLine.Y2 =
                    proj.Y;

                ProjectionLine.Visibility =
                    Visibility.Visible;
            }
            else
            {
                WaypointMarker.Visibility =
                    Visibility.Collapsed;

                ProjectionMarker.Visibility =
                    Visibility.Collapsed;

                ProjectionLine.Visibility =
                    Visibility.Collapsed;
            }

            DrawTarget();
        }

        // -------------------------------------------------------
        // TARGET ET PROJECTION ORTHOGONALE
        // -------------------------------------------------------

        private void DrawTarget()
        {
            if (!hasWaypoint ||
                AngleOffsetCheckBox.IsChecked != true)
            {
                TargetMarker.Visibility =
                    Visibility.Collapsed;

                TargetLabel.Visibility =
                    Visibility.Collapsed;

                TargetProjectionMarker.Visibility =
                    Visibility.Collapsed;

                TargetProjectionLine.Visibility =
                    Visibility.Collapsed;

                return;
            }

            // Target = waypoint original

            Point targetPoint =
                WorldToScreen(
                    waypoint);

            Canvas.SetLeft(
                TargetMarker,
                targetPoint.X - 6);

            Canvas.SetTop(
                TargetMarker,
                targetPoint.Y - 6);

            TargetMarker.Visibility =
                Visibility.Visible;

            // Texte Target

            Canvas.SetLeft(
                TargetLabel,
                targetPoint.X + 8);

            Canvas.SetTop(
                TargetLabel,
                targetPoint.Y - 10);

            TargetLabel.Visibility =
                Visibility.Visible;

            // Projection orthogonale

            Vector2D targetProjection =
                ProjectPointOnRobotAxis(
                    waypoint);

            Point projectionPoint =
                WorldToScreen(
                    targetProjection);

            Canvas.SetLeft(
                TargetProjectionMarker,
                projectionPoint.X - 4);

            Canvas.SetTop(
                TargetProjectionMarker,
                projectionPoint.Y - 4);

            TargetProjectionMarker.Visibility =
                Visibility.Visible;

            // Ligne pointillée

            TargetProjectionLine.X1 =
                targetPoint.X;

            TargetProjectionLine.Y1 =
                targetPoint.Y;

            TargetProjectionLine.X2 =
                projectionPoint.X;

            TargetProjectionLine.Y2 =
                projectionPoint.Y;

            TargetProjectionLine.Visibility =
                Visibility.Visible;
        }

        // -------------------------------------------------------
        // COORDONNEES MONDE -> ECRAN
        // -------------------------------------------------------

        private Point WorldToScreen(
            Vector2D position)
        {
            /*
             * Origine :
             * (0,0) = bas gauche
             *
             * X positif = droite
             * Y positif = haut
             */

            double screenX =
                LeftMargin +
                position.X * Scale;

            double screenY =
                SimulationCanvas.ActualHeight -
                BottomMargin -
                position.Y * Scale;

            return new Point(
                screenX,
                screenY);
        }

        // -------------------------------------------------------
        // GRILLE ET GRADUATIONS
        // -------------------------------------------------------

        private void DrawGrid()
        {
            SimulationCanvas.Children.Clear();

            double width =
                SimulationCanvas.ActualWidth;

            double height =
                SimulationCanvas.ActualHeight;

            if (width <= 0 ||
                height <= 0)
            {
                return;
            }

            double terrainWidth =
                width -
                LeftMargin -
                RightMargin;

            double terrainHeight =
                height -
                TopMargin -
                BottomMargin;

            if (terrainWidth <= 0 ||
                terrainHeight <= 0)
            {
                return;
            }

            double maxX =
                terrainWidth / Scale;

            double maxY =
                terrainHeight / Scale;

            int maxXInt =
                (int)Math.Floor(maxX);

            int maxYInt =
                (int)Math.Floor(maxY);

            // ---------------------------------------------------
            // GRILLE VERTICALE
            // ---------------------------------------------------

            for (int i = 0;
                 i <= maxXInt;
                 i++)
            {
                double x =
                    LeftMargin +
                    i * Scale;

                Line line =
                    new Line
                    {
                        X1 = x,
                        Y1 = TopMargin,
                        X2 = x,
                        Y2 = height - BottomMargin,
                        Stroke =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    65,
                                    65,
                                    65)),
                        StrokeThickness = 1
                    };

                SimulationCanvas.Children.Add(line);
            }

            // ---------------------------------------------------
            // GRILLE HORIZONTALE
            // ---------------------------------------------------

            for (int i = 0;
                 i <= maxYInt;
                 i++)
            {
                double y =
                    height -
                    BottomMargin -
                    i * Scale;

                Line line =
                    new Line
                    {
                        X1 = LeftMargin,
                        Y1 = y,
                        X2 = width - RightMargin,
                        Y2 = y,
                        Stroke =
                            new SolidColorBrush(
                                Color.FromRgb(
                                    65,
                                    65,
                                    65)),
                        StrokeThickness = 1
                    };

                SimulationCanvas.Children.Add(line);
            }

            // ---------------------------------------------------
            // AXE X
            // ---------------------------------------------------

            double xAxisY =
                height - BottomMargin;

            Line xAxis =
                new Line
                {
                    X1 = LeftMargin,
                    Y1 = xAxisY,
                    X2 = width - RightMargin,
                    Y2 = xAxisY,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                };

            SimulationCanvas.Children.Add(xAxis);

            // ---------------------------------------------------
            // AXE Y
            // ---------------------------------------------------

            double yAxisX =
                width - RightMargin;

            Line yAxis =
                new Line
                {
                    X1 = yAxisX,
                    Y1 = TopMargin,
                    X2 = yAxisX,
                    Y2 = height - BottomMargin,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                };

            SimulationCanvas.Children.Add(yAxis);

            // ---------------------------------------------------
            // GRADUATIONS X
            // ---------------------------------------------------

            for (int i = 0;
                 i <= maxXInt;
                 i++)
            {
                double x =
                    LeftMargin +
                    i * Scale;

                Line tick =
                    new Line
                    {
                        X1 = x,
                        Y1 = TopMargin,
                        X2 = x,
                        Y2 = TopMargin + 7,
                        Stroke = Brushes.White,
                        StrokeThickness = 1
                    };

                SimulationCanvas.Children.Add(tick);

                TextBlock label =
                    new TextBlock
                    {
                        Text = i.ToString(),
                        Foreground = Brushes.White,
                        FontSize = 11
                    };

                Canvas.SetLeft(
                    label,
                    x - 5);

                Canvas.SetTop(
                    label,
                    5);

                SimulationCanvas.Children.Add(label);
            }

            // ---------------------------------------------------
            // TITRE X
            // ---------------------------------------------------

            TextBlock xTitle =
                new TextBlock
                {
                    Text = "X",
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold
                };

            Canvas.SetLeft(
                xTitle,
                width - RightMargin - 15);

            Canvas.SetTop(
                xTitle,
                5);

            SimulationCanvas.Children.Add(xTitle);

            // ---------------------------------------------------
            // GRADUATIONS Y
            // ---------------------------------------------------

            for (int i = 0;
                 i <= maxYInt;
                 i++)
            {
                double y =
                    height -
                    BottomMargin -
                    i * Scale;

                Line tick =
                    new Line
                    {
                        X1 = yAxisX - 7,
                        Y1 = y,
                        X2 = yAxisX,
                        Y2 = y,
                        Stroke = Brushes.White,
                        StrokeThickness = 1
                    };

                SimulationCanvas.Children.Add(tick);

                TextBlock label =
                    new TextBlock
                    {
                        Text = i.ToString(),
                        Foreground = Brushes.White,
                        FontSize = 11
                    };

                Canvas.SetLeft(
                    label,
                    yAxisX + 5);

                Canvas.SetTop(
                    label,
                    y - 8);

                SimulationCanvas.Children.Add(label);
            }

            // ---------------------------------------------------
            // TITRE Y
            // ---------------------------------------------------

            TextBlock yTitle =
                new TextBlock
                {
                    Text = "Y",
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold
                };

            Canvas.SetLeft(
                yTitle,
                yAxisX + 5);

            Canvas.SetTop(
                yTitle,
                TopMargin - 25);

            SimulationCanvas.Children.Add(yTitle);

            // ---------------------------------------------------
            // REMETTRE LES OBJETS WPF
            // ---------------------------------------------------

            SimulationCanvas.Children.Add(
                TargetProjectionLine);

            SimulationCanvas.Children.Add(
                TargetProjectionMarker);

            SimulationCanvas.Children.Add(
                ProjectionLine);

            SimulationCanvas.Children.Add(
                WaypointMarker);

            SimulationCanvas.Children.Add(
                ProjectionMarker);

            SimulationCanvas.Children.Add(
                TargetMarker);

            SimulationCanvas.Children.Add(
                TargetLabel);

            SimulationCanvas.Children.Add(
                RobotAxis);

            SimulationCanvas.Children.Add(
                RobotTriangle);
        }

        // -------------------------------------------------------
        // WAYPOINT 1
        // -------------------------------------------------------

        private void Waypoint1_Click(
            object sender,
            RoutedEventArgs e)
        {
            GoToWaypoint(
                new Vector2D(4, 4));
        }

        // -------------------------------------------------------
        // WAYPOINT 2
        // -------------------------------------------------------

        private void Waypoint2_Click(
            object sender,
            RoutedEventArgs e)
        {
            GoToWaypoint(
                new Vector2D(10, 15));
        }

        // -------------------------------------------------------
        // WAYPOINT 3
        // -------------------------------------------------------

        private void Waypoint3_Click(
            object sender,
            RoutedEventArgs e)
        {
            GoToWaypoint(
                new Vector2D(15, 5));
        }

        // -------------------------------------------------------
        // WAYPOINT 4
        // -------------------------------------------------------

        private void Waypoint4_Click(
            object sender,
            RoutedEventArgs e)
        {
            GoToWaypoint(
                new Vector2D(3, 17));
        }

        // -------------------------------------------------------
        // STOP
        // -------------------------------------------------------

        private void Stop_Click(
            object sender,
            RoutedEventArgs e)
        {
            state =
                RobotState.Idle;
        }
    }


}
