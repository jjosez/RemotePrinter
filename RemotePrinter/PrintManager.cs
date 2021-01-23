using System;
using System.Management;
using System.Text;

namespace RemotePrinter
{
    public class PrintManager
    {
        public static string openCommand;
        public static string cutCommand;

        public static void Print(string text, bool cut = false, bool open = false)
        {
            LoadCommands(cut, open);           
            string PrinterName = Properties.Settings.Default.defaultprinter;

            //StringBuilder builder = new StringBuilder(data);
            //builder.Replace("[[cut]]", cutCommand);
            //builder.Replace("[[opendrawer]]", openCommand);
            //data = builder.ToString();
                       
            if (!string.IsNullOrEmpty(text))
            {
                string ticket = string.Concat(text, cutCommand, openCommand);
                //string ticket = text.Replace("[[cut]]", cutCommand).Replace("[[opendrawer]]", openCommand);                
                RawPrinterHelper.SendStringToPrinter(PrinterName, ticket);
                //RawPrinterHelper.SendStringToPrinterISO(PrinterName, ticket);
                //RawPrinterHelper.SendUTF8StringToPrinter(PrinterName, ticket);
                Console.WriteLine(ticket);
            }
            else
            {
                Console.WriteLine("La respuesta esta vacia");
            }
        }

        private static void LoadCommands(bool cut, bool open)
        {
            if (cut)
            {
                foreach (var c in Properties.Settings.Default.cutcommand.Split('.'))
                {
                    int unicode = Convert.ToInt32(c);
                    char character = (char)unicode;

                    cutCommand += character.ToString();
                }
                Console.WriteLine(cutCommand);
            } 
            else
            {
                cutCommand = "";
            }

            if (open)
            {
                foreach (var c in Properties.Settings.Default.drawercommand.Split('.'))
                {
                    int unicode = Convert.ToInt32(c);
                    char character = (char)unicode;

                    openCommand += character.ToString();
                }
                Console.WriteLine(openCommand);
            }
            else
            {
                openCommand = "";
            }
        }

        private static void getPrintersProperties()
        {
            string query = string.Format("SELECT * from Win32_Printer WHERE Name LIKE '%{0}'", "nombre de impresora");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection coll = searcher.Get();

            foreach (ManagementObject printer in coll)
            {
                string portName = printer["PortName"].ToString();
                Console.WriteLine(string.Format("Printer IP Address: {0}", portName));
                Console.WriteLine(string.Format("Printer{0}", printer.ToString()));
            }
        }
    }
}
