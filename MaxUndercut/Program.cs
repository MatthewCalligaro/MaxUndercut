using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml.Serialization;

namespace MaxUndercut
{
    class Program
    {
        /// <summary>
        /// The TDAmeritrade API key used for requests
        /// </summary>
        const string apikey = "MCALLIGARODEV";

        /// <summary>
        /// Miliseconds in 1 day
        /// </summary>
        const long msPerDay = 1000 * 60 * 60 * 24;

        /// <summary>
        /// The number of minutes in a trading day (9:30 am to 4:00 pm)
        /// </summary>
        const long minPerTradingDay = 390;

        /// <summary>
        /// The number of days to request at one time from the TDAmeritrade API
        /// </summary>
        const int requestSize = 7;

        /// <summary>
        /// The number of days to overlap in each API request to help fill in missing data
        /// </summary>
        const int requestOverlap = 1;

        /// <summary>
        /// The minimum number of data points needed for a result to be included in the output
        /// </summary>
        const int minUndercutPoints = 100;

        /// <summary>
        /// The name of the file created to help the user interpret the output
        /// </summary>
        const string helpFilename = "InterpretingOutputHelp.csv";

        /// <summary>
        /// The number of days of data to analyze using the slow and fast settings respectively
        /// </summary>
        static readonly int[] sampleDays = { 26, 15 };

        /// <summary>
        /// The different numbers of "days to sell" to investigate and report in the output
        /// </summary>
        static readonly int[] timeLimits = { 1, 2, 3, 5 };

        /// <summary>
        /// The different confidence thresholds to investigate and report in the output
        /// </summary>
        static readonly double[] confidences = { 0.95, 0.9, 0.85, 0.8, 0.75, 0.7, 0.65, 0.6, 0.55, 0.5 };

        /// <summary>
        /// The text printed when the user requests help
        /// </summary>
        static readonly string[] helpText =
        {
            "MaxUndercut",
            "Written by Matthew Calligaro, August 2018",
            "",
            ">> Summary",
            "MaxUndercut analyzes a stock's price history over the past month to calculate the average amount that one could undercut/overcut the stock's price with a limit order and have the order execute in a given period of time.  This data can help the user choose the optimal price when placing a limit order to buy or sell.",
            "",
            ">> Disclaimer",
            "MaxUndercut is simply a tool for processing past data and does not claim to predict future behavior.  The confidence intervals provided refer to the training data and do not necessarily correspond to probabilities in the future.",
            "",
            ">> Input Structure",
            "To analyze a list of one or more stocks, enter a list of stock symbols separated by commas without spaces.  For example, to analyze Microsoft and Ford stocks, enter \"MSFT,F\"",
            "",
            "Flags: the following flags can be used to override your default settings for the current command and can be given in any order.",
            "    -o <filename>: sets the output filename.",
            "    -v: sets verbose to true, causing MaxUndercut to print more detailed information to the console while executing.",
            "    -r: sets raw output to true, causing MaxUndercut to save the raw output for each stock analyzed to a file named \"{stock symbol}.csv\" in your working directory.",
            $"    -s <fast or slow>: sets the execution speed to either fast or slow.  When set to \"fast\", MaxUndercut only uses data from the past {sampleDays[Speed.fast.GetHashCode()]} days, which allows for faster execution time but provides less reliable data.",
            "",
            "Example: the command \"MSFT,F,AMZN -v -r -o output.csv -s slow\" will analyze Microsoft, Ford, and Amazon stocks and save the output to output.csv.  This command has verbose and raw output set to true and will execute at slow speed (uses a full month of data).",
            "",
            $"Output: after execution, data is saved in your working directory to a .csv file.  For help interpreting this data, open {helpFilename} which is in your working directory.",
            "",
            ">> Additional Commands",
            "quit: closes the application.",
            $"?: prints the help dialog and creates {helpFilename}.",
            "-o <filename>: sets the default output filename.",
            "-v: toggles the default verbose flag.  When set to true, MaxUndercut will print more detailed information to the console while executing.",
            "-v <true or false>: sets the default verbose flag to either true or false.",
            "-r: toggles the default raw output flag.  When set to true, MaxUndercut will also save the raw output for each stock analyzed to a file named \"{stock symbol}.csv\" in your working directory.",
            "-r <true or false>: sets the raw output flag to either true or false.",
            $"-s <fast or slow>: sets the default execution speed.  When set to \"fast\", MaxUndercut only uses data from the past {sampleDays[Speed.fast.GetHashCode()]} days, which allows for faster execution time but provides less reliable data.",
            "settings: prints the current default settings.",
            "",
            ">> Current Default Settings",
        };

