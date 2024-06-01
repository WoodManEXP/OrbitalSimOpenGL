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

        internal void ApproachDistances(String approachStatusStr)
        {
            Paragraph aParagraph;
            Table aTable;
            TableRow aTableRow;
            TableRowGroup aTableRowGroup;
            TableCell aTableCell;
            Run aRun;
            String aStr;

            ApproachStatus approachStatus = new(approachStatusStr);

            FlowDocument.Blocks.Clear(); // Empty the document contents

            aParagraph = new Paragraph();
            aParagraph.Inlines.Add(new Bold(new Run("Approaches ")));
            aParagraph.Inlines.Add(new Run("(Dist is km)"));
            aParagraph.Inlines.Add(new LineBreak());
            aParagraph.Inlines.Add(new Run("Ephemeris gather DT "));
            aParagraph.Inlines.Add(approachStatus.ApproachStatusInfo.DateTime.ToString("F"));

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
            aTableCell.Blocks.Add(new Paragraph(new Run("Dist")));
            aTableRow.Cells.Add(aTableCell);
            aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Center };
            aTableCell.Blocks.Add(new Paragraph(new Run("Date/Time")));
            aTableRow.Cells.Add(aTableCell);
            aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Center };
            aTableCell.Blocks.Add(new Paragraph(new Run("Dist")));
            aTableRow.Cells.Add(aTableCell);
            aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Center };
            aTableCell.Blocks.Add(new Paragraph(new Run("Date/Time")));
            aTableRow.Cells.Add(aTableCell);
            aTableRowGroup.Rows.Add(aTableRow);

            // Over each element in ApproachStatusInfo.ApproachStatusBody
            foreach (var approachStatusBody in approachStatus.ApproachStatusInfo.ApproachStatusBody)
            {
                // Body for which approaches will be displayed
                aTableRow = new TableRow();
                aTableCell = new TableCell() { ColumnSpan = 1, Background = Brushes.LightGreen };
                aTableCell.Blocks.Add(new Paragraph(new Run(approachStatusBody.Name)));
                aTableRow.Cells.Add(aTableCell);
                aTableRowGroup.Rows.Add(aTableRow);

                // What are the closest and furthest distances in this pass?
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
                    aTableCell.Blocks.Add(new Paragraph(new Run("\u00A0\u00A0" + approachElement.Name))); // Thise are nonbreaking spaces
                    aTableRow.Cells.Add(aTableCell);
                    // Closest distance
                    aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Right, Background = Brushes.LightGreen };
                    aStr = approachElement.CDist.ToString("#,##0");
                    if (closestDist == approachElement.CDist)
                        aParagraph = new Paragraph(new Bold(new Run(aStr)));
                    else
                        aParagraph = new Paragraph(new Run(aStr));
                    aTableCell.Blocks.Add(aParagraph);
                    aTableRow.Cells.Add(aTableCell);
                    // Date/Time for this CDist
                    DateTime aDT = approachStatus.ApproachStatusInfo.DateTime.AddSeconds(approachElement.CSeconds);
                    aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Right, Background = Brushes.LightGreen };
                    aParagraph = new Paragraph();
                    aParagraph.Inlines.Add(new Run(aDT.ToString("F")));
                    aParagraph.Inlines.Add(new LineBreak());
                    aParagraph.Inlines.Add(new Run(ElapsedTime(approachElement.CSeconds)));
                    aTableCell.Blocks.Add(aParagraph);
                    aTableRow.Cells.Add(aTableCell);
                    // Furthest distance
                    aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Right, Background = Brushes.LightGreen };
                    aStr = approachElement.FDist.ToString("#,##0");
                    if (furthestDist == approachElement.FDist)
                        aParagraph = new Paragraph(new Bold(new Run(aStr)));
                    else
                        aParagraph = new Paragraph(new Run(aStr));
                    aTableCell.Blocks.Add(aParagraph);
                    aTableRow.Cells.Add(aTableCell);
                    // Date/Time for this FDist
                    aDT = approachStatus.ApproachStatusInfo.DateTime.AddSeconds(approachElement.FSeconds);
                    aTableCell = new TableCell() { ColumnSpan = 1, TextAlignment = TextAlignment.Right, Background = Brushes.LightGreen };
                    aParagraph = new Paragraph();
                    aParagraph.Inlines.Add(new Run(aDT.ToString("F")));
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
