using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OrbitalSimOpenGL
{
    /// <summary>
    /// Interaction logic for StatusWindow.xaml
    /// </summary>
    public partial class OrbitalSimStatusWindow : Window
    {
        #region Properties
        public CommandStatuslWindow? CommandStatuslWindow { get; set; }
        #endregion

        public OrbitalSimStatusWindow()
        {
            InitializeComponent();

            // Receives commands for status window
            CommandStatuslWindow = new(Dispatcher);

            // Register command delegate(s)
            CommandStatuslWindow.GenericRegister(GenericCommand);
        }

        // Window loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Command coming in from somewhere on the message queue
        /// </summary>
        /// <param name="args"></param>
        private void GenericCommand(object[] args)
        {
            switch ((CommandStatuslWindow.GenericCommands)args[0])
            {
                case CommandStatuslWindow.GenericCommands.ApproachDistance:
                    ApproachDistances((String)args[1]);
                    break;

                default:
                    break;
            }
        }

        readonly String DFmt = "dd MMM yyyy HH:mm";

        internal void ApproachDistances(String approachStatusStr)
        {
            Paragraph aParagraph;
            Table aTable;
            TableRow aTableRow;
            TableRowGroup aTableRowGroup;
            TableCell aTableCell;
            Run aRun;
            SolidColorBrush aSolidColorBrush;
            String aStr, vectorLenStr;

            ApproachStatus approachStatus = new(approachStatusStr);

            FlowDocument.Blocks.Clear(); // Clear any document contents

            aParagraph = new Paragraph();
            aParagraph.Inlines.Add(new Bold(new Run("Approaches ")));
            aParagraph.Inlines.Add(new Run("(Dist is km, Velocity is km/s)"));
            aParagraph.Inlines.Add(new LineBreak());
            aParagraph.Inlines.Add(new Run("Ephemeris gather DT "));
            aParagraph.Inlines.Add(approachStatus.ApproachStatusInfo.DateTime.ToString(DFmt));

            FlowDocument.Blocks.Add(aParagraph);

            aTable = new Table();
            aTable.Columns.Add(new TableColumn());
            aTable.Columns.Add(new TableColumn());
            aTable.Columns.Add(new TableColumn());
            aTable.Columns.Add(new TableColumn());
            aTable.Columns.Add(new TableColumn());

            aTableRowGroup = new TableRowGroup();

            // Headers
            aTableRow = new TableRow();
            aTableCell = new TableCell() { ColumnSpan = 1 };
            aTableCell.Blocks.Add(new Paragraph());
            aTableRow.Cells.Add(aTableCell);
            aTableCell = new TableCell() { ColumnSpan = 2, TextAlignment = TextAlignment.Center };
            aTableCell.Blocks.Add(new Paragraph(new Run("Closest")));
            aTableRow.Cells.Add(aTableCell);
            aTableCell = new TableCell() { ColumnSpan = 2, TextAlignment = TextAlignment.Center };
            aTableCell.Blocks.Add(new Paragraph(new Run("Furthest")));
            aTableRow.Cells.Add(aTableCell);
            aTableRowGroup.Rows.Add(aTableRow);

            aTableRow = new TableRow();
            aTableCell = new TableCell() { ColumnSpan = 1 };
            aTableCell.Blocks.Add(new Paragraph());
            aTableRow.Cells.Add(aTableCell);
            aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Center };
            aTableCell.Blocks.Add(new Paragraph(new Run("Dist/Rel velocity")));
            aTableRow.Cells.Add(aTableCell);
            aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Center };
            aTableCell.Blocks.Add(new Paragraph(new Run("Date/Time")));
            aTableRow.Cells.Add(aTableCell);
            aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Center };
            aTableCell.Blocks.Add(new Paragraph(new Run("Dist/Rel velocity")));
            aTableRow.Cells.Add(aTableCell);
            aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Center };
            aTableCell.Blocks.Add(new Paragraph(new Run("Date/Time")));
            aTableRow.Cells.Add(aTableCell);
            aTableRowGroup.Rows.Add(aTableRow);

            bool first = true;
            // Over each element in ApproachStatusInfo.ApproachStatusBody
            foreach (var approachStatusBody in approachStatus.ApproachStatusInfo.ApproachStatusBody)
            {
                // Separator between sections
                if (!first)
                {
                    aTableRow = new TableRow();
                    aTableCell = new TableCell() { ColumnSpan = 1 };
                    aTableCell.Blocks.Add(new Paragraph());
                    aTableRow.Cells.Add(aTableCell);
                    aTableRowGroup.Rows.Add(aTableRow);
                }
                first = false;

                // Body for which approaches will be displayed
                aTableRow = new TableRow();
                aTableCell = new TableCell() { ColumnSpan = 1, Background = Brushes.LightGreen };
                aTableCell.Blocks.Add(new Paragraph(new Run(approachStatusBody.Name)));
                aTableRow.Cells.Add(aTableCell);
                aTableRowGroup.Rows.Add(aTableRow);

                // What are the closest and furthest distances in this pass?
                // Calc their values for highlighting below.
                Double closestDist = Double.MaxValue;
                Double furthestDist = Double.MinValue;
                foreach (var approachElement in approachStatusBody.ApproachElements)
                {
                    closestDist = Math.Min(closestDist, approachElement.CDist);
                    furthestDist = Math.Max(furthestDist, approachElement.FDist);
                }

                // Approach info for approaches to each of the other bodies
                foreach (var approachElement in approachStatusBody.ApproachElements)
                {
                    aTableRow = new TableRow();
                    // Body name
                    aTableCell = new TableCell() { ColumnSpan = 1, Background = Brushes.LightGreen };
                    aTableCell.Blocks.Add(new Paragraph(new Run("\u00A0\u00A0" + approachElement.Name))); // These are nonbreaking spaces
                    aTableRow.Cells.Add(aTableCell);
                    // Closest distance
                    aStr = Math.Sqrt(approachElement.CDist).ToString("#,##0");
                    vectorLenStr = VectorLen(approachElement.CVX, approachElement.CVY, approachElement.CVZ);
                    if (closestDist == approachElement.CDist)
                    {
                        aParagraph = new Paragraph(new Bold(new Run(aStr))) { Foreground = Brushes.Black };
                        aParagraph.Inlines.Add(new LineBreak());
                        aParagraph.Inlines.Add(new Bold(new Run(vectorLenStr)));
                        aSolidColorBrush = Brushes.DarkSalmon;
                    }
                    else
                    {
                        aParagraph = new Paragraph(new Run(aStr));
                        aParagraph.Inlines.Add(new LineBreak());
                        aParagraph.Inlines.Add(new Run(vectorLenStr));
                        aSolidColorBrush = Brushes.LightGreen;
                    }

                    aParagraph.Padding = new Thickness(2);
                    aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Right, Background = aSolidColorBrush };
                    aTableCell.Blocks.Add(aParagraph);
                    aTableRow.Cells.Add(aTableCell);
                    // Date/Time for this CDist
                    DateTime aDT = approachStatus.ApproachStatusInfo.DateTime.AddSeconds(approachElement.CSeconds);
                    aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Right, Background = Brushes.LightGreen };
                    aParagraph = new Paragraph();
                    aParagraph.Padding = new Thickness(2);
                    aParagraph.Inlines.Add(new Run(aDT.ToString(DFmt)));
                    aParagraph.Inlines.Add(new LineBreak());
                    aParagraph.Inlines.Add(new Run(ElapsedTime(approachElement.CSeconds)));
                    aTableCell.Blocks.Add(aParagraph);
                    aTableRow.Cells.Add(aTableCell);
                    // Furthest distance
                    aStr = Math.Sqrt(approachElement.FDist).ToString("#,##0");
                    vectorLenStr = VectorLen(approachElement.FVX, approachElement.FVY, approachElement.FVZ);
                    if (furthestDist == approachElement.FDist)
                    {
                        aParagraph = new Paragraph(new Bold(new Run(aStr))) { Foreground = Brushes.Black };
                        aParagraph.Inlines.Add(new LineBreak());
                        aParagraph.Inlines.Add(new Bold(new Run(vectorLenStr)));
                        aSolidColorBrush = Brushes.DarkSalmon;
                    }
                    else
                    {
                        aParagraph = new Paragraph(new Run(aStr));
                        aParagraph.Inlines.Add(new LineBreak());
                        aParagraph.Inlines.Add(new Run(vectorLenStr));
                        aSolidColorBrush = Brushes.LightGreen;
                    }

                    aParagraph.Padding = new Thickness(2);
                    aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Right, Background = aSolidColorBrush };
                    aTableCell.Blocks.Add(aParagraph);
                    aTableRow.Cells.Add(aTableCell);
                    // Date/Time for this FDist
                    aDT = approachStatus.ApproachStatusInfo.DateTime.AddSeconds(approachElement.FSeconds);
                    aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Right, Background = Brushes.LightGreen };
                    aParagraph = new Paragraph();
                    aParagraph.Padding = new Thickness(2);
                    aParagraph.Inlines.Add(new Run(aDT.ToString(DFmt)));
                    aParagraph.Inlines.Add(new LineBreak());
                    aParagraph.Inlines.Add(new Run(ElapsedTime(approachElement.FSeconds)));
                    aTableCell.Blocks.Add(aParagraph);
                    aTableRow.Cells.Add(aTableCell);

                    aTableRowGroup.Rows.Add(aTableRow);

                }
            }

            aTable.RowGroups.Add(aTableRowGroup);

            FlowDocument.Blocks.Add(aTable);
        }

        private static string VectorLen(double cVX, double cVY, double cVZ)
        {
            return Math.Sqrt(cVX*cVX + cVY*cVY + cVZ*cVZ).ToString("#,##0");
        }

        private String ElapsedTime(Double seconds)
        {
            TimeSpan elapsedTime = TimeSpan.FromSeconds(seconds);

            int minutes = elapsedTime.Minutes;
            int hours = elapsedTime.Hours;
            int days = elapsedTime.Days;

            Single years = (Single)days / 365.25F;

            return days.ToString("#,##0") + " days "
                + hours.ToString("#,##0") + " hrs "
                + minutes.ToString("#,##0") + " mins "
                + " ~" + years.ToString("#,##0") + " Earth yrs"
                ;
        }
    }
}