        /// <summary>
        /// The contents of the .csv file created to help the user interpret the output
        /// </summary>
        static readonly string[] helpFile =
        {
            ",Symbol,Average stock price,Data points,Days to buy,Confidence: 0.95 (buy),0.9 (buy),0.85 (buy),0.8 (buy),0.75 (buy),0.7 (buy),0.65 (buy),0.6 (buy),0.55 (buy),0.5 (buy),,Days to sell, Confidence: 0.95 (sell),0.9 (sell),0.85 (sell),0.8 (sell),0.75 (sell),0.7 (sell),0.65 (sell),0.6 (sell),0.55 (sell),0.5 (sell)",
            ",MSFT,$107.94 ,5619,1,($0.01),($0.04),($0.08),($0.12),($0.18),($0.24),($0.32),($0.39),($0.48),($0.56),,1,$0.02 ,$0.07 ,$0.11 ,$0.16 ,$0.21 ,$0.26 ,$0.31 ,$0.37 ,$0.46 ,$0.55",
            ",,,,2,($0.02),($0.07),($0.12),($0.18),($0.26),($0.37),($0.53),($0.71),($0.88),($0.99),,2,$0.03 ,$0.08 ,$0.14 ,$0.21 ,$0.28 ,$0.34 ,$0.43 ,$0.54 ,$0.66 ,$0.79",
            ",,,,3,($0.04),($0.12),($0.24),($0.42),($0.59),($0.71),($0.85),($0.99),($1.09),($1.18),,3,$0.05 ,$0.11 ,$0.20 ,$0.31 ,$0.39 ,$0.57 ,$0.78 ,$0.91 ,$1.04 ,$1.12",
            ",,,,5,($0.05),($0.17),($0.34),($0.50),($0.75),($1.06),($1.16),($1.23),($1.29),($1.38),,5,$0.06 ,$0.14 ,$0.28 ,$0.38 ,$0.58 ,$0.86 ,$1.06 ,$1.18 ,$1.29 ,$1.37",
            ",,,,,,,,,,,,,,,,,,,,,,,,,,",
            "Column Explanations,The symbol of the stock being analyzed,The average price of that stock over the period analyzed,\"The number of data points collected for the stock.  The higher this number is, the more accurate the results will likely be\",\"The time window in which we are willing to wait for a limit order to buy to execute.  For example, when Days to buy is 1, all of the undercut prices given in that row correspond to the maximum undercut if we are willing to wait 1 trading day for the limit buy to execute.\",\"The maximum undercut that provided a 95% confidence that a limit order to buy would execute on past data.  In other words, if we place a limit order to buy that is this value under the current stock price, we would expect that limit order to execute within the Days To buy window 95% of the time.\",The maximum undercut that provided a 90% confidence.,The maximum undercut that provided a 85% confidence.,The maximum undercut that provided a 80% confidence.,The maximum undercut that provided a 75% confidence.,The maximum undercut that provided a 70% confidence.,The maximum undercut that provided a 65% confidence.,The maximum undercut that provided a 60% confidence.,The maximum undercut that provided a 55% confidence.,The maximum undercut that provided a 50% confidence.,,The time window in which we are willing to wait for a limit order to sell to execute.,The maximum overcut that provided a 95% confidence that a limit order to sell would execute on past data. ,The maximum overcut that provided a 90% confidence.,The maximum overcut that provided a 85% confidence.,The maximum overcut that provided a 80% confidence.,The maximum overcut that provided a 75% confidence.,The maximum overcut that provided a 70% confidence.,The maximum overcut that provided a 65% confidence.,The maximum overcut that provided a 60% confidence.,The maximum overcut that provided a 55% confidence.,The maximum overcut that provided a 50% confidence.",
            ",,,,,,,,,,,,,,,,,,,,,,,,,,",
            "Examples,\"Suppose that you wish to purchase shares of Microsoft and MSFT is currently priced at $100.00 per share.  To do so, you would create a limit order to buy that undercut the current share price, using an undercut value given in the data table to the left.\",,,,,,,,,,,,,,,,,,,,,,,,,",
            ",\"For example, if you wanted to place a limit order to buy that theoretically has a 90% probabilty of executing in 1 day, you should price your limit order at $99.96 per share (an undercut of $0.04 based on cell G2).\",,,,,,,,,,,,,,,,,,,,,,,,,",
            ",\"If you wanted your order to have an 80% probability of executing in 3 days, you should price your limit order to buy at $99.58 per share (an undercut of $0.42 based on cell I4).\",,,,,,,,,,,,,,,,,,,,,,,,,",
            ",,,,,,,,,,,,,,,,,,,,,,,,,,",
            ",\"Instead, suppose you wanted to sell shares of Microsoft.  You would create a limit order to sell which overcut the current stock price, using the data table to the right to determine the overcut value.\",,,,,,,,,,,,,,,,,,,,,,,,,",
            ",\"For example, if you wanted your order to have a 75% probability of executing in 1 day, you would price your limit order to sell at $100.21 per share (an overcut of $0.21 based on cell V2).\",,,,,,,,,,,,,,,,,,,,,,,,,",
            ",\"If you wanted your order to have an 85% probability of executing in 5 days, you would price your limit order to sell at $100.28 per share (an overcut of $0.28 based on cell T5).\",,,,,,,,,,,,,,,,,,,,,,,,,",
        };

