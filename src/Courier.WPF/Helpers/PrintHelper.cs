using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;

namespace Courier.WPF.Helpers;

public static class PrintHelper
{
    public static void PrintTable(DataTable table, string title)
    {
        if (table.Rows.Count == 0)
        {
            Ui.Warn("Нет данных для печати.");
            return;
        }

        var document = new FlowDocument
        {
            PagePadding = new Thickness(40),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 10
        };

        document.Blocks.Add(new Paragraph(new Run("ИС «Курьерская служба»\n" + title))
        {
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        });

        document.Blocks.Add(new Paragraph(new Run(
            $"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}\n" +
            $"Пользователь: {AppSession.User.Fio} ({AppSession.User.Role})"))
        {
            FontSize = 10,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var printTable = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) };
        foreach (DataColumn _ in table.Columns)
            printTable.Columns.Add(new TableColumn());

        var group = new TableRowGroup();
        var header = new TableRow { Background = new SolidColorBrush(Color.FromRgb(204, 251, 241)) };
        foreach (DataColumn column in table.Columns)
        {
            header.Cells.Add(new TableCell(new Paragraph(new Run(column.ColumnName)))
            {
                Padding = new Thickness(4),
                FontWeight = FontWeights.SemiBold
            });
        }
        group.Rows.Add(header);

        foreach (DataRow row in table.Rows)
        {
            var tableRow = new TableRow();
            foreach (DataColumn column in table.Columns)
            {
                var value = row[column] is DBNull ? "" : row[column]?.ToString() ?? "";
                if (row[column] is DateTime dt) value = dt.ToString("dd.MM.yyyy HH:mm");
                else if (row[column] is decimal d) value = d.ToString("N2", CultureInfo.CurrentCulture);
                tableRow.Cells.Add(new TableCell(new Paragraph(new Run(value)))
                {
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 0.3)
                });
            }
            group.Rows.Add(tableRow);
        }

        printTable.RowGroups.Add(group);
        document.Blocks.Add(printTable);
        document.Blocks.Add(new Paragraph(new Run($"Всего записей: {table.Rows.Count}"))
        {
            FontSize = 10,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 10, 0, 0)
        });

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;
        document.PageHeight = dialog.PrintableAreaHeight;
        document.PageWidth = dialog.PrintableAreaWidth;
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, title);
    }

    public static void ExportCsv(DataTable table, string defaultName)
    {
        if (table.Rows.Count == 0)
        {
            Ui.Warn("Нет данных для экспорта.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV-файл (*.csv)|*.csv",
            FileName = $"{defaultName}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dialog.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", table.Columns.Cast<DataColumn>()
            .Select(c => Escape(c.ColumnName))));

        foreach (DataRow row in table.Rows)
        {
            sb.AppendLine(string.Join(";", table.Columns.Cast<DataColumn>()
                .Select(c => Escape(row[c] is DBNull ? "" : row[c]?.ToString() ?? ""))));
        }

        File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
        Ui.Info($"Файл сохранён:\n{dialog.FileName}", "Экспорт");
    }

    private static string Escape(string value)
        => value.Contains(';') || value.Contains('"') || value.Contains('\n')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
