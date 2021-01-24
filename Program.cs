using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using Newtonsoft.Json;

namespace cvs
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args == null || args.Length < 2)
            {
                PrintUsage();
            }
            else if(args[0] == "-d")
            {
                DecryptCVS(args[1]);
                return;
            }
            else if(args[0] == "-e")
            {
                EncryptCVS(args[1]);
                return;
            }
        } 

        static void DecryptCVS(string inputfile)
        {
            string filenameWE = Path.GetFileNameWithoutExtension(inputfile);
            byte[] inputArray = File.ReadAllBytes(inputfile);
            using (MemoryStream inStream = new MemoryStream(inputArray))
            using (BinaryReader reader = new BinaryReader(inStream))
            {
                var strings = reader.ReadInt32();
                reader.BaseStream.Seek(0x1CFAC, SeekOrigin.Begin); //skip data entry
                string[] lines = new string[strings];
                for (int i = 0; i < lines.Length; i++)
                {
                    var offset = reader.ReadInt32();
                    var savepos = reader.BaseStream.Position;
                    var nextoffset = reader.ReadInt32();
                    var size = nextoffset - offset;
                    if (size <= 0)
                    {
                        size = (int)reader.BaseStream.Length - offset;
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        var line = Encoding.UTF8.GetString(reader.ReadBytes(size));
                        lines[i] = Regex.Replace(line, @"\0", "");
                        reader.BaseStream.Seek(savepos, SeekOrigin.Begin);
                    }
                    else
                    {
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                        var line = Encoding.UTF8.GetString(reader.ReadBytes(size));
                        lines[i] = Regex.Replace(line, @"\0", "");
                        reader.BaseStream.Seek(savepos, SeekOrigin.Begin);
                    }
                }
                var linesJson = JsonConvert.SerializeObject(lines, Formatting.Indented);
                File.WriteAllText(filenameWE + ".json", linesJson);
                Console.WriteLine($"File {filenameWE}.json created!");
                reader.Close();
                inStream.Close();
            }
        }

        static void EncryptCVS(string inputJson)
        {
            string txtFileWO = Path.GetFileNameWithoutExtension(inputJson);
            string[] inputArrayJson = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(inputJson));
            try
            {
                byte[] inputArrayCVS = File.ReadAllBytes(txtFileWO + ".cvs");
                using (MemoryStream inStreamCVS = new MemoryStream(inputArrayCVS))
                using (BinaryReader reader = new BinaryReader(inStreamCVS))
                {
                    var strings = reader.ReadInt32();
                    var dataBlock = reader.ReadBytes(0x1CFA8);
                    using (BinaryWriter writer = new BinaryWriter(new FileStream(txtFileWO + "_new.cvs", FileMode.Create)))
                    {
                        writer.Write(strings);
                        writer.Write(dataBlock);


                        for (int i = 0; i < strings; i++)
                        {
                            var offset = reader.ReadInt32();
                            writer.Write(offset);
                            var savepos = writer.BaseStream.Position;
                            writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                            var lineArray = Encoding.UTF8.GetBytes(inputArrayJson[i]);
                            writer.Write(lineArray);
                            writer.BaseStream.Seek(savepos, SeekOrigin.Begin);
                        }
                        writer.BaseStream.Seek(writer.BaseStream.Length, SeekOrigin.Begin);
                        if (writer.BaseStream.Position != 0x1F9457)
                        {
                            byte[] buffer = new byte[inputArrayCVS.Length - writer.BaseStream.Length];
                            writer.Write(buffer);
                        }
                        Console.WriteLine($"File {txtFileWO}_new.cvs created!");
                        writer.Close();
                        reader.Close();
                        inStreamCVS.Close();
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"File {txtFileWO}.cvs not found! \r\nJSON to CVS convertion feature required an original CVS file!");
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Dead Rising 4 CVS converter");
            Console.WriteLine("by LinkOFF");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("cvsTool.exe [argument] <inputfile>");
            Console.WriteLine("");
            Console.WriteLine("Arguments:");
            Console.WriteLine("-d:\tConvert CVS file to JSON");
            Console.WriteLine("-e:\tConvert JSON to CVS");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("cvsTool.exe -d swg_stringtable_en.cvs");
            Console.WriteLine("cvsTool.exe -e swg_stringtable_en.json");
            Console.WriteLine("");
            Console.WriteLine("Note:");
            Console.WriteLine("Convertion JSON to CVS option requires an original CVS file!");
        }
    }
}