        /// <summary>
        /// The default settings for AnalyzeSymbols, which can be overriden by flags 
        /// </summary>
        static Settings DefaultSettings;

        /// <summary>
        /// Entry point for the application
        /// </summary>
        /// <param name="args">The arguments passed to the application (not used)</param>
        static void Main(string[] args)
        {
            InitializeSettings();
            bool writePrompt = true;
            while (true)
            {
                if (writePrompt)
                {
                    Console.WriteLine("Please enter a comma separated list of one or more stock symbols or enter \"?\" for help");
                }
                writePrompt = ParseInput(Console.ReadLine());
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Initializes DefaultSettings by either loading from a settings file (if found) or resorting to the default values
        /// </summary>
        static void InitializeSettings()
        {
            // Check if the working directory contains a settings file
            bool savedSettings = false;
            string[] filesInWorkingDirectory = Directory.GetFiles(Directory.GetCurrentDirectory());
            foreach (string file in filesInWorkingDirectory)
            {
                if (new FileInfo(file).Name == Settings.SettingsFileName)
                {
                    savedSettings = true;
                    break;
                }
            }

            // If so, load the settings from teh settings file
            if (savedSettings)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                using (Stream reader = File.OpenRead(Settings.SettingsFileName))
                {
                    DefaultSettings = (Settings)serializer.Deserialize(reader);
                }
            }

            // Otherwise, resort to the default settings
            else
            {
                DefaultSettings = new Settings();
            }
        }

        /// <summary>
        /// Interprets the user's input and takes the corresponding action
        /// </summary>
        /// <param name="rawInput">The exact string that the user entered</param>
        /// <returns>True if the application should print the default prompt</returns>
        static bool ParseInput(string rawInput)
        {
            string[] input = rawInput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // First check for specific commands
            // (Note that none of the cases can be valid stock symbols)
            switch (input[0].ToLower())
            {
                case "":
                    return false;

                case "q":
                case "-q":
                case "quit":
                case "exit":
                    HandleExit();
                    return false;

                case "-h":
                case "help":
                case "?":
                    Help();
                    return false;

                case "-o":
                case "output":
                case "file":
                case "filename":
                    DefaultFilename(input);
                    return false;

                case "-v":
                case "verbose":
                    DefaultVerbose(input);
                    return false;

                case "-r":
                case "raw":
                case "rawoutput":
                    DefaultRaw(input);
                    return false;

                case "-s":
                case "speed":
                    DefaultSpeed(input);
                    return false;

                case "settings":
                    Console.WriteLine("Current Default Settings:");
                    Console.WriteLine(DefaultSettings.ToString());
                    return true;

                case "thanks":
                case "thanks!":
                case "thank":
                    Console.WriteLine("You're welcome!");
                    return true;

                default:
                    break;
            }

            // Otherwise, assume the input is a list of stocks plus flags
            // Extract valid stock symbols
            string[] symbols = input[0].ToUpper().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> validSymbols = new List<string>();
            foreach (string symbol in symbols)
            {
                if (VerifySymbol(symbol))
                {
                    validSymbols.Add(symbol);
                }
                else
                {
                    Console.WriteLine($"{symbol} is not a recognized stock symbol.  For example, use MSFT for Microsoft Corporation");
                }
            }

            if (validSymbols.Count == 0)
            {
                Console.WriteLine($"We could not extract any recognized stock symbols from the list \"{input[0]}\".  Enter \"?\" for help");
                return false;
            }

            // Create temporary settings based on flags
            bool validFlags = true;
            Settings curSettings = new Settings(DefaultSettings);
            for (int i = 1; i < input.Length && validFlags; i++)
            {
                switch (input[i].ToLower())
                {
                    case "-o":
                        if (input.Length > i + 1)
                        {
                            curSettings.Filename = input[i + 1];
                            i++;
                        }
                        else
                        {
                            validFlags = false;
                        }
                        break;

                    case "-v":
                        curSettings.Verbose = true;
                        break;

                    case "-r":
                        curSettings.PrintRaw = true;
                        break;

                    case "-s":
                        if (input.Length > i + 1)
                        {
                            switch (input[i + 1].ToLower())
                            {
                                case "slow":
                                    curSettings.Speed = Speed.slow;
                                    break;

                                case "fast":
                                    curSettings.Speed = Speed.fast;
                                    break;

                                default:
                                    validFlags = false;
                                    break;
                            }
                            i++;
                        }
                        else
                        {
                            validFlags = false;
                        }
                        break;

                    default:
                        validFlags = false;
                        break;
                }
            }

            if (!validFlags)
            {
                Console.WriteLine("One or more of the flags on this request were not recognized.  Enter \"?\" for help");
                return false;
            }

            // Submit the request
            AnalyzeSymbols(validSymbols.ToArray(), curSettings);
            return true;
        }

        /// <summary>
        /// Handles when the user enters the exit command
        /// </summary>
        static void HandleExit()
        {
            Environment.Exit(0);
        }

        /// <summary>
        /// Prints the help text and creates a .csv file in the working directory to help interpret the program output
        /// </summary>
        static void Help()
        {
            foreach (string line in helpText)
            {
                Console.WriteLine(line);
            }
            Console.WriteLine(DefaultSettings.ToString());

            WriteToFile(helpFilename, helpFile);
        }

        /// <summary>
        /// Handles when the user sets the default verbose flag
        /// </summary>
        /// <param name="input">The user input split on spaces</param>
        static void DefaultVerbose(string[] input)
        {
            if (input.Length == 1)
            {
                DefaultSettings.Verbose = !DefaultSettings.Verbose;
                DefaultSettings.Save();
            }
            else if (input.Length == 2)
            {
                switch (input[1].ToLower())
                {
                    case "t":
                    case "true":
                    case "1":
                        DefaultSettings.Verbose = true;
                        DefaultSettings.Save();
                        break;

                    case "f":
                    case "false":
                    case "0":
                        DefaultSettings.Verbose = false;
                        DefaultSettings.Save();
                        break;

                    default:
                        Console.WriteLine($"You tried to set the default verbose flag to {input[1]}, but the flag must be set to either true or false.  (Enter \"?\" for help)");
                        return;
                }
            }
            else
            {
                Console.WriteLine("To set the default verbose flag, use the format \"-v <true or false>\".  (Enter \"?\" for help)");
                return;
            }

            Console.WriteLine($"Default verbose flag is now set to {DefaultSettings.Verbose}.  To view all default settings, enter \"settings\"");
        }

        /// <summary>
        /// Handles when the user sets the default raw output flag
        /// </summary>
        /// <param name="input">The user input split on spaces</param>
        static void DefaultRaw(string[] input)
        {
            if (input.Length == 1)
            {
                DefaultSettings.PrintRaw = !DefaultSettings.PrintRaw;
                DefaultSettings.Save();
            }
            else if (input.Length == 2)
            {
                switch (input[1].ToLower())
                {
                    case "t":
                    case "true":
                    case "1":
                        DefaultSettings.PrintRaw = true;
                        DefaultSettings.Save();
                        break;

                    case "f":
                    case "false":
                    case "0":
                        DefaultSettings.PrintRaw = false;
                        DefaultSettings.Save();
                        break;

                    default:
                        Console.WriteLine($"You tried to set the default raw output flag to {input[1]}, but the flag must be set to either true or false.  (Enter \"?\" for help)");
                        return;
                }
            }
            else
            {
                Console.WriteLine("To set the default raw output flag, use the format \"-v <true or false>\".  (Enter \"?\" for help)");
                return;
            }

            Console.WriteLine($"Default raw output flag is now set to {DefaultSettings.PrintRaw}.  To view all default settings, enter \"settings\"");
        }

        /// <summary>
        /// Handles when the user sets the default output filename
        /// </summary>
        /// <param name="input">The user input split on spaces</param>
        static void DefaultFilename(string[] input)
        {
            if (input.Length == 1)
            {
                Console.WriteLine("You must provide a non-empty output filename.  (Enter \"?\" for help)");
            }
            else if (input.Length == 2)
            {
                DefaultSettings.Filename = input[1];
                DefaultSettings.Save();
                Console.WriteLine($"Default output filename is now set to {DefaultSettings.Filename}.  To view all default settings, enter \"settings\"");
            }
            else
            {
                Console.WriteLine("The output filename cannot contain any spaces.  (Enter \"?\" for help)");
            }
        }

        /// <summary>
        /// Handles when the user sets the default execution speed
        /// </summary>
        /// <param name="input">The user input split on spaces</param>
        static void DefaultSpeed(string[] input)
        {
            if (input.Length == 2)
            {
                switch(input[1].ToLower())
                {
                    case "slow":
                        DefaultSettings.Speed = Speed.slow;
                        DefaultSettings.Save();
                        break;

                    case "fast":
                        DefaultSettings.Speed = Speed.fast;
                        DefaultSettings.Save();
                        break;

                    default:
                        Console.WriteLine($"{input[1]} is not a valid execution speed.  Valid speeds are \"slow\" or \"fast\".  (Enter \"?\" for help)");
                        return;
                }

                Console.WriteLine($"Default execution speed is now set to {DefaultSettings.Speed.ToString()}.  To view all default settings, enter \"settings\"");
            }
            else
            {
                Console.WriteLine("To set the default execution speed, use the format \"-s <slow, fast>\".  (Enter \"?\" for help)");
            }
        }

        /// <summary>
        /// Analyzes a set of stock symbols and prints the output to a single .csv file in the working directory
        /// </summary>
        /// <param name="symbols">The list of stock symbols to analyze</param>
        /// <param name="settings">The settings to use when analyzing each symbol</param>
        static void AnalyzeSymbols(string[] symbols, Settings settings)
        {
            Console.WriteLine("Request Submitted.  Depending on your settings and number of stocks requested, this may take a few moments...");
            var output = new List<string>();

            // Create output header
            string header = "Symbol,Average stock price,Data points,Days to buy,Confidence: ";
            foreach (double confidence in confidences)
            {
                header += $"{confidence} (buy),";
            }
            header += ",Days to sell, Confidence: ";
            foreach (double confidence in confidences)
            {
                header += $"{confidence} (sell),";
            }
            output.Add(header.Substring(0, header.Length - 1));

            // Calculate begining and ending dates for the data window
            long endDate = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, DateTimeOffset.UtcNow.Day, 0, 0, 0, TimeSpan.Zero)
                .Subtract(TimeSpan.FromDays(1)).ToUnixTimeMilliseconds();
            long startDate = endDate - (msPerDay * sampleDays[settings.Speed.GetHashCode()]);

            // Analyze each symbol
            foreach (string symbol in symbols)
            {
                output.AddRange(AnalyzeSymbol(symbol, startDate, endDate, settings));
            }

            // Write output to file
            WriteToFile(settings.Filename, output.ToArray());
            Console.WriteLine($"Results saved to {Directory.GetCurrentDirectory()}\\{settings.Filename}");
        }

