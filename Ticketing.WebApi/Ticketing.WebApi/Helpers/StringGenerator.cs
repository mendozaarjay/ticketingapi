
using System.Collections.Generic;
using System.IO;
using System;
using System.Configuration;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Drawing;

public static class StringGenerator
{
    private static readonly int STRING_WIDTH = 50;
    public static string FormatCenterString(string value)
    {
        int width = STRING_WIDTH;
        int padding = (width - value.Length) / 2;
        return value.PadLeft(padding + value.Length);
    }
    public static string FormatLabelWithDoubleValue(string label, string value)
    {
        var output = String.Format("{0,-26}  {1,-10}  {2,10}", label, ": Php", double.Parse(value).ToString("N2"));
        return output;
    }

    public static string FormatLabel(string label)
    {
        var output = string.Format("{0,-15} : {1}", label, "_");
        var underscorecount = 50 - output.Length;
        var underscores = new string('_', underscorecount);
        return output + underscores;
    }
    public static string FormatLabelWithStringValue(string label, string value,bool alignToDouble = false)
    {
        var output = string.Empty;
        if (alignToDouble)
        {
             output = String.Format("{0,-26}  {1,-10}  {2,10}", label, ": ", value);
            return output;
        }
        output = string.Format("{0,-15} : {1}", label, value);
        return output;
    }
    public static string FormatPreparedBy(string label, string value)
    {
        var output = string.Empty;
        output = String.Format("{0,-26}  {1,-2}  {2,18}", label, ": ", value);
        return output;
    }
    public static string AddNewLineSeparator()
    {
        var output = new string('=', 50);
        return output;
    }
    public static string FormatLabelWithThreeColumns(string column1, string column2, string column3)
    {
        int columnWidth = STRING_WIDTH / 3;
        string formattedString = string.Format("{0,-" + columnWidth + "} {1,-" + columnWidth + "} {2,-" + columnWidth + "}",
                                               column1.Substring(0, Math.Min(column1.Length, columnWidth)),
                                               column2.Substring(0, Math.Min(column2.Length, columnWidth)),
                                               column3.Substring(0, Math.Min(column3.Length, columnWidth)));
        return formattedString;
    }
    public static string FormatLabelWithFourColumns(string column1, string column2, string column3, string column4)
    {
        int columnWidth = STRING_WIDTH / 4;
        string formattedString = string.Format("{0,-" + columnWidth + "} {1,-" + columnWidth + "} {2,-" + columnWidth + "} {3,-" + columnWidth + "}",
                                               column1.Substring(0, Math.Min(column1.Length, columnWidth)),
                                               column2.Substring(0, Math.Min(column2.Length, columnWidth)),
                                               column3.Substring(0, Math.Min(column3.Length, columnWidth)),
                                               column4.Substring(0, Math.Min(column4.Length, columnWidth)));
        return formattedString;
    }
    public static void GenerateJournal(JournalType journalType, List<string> file, string filename)
    {
        if (!filename.ToLower().Contains(".txt"))
        {
            filename = filename + ".txt";
        }
        var initialPath = ConfigurationManager.AppSettings["JournalPath"].ToString();
        var year = DateTime.Now.ToString("yyyy");
        var month = DateTime.Now.ToString("MM");
        var day = DateTime.Now.ToString("dd");

        var foldername = string.Empty;
        switch (journalType)
        {
            case JournalType.OfficialReceipt:
                foldername = "OFFICIAL RECEIPTS";
                break;
            case JournalType.XReading:
                foldername = "X READING";
                break;
            case JournalType.YReading:
                foldername = "Y READING";
                break;
            case JournalType.ZReading:
                foldername = "Z READING";
                break;
        }

        var path = Path.Combine(initialPath, foldername, year.ToString(), $"{year}{month}", $"{year}{month}{day}");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        var filePath = Path.Combine(path, filename);
        File.WriteAllLines(filePath, file);

    }

}
public enum JournalType
{
    OfficialReceipt,
    XReading,
    YReading,
    ZReading,
}