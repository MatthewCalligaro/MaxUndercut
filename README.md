# MaxUndercut
Written by Matthew Calligaro, August 2018

## Summary
MaxUndercut analyzes a stock's price history over the past month to calculate the average amount that one could undercut/overcut the stock's price with a limit order and have the order execute in a given period of time.  This data can help the user choose the optimal price when placing a limit order to buy or sell.

## Disclaimer
MaxUndercut is simply a tool for processing past data and does not claim to predict future behavior.  The confidence intervals provided refer to the training data and do not necessarily correspond to probabilities in the future.

## Input Structure
To analyze a list of one or more stocks, enter a list of stock symbols separated by commas without spaces.  For example, to analyze Microsoft and Ford stocks, enter `MSFT,F`

### Flags
The following flags can be used to override your default settings for the current command and can be given in any order.
* `-o <filename>`: sets the output filename.
* `-v`: sets verbose to true, causing MaxUndercut to print more detailed information to the console while executing.
* `-r`: sets raw output to true, causing MaxUndercut to save the raw output for each stock analyzed to a file named "{stock symbol}.csv" in your working directory.
* `-s <fast or slow>`: sets the execution speed to either fast or slow.  When set to "fast", MaxUndercut only uses data from the past 15 days, which allows for faster execution time but provides less reliable data.

### Example
The command `MSFT,F,AMZN -v -r -o output.csv -s slow` will analyze Microsoft, Ford, and Amazon stocks and save the output to output.csv.  This command has verbose and raw output set to true and will execute at slow speed (uses a full month of data).

### Output
After execution, data is saved in your working directory to a .csv file.  Running the help command will create InterpretingOutputHelp.csv in your working directory, which explains how to interpret this output.

## Additional Commands
* `quit`: closes the application.
* `?`: prints the help dialog and creates InterpretingOutputHelp.csv.
* `-o <filename>`: sets the default output filename.
* `-v`: toggles the default verbose flag.  When set to true, MaxUndercut will print more detailed information to the console while executing.
* `-v <true or false>`: sets the default verbose flag to either true or false.
* `-r`: toggles the default raw output flag.  When set to true, MaxUndercut will also save the raw output for each stock analyzed to a file named "{stock symbol}.csv" in your working directory.
* `-r <true or false>`: sets the raw output flag to either true or false.
* `h-s <fast or slow>`: sets the default execution speed.  When set to "fast", MaxUndercut only uses data from the past 15 days, which allows for faster execution time but provides less reliable data.
* `settings`: prints the current default settings.