        /// <summary>
        /// Determines the maximum undercut and overcut statistics for a single stock over the requested period
        /// </summary>
        /// <param name="symbol">The stock to analyze</param>
        /// <param name="startDate">The begining of the data window</param>
        /// <param name="endDate">The end of the data window</param>
        /// <param name="settings">The settings to use for this analysis</param>
        /// <returns>The lines to be added to the results file</returns>
        static string[] AnalyzeSymbol(string symbol, long startDate, long endDate, Settings settings)
        {
            // Prepare relevant data structures
            int[] minPricePoints = new int[timeLimits.Length];
            string resultsHeader = "Date";
            for (int i = 0; i < timeLimits.Length; i++)
            {
                minPricePoints[i] = MinPricePoints(timeLimits[i]);
                resultsHeader += $",{timeLimits[i]} days to buy,{timeLimits[i]} days to sell";
            }
            List<string> results = new List<string> { resultsHeader };
            List<int[]> undercuts = new List<int[]>();
            List<int[]> overcuts = new List<int[]>();

            // Request data from TDAmeritrade
            if (settings.Verbose)
            {
                Console.WriteLine($"Requesting {symbol} data from TDAmeritrade");
            }
            DataSet data = BuildDataSet(symbol, startDate, endDate);

            // Calculate the maximum undercut for each point in the data set
            if (settings.Verbose)
            {
                Console.WriteLine($"Analyzing {symbol} data");
            }
            long priceSum = 0;
            int nextWeekendIndexIndex = 0;
            int nextWeekendIndex = data.WeekendIndexes[0];
            long lastStartTime = endDate - (timeLimits[timeLimits.Length - 1] * msPerDay);
            for (int i = 0; data.Points[i].Date <= lastStartTime; i++)
            { 
                string resultLine = new DateTime(1970, 1, 1).AddMinutes(data.Points[i].Date / 1000 / 60).ToString("g");
                int[] undercut = new int[timeLimits.Length];
                int[] overcut = new int[timeLimits.Length];
                if (i == nextWeekendIndex)
                {
                    nextWeekendIndexIndex++;
                    nextWeekendIndex = nextWeekendIndexIndex < data.WeekendIndexes.Length ? data.WeekendIndexes[nextWeekendIndexIndex] : int.MaxValue;
                }

                bool badPoint = false;
                int min = data.Points[i].Price;
                int max = min;
                int j = 0;
                for (int k = 0; k < timeLimits.Length; k++)
                {
                    while (i + j < data.Points.Length && data.Points[i + j].Date - data.Points[i].Date < timeLimits[k] * msPerDay + (i + j > nextWeekendIndex ? 2 * msPerDay : 0))
                    {
                        min = data.Points[i + j].Price < min ? data.Points[i + j].Price : min;
                        max = data.Points[i + j].Price > max ? data.Points[i + j].Price : max;
                        j++;
                    }

                    if (j >= minPricePoints[k])
                    {
                        undercut[k] = data.Points[i].Price - min;
                        overcut[k] = max - data.Points[i].Price;
                        resultLine += $",\"-{string.Format("{0:C}", undercut[k] / 100.0)}\",\"+{string.Format("{0:C}", overcut[k] / 100.0)}\"";
                    }
                    else
                    {
                        badPoint = true;
                        break;
                    }
                }

                if (!badPoint)
                {
                    undercuts.Add(undercut);
                    overcuts.Add(overcut);
                    results.Add(resultLine);
                    priceSum += data.Points[i].Price;
                }
            }

            // If raw output flag is true, create a raw output document
            if (settings.PrintRaw)
            {
                string filename = $"{symbol}.csv";
                WriteToFile(filename, results.ToArray());
                Console.WriteLine($"Saved {symbol} raw output to {Directory.GetCurrentDirectory()}\\{filename}");
            }

            // Aggregate undercuts and overcuts into confidence intervals
            List<string> output = new List<string>();
            int[][] undercutsAry = undercuts.ToArray();
            int[][] overcutsAry = overcuts.ToArray();
            if (undercutsAry.Length >= minUndercutPoints)
            {
                for (int i = 0; i < timeLimits.Length; ++i)
                {
                    string line = i == 0 ? $"{symbol},\"{string.Format("{0:C}", priceSum / 100.0 / undercutsAry.Length)}\",{undercutsAry.Length},{timeLimits[i]}" : $",,,{timeLimits[i]}";
                    ArrayComparer comparer = new ArrayComparer(i);
                    Array.Sort(undercutsAry, comparer);
                    for (int j = 0; j < confidences.Length; j++)
                    {
                        line += $",\"-{string.Format("{0:C}", undercutsAry[(int)(undercutsAry.Length * (1 - confidences[j]))][i] / 100.0)}\"";
                    }
                    line += $",,{timeLimits[i]}";                    
                    Array.Sort(overcutsAry, comparer);
                    for (int j = 0; j < confidences.Length; j++)
                    {
                        line += $",\"+{string.Format("{0:C}", overcutsAry[(int)(overcutsAry.Length * (1 - confidences[j]))][i] / 100.0)}\"";
                    }
                    output.Add(line);
                }
            }
            else
            {
                output.Add($"{symbol},\"{string.Format("{0:C}", priceSum / 100.0 / Math.Max(undercutsAry.Length, 1))}\",{undercutsAry.Length},Insufficient Data");
            }
            output.Add("");

            return output.ToArray();
        }

