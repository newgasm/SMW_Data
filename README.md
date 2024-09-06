# SMW Data

GUI for Super Mario World Data.

Includes interface for:
- Death Counts (Current Level, Total Deaths)
- Timers (Current Level, Last Level, Total Time)
- Exit Counts (Current Level, Total Levels)
- Hack Data (Title, Author(s), Exit Count, etc.)
- Functionality to change colors, fonts, timer accuracy, and Mario death image
- Functionality to save/load/clear data
- Qusb2Snes connectivity through WebSocket.

![image](https://github.com/user-attachments/assets/b4964f4f-757a-4e0c-ae9f-1796ea0d88eb)

Death Counts inspired by germdove... go check him out at twitch.tv/germdove or tiktok.com/@germdove

_____________________________________________________

Installation:
1) Download the SMW_Data.zip file
2) Unzip and add the SMW_Data Folder to the ./QUsb2Snes/apps directory
3) Restart QUsb2Snes if and instance is already runnning
4) Right Click on QUsb2Snes tray icon --> Select "Applications" --> Select "SMW Data"  
![image](https://github.com/user-attachments/assets/61bf81f2-b5db-4516-a0dd-23b757ddb521)

_____________________________________________________

Instructions for Use:
1) Enter Hack Name in the Hack Name text field and click the "Update Hack Info". This will automatically update the Title, Creator(s), and Exit Count.
   Note: Click on "See Hack Data" to display information about the hack.
2) In the File Menu, select the Colors, Fonts, Timer Accuracy, and Death Image to display.
3) Click the "Show Switch Exits" if you want to display the switch exit count separately from the current exit count (some hacks do not include switch exits in the total exit count).
4) Turn on the console or emulator and click the "Connect to WebSocket" button. This will automatically find the device you are using to play the game.
5) Use the Level/Last/Total text fields to manually set the Level, Last Level, and/or Total Times.
6) Use the Level Death Count and Total Death Count buttons to manually set the deaths
7) Use the "Set Total Exits" to override the total exit count pulled in from the hack data. This could be useful if you are playing any% instead of 100%. 
   Note: the "Set Current Exits" can only be used if not connected to the QUsb2Snes websocket. This function was added to enable offline use of the software.
8) Click on "Start Timer"
   Note: If you want the timer to start automatically when the player select is chosen, check the "Auto-Start Timer?" checkbox.
9) Data can be saved, loaded, or cleared using from the "Save/Load" menu.
   
Once connected to WebSocket and In-Game, the current exit count, timer data, and death counts will update automatically.

_____________________________________________________

