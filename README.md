# Falcon BMS Alternative Launcher
<img width="1024" height="768" alt="Falcon BMS Alternative Launcher" src="https://github.com/user-attachments/assets/2cce1b3a-a006-4c5f-a9cd-f1a92a7e630c" />

The **Alternative Launcher** is a modern utility for assigning key, axis, and configuration settings used before launching Falcon BMS.

It **replaces** the need to configure controls inside the in-game Setup screen. When BMS is launched through the Alternative Launcher, it automatically configures and applies proper setup files ensuring your devices stay in the correct order every time you launch.

The Alternative Launcher works with every version of Falcon BMS and supports systems with multiple version of BMS installed simultaneously.

## Quick Links
* [Launcher Tab](#launcher-tab)
* [AxisAssign Tab](#axisassign-tab)
* [KeyMapping Tab](#keymapping-tab)
 
# Using the Alternative Launcher
The Alternative Launcher includes several pages (tabs) that help configure different parts of Falcon BMS.

## LAUNCHER Tab
<img width="537" height="472" alt="Launcher tab" src="https://github.com/user-attachments/assets/c550e183-fba5-448d-8631-20ea5bf997d2" />

* **News** – Displays recent official announcements from the Falcon BMS forum
* **Theater** – Select which theater BMS loads when launching BMS (if multiple theaters are installed)
* **Documentation** – Opens the \Docs folder of the selected BMS version
* **Virtual / Mixed Reality** – Enables VR or mixed-reality output, if you use a headset
* **Export RTT Textures** – Enables cockpit displays (MFDs/HUD/etc.) to be exported to external monitors and devices

### Launcher Options
<img width="209" height="321" alt="Launcher Options" src="https://github.com/user-attachments/assets/ed2f3a64-3cb4-4d94-b51a-9d3feb6c9ed7" />

* **AMCI** – Enables ACMI recording
* **Window** – Enables running BMS in windowed mode
* **NoMovie** – Enabling this disables the intro and in-game videos
* **EyeFly** – Enables the free camera mode that lets you move the camera independently of aircraft
* **Debug** – Enables debug console which also writes xlog files to the User/Logs folder

### Tools and Utilities
<img width="659" height="171" alt="Tools and Utilities" src="https://github.com/user-attachments/assets/53f3a4e6-c015-4add-b75c-7f4fb78bc873" />

These launch external tools and utilities:
* **Update** – Checks for and installs BMS updates
* **Config** – Opens the Falcon BMS Configurator for advanced settings
* **Display Config** – Opens the Falcon BMS Display Selector, used to assign which monitors/displays BMS should use
* **RTT Client** – Displays exported cockpit textures (MFD/HUD/HSD/etc.) on external devices
* **RTT Server** – Broadcasts RTT cockpit textures to connected clients
* **IVC Client (MP Radio)** – Multiplayer radio/voice chat client for multiplayer flights
* **IVC Server (MP Radio)** – Hosts an IVC radio server for multiplayer flights
* **Avionics Configurator** – Customize each type of aircraft
* **Editor** – Falcon BMS Mission Editor (campaign/tactical/ground object editing)
* **Weapon Delivery Planner** – Preflight planning, DTC generation, steerpoints, weapon profiles, package planning
* **Mission Commander** – Campaign building and editing tool
* **Weather Commander** – Create and edit weather maps
* **F4Wx Real Weather** – Generates weather based off real forecasts 
* **F4 RADAR C2 (AWACS) Tool** – External radar tool, displays ATC/AWACS like contact maps
 
### Version Selection
<img width="229" height="83" alt="Version Selection" src="https://github.com/user-attachments/assets/87cae7d7-1207-4307-bd7b-0be5b726866c" />

Choose which version of BMS to launch (if you have multiple installs).

### Launch Without Applying Overrides
<img width="220" height="33" alt="Launch Without Applying Overrides" src="https://github.com/user-attachments/assets/6ea1750a-c67a-46db-bd56-be4050d0ce18" />

Runs BMS without using any Alternative Launcher settings (no saved settings are used).

### LAUNCH Button
<img width="220" height="83" alt="LAUNCH" src="https://github.com/user-attachments/assets/2f413e93-3718-4e5c-a273-586a58fffaa0" />

Starts the selected version of BMS.


## AXISASSIGN Tab

### To assign an axis
<img width="504" height="236" alt="axis assign popup" src="https://github.com/user-attachments/assets/5ab1bfa9-7229-4ee4-aa0d-4efaf9d7aeac" />

1. Click "Assign" next to the control you want to map

1. In the popup window, move the control you want to bind

    * The blue bar will react to your input

    * The detected axis name will appear in the center box (e.g., "Axis X : Joystick – HOTAS Warthog")

1. Adjust options as needed:

    * Invert – reverses the direction of the axis

    * DeadZone – removes small unwanted movements around center

    * Saturation – controls where the maximum travel point is detected, used when the device cannot reach it's full travel

1. Click "Save" to apply the mapping.

Clicking "Clear" removes the current assignment. The Alternative Launcher will then listen for the next control move you make and automatically assign that input to this axis.

To close the popup without saving anything, click the "X" button in the top-right corner.


### Flight Control & Avionics Subtab
<img width="1024" height="776" alt="AXISASSIGN Tab" src="https://github.com/user-attachments/assets/e2934213-0051-452f-b6b7-54f4bced2cff" />

Assign axis for:
* Stick
* Throttle
* Rudder
* Avionics inputs

### ICP, Radios, Audio & Altimeter Subtab
<img width="1024" height="776" alt="ICP, Radios, Audio & Altimeter Subtab" src="https://github.com/user-attachments/assets/ee9756b1-4685-40c9-9bcd-e83634ccea87" />

Assign axis for:
* ICP
* HSI
* Altimeter
* Audio panel
 
### VIEW Subtab
<img width="1024" height="776" alt="VIEW Tab" src="https://github.com/user-attachments/assets/48b2020f-b5c9-473e-a29d-64c683af7038" />

Used to configure camera and cockpit view behaviors.

TrackIR options:
* Head Forward – Leaning forward moves your head physically forward
* Zoom FOV – Leaning forward acts as a zoom

View options:
* Field-of-view settings
* View distance options
* External mouse look on/off
* In-cockpit pilot model on/off
* Smart scaling on/off
 
## KEYMAPPING Tab
<img width="1024" height="776" alt="KEYMAPPING Tab" src="https://github.com/user-attachments/assets/e42fa8e8-fe90-41cb-88dc-981a3b843de0" />

This tab lets you assign buttons to your keyboard, HOTAS, MFDs, button boxes, and any other device. Each device is organized into its own column.

### Filtering Options
<img width="1004" height="54" alt="Filtering Options" src="https://github.com/user-attachments/assets/3fbe9d98-f5d3-4b97-8798-cdcbfe00903a" />

* **Profile** – Switch between assigning F-16 and F-15 controls
* **Category** – Filter mappings by cockpit area
* **Filter** – Search by text

### DX vs Key Assignments
* DX — DirectX button assignment, used for mapping HOTAS, sticks, throttles, MFDs, and other physical devices

* Key — Keyboard assignment (e.g., Ctrl+Alt+D, Shift+K, etc.)

### How to Map a Key
<img width="512" height="236" alt="How to Map a Key" src="https://github.com/user-attachments/assets/0025a513-0ecb-4193-8c7f-b2fb38499fed" />

1. Find the command you want to map in the table

1. Double-click the command

1. When the popup appears, press the button you want to assign

    * The detected input (e.g., Ctrl+Alt D1; Joy 3 DX1) will appear in the textbox

1. Click "Save" to apply the mapping.

To test a mapping, simply press the button you want to test. The table will jump to the command associated with that key, and the button you pressed will be shown in the preview area below the table.

<img width="471" height="25" alt="assignment" src="https://github.com/user-attachments/assets/bb5f0a97-365e-4c9d-9115-7f61524173da" />

#### Clearing Keys
* Clicking "Clear DX" removes the current DX assignment, leaves Key intact.
* Clicking "Clear Key" removes the current keyboard assignment, leaves DX intact.

After clearing an assignment, the Alternative Launcher will listen for the next control input you make and automatically assign it to this mapping.

To close the popup without saving anything, click the "X" button in the top-right corner.

 
# Files Overwritten by the Launcher
When you start BMS through the Alternative Launcher, it overwrites the following setup files and relevant registry entries.

The app **automatically generates backups** in: User/Config/Backup/

Files affected:
* User/Config/axismapping.dat
* User/Config/DeviceSorting.txt
* User/Config/Falcon bms.cfg
* User/Config/joystick.cal
* User/Config/.pop
 
 
# For Developers

### Required SDKs / Tools
* **DirectX SDK**
 https://www.microsoft.com/en-us/download/details.aspx?id=6812
* **Microsoft Visual Studio Installer Projects**
https://marketplace.visualstudio.com/items?itemName=visualstudioclient.MicrosoftVisualStudio2017InstallerProjects
* **BitSwarm.exe**
 https://github.com/SuRGeoNix/BitSwarm
Place bitswarm.exe and BMSUpdate.xml in bin/Debug or bin/Release

### Setup Notes
* Restore NuGet packages after cloning
* Update references on first launch of .sln
* Disable Managed Debugging Assistants → LoaderLock
 
# Disclaimer
The appearance of U.S. Department of Defense (DoD) visual information does not imply or constitute DoD endorsement.
 