        /// <summary>
        /// Builds a dataset containing the price history for a given stock
        /// </summary>
        /// <param name="stockSymbol">The stock to investigate</param>
        /// <param name="startDate">The begining of the time window to investigate</param>
        /// <param name="endDate">The end of the time window to investigate</param>
        /// <returns>A DataSet object containing the price history from the period requested</returns>
        static DataSet BuildDataSet(string stockSymbol, long startDate, long endDate)
        {
            /* The TDAmeritrade PriceHistory API has a number of issues:
             * 1. extra candles are often included outside of the start and end date
             * 2. requests will often have missing values which may show in a different request with different start and end dates
             * 3. data is often cut off for requests across a larger time window (although the maximum size is inconsistant)
             * 4. minute-level candles are not given for requests dating back more than roughly 27 days
             * 
             * To maximize the amount of valid data points while maintaining reasonable performance, we request minute-level candles accross the previous 
             * {sampleDays} days in {requestSize} day blocks, with each block overlapping the previous by {requestOverlap} days.  
             * We then process this data to remove duplicates and candles outside of the requested range.
             */
            
            // Request price history data in small, overlapping blocks
            List<PriceHistory> histories = new List<PriceHistory>();
            int numRawPoints = 0;
            for (long start = startDate; start < endDate; start += (requestSize - requestOverlap) * msPerDay)
            {
                long end = endDate < start + requestSize * msPerDay ? endDate : start + requestSize * msPerDay;
                PriceHistory nextHistory = RequestData(stockSymbol, start, end);
                numRawPoints += nextHistory.candles.Length;
                histories.Add(nextHistory);
            }

            // Aggregate all of the data points into rawPoints
            Point[] rawPoints = new Point[numRawPoints];
            int i = 0; 
            foreach (PriceHistory history in histories)
            {
                foreach (Candle candle in history.candles)
                {
                    rawPoints[i] = new Point
                    {
                        Date = candle.datetime,
                        Price = (int)(candle.low * 100)
                    };
                    i++;
                }
            }

            // Sort, remove data points outside of the request window, dedupe, and calculate weekends and average price
            Array.Sort(rawPoints);
            int index = 0;
            long prevDate = rawPoints[0].Date - 1;
            List<Point> points = new List<Point>();
            List<int> weekendIndexes = new List<int>();
            long priceSum = 0;
            foreach (Point point in rawPoints)
            {
                if (point.Date >= startDate && point.Date <= endDate && point.Date > prevDate)
                {
                    if (point.Date - prevDate > 2 * msPerDay)
                    {
                        weekendIndexes.Add(index);
                    }
                    prevDate = point.Date;
                    priceSum += point.Price;
                    points.Add(point);
                    index++;
                }
            }

            return new DataSet
            {
                Points = points.ToArray(),
                WeekendIndexes = weekendIndexes.ToArray(),
                AveragePrice = (int)(priceSum / points.Count)
            };
        }

