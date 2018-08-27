using System;
using System.IO;
using System.Xml.Serialization;

namespace MaxUndercut
{
    /// <summary>
    /// Stores the settings used to analyze a stock for maximum undercut
    /// </summary>
    [Serializable]
    [XmlRoot]
    public class Settings
    {
        /// <summary>
        /// The filename to which the default settings are saved
        /// </summary>
        [XmlIgnore]
        public const string SettingsFileName = "MaxUndercutSettings.xml";

        /// <summary>
        /// The name of the file to which the output is written
        /// </summary>
        [XmlElement]
        public string Filename
        {
            get
            {
                return this.filename;
            }
            set
            {
                this.filename = Path.GetExtension(value) == ".csv" ? value : value + ".csv";
            }
        }
        [XmlIgnore]
        private string filename;

        /// <summary>
        /// If true, additional information is printed to the console while the stock is being analyzed
        /// </summary>
        [XmlElement]
        public bool Verbose { get; set; }

        /// <summary>
        /// If true, the raw output is saved for the individual stock
        /// </summary>
        [XmlElement]
        public bool PrintRaw { get; set; }

        /// <summary>
        /// The speed at which the execution will occur, which coresponds to the size of the data window to analyze
        /// </summary>
        [XmlElement]
        public Speed Speed { get; set; }

        /// <summary>
        /// Default constructor giving each setting its default value
        /// </summary>
        public Settings()
        {
            this.Filename = "summary.csv";
            this.Verbose = false;
            this.PrintRaw = false;
            this.Speed = Speed.slow;
        }

        /// <summary>
        /// Copy constructor to create a deep copy of another Settings object
        /// </summary>
        /// <param name="other">The Settings object to copy</param>
        public Settings(Settings other)
        {
            this.Filename = other.Filename;
            this.Verbose = other.Verbose;
            this.PrintRaw = other.PrintRaw;
            this.Speed = other.Speed;
        }

        /// <summary>
        /// Creates a string listing each setting value
        /// </summary>
        /// <returns>A string containing each setting value, each on its own line</returns>
        public override string ToString()
        {
            return $"Filename: {this.Filename}\nVerbose: {this.Verbose}\nPrint Raw: {this.PrintRaw}\nSpeed: {this.Speed}";
        }

        /// <summary>
        /// Serializes the Settings to an .xml file
        /// </summary>
        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(this.GetType());

            try
            {
                FileStream file = File.Create(SettingsFileName);
                serializer.Serialize(file, this);
                file.Close();
            }
            catch (IOException)
            {
                Console.WriteLine($"We could not update {SettingsFileName} because it is in use by another process.  Please close the program using {SettingsFileName} and try again.");
            }
        }
    }
}
