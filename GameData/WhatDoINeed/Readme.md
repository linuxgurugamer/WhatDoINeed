# What Do I Need?

### This is a simple mod designed to show on-screen what science parts are needed to support the selected contracts.  The display can be made transparent and click-through can be allowed to allow it to not interfere with the editor.  It is only available in the Editor (VAB or SPH)

### The initial screen will initially show all active contracts.  At the bottom there will be three buttons:
	* Select		Click to open contract selection screen
	* Close		Close the window
	* Settings	Open the settings panel

### The Contract Selection screen will show a list of all active contracts.  You can deselect some or all of the active contracts on this screen.  Click the toggle to select those contracts you want to display or deselect, then click the Select Button

### The Settings page will display three toggles on the first line:
	* Display Briefing		If checked, then the briefing for the contract will be shown
	* Bold					Make the display font bold
	* Lock Position			Clock the window position
	* Hide Buttons			Hide the buttons at the bottom when the selection screen is closed

### The next two lines will have the following toggles:
	* Allow click-through	If enabled, this will have the contract display window to 
							allow clicks to fall through to game objects below.  This 
							will only be enabled when the settings panel is NOT displayed
	* Show all active contracts upon entry	If enabled (defaults to enabled), then entering the editor will
							automatically populate all the active contracts in the mod

### The following sliders are available:
	* Transparency			Set the transparency of the window, all the way left makes the 
							window totally transparent
	* Hide Time				Sets the amount of time the window will hide when the H button 
							in the upper right is pressed
	* Font Size				Set the size of the font to be used

### Additional controls are in the upper left:
	* B					Hide or show the buttons
	* L					Lock the position of the window.  When locked, the window cannot be moved or resized
	* S					Show the Settings panel

### Upper right:
	* X					Close the window

### and lower right:
	* diagonal arrow		Resize the window


The window will show the selected active contracts.  Initially all experiments will be shown, along with all 
the parts that can provide the experiment needed.  There is a button to the left of each contract, which will
allow you to collapse the experiments included in that contract.

## Font Colors
	* Contacts will be shown in yellow.
	* If enabled, the briefing text will be white
	* Experiments will be shown in a light blue.  
	* Fulfilling parts (parts which can run the experiment):  The parts will be shown in either red or green.  
	  Red means it is NOT part of the current vessel, green means it IS on the current vessel.  Additionally, 
	  for parts which are already on the vessel, the number of those parts on the vessel will be shown in 
	  parenthesis to the right of the part name

## Supported Mods
	* Orbital Science
	* ScanSAT

## DEPENDENCIES
	* ClickThroughBlocker
	* ToolbarController
	* SpaceTuxLibrary
	* ContractParser