        /// <summary>
        /// Requests the price history for a given stock from the TDAmeritrade PriceHistory API
        /// </summary>
        /// <param name="stockSymbol">The stock to request</param>
        /// <param name="startDate">The begining of the time window to request</param>
        /// <param name="endDate">The end of the time window to request</param>
        /// <returns>A PriceHistory object containing the information in the JSON returned from the API</returns>
        static PriceHistory RequestData(string stockSymbol, long startDate, long endDate)
        {
            string uri = $"https://api.tdameritrade.com/v1/marketdata/{stockSymbol}/pricehistory?apikey={apikey}%40AMER.OAUTHAP&frequencyType=minute&frequency=1&endDate={endDate}&startDate{startDate}&needExtendedHoursData=false";
            return RequestData(uri);
        }

        /// <summary>
        /// Requests the price history for a given stock from the TDAmeritrade PriceHistory API
        /// </summary>
        /// <param name="uri">The URI encoding the request to the API</param>
        /// <returns>A PriceHistory object containing the information in the JSON returned from the API</returns>
        static PriceHistory RequestData(string uri)
        {
            HttpWebRequest request = WebRequest.CreateHttp(uri);
            string json;

            using (StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                json = sr.ReadLine();
            }

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(new PriceHistory().GetType());
                return ser.ReadObject(ms) as PriceHistory;
            }
        }

