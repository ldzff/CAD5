using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
// using netDxf.Tables;
// using netDxf.Units;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Linq;
using RobTeach.Models; // Added for Trajectory type
// using System.Windows.Shapes;

namespace RobTeach.Services
{
    /// <summary>
    /// Provides services for loading CAD (DXF) files and converting DXF entities
    /// into WPF shapes and trajectory points using IxMilia.Dxf library.
    /// </summary>
    public class CadService
    {
        /// <summary>
        /// Loads a DXF document from the specified file path with enhanced error handling and version compatibility.
        /// </summary>
        public DxfFile LoadDxf(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty.");
            }
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("DXF file not found.", filePath);
            }
            
            try
            {
                // IxMilia.Dxf is more forgiving with DXF formats
                DxfFile dxf = DxfFile.Load(filePath);
                return dxf;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading or parsing DXF file: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Converts entities from a <see cref="DxfFile"/> into a list of WPF <see cref="System.Windows.Shapes.Shape"/> objects for display.
        /// Supports Lines, Arcs, and Circles.
        /// </summary>
        public List<System.Windows.Shapes.Shape> GetWpfShapesFromDxf(DxfFile dxfFile)
        {
            var wpfShapes = new List<System.Windows.Shapes.Shape>();
            if (dxfFile == null)
            {
                System.Diagnostics.Debug.WriteLine("[JULES_DEBUG] CadService.GetWpfShapesFromDxf: dxfFile is null. Returning empty list.");
                return wpfShapes;
            }
            System.Diagnostics.Debug.WriteLine($"[JULES_DEBUG] CadService.GetWpfShapesFromDxf: Processing {dxfFile.Entities.Count()} entities from DXF document.");
            int entityCounter = 0;

            // Process all entities
            foreach (var entity in dxfFile.Entities)
            {
                System.Windows.Shapes.Shape? wpfShape = null; // Use nullable Shape
                // string entityIdentifier = $"Handle: {entity.Handle.ToString("X")}, Type: {entity.GetType().Name}"; // Removed due to compile error
                System.Diagnostics.Debug.WriteLine($"[JULES_DEBUG] CadService.GetWpfShapesFromDxf: Processing entity at index {entityCounter} (C# Type: {entity.GetType().Name}).");

                switch (entity)
                {
                    case DxfLine dxfLine:
                        wpfShape = new System.Windows.Shapes.Line
                        {
                            X1 = dxfLine.P1.X, Y1 = dxfLine.P1.Y,
                            X2 = dxfLine.P2.X, Y2 = dxfLine.P2.Y,
                            IsHitTestVisible = true
                        };
                        System.Diagnostics.Debug.WriteLine($"[JULES_DEBUG] CadService.GetWpfShapesFromDxf:   Converted DxfLine to WPF Line.");
                        break;

                    case DxfArc dxfArc:
                        wpfShape = CreateArcPath(dxfArc);
                        if (wpfShape != null)
                            System.Diagnostics.Debug.WriteLine($"[JULES_DEBUG] CadService.GetWpfShapesFromDxf:   Converted DxfArc to WPF Path.");
                        else
                            System.Diagnostics.Debug.WriteLine($"[JULES_DEBUG] CadService.GetWpfShapesFromDxf:   FAILED to convert DxfArc to WPF Path.");
                        break;

                    case DxfCircle dxfCircle:
                        var ellipseGeometry = new EllipseGeometry(
                            new System.Windows.Point(dxfCircle.Center.X, dxfCircle.Center.Y),
                            dxfCircle.Radius,
                            dxfCircle.Radius
                        );
                        wpfShape = new System.Windows.Shapes.Path
                        {
                            Data = ellipseGeometry,
                            Fill = Brushes.Transparent,
                            IsHitTestVisible = true
                        };
                        System.Diagnostics.Debug.WriteLine($"[JULES_DEBUG] CadService.GetWpfShapesFromDxf:   Converted DxfCircle to WPF Path (EllipseGeometry).");
                        break;
                    // IMPORTANT: Add case for DxfLwPolyline if it's used in your DXFs
                    case DxfLwPolyline lwPoly:
                        // Placeholder: Implement DxfLwPolyline to WPF Path conversion
                        wpfShape = ConvertLwPolylineToWpfPath(lwPoly); // You'll need to create this method
                        if(wpfShape != null)
                            System.Diagnostics.Debug.WriteLine($"[JULES_DEBUG] CadService.GetWpfShapesFromDxf:   Converted DxfLwPolyline to WPF Path.");
                        else
                            System.Diagnostics.Debug.WriteLine($"[JULES_DEBUG] CadService.GetWpfShapesFromDxf:   FAILED to convert DxfLwPolyline to WPF Path.");
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine($"[JULES_DEBUG] CadService.GetWpfShapesFromDxf:   EntityType '{entity.GetType().Name}' not explicitly supported for WPF shape conversion. Entity skipped.");
                        break;
                }
                // Add the created shape (or null if conversion failed or type not supported)
                // This ensures the returned list has a 1:1 correspondence with dxfFile.Entities
                wpfShapes.Add(wpfShape);
                entityCounter++;
            }
            System.Diagnostics.Debug.WriteLine($"[JULES_DEBUG] CadService.GetWpfShapesFromDxf: Finished processing. Returning list with {wpfShapes.Count} elements (Shape or null).");
            return wpfShapes;
        }

        /// <summary>
        /// Creates a WPF Path for a DXF Arc.
        /// </summary>
        private System.Windows.Shapes.Path? CreateArcPath(DxfArc dxfArc)
        {
            if (dxfArc == null) {
                System.Diagnostics.Debug.WriteLine("[JULES_DEBUG] CreateArcPath: Input DxfArc is null.");
                return null;
            }
            try
            {
                double startAngleRad = dxfArc.StartAngle * Math.PI / 180.0;
                double endAngleRad = dxfArc.EndAngle * Math.PI / 180.0;
                
                double arcStartX = dxfArc.Center.X + dxfArc.Radius * Math.Cos(startAngleRad);
                double arcStartY = dxfArc.Center.Y + dxfArc.Radius * Math.Sin(startAngleRad);
                var pathStartPoint = new System.Windows.Point(arcStartX, arcStartY);

                double arcEndX = dxfArc.Center.X + dxfArc.Radius * Math.Cos(endAngleRad);
                double arcEndY = dxfArc.Center.Y + dxfArc.Radius * Math.Sin(endAngleRad);
                var arcSegmentEndPoint = new System.Windows.Point(arcEndX, arcEndY);

                double sweepAngleDegrees = dxfArc.EndAngle - dxfArc.StartAngle;
                if (sweepAngleDegrees < 0) sweepAngleDegrees += 360;
                
                bool isLargeArc = sweepAngleDegrees > 180.0;
                SweepDirection sweepDirection = SweepDirection.Counterclockwise;

                ArcSegment arcSegment = new ArcSegment
                {
                    Point = arcSegmentEndPoint,
                    Size = new System.Windows.Size(dxfArc.Radius, dxfArc.Radius),
                    IsLargeArc = isLargeArc,
                    SweepDirection = sweepDirection,
                    RotationAngle = 0,
                    IsStroked = true
                };

                PathFigure pathFigure = new PathFigure
                {
                    StartPoint = pathStartPoint,
                    IsClosed = false
                };
                pathFigure.Segments.Add(arcSegment);

                PathGeometry pathGeometry = new PathGeometry();
                pathGeometry.Figures.Add(pathFigure);

                return new System.Windows.Shapes.Path
                {
                    Data = pathGeometry,
                    Fill = Brushes.Transparent,
                    IsHitTestVisible = true
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a DXF Line entity into a list of two System.Windows.Point objects.
        /// </summary>
        public List<System.Windows.Point> ConvertLineToPoints(DxfLine line)
        {
            var points = new List<System.Windows.Point>();
            if (line == null) return points;
            points.Add(new System.Windows.Point(line.P1.X, line.P1.Y));
            points.Add(new System.Windows.Point(line.P2.X, line.P2.Y));
            return points;
        }

        /// <summary>
        /// Converts a DXF Arc entity into a list of discretized System.Windows.Point objects.
        /// </summary>
        public List<System.Windows.Point> ConvertArcToPoints(DxfArc arc, double resolutionDegrees)
        {
            var points = new List<System.Windows.Point>();
            if (arc == null || resolutionDegrees <= 0) return points;

            double startAngle = arc.StartAngle;
            double endAngle = arc.EndAngle;
            double radius = arc.Radius;
            System.Windows.Point center = new System.Windows.Point(arc.Center.X, arc.Center.Y);

            // Normalize angles
            if (endAngle < startAngle) endAngle += 360;

            double currentAngle = startAngle;
            while (currentAngle <= endAngle)
            {
                double radAngle = currentAngle * Math.PI / 180.0;
                double x = center.X + radius * Math.Cos(radAngle);
                double y = center.Y + radius * Math.Sin(radAngle);
                points.Add(new System.Windows.Point(x, y));
                
                currentAngle += resolutionDegrees;
            }

            // Ensure end point is included
            if (Math.Abs(currentAngle - resolutionDegrees - endAngle) > 0.001)
            {
                double endRadAngle = endAngle * Math.PI / 180.0;
                double endX = center.X + radius * Math.Cos(endRadAngle);
                double endY = center.Y + radius * Math.Sin(endRadAngle);
                points.Add(new System.Windows.Point(endX, endY));
            }

            return points;
        }

        /// <summary>
        /// Converts a DXF Circle entity to a list of points representing its perimeter.
        /// </summary>
        public List<System.Windows.Point> ConvertCircleToPoints(DxfCircle circle, double resolutionDegrees)
        {
            List<System.Windows.Point> points = new List<System.Windows.Point>();
            if (circle == null || resolutionDegrees <= 0) return points;

            for (double angle = 0; angle < 360.0; angle += resolutionDegrees)
            {
                double radAngle = angle * Math.PI / 180.0;
                double x = circle.Center.X + circle.Radius * Math.Cos(radAngle);
                double y = circle.Center.Y + circle.Radius * Math.Sin(radAngle);
                points.Add(new System.Windows.Point(x, y));
            }

            return points;
        }

        /* // LightWeightPolyline processing removed as per subtask
        /// <summary>
        /// Converts a DXF LwPolyline entity into a list of discretized System.Windows.Point objects.
        /// </summary>
        public List<System.Windows.Point> ConvertLwPolylineToPoints(LightWeightPolyline polyline, double arcResolutionDegrees)
        {
            var points = new List<System.Windows.Point>();
            if (polyline == null || polyline.Vertices.Count == 0) return points;
            for (int i = 0; i < polyline.Vertices.Count; i++) {
                var currentVertexInfo = polyline.Vertices[i];
                System.Windows.Point currentDxfPoint = new System.Windows.Point(currentVertexInfo.Position.X, currentVertexInfo.Position.Y);
                points.Add(currentDxfPoint);
                if (Math.Abs(currentVertexInfo.Bulge) > 0.0001) {
                    if (!polyline.IsClosed && i == polyline.Vertices.Count - 1) continue;
                    // TODO: Implement LwPolyline bulge to Arc conversion for trajectory points.
                }
            }
            if (polyline.IsClosed && points.Count > 1 && System.Windows.Point.Subtract(points.First(), points.Last()).Length > 0.001) { // Requires System.Linq for .First() and .Last()
                 points.Add(points[0]);
            } else if (polyline.Vertices.Count == 1 && !points.Any()){ // Requires System.Linq for .Any()
                 points.Add(new System.Windows.Point(polyline.Vertices[0].Position.X, polyline.Vertices[0].Position.Y));
            }
            return points;
        }
        */
    // } // This closing brace was prematurely ending the CadService class. Moved to the end.

    // New methods to generate points from Trajectory geometric parameters

    /// <summary>
    /// Converts a Line Trajectory object into a list of two System.Windows.Point objects.
    /// Respects the IsReversed flag on the Trajectory.
    /// </summary>
    public List<System.Windows.Point> ConvertLineTrajectoryToPoints(Trajectory trajectory)
    {
        var points = new List<System.Windows.Point>();
        if (trajectory == null || trajectory.PrimitiveType != "Line") return points;

        DxfPoint start = trajectory.LineStartPoint;
        DxfPoint end = trajectory.LineEndPoint;

        if (trajectory.IsReversed)
        {
            points.Add(new System.Windows.Point(end.X, end.Y));
            points.Add(new System.Windows.Point(start.X, start.Y));
        }
        else
        {
            points.Add(new System.Windows.Point(start.X, start.Y));
            points.Add(new System.Windows.Point(end.X, end.Y));
        }
        return points;
    }

    /// <summary>
    /// Converts an Arc Trajectory object into a list of discretized System.Windows.Point objects.
    /// Respects the IsReversed flag on the Trajectory.
    /// </summary>
    public List<System.Windows.Point> ConvertArcTrajectoryToPoints(Trajectory trajectory, double resolutionDegrees)
    {
        var points = new List<System.Windows.Point>();
        if (trajectory == null || trajectory.PrimitiveType != "Arc" || resolutionDegrees <= 0) return points;

        // Use the 3-point arc definition
        DxfPoint p1 = trajectory.ArcPoint1.Coordinates;
        DxfPoint p2 = trajectory.ArcPoint2.Coordinates; // Midpoint
        DxfPoint p3 = trajectory.ArcPoint3.Coordinates;

        // Calculate center, radius, start angle, end angle from 3 points
        // This is a non-trivial calculation. For simplicity, we'll assume these were pre-calculated
        // and stored in the DxfArc object if available, or we need a helper function.
        // For now, let's assume OriginalDxfEntity is a DxfArc and use its properties.
        if (trajectory.OriginalDxfEntity is DxfArc dxfArc)
        {
            double startAngle = dxfArc.StartAngle;
            double endAngle = dxfArc.EndAngle;
            double radius = dxfArc.Radius;
            System.Windows.Point center = new System.Windows.Point(dxfArc.Center.X, dxfArc.Center.Y);

            if (trajectory.IsReversed)
            {
                double temp = startAngle;
                startAngle = endAngle;
                endAngle = temp;
            }

            double currentSweepAngle = endAngle - startAngle;
            if (currentSweepAngle < 0)
            {
                currentSweepAngle += 360.0;
            }
            double effectiveEndAngle = startAngle + currentSweepAngle;

            double currentAngle = startAngle;
            while (currentAngle <= effectiveEndAngle + (resolutionDegrees / 2.0))
            {
                double angleToProcess = Math.Min(currentAngle, effectiveEndAngle);
                double radAngle = angleToProcess * Math.PI / 180.0;
                double x = center.X + radius * Math.Cos(radAngle);
                double y = center.Y + radius * Math.Sin(radAngle);
                points.Add(new System.Windows.Point(x, y));

                if (angleToProcess >= effectiveEndAngle - 0.00001) break;

                currentAngle += resolutionDegrees;
                if (currentAngle > effectiveEndAngle && angleToProcess < effectiveEndAngle - 0.00001)
                {
                    currentAngle = effectiveEndAngle;
                }
            }
        }
        else
        {
            // Fallback or error handling if OriginalDxfEntity is not a DxfArc or is null
            // This part needs a robust way to calculate arc parameters from p1, p2, p3
            // For now, we'll return an empty list if we can't get parameters from DxfArc
            Console.WriteLine("Warning: Could not generate points for arc trajectory. OriginalDxfEntity is not a DxfArc or is null.");
        }
        return points;
    }

    /// <summary>
    /// Converts a Circle Trajectory object to a list of points representing its perimeter.
    /// IsReversed is currently not implemented for circle point generation.
    /// </summary>
    public List<System.Windows.Point> ConvertCircleTrajectoryToPoints(Trajectory trajectory, double resolutionDegrees)
    {
        // This method is now superseded by logic in MainWindow.PopulateTrajectoryPoints for circles,
        // which uses the new 3-point circle definition.
        // The Trajectory object no longer has CircleCenter or CircleRadius directly.
        // Keeping the method signature for now but it will not compile/work as is.
        // It should be removed or refactored if CadService needs to generate points from a 3-point defined circle trajectory.
        System.Diagnostics.Debug.WriteLine("[WARNING] CadService.ConvertCircleTrajectoryToPoints is called but is based on obsolete Trajectory properties.");
        List<System.Windows.Point> points = new List<System.Windows.Point>();
        if (trajectory == null || trajectory.PrimitiveType != "Circle" || resolutionDegrees <= 0) return points;

        // The following code will not compile as trajectory.CircleCenter and trajectory.CircleRadius are removed.
        // // Note: trajectory.CircleNormal is available if needed for 3D calculations.
        // // IsReversed is not currently affecting circle point generation.
        // for (double angle = 0; angle < 360.0; angle += resolutionDegrees)
        // {
        //     double radAngle = angle * Math.PI / 180.0;
        //     double x = trajectory.CircleCenter.X + trajectory.CircleRadius * Math.Cos(radAngle);
        //     double y = trajectory.CircleCenter.Y + trajectory.CircleRadius * Math.Sin(radAngle);
        //     points.Add(new System.Windows.Point(x, y));
        // }
        // // Ensure the circle is closed by adding the start point if it's not already the last point due to resolution.
        // if (points.Count > 0)
        // {
        //      if (Math.Abs(points.Last().X - (trajectory.CircleCenter.X + trajectory.CircleRadius)) > 0.001 ||
        //          Math.Abs(points.Last().Y - trajectory.CircleCenter.Y) > 0.001)
        //          {
        //               points.Add(new System.Windows.Point(trajectory.CircleCenter.X + trajectory.CircleRadius, trajectory.CircleCenter.Y));
        //          }
        // }
        return points; // Returns empty list as the functional code is commented out.
    }


    private System.Windows.Shapes.Path? ConvertLwPolylineToWpfPath(DxfLwPolyline lwPolyline)
    {
        if (lwPolyline.Vertices.Count == 0)
            return null;

        PathGeometry pathGeometry = new PathGeometry();
        PathFigure pathFigure = new PathFigure();

        var firstVertex = lwPolyline.Vertices.First();
        pathFigure.StartPoint = new Point(firstVertex.X, firstVertex.Y);

        for (int i = 0; i < lwPolyline.Vertices.Count; i++)
        {
            var v1 = lwPolyline.Vertices[i];
            DxfLwPolylineVertex v2;

            if (i < lwPolyline.Vertices.Count - 1)
            {
                v2 = lwPolyline.Vertices[i + 1];
            }
            else if (lwPolyline.IsClosed)
            {
                v2 = lwPolyline.Vertices.First(); // Close back to the start
            }
            else
            {
                // Open polyline, last vertex already added as part of a previous segment's end or as start point if only one vertex
                if (lwPolyline.Vertices.Count == 1 && i == 0) {
                     // Special case: Polyline with a single vertex. PathFigure already has StartPoint.
                     // To make it visible, we can add a tiny line segment to itself, or just let it be a point.
                     // For simplicity, if it's just one vertex, it's effectively a point. PathFigure alone might not render.
                     // Adding a line to itself to make it a "visible" zero-length path for consistency.
                     pathFigure.Segments.Add(new LineSegment(pathFigure.StartPoint, true));
                }
                break;
            }

            Point p2Wpf = new Point(v2.X, v2.Y);

            if (Math.Abs(v1.Bulge) < 1e-6) // Consider bulge 0 as a straight line
            {
                // Only add segment if it's not the very first vertex (which is StartPoint)
                // Or if it's a polyline with one vertex, handled above.
                if (i > 0 || lwPolyline.Vertices.Count > 1 || (i==0 && lwPolyline.IsClosed)) { // ensure p2Wpf is an actual next point
                     if ( (pathFigure.Segments.Any() && ((pathFigure.Segments.Last() as LineSegment)?.Point != p2Wpf && (pathFigure.Segments.Last() as ArcSegment)?.Point != p2Wpf)) || !pathFigure.Segments.Any())
                     {
                        if (pathFigure.StartPoint != p2Wpf || lwPolyline.IsClosed || i < lwPolyline.Vertices.Count -1 ) // Avoid adding segment from last point to itself unless closed
                            pathFigure.Segments.Add(new LineSegment(p2Wpf, true));
                     }
                } else if (lwPolyline.Vertices.Count == 1 && i == 0) {
                    // Covered by initial single vertex case - tiny line segment added if desired
                }


            }
            else // Bulge is non-zero, create an ArcSegment
            {
                var arcSegment = CalculateArcSegmentFromBulge(
                    new Point(v1.X, v1.Y),
                    p2Wpf,
                    v1.Bulge);
                pathFigure.Segments.Add(arcSegment);
            }
        }

        pathFigure.IsClosed = lwPolyline.IsClosed;
        pathGeometry.Figures.Add(pathFigure);

        return new System.Windows.Shapes.Path
        {
            Data = pathGeometry,
            Fill = Brushes.Transparent, // LwPolylines are typically not filled unless specified
            IsHitTestVisible = true
        };
    }

    /// <summary>
    /// Calculates WPF ArcSegment parameters from two DxfLwPolylineVertex points and a bulge value.
    /// </summary>
    private ArcSegment CalculateArcSegmentFromBulge(Point p1, Point p2, double bulge)
    {
        // Formulas for converting bulge to arc parameters (center, radius, angles)
        // bulge = tan(theta / 4), where theta is the included angle of the arc segment.
        double theta = 4 * Math.Atan(bulge);

        // Distance between p1 and p2 (chord length c)
        double c = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

        if (Math.Abs(c) < 1e-9) // Points are coincident, treat as zero-length line segment
        {
            return new ArcSegment(p2, new Size(0,0), 0, false, SweepDirection.Counterclockwise, true);
        }

        // Radius of the arc
        // R = c / (2 * sin(theta / 2))
        // Handle theta = 0 case (straight line, though already filtered by bulge check)
        // Handle theta = 2*PI (full circle, bulge would be infinite or very large, not typical for segment)
        double radius;
        if (Math.Abs(Math.Sin(theta / 2.0)) < 1e-9) // Angle is too small, effectively a line
        {
             radius = double.MaxValue; // Effectively a line
        } else {
             radius = c / (2 * Math.Sin(theta / 2.0));
        }

        radius = Math.Abs(radius); // Radius must be positive

        // Determine sweep direction and isLargeArc flag
        // SweepDirection is Counterclockwise if bulge > 0, Clockwise if bulge < 0.
        SweepDirection sweepDirection = (bulge > 0) ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;

        // IsLargeArc is true if |theta| > PI (180 degrees)
        bool isLargeArc = Math.Abs(theta) > Math.PI;

        // WPF ArcSegment uses Size(radius, radius) for circular arcs
        Size arcSize = new Size(radius, radius);

        // If radius becomes excessively large (almost straight line), WPF might have issues.
        // It might be better to return a LineSegment in such extreme cases, but the bulge check should catch most.
        if (double.IsInfinity(radius) || double.IsNaN(radius) || radius > 1e12) // Arbitrary large number
        {
            // This case should ideally be caught by the bulge check (bulge near zero -> LineSegment)
            // If we reach here, it's an extreme arc. Fallback to a line-like arc.
            // Forcing a very small curvature or treating as line if radius is too large.
            // However, WPF's ArcSegment is robust. Let's trust it with large radii if bulge wasn't zero.
            // The primary check is for bulge == 0.
        }


        return new ArcSegment(
            p2,             // End point of the arc segment
            arcSize,        // Size (radiusX, radiusY)
            0,              // RotationAngle (usually 0 for 2D DXF LwPolyline arcs)
            isLargeArc,
            sweepDirection,
            true            // IsStroked
        );
    }
} // This closes the CadService class

} // This closes the namespace RobTeach.Services
