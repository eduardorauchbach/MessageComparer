## Fixes & Features
- **2023-01-30**: Possibility to customize the Message Titles to prevent mislead information when passing the Save to another person.
- **2023-01-27**: Wrong behavior when parsing a XML dictionary to a Json List;
- **2023-01-26**: Xml wrong behavior when trying to compare some values;
- **2023-02-17**: Tab delimited CSV fix, no header CSV fix, different column count per row fix;

# Description

##The problem
When we have to compare 2 massages with a similar structure but no common rows ordering, or different properties ordering, or no common format, we need to do it manually and it can take some time. Even using tools like the Notepad++ compare will not solve too much out issues.

##Solution
This tool can get messages in XML, CSV and Json , parse them, reorder the properties, reorder the rows using the [**Keys Configurations**](#Keys-Configurations) and compare the results side by side as a json.

- XML - Simple parsing to Json
- CSV - Will generate a array named **Rows** at the root, where all the content of each line will be placed, than parse to Json
- Json - Deserialize
<br/>

# How to use it

## Keys Configurations
<br/>
This configuration determine the sequence used when ordering rows using the parent as an anchor. It accepts multiple configurations.

- Parent: Name of the array that needs to be reordered.
- Primary Keys: Array of keys separated by ';'
  - The primary key priority order will be from left to right.
  - If no rows match with this keys, one key per cycle will be removed and a new match will be tried until no PKs are left.
- Sorter Keys: Array of keys used only to order the rows, from left to right.

<br/>

## Message 1 and 2
<br/>
No secrets, just copy and paste the 2 messages to parse, the structure needs to match no mattering the type. In the image we have a XML and a Json, both has hierarch 'Payload / Ledger' as the array in the same level, so it can match the results.
<br/>
<br/>

<span style="color:orange">**Clicking in the "Message 1 or Message 2" titles allows edition, and this information will be saved if Save is triggered**</span>
<br/>

## Comparison Zone
<br/>
Simple the output of the comparison, if any difference is found the row will be displayed pink.
The button 'P' looks for a previous mismatch and the 'N' the next one, if any.
<br/>

## Arrange
Executes the application.
<br/>

## Save and Load

Saves the configurations and the messages, without the output, so you can continue the analysis later if you need.