        /// <summary>
        /// Checks if a potential stock symbol is listed in TDAmeritrade 
        /// </summary>
        /// <param name="stockSymbol">The stock symbol to verify</param>
        /// <returns>True if the stock symbol is listed in TDAmeritrade</returns>
        static bool VerifySymbol(string stockSymbol)
        {
            // First, check that the symbol only contains valid characters
            foreach (char letter in stockSymbol)
            {
                if (!Char.IsLetterOrDigit(letter) && letter != '.')
                {
                    return false;
                }
            }

            // Otherwise, use the PriceHistory API to check if TDAmeritrade contains price history for the potential stock
            string uri = $"https://api.tdameritrade.com/v1/marketdata/{stockSymbol}/pricehistory?apikey={apikey}%40AMER.OAUTHAP&periodType=day&period=1&frequencyType=minute&frequency=30&needExtendedHoursData=false";
            return !RequestData(uri).empty;
        }

        /// <summary>
        /// Writes a set of lines to a file and handles if the file is locked for writting
        /// </summary>
        /// <param name="filename">The name of the file to which to write</param>
        /// <param name="lines">An array of lines to write to the file</param>
        /// <returns>True if the write is successful</returns>
        static bool WriteToFile(string filename, string[] lines)
        {
            try
            {
                File.WriteAllLines(filename, lines);
            }
            catch (IOException)
            {
                Console.WriteLine($"We could not write to {Directory.GetCurrentDirectory()}\\{filename} because it is in use by another process.  Please close the program using {filename} and try again.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gives the minimum number of price points needed in a given time period for an undercut/overcut calculation to be deemed valid
        /// </summary>
        /// <param name="timeLimit">The days to buy/sell for the undercut/overcut calculation</param>
        /// <returns>The minimum number of prices points needed for the given time period</returns>
        static int MinPricePoints(int timeLimit)
        {
            return (int)(timeLimit * minPerTradingDay * 0.1);
        }
    }
}
